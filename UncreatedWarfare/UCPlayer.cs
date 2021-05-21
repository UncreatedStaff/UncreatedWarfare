using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Squads;
using UncreatedWarfare.Teams;
using static UncreatedWarfare.Kits.Kit;
using static UncreatedWarfare.Teams.Branch;

namespace UncreatedWarfare
{
    public class UCPlayer
    {
        public readonly CSteamID steamID;
        public readonly string characterName;
        public readonly string groupName;
        public readonly ulong teamID;
        public readonly EBranch branch;
        public readonly EClass kitclass;

        public readonly Player nelsonplayer;
        public readonly UnturnedPlayer rocketplayer;
        public readonly SteamPlayer steamplayer;

        public readonly string kitName;
        public readonly string squadName;


        public UCPlayer(UnturnedPlayer player, EBranch branch, EClass kitclass, string kitname)
        {
            steamID = player.CSteamID;
            characterName = player.CharacterName;
            groupName = player.DisplayName; // pls check
            this.branch = branch;
            this.kitclass = kitclass;
            kitName = kitname;
            nelsonplayer = player.Player;
            rocketplayer = player;
            steamplayer = player.SteamPlayer();
        }

        public bool HasTeam() => teamID == TeamManager.Team1ID || teamID == TeamManager.Team2ID;
        public bool IsTeam1() => teamID == TeamManager.Team1ID;
        public bool IsTeam2() => teamID == TeamManager.Team2ID;

        public bool HasKit() => kitclass != EClass.NONE;

        public override bool Equals(object obj)
        {
            UCPlayer player = (UCPlayer)obj;
            return steamID.m_SteamID == player.steamID.m_SteamID;
        }
        public override int GetHashCode() => 363513814 + EqualityComparer<string>.Default.GetHashCode(steamID.ToString());

        public static bool operator ==(UCPlayer player1, UCPlayer player2) => player1.steamID.m_SteamID == player2.steamID.m_SteamID;

        public static bool operator !=(UCPlayer player1, UCPlayer player2) => player1.steamID.m_SteamID != player2.steamID.m_SteamID;
    }
}
