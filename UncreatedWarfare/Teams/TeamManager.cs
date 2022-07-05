using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public delegate void PlayerTeamDelegate(SteamPlayer player, ulong team);
public static class TeamManager
{
    private static TeamConfig _data;
    private static List<FactionInfo> _factions;
    public const ulong ZOMBIE_TEAM_ID = ulong.MaxValue;
    private static readonly FactionInfo[] DefaultFactions = new FactionInfo[]
    {
        new FactionInfo("admins", "Admins", "ADMIN", "Admins", "0099ff", "default"),
        new FactionInfo("usa", "United States", "USA", "USA", "78b2ff", "usunarmed", @"https://i.imgur.com/P4JgkHB.png")
        {
            Build = "a70978a0b47e4017a0261e676af57042",
            Ammo = "51e1e372bf5341e1b4b16a0eacce37eb",
            FOBRadio = "7715ad81f1e24f60bb8f196dd09bd4ef",
            RallyPoint = "5e1db525179341d3b0c7576876212a81"
        },
        new FactionInfo("russia", "Russia", "RU", "Russia", "f53b3b", "ruunarmed", @"https://i.imgur.com/YMWSUZC.png")
        {
            Build = "6a8b8b3c79604aeea97f53c235947a1f",
            Ammo = "8dd66da5affa480ba324e270e52a46d7",
            FOBRadio = "fb910102ad954169abd4b0cb06a112c8",
            RallyPoint = "0d7895360c80440fbe4a45eba28b2007"
        },
        new FactionInfo("mec", "Middle Eastern Coalition", "MEC", "MEC", "ffcd8c", "meunarmed", @"https://i.imgur.com/rPmpNzz.png")
        {
            Build = "9c7122f7e70e4a4da26a49b871087f9f",
            Ammo = "bfc9aed75a3245acbfd01bc78fcfc875",
            FOBRadio = "c7754ac78083421da73006b12a56811a",
            RallyPoint = "c03352d9e6bb4e2993917924b604ee76"
        },
        new FactionInfo("germany", "Germany", "DE", "Germany", "ffcc00", "deunarmed"),
        new FactionInfo("china", "China", "CN", "China", "ef1620", "cnunarmed")
    };
    public static ushort Team1Tickets;
    public static ushort Team2Tickets;
    private static Zone? _t1main;
    private static Zone? _t1amc;
    private static Zone? _t2main;
    private static Zone? _t2amc;
    private static Zone? _lobbyZone;
    private static Color? _t1Clr;
    private static Color? _t2Clr;
    private static Color? _t3Clr;
    private static FactionInfo? _t1Faction;
    private static FactionInfo? _t2Faction;
    private static FactionInfo? _t3Faction;
    private static Vector3 _lobbySpawn = default;
    internal static readonly Dictionary<ulong, byte> PlayerBaseStatus = new Dictionary<ulong, byte>();
    public static event PlayerTeamDelegate OnPlayerEnteredMainBase;
    public static event PlayerTeamDelegate OnPlayerLeftMainBase;
    public const ulong Team1ID = 1;
    public const ulong Team2ID = 2;
    public const ulong AdminID = 3;
    public static TeamConfigData Config => _data.Data;
    public static string Team1Name => Team1Faction.Name;
    public static string Team2Name => Team2Faction.Name;
    public static string AdminName => AdminFaction.Name;
    public static string Team1Code => Team1Faction.Abbreviation;
    public static string Team2Code => Team2Faction.Abbreviation;
    public static string AdminCode => AdminFaction.Abbreviation;
    public static Color Team1Color
    {
        get
        {
            if (_t1Clr.HasValue)
                return _t1Clr.Value;
            _t1Clr = Team1ColorHex.Hex();
            return _t1Clr.Value;
        }
    }
    public static Color Team2Color
    {
        get
        {
            if (_t2Clr.HasValue)
                return _t2Clr.Value;
            _t2Clr = Team2ColorHex.Hex();
            return _t2Clr.Value;
        }
    }
    public static Color AdminColor
    {
        get
        {
            if (_t3Clr.HasValue)
                return _t3Clr.Value;
            _t3Clr = AdminColorHex.Hex();
            return _t3Clr.Value;
        }
    }
    public static Color NeutralColor => UCWarfare.GetColor("neutral");
    public static FactionInfo Team1Faction
    {
        get
        {
            if (_t1Faction is not null)
                return _t1Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionID.Equals(_data.Data.Team1FactionId.Value))
                {
                    _t1Faction = _factions[i];
                    return _t1Faction;
                }
            }

