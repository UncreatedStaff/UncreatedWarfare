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
        public List<Squad> Squads;

        public SquadManager()
        {
            Squads = new List<Squad>();
        }

        public void CreateSquad(string name, ulong team, EBranch branch, UnturnedPlayer leader)
        {
            var squad = new Squad(name, team, branch, leader);
            Squads.Add(squad);
        }

        public void UpdateUI(Squad targetSquad)
        {
            foreach (var squad in Squads)
            {
                
                if (squad.Name == targetSquad.Name)
                {
                    foreach (UnturnedPlayer player in squad.Members)
                    {
                        // send member count to this player's UI effect
                    }
                    break;
                }
                // if we reach here, it means the squad

            }
            foreach (var steamplayer in Provider.clients)
            {
                if (IsInSquad(steamplayer.playerID.steamID))
                {
                    // update this UI
                }
            }
        }

        public bool IsInSquad(CSteamID steamID) => Squads.Exists(s => s.Members.Exists(p => p.CSteamID == steamID));

    }

    public class Squad
    {
        public string Name;
        public ulong Team;
        public EBranch Branch;
        public UnturnedPlayer Leader;
        public List<UnturnedPlayer> Members;
        public Squad(string name, ulong team, EBranch branch, UnturnedPlayer leader)
        {
            Name = name;
            Team = team;
            Branch = branch;
            Leader = leader;
            Members = new List<UnturnedPlayer>();
            this.Members.Add(leader);
        }
    }
}
