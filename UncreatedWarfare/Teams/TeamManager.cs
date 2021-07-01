using Newtonsoft.Json;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Teams
{
    public class TeamManager : JSONSaver<TeamConfig>
    {
        private static TeamConfig _data;
        public const ulong ZombieTeamID = ulong.MaxValue;

        public static ushort Team1Tickets;
        public static ushort Team2Tickets;

        public TeamManager()
            : base(Data.TeamStorage + "teams.json")
        {
            _data = ReloadSingle(LoadDefaults(), () => { return new TeamConfig(); });
            if (!KitManager.KitExists(_data.team1unarmedkit, out _)) 
                F.LogError("Team 1's unarmed kit, \"" + _data.team1unarmedkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if(!KitManager.KitExists(_data.team2unarmedkit, out _)) 
                F.LogError("Team 2's unarmed kit, \"" + _data.team2unarmedkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if(!KitManager.KitExists(_data.defaultkit, out _)) 
                F.LogError("The default kit, \"" + _data.defaultkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            
        }
        public new static void Save() => WriteSingleObject(_data);
        protected override string LoadDefaults() => F.QuickSerialize(new TeamConfig());
        public static ulong Team1ID { get => _data.team1id; }
        public static ulong Team2ID { get => _data.team2id; }
        public static ulong AdminID { get => _data.adminid; }
        public static string Team1Name { get => _data.team1name; }
        public static string Team2Name { get => _data.team2name; }
        public static string AdminName { get => _data.adminname; }
        public static string Team1Code { get => _data.team1code; }
        public static string Team2Code { get => _data.team2code; }
        public static string AdminCode { get => _data.admincode; }
        public static Color Team1Color { get => _data.Team1Color; }
        public static Color Team2Color { get => _data.Team2Color; }
        public static Color AdminColor { get => _data.AdminColor; }
        public static Color NeutralColor { get => _data.AdminColor; }
        public static string Team1ColorHex { get => _data.Team1ColorHex; }
        public static string Team2ColorHex { get => _data.Team2ColorHex; }
        public static string AdminColorHex { get => _data.AdminColorHex; }
        public static string NeutralColorHex { get => _data.AdminColorHex; }
        public static string Team1UnarmedKit { get => _data.team1unarmedkit; }
        public static string Team2UnarmedKit { get => _data.team2unarmedkit; }
        public static float Team1SpawnAngle { get => _data.team1spawnangle; }
        public static float Team2SpawnAngle { get => _data.team2spawnangle; }
        public static float LobbySpawnAngle { get => _data.lobbyspawnangle; }
        public static string DefaultKit { get => _data.defaultkit; }
        public static Zone Team1Main { get {
                if (Data.ExtraZones != null && Data.ExtraZones.ContainsKey(1))
                    return Data.ExtraZones[1];
                else
                    return Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[1]);
            } }
        public static Zone Team2Main { get {
                if (Data.ExtraZones != default && Data.ExtraZones.ContainsKey(2))
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
        public static ulong Other(ulong team)
        {
            if (team == 1) return 2;
            else if (team == 2) return 1;
            else return team;
        }
        public static bool IsTeam1(ulong ID) => ID == Team1ID;
        public static bool IsTeam1(CSteamID steamID) => steamID.m_SteamID == Team1ID;
        public static bool IsTeam1(UnturnedPlayer player) => player.Player.quests.groupID.m_SteamID == Team1ID;
        public static bool IsTeam1(Player player) => player.quests.groupID.m_SteamID == Team1ID;
        public static bool IsTeam2(ulong ID) => ID == Team2ID;
        public static bool IsTeam2(CSteamID steamID) => steamID.m_SteamID == Team2ID;
        public static bool IsTeam2(UnturnedPlayer player) => player.Player.quests.groupID.m_SteamID == Team2ID;
        public static bool IsTeam2(Player player) => player.quests.groupID.m_SteamID == Team2ID;

        // Same as Team.LocalizedName from before./ 
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
            else if (team == 0) uncolorized = F.Translate("neutral", player);
            else uncolorized = team.ToString(Data.Locale);
            if (!colorize) return uncolorized;
            return F.ColorizeName(uncolorized, team);
        }
        public static string GetUnarmedFromS64ID(ulong playerSteam64)
        {
            ulong team = F.GetTeamFromPlayerSteam64ID(playerSteam64);
            if (team == 1) return Team1UnarmedKit;
            else if (team == 2) return Team2UnarmedKit;
            else return DefaultKit;
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
        public static void GetBothTeamPlayersFast(out List<SteamPlayer> t1, out List<SteamPlayer> t2)
        {
            t1 = new List<SteamPlayer>();
            t2 = new List<SteamPlayer>();
            foreach(SteamPlayer player in Provider.clients)
            {
                if (player.player.quests.groupID.m_SteamID == Team1ID) t1.Add(player);
                else if (player.player.quests.groupID.m_SteamID == Team2ID) t2.Add(player);
            }
        }
        public static List<SteamPlayer> GetTeamPlayers(ulong team)
        {
            if (team == 1) return Team1Players;
            else if (team == 2) return Team2Players;
            else return Provider.clients.Where(sp => sp.player.quests.groupID.m_SteamID == team).ToList();
        }
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

        public static bool CanJoinTeam(ulong team)
        {
            if (UCWarfare.Config.TeamSettings.BalanceTeams)
            {
                GetBothTeamPlayersFast(out List<SteamPlayer> t1, out List<SteamPlayer> t2);
                int Team1Count = t1.Count;
                int Team2Count = t2.Count;
                if (Team1Count == Team2Count) return true;
                if(team == 1)
                {
                    if (Team2Count > Team1Count) return true;
                    if ((Team1Count - Team2Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                } else if (team == 2)
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
        public ulong team1id;
        public ulong team2id;
        public ulong adminid;
        public string team1name;
        public string team2name;
        public string adminname;
        public string team1code;
        public string team2code;
        public string admincode;
        public string team1unarmedkit;
        public string team2unarmedkit;
        public string defaultkit;
        // 0-360
        public float team1spawnangle;
        public float team2spawnangle;
        public float lobbyspawnangle;
        [JsonIgnore]
        public Color Team1Color
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_1_color");
                else return Color.white;
            }
        }
        [JsonIgnore]
        public Color Team2Color
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_2_color");
                else return Color.white;
            }
        }
        [JsonIgnore]
        public Color AdminColor
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("team_3_color");
                else return Color.cyan;
            }
        }
        [JsonIgnore]
        public Color NeutralColor
        {
            get
            {
                if (Data.Colors != default)
                    return UCWarfare.GetColor("neutral_color");
                else return Color.white;
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
        
        public TeamConfig()
        {
            team1id = 1;
            team2id = 2;
            adminid = 3;
            team1name = "USA";
            team2name = "Russia";
            adminname = "Admins";
            team1code = "us";
            team2code = "ru";
            admincode = "ad";
            team1unarmedkit = "usunarmed";
            team2unarmedkit = "ruunarmed";
            defaultkit = "default";
            team1spawnangle = 0f;
            team2spawnangle = 0f;
            lobbyspawnangle = 0f;
        }
        [JsonConstructor]
        public TeamConfig(ulong team1id, 
            ulong team2id, 
            ulong adminid, 
            string team1name, 
            string team2name, 
            string adminname, 
            string team1code, 
            string team2code, 
            string admincode, 
            string team1unarmedkit, 
            string team2unarmedkit, 
            string defaultkit,
            float team1spawnangle,
            float team2spawnangle,
            float lobbyspawnangle)
        {
            this.team1id = team1id;
            this.team2id = team2id;
            this.adminid = adminid;
            this.team1name = team1name ?? "USA";
            this.team2name = team2name ?? "Russia";
            this.adminname = adminname ?? "Admins";
            this.team1code = team1code ?? "us";
            this.team2code = team2code ?? "ru";
            this.admincode = admincode ?? "ad";
            this.team1unarmedkit = team1unarmedkit ?? "usunarmed";
            this.team2unarmedkit = team2unarmedkit ?? "ruunarmed";
            this.defaultkit = defaultkit ?? "default";
            this.team1spawnangle = team1spawnangle;
            this.team2spawnangle = team2spawnangle;
            this.lobbyspawnangle = lobbyspawnangle;
        }
    }
}
