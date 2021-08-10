using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.FOBs
{
    public class FOBManager
    {
        public static Config<FOBConfig> config;
        internal static readonly List<FOB> Team1FOBs = new List<FOB>();
        internal static readonly List<FOB> Team2FOBs = new List<FOB>();

        public FOBManager()
        {
            config = new Config<FOBConfig>(Data.FOBStorage, "config.json");
        }

        public static void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (data.barricade.id == config.Data.FOBID)
            {
                TryDeleteFOB(instanceID, data.group.GetTeam(), drop.model.TryGetComponent(out BarricadeOwnerDataComponent o) ? o.lastDamaged : 0);
            }
        }

        public static void LoadFobs()
        {
            GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1FOBBarricades,
                out List<BarricadeDrop> Team2FOBBarricades
                );

            Team1FOBs.Clear();
            Team2FOBs.Clear();

            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                Team1FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team1FOBBarricades[i]));
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                Team2FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team2FOBBarricades[i]));
            }
            UpdateUIAll();
        }
        public static void RegisterNewFOB(BarricadeDrop Structure)
        {
            ulong team = Structure.GetServersideData().group.GetTeam();
            if (Data.Gamemode is Gamemodes.Flags.TeamCTF.TeamCTF ctf && ctf.GameStats != null)
            {
                if (F.TryGetPlaytimeComponent(Structure.GetServersideData().owner, out PlaytimeComponent c) && c.stats != null)
                    c.stats.fobsplaced++;
                if (team == 1)
                {
                    ctf.GameStats.fobsPlacedT1++;
                } else if (team == 2)
                {
                    ctf.GameStats.fobsPlacedT2++;
                }
            }
            if (team == 1)
            {
                for (int i = 0; i < Team1FOBs.Count; i++)
                {
                    if (Team1FOBs[i].Number != i + 1)
                    {
                        Team1FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Structure));
                        return;
                    }
                }

                Team1FOBs.Add(new FOB("FOB" + (Team1FOBs.Count + 1).ToString(Data.Locale), Team1FOBs.Count + 1, Structure));
            }
            else if (team == 2)
            {
                for (int i = 0; i < Team2FOBs.Count; i++)
                {
                    if (Team2FOBs[i].Number != i + 1)
                    {
                        Team2FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Structure));
                        return;
                    }
                }

                Team2FOBs.Add(new FOB("FOB" + (Team2FOBs.Count + 1).ToString(Data.Locale), Team2FOBs.Count + 1, Structure));
            }

            UpdateUIForTeam(team);
        }

        public static void TryDeleteFOB(uint instanceID, ulong team, ulong player)
        {
            FOB removed;
            if (team == 1)
            {
                removed = Team1FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team1FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else if (team == 2)
            {
                removed = Team2FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team2FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else removed = null;


            if (Data.Gamemode is Gamemodes.Flags.TeamCTF.TeamCTF ctf && ctf.GameStats != null && ctf.State == Gamemodes.EState.ACTIVE)
            // doesnt count destroying fobs after game ends
            {
                if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c) && c.stats != null)
                    c.stats.fobsdestroyed++;
                if (team == 1)
                {
                    ctf.GameStats.fobsDestroyedT2++;
                }
                else if (team == 2)
                {
                    ctf.GameStats.fobsDestroyedT1++;
                }
                UCPlayer ucplayer = UCPlayer.FromID(player);
                if (ucplayer != null)
                {
                    if (ucplayer.GetTeam() == team)
                    {
                        XP.XPManager.AddXP(ucplayer.Player, ucplayer.GetTeam(), XP.XPManager.config.Data.FOBTeamkilledXP, F.Translate("xp_fob_teamkilled", player));
                    }
                    else
                    {
                        XP.XPManager.AddXP(ucplayer.Player, ucplayer.GetTeam(), XP.XPManager.config.Data.FOBKilledXP, F.Translate("xp_fob_killed", player));
                    }
                }
                    
            }
            UpdateUIForTeam(team);
        }

        public static List<FOB> GetAvailableFobs(UnturnedPlayer player)
        {
            if (TeamManager.IsTeam1(player))
            {
                return Team1FOBs;
            }
            else if (TeamManager.IsTeam2(player))
            {
                return Team2FOBs;
            }

            return new List<FOB>();
        }

        public static void GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1Barricades,
                out List<BarricadeDrop> Team2Barricades
                )
        {
            IEnumerable<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            Team1Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam1(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
            Team2Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam2(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
        }

        public static bool FindFOBByName(string name, ulong team, out FOB fob)
        {
            if (team == 1)
            {
                fob = Team1FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            else if (team == 2)
            {
                fob = Team2FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            fob = null;
            return false;
        }

        public static void UpdateUI(UCPlayer player)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                FOBList = Team1FOBs;
            }
            else if (team == 2)
            {
                FOBList = Team2FOBs;
            }
            else return;
            ushort UINumber = 0;
            for (int i = 0; i < config.Data.FobLimit; i++)
            {
                if (i >= FOBList.Count)
                {
                    EffectManager.askEffectClearByID(unchecked((ushort)(config.Data.FirstFOBUiId + i)), player.Player.channel.owner.transportConnection);
                } else
                {
                    if (UINumber >= 10)
                        break;

                    if (FOBList[i] == null || FOBList[i].Structure.GetServersideData().barricade.isDead)
                        continue;

                    EffectManager.sendUIEffect(unchecked((ushort)(config.Data.FirstFOBUiId + UINumber)), unchecked((short)(config.Data.FirstFOBUiId + UINumber)),
                        player.Player.channel.owner.transportConnection, true, F.Translate("fob_ui", player.Steam64, FOBList[i].Name, FOBList[i].ClosestLocation));
                    UINumber++;
                }
            }
        }
        public static void UpdateUIAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                UpdateUI(player);
            }
        }
        public static void UpdateUIForTeam(ulong team)
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team))
            {
                UpdateUI(player);
            }
        }
    }

    public class FOB
    {
        public string Name;
        public int Number;
        public BarricadeDrop Structure;
        public DateTime DateCreated;
        public string ClosestLocation;
        public FOB(string Name, int number, BarricadeDrop Structure)
        {
            this.Name = Name;
            Number = number;
            this.Structure = Structure;
            DateCreated = new DateTime(DateTime.Now.Ticks);
            ClosestLocation = 
                LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Cast<LocationNode>()
                .Aggregate((n1, n2) =>
                    (n1.point - Structure.model.position).sqrMagnitude <= (n2.point - Structure.model.position).sqrMagnitude ? n1 : n2)
                .name;
        }
    }

    public class FOBConfig : ConfigData
    {
        public ushort Team1BuildID;
        public ushort Team2BuildID;
        public ushort Team1AmmoID;
        public ushort Team2AmmoID;
        public ushort FOBBaseID;
        public ushort FOBID;
        public ushort FOBRequiredBuild;
        public int FOBBuildPickupRadius;
        public byte FobLimit;

        public float AmmoCommandCooldown;
        public ushort AmmoCrateBaseID;
        public ushort AmmoCrateID;
        public ushort AmmoCrateRequiredBuild;
        public ushort RepairStationBaseID;
        public ushort RepairStationID;
        public ushort RepairStationRequiredBuild;
        public ushort MortarID;
        public ushort MortarBaseID;
        public ushort MortarRequiredBuild;
        public ushort MortarShellID;

        public List<Emplacement> Emplacements;
        public List<Fortification> Fortifications;
        public List<ushort> LogiTruckIDs;
        public List<ushort> AmmoBagIDs;

        public float DeloyMainDelay;
        public float DeloyFOBDelay;

        public bool EnableCombatLogger;
        public uint CombatCooldown;

        public bool EnableDeployCooldown;
        public uint DeployCooldown;
        public bool DeployCancelOnMove;
        public bool DeployCancelOnDamage;

        public bool ShouldRespawnAtMain;
        public bool ShouldWipeAllFOBsOnRoundedEnded;
        public bool ShouldSendPlayersBackToMainOnRoundEnded;
        public bool ShouldKillMaincampers;

        public ushort FirstFOBUiId;

        public override void SetDefaults()
        {
            Team1BuildID = 38312;
            Team2BuildID = 38313;
            Team1AmmoID = 38314;
            Team2AmmoID = 38315;
            FOBBaseID = 38310;
            FOBID = 38311;
            FOBRequiredBuild = 20;
            FOBBuildPickupRadius = 20;
            FobLimit = 10;

            AmmoCrateBaseID = 38316;
            AmmoCrateID = 38317;
            AmmoCrateRequiredBuild = 3;
            AmmoCommandCooldown = 5f;

            RepairStationBaseID = 38318;
            RepairStationID = 38319;
            RepairStationRequiredBuild = 10;

            MortarID = 38313;
            MortarBaseID = 38336;
            MortarRequiredBuild = 10;
            MortarShellID = 38330;

            LogiTruckIDs = new List<ushort>() { 38305, 38306, 38311, 38312 };
            AmmoBagIDs = new List<ushort>() { 38398 };

            Fortifications = new List<Fortification>() {
                new Fortification
                {
                    base_id = 38350,
                    barricade_id = 38351,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38352,
                    barricade_id = 38353,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38354,
                    barricade_id = 38355,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38358,
                    barricade_id = 38359,
                    required_build = 1
                }
            };

            Emplacements = new List<Emplacement>() {
                new Emplacement
                {
                    baseID = 38345,
                    vehicleID = 38316,
                    ammoID = 38302,
                    ammoAmount = 2,
                    requiredBuild = 6
                },
                new Emplacement
                {
                    baseID = 38346,
                    vehicleID = 38317,
                    ammoID = 38305,
                    ammoAmount = 2,
                    requiredBuild = 6
                },
                new Emplacement
                {
                    baseID = 38342,
                    vehicleID = 38315,
                    ammoID = 38341,
                    ammoAmount = 1,
                    requiredBuild = 10
                },
                new Emplacement
                {
                    baseID = 38339,
                    vehicleID = 38314,
                    ammoID = 38338,
                    ammoAmount = 1,
                    requiredBuild = 10
                },
                new Emplacement
                {
                    baseID = 38336,
                    vehicleID = 38313,
                    ammoID = 38330,
                    ammoAmount = 3,
                    requiredBuild = 8
                },
            };

            DeloyMainDelay = 3;
            DeloyFOBDelay = 10;

            DeployCancelOnMove = true;
            DeployCancelOnDamage = true;

            ShouldRespawnAtMain = true;
            ShouldSendPlayersBackToMainOnRoundEnded = true;
            ShouldWipeAllFOBsOnRoundedEnded = true;
            ShouldKillMaincampers = true;

            FirstFOBUiId = 36020;
        }

        public FOBConfig() { }
    }

    public class Emplacement
    {
        public ushort vehicleID;
        public ushort baseID;
        public ushort ammoID;
        public ushort ammoAmount;
        public ushort requiredBuild;
    }

    public class Fortification
    {
        public ushort barricade_id;
        public ushort base_id;
        public ushort required_build;
    }
}
