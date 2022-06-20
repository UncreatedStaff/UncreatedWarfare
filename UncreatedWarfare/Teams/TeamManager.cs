using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Gamemodes.Flags;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public delegate void PlayerTeamDelegate(SteamPlayer player, ulong team);
public static class TeamManager
{
    //private static TeamConfig _data;
    private static readonly Config<TeamConfig> _data = new Config<TeamConfig>(Data.Paths.TeamStorage, "teams.json");
    public const ulong ZOMBIE_TEAM_ID = ulong.MaxValue;

    public static ushort Team1Tickets;
    public static ushort Team2Tickets;
    private static Zone? _t1main;
    private static Zone? _t1amc;
    private static Zone? _t2main;
    private static Zone? _t2amc;
    private static Zone? _lobbyZone;
    private static Vector3 _lobbySpawn = default;
    internal static readonly Dictionary<ulong, byte> PlayerBaseStatus = new Dictionary<ulong, byte>();
    public static event PlayerTeamDelegate OnPlayerEnteredMainBase;
    public static event PlayerTeamDelegate OnPlayerLeftMainBase;
    public static TeamConfig Config => _data.Data;
    public static ulong Team1ID => 1;
    public static ulong Team2ID => 2;
    public static ulong AdminID => 3;
    public static string Team1Name => _data.Data.Team1Name;
    public static string Team2Name => _data.Data.Team2Name;
    public static string AdminName => _data.Data.AdminTeamName;
    public static string Team1Code => _data.Data.Team1Abbreviation;
    public static string Team2Code => _data.Data.Team2Abbreviation;
    public static string AdminCode => _data.Data.AdminTeamAbbreviation;
    public static Color Team1Color => _data.Data.Team1Color;
    public static Color Team2Color => _data.Data.Team2Color;
    public static Color AdminColor => _data.Data.AdminColor;
    public static Color NeutralColor => _data.Data.NeutralColor;
    public static string Team1ColorHex => _data.Data.Team1ColorHex;
    public static string Team2ColorHex => _data.Data.Team2ColorHex;
    public static string AdminColorHex => _data.Data.AdminColorHex;
    public static string NeutralColorHex => _data.Data.NeutralColorHex;
    public static string Team1UnarmedKit => _data.Data.Team1UnarmedKit;
    public static string Team2UnarmedKit => _data.Data.Team2UnarmedKit;
    public static float Team1SpawnAngle => _data.Data.Team1SpawnYaw;
    public static float Team2SpawnAngle => _data.Data.Team2SpawnYaw;
    public static float LobbySpawnAngle => _data.Data.LobbySpawnpointYaw;
    public static float TeamSwitchCooldown => _data.Data.TeamSwitchCooldown;
    public static string DefaultKit => _data.Data.DefaultKit;
    public static Zone Team1Main
    {
        get
        {
            if (_t1main is null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.T1_MAIN)
                    {
                        _t1main = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (_t1main is null)
                {
                    L.LogWarning("There is no defined Team 1 base. Using default instead.");
                    for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                    {
                        if (JSONMethods.DefaultZones[i].UseCase == EZoneUseCase.T1_MAIN)
                        {
                            _t1main = JSONMethods.DefaultZones[i].GetZone();
                            break;
                        }
                    }
                }
            }
            return _t1main!;
        }
    }
    public static Zone Team2Main
    {
        get
        {
            if (_t2main is null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.T2_MAIN)
                    {
                        _t2main = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (_t2main is null)
                {
                    L.LogWarning("There is no defined Team 2 base. Using default instead.");
                    for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                    {
                        if (JSONMethods.DefaultZones[i].UseCase == EZoneUseCase.T2_MAIN)
                        {
                            _t2main = JSONMethods.DefaultZones[i].GetZone();
                            break;
                        }
                    }
                }
            }
            return _t2main!;
        }
    }
    public static Zone Team1AMC
    {
        get
        {
            if (_t1amc == null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.T1_AMC)
                    {
                        _t1amc = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (_t1amc == null)
                {
                    L.LogWarning("There is no defined Team 1 AMC. Using default instead.");
                    for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                    {
                        if (JSONMethods.DefaultZones[i].UseCase == EZoneUseCase.T1_AMC)
                        {
                            _t1amc = JSONMethods.DefaultZones[i].GetZone();
                            break;
                        }
                    }
                }
            }
            return _t1amc!;
        }
    }
    public static Zone Team2AMC
    {
        get
        {
            if (_t2amc == null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.T2_AMC)
                    {
                        _t2amc = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (_t2amc == null)
                {
                    L.LogWarning("There is no defined Team 2 AMC. Using default instead.");
                    for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                    {
                        if (JSONMethods.DefaultZones[i].UseCase == EZoneUseCase.T2_AMC)
                        {
                            _t2amc = JSONMethods.DefaultZones[i].GetZone();
                            break;
                        }
                    }
                }
            }
            return _t2amc!;
        }
    }
    public static Zone LobbyZone
    {
        get
        {
            if (_lobbyZone == null)
            {
                for (int i = 0; i < Data.ZoneProvider.Zones.Count; ++i)
                {
                    if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.LOBBY)
                    {
                        _lobbyZone = Data.ZoneProvider.Zones[i];
                        break;
                    }
                }
                if (_lobbyZone == null)
                {
                    L.LogWarning("There is no defined lobby zone. Using default instead.");
                    for (int i = 0; i < JSONMethods.DefaultZones.Count; ++i)
                    {
                        if (JSONMethods.DefaultZones[i].UseCase == EZoneUseCase.LOBBY)
                        {
                            _lobbyZone = JSONMethods.DefaultZones[i].GetZone();
                            break;
                        }
                    }
                }
            }
            return _lobbyZone!;
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
    internal static void ResetLocations()
    {
        _t1main = null;
        _t2main = null;
        _t1amc = null;
        _t2amc = null;
        _lobbyZone = null;
        _lobbySpawn = default;
    }
    internal static void OnReloadFlags()
    {
        ResetLocations();

        // cache them all
        _ = LobbyZone;
        _ = Team1Main;
        _ = Team2Main;
        _ = Team1AMC;
        _ = Team2AMC;
    }
    public static void CheckGroups()
    {
        object? val = typeof(GroupManager).GetField("knownGroups", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
        if (val is Dictionary<CSteamID, GroupInfo> val2)
        {
            foreach (KeyValuePair<CSteamID, GroupInfo> kv in val2.ToList())
            {
                if (kv.Key.m_SteamID == _data.Data.Team1ID)
                {
                    if (kv.Value.name != Team1Name)
                    {
                        L.Log("Renamed T1 group " + kv.Value.name + " to " + Team1Name, ConsoleColor.Magenta);
                        kv.Value.name = Team1Name;
                    }
                }
                else if (kv.Key.m_SteamID == _data.Data.Team2ID)
                {
                    if (kv.Value.name != Team2Name)
                    {
                        L.Log("Renamed T2 group " + kv.Value.name + " to " + Team2Name, ConsoleColor.Magenta);
                        kv.Value.name = Team2Name;
                    }
                }
                else if (kv.Key.m_SteamID == _data.Data.AdminID)
                {
                    if (kv.Value.name != AdminName)
                    {
                        L.Log("Renamed Admin group " + kv.Value.name + " to " + AdminName, ConsoleColor.Magenta);
                        kv.Value.name = AdminName;
                    }
                }
                else if (kv.Key.m_SteamID > _data.Data.AdminID || kv.Key.m_SteamID < _data.Data.Team1ID)
                    val2.Remove(kv.Key);
            }
            GroupManager.save();
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
    public static bool IsTeam1(Player player) => player.quests.groupID.m_SteamID == Team1ID;
    public static bool IsTeam2(ulong ID) => ID == Team2ID;
    public static bool IsTeam2(CSteamID steamID) => steamID.m_SteamID == Team2ID;
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
    public static bool IsTeam2(Player player) => player.quests.groupID.m_SteamID == Team2ID;
    public static string TranslateName(ulong team, SteamPlayer player, bool colorize = false) => TranslateName(team, player.playerID.steamID.m_SteamID, colorize);
    public static string TranslateName(ulong team, Player player, bool colorize = false) => TranslateName(team, player.channel.owner.playerID.steamID.m_SteamID, colorize);
    public static string TranslateName(ulong team, CSteamID player, bool colorize = false) => TranslateName(team, player.m_SteamID, colorize);
    public static string TranslateName(ulong team, UCPlayer player, bool colorize = false) => TranslateName(team, player.Steam64, colorize);
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
    public static string TranslateName(ulong team, string language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Translation.Translate("team_1", language);
        else if (team == 2) uncolorized = Translation.Translate("team_2", language);
        else if (team == 3) uncolorized = Translation.Translate("team_3", language);
        else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", language);
        else if (team == 0) uncolorized = Translation.Translate("neutral", language);
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
    public static string TranslateShortName(ulong team, string language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Translation.Translate("team_1_short", language);
        else if (team == 2) uncolorized = Translation.Translate("team_2_short", language);
        else if (team == 3) uncolorized = Translation.Translate("team_3_short", language);
        else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", language);
        else if (team == 0) uncolorized = Translation.Translate("neutral", language);
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
        return team switch
        {
            1 => Team1ColorHex,
            2 => Team2ColorHex,
            3 => AdminColorHex,
            ZOMBIE_TEAM_ID => UCWarfare.GetColorHex("death_zombie_name_color"),
            _ => NeutralColorHex,
        };
    }
    public static Color GetTeamColor(ulong team)
    {
        return team switch
        {
            1 => Team1Color,
            2 => Team2Color,
            3 => AdminColor,
            ZOMBIE_TEAM_ID => UCWarfare.GetColor("death_zombie_name_color"),
            _ => NeutralColor,
        };
    }
    public static ulong GetGroupID(ulong team)
    {
        if (team == 1) return Team1ID;
        else if (team == 2) return Team2ID;
        else if (team == 3) return AdminID;
        else return 0;
    }
    public static ulong GetTeam(SteamPlayer player) => player.GetTeam();
    public static ulong GetTeam(Player player) => player.GetTeam();
    public static bool HasTeam(SteamPlayer player) => HasTeam(player.player);
    public static bool HasTeam(Player player)
    {
        ulong t = player.GetTeam();
        return t == 1 || t == 2;
    }
    public static bool HasTeam(ulong groupID) => groupID == Team1ID || groupID == Team2ID;
    public static bool IsFriendly(ulong ID1, ulong ID2) => ID1 == ID2;
    public static bool IsFriendly(SteamPlayer player, CSteamID groupID) => player.player.quests.groupID.m_SteamID == groupID.m_SteamID;
    public static bool IsFriendly(Player player, CSteamID groupID) => player.quests.groupID.m_SteamID == groupID.m_SteamID;
    public static bool IsFriendly(SteamPlayer player, ulong groupID) => player.player.quests.groupID.m_SteamID == groupID;
    public static bool IsFriendly(Player player, ulong groupID) => player.quests.groupID.m_SteamID == groupID;
    public static bool IsFriendly(SteamPlayer player, SteamPlayer player2) => player.player.quests.groupID.m_SteamID == player2.player.quests.groupID.m_SteamID;
    public static bool IsFriendly(Player player, Player player2) => player.quests.groupID.m_SteamID == player2.quests.groupID.m_SteamID;
    public static bool CanJoinTeam(ulong team)
    {
        if (_data.Data.BalanceTeams)
        {
            int Team1Count = PlayerManager.OnlinePlayers.Count(x => x.GetTeam() == 1);
            int Team2Count = PlayerManager.OnlinePlayers.Count(x => x.GetTeam() == 2);
            if (Team1Count == Team2Count) return true;
            if (team == 1)
            {
                if (Team2Count > Team1Count) return true;
                if ((float)(Team1Count - Team2Count) / (Team1Count + Team2Count) >= _data.Data.AllowedDifferencePercent) return false;
            }
            else if (team == 2)
            {
                if (Team1Count > Team2Count) return true;
                if ((float)(Team2Count - Team1Count) / (Team1Count + Team2Count) >= _data.Data.AllowedDifferencePercent) return false;
            }
        }
        return true;
    }

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
    [JsonPropertyName("team1id")]
    public ulong Team1ID;
    [JsonPropertyName("team2id")]
    public ulong Team2ID;
    [JsonPropertyName("adminid")]
    public ulong AdminID;
    [JsonPropertyName("team1name")]
    public string Team1Name;
    [JsonPropertyName("team2name")]
    public string Team2Name;
    [JsonPropertyName("adminname")]
    public string AdminTeamName;
    [JsonPropertyName("team1code")]
    public string Team1Abbreviation;
    [JsonPropertyName("team2code")]
    public string Team2Abbreviation;
    [JsonPropertyName("admincode")]
    public string AdminTeamAbbreviation;
    [JsonPropertyName("team1unarmedkit")]
    public string Team1UnarmedKit;
    [JsonPropertyName("team2unarmedkit")]
    public string Team2UnarmedKit;
    [JsonPropertyName("defaultkit")]
    public string DefaultKit;
    [JsonPropertyName("team1spawnangle")]
    public float Team1SpawnYaw;
    [JsonPropertyName("team2spawnangle")]
    public float Team2SpawnYaw;
    [JsonPropertyName("lobbyspawnangle")]
    public float LobbySpawnpointYaw;
    [JsonPropertyName("team_switch_cooldown")]
    public float TeamSwitchCooldown;
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
    [JsonPropertyName("allowedTeamGap")]
    public float AllowedDifferencePercent;
    [JsonPropertyName("balanceTeams")]
    public bool BalanceTeams;
    public TeamConfig() => SetDefaults();
    [JsonConstructor]
    public TeamConfig(ulong team1id,
        ulong team2id,
        ulong adminid,
        string team1name,
        string team2name,
        string adminteamname,
        string team1Abbreviation,
        string team2Abbreviation,
        string adminTeamAbbreviation,
        string team1unarmedkit,
        string team2unarmedkit,
        string defaultkit,
        float team1spawnyaw,
        float team2spawnyaw,
        float lobbySpawnpointYaw,
        float teamSwitchCooldown)
    {
        this.Team1ID = team1id;
        this.Team2ID = team2id;
        this.AdminID = adminid;
        this.Team1Name = team1name ?? "United States";
        this.Team2Name = team2name ?? "Middle Eastern Coalition";
        this.AdminTeamName = adminteamname ?? "Admins";
        this.Team1Abbreviation = team1Abbreviation ?? "USA";
        this.Team2Abbreviation = team2Abbreviation ?? "MEC";
        this.AdminTeamAbbreviation = adminTeamAbbreviation ?? "ADMIN";
        this.Team1UnarmedKit = team1unarmedkit ?? "usunarmed";
        this.Team2UnarmedKit = team2unarmedkit ?? "meunarmed";
        this.DefaultKit = defaultkit ?? "default";
        this.Team1SpawnYaw = team1spawnyaw;
        this.Team2SpawnYaw = team2spawnyaw;
        this.LobbySpawnpointYaw = lobbySpawnpointYaw;
        this.TeamSwitchCooldown = teamSwitchCooldown;
    }
    public override void SetDefaults()
    {
        Team1ID = 1;
        Team2ID = 2;
        AdminID = 3;
        Team1Name = "United States";
        Team2Name = "Middle Eastern Coalition";
        AdminTeamName = "Admins";
        Team1Abbreviation = "USA";
        Team2Abbreviation = "MEC";
        AdminTeamAbbreviation = "ADMIN";
        Team1UnarmedKit = "usunarmed";
        Team2UnarmedKit = "meunarmed";
        DefaultKit = "default";
        Team1SpawnYaw = 0;
        Team2SpawnYaw = 0;
        LobbySpawnpointYaw = 90;
        TeamSwitchCooldown = 1200;
    }
}
