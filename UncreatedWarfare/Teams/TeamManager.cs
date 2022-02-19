using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Kits;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Teams
{
    public delegate void PlayerTeamDelegate(SteamPlayer player, ulong team);
    public class TeamManager
    {
        //private static TeamConfig _data;
        private static readonly Config<TeamConfig> _data = new Config<TeamConfig>(Data.TeamStorage, "teams.json");
        public const ulong ZOMBIE_TEAM_ID = ulong.MaxValue;

        public static ushort Team1Tickets;
        public static ushort Team2Tickets;

        public TeamManager()
        {
            if (!KitManager.KitExists(_data.data.team1unarmedkit, out _))
                L.LogError("Team 1's unarmed kit, \"" + _data.data.team1unarmedkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(_data.data.team2unarmedkit, out _))
                L.LogError("Team 2's unarmed kit, \"" + _data.data.team2unarmedkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            if (!KitManager.KitExists(_data.data.defaultkit, out _))
                L.LogError("The default kit, \"" + _data.data.defaultkit + "\", was not found, it should be added to \"" + Data.KitsStorage + "kits.json\".");
            object val = typeof(GroupManager).GetField("knownGroups", BindingFlags.Static | BindingFlags.NonPublic);
            if (val is Dictionary<CSteamID, GroupInfo> val2)
            {
                foreach (KeyValuePair<CSteamID, GroupInfo> kv in val2)
                {
                    if (kv.Key.m_SteamID == _data.data.team1id)
                    {
                        if (kv.Value.name != Team1Name)
                        {
                            L.Log("Renamed T1 group " + kv.Value.name + " to " + Team1Name, System.ConsoleColor.Magenta);
                            kv.Value.name = Team1Name;
                        }
                    }
                    else if (kv.Key.m_SteamID == _data.data.team2id)
                    {
                        if (kv.Value.name != Team2Name)
                        {
                            L.Log("Renamed T2 group " + kv.Value.name + " to " + Team2Name, System.ConsoleColor.Magenta);
                            kv.Value.name = Team2Name;
                        }
                    }
                    else if (kv.Key.m_SteamID == _data.data.adminid)
                    {
                        if (kv.Value.name != AdminName)
                        {
                            L.Log("Renamed Admin group " + kv.Value.name + " to " + AdminName, System.ConsoleColor.Magenta);
                            kv.Value.name = AdminName;
                        }
                    }
                    GroupManager.save();
                }
            }
        }
        public static ulong Team1ID { get => 1; }
        public static ulong Team2ID { get => 2; }
        public static ulong AdminID { get => 3; }
        public static string Team1Name { get => _data.data.team1name; }
        public static string Team2Name { get => _data.data.team2name; }
        public static string AdminName { get => _data.data.adminname; }
        public static string Team1Code { get => _data.data.team1code; }
        public static string Team2Code { get => _data.data.team2code; }
        public static string AdminCode { get => _data.data.admincode; }
        public static Color Team1Color { get => _data.data.Team1Color; }
        public static Color Team2Color { get => _data.data.Team2Color; }
        public static Color AdminColor { get => _data.data.AdminColor; }
        public static Color NeutralColor { get => _data.data.AdminColor; }
        public static string Team1ColorHex { get => _data.data.Team1ColorHex; }
        public static string Team2ColorHex { get => _data.data.Team2ColorHex; }
        public static string AdminColorHex { get => _data.data.AdminColorHex; }
        public static string NeutralColorHex { get => _data.data.AdminColorHex; }
        public static string Team1UnarmedKit { get => _data.data.team1unarmedkit; }
        public static string Team2UnarmedKit { get => _data.data.team2unarmedkit; }
        public static float Team1SpawnAngle { get => _data.data.team1spawnangle; }
        public static float Team2SpawnAngle { get => _data.data.team2spawnangle; }
        public static float LobbySpawnAngle { get => _data.data.lobbyspawnangle; }
        public static float TeamSwitchCooldown { get => _data.data.team_switch_cooldown; }
        public static string DefaultKit { get => _data.data.defaultkit; }
        internal static void ResetLocations()
        {
            _t1main = null;
            _t2main = null;
            _t1amc = null;
            _t2amc = null;
            _lobbyZone = null;
            _lobbySpawn = default;
        }
        private static Zone _t1main;
        private static Zone _t1amc;
        private static Zone _t2main;
        private static Zone _t2amc;
        private static Zone _lobbyZone;
        private static Vector3 _lobbySpawn = default;
        internal static void OnReloadFlags()
        {
            _lobbySpawn = default;
            _t1main = null;
            _t1amc = null;
            _t2main = null;
            _t2amc = null;
            _lobbyZone = null;
        }
        public static Zone Team1Main
        {
            get
            {
                if (_t1main == null && (Data.ExtraZones == null || !Data.ExtraZones.TryGetValue(1, out _t1main)))
                    _t1main = Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[1]);
                return _t1main;
            }
        }
        public static Zone Team2Main
        {
            get
            {
                if (_t2main == null && (Data.ExtraZones == null || !Data.ExtraZones.TryGetValue(2, out _t2main)))
                        _t2main = Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[2]);
                return _t2main;
            }
        }
        public static Zone Team1AMC
        {
            get
            {
                if (_t1amc == null && (Data.ExtraZones == null || !Data.ExtraZones.TryGetValue(101, out _t1amc)))
                    _t1amc = Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[101]);
                return _t1amc;
            }
        }
        public static Zone Team2AMC
        {
            get
            {
                if (_t2amc == null && (Data.ExtraZones == null || !Data.ExtraZones.TryGetValue(102, out _t2amc)))
                    _t2amc = Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[102]);
                return _t2amc;
            }
        }
        public static Zone LobbyZone
        {
            get
            {
                if (_lobbyZone == null && (Data.ExtraZones == null || !Data.ExtraZones.TryGetValue(-69, out _lobbyZone)))
                    _lobbyZone = Flag.ComplexifyZone(JSONMethods.DefaultExtraZones[-69]);
                return _lobbyZone;
            }
        }
        public static Vector3 LobbySpawn
        {
            get
            {
                if (_lobbySpawn == default && (Data.ExtraPoints == null || !Data.ExtraPoints.TryGetValue("lobby_spawn", out _lobbySpawn)))
                    _lobbySpawn = JSONMethods.DefaultExtraPoints.FirstOrDefault(x => x.name == "lobby_spawn").Vector3;
                return _lobbySpawn;
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

        public static bool IsInMain(UnturnedPlayer player)
        {
            ulong team = player.GetTeam();
            if (team == 1)
            {
                return Team1Main.IsInside(player.Position);
            }
            if (team == 2)
            {
                return Team2Main.IsInside(player.Position);
            }
            return false;
        }
        public static bool IsInMain(Player player)
        {
            if (player.life.isDead) return false;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                return Team1Main.IsInside(player.transform.position);
            }
            if (team == 2)
            {
                return Team2Main.IsInside(player.transform.position);
            }
            return false;
        }
        public static bool IsInMainOrLobby(Player player)
        {
            if (player.life.isDead) return false;
            ulong team = player.GetTeam();
            if (LobbyZone.IsInside(player.transform.position))
                return true;
            if (team == 1)
            {
                return Team1Main.IsInside(player.transform.position);
            }
            if (team == 2)
            {
                return Team2Main.IsInside(player.transform.position);
            }
            return false;
        }
        public static bool IsInAnyMainOrLobby(Player player)
        {
            if (player.life.isDead) return false;
            return LobbyZone.IsInside(player.transform.position) || Team1Main.IsInside(player.transform.position) || Team2Main.IsInside(player.transform.position);
        }
        public static bool IsInAnyMain(Vector3 player)
        {
            return Team1Main.IsInside(player) || Team2Main.IsInside(player);
        }
        public static bool IsInAnyMainOrAMCOrLobby(Vector3 player)
        {
            return LobbyZone.IsInside(player) || Team1Main.IsInside(player) || Team2Main.IsInside(player) || Team1AMC.IsInside(player) || Team2AMC.IsInside(player);
        }
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
            if (team == 1) uncolorized = Translation.Translate("team_1", player);
            else if (team == 2) uncolorized = Translation.Translate("team_2", player);
            else if (team == 3) uncolorized = Translation.Translate("team_3", player);
            else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", player);
            else if (team == 0) uncolorized = Translation.Translate("neutral", player);
            else uncolorized = team.ToString(Data.Locale);
            if (!colorize) return uncolorized;
            return F.ColorizeName(uncolorized, team);
        }
        public static string TranslateShortName(ulong team, ulong player, bool colorize = false)
        {
            string uncolorized;
            if (team == 1) uncolorized = Translation.Translate("team_1_short", player);
            else if (team == 2) uncolorized = Translation.Translate("team_2_short", player);
            else if (team == 3) uncolorized = Translation.Translate("team_3_short", player);
            else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", player);
            else if (team == 0) uncolorized = Translation.Translate("neutral", player);
            else uncolorized = team.ToString(Data.Locale);
            if (!colorize) return uncolorized;
            return F.ColorizeName(uncolorized, team);
        }
        public static string GetUnarmedFromS64ID(ulong playerSteam64)
        {
            ulong team = playerSteam64.GetTeamFromPlayerSteam64ID();
            if (team == 1) return Team1UnarmedKit;
            else if (team == 2) return Team2UnarmedKit;
            else return DefaultKit;
        }
        public static string GetTeamHexColor(ulong team)
        {
            switch (team)
            {
                case 1:
                    return Team1ColorHex;
                case 2:
                    return Team2ColorHex;
                case 3:
                    return AdminColorHex;
                case ZOMBIE_TEAM_ID:
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
                case ZOMBIE_TEAM_ID:
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
            foreach (SteamPlayer player in Provider.clients)
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
        public static ulong GetGroupID(ulong team)
        {
            if (team == 1) return Team1ID;
            else if (team == 2) return Team2ID;
            else if (team == 3) return AdminID;
            else return 0;
        }
        public static ulong GetTeam(UnturnedPlayer player) => player.GetTeam();
        public static ulong GetTeam(SteamPlayer player) => player.GetTeam();
        public static ulong GetTeam(Player player) => player.GetTeam();
        public static bool HasTeam(UnturnedPlayer player) => HasTeam(player.Player);
        public static bool HasTeam(SteamPlayer player) => HasTeam(player.player);
        public static bool HasTeam(Player player)
        {
            ulong t = player.GetTeam();
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
                if (team == 1)
                {
                    if (Team2Count > Team1Count) return true;
                    if ((float)(Team1Count - Team2Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                }
                else if (team == 2)
                {
                    if (Team1Count > Team2Count) return true;
                    if ((float)(Team2Count - Team1Count) / (Team1Count + Team2Count) >= UCWarfare.Config.TeamSettings.AllowedDifferencePercent) return false;
                }
            }
            return true;
        }
        internal static readonly Dictionary<ulong, byte> PlayerBaseStatus = new Dictionary<ulong, byte>();

        public static event PlayerTeamDelegate OnPlayerEnteredMainBase;
        public static event PlayerTeamDelegate OnPlayerLeftMainBase;

        public static void EvaluateBases()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer pl = Provider.clients[i];
                if (Team1Main.IsInside(pl.player.transform.position))
                {
                    if (PlayerBaseStatus.TryGetValue(pl.playerID.steamID.m_SteamID, out byte x))
                    {
                        if (x != 1)
                        {
                            PlayerBaseStatus[pl.playerID.steamID.m_SteamID] = 1;
                            OnPlayerLeftMainBase?.Invoke(pl, x);
                            OnPlayerEnteredMainBase?.Invoke(pl, 1);
                        }
                    }
                    else
                    {
                        PlayerBaseStatus.Add(pl.playerID.steamID.m_SteamID, 1);
                        OnPlayerEnteredMainBase?.Invoke(pl, 1);
                    }
                }
                else if (Team2Main.IsInside(pl.player.transform.position))
                {
                    if (PlayerBaseStatus.TryGetValue(pl.playerID.steamID.m_SteamID, out byte x))
                    {
                        if (x != 2)
                        {
                            PlayerBaseStatus[pl.playerID.steamID.m_SteamID] = 2;
                            OnPlayerLeftMainBase?.Invoke(pl, x);
                            OnPlayerEnteredMainBase?.Invoke(pl, 2);
                        }
                    }
                    else
                    {
                        PlayerBaseStatus.Add(pl.playerID.steamID.m_SteamID, 2);
                        OnPlayerEnteredMainBase?.Invoke(pl, 2);
                    }
                }
                else if (PlayerBaseStatus.TryGetValue(pl.playerID.steamID.m_SteamID, out byte x))
                {
                    PlayerBaseStatus.Remove(pl.playerID.steamID.m_SteamID);
                    OnPlayerLeftMainBase?.Invoke(pl, x);
                }
            }
        }
    }

    public class TeamConfig : ConfigData
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
        public float team1spawnangle;
        public float team2spawnangle;
        public float lobbyspawnangle;
        public float team_switch_cooldown;
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

        public TeamConfig() => SetDefaults();
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
            float lobbyspawnangle,
            float team_switch_cooldown)
        {
            this.team1id = team1id;
            this.team2id = team2id;
            this.adminid = adminid;
            this.team1name = team1name ?? "USA";
            this.team2name = team2name ?? "MEC";
            this.adminname = adminname ?? "Admins";
            this.team1code = team1code ?? "us";
            this.team2code = team2code ?? "me";
            this.admincode = admincode ?? "ad";
            this.team1unarmedkit = team1unarmedkit ?? "usunarmed";
            this.team2unarmedkit = team2unarmedkit ?? "meunarmed";
            this.defaultkit = defaultkit ?? "default";
            this.team1spawnangle = team1spawnangle;
            this.team2spawnangle = team2spawnangle;
            this.lobbyspawnangle = lobbyspawnangle;
            this.team_switch_cooldown = team_switch_cooldown;
        }
        public override void SetDefaults()
        {
            team1id = 1;
            team2id = 2;
            adminid = 3;
            team1name = "USA";
            team2name = "MEC";
            adminname = "Admins";
            team1code = "us";
            team2code = "me";
            admincode = "ad";
            team1unarmedkit = "usunarmed";
            team2unarmedkit = "meunarmed";
            defaultkit = "default";
            team1spawnangle = 0;
            team2spawnangle = 0;
            lobbyspawnangle = 90;
            team_switch_cooldown = 1200;
        }
    }
}
