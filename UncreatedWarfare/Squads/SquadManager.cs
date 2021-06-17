using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Squads
{
    public class SquadManager
    {
        public static List<Squad> Squads;

        public SquadManager()
        {
            Squads = new List<Squad>();
            KitManager.OnKitChanged += OnKitChanged;
        }
        private static void OnKitChanged(UnturnedPlayer player, Kit kit)
        {
            if (IsInAnySquad(player.CSteamID, out var squad))
                UpdateUISquad(squad);
        }

        public static void ClearUIsquad(SteamPlayer steamplayer)
        {
            for (int i = 0; i < 6; i++)
                EffectManager.askEffectClearByID((ushort)(30071 + i), steamplayer.transportConnection);
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(30081 + i), steamplayer.transportConnection);
        }
        public static void ClearUIList(SteamPlayer steamplayer)
        {
            for (int i = 0; i < 8; i++)
                EffectManager.askEffectClearByID((ushort)(30061 + i), steamplayer.transportConnection);
        }
        public static void UpdateUISquad(Squad squad)
        {
            foreach (var member in squad.Members)
            {
                for (int i = 0; i < 6; i++)
                    EffectManager.askEffectClearByID((ushort)(30071 + i), member.SteamPlayer().transportConnection);

                for (int i = 0; i < squad.Members.Count; i++)
                {
                    if (squad.Members[i] == squad.Leader)
                    {
                        EffectManager.sendUIEffect(30071, 30071, member.SteamPlayer().transportConnection, true,
                                 squad.Members[i].SteamPlayer().playerID.nickName,
                                 squad.Members[i].Icon,
                                 squad.IsLocked ? $"{squad.Name} <color=#cf6a59>({squad.Members.Count}/6)</color>" : $"{squad.Name} ({squad.Members.Count}/6)"
                             );
                    }
                    else
                    {
                        EffectManager.sendUIEffect((ushort)(30071 + i), (short)(30071 + i), member.SteamPlayer().transportConnection, true,
                               squad.Members[i].SteamPlayer().playerID.nickName,
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
                                    display = $"<color=#cf6a59>{display}</color>";
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
                                Squads[i].Name,
                                $"{Squads[i].Members.Count}/6",
                                Squads[i].IsLocked ? $"<color=#cf6a59>{Squads[i].Name}/6</color>" : $"{Squads[i].Name}/6"
                            );
                        }
                    }
                }
            }
        }

        public static void InvokePlayerJoined(UCPlayer player)
        {
            for (int i = 0; i < Squads.Count; i++)
            {
                EffectManager.sendUIEffect((ushort)(30061 + i),
                    (short)(30061 + i),
                    player.SteamPlayer().transportConnection,
                    true,
                    Squads[i].Name,
                    $"{Squads[i].Members.Count}/6",
                    Squads[i].IsLocked ? $"<color=#cf6a59>{Squads[i].Name}/6</color>" : $"{Squads[i].Name}/6"
                );
            }
        }
        public static void InvokePlayerLeft(UCPlayer player)
        {
            if (IsInAnySquad(player.CSteamID, out var squad))
                LeaveSquad(player, ref squad);
        }

        public static void CreateSquad(string name, UCPlayer leader, ulong team, EBranch branch)
        {
            var squad = new Squad(name, leader, team, branch);
            Squads.Add(squad);

            leader.Squad = squad;

            ClearUIList(leader.SteamPlayer());
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

            ClearUIList(player.SteamPlayer());
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
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
                ClearUIsquad(squad.Leader.SteamPlayer());

                UpdateUIMemberCount(squad.Team);
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
            ClearUIsquad(player.SteamPlayer());
            UpdateUIMemberCount(squad.Team);
        }
        public static void DisbandSquad(Squad squad)
        {
            Squads.RemoveAll(s => s.Name == squad.Name);

            foreach (var member in squad.Members)
            {
                member.Squad = null;

                member.Message("squad_disbanded");
                ClearUIsquad(member.SteamPlayer());
                UpdateUIMemberCount(squad.Team);
            }
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

            ClearUIsquad(player.SteamPlayer());
            UpdateUISquad(squad);
            UpdateUIMemberCount(squad.Team);
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

            if (name.ToLower().StartsWith("squad") && name.Length < 10 && Int32.TryParse(name[5].ToString(), out var squadNumber))
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
    }

    public class SquadConfigData : ConfigData
    {
        public float MemberNearXPMultiplier;

        public override void SetDefaults()
        {
            MemberNearXPMultiplier = 1.1F;
        }

        public SquadConfigData() { }
    }
}
