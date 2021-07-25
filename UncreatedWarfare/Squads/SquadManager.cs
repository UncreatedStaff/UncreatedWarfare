using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
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
        private static void OnKitChanged(UnturnedPlayer player, Kit kit, string oldkit)
        {
            if (IsInAnySquad(player.CSteamID, out Squad squad))
                UpdateUISquad(squad); 
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
            foreach (var member in squad.Members)
            {
                for (int i = 0; i < 6; i++)
                    EffectManager.askEffectClearByID((ushort)(config.Data.squadSUI + i), member.Player.channel.owner.transportConnection);

                for (int i = 0; i < squad.Members.Count; i++)
                {
                    if (squad.Members[i] == squad.Leader)
                    {
                        EffectManager.sendUIEffect(config.Data.squadSUI, unchecked((short)config.Data.squadSUI), member.Player.channel.owner.transportConnection, true,
                                 F.Translate("squad_ui_player_name", member, F.GetPlayerOriginalNames(squad.Members[i]).NickName),
                                 squad.Members[i].Icon.ToString(),
                                 F.Translate($"squad_ui_header_name", member, squad.Name, squad.Members.Count.ToString(Data.Locale)),
                                 squad.IsLocked ? F.Translate("squad_ui_locked_symbol", member, config.Data.lockCharacter.ToString()) : ""
                             );
                    }
                    else
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
            var friendlySquads = Squads.Where(s => s.Team == team).ToList();

            foreach (var steamplayer in Provider.clients)
            {
                if (TeamManager.IsFriendly(steamplayer, team))
                {
                    if (IsInAnySquad(steamplayer.playerID.steamID, out var currentSquad))
                    {
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(config.Data.squadLTUI + i), steamplayer.transportConnection);

                        var sortedSquads = friendlySquads.OrderBy(s => s.Name != currentSquad.Name).ToList();

                        for (int i = 0; i < sortedSquads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(config.Data.squadLTUI + i),
                                unchecked((short)(config.Data.squadLTUI + i)),
                                steamplayer.transportConnection,
                                true,
                                i == 0 ? 
                                F.Translate("squad_ui_expanded", steamplayer) : 
                                F.Translate(sortedSquads[i].IsLocked ?
                                    "squad_ui_player_count_small_locked" : "squad_ui_player_count_small", steamplayer, 
                                    sortedSquads[i].Members.Count.ToString(Data.Locale))
                            );
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(config.Data.squadLUI + i), steamplayer.transportConnection);

                        for (int i = 0; i < friendlySquads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(config.Data.squadLUI + i),
                                unchecked((short)(config.Data.squadLUI + i)),
                                steamplayer.transportConnection,
                                true,
                                F.Translate("squad_ui_leader_name", steamplayer, Squads[i].Name),
                                F.Translate("squad_ui_player_count", steamplayer, Squads[i].IsLocked ? 
                                config.Data.lockCharacter + "  " : "", Squads[i].Members.Count.ToString(Data.Locale)),
                                F.Translate("squad_ui_leader_name", steamplayer, F.GetPlayerOriginalNames(Squads[i].Leader).CharacterName)
                            );
                        }
                    }
                }
            }
        }

        public static void InvokePlayerJoined(UCPlayer player, string squadName)
        {
            var squad = Squads.Find(s => s.Name == squadName);

            if (squad != null && !squad.IsFull())
            {
                JoinSquad(player, ref squad);
            }
            else
            {
                for (int i = 0; i < Squads.Count; i++)
                {
                    if (Squads[i].Team == player.GetTeam())
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
            }
        }
        public static void InvokePlayerLeft(UCPlayer player)
        {
            if (IsInAnySquad(player.CSteamID, out var squad))
                LeaveSquad(player, ref squad);
        }

        public static void CreateSquad(string name, UCPlayer leader, ulong team, EBranch branch)
        {
            var squad = new Squad(name.ToUpper(), leader, team, branch);
            Squads.Add(squad);

            leader.Squad = squad;

            ClearUIList(leader.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
        }

        public static bool IsInAnySquad(CSteamID playerID, out Squad squad)
        {
            squad = Squads.Find(s => s.Members.Exists(p => p.Steam64 == playerID.m_SteamID));
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

            player.Squad = squad;

            ClearUIList(player.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);

            if (RallyManager.HasRally(player, out var rally))
                rally.UpdateUIForSquad();

            PlayerManager.Save();
        }
        public static void LeaveSquad(UCPlayer player, ref Squad squad)
        {
            player.Message("squad_left");

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

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
                Squads.RemoveAll(s => s.Name == name);

                squad.Leader.Message("squad_disbanded");
                ClearUIsquad(squad.Leader.Player);

                UpdateUIMemberCount(squad.Team);

                PlayerManager.Save();

                return;
            }

            if (squad.Leader.CSteamID == player.CSteamID)
            {
                squad.Leader = squad.Members[0];
                CSteamID leaderID = squad.Leader.CSteamID;
                squad.Members = squad.Members.OrderBy(p => p.CSteamID != leaderID).ToList();
                squad.Leader.Message("squad_squadleader", squad.Leader.SteamPlayer.playerID.nickName);
            }

            UpdateUISquad(squad);
            ClearUIsquad(player.Player);
            UpdateUIMemberCount(squad.Team);

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

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

            foreach (var p in squad.Members)
            {
                if (p.Steam64 != player.Steam64)
                    p.Message("squad_player_kicked", player.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_kicked");

            }

            player.Squad = null;

            ClearUIsquad(player.Player);
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);

            PlayerManager.Save();
        }
        public static void PromoteToLeader(ref Squad squad, UCPlayer newLeader)
        {
            squad.Leader = newLeader;

            foreach (var p in squad.Members)
            {
                if (p.CSteamID != squad.Leader.CSteamID)
                    p.Message("squad_player_promoted", newLeader.Player.channel.owner.playerID.nickName);
                else
                    p.Message("squad_promoted");
            }

            var leaderID = squad.Leader.CSteamID;
            squad.Members = squad.Members.OrderBy(p => p.CSteamID != leaderID).ToList();

            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
        }
        public static bool FindSquad(string name, ulong teamID, out Squad squad)
        {
            var friendlySquads = Squads.Where(s => s.Team == teamID).ToList();

            if (name.ToLower().StartsWith("squad") && name.Length < 10 && Int32.TryParse(name[5].ToString(), System.Globalization.NumberStyles.Any, Data.Locale, out var squadNumber))
            {
                if (squadNumber < friendlySquads.Count)
                {
                    squad = friendlySquads[squadNumber];
                    return true;
                }
            }
            squad = friendlySquads.Find(
                s =>
                name.Equals(s.Name, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Replace(" ", "").Replace("'", "").ToLower().Contains(name)
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

    public class Squad
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
            Members = new List<UCPlayer>();
            Members.Add(leader);
        }

        public bool IsFull() => Members.Count < 6;
        public bool IsNotSolo() => Members.Count > 1;
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
        public ushort MortarMarker;

        public override void SetDefaults()
        {
            Team1RallyID = 38381;
            Team2RallyID = 38382;
            RallyTimer = 60;
            rallyUI = 36060;
            squadLUI = 36040;
            squadSUI = 36060;
            squadLTUI = 36050;
            EmptyMarker = 36100;
            MortarMarker = 36120;
            SquadDisconnectTime = 120;
            lockCharacter = '²';
            Classes = new Dictionary<Kit.EClass, ClassConfig>
            {
                { Kit.EClass.NONE, new ClassConfig('±', 36101) },
                { Kit.EClass.UNARMED, new ClassConfig('±', 36101) },
                { Kit.EClass.SQUADLEADER, new ClassConfig('¦', 36102) },
                { Kit.EClass.RIFLEMAN, new ClassConfig('¡', 36103) },
                { Kit.EClass.MEDIC, new ClassConfig('¢', 36104) },
                { Kit.EClass.BREACHER, new ClassConfig('¤', 36105) },
                { Kit.EClass.AUTOMATIC_RIFLEMAN, new ClassConfig('¥', 36106) },
                { Kit.EClass.GRENADIER, new ClassConfig('¬', 36107) },
                { Kit.EClass.MACHINE_GUNNER, new ClassConfig('«', 36108) },
                { Kit.EClass.LAT, new ClassConfig('®', 36109) },
                { Kit.EClass.HAT, new ClassConfig('¯', 36110) },
                { Kit.EClass.MARKSMAN, new ClassConfig('¨', 36111) },
                { Kit.EClass.SNIPER, new ClassConfig('£', 36112) },
                { Kit.EClass.AP_RIFLEMAN, new ClassConfig('©', 36113) },
                { Kit.EClass.COMBAT_ENGINEER, new ClassConfig('ª', 36114) },
                { Kit.EClass.CREWMAN, new ClassConfig('§', 36115) },
                { Kit.EClass.PILOT, new ClassConfig('°', 36116) },
                { Kit.EClass.SPEC_OPS, new ClassConfig('À', 36117) },
            };
        }

        public SquadConfigData() { }
    }

    public class ClassConfig
    {
        public char Icon;
        public ushort MarkerEffect;
        [Newtonsoft.Json.JsonConstructor]
        public ClassConfig(char Icon, ushort MarkerEffect)
        {
            this.Icon = Icon;
            this.MarkerEffect = MarkerEffect;
        }
    }
}
