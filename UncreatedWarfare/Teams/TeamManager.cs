using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Flags;
using UnityEngine;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare.Teams
{
    public class TeamManager : JSONSaver<TeamConfig>
    {
        private static TeamConfig _data;
        public const ulong ZombieTeamID = ulong.MaxValue;
        public TeamManager()
            : base(Data.TeamStorage + "teams.json")
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
            };

            WriteSingleObject(defaults);

            return "";
        }
        public static ulong Team1ID { get => _data.Team1ID; }
        public static ulong Team2ID { get => _data.Team1ID; }
        public static ulong AdminID { get => _data.AdminID; }
        public static string Team1Name { get => _data.Team1Name; }
        public static string Team2Name { get => _data.Team2Name; }
        public static string AdminName { get => _data.AdminName; }
        public static string Team1Code { get => _data.Team1Code; }
        public static string Team2Code { get => _data.Team2Code; }
        public static string AdminCode { get => _data.AdminCode; }
        public static Color Team1Color => _data.Team1Color;
        public static Color Team2Color => _data.Team2Color;
        public static Color AdminColor => _data.AdminColor;
        public static Color NeutralColor => _data.AdminColor;
        public static string Team1ColorHex => _data.Team1ColorHex;
        public static string Team2ColorHex => _data.Team2ColorHex;
        public static string AdminColorHex => _data.AdminColorHex;
        public static string NeutralColorHex => _data.AdminColorHex;
        public static Zone Team1Main { get {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(1))
                    return Data.ExtraZones[1];
                else
                    return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[1]);
            } }
        public static Zone Team2Main { get {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(2))
                    return Data.ExtraZones[2];
                else
                    return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[2]);
            } }
        public static Zone Team1AMC 
        { 
            get {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(101))
                    return Data.ExtraZones[101];
                else return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[101]);
            }
        }
        public static Zone Team2AMC
        {
            get
            {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(102))
                    return Data.ExtraZones[102];
                else return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[102]);
            }
        }
        public static Zone LobbyZone { 
            get
            {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(-69))
                    return Data.ExtraZones[-69];
                else return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[-69]);
            }
        }
        public static Vector3 LobbySpawn
        {
            get
            {
                if (Data.ExtraPoints != default && Data.ExtraPoints.ContainsKey("lobby_spawn"))
                    return Data.ExtraPoints["lobby_spawn"];
                else return JSONMethods.DefaultExtraPoints.FirstOrDefault(x => x.name == "lobby_spawn").Vector3;
            }
        }
        public static bool IsTeam1(ulong ID) => ID == Team1ID;
        public static bool IsTeam1(CSteamID steamID) => steamID.m_SteamID == Team1ID;
        public static bool IsTeam1(UnturnedPlayer player) => player.CSteamID.m_SteamID == Team1ID;
        public static bool IsTeam2(ulong ID) => ID == Team2ID;
        public static bool IsTeam2(CSteamID steamID) => steamID.m_SteamID == Team2ID;
        public static bool IsTeam2(UnturnedPlayer player) => player.Player.quests.groupID.m_SteamID == Team2ID;

        // Same as Team.LocalizedName from before.
        public static string TranslateName(ulong team, UnturnedPlayer player, bool colorize = false) => TranslateName(team, player.CSteamID.m_SteamID, colorize);
        public static string TranslateName(ulong team, SteamPlayer player, bool colorize = false) => TranslateName(team, player.playerID.steamID.m_SteamID, colorize);
        public static string TranslateName(ulong team, Player player, bool colorize = false) => TranslateName(team, player.channel.owner.playerID.steamID.m_SteamID, colorize);
        public static string TranslateName(ulong team, CSteamID player, bool colorize = false) => TranslateName(team, player.m_SteamID, colorize);
        public static string TranslateName(ulong team, ulong player, bool colorize = false)
        {
            string uncolorized;
            if (team == 1) uncolorized = F.Translate("team_1", player);
            else if (team == 2) uncolorized = F.Translate("team_2", player);
            else if (team == 3) uncolorized = F.Translate("team_3", player);
            else if (team == ZombieTeamID) uncolorized = F.Translate("zombie", player);
            else uncolorized = F.Translate("neutral", player);
            if (!colorize) return uncolorized;
            return F.ColorizeName(uncolorized, team);
        }
        public static string GetTeamHexColor(ulong team)
        {
            switch(team)
            {
                case 1:
                    return Team1ColorHex;
                case 2:
                    return Team2ColorHex;
                case 3:
                    return AdminColorHex;
                case ZombieTeamID:
                    return UCWarfare.GetColorHex("death_zombie_name_color");
                default:
                    return NeutralColorHex;
            }
        }
        public static Color GetTeamColor(ulong team)
        {
            switch (team)
            {
                case 1:
                    return Team1Color;
                case 2:
                    return Team2Color;
                case 3:
                    return AdminColor;
                case ZombieTeamID:
                    return UCWarfare.GetColor("death_zombie_name_color");
                default:
                    return NeutralColor;
            }
        }
        public static List<SteamPlayer> Team1Players => Provider.clients.Where(sp => sp.player.quests.groupID.m_SteamID == Team1ID).ToList();
        public static List<SteamPlayer> Team2Players => Provider.clients.Where(sp => sp.player.quests.groupID.m_SteamID == Team2ID).ToList();

        public static ulong GetTeam(UnturnedPlayer player) => F.GetTeam(player);
        public static ulong GetTeam(SteamPlayer player) => F.GetTeam(player);
        public static ulong GetTeam(Player player) => F.GetTeam(player);
        public static bool HasTeam(UnturnedPlayer player) => HasTeam(player.Player);
        public static bool HasTeam(SteamPlayer player) => HasTeam(player.player);
        public static bool HasTeam(Player player)
        {
            ulong t = F.GetTeam(player);
            return t == 1 || t == 2;
        }
        public static bool HasTeam(ulong groupID) => groupID == Team1ID || groupID == Team2ID;
        public static bool IsFriendly(ulong ID1, ulong ID2) => ID1 == ID2;
        public static bool IsFriendly(UnturnedPlayer player, CSteamID groupID) => player.Player.quests.groupID.m_SteamID == groupID.m_SteamID;
        public static bool IsFriendly(SteamPlayer player, CSteamID groupID) => player.player.quests.groupID.m_SteamID == groupID.m_SteamID;
        public static bool IsFriendly(Player player, CSteamID groupID) => player.quests.groupID.m_SteamID == groupID.m_SteamID;
        public static bool IsFriendly(UnturnedPlayer player, ulong groupID) => player.Player.quests.groupID.m_SteamID == groupID;
        public static bool IsFriendly(SteamPlayer player, ulong groupID) => player.player.quests.groupID.m_SteamID == groupID;
        public static bool IsFriendly(Player player, ulong groupID) => player.quests.groupID.m_SteamID == groupID;
        public static bool IsFriendly(UnturnedPlayer player, UnturnedPlayer player2) => player.Player.quests.groupID.m_SteamID == player2.Player.quests.groupID.m_SteamID;
        public static bool IsFriendly(SteamPlayer player, SteamPlayer player2) => player.player.quests.groupID.m_SteamID == player2.player.quests.groupID.m_SteamID;
        public static bool IsFriendly(Player player, Player player2) => player.quests.groupID.m_SteamID == player2.quests.groupID.m_SteamID;
        public static bool CanJoinTeam(ulong groupID)
        {
            if (UCWarfare.Config.TeamSettings.BalanceTeams)
            {
                int Team1Count = Team1Players.Count;
                int Team2Count = Team2Players.Count;
                if (Team1Count == Team2Count) return true;
                if(groupID == _data.Team1ID)
                {
                    if (Team2Count > Team1Count) return true;
                    if ((Team1Count - Team2Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                } else if (groupID == _data.Team2ID)
                {
                    if (Team1Count > Team2Count) return true;
                    if ((Team2Count - Team1Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                }
            }
            return true;
        }
    }

    public class TeamConfig
    {
        public ulong Team1ID;
        public ulong Team2ID;
        public ulong AdminID;
        public string Team1Name;
        public string Team2Name;
        public string AdminName;
        public string Team1Code;
        public string Team2Code;
        public string AdminCode; 
        [JsonIgnore]
        public Color Team1Color
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_1_color");
                else return UnityEngine.Color.white;
            }
        }
        [JsonIgnore]
        public Color Team2Color
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_2_color");
                else return UnityEngine.Color.white;
            }
        }
        [JsonIgnore]
        public Color AdminColor
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_3_color");
                else return UnityEngine.Color.cyan;
            }
        }
        [JsonIgnore]
        public Color NeutralColor
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("neutral_color");
                else return UnityEngine.Color.white;
            }
        }
        [JsonIgnore]
        public string Team1ColorHex
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColorHex("team_1_color");
                else return "ffffff";
            }
        }
        [JsonIgnore]
        public string Team2ColorHex
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColorHex("team_2_color");
                else return "ffffff";
            }
        }
        [JsonIgnore]
        public string AdminColorHex
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColorHex("team_3_color");
                else return "00ffff";
            }
        }
        [JsonIgnore]
        public string NeutralColorHex
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColorHex("neutral_color");
                else return "ffffff";
            }
        }
        public MainBase Team1Main;
        public MainBase Team2Main;

        public TeamConfig()
        {
            Team1ID = 1;
            Team2ID = 2;
            AdminID = 3;
            Team1Name = "USA";
            Team2Name = "Russia";
            AdminName = "Admins";
            Team1Code = "us";
            Team2Code = "ru";
            AdminCode = "ad";
            Team1Main = new MainBase(0, 0, 0, 0);
            Team2Main = new MainBase(0, 0, 0, 0);
        }
    }
}
