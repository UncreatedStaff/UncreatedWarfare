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
        }

        public static void UpdateUIForTeam(ulong team, Squad targetSquad = null)
        {
            foreach (var steamplayer in Provider.clients)
            {
                if (TeamManager.IsFriendly(steamplayer, team))
                {
                    for (int i = 0; i < 6; i++)
                        EffectManager.askEffectClearByID((ushort)(30071 + i), steamplayer.transportConnection);
                    for (int i = 0; i < 8; i++)
                        EffectManager.askEffectClearByID((ushort)(30061 + i), steamplayer.transportConnection);
                    for (int i = 0; i < 8; i++)
                        EffectManager.askEffectClearByID((ushort)(30081 + i), steamplayer.transportConnection);

                    bool IsInSquad = false;

                    foreach (var squad in Squads)
                    {
                        if ((targetSquad == null && squad.Members.Exists(p => p.CSteamID == steamplayer.playerID.steamID)) || squad.Name == targetSquad.Name)
                        {
                            IsInSquad = true;

                            EffectManager.sendUIEffect(30071, 30071, steamplayer.transportConnection, true,
                                            squad.Leader.SteamPlayer().playerID.nickName,
                                            squad.Leader.GetKitIcon(),
                                            squad.IsLocked? $"{squad.Name} <color=#cf6a59>({squad.Members.Count}/6)</color>" : $"{squad.Name} ({squad.Members.Count}/6)"
                                        );

                            for (int i = 0; i < squad.Members.Count; i++)
                            {
                                if (squad.Members[i] != squad.Leader)
                                {
                                    EffectManager.sendUIEffect((ushort)(30072 + i), (short)(30072 + i), steamplayer.transportConnection, true,
                                            squad.Members[i].SteamPlayer().playerID.nickName,
                                            squad.Members[i].GetKitIcon()
                                        );
                                }
                            }

                            for (int i = 0; i < Squads.Count; i++)
                            {
                                EffectManager.sendUIEffect(
                                    (ushort)(30081 + i),
                                    (short)(30081 + i),
                                    steamplayer.transportConnection,
                                    true,
                                    Squads[i].Name == squad.Name ? $"<color=#878787>...</color>" : $"{Squads[i].Members.Count}/6"
                                );
                            }
                            break;
                        }
                    }

                    if (!IsInSquad) // if player is not in a squad, display full list UI
                    {
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

        public static void InvokePlayerJoined(UnturnedPlayer player)
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
        public static void InvokePlayerLeft(UnturnedPlayer player)
        {
            if (IsInAnySquad(player.CSteamID, out var squad))
                LeaveSquad(player, ref squad);
        }

        public static void CreateSquad(string name, UnturnedPlayer leader, ulong team, EBranch branch)
        {
            var squad = new Squad(name, leader, team, branch);
            Squads.Add(squad);
            UpdateUIForTeam(squad.Team, squad);
        }

        public static bool IsInAnySquad(CSteamID playerID, out Squad squad)
        {
            squad = Squads.Find(s => s.Members.Exists(p => p.CSteamID == playerID));
            return squad != null;
        }
        public static bool IsInSquad(CSteamID playerID, Squad targetSquad) => targetSquad.Members.Exists(p => p.CSteamID == playerID);
        public static void JoinSquad(UnturnedPlayer player, ref Squad squad)
        {
            foreach (var p in squad.Members)
                p.Message("squad_player_joined", F.GetPlayerOriginalNames(player).NickName);

            squad.Members.Add(player);
            UpdateUIForTeam(squad.Team, squad);
        }
        public static void LeaveSquad(UnturnedPlayer player, ref Squad squad)
        {
            if (squad.Members.Count <= 1)
            {
                DisbandSquad(squad);
                return;
            }

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

            foreach (var p in squad.Members)
                p.Message("squad_player_left", F.GetPlayerOriginalNames(player).NickName);

            if (squad.Leader.CSteamID == player.CSteamID)
            {
                squad.Leader = squad.Members[0];
                squad.Leader.Message("squad_squadleader");
            }
            UpdateUIForTeam(squad.Team, squad);
        }
        public static void DisbandSquad(Squad squad)
        {
            Squads.RemoveAll(s => s.Name == squad.Name);

            foreach (var p in squad.Members)
                p.Message("squad_disbanded");

            UpdateUIForTeam(squad.Team, squad);
        }
        public static void KickPlayerFromSquad(UnturnedPlayer player, ref Squad squad)
        {
            if (squad.Members.Count <= 1)
                return;

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

            if (Provider.clients.Exists(p => p.playerID.steamID == player.CSteamID))
                player.Message("squad_kicked");

            foreach (var p in squad.Members)
                p.Message("squad_player_kicked", F.GetPlayerOriginalNames(player).NickName);

            UpdateUIForTeam(squad.Team, squad);
        }
        public static void PromoteToLeader(ref Squad squad, UnturnedPlayer newLeader)
        {
            squad.Leader = newLeader;

            foreach (var p in squad.Members)
            {
                if (p.CSteamID == squad.Leader.CSteamID)
                    p.Message("squad_promoted");
                else
                    p.Message("squad_player_promoted", F.GetPlayerOriginalNames(newLeader).NickName);
            }       

            UpdateUIForTeam(squad.Team);
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
                s.Name.Split(' ').All(l => name.ToLower().Contains(name))
                );

            return squad != null;
        }
        public static void SetLocked(ref Squad squad, bool value)
        {
            squad.IsLocked = value;
            UpdateUIForTeam(squad.Team);
        }
    }

    public class Squad
    {
        public string Name;
        public ulong Team;
        public EBranch Branch;
        public bool IsLocked;
        public UnturnedPlayer Leader;
        public List<UnturnedPlayer> Members;
        public Squad(string name, UnturnedPlayer leader, ulong team, EBranch branch)
        {
            Name = name;
            Team = team;
            Branch = branch;
            Leader = leader;
            IsLocked = false;
            Members = new List<UnturnedPlayer>();
            Members.Add(leader);
        }
    }
}
