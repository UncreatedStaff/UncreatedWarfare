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

        public static void CreateSquad(string name, UnturnedPlayer leader, ulong team, EBranch branch)
        {
            var squad = new Squad(name, leader, team, branch);
            Squads.Add(squad);
            UpdateUIForTeam(squad.Team);
        }

        public static void UpdateUIForTeam(ulong team)
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

                    foreach (var squad in Squads)
                    {
                        if (squad.Members.Exists(p => p.CSteamID == steamplayer.playerID.steamID))
                        {
                            EffectManager.sendUIEffect(30071, 30071, steamplayer.transportConnection, true,
                                            squad.Leader.SteamPlayer().playerID.nickName,
                                            squad.Leader.GetKitIcon(),
                                            $"{squad.Name} ({squad.Members.Count}/6)"
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
                            return;
                        }
                    }
                    // if player is not in a squad, display full list UI
                    {
                        for (int i = 0; i < Squads.Count; i++)
                        {
                            EffectManager.sendUIEffect((ushort)(30061 + i),
                                (short)(30061 + i),
                                steamplayer.transportConnection,
                                true,
                                Squads[i].Name,
                                $"{Squads[i].Members.Count}/6"
                            );
                        }
                    }
                }
            }
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
                p.Message("{0} joined your squad.", player.CharacterName);

            squad.Members.Add(player);
            UpdateUIForTeam(squad.Team);
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
                p.Message("{0} left your squad.", player.CharacterName);

            if (squad.Leader.CSteamID == player.CSteamID)
            {
                squad.Leader = squad.Members[0];
                squad.Leader.Message("You are now the squad leader.");
            }
            UpdateUIForTeam(squad.Team);
        }
        public static void DisbandSquad(Squad squad)
        {
            Squads.RemoveAll(s => s.Name == squad.Name);

            foreach (var p in squad.Members)
                p.Message("Your squad was disbanded.");

            UpdateUIForTeam(squad.Team);
        }
        public static void KickPlayerFromSquad(UnturnedPlayer player, ref Squad squad)
        {
            if (squad.Members.Count <= 1)
                return;

            squad.Members.RemoveAll(p => p.CSteamID == player.CSteamID);

            if (Provider.clients.Exists(p => p.playerID.steamID == player.CSteamID))
                player.Message("You were kicked from the squad.", player.CharacterName);

            foreach (var p in squad.Members)
                p.Message("{0} was kicked from the squad.", player.CharacterName);

            UpdateUIForTeam(squad.Team);
        }
        public static void PromoteToLeader(ref Squad squad, UnturnedPlayer newLeader)
        {
            squad.Leader = newLeader;

            foreach (var p in squad.Members)
            {
                if (p.CSteamID == squad.Leader.CSteamID)
                    p.Message("You were promoted to squad leader");
                else
                    p.Message("{0} was promoted to squad leader.", newLeader.CharacterName);
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
        public static bool SetLocked(ref Squad squad, bool value) => squad.IsLocked = value;
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
