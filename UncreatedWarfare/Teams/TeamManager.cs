using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Teams
{
    public class TeamManager : JSONSaver<Team>
    {
        public Team Team1;
        public Team Team2;
        public Team Neutral;


        public TeamManager()
            : base(UCWarfare.TeamStorage + "teams.json")
        {
            Team1 = GetObject(t => t.ID == ETeam.TEAM1);
            Team2 = GetObject(t => t.ID == ETeam.TEAM2);
            Neutral = GetObject(t => t.ID == ETeam.NEUTRAL);
        }

        public void CreateTeams()
        {
            RemoveAllObjectsFromSave();
            AddObjectToSave(new Team(ETeam.TEAM1, 1, "USA"));
            AddObjectToSave(new Team(ETeam.TEAM2, 2, "Russia"));
            AddObjectToSave(new Team(ETeam.NEUTRAL, 3, "Neutral"));
        }

        public void RenameTeam(ETeam team, string newName)
        {
            UpdateObjectsWhere(t => t.ID == team, t => t.Name = newName);
            ReloadTeam(team);
        }
        public void AddPlayerToTeam(ETeam team, CSteamID steamID)
        {
            UpdateObjectsWhere(t => t.ID == team, t => t.Players.Add(steamID.m_SteamID));
            ReloadTeam(team);
        }
        public void RemovePlayerFromTeam(CSteamID steamID)
        {
            UpdateObjectsWhere(t => t.Players.Contains(steamID.m_SteamID), t => t.Players.Remove(steamID.m_SteamID));
            ReloadTeam(GetTeam(steamID).ID);
        }

        public void ReloadTeam(ETeam team)
        {
            var target = GetObject(t => t.ID == team);

            switch (team)
            {
                case ETeam.TEAM1:
                    Team1 = target;
                    return;
                case ETeam.TEAM2:
                    Team2 = target;
                    return;
                case ETeam.NEUTRAL:
                    Neutral = target;
                    return;
            }
        }

        public List<UnturnedPlayer> GetOnlineMembers(ETeam team) => Provider.clients.Where(sp => GetObjectsWhere(t => t.ID == team).FirstOrDefault().GroupID == sp.player.quests.groupID.m_SteamID).Cast<UnturnedPlayer>().ToList();
        public Team GetTeam(UnturnedPlayer player) => GetTeam(player.CSteamID);
        public Team GetTeam(Player player) => GetTeam(player);
        public Team GetTeam(CSteamID steamID) => GetTeam(steamID.m_SteamID);
        public Team GetTeam(ulong steamID)
        {
            if (steamID == Team1.GroupID)
                return Team1;
            if (steamID == Team2.GroupID)
                return Team2;
            if (steamID == Neutral.GroupID)
                return Neutral;
            return new Team(ETeam.NEUTRAL, 0, "null");
        }

        public bool hasTeam(UnturnedPlayer player) => GetTeam(player) != null;

        public bool IsTeam(ulong steamID, ETeam team) => GetTeam(steamID).ID == team;
        public bool IsTeam(UnturnedPlayer player, ETeam team) => IsTeam(player.Player.quests.groupID.m_SteamID, team);

        public bool IsFriendly(ulong steamID1, ulong steamID2) => steamID1 == steamID2;
        public bool IsFriendly(UnturnedPlayer player, ulong steamID) => player.Player.quests.groupID.m_SteamID == steamID;
        public bool IsFriendly(CSteamID steamID1, CSteamID steamID2) => IsFriendly(steamID1.m_SteamID, steamID2.m_SteamID);

        public bool CanJoinTeam(ETeam team)
        {
            if (UCWarfare.Config.TeamSettings.BalanceTeams)
            {
                int Team1Count = GetOnlineMembers(ETeam.TEAM1).Count;
                int Team2Count = GetOnlineMembers(ETeam.TEAM2).Count;

                switch (team)
                {
                    case ETeam.TEAM1:
                        if ((float)Team1Count / (float)Team2Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent)
                            return false;
                        return true;
                    case ETeam.TEAM2:
                        if ((float)Team2Count / (float)Team1Count - 1 >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent)
                            return false;
                        return true;
                }
            }
            return true;
        }
        public void TeleportToMain(UnturnedPlayer player)
        {
            if (player.IsInVehicle)
                return;

            Team team = GetTeam(player);
            player.Player.teleportToLocation(team.Main.GetPosition(), team.Main.rotation);
        }
        public void PlayerLeaveProcess(SteamPlayer player)
        {
            var team = GetTeam(player.playerID.steamID);
            if (team != null)
                CommandWindow.LogWarning(player.playerID.playerName + " left team " + team.LocalizedName);
        }
    }

    public class Team
    {
        public ETeam ID;
        public ulong GroupID;
        public string Name;
        public MainBase Main;
        public List<ulong> Players;

        [JsonIgnore]
        public string LocalizedName
        {
            get
            {
                if (GroupID == 1)
                    return F.Translate("team_1");
                else if (GroupID == 2)
                    return F.Translate("team_2");
                else if (GroupID == 3)
                    return F.Translate("team_3");
                else
                {
                    if (Name == null)
                        return GroupID.ToString();
                    else return Name;
                }
            }
        }
        [JsonIgnore]
        public string Color
        {
            get
            {
                switch (ID)
                {
                    case ETeam.TEAM1:
                        return UCWarfare.I.ColorsHex["team_1_color"];
                    case ETeam.TEAM2:
                        return UCWarfare.I.ColorsHex["team_2_color"];
                    case ETeam.NEUTRAL:
                        return UCWarfare.I.ColorsHex["neutral_color"];
                }
                return UCWarfare.I.ColorsHex["neutral_color"];
            }
        }

        public Team(ETeam teamID, ulong groupID, string name)
        {
            ID = teamID;
            GroupID = groupID;
            Name = name;
            Main = null;
            Players = new List<ulong>();
        }
    }
    public enum ETeam
    {
        TEAM1 = 1,
        TEAM2 = 2,
        NEUTRAL = 3
    }
}