            throw new Exception("Team 1 Faction not selected.");
        }
    }
    public static FactionInfo Team2Faction
    {
        get
        {
            if (_t2Faction is not null)
                return _t2Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionID.Equals(_data.Data.Team2FactionId.Value))
                {
                    _t2Faction = _factions[i];
                    return _t2Faction;
                }
            }

            throw new Exception("Team 2 Faction not selected.");
        }
    }
    public static FactionInfo AdminFaction
    {
        get
        {
            if (_t3Faction is not null)
                return _t3Faction;
            for (int i = 0; i < _factions.Count; ++i)
            {
                if (_factions[i].FactionID.Equals(_data.Data.AdminFactionId.Value))
                {
                    _t3Faction = _factions[i];
                    return _t3Faction;
                }
            }

            throw new Exception("Admin Faction not selected.");
        }
    }
    public static string Team1ColorHex => Team1Faction.HexColor;
    public static string Team2ColorHex => Team2Faction.HexColor;
    public static string AdminColorHex => AdminFaction.HexColor;
    public static string NeutralColorHex
    {
        get
        {
            if (Data.Colors != default)
                return UCWarfare.GetColorHex("neutral_color");
            else return "ffffff";
        }
    }
    public static string Team1UnarmedKit => Team1Faction.UnarmedKit;
    public static string Team2UnarmedKit => Team1Faction.UnarmedKit;
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
    public static FactionInfo GetFaction(ulong team)
    {
        if (team == 1) return Team1Faction;
        if (team == 2) return Team2Faction;
        if (team == 3) return AdminFaction;
        throw new ArgumentOutOfRangeException(nameof(team));
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
            bool ft1 = false, ft2 = false, ft3 = false;
            foreach (KeyValuePair<CSteamID, GroupInfo> kv in val2.ToList())
            {
                if (kv.Key.m_SteamID == Team1ID)
                {
                    ft1 = true;
                    if (kv.Value.name != Team1Name)
                    {
                        L.Log("Renamed T1 group " + kv.Value.name + " to " + Team1Name, ConsoleColor.Magenta);
                        kv.Value.name = Team1Name;
                    }
                }
                else if (kv.Key.m_SteamID == Team2ID)
                {
                    ft2 = true;
                    if (kv.Value.name != Team2Name)
                    {
                        L.Log("Renamed T2 group " + kv.Value.name + " to " + Team2Name, ConsoleColor.Magenta);
                        kv.Value.name = Team2Name;
                    }
                }
                else if (kv.Key.m_SteamID == AdminID)
                {
                    ft3 = true;
                    if (kv.Value.name != AdminName)
                    {
                        L.Log("Renamed Admin group " + kv.Value.name + " to " + AdminName, ConsoleColor.Magenta);
                        kv.Value.name = AdminName;
                    }
                }
                else if (kv.Key.m_SteamID > AdminID || kv.Key.m_SteamID < Team1ID)
                    val2.Remove(kv.Key);
            }

            if (!ft1)
            {
                CSteamID gid = new CSteamID(Team1ID);
                val2.Add(gid, new GroupInfo(gid, Team1Name, 0));
                L.Log("Created group " + Team1ID + ": " + Team1Name + ".", ConsoleColor.Magenta);
            }
            if (!ft2)
            {
                CSteamID gid = new CSteamID(Team2ID);
                val2.Add(gid, new GroupInfo(gid, Team2Name, 0));
                L.Log("Created group " + Team2ID + ": " + Team2Name + ".", ConsoleColor.Magenta);
            }
            if (!ft3)
            {
                CSteamID gid = new CSteamID(AdminID);
                val2.Add(gid, new GroupInfo(gid, AdminName, 0));
                L.Log("Created group " + AdminID + ": " + AdminName + ".", ConsoleColor.Magenta);
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
    public static bool IsTeam1(this ulong ID) => ID == Team1ID;
    public static bool IsTeam2(this ulong ID) => ID == Team2ID;
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
    public static string TranslateName(ulong team, SteamPlayer player, bool colorize = false) => TranslateName(team, player.playerID.steamID.m_SteamID, colorize);
    public static string TranslateName(ulong team, Player player, bool colorize = false) => TranslateName(team, player.channel.owner.playerID.steamID.m_SteamID, colorize);
    public static string TranslateName(ulong team, CSteamID player, bool colorize = false) => TranslateName(team, player.m_SteamID, colorize);
    public static string TranslateName(ulong team, UCPlayer player, bool colorize = false) => TranslateName(team, player.Steam64, colorize);
    public static string TranslateName(ulong team, ulong player, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.Name;
        else if (team == 2) uncolorized = Team2Faction.Name;
        else if (team == 3) uncolorized = AdminFaction.Name;
        else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", player);
        else if (team == 0) uncolorized = Translation.Translate("neutral", player);
        else uncolorized = team.ToString(Data.Locale);
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateName(ulong team, string language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.Name;
        else if (team == 2) uncolorized = Team2Faction.Name;
        else if (team == 3) uncolorized = AdminFaction.Name;
        else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", language);
        else if (team == 0) uncolorized = Translation.Translate("neutral", language);
        else uncolorized = team.ToString(Data.Locale);
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateShortName(ulong team, ulong player, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.ShortName;
        else if (team == 2) uncolorized = Team2Faction.ShortName;
        else if (team == 3) uncolorized = AdminFaction.ShortName;
        else if (team == ZOMBIE_TEAM_ID) uncolorized = Translation.Translate("zombie", player);
        else if (team == 0) uncolorized = Translation.Translate("neutral", player);
        else uncolorized = team.ToString(Data.Locale);
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateShortName(ulong team, string language, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.ShortName;
        else if (team == 2) uncolorized = Team2Faction.ShortName;
        else if (team == 3) uncolorized = AdminFaction.ShortName;
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
    public static bool HasTeam(Player player)
    {
        ulong t = player.GetTeam();
        return t == 1 || t == 2;
    }
    public static bool IsFriendly(Player player, ulong groupID) => player.quests.groupID.m_SteamID == groupID;
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
    internal static void OnConfigReload()
    {
        _t1Clr = null;
        _t2Clr = null;
        _t3Clr = null;
        _t1Faction = null;
        _t2Faction = null;
        _t3Faction = null;
    }
    internal static void SetupConfig()
    {
        (_factions ??= new List<FactionInfo>(16)).Clear();

        DirectoryInfo dinfo = new DirectoryInfo(Data.Paths.FactionsStorage);
        if (!dinfo.Exists)
        {
            dinfo.Create();
            for (int i = 0; i < DefaultFactions.Length; ++i)
            {
                FactionInfo info = DefaultFactions[i];
                string path = Path.Combine(Data.Paths.FactionsStorage, info.FactionID + ".json");
                try
                {
                    using (FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        JsonSerializer.Serialize(str, info, JsonEx.serializerSettings);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Error writing default faction " + info.FactionID + ":");
                    L.LogError(ex);
                }
            }

            _factions.AddRange(DefaultFactions);
            goto tc;
        }
        foreach (FileInfo file in dinfo.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
        {
            string faction = Path.GetFileNameWithoutExtension(file.Name);

            for (int i = 0; i < _factions.Count; ++i)
                if (_factions[i].FactionID.Equals(faction, StringComparison.Ordinal))
                    goto cont;

            try
            {
                using (FileStream str = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FactionInfo? info = JsonSerializer.Deserialize<FactionInfo>(str, JsonEx.serializerSettings);
                    if (info != null)
                    {
                        _factions.Add(info);
                        L.Log("Registered faction: " + info.Name, ConsoleColor.Magenta);
                    }
                }
            }
            catch (Exception ex)
            {
                L.LogError("Error reading faction " + faction + ":");
                L.LogError(ex);
            }
        cont: continue;
        }
    tc:
        if (_data == null)
            _data = new TeamConfig();
        else
            _data.Reload();
    }
    internal static Guid CheckAssetRedirect(Guid input, ulong team)
    {
        if (team is < 1 or > 2) return input;
        if (input == RADIO_REDIRECT)
            GetFaction(team).FOBRadio.ValidReference(out input);
        else if (input == RALLY_POINT_REDIRECT)
            GetFaction(team).RallyPoint.ValidReference(out input);
        else if (input == BUILDING_SUPPLIES_REDIRECT)
            GetFaction(team).Build.ValidReference(out input);
        else if (input == AMMO_SUPPLIES_REDIRECT)
            GetFaction(team).Ammo.ValidReference(out input);
        else if (input == ZONE_BLOCKER_REDIRECT)
        {
            if (team == 1)
                Gamemode.Config.Barricades.Team1ZoneBlocker.ValidReference(out input);
            else if (team == 2)
                Gamemode.Config.Barricades.Team2ZoneBlocker.ValidReference(out input);
        }
        return input;
    }
    internal static Guid GetRedirectGuid(Guid input)
    {
        if (input == Guid.Empty) return input;

        FactionInfo faction1 = GetFaction(1);
        // radio
        if (faction1.FOBRadio.ValidReference(out Guid guid) && guid == input)
            return RADIO_REDIRECT;
        FactionInfo faction2 = GetFaction(2);
        if (faction2.FOBRadio.ValidReference(out guid) && guid == input)
            return RADIO_REDIRECT;

        // rally point supplies
        if (faction1.RallyPoint.ValidReference(out guid) && guid == input)
            return RALLY_POINT_REDIRECT;
        if (faction2.RallyPoint.ValidReference(out guid) && guid == input)
            return RALLY_POINT_REDIRECT;

        // building supplies
        if (faction1.Build.ValidReference(out guid) && guid == input)
            return BUILDING_SUPPLIES_REDIRECT;
        if (faction2.Build.ValidReference(out guid) && guid == input)
            return BUILDING_SUPPLIES_REDIRECT;

        // ammo supplies
        if (faction1.Ammo.ValidReference(out guid) && guid == input)
            return AMMO_SUPPLIES_REDIRECT;
        if (faction2.Ammo.ValidReference(out guid) && guid == input)
            return AMMO_SUPPLIES_REDIRECT;

        // zone blockers
        if (Gamemode.Config.Barricades.Team1ZoneBlocker.ValidReference(out guid) && guid == input)
            return ZONE_BLOCKER_REDIRECT;
        if (Gamemode.Config.Barricades.Team2ZoneBlocker.ValidReference(out guid) && guid == input)
            return ZONE_BLOCKER_REDIRECT;

        return input;
    }

    private static readonly Guid RADIO_REDIRECT                 = new Guid("dea738f0e4894bd4862fd0c850185a6d");
    private static readonly Guid RALLY_POINT_REDIRECT           = new Guid("60240b23b1604ffbbc1bb3771ea5081f");
    private static readonly Guid BUILDING_SUPPLIES_REDIRECT     = new Guid("96e27895c1b34e128121296c14dd9bf5");
    private static readonly Guid AMMO_SUPPLIES_REDIRECT         = new Guid("c4cee82e290b4b26b7a6e2be9cd70df7");
    private static readonly Guid ZONE_BLOCKER_REDIRECT          = new Guid("7959dc824a154035934049289e011a70");
}
public class FactionInfo
{
    public const string UNKNOWN_TEAM_IMG_URL = @"https://i.imgur.com/cs0cImN.png";

    public const string Admins = "admins";
    public const string USA = "usa";
    public const string Russia = "russia";
    public const string MEC = "mec";
    public const string Germany = "germany";
    public const string China = "china";

    [JsonPropertyName("factionId")]
    public string FactionID;
    [JsonPropertyName("displayName")]
    public string Name;
    [JsonPropertyName("shortName")]
    public string ShortName;
    [JsonPropertyName("abbreviation")]
    public string Abbreviation;
    [JsonPropertyName("color")]
    public string HexColor;
    [JsonPropertyName("unarmed")]
    public string UnarmedKit;
    [JsonPropertyName("flagImg")]
    public string FlagImageURL;
    [JsonPropertyName("ammoSupplies")]
    public JsonAssetReference<ItemAsset>? Ammo;
    [JsonPropertyName("buildingSupplies")]
    public JsonAssetReference<ItemAsset>? Build;
    [JsonPropertyName("rallyPoint")]
    public JsonAssetReference<ItemBarricadeAsset>? RallyPoint;
    [JsonPropertyName("radio")]
    public JsonAssetReference<ItemBarricadeAsset>? FOBRadio;
    public FactionInfo() { }
    public FactionInfo(string factionId, string name, string abbreviation, string shortName, string hexColor, string unarmedKit, string flagImage = UNKNOWN_TEAM_IMG_URL)
    {
        FactionID = factionId;
        Name = name;
        Abbreviation = abbreviation;
        ShortName = shortName;
        HexColor = hexColor;
        UnarmedKit = unarmedKit;
        FlagImageURL = flagImage;
    }
}

public class TeamConfig : Config<TeamConfigData>
{
    public TeamConfig() : base(Warfare.Data.Paths.BaseDirectory, "teams.json", "teams")
    {
    }
    protected override void OnReload()
    {
        TeamManager.OnConfigReload();
    }
}

public class TeamConfigData : ConfigData
{
    [JsonPropertyName("t1Faction")]
    public RotatableConfig<string> Team1FactionId;
    [JsonPropertyName("t2Faction")]
    public RotatableConfig<string> Team2FactionId;
    [JsonPropertyName("adminFaction")]
    public RotatableConfig<string> AdminFactionId;

    [JsonPropertyName("defaultkit")]
    public RotatableConfig<string> DefaultKit;
    [JsonPropertyName("team1spawnangle")]
    public RotatableConfig<float> Team1SpawnYaw;
    [JsonPropertyName("team2spawnangle")]
    public RotatableConfig<float> Team2SpawnYaw;
    [JsonPropertyName("lobbyspawnangle")]
    public RotatableConfig<float> LobbySpawnpointYaw;

    [JsonPropertyName("team_switch_cooldown")]
    public float TeamSwitchCooldown;
    [JsonPropertyName("allowedTeamGap")]
    public float AllowedDifferencePercent;
    [JsonPropertyName("balanceTeams")]
    public bool BalanceTeams;

    public TeamConfigData() { }
    public override void SetDefaults()
    {
        Team1FactionId = new RotatableConfig<string>(FactionInfo.USA, new RotatableDefaults<string>
        {
            { MapScheduler.FoolsRoad,   FactionInfo.USA },
            { MapScheduler.Nuijamaa,    FactionInfo.USA },
            { MapScheduler.GooseBay,    FactionInfo.USA },
            { MapScheduler.GulfOfAqaba, FactionInfo.USA },
            { MapScheduler.S3Map,       FactionInfo.Germany },
        });
        Team2FactionId = new RotatableConfig<string>(FactionInfo.Russia, new RotatableDefaults<string>
        {
            { MapScheduler.FoolsRoad,   FactionInfo.Russia },
            { MapScheduler.Nuijamaa,    FactionInfo.Russia },
            { MapScheduler.GooseBay,    FactionInfo.Russia },
            { MapScheduler.GulfOfAqaba, FactionInfo.MEC },
            { MapScheduler.S3Map,       FactionInfo.China },
        });
        AdminFactionId = FactionInfo.Admins;
        DefaultKit = "default";
        Team1SpawnYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,   180f },
            { MapScheduler.Nuijamaa,    0f },
            { MapScheduler.GooseBay,    0f },
            { MapScheduler.GulfOfAqaba, 90f },
            { MapScheduler.S3Map,       0f },
        });
        Team2SpawnYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,   180f },
            { MapScheduler.Nuijamaa,    0f },
            { MapScheduler.GooseBay,    0f },
            { MapScheduler.GulfOfAqaba, 90f },
            { MapScheduler.S3Map,       0f },
        });
        LobbySpawnpointYaw = new RotatableConfig<float>(0f, new RotatableDefaults<float>
        {
            { MapScheduler.FoolsRoad,   0f },
            { MapScheduler.Nuijamaa,    0f },
            { MapScheduler.GooseBay,    0f },
            { MapScheduler.GulfOfAqaba, 90f },
            { MapScheduler.S3Map,       0f },
        });
        TeamSwitchCooldown = 1200;
        BalanceTeams = true;
    }
}
