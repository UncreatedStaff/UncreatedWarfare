﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Teams;

public delegate void PlayerTeamDelegate(SteamPlayer player, ulong team);
public static class TeamManager
{
    private static TeamConfig _data;
    private static List<FactionInfo> _factions;
    public const ulong ZOMBIE_TEAM_ID = ulong.MaxValue;
    internal static readonly FactionInfo[] DefaultFactions =
    {
        new FactionInfo("admins", "Admins", "ADMIN", "Admins", "0099ff", "default")
        {
            NameTranslations = new Dictionary<string, string>(4)
            {
                { LanguageAliasSet.RUSSIAN, "Администрация" }
            }
        },
        new FactionInfo("usa", "United States", "USA", "USA", "78b2ff", "usunarmed", @"https://i.imgur.com/P4JgkHB.png")
        {
            Build = "a70978a0b47e4017a0261e676af57042",
            Ammo = "51e1e372bf5341e1b4b16a0eacce37eb",
            FOBRadio = "7715ad81f1e24f60bb8f196dd09bd4ef",
            RallyPoint = "5e1db525179341d3b0c7576876212a81",
            NameTranslations = new Dictionary<string, string>(4)
            {
                { LanguageAliasSet.RUSSIAN, "США" }
            },
            AbbreviationTranslations = new Dictionary<string, string>(4)
            {
                { LanguageAliasSet.RUSSIAN, "США" }
            }
        },
        new FactionInfo("russia", "Russia", "RU", "Russia", "f53b3b", "ruunarmed", @"https://i.imgur.com/YMWSUZC.png")
        {
            Build = "6a8b8b3c79604aeea97f53c235947a1f",
            Ammo = "8dd66da5affa480ba324e270e52a46d7",
            FOBRadio = "fb910102ad954169abd4b0cb06a112c8",
            RallyPoint = "0d7895360c80440fbe4a45eba28b2007",
            NameTranslations = new Dictionary<string, string>(4)
            {
                { LanguageAliasSet.RUSSIAN, "РОССИЯ" }
            },
            AbbreviationTranslations = new Dictionary<string, string>(4)
            {
                { LanguageAliasSet.RUSSIAN, "РФ" }
            }
        },
        new FactionInfo("mec", "Middle Eastern Coalition", "MEC", "MEC", "ffcd8c", "meunarmed", @"https://i.imgur.com/rPmpNzz.png")
        {
            Build = "9c7122f7e70e4a4da26a49b871087f9f",
            Ammo = "bfc9aed75a3245acbfd01bc78fcfc875",
            FOBRadio = "c7754ac78083421da73006b12a56811a",
            RallyPoint = "c03352d9e6bb4e2993917924b604ee76"
        },
        // don't even think about leaking these
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
                if (_factions[i].FactionId.Equals(_data.Data.Team1FactionId.Value))
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
                if (_factions[i].FactionId.Equals(_data.Data.Team2FactionId.Value))
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
                if (_factions[i].FactionId.Equals(_data.Data.AdminFactionId.Value))
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
    public static FactionInfo? GetFactionSafe(ulong team)
    {
        if (team == 1) return Team1Faction;
        if (team == 2) return Team2Faction;
        if (team == 3) return AdminFaction;
        return null;
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
    internal static void SaveConfig() => _data.Save();
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
        else if (team == 0) uncolorized = T.Neutral.Translate(player);
        else uncolorized = team.ToString(Data.Locale);
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateName(ulong team, IPlayer player, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.Name;
        else if (team == 2) uncolorized = Team2Faction.Name;
        else if (team == 3) uncolorized = AdminFaction.Name;
        else if (team == 0) uncolorized = T.Neutral.Translate(player);
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
        else if (team == 0) uncolorized = T.Neutral.Translate(language);
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
        else if (team == 0) uncolorized = T.Neutral.Translate(player);
        else uncolorized = team.ToString(Data.Locale);
        if (!colorize) return uncolorized;
        return F.ColorizeName(uncolorized, team);
    }
    public static string TranslateShortName(ulong team, IPlayer player, bool colorize = false)
    {
        string uncolorized;
        if (team == 1) uncolorized = Team1Faction.ShortName;
        else if (team == 2) uncolorized = Team2Faction.ShortName;
        else if (team == 3) uncolorized = AdminFaction.ShortName;
        else if (team == 0) uncolorized = T.Neutral.Translate(player);
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
        else if (team == 0) uncolorized = T.Neutral.Translate(language);
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
                string path = Path.Combine(Data.Paths.FactionsStorage, info.FactionId + ".json");
                try
                {
                    using (FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        JsonSerializer.Serialize(str, info, JsonEx.serializerSettings);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Error writing default faction " + info.FactionId + ":");
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
                if (_factions[i].FactionId.Equals(faction, StringComparison.Ordinal))
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
    internal static Guid CheckClothingAssetRedirect(Guid input, ulong team)
    {
        if (team is not 1 and not 2) return input;
        if (input == BACKPACK_REDIRECT)
            GetFaction(team).DefaultBackpack.ValidReference(out input);
        else if (input == SHIRT_REDIRECT)
            GetFaction(team).DefaultShirt.ValidReference(out input);
        else if (input == PANTS_REDIRECT)
            GetFaction(team).DefaultPants.ValidReference(out input);
        else if (input == VEST_REDIRECT)
            GetFaction(team).DefaultVest.ValidReference(out input);

        return input;
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
                Gamemode.Config.BarricadeZoneBlockerTeam1.ValidReference(out input);
            else if (team == 2)
                Gamemode.Config.BarricadeZoneBlockerTeam2.ValidReference(out input);
        }
        return input;
    }
    internal static Guid GetClothingRedirectGuid(Guid input)
    {
        if (input == Guid.Empty) return input;

        // backpack
        FactionInfo faction1 = GetFaction(1);
        if (faction1.DefaultBackpack.ValidReference(out Guid guid) && guid == input)
            return BACKPACK_REDIRECT;
        FactionInfo faction2 = GetFaction(2);
        if (faction2.DefaultBackpack.ValidReference(out guid) && guid == input)
            return BACKPACK_REDIRECT;

        // shirt
        if (faction1.DefaultShirt.ValidReference(out guid) && guid == input || faction2.DefaultShirt.ValidReference(out guid) && guid == input)
            return SHIRT_REDIRECT;

        // pants
        if (faction1.DefaultPants.ValidReference(out guid) && guid == input || faction2.DefaultPants.ValidReference(out guid) && guid == input)
            return PANTS_REDIRECT;

        // vest
        if (faction1.DefaultVest.ValidReference(out guid) && guid == input || faction2.DefaultVest.ValidReference(out guid) && guid == input)
            return VEST_REDIRECT;

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
        if (faction1.RallyPoint.ValidReference(out guid) && guid == input || faction2.RallyPoint.ValidReference(out guid) && guid == input)
            return RALLY_POINT_REDIRECT;

        // building supplies
        if (faction1.Build.ValidReference(out guid) && guid == input || faction2.Build.ValidReference(out guid) && guid == input)
            return BUILDING_SUPPLIES_REDIRECT;

        // ammo supplies
        if (faction1.Ammo.ValidReference(out guid) && guid == input || faction2.Ammo.ValidReference(out guid) && guid == input)
            return AMMO_SUPPLIES_REDIRECT;

        // zone blockers
        if (Gamemode.Config.BarricadeZoneBlockerTeam1.ValidReference(out guid) && guid == input || Gamemode.Config.BarricadeZoneBlockerTeam2.ValidReference(out guid) && guid == input)
            return ZONE_BLOCKER_REDIRECT;

        return input;
    }

    // items
    private static readonly Guid RADIO_REDIRECT = new Guid("dea738f0e4894bd4862fd0c850185a6d");
    private static readonly Guid RALLY_POINT_REDIRECT = new Guid("60240b23b1604ffbbc1bb3771ea5081f");
    private static readonly Guid BUILDING_SUPPLIES_REDIRECT = new Guid("96e27895c1b34e128121296c14dd9bf5");
    private static readonly Guid AMMO_SUPPLIES_REDIRECT = new Guid("c4cee82e290b4b26b7a6e2be9cd70df7");
    private static readonly Guid ZONE_BLOCKER_REDIRECT = new Guid("7959dc824a154035934049289e011a70");

    // clothes
    private static readonly Guid BACKPACK_REDIRECT = new Guid("bfc294a392294438b29194abfa9792f9");
    private static readonly Guid SHIRT_REDIRECT = new Guid("bc84a3c778884f38a4804da8ab1ca925");
    private static readonly Guid PANTS_REDIRECT = new Guid("dacac5a5628a44d7b40b16f14be681f4");
    private static readonly Guid VEST_REDIRECT = new Guid("2b22ac1b5de74755a24c2f05219c5e1f");

    public static Task ReloadFactions()
    {
        _factions ??= new List<FactionInfo>(DefaultFactions.Length);
        return FactionInfo.DownloadFactions(Data.AdminSql, _factions, CancellationToken.None);
    }
}
public class FactionInfo : ITranslationArgument, IListItem, ICloneable
{
    public const string UNKNOWN_TEAM_IMG_URL = @"https://i.imgur.com/cs0cImN.png";
    public const int FACTION_ID_MAX_CHAR_LIMIT = 16;
    public const int FACTION_NAME_MAX_CHAR_LIMIT = 32;
    public const int FACTION_SHORT_NAME_MAX_CHAR_LIMIT = 24;
    public const int FACTION_ABBREVIATION_MAX_CHAR_LIMIT = 6;
    public const int FACTION_IMAGE_LINK_MAX_CHAR_LIMIT = 128;

    public const string Admins = "admins";
    public const string USA = "usa";
    public const string Russia = "russia";
    public const string MEC = "mec";
    public const string Germany = "germany";
    public const string China = "china";
    [JsonIgnore]
    private string _factionId;
    [JsonPropertyName("displayName")]
    public string Name;
    [JsonPropertyName("shortName")]
    public string ShortName;
    [JsonPropertyName("nameLocalization")]
    public Dictionary<string, string>? NameTranslations;
    [JsonPropertyName("shortNameLocalization")]
    public Dictionary<string, string>? ShortNameTranslations;
    [JsonPropertyName("abbreviationLocalization")]
    public Dictionary<string, string>? AbbreviationTranslations;
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
    [JsonPropertyName("defaultBackpack")]
    public JsonAssetReference<ItemBackpackAsset>? DefaultBackpack;
    [JsonPropertyName("defaultShirt")]
    public JsonAssetReference<ItemShirtAsset>? DefaultShirt;
    [JsonPropertyName("defaultPants")]
    public JsonAssetReference<ItemPantsAsset>? DefaultPants;
    [JsonPropertyName("defaultVest")]
    public JsonAssetReference<ItemVestAsset>? DefaultVest;
    [JsonIgnore]
    public PrimaryKey PrimaryKey { get; set; }
    [JsonPropertyName("factionId")]
    public string FactionId
    {
        get => _factionId;
        set
        {
            if (value.Length > FACTION_ID_MAX_CHAR_LIMIT)
                throw new ArgumentException("Faction ID must be less than " + FACTION_ID_MAX_CHAR_LIMIT + " characters.", "factionId");
            _factionId = value;
        }
    }

    public FactionInfo() { }
    public FactionInfo(string factionId, string name, string abbreviation, string shortName, string hexColor, string unarmedKit, string flagImage = UNKNOWN_TEAM_IMG_URL)
    {
        FactionId = factionId;
        Name = name;
        Abbreviation = abbreviation;
        ShortName = shortName;
        HexColor = hexColor;
        UnarmedKit = unarmedKit;
        FlagImageURL = flagImage;
    }

    [FormatDisplay("ID")]
    public const string ID_FORMAT = "i";
    [FormatDisplay("Colored ID")]
    public const string COLOR_ID_FORMAT = "ic";
    [FormatDisplay("Short Name")]
    public const string SHORT_NAME_FORMAT = "s";
    [FormatDisplay("Display Name")]
    public const string DISPLAY_NAME_FORMAT = "d";
    [FormatDisplay("Abbreviation")]
    public const string ABBREVIATION_FORMAT = "a";
    [FormatDisplay("Colored Short Name")]
    public const string COLOR_SHORT_NAME_FORMAT = "sc";
    [FormatDisplay("Colored Display Name")]
    public const string COLOR_DISPLAY_NAME_FORMAT = "dc";
    [FormatDisplay("Colored Abbreviation")]
    public const string COLOR_ABBREVIATION_FORMAT = "ac";

    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(COLOR_DISPLAY_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetName(language), flags);
            else if (format.Equals(SHORT_NAME_FORMAT, StringComparison.Ordinal))
                return GetShortName(language);
            else if (format.Equals(COLOR_SHORT_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetShortName(language), flags);
            else if (format.Equals(ABBREVIATION_FORMAT, StringComparison.Ordinal))
                return GetAbbreviation(language);
            else if (format.Equals(COLOR_ABBREVIATION_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(HexColor, GetAbbreviation(language), flags);
            else if (format.Equals(ID_FORMAT, StringComparison.Ordinal) ||
                     format.Equals(COLOR_ID_FORMAT, StringComparison.Ordinal))
            {
                ulong team = 0;
                if (TeamManager.Team1Faction == this)
                    team = 1;
                else if (TeamManager.Team2Faction == this)
                    team = 2;
                else if (TeamManager.AdminFaction == this)
                    team = 3;
                if (format.Equals(ID_FORMAT, StringComparison.Ordinal))
                    return team.ToString(Data.Locale);

                return Localization.Colorize(HexColor, team.ToString(Data.Locale), flags);
            }
        }
        return GetName(language);
    }
    public string GetName(string? language)
    {
        if (language is null || language.Equals(L.DEFAULT, StringComparison.OrdinalIgnoreCase) || NameTranslations is null || !NameTranslations.TryGetValue(language, out string val))
            return Name;
        return val;
    }
    public string GetShortName(string? language)
    {
        if (language is null || language.Equals(L.DEFAULT, StringComparison.OrdinalIgnoreCase))
            return ShortName ?? Name;
        if (ShortNameTranslations is null || !ShortNameTranslations.TryGetValue(language, out string val))
        {
            if (NameTranslations is null || !NameTranslations.TryGetValue(language, out val))
                return ShortName ?? Name;
        }
        return val;
    }
    public string GetAbbreviation(string? language)
    {
        if (language is null || language.Equals(L.DEFAULT, StringComparison.OrdinalIgnoreCase) || AbbreviationTranslations is null || !AbbreviationTranslations.TryGetValue(language, out string val))
            return Abbreviation;
        return val;
    }

    public const string TABLE_MAIN = "factions";
    public const string TABLE_MAP_ASSETS = "faction_assets";
    public const string TABLE_NAME_TRANSLATIONS = "faction_name_translations";
    public const string TABLE_SHORT_NAME_TRANSLATIONS = "faction_short_name_translations";
    public const string TABLE_ABBREVIATIONS_TRANSLATIONS = "faction_abbreviation_translations";
    public const string COLUMN_PK = "pk";
    public const string COLUMN_ID = "Id";
    public const string COLUMN_NAME = "Name";
    public const string COLUMN_SHORT_NAME = "ShortName";
    public const string COLUMN_ABBREVIATION = "Abbreviation";
    public const string COLUMN_HEX_COLOR = "HexColor";
    public const string COLUMN_UNARMED_KIT = "UnarmedKit";
    public const string COLUMN_FLAG_IMAGE_URL = "FlagImageUrl";
    public const string COLUMN_EXT_PK = "Faction";
    public const string COLUMN_ASSETS_SUPPLY_AMMO = "AmmoSupply";
    public const string COLUMN_ASSETS_SUPPLY_BUILD = "BuildSupply";
    public const string COLUMN_ASSETS_RALLY_POINT = "RallyPoint";
    public const string COLUMN_ASSETS_FOB_RADIO = "Radio";
    public const string COLUMN_ASSETS_DEFAULT_BACKPACK = "DefaultBackpack";
    public const string COLUMN_ASSETS_DEFAULT_SHIRT = "DefaultShirt";
    public const string COLUMN_ASSETS_DEFAULT_PANTS = "DefaultPants";
    public const string COLUMN_ASSETS_DEFAULT_VEST = "DefaultVest";
    private const string EMPTY_GUID = "00000000000000000000000000000000";
    public static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_ID, "varchar(" + FACTION_ID_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")"),
            new Schema.Column(COLUMN_NAME, "varchar(" + FACTION_NAME_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")"),
            new Schema.Column(COLUMN_SHORT_NAME, "varchar(" + FACTION_SHORT_NAME_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")"),
            new Schema.Column(COLUMN_ABBREVIATION, "varchar(" + FACTION_ABBREVIATION_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")")
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_HEX_COLOR, "char(6)")
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_UNARMED_KIT, "varchar(" + KitEx.KIT_NAME_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")"),
            new Schema.Column(COLUMN_FLAG_IMAGE_URL, "varchar(" + FACTION_IMAGE_LINK_MAX_CHAR_LIMIT.ToString(CultureInfo.InvariantCulture) + ")")
            {
                Nullable = true
            }
        }, true, typeof(FactionInfo)),
        new Schema(TABLE_MAP_ASSETS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                ForeignKey = true,
                AutoIncrement = true,
                ForeignKeyTable = TABLE_MAIN,
                ForeignKeyColumn = COLUMN_PK
            },
            new Schema.Column(COLUMN_ASSETS_SUPPLY_AMMO, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_SUPPLY_BUILD, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_RALLY_POINT, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_FOB_RADIO, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_DEFAULT_BACKPACK, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_DEFAULT_SHIRT, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_DEFAULT_PANTS, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            },
            new Schema.Column(COLUMN_ASSETS_DEFAULT_VEST, SqlTypes.GUID_STRING)
            {
                Default = EMPTY_GUID,
                Nullable = true
            }
        }, false, typeof(FactionInfo)),
        F.GetTranslationListSchema(TABLE_NAME_TRANSLATIONS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, FACTION_NAME_MAX_CHAR_LIMIT),
        F.GetTranslationListSchema(TABLE_SHORT_NAME_TRANSLATIONS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, FACTION_SHORT_NAME_MAX_CHAR_LIMIT),
        F.GetTranslationListSchema(TABLE_ABBREVIATIONS_TRANSLATIONS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, FACTION_ABBREVIATION_MAX_CHAR_LIMIT)
    };

    private static async Task AddDefaults(MySqlDatabase sql, CancellationToken token = default)
    {
        StringBuilder builder = new StringBuilder($"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_PK}`,`{COLUMN_ID}`,`{COLUMN_NAME}`,`{COLUMN_SHORT_NAME}`,`{COLUMN_ABBREVIATION}`," +
                                                  $"`{COLUMN_HEX_COLOR}`,`{COLUMN_UNARMED_KIT}`,`{COLUMN_FLAG_IMAGE_URL}`) VALUES ", 256);
        object[] objs = new object[TeamManager.DefaultFactions.Length * 8];
        for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
        {
            FactionInfo def = TeamManager.DefaultFactions[i];
            def.PrimaryKey = i + 1;
            if (i != 0)
                builder.Append(',');
            builder.Append('(');
            int st = i * 8;
            for (int j = 0; j < 8; ++j)
            {
                if (j != 0)
                    builder.Append(',');
                builder.Append('@').Append(st + j);
            }
            builder.Append(')');
            objs[st] = def.PrimaryKey.Key;
            objs[st + 1] = def.FactionId;
            objs[st + 2] = def.Name;
            objs[st + 3] = def.ShortName;
            objs[st + 4] = def.Abbreviation;
            objs[st + 5] = def.HexColor;
            objs[st + 6] = def.UnarmedKit;
            objs[st + 7] = def.FlagImageURL;
        }

        builder.Append(';');
        await sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
        builder.Clear();
        builder.Append($"INSERT INTO `{TABLE_MAP_ASSETS}` (`{COLUMN_EXT_PK}`,`{COLUMN_ASSETS_SUPPLY_AMMO}`,`{COLUMN_ASSETS_SUPPLY_BUILD}`,`{COLUMN_ASSETS_RALLY_POINT}`," +
                       $"`{COLUMN_ASSETS_FOB_RADIO}`,`{COLUMN_ASSETS_DEFAULT_BACKPACK}`,`{COLUMN_ASSETS_DEFAULT_SHIRT}`," +
                       $"`{COLUMN_ASSETS_DEFAULT_PANTS}`,`{COLUMN_ASSETS_DEFAULT_VEST}`) VALUES ");
        objs = new object[TeamManager.DefaultFactions.Length * 9];
        for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
        {
            FactionInfo def = TeamManager.DefaultFactions[i];
            if (i != 0)
                builder.Append(',');
            builder.Append('(');
            int st = i * 9;
            for (int j = 0; j < 9; ++j)
            {
                if (j != 0)
                    builder.Append(',');
                builder.Append('@').Append(st + j);
            }
            builder.Append(')');
            objs[st] = def.PrimaryKey.Key;
            if (def.Ammo is null) objs[st + 1] = DBNull.Value;
            else objs[st + 1] = def.Ammo.Guid.ToString("N");

            if (def.Build is null) objs[st + 2] = DBNull.Value;
            else objs[st + 2] = def.Build.Guid.ToString("N");

            if (def.RallyPoint is null) objs[st + 3] = DBNull.Value;
            else objs[st + 3] = def.RallyPoint.Guid.ToString("N");

            if (def.FOBRadio is null) objs[st + 4] = DBNull.Value;
            else objs[st + 4] = def.FOBRadio.Guid.ToString("N");

            if (def.DefaultBackpack is null) objs[st + 5] = DBNull.Value;
            else objs[st + 5] = def.DefaultBackpack.Guid.ToString("N");

            if (def.DefaultShirt is null) objs[st + 6] = DBNull.Value;
            else objs[st + 6] = def.DefaultShirt.Guid.ToString("N");

            if (def.DefaultPants is null) objs[st + 7] = DBNull.Value;
            else objs[st + 7] = def.DefaultPants.Guid.ToString("N");

            if (def.DefaultVest is null) objs[st + 8] = DBNull.Value;
            else objs[st + 8] = def.DefaultVest.Guid.ToString("N");
        }

        builder.Append(';');

        await sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
        builder.Clear();
        builder.Append($"INSERT INTO `{TABLE_NAME_TRANSLATIONS}` (`{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}`) VALUES ");
        List<object> objs2 = new List<object>(TeamManager.DefaultFactions.Length * 3);
        bool f = false;
        for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
        {
            FactionInfo def = TeamManager.DefaultFactions[i];
            if (def.NameTranslations == null)
                continue;
            foreach (KeyValuePair<string, string> v in def.NameTranslations)
            {
                if (f)
                    builder.Append(',');
                else
                    f = true;
                int c = objs2.Count;
                builder.Append("(@" + c.ToString(Data.AdminLocale) + ",@" + (c + 1).ToString(Data.AdminLocale) + ",@" + (c + 2).ToString(Data.AdminLocale) + ")");
                objs2.Add(def.PrimaryKey.Key);
                objs2.Add(v.Key);
                objs2.Add(v.Value);
            }
        }
        if (objs2.Count != 0)
        {
            builder.Append(';');
            await sql.NonQueryAsync(builder.ToString(), objs2.ToArray(), token).ConfigureAwait(false);
            objs2.Clear();
        }

        builder.Clear();
        builder.Append($"INSERT INTO `{TABLE_SHORT_NAME_TRANSLATIONS}` (`{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}`) VALUES ");
        f = false;
        for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
        {
            FactionInfo def = TeamManager.DefaultFactions[i];
            if (def.ShortNameTranslations == null)
                continue;
            foreach (KeyValuePair<string, string> v in def.ShortNameTranslations)
            {
                if (f)
                    builder.Append(',');
                else
                    f = true;
                int c = objs2.Count;
                builder.Append("(@" + c.ToString(Data.AdminLocale) + ",@" + (c + 1).ToString(Data.AdminLocale) + ",@" + (c + 2).ToString(Data.AdminLocale) + ")");
                objs2.Add(def.PrimaryKey.Key);
                objs2.Add(v.Key);
                objs2.Add(v.Value);
            }
        }

        if (objs2.Count != 0)
        {
            builder.Append(';');
            await sql.NonQueryAsync(builder.ToString(), objs2.ToArray(), token).ConfigureAwait(false);
            objs2.Clear();
        }
        builder.Clear();
        builder.Append($"INSERT INTO `{TABLE_ABBREVIATIONS_TRANSLATIONS}` (`{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}`) VALUES ");
        f = false;
        for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
        {
            FactionInfo def = TeamManager.DefaultFactions[i];
            if (def.AbbreviationTranslations == null)
                continue;
            foreach (KeyValuePair<string, string> v in def.AbbreviationTranslations)
            {
                if (f)
                    builder.Append(',');
                else
                    f = true;
                int c = objs2.Count;
                builder.Append("(@" + c.ToString(Data.AdminLocale) + ",@" + (c + 1).ToString(Data.AdminLocale) + ",@" + (c + 2).ToString(Data.AdminLocale) + ")");
                objs2.Add(def.PrimaryKey.Key);
                objs2.Add(v.Key);
                objs2.Add(v.Value);
            }
        }

        if (objs2.Count == 0)
            return;

        builder.Append(';');
        await sql.NonQueryAsync(builder.ToString(), objs2.ToArray(), token).ConfigureAwait(false);
    }
    internal static async Task DownloadFactions(MySqlDatabase sql, List<FactionInfo> list, CancellationToken token = default)
    {
        int[] vals = await sql.VerifyTables(SCHEMAS, token).ConfigureAwait(false);
        if (vals[0] == 3)
        {
            await AddDefaults(sql, token).ConfigureAwait(false);
            for (int i = 0; i < TeamManager.DefaultFactions.Length; ++i)
            {
                int pk = TeamManager.DefaultFactions[i].PrimaryKey.Key;
                FactionInfo def = TeamManager.DefaultFactions[i];
                bool found = false;
                for (int j = 0; j < list.Count; ++j)
                {
                    if (list[j].PrimaryKey == pk)
                    {
                        FactionInfo faction = list[j];
                        faction.FactionId = def.FactionId;
                        faction.Name = def.Name;
                        faction.ShortName = def.ShortName;
                        faction.Abbreviation = def.Abbreviation;
                        faction.HexColor = def.HexColor;
                        faction.UnarmedKit = def.UnarmedKit;
                        faction.FlagImageURL = def.FlagImageURL;
                        faction.Ammo = def.Ammo?.Clone() as JsonAssetReference<ItemAsset>;
                        faction.Build = def.Build?.Clone() as JsonAssetReference<ItemAsset>;
                        faction.RallyPoint = def.RallyPoint?.Clone() as JsonAssetReference<ItemBarricadeAsset>;
                        faction.FOBRadio = def.FOBRadio?.Clone() as JsonAssetReference<ItemBarricadeAsset>;
                        faction.DefaultBackpack = def.DefaultBackpack?.Clone() as JsonAssetReference<ItemBackpackAsset>;
                        faction.DefaultShirt = def.DefaultShirt?.Clone() as JsonAssetReference<ItemShirtAsset>;
                        faction.DefaultPants = def.DefaultPants?.Clone() as JsonAssetReference<ItemPantsAsset>;
                        faction.DefaultVest = def.DefaultVest?.Clone() as JsonAssetReference<ItemVestAsset>;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    list.Add((FactionInfo)def.Clone());
                }
            }
        }
        await sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_ID}`,`{COLUMN_NAME}`," +
                             $"`{COLUMN_SHORT_NAME}`,`{COLUMN_ABBREVIATION}`,`{COLUMN_HEX_COLOR}`,`{COLUMN_UNARMED_KIT}`," +
                             $"`{COLUMN_FLAG_IMAGE_URL}` FROM `{TABLE_MAIN}`;", null,
            reader =>
            {
                int pk = reader.GetInt32(0);
                string name = reader.GetString(2);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].PrimaryKey.Key == pk)
                    {
                        FactionInfo faction = list[i];
                        faction.FactionId = reader.GetString(1);
                        faction.Name = name;
                        faction.ShortName = reader.IsDBNull(3) ? name : reader.GetString(3);
                        faction.Abbreviation = reader.GetString(4);
                        faction.HexColor = reader.IsDBNull(5) ? UCWarfare.GetColorHex("default") : reader.GetString(5);
                        faction.UnarmedKit = reader.GetString(6);
                        faction.FlagImageURL = reader.IsDBNull(7) ? UNKNOWN_TEAM_IMG_URL : reader.GetString(7);
                        return;
                    }
                }
                list.Add(
                    new FactionInfo(
                        reader.GetString(1),
                        name,
                        reader.GetString(4),
                        reader.IsDBNull(3) ? name : reader.GetString(3),
                        reader.IsDBNull(5) ? UCWarfare.GetColorHex("default") : reader.GetString(5),
                        reader.GetString(6),
                        reader.IsDBNull(7) ? UNKNOWN_TEAM_IMG_URL : reader.GetString(7))
                    {
                        PrimaryKey = reader.GetInt32(0)
                    });
        }, token).ConfigureAwait(false);
        await sql.QueryAsync(
            $"SELECT `{COLUMN_EXT_PK}`,`{COLUMN_ASSETS_SUPPLY_AMMO}`,`{COLUMN_ASSETS_SUPPLY_BUILD}`," +
            $"`{COLUMN_ASSETS_RALLY_POINT}`,`{COLUMN_ASSETS_FOB_RADIO}`,`{COLUMN_ASSETS_DEFAULT_BACKPACK}`," +
            $"`{COLUMN_ASSETS_DEFAULT_SHIRT}`,`{COLUMN_ASSETS_DEFAULT_PANTS}`,`{COLUMN_ASSETS_DEFAULT_VEST}` FROM `{TABLE_MAP_ASSETS}`;", null,
            reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].PrimaryKey.Key == pk)
                    {
                        FactionInfo faction = list[i];
                        if (!reader.IsDBNull(1))
                        {
                            Guid? guid = reader.ReadGuidString(1);
                            if (guid.HasValue)
                                faction.Ammo = new JsonAssetReference<ItemAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            Guid? guid = reader.ReadGuidString(2);
                            if (guid.HasValue)
                                faction.Build = new JsonAssetReference<ItemAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(3))
                        {
                            Guid? guid = reader.ReadGuidString(3);
                            if (guid.HasValue)
                                faction.RallyPoint = new JsonAssetReference<ItemBarricadeAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(4))
                        {
                            Guid? guid = reader.ReadGuidString(4);
                            if (guid.HasValue)
                                faction.FOBRadio = new JsonAssetReference<ItemBarricadeAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(5))
                        {
                            Guid? guid = reader.ReadGuidString(5);
                            if (guid.HasValue)
                                faction.DefaultBackpack = new JsonAssetReference<ItemBackpackAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(6))
                        {
                            Guid? guid = reader.ReadGuidString(6);
                            if (guid.HasValue)
                                faction.DefaultShirt = new JsonAssetReference<ItemShirtAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(7))
                        {
                            Guid? guid = reader.ReadGuidString(7);
                            if (guid.HasValue)
                                faction.DefaultPants = new JsonAssetReference<ItemPantsAsset>(guid.Value);
                        }
                        if (!reader.IsDBNull(8))
                        {
                            Guid? guid = reader.ReadGuidString(8);
                            if (guid.HasValue)
                                faction.DefaultVest = new JsonAssetReference<ItemVestAsset>(guid.Value);
                        }
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        await sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}` FROM `{TABLE_NAME_TRANSLATIONS}`;", null,
            reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].PrimaryKey.Key == pk)
                    {
                        string lang = reader.GetString(1);
                        FactionInfo faction = list[i];
                        if (faction.NameTranslations == null)
                            faction.NameTranslations = new Dictionary<string, string>(1);
                        else if (faction.NameTranslations.ContainsKey(lang))
                            break;
                        faction.NameTranslations.Add(lang, reader.GetString(2));
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        await sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}` FROM `{TABLE_SHORT_NAME_TRANSLATIONS}`;", null,
            reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].PrimaryKey.Key == pk)
                    {
                        string lang = reader.GetString(1);
                        FactionInfo faction = list[i];
                        if (faction.ShortNameTranslations == null)
                            faction.ShortNameTranslations = new Dictionary<string, string>(1);
                        else if (faction.ShortNameTranslations.ContainsKey(lang))
                            break;
                        faction.ShortNameTranslations.Add(lang, reader.GetString(2));
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        await sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{F.COLUMN_LANGUAGE}`,`{F.COLUMN_VALUE}` FROM `{TABLE_ABBREVIATIONS_TRANSLATIONS}`;", null,
            reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i].PrimaryKey.Key == pk)
                    {
                        string lang = reader.GetString(1);
                        FactionInfo faction = list[i];
                        if (faction.AbbreviationTranslations == null)
                            faction.AbbreviationTranslations = new Dictionary<string, string>(1);
                        else if (faction.AbbreviationTranslations.ContainsKey(lang))
                            break;
                        faction.AbbreviationTranslations.Add(lang, reader.GetString(2));
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
    }

    public object Clone()
    {
        return new FactionInfo(FactionId, Name, Abbreviation, ShortName, HexColor, UnarmedKit, FlagImageURL)
        {
            PrimaryKey = PrimaryKey,
            Ammo = Ammo?.Clone() as JsonAssetReference<ItemAsset>,
            Build = Build?.Clone() as JsonAssetReference<ItemAsset>,
            RallyPoint = RallyPoint?.Clone() as JsonAssetReference<ItemBarricadeAsset>,
            FOBRadio = FOBRadio?.Clone() as JsonAssetReference<ItemBarricadeAsset>,
            DefaultBackpack = DefaultBackpack?.Clone() as JsonAssetReference<ItemBackpackAsset>,
            DefaultShirt = DefaultShirt?.Clone() as JsonAssetReference<ItemShirtAsset>,
            DefaultPants = DefaultPants?.Clone() as JsonAssetReference<ItemPantsAsset>,
            DefaultVest = DefaultVest?.Clone() as JsonAssetReference<ItemVestAsset>
        };
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

public class TeamConfigData : JSONConfigData
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
        // don't even think about leaking these
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
