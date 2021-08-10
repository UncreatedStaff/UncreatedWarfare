using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Squads
{
    public class SquadManager : IDisposable
    {
        public static Config<SquadConfigData> config;
        public static List<Squad> Squads;

        public SquadManager()
        {
            config = new Config<SquadConfigData>(Data.SquadStorage, "config.json");

            Squads = new List<Squad>();
            KitManager.OnKitChanged += OnKitChanged;
        }
        private static void OnKitChanged(UCPlayer player, Kit kit, string oldkit)
        {
            if (IsInAnySquad(player.CSteamID, out Squad squad, out _))
                UpdateUISquad(squad);
        }
        public static void OnGroupChanged(SteamPlayer steamplayer, ulong oldGroup, ulong newGroup)
        {
            if (IsInAnySquad(steamplayer.playerID.steamID, out var squad, out var player))
            {
                LeaveSquad(player, squad);
            }
            UpdateSquadList(UCPlayer.FromSteamPlayer(steamplayer), newGroup.GetTeam(), true);
        }
        public static void ClearUIsquad(Player player)
        {
            for (int i = 0; i < 6; i++)
                EffectManager.askEffectClearByID((ushort)(config.Data.squadSUI + i), player.channel.owner.transportConnection);
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(config.Data.squadLTUI + i), player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(config.Data.rallyUI, player.channel.owner.transportConnection);
        }
        public static void ClearUIList(Player player)
        {
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(config.Data.squadLUI + i), player.channel.owner.transportConnection);
        }
        public static void UpdateUISquad(Squad squad)
        {
            foreach (UCPlayer member in squad.Members)
            {
                for (int i = 0; i < 6; i++)
                    EffectManager.askEffectClearByID((ushort)(config.Data.squadSUI + i), member.Player.channel.owner.transportConnection);

                for (int i = 0; i < squad.Members.Count; i++)
                {
                    if (i == 0) // first one, includes header (squad leader)
                    {
                        EffectManager.sendUIEffect(config.Data.squadSUI, unchecked((short)config.Data.squadSUI), member.Player.channel.owner.transportConnection, true,
                                 F.Translate("squad_ui_player_name", member, F.GetPlayerOriginalNames(squad.Members[i]).NickName),
                                 squad.Members[i].Icon.ToString(),
                                 F.Translate($"squad_ui_header_name", member, squad.Name, squad.Members.Count.ToString(Data.Locale)),
                                 squad.IsLocked ? F.Translate("squad_ui_locked_symbol", member, config.Data.lockCharacter.ToString()) : ""
                             );
                    }
                    else // all other members
                    {
                        EffectManager.sendUIEffect((ushort)(config.Data.squadSUI + i), unchecked((short)(config.Data.squadSUI + i)), member.SteamPlayer.transportConnection, true,
                               F.Translate("squad_ui_player_name", member, F.GetPlayerOriginalNames(squad.Members[i]).NickName),
                               squad.Members[i].Icon.ToString()
                           );
                    }
                }
            }
        }

        public static void UpdateUIMemberCount(ulong team)
        {
            List<Squad> friendlySquads = Squads.Where(s => s.Team == team).ToList();

            foreach (SteamPlayer player in Provider.clients)
            {
                if (player.GetTeam() == team)
                {
                    if (IsInAnySquad(player.playerID.steamID, out Squad currentSquad, out _))
                    {
                        // clear all tiny lists
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(config.Data.squadLTUI + i), player.transportConnection);

                        List<Squad> sortedSquads = friendlySquads.OrderBy(s => s.Name != currentSquad.Name).ToList();

                        // send a list of all squads
                        for (int i = 0; i < sortedSquads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(config.Data.squadLTUI + i),
                                unchecked((short)(config.Data.squadLTUI + i)),
                                player.transportConnection,
                                true,
                                i == 0 ? 
                                F.Translate("squad_ui_expanded", player) : 
                                F.Translate(sortedSquads[i].IsLocked ?
                                    "squad_ui_player_count_small_locked" : "squad_ui_player_count_small", player, 
                                    sortedSquads[i].Members.Count.ToString(Data.Locale))
                            );
                        }
                    }
                    else
                    {
                        // clear all labels (out of squad)
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(config.Data.squadLUI + i), player.transportConnection);
                            
                        // send labels for squad showing leader name, player count, islocked
                        for (int i = 0; i < friendlySquads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(config.Data.squadLUI + i),
                                unchecked((short)(config.Data.squadLUI + i)),
                                player.transportConnection,
                                true,
                                F.Translate("squad_ui_squad_name", player, friendlySquads[i].Name),
                                F.Translate("squad_ui_player_count", player, friendlySquads[i].IsLocked ? 
                                config.Data.lockCharacter + "  " : "", friendlySquads[i].Members.Count.ToString(Data.Locale)),
                                F.Translate("squad_ui_leader_name", player, F.GetPlayerOriginalNames(friendlySquads[i].Leader).CharacterName)
                            );
                        }
                    }
                }
            }
        }

        public static void InvokePlayerJoined(UCPlayer player, string squadName)
        {
            var squad = Squads.Find(s => s.Name == squadName && s.Team == player.GetTeam());

            if (squad != null && !squad.IsFull())
            {
                JoinSquad(player, ref squad);
            }
            else
            {
                UpdateSquadList(player, false); // no need to clear since player just joined
            }
        }
        public static void UpdateSquadList(UCPlayer player, bool clear = true) => UpdateSquadList(player, player.GetTeam(), clear);
        public static void UpdateSquadList(UCPlayer player, ulong team, bool clear = true)
        {
            // send squad list to player for every squad on that team
            int i = 0;
            for (; i < Squads.Count; i++)
            {
                if (Squads[i].Team == team)
                {
                    EffectManager.sendUIEffect((ushort)(config.Data.squadLUI + i),
                    unchecked((short)(config.Data.squadLUI + i)),
                    player.SteamPlayer.transportConnection,
                    true,
                    F.Translate("squad_ui_leader_name", player, Squads[i].Name),
                    F.Translate("squad_ui_player_count", player, Squads[i].IsLocked ? $"{config.Data.lockCharacter}  " : "", Squads[i].Members.Count.ToString(Data.Locale)),
                    F.Translate("squad_ui_leader_name", player, F.GetPlayerOriginalNames(Squads[i].Leader).CharacterName)
                );
                }
            }
            if (clear)
                for (; i < 8; i++) // clear the rest of the ui.
                    EffectManager.askEffectClearByID((ushort)(config.Data.squadLUI + i), player.SteamPlayer.transportConnection);
        }
        public static void InvokePlayerLeft(UCPlayer player)
        {
            if (IsInAnySquad(player.CSteamID, out var squad, out _))
                LeaveSquad(player, squad);
        }

        public static Squad CreateSquad(string name, UCPlayer leader, ulong team, EBranch branch)
        {
            Squad squad = new Squad(name.ToUpper(), leader, team, branch);
            SortMembers(squad);
            Squads.Add(squad);

            leader.Squad = squad;

            ClearUIList(leader.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
            return squad;
        }

        public static bool IsInAnySquad(CSteamID playerID, out Squad squad, out UCPlayer player)
        {
            squad = Squads.Find(s => s.Members.Exists(p => p.Steam64 == playerID.m_SteamID));
            player = squad?.Members.Find(p => p.CSteamID == playerID);
            return squad != null;
        }
        public static bool IsInSquad(CSteamID playerID, Squad targetSquad) => targetSquad.Members.Exists(p => p.Steam64 == playerID.m_SteamID);
        public static void JoinSquad(UCPlayer player, ref Squad squad)
        {
            foreach (var p in squad.Members)
            {
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_joined", player.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_joined", squad.Name);
            }

            squad.Members.Add(player);
            SortMembers(squad);

            player.Squad = squad;

            ClearUIList(player.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out var rally))
                rally.ShowUIForSquad();

            PlayerManager.Save();
        }
        public static void SortMembers(Squad squad)
        {
            squad.Members.Sort(delegate (UCPlayer a, UCPlayer b)
            {
                int o = b.cachedOfp.CompareTo(a.cachedOfp); // sort players by their officer status
                return o == 0 ? b.cachedXp.CompareTo(a.cachedXp) : o;
            });
            squad.Members.RemoveAll(x => x.Steam64 == squad.Leader.Steam64);
            squad.Members.Insert(0, squad.Leader);
        }
        public static void LeaveSquad(UCPlayer player, Squad squad)
        {
            player.Message("squad_left");

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);
            bool willNeedNewLeader = squad.Leader == null || squad.Leader.CSteamID == player.CSteamID;
            if (!willNeedNewLeader) SortMembers(squad);
            player.Squad = null;

            foreach (var p in squad.Members)
            {
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_left", player.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_left", squad.Name);
            }

            if (squad.Members.Count == 0)
            {
                string name = squad.Name;
                ulong team = squad.Team;
                Squads.RemoveAll(s => s.Name == name && s.Team == team);

                squad.Leader?.Message("squad_disbanded");
                ClearUIsquad(squad.Leader.Player);

                UpdateUIMemberCount(squad.Team);

                if (RallyManager.HasRally(squad, out RallyPoint rally1))
                {
                    if (rally1.drop != null && Regions.tryGetCoordinate(rally1.drop.model.position, out byte x, out byte y))
                        BarricadeManager.destroyBarricade(rally1.drop, x, y, ushort.MaxValue);

                    RallyManager.TryDeleteRallyPoint(rally1.structure.instanceID);
                }

                if (Provider.clients.Exists(sp => sp.playerID.steamID == player.CSteamID))
                {
                    if (squad.Leader.KitClass == Kit.EClass.SQUADLEADER)
                        KitManager.TryGiveUnarmedKit(squad.Leader);
                }

                PlayerManager.Save();

                return;
            }

            if (willNeedNewLeader)
            {
                squad.Leader = squad.Members[0]; // goes to the best officer, then the best xp=
                SortMembers(squad);
                squad.Leader.Message("squad_squadleader", squad.Leader.SteamPlayer.playerID.nickName);
            }

            UpdateUISquad(squad);
            ClearUIsquad(player.Player);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally2))
                rally2.ClearUIForPlayer(player);

            PlayerManager.Save();
        }
        public static void DisbandSquad(Squad squad)
        {
            Squads.RemoveAll(s => s.Name == squad.Name);

            foreach (var member in squad.Members)
            {
                member.Squad = null;

                member.Message("squad_disbanded");
                ClearUIsquad(member.Player);
            }
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally))
            {
                if (rally.drop != null && Regions.tryGetCoordinate(rally.drop.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(rally.drop, x, y, ushort.MaxValue);

                RallyManager.TryDeleteRallyPoint(rally.structure.instanceID);
            }

            PlayerManager.Save();
        }
        public static void RenameSquad(Squad squad, string newName)
        {
            squad.Name = newName.ToUpper();

            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);

            PlayerManager.Save();
        }
        public static void KickPlayerFromSquad(UCPlayer player, ref Squad squad)
        {
            if (squad.Members.Count <= 1)
                return;

            if(squad.Members.RemoveAll(p => p.CSteamID.m_SteamID == player.CSteamID.m_SteamID) > 0)
                player?.Message("squad_kicked");
            SortMembers(squad);
            foreach (UCPlayer p in squad.Members)
            {
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_kicked", player.Player.channel.owner.playerID.nickName);
                    
            }

            player.Squad = null;

            ClearUIsquad(player.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(squad, out RallyPoint rally))
                rally.ClearUIForPlayer(player);

            PlayerManager.Save();
        }
        public static void PromoteToLeader(Squad squad, UCPlayer newLeader)
        {
            if (squad.Leader.KitClass == Kit.EClass.SQUADLEADER)
                KitManager.TryGiveUnarmedKit(squad.Leader);

            squad.Leader = newLeader;

            foreach (var p in squad.Members)
            {
                if (p.CSteamID != squad.Leader.CSteamID)
                    p.Message("squad_player_promoted", newLeader.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_promoted", squad.Leader.SteamPlayer.playerID.nickName);
            }

            SortMembers(squad);

            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
        }
        public static bool FindSquad(string input, ulong teamID, out Squad squad)
        {
            List<Squad> friendlySquads = Squads.Where(s => s.Team == teamID).OrderBy(x => x.Name.Length).ToList();
            string name = input.ToLower();
            squad = friendlySquads.Find(
                s =>
                name == s.Name.ToLower() ||
                s.Name.Replace(" ", "").Replace("'", "").ToLower().Contains(name.ToLower())
                );

            return squad != null;
        }
        public static void SetLocked(ref Squad squad, bool value)
        {
            squad.IsLocked = value;
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
        }
        public void Dispose()
        {
            KitManager.OnKitChanged -= OnKitChanged;
        }
    }

    public class Squad : IEnumerable<UCPlayer>
    {
        public string Name;
        public ulong Team;
        public EBranch Branch;
        public bool IsLocked;
        public UCPlayer Leader;
        public List<UCPlayer> Members;
        public Squad(string name, UCPlayer leader, ulong team, EBranch branch)
        {
            Name = name;
            Team = team;
            Branch = branch;
            Leader = leader;
            IsLocked = false;
            Members = new List<UCPlayer> { leader };
        }

        public IEnumerator<UCPlayer> GetEnumerator() => Members.GetEnumerator();

        public bool IsFull() => Members.Count < 6;
        public bool IsNotSolo() => Members.Count > 1;

        IEnumerator IEnumerable.GetEnumerator() => Members.GetEnumerator();
        public IEnumerator<ITransportConnection> EnumerateMembers()
        {
            IEnumerator<UCPlayer> players = Members.GetEnumerator();
            while (players.MoveNext())
                yield return players.Current.Player.channel.owner.transportConnection;
            players.Dispose();
        }
    }

    public class SquadConfigData : ConfigData
    {
        public ushort Team1RallyID;
        public ushort Team2RallyID;
        public ushort RallyTimer;
        public ushort rallyUI;
        public ushort squadLUI;
        public ushort squadSUI;
        public ushort squadLTUI;
        public int SquadDisconnectTime;
        public char lockCharacter;
        public Dictionary<Kit.EClass, ClassConfig> Classes;
        public ushort EmptyMarker;
        public ushort SquadLeaderEmptyMarker;
        public ushort MortarMarker;
        public ushort InjuredMarker;
        public int MaxSquadNameLength;

        public override void SetDefaults()
        {
            Team1RallyID = 38381;
            Team2RallyID = 38382;
            RallyTimer = 60;
            rallyUI = 36030;
            squadLUI = 36040;
            squadSUI = 36060;
            squadLTUI = 36050;
            EmptyMarker = 36100;
            SquadLeaderEmptyMarker = 36130;
            MortarMarker = 36120;
            InjuredMarker = 36121;
            SquadDisconnectTime = 120;
            MaxSquadNameLength = 16;
            lockCharacter = '²';
            Classes = new Dictionary<Kit.EClass, ClassConfig>
            {
                { Kit.EClass.NONE, new ClassConfig('±', 36101, 36131) },
                { Kit.EClass.UNARMED, new ClassConfig('±', 36101, 36131) },
                { Kit.EClass.SQUADLEADER, new ClassConfig('¦', 36102, 36132) },
                { Kit.EClass.RIFLEMAN, new ClassConfig('¡', 36103, 36133) },
                { Kit.EClass.MEDIC, new ClassConfig('¢', 36104, 36134) },
                { Kit.EClass.BREACHER, new ClassConfig('¤', 36105, 36135) },
                { Kit.EClass.AUTOMATIC_RIFLEMAN, new ClassConfig('¥', 36106, 36136) },
                { Kit.EClass.GRENADIER, new ClassConfig('¬', 36107, 36137) },
                { Kit.EClass.MACHINE_GUNNER, new ClassConfig('«', 36108, 36138) },
                { Kit.EClass.LAT, new ClassConfig('®', 36109, 36139) },
                { Kit.EClass.HAT, new ClassConfig('¯', 36110, 36140) },
                { Kit.EClass.MARKSMAN, new ClassConfig('¨', 36111, 36141) },
                { Kit.EClass.SNIPER, new ClassConfig('£', 36112, 36142) },
                { Kit.EClass.AP_RIFLEMAN, new ClassConfig('©', 36113, 36143) },
                { Kit.EClass.COMBAT_ENGINEER, new ClassConfig('ª', 36114, 36144) },
                { Kit.EClass.CREWMAN, new ClassConfig('§', 36115, 36145) },
                { Kit.EClass.PILOT, new ClassConfig('°', 36116, 36146) },
                { Kit.EClass.SPEC_OPS, new ClassConfig('À', 36117, 36147) },
            };
        }

        public SquadConfigData() { }
    }

    public class ClassConfig
    {
        public char Icon;
        public ushort MarkerEffect;
        public ushort SquadLeaderMarkerEffect;
        [Newtonsoft.Json.JsonConstructor]
        public ClassConfig(char Icon, ushort MarkerEffect, ushort SquadLeaderMarkerEffect)
        {
            this.Icon = Icon;
            this.MarkerEffect = MarkerEffect;
            this.SquadLeaderMarkerEffect = SquadLeaderMarkerEffect;
        }
    }
}
