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
    public class SquadManager
    {
        public static Config<SquadConfigData> config;
        public static List<Squad> Squads;

        public SquadManager()
        {
            config = new Config<SquadConfigData>(Data.SquadStorage, "config.json");

            Squads = new List<Squad>();
            KitManager.OnKitChanged += OnKitChanged;
        }
        private static void OnKitChanged(UnturnedPlayer player, Kit kit)
        {
            if (IsInAnySquad(player.CSteamID, out var squad))
                UpdateUISquad(squad);
        }
        public static void ClearUIsquad(Player player)
        {
            for (int i = 0; i < 6; i++)
                EffectManager.askEffectClearByID((ushort)(30071 + i), player.channel.owner.transportConnection);
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(30081 + i), player.channel.owner.transportConnection);
            EffectManager.askEffectClearByID(config.data.rallyUI, player.channel.owner.transportConnection);
        }
        public static void ClearUIList(Player player)
        {
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(30061 + i), player.channel.owner.transportConnection);
        }
        public static void UpdateUISquad(Squad squad)
        {
            foreach (var member in squad.Members)
            {
                for (int i = 0; i < 6; i++)
                    EffectManager.askEffectClearByID((ushort)(30071 + i), member.Player.channel.owner.transportConnection);

                for (int i = 0; i < squad.Members.Count; i++)
                {
                    if (squad.Members[i] == squad.Leader)
                    {
                        EffectManager.sendUIEffect(30071, 30071, member.Player.channel.owner.transportConnection, true,
                                 squad.Members[i].NickName,
                                 squad.Members[i].Icon,
                                 $"{squad.Name}  <color=#8c8c8c>{squad.Members.Count}/6</color>",
                                 squad.IsLocked ? "<color=#bd6b5b>²</color>" : ""
                             );
                    }
                    else
                    {
                        EffectManager.sendUIEffect((ushort)(30071 + i), (short)(30071 + i), member.SteamPlayer().transportConnection, true,
                               squad.Members[i].NickName,
                               squad.Members[i].Icon
                           );
                    }
                }
            }
        }

        public static void UpdateUIMemberCount(ulong team)
        {
            foreach (var steamplayer in Provider.clients)
            {
                if (TeamManager.IsFriendly(steamplayer, team))
                {
                    if (IsInAnySquad(steamplayer.playerID.steamID, out var currentSquad))
                    {
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(30081 + i), steamplayer.transportConnection);

                        var sortedSquads = Squads.OrderBy(s => s.Name != currentSquad.Name).ToList();

                        for (int i = 0; i < sortedSquads.Count; i++)
                        {
                            string display = "...";
                            if (i != 0)
                            {
                                display = $"{sortedSquads[i].Members.Count}/6";
                                if (sortedSquads[i].IsLocked)
                                    display = $"<color=#969696>{display}</color>";
                            }

                            EffectManager.sendUIEffect((ushort)(30081 + i),
                                (short)(30081 + i),
                                steamplayer.transportConnection,
                                true,
                                display
                            );
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 8; i++)
                            EffectManager.askEffectClearByID((ushort)(30061 + i), steamplayer.transportConnection);

                        for (int i = 0; i < Squads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(30061 + i),
                                (short)(30061 + i),
                                steamplayer.transportConnection,
                                true,
                                !Squads[i].IsLocked ? Squads[i].Name : $"<color=#969696>{Squads[i].Name}</color>",
                                !Squads[i].IsLocked ? $"{Squads[i].Members.Count}/6" : $"<color=#bd6b5b>²</color>  <color=#969696>{Squads[i].Members.Count}/6</color>",
                                Squads[i].Leader.NickName
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
                    EffectManager.sendUIEffect((ushort)(30061 + i),
                        (short)(30061 + i),
                        player.SteamPlayer().transportConnection,
                        true,
                        Squads[i].Name,
                        !Squads[i].IsLocked ? Squads[i].Name : $"<color=#969696>{Squads[i].Name}</color>",
                                    !Squads[i].IsLocked ? $"{Squads[i].Members.Count}/6" : $"<color=#bd6b5b>{config.data.lockCharacter}</color>  <color=#969696>{Squads[i].Members.Count}/6</color>"
                    );
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
                p.Message("squad_player_joined", p.Player.channel.owner.playerID.nickName);

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
                p.Message("squad_player_left", p.SteamPlayer().playerID.nickName);

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
                var leaderID = squad.Leader.CSteamID;
                squad.Members = squad.Members.OrderBy(p => p.CSteamID != leaderID).ToList();
                squad.Leader.Message("squad_squadleader", squad.Leader.SteamPlayer().playerID.nickName);
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
                UpdateUIMemberCount(squad.Team);
            }

            PlayerManager.Save();
        }
        public static void KickPlayerFromSquad(UCPlayer player, ref Squad squad)
        {
            if (squad.Members.Count <= 1)
                return;

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

            if (Provider.clients.Exists(p => p.playerID.steamID == player.CSteamID))
                player.Message("squad_kicked");

            foreach (var p in squad.Members)
                p.Message("squad_player_kicked", player.SteamPlayer().playerID.nickName);

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
                if (p.CSteamID == squad.Leader.CSteamID)
                    p.Message("squad_promoted");
                else
                    p.Message("squad_player_promoted", p.SteamPlayer().playerID.nickName);
            }

            var leaderID = squad.Leader.CSteamID;
            squad.Members = squad.Members.OrderBy(p => p.CSteamID != leaderID).ToList();

            UpdateUISquad(squad);
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
        public int SquadDisconnectTime;
        public char lockCharacter;

        public override void SetDefaults()
        {
            Team1RallyID = 38381;
            Team1RallyID = 38382;
            RallyTimer = 60;
            rallyUI = 32395;
            SquadDisconnectTime = 120;
            lockCharacter = '²';
        }

        public SquadConfigData() { }
    }
}
