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
    public class TeamManager : JSONSaver<TeamConfig>
    {
        private static TeamConfig _data;

        public TeamManager()
            : base(UCWarfare.TeamStorage + "teams.json")
        {
            if (GetExistingObjects().Count == 0)
            {
                LoadDefaults();
            }
        }
        public void Reload() => _data = GetExistingObjects().FirstOrDefault();
        public void Save() => WriteSingleObject(_data);

        protected override string LoadDefaults()
        {
            TeamConfig defaults = new TeamConfig
            {
                Team1ID = 1,
                Team2ID = 2,
                Team1Name = "USA",
                Team2Name = "Russia",
                Team1Code = "us",
                Team2Code = "ru",
                Team1Main = new MainBase(0, 0, 0, 0),
                Team2Main = new MainBase(0, 0, 0, 0)
            };

            WriteSingleObject(defaults);

            return "";
        }

        public static ulong Team1ID { get { return _data.Team1ID; } }
        public static ulong Team2ID { get { return _data.Team1ID; } }
        public static string Team1Name { get { return _data.Team1Name; } }
        public static string Team2Name { get { return _data.Team2Name; } }
        public static string Team1Code { get { return _data.Team1Code; } }
        public static string Team2Code { get { return _data.Team2Code; } }
        public static MainBase Team1Main { get { return _data.Team1Main; } }
        public static MainBase Team2Main { get { return _data.Team1Main; } }

        public static bool IsTeam1(ulong ID) => ID == Team1ID;
        public static bool IsTeam1(CSteamID steamID) => steamID.m_SteamID == Team1ID;
        public static bool IsTeam1(UnturnedPlayer player) => player.CSteamID.m_SteamID == Team1ID;
        public static bool IsTeam2(ulong ID) => ID == Team2ID;
        public static bool IsTeam2(CSteamID steamID) => steamID.m_SteamID == Team2ID;
        public static bool IsTeam2(UnturnedPlayer player) => player.Player.quests.groupID.m_SteamID == Team2ID;

        // better way of doing this??
        public static List<UnturnedPlayer> Team1Players => Provider.clients.Where(sp => sp.player.quests.groupID.m_SteamID == Team1ID).Cast<UnturnedPlayer>().ToList();
        public static List<UnturnedPlayer> Team2Players => Provider.clients.Where(sp => sp.player.quests.groupID.m_SteamID == Team2ID).Cast<UnturnedPlayer>().ToList();

        public static ulong GetTeam(ulong ID)
        {
            if (ID == Team1ID)
                return Team1ID;
            else if (ID == Team2ID)
                return Team2ID;
            return ID;
        }
        public static ulong GetTeam(UnturnedPlayer player) => GetTeam(player.CSteamID.m_SteamID);
        public static bool IsFriendly(ulong ID1, ulong ID2) => GetTeam(ID1) == GetTeam(ID2);
        public static bool IsFriendly(UnturnedPlayer player, CSteamID steamID) => GetTeam(player) == GetTeam(steamID.m_SteamID);
    }

    public class TeamConfig
    {
        public ulong Team1ID;
        public ulong Team2ID;
        public string Team1Name;
        public string Team2Name;
        public string Team1Code;
        public string Team2Code;
        public MainBase Team1Main;
        public MainBase Team2Main;

        public TeamConfig() { }
    }
}
