using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare;

public static class F
{
    public const string COLUMN_LANGUAGE = "Language";
    public const string COLUMN_VALUE = "Value";
    public static bool IsMono { get; } = Type.GetType("Mono.Runtime") != null;
#if DEBUG
    public static CancellationToken DebugTimeout => new CancellationTokenSource(10000).Token;
#else
    public static CancellationToken DebugTimeout => default;
#endif
    public static bool HasPlayer(this List<UCPlayer> list, UCPlayer player)
    {
        IEqualityComparer<UCPlayer> c = UCPlayer.Comparer;
        for (int i = 0; i < list.Count; ++i)
        {
            if (c.Equals(list[i], player))
                return true;
        }
        return false;
    }
    public static bool HasPlayer(this IEnumerable<UCPlayer> list, UCPlayer player)
    {
        IEqualityComparer<UCPlayer> c = UCPlayer.Comparer;
        foreach (UCPlayer pl in list)
        {
            if (c.Equals(pl, player))
                return true;
        }
        return false;
    }
    public static bool HasPlayer(this UCPlayer[] array, UCPlayer player)
    {
        IEqualityComparer<UCPlayer> c = UCPlayer.Comparer;
        for (int i = 0; i < array.Length; ++i)
        {
            if (c.Equals(array[i], player))
                return true;
        }
        return false;
    }
    public static string FilterRarityToHex(string color)
    {
        if (color == null)
            return UCWarfare.GetColorHex("default");
        string f1 = "color=" + color;
        string f2 = ItemTool.filterRarityRichText(f1);
        string rtn;
        if (f2.Equals(f1) || f2.Length <= 7)
            rtn = color;
        else
            rtn = f2.Substring(7); // 7 is "color=#" length
        return !int.TryParse(rtn, NumberStyles.HexNumber, Data.Locale, out _) ? UCWarfare.GetColorHex("default") : rtn;
    }
    public static string MakeRemainder(this string[] array, int startIndex = 0, int length = -1, string deliminator = " ")
    {
        StringBuilder builder = new StringBuilder();
        for (int i = startIndex; i < (length == -1 ? array.Length : length); i++)
        {
            if (i > startIndex) builder.Append(deliminator);
            builder.Append(array[i]);
        }
        return builder.ToString();
    }
    public static int DivideRemainder(float divisor, float dividend, out int remainder)
    {
        float answer = divisor / dividend;
        remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * dividend);
        return (int)Mathf.Floor(answer);
    }
    public static int DivideRemainder(int divisor, int dividend, out int remainder)
    {
        decimal answer = (decimal)divisor / dividend;
        remainder = (int)Math.Round((answer - Math.Floor(answer)) * dividend);
        return (int)Math.Floor(answer);
    }
    public static int DivideRemainder(int divisor, decimal dividend, out int remainder)
    {
        decimal answer = divisor / dividend;
        remainder = (int)Math.Round((answer - Math.Floor(answer)) * dividend);
        return (int)Math.Floor(answer);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PermissionCheck(this UCPlayer? player, EAdminType type, PermissionComparison comparsion = PermissionComparison.AtLeast)
    {
        if (player is null) return EAdminType.CONSOLE.IsOfPermission(type, comparsion);
        EAdminType perms = player.PermissionLevel;
        if (player.Player.channel.owner.isAdmin)
            perms |= EAdminType.VANILLA_ADMIN;
        return perms.IsOfPermission(type, comparsion);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool PermissionCheck(ulong player, EAdminType type, PermissionComparison comparsion = PermissionComparison.AtLeast)
    {
        EAdminType perms = PermissionSaver.Instance.GetPlayerPermissionLevel(player);
        if (SteamAdminlist.checkAdmin(new CSteamID(player)))
            perms |= EAdminType.VANILLA_ADMIN;
        return perms.IsOfPermission(type, comparsion);
    }
    public static EAdminType GetPermissions(ulong player)
    {
        if (player == default) return EAdminType.CONSOLE;
        if (SteamAdminlist.checkAdmin(new CSteamID(player)))
            return EAdminType.VANILLA_ADMIN | PermissionSaver.Instance.GetPlayerPermissionLevel(player);
        return PermissionSaver.Instance.GetPlayerPermissionLevel(player);
    }
    public static EAdminType GetPermissions(this UCPlayer? player)
    {
        if (player is null) return EAdminType.CONSOLE;
        EAdminType perms = player.PermissionLevel;
        if (player.Player.channel.owner.isAdmin)
            perms |= EAdminType.VANILLA_ADMIN;
        return perms | PermissionSaver.Instance.GetPlayerPermissionLevel(player.Steam64);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnDutyOrAdmin(this UCPlayer player)
        => player.Player.channel.owner.isAdmin || player.PermissionCheck(EAdminType.ADMIN_ON_DUTY | EAdminType.TRIAL_ADMIN_ON_DUTY | EAdminType.VANILLA_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnDutyOrAdmin(this ulong player)
        => PermissionCheck(player, EAdminType.ADMIN_ON_DUTY | EAdminType.TRIAL_ADMIN_ON_DUTY | EAdminType.VANILLA_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnDuty(this UCPlayer player)
        => player.PermissionCheck(EAdminType.ADMIN_ON_DUTY | EAdminType.TRIAL_ADMIN_ON_DUTY, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OnDuty(this ulong player)
        => PermissionCheck(player, EAdminType.ADMIN_ON_DUTY | EAdminType.TRIAL_ADMIN_ON_DUTY, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OffDuty(this ulong player) => !OnDuty(player);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OffDuty(this UCPlayer player) => !OnDuty(player);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntern(this ulong player) => PermissionCheck(player, EAdminType.TRIAL_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntern(this UCPlayer player) => player.PermissionCheck(EAdminType.TRIAL_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAdmin(this ulong player) => PermissionCheck(player, EAdminType.ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAdmin(this UCPlayer player) => player.PermissionCheck(EAdminType.ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHelper(this ulong player) => PermissionCheck(player, EAdminType.HELPER, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHelper(this UCPlayer player) => player.PermissionCheck(EAdminType.HELPER, PermissionComparison.MaskOverlaps);

    /// <summary>Ban someone for <paramref name="duration"/> seconds.</summary>
    /// <param name="duration">Duration of ban IN SECONDS</param>
    public static void OfflineBan(ulong offender, uint ipAddress, CSteamID banner, string reason, uint duration, byte[][] hwids)
    {
        CSteamID banned = new CSteamID(offender);
        Provider.ban(banned, reason, duration);
        SteamBlacklistID id = new SteamBlacklistID(banned, ipAddress, banner, reason, duration, Provider.time, hwids);
        for (int index = 0; index < SteamBlacklist.list.Count; ++index)
        {
            if (SteamBlacklist.list[index].playerID.m_SteamID == offender)
            {
                SteamBlacklist.list[index] = id;
                goto save;
            }
        }
        SteamBlacklist.list.Add(id);
    save:
        SteamBlacklist.save();
    }
    public static ulong GetTeamFromPlayerSteam64ID(this ulong s64)
    {
        if (!Data.Is<ITeams>(out _))
        {
            SteamPlayer pl2 = PlayerTool.getSteamPlayer(s64);
            if (pl2 == null) return 0;
            else return pl2.player.quests.groupID.m_SteamID;
        }
        SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
        if (pl == default)
        {
            if (PlayerManager.HasSave(s64, out PlayerSave save))
                return save.Team;
            else return 0;
        }
        else return pl.GetTeam();
    }
    public static ulong GetTeam(this UCPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
    public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
    public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
    public static ulong GetTeam(this IPlayer player) => player is UCPlayer ucp ? ucp.GetTeam() : GetTeamFromPlayerSteam64ID(player.Steam64);
    public static ulong GetTeam(this ulong groupID)
    {
        if (!Data.Is<ITeams>(out _)) return groupID;
        if (groupID == TeamManager.Team1ID) return 1;
        else if (groupID == TeamManager.Team2ID) return 2;
        else if (groupID == TeamManager.AdminID) return 3;
        else return 0;
    }
    public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
    public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
    public static byte GetTeamByte(this ulong groupID)
    {
        if (!Data.Is<ITeams>(out _)) return groupID > byte.MaxValue ? byte.MaxValue : (byte)groupID;
        if (groupID == TeamManager.Team1ID) return 1;
        else if (groupID == TeamManager.Team2ID) return 2;
        else if (groupID == TeamManager.AdminID) return 3;
        else return 0;
    }
    public static Vector3 GetBaseSpawn(this Player player)
    {
        if (!Data.Is<ITeams>(out _)) return TeamManager.LobbySpawn;
        ulong team = player.GetTeam();
        if (team == 1)
        {
            return TeamManager.Team1Main.Center3D;
        }
        else if (team == 2)
        {
            return TeamManager.Team2Main.Center3D;
        }
        else return TeamManager.LobbySpawn;
    }
    public static Vector3 GetBaseSpawn(this Player player, out ulong team)
    {
        if (!Data.Is<ITeams>(out _))
        {
            team = player.quests.groupID.m_SteamID;
            return TeamManager.LobbySpawn;
        }
        team = player.GetTeam();
        if (team == 1)
        {
            return TeamManager.Team1Main.Center3D;
        }
        else if (team == 2)
        {
            return TeamManager.Team2Main.Center3D;
        }
        else return TeamManager.LobbySpawn;
    }
    public static Vector3 GetBaseSpawn(this ulong playerID, out ulong team)
    {
        team = playerID.GetTeamFromPlayerSteam64ID();
        if (!Data.Is<ITeams>(out _))
        {
            return TeamManager.LobbySpawn;
        }
        return team.GetBaseSpawnFromTeam();
    }
    public static Vector3 GetBaseSpawnFromTeam(this ulong team)
    {
        if (!Data.Is<ITeams>(out _))
        {
            return TeamManager.LobbySpawn;
        }
        if (team == 1) return TeamManager.Team1Main.Center3D;
        else if (team == 2) return TeamManager.Team2Main.Center3D;
        else return TeamManager.LobbySpawn;
    }
    public static float GetBaseAngle(this ulong team)
    {
        if (!Data.Is<ITeams>(out _))
        {
            return TeamManager.LobbySpawnAngle;
        }
        if (team == 1) return TeamManager.Team1SpawnAngle;
        else if (team == 2) return TeamManager.Team2SpawnAngle;
        else return TeamManager.LobbySpawnAngle;
    }
    public static IEnumerable<SteamPlayer> EnumerateClients_Remote(byte x, byte y, byte distance)
    {
        for (int i = 0; i < Provider.clients.Count; i++)
        {
            SteamPlayer client = Provider.clients[i];
            if (client.player != null && Regions.checkArea(x, y, client.player.movement.region_x, client.player.movement.region_y, distance))
                yield return client;
        }
    }
    public static float GetTerrainHeightAt2DPoint(float x, float z, float above = 0)
    {
        return LevelGround.getHeight(new Vector3(x, 0, z)) + above;
    }
    internal static float GetHeight(Vector2 point, float minHeight) => GetHeight(new Vector3(point.x, 0f, point.y), minHeight);
    internal static float GetHeight(Vector3 point, float minHeight)
    {
        float height;
        if (Physics.Raycast(new Ray(new Vector3(point.x, Level.HEIGHT, point.z), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            height = hit.point.y;
            if (!float.IsNaN(minHeight))
                return Mathf.Max(height, minHeight);
            return height;
        }
        else
        {
            height = LevelGround.getHeight(point);
            if (!float.IsNaN(minHeight))
                return Mathf.Max(height, minHeight);
            return height;
        }
    }
    public static float GetHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
    {
        if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
            return h.point.y + above;
        else return defaultY;
    }
    public static string ReplaceCaseInsensitive(this string source, string replaceIf, string replaceWith = "")
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (replaceIf == null || replaceWith == null || source.Length == 0 || replaceIf.Length == 0) return source;
        char[] chars = source.ToCharArray();
        char[] lowerchars = source.ToLower().ToCharArray();
        char[] replaceIfChars = replaceIf.ToLower().ToCharArray();
        StringBuilder buffer = new StringBuilder();
        int replaceIfLength = replaceIfChars.Length;
        StringBuilder newString = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            if (buffer.Length < replaceIfLength)
            {
                if (lowerchars[i] == replaceIfChars[buffer.Length]) buffer.Append(chars[i]);
                else
                {
                    if (buffer.Length != 0)
                        newString.Append(buffer);
                    buffer.Clear();
                    newString.Append(chars[i]);
                }
            }
            else
            {
                if (replaceWith.Length != 0) newString.Append(replaceWith);
                newString.Append(chars[i]);
            }
        }
        return newString.ToString();
    }
    public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (replacables.Length == 0) return source;
        char[] chars = source.ToCharArray();
        char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
        char[] lowerrepls;
        if (!caseSensitive)
        {
            lowerrepls = new char[replacables.Length];
            for (int i = 0; i < replacables.Length; i++)
            {
                lowerrepls[i] = char.ToLower(replacables[i]);
            }
        }
        else lowerrepls = replacables;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            bool found = false;
            for (int c = 0; c < lowerrepls.Length; c++)
            {
                if (lowerrepls[c] == lowerchars[i])
                {
                    found = true;
                }
            }
            if (!found) sb.Append(chars[i]);
        }
        return sb.ToString();
    }
    public static void TriggerEffectReliable(ushort ID, CSteamID player, Vector3 position)
    {
        TriggerEffectParameters p = new TriggerEffectParameters(ID)
        {
            position = position,
            reliable = true,
            relevantPlayerID = player
        };
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectReliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        EffectManager.sendEffectReliable(asset.id, connection, position);
    }
    public static bool SavePhotoToDisk(string path, Texture2D texture)
    {
        byte[] data = texture.EncodeToPNG();
        try
        {
            FileStream stream = File.Create(path);
            stream.Write(data, 0, data.Length);
            stream.Close();
            stream.Dispose();
            return true;
        }
        catch { return false; }
    }
    public static bool TryGetPlayerData(this Player player, out UCPlayerData component)
    {
        component = GetPlayerData(player, out bool success)!;
        return success;
    }
    public static bool TryGetPlayerData(this CSteamID player, out UCPlayerData component)
    {
        component = GetPlayerData(player, out bool success)!;
        return success;
    }
    public static bool TryGetPlayerData(this ulong player, out UCPlayerData component)
    {
        component = GetPlayerData(player, out bool success)!;
        return success;
    }
    public static UCPlayerData? GetPlayerData(this Player player, out bool success)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.PlaytimeComponents.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out UCPlayerData pt))
        {
            success = pt != null;
            return pt;
        }
        else if (player == null || player.transform == null)
        {
            success = false;
            return null;
        }
        else if (player.transform.TryGetComponent(out UCPlayerData playtimeObj))
        {
            success = true;
            return playtimeObj;
        }
        else
        {
            success = false;
            return null;
        }
    }
    public static UCPlayerData? GetPlayerData(this CSteamID player, out bool success)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.PlaytimeComponents.TryGetValue(player.m_SteamID, out UCPlayerData pt))
        {
            success = pt != null;
            return pt;
        }
        else if (player == default || player == CSteamID.Nil)
        {
            success = false;
            return null;
        }
        else
        {
            Player p = PlayerTool.getPlayer(player);
            if (p == null)
            {
                success = false;
                return null;
            }
            if (p.transform.TryGetComponent(out UCPlayerData playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
    }
    public static UCPlayerData? GetPlayerData(this ulong player, out bool success)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == 0)
        {
            success = false;
            return default;
        }
        if (Data.PlaytimeComponents.TryGetValue(player, out UCPlayerData pt))
        {
            success = pt != null;
            return pt;
        }
        else
        {
            SteamPlayer p = PlayerTool.getSteamPlayer(player);
            if (p == default || p.player == default)
            {
                success = false;
                return null;
            }
            if (p.player.transform.TryGetComponent(out UCPlayerData playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
    }
    public static FPlayerName GetPlayerOriginalNames(UCPlayer player) => player.Name;
    public static FPlayerName GetPlayerOriginalNames(SteamPlayer player) => GetPlayerOriginalNames(player.player);
    public static FPlayerName GetPlayerOriginalNames(Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.OriginalNames.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out FPlayerName names))
            return names;
        else return new FPlayerName(player);
    }
    public static FPlayerName GetPlayerOriginalNames(ulong player)
    {
        if (player == 0) return FPlayerName.Console;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
            return names;
        else
        {
            SteamPlayer? pl = PlayerTool.getSteamPlayer(player);
            if (pl == null)
            {
                try
                {
                    return Data.DatabaseManager.GetUsernames(player);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Equals("Not connected", StringComparison.Ordinal))
                        throw ex;
                    string tname = player.ToString(Data.Locale);
                    return new FPlayerName() { Steam64 = player, PlayerName = tname, CharacterName = tname, NickName = tname, WasFound = false };
                }
            }
            else return new FPlayerName()
            {
                CharacterName = pl.playerID.characterName,
                NickName = pl.playerID.nickName,
                PlayerName = pl.playerID.playerName,
                Steam64 = player,
                WasFound = true
            };
        }
    }
    public static Task<FPlayerName> GetPlayerOriginalNamesAsync(ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
            return Task.FromResult(names);
        else if (OffenseManager.IsValidSteam64ID(player))
        {
            SteamPlayer? pl = PlayerTool.getSteamPlayer(player);
            if (pl == default)
                return Data.DatabaseManager.GetUsernamesAsync(player);
            else return Task.FromResult(new FPlayerName()
            {
                CharacterName = pl.playerID.characterName,
                NickName = pl.playerID.nickName,
                PlayerName = pl.playerID.playerName,
                Steam64 = player
            });
        }
        return Task.FromResult(FPlayerName.Nil);
    }
    public static bool IsInMain(this Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is<ITeams>(out _)) return false;
        ulong team = player.GetTeam();
        if (team == 1) return TeamManager.Team1Main.IsInside(player.transform.position);
        else if (team == 2) return TeamManager.Team2Main.IsInside(player.transform.position);
        else return false;
    }
    public static bool IsInMain(Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is<ITeams>(out _)) return false;
        return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
    }
    public static bool IsOnFlag(this Player player) => player != null && Data.Is(out IFlagRotation fg) && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
    public static bool IsOnFlag(this Player player, out Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player != null && Data.Is(out IFlagRotation fg))
        {
            if (fg.OnFlag == null || fg.Rotation == null)
            {
                flag = null!;
                return false;
            }
            if (fg.OnFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int id))
            {
                flag = fg.Rotation.Find(x => x.ID == id);
                return flag != null;
            }
        }
        flag = null!;
        return false;
    }
    public static string Colorize(this string inner, string colorhex) => $"<color=#{colorhex}>{inner}</color>";

    public static string ColorizeTMPro(this string inner, string colorhex, bool endTag = true) => endTag ? $"<#{colorhex}>{inner}</color>" : $"<#{colorhex}>{inner}";
    public static string ColorizeName(string innerText, ulong team)
    {
        if (!Data.Is<ITeams>(out _)) return innerText;
        if (team == TeamManager.ZOMBIE_TEAM_ID) return $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>";
        else if (team == TeamManager.Team1ID) return $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>";
        else if (team == TeamManager.Team2ID) return $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>";
        else if (team == TeamManager.AdminID) return $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>";
        else return $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>";
    }
    /// <exception cref="SingletonLoadException"/>
    public static void CheckDir(string path, out bool success, bool unloadIfFail = false)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
                success = true;
                L.Log("Created directory: " + path + ".", ConsoleColor.Magenta);
            }
            catch (Exception ex)
            {
                L.LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                success = false;
                if (unloadIfFail)
                {
                    _ = Gamemode.FailToLoadGame(ex);
                    throw new SingletonLoadException(ESingletonLoadType.LOAD, null, ex);
                }
            }
        }
        else success = true;
    }
#if DEBUG
    public static void SaveProfilingData()
    {
        string directory = Path.Combine(Data.Paths.BaseDirectory, "Profiling") + Path.DirectorySeparatorChar;
        CheckDir(directory, out _);
        string fi = Path.Combine(directory, DateTime.Now.ToString("yyyy-mm-dd_HH-mm-ss") + "_profile.csv");
        L.Log("Flushing profiling information to " + fi, ConsoleColor.Cyan);
        ProfilingUtils.WriteAllDataToCSV(fi);
        ProfilingUtils.Clear();
    }
#endif
    public static void SendSteamURL(this SteamPlayer player, string message, ulong SteamID) => player.SendURL(message, $"https://steamcommunity.com/profiles/{SteamID}/");
    public static void SendURL(this SteamPlayer player, string message, string url)
    {
        if (player == default || url == default) return;
        player.player.sendBrowserRequest(message, url);
    }
    public static string GetLayer(Vector3 direction, Vector3 origin, int Raymask)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, 8192f, Raymask))
        {
            if (hit.transform != null)
                return hit.transform.gameObject.layer.ToString();
            else return "nullHitNoTransform";
        }
        else return "nullNoHit";
    }

    public static bool CanStandAtLocation(Vector3 source) => PlayerStance.hasStandingHeightClearanceAtPosition(source);
    public static string GetClosestLocation(Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int index = GetClosestLocationIndex(point);
        return index == -1 ? string.Empty : ((LocationNode)LevelNodes.nodes[index]).name;
    }
    public static int GetClosestLocationIndex(Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int index = -1;
        float smallest = -1f;
        for (int i = 0; i < LevelNodes.nodes.Count; i++)
        {
            if (LevelNodes.nodes[i] is LocationNode node)
            {
                float amt = (point - node.point).sqrMagnitude;
                if (smallest < 0f || amt < smallest)
                {
                    index = i;
                    smallest = amt;
                }
            }
        }
        return index;
    }
    public static void NetInvoke(this NetCall call)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!);
    }
    public static void NetInvoke(this NetCallCustom call, NetCallCustom.WriterTask task)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, task);
    }
    public static void NetInvoke<T>(this NetCallRaw<T> call, T arg)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg);
    }
    public static void NetInvoke<T1, T2>(this NetCallRaw<T1, T2> call, T1 arg1, T2 arg2)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2);
    }
    public static void NetInvoke<T1, T2, T3>(this NetCallRaw<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3);
    }
    public static void NetInvoke<T1, T2, T3, T4>(this NetCallRaw<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4);
    }
    public static void NetInvoke<T1>(this NetCall<T1> call, T1 arg1)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1);
    }
    public static void NetInvoke<T1, T2>(this NetCall<T1, T2> call, T1 arg1, T2 arg2)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2);
    }
    public static void NetInvoke<T1, T2, T3>(this NetCall<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3);
    }
    public static void NetInvoke<T1, T2, T3, T4>(this NetCall<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5>(this NetCall<T1, T2, T3, T4, T5> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6>(this NetCall<T1, T2, T3, T4, T5, T6> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7>(this NetCall<T1, T2, T3, T4, T5, T6, T7> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(UCWarfare.I.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
    public static bool FilterName(string original, out string final)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (UCWarfare.Config.DisableNameFilter || UCWarfare.Config.MinAlphanumericStringLength <= 0)
        {
            final = original;
            return false;
        }
        IEnumerator<char> charenum = original.GetEnumerator();
        int alphanumcount = 0;
        while (charenum.MoveNext())
        {
            char ch = charenum.Current;
            int c = ch;
            if (c is > 31 and < 127)
            {
                if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                {
                    final = original;
                    charenum.Dispose();
                    return false;
                }
                alphanumcount++;
            }
            else
            {
                alphanumcount = 0;
            }
        }
        charenum.Dispose();
        final = original;
        return alphanumcount != original.Length;
    }
    public static bool HasGUID<T>(this JsonAssetReference<T>[] assets, Guid guid) where T : Asset
    {
        for (int i = 0; i < assets.Length; ++i)
        {
            T? asset = assets[i].Asset;
            if (asset is null) continue;
            if (asset.GUID == guid) return true;
        }
        return false;
    }
    public static bool HasID<T>(this JsonAssetReference<T>[] assets, ushort id) where T : Asset
    {
        for (int i = 0; i < assets.Length; ++i)
        {
            T? asset = assets[i].Asset;
            if (asset is null) continue;
            if (asset.id == id) return true;
        }
        return false;
    }
    public static bool ValidReference<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, out Guid guid) where TAsset : Asset
    {
        if (reference is not null && reference.HasValue && reference.Value.Exists)
        {
            guid = reference.Value.Guid;
            return true;
        }

        guid = Guid.Empty;
        return false;
    }
    public static bool ValidReference<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, out TAsset asset) where TAsset : Asset
    {
        if (reference is not null && reference.HasValue && reference.Value.Exists)
        {
            asset = reference.Value.Asset!;
            return true;
        }

        asset = null!;
        return false;
    }
    public static bool ValidReference<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, out ushort id) where TAsset : Asset
    {
        if (reference is not null && reference.HasValue && reference.Value.Exists)
        {
            id = reference.Value.Id;
            return true;
        }

        id = default;
        return false;
    }
    public static bool ValidReference<TAsset>(this JsonAssetReference<TAsset>? reference, out Guid guid) where TAsset : Asset
    {
        if (reference is not null && reference.Exists)
        {
            guid = reference.Guid;
            return true;
        }

        guid = Guid.Empty;
        return false;
    }
    public static bool ValidReference<TAsset>(this JsonAssetReference<TAsset>? reference, out TAsset asset) where TAsset : Asset
    {
        if (reference is not null && reference.Exists)
        {
            asset = reference.Asset!;
            return true;
        }

        asset = null!;
        return false;
    }
    public static bool ValidReference<TAsset>(this JsonAssetReference<TAsset>? reference, out ushort id) where TAsset : Asset
    {
        if (reference is not null && reference.Exists)
        {
            id = reference.Id;
            return true;
        }

        id = default;
        return false;
    }
    public static bool MatchGuid<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, Guid match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && guid == match;
    }
    public static bool MatchGuid<TAsset>(this JsonAssetReference<TAsset>? reference, Guid match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && guid == match;
    }
    public static bool MatchGuid<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, RotatableConfig<JsonAssetReference<TAsset>>? match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && match.ValidReference(out Guid guid2) && guid == guid2;
    }
    public static bool MatchGuid<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, JsonAssetReference<TAsset>? match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && match.ValidReference(out Guid guid2) && guid == guid2;
    }
    public static bool MatchGuid<TAsset>(this JsonAssetReference<TAsset>? reference, RotatableConfig<JsonAssetReference<TAsset>>? match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && match.ValidReference(out Guid guid2) && guid == guid2;
    }
    public static bool MatchGuid<TAsset>(this JsonAssetReference<TAsset>? reference, JsonAssetReference<TAsset>? match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && match.ValidReference(out Guid guid2) && guid == guid2;
    }
    public static string RemoveColorTag(string questName)
    {
        if (questName is null || questName.Length < 6) return questName!;

        int ind;
        if (questName[0] == '<')
        {
            if (questName[1] == '#')
                ind = questName.IndexOf('>', 2);
            else if (questName.Length > 8 && questName[1] == 'c' && questName[2] == 'o' && questName[3] == 'l' &&
                     questName[4] == 'o' && questName[5] == 'r' && questName[6] == '=')
            {
                ind = questName.IndexOf('>', 7);
            }
            else return questName;

            if (ind != -1)
                questName = questName.Substring(ind + 1);
            else return questName;
        }
        if (questName[questName.Length - 1] == '>')
        {
            ind = questName.LastIndexOf('<', questName.Length - 2);
            if (ind != -1)
                questName = questName.Substring(0, ind);
        }

        return questName;
    }
    public static void SetOwnerOrGroup(this IBuildable obj, ulong? owner = null, ulong? group = null)
    {
        if (obj.Drop is BarricadeDrop bdrop)
            SetOwnerOrGroup(bdrop, owner, group);
        else if (obj.Drop is StructureDrop sdrop)
            SetOwnerOrGroup(sdrop, owner, group);
        else
            throw new InvalidOperationException("Unable to get drop from IBuildable of type " + obj.Type + ".");
    }
    public static void SetOwnerOrGroup(BarricadeDrop drop, ulong? owner = null, ulong? group = null)
    {
        ThreadUtil.assertIsGameThread();
        if (!owner.HasValue && !group.HasValue)
            return;
        BarricadeData bdata = drop.GetServersideData();
        ulong o = owner ?? bdata.owner;
        ulong g = group ?? bdata.group;
        BarricadeManager.changeOwnerAndGroup(drop.model, o, g);
        byte[] oldSt = bdata.barricade.state;
        byte[] state;
        if (drop.interactable is InteractableStorage storage)
        {
            if (oldSt.Length < sizeof(ulong) * 2)
                oldSt = new byte[sizeof(ulong) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(o), 0, oldSt, 0, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(g), 0, oldSt, sizeof(ulong), sizeof(ulong));
            BarricadeManager.updateState(drop.model, oldSt, oldSt.Length);
            drop.ReceiveUpdateState(oldSt);
            if (Data.SendUpdateBarricadeState != null && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
            {
                if (storage.isDisplay)
                {
                    Block block = new Block();
                    if (storage.displayItem != null)
                        block.write(storage.displayItem.id, storage.displayItem.quality,
                            storage.displayItem.state ?? Array.Empty<byte>());
                    else
                        block.step += 4;
                    block.write(storage.displaySkin, storage.displayMythic,
                        storage.displayTags ?? string.Empty,
                        storage.displayDynamicProps ?? string.Empty, storage.rot_comp);
                    byte[] b = block.getBytes(out int size);
                    state = new byte[size + sizeof(ulong) * 2];
                    Buffer.BlockCopy(b, 0, state, sizeof(ulong) * 2, size);
                }
                else
                    state = new byte[sizeof(ulong) * 2];
                Buffer.BlockCopy(oldSt, 0, state, 0, sizeof(ulong) * 2);
                Data.SendUpdateBarricadeState.Invoke(drop.GetNetId(), ENetReliability.Reliable,
                    BarricadeManager.EnumerateClients_Remote(x, y, plant), state);
            }
        }
        else if (drop.interactable is InteractableSign sign)
        {
            if (oldSt.Length < sizeof(ulong) * 2 + 1)
                oldSt = new byte[sizeof(ulong) * 2 + 1];
            Buffer.BlockCopy(BitConverter.GetBytes(o), 0, oldSt, 0, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(g), 0, oldSt, sizeof(ulong), sizeof(ulong));
            if (sign.text.StartsWith(Signs.PREFIX, StringComparison.Ordinal) && Data.SendUpdateBarricadeState != null && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
            {
                BarricadeManager.updateState(drop.model, oldSt, oldSt.Length);
                drop.ReceiveUpdateState(oldSt);
                NetId id = drop.GetNetId();
                state = null!;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (plant != ushort.MaxValue || Regions.checkArea(x, y, pl.Player.movement.region_x,
                            pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                    {
                        byte[] text = System.Text.Encoding.UTF8.GetBytes(Signs.GetClientText(sign.text, pl, sign));
                        int txtLen = Math.Min(text.Length, byte.MaxValue - 17);
                        if (state == null || state.Length != txtLen + 17)
                        {
                            state = new byte[txtLen + 17];
                            Buffer.BlockCopy(oldSt, 0, state, 0, sizeof(ulong) * 2);
                            state[16] = (byte)txtLen;
                        }

                        Buffer.BlockCopy(text, 0, state, 17, txtLen);
                        Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, pl.Connection, state);
                    }
                }
            }
            else
            {
                BarricadeManager.updateReplicatedState(drop.model, oldSt, oldSt.Length);
            }
        }
        else
        {
            switch (drop.asset.build)
            {
                case EBuild.DOOR:
                case EBuild.GATE:
                case EBuild.SHUTTER:
                case EBuild.HATCH:
                    state = new byte[17];
                    Buffer.BlockCopy(BitConverter.GetBytes(o), 0, state, 0, sizeof(ulong));
                    Buffer.BlockCopy(BitConverter.GetBytes(g), 0, state, sizeof(ulong), sizeof(ulong));
                    state[16] = (byte)(oldSt[16] > 0 ? 1 : 0);
                    break;
                case EBuild.BED:
                    state = BitConverter.GetBytes(o);
                    break;
                case EBuild.STORAGE:
                case EBuild.SENTRY:
                case EBuild.SENTRY_FREEFORM:
                case EBuild.SIGN:
                case EBuild.SIGN_WALL:
                case EBuild.NOTE:
                case EBuild.LIBRARY:
                case EBuild.MANNEQUIN:
                    state = oldSt.Length < sizeof(ulong) * 2
                        ? new byte[sizeof(ulong) * 2]
                        : Util.CloneBytes(oldSt);
                    Buffer.BlockCopy(BitConverter.GetBytes(o), 0, state, 0, sizeof(ulong));
                    Buffer.BlockCopy(BitConverter.GetBytes(g), 0, state, sizeof(ulong), sizeof(ulong));
                    break;
                case EBuild.SPIKE:
                case EBuild.WIRE:
                case EBuild.CHARGE:
                case EBuild.BEACON:
                case EBuild.CLAIM:
                    state = oldSt.Length == 0 ? oldSt : Array.Empty<byte>();
                    if (drop.interactable is InteractableCharge charge)
                    {
                        charge.owner = o;
                        charge.group = g;
                    }
                    else if (drop.interactable is InteractableClaim claim)
                    {
                        claim.owner = o;
                        claim.group = g;
                    }
                    break;
                default:
                    state = oldSt;
                    break;
            }
            bool diff = state.Length != oldSt.Length;
            if (!diff && state != oldSt)
            {
                for (int i = 0; i < state.Length; ++i)
                {
                    if (state[i] != oldSt[i])
                    {
                        diff = true;
                        break;
                    }
                }
            }
            if (diff)
            {
                BarricadeManager.updateReplicatedState(drop.model, state, state.Length);
            }
        }
    }
    public static void SetOwnerOrGroup(StructureDrop drop, ulong? owner = null, ulong? group = null)
    {
        ThreadUtil.assertIsGameThread();
        if (!owner.HasValue && !group.HasValue)
            return;
        StructureData sdata = drop.GetServersideData();
        StructureManager.changeOwnerAndGroup(drop.model, owner ?? sdata.owner, group ?? sdata.group);
    }
    public static void EulerToBytes(Vector3 euler, out byte angle_x, out byte angle_y, out byte angle_z)
    {
        angle_x = MeasurementTool.angleToByte(euler.x);
        angle_y = MeasurementTool.angleToByte(euler.y);
        angle_z = MeasurementTool.angleToByte(euler.z);
    }
    public static Vector3 BytesToEuler(byte angle_x, byte angle_y, byte angle_z) =>
        new Vector3(MeasurementTool.byteToAngle(angle_x), MeasurementTool.byteToAngle(angle_y),
            MeasurementTool.byteToAngle(angle_z));
    public static (byte angle_x, byte angle_y, byte angle_z) EulerToBytes(Vector3 euler)
        => (MeasurementTool.angleToByte(euler.x), MeasurementTool.angleToByte(euler.y),
            MeasurementTool.angleToByte(euler.z));
    public static bool AlmostEquals(this Vector3 left, Vector3 right, float tolerance = 0.05f)
    {
        return Mathf.Abs(left.x - right.x) < tolerance &&
               Mathf.Abs(left.y - right.y) < tolerance &&
               Mathf.Abs(left.z - right.z) < tolerance;
    }
    public static bool AlmostEquals(this Vector2 left, Vector2 right, float tolerance = 0.05f)
    {
        return Mathf.Abs(left.x - right.x) < tolerance &&
               Mathf.Abs(left.y - right.y) < tolerance;
    }
    public static Schema GetListSchema<T>(string tableName, string pkColumn, string valueColumn, string primaryTableName, string primaryTablePkColumn, bool hasPk = false, bool oneToOne = false, int length = -1, bool nullable = false, bool unique = false, string pkName = "pk")
    {
        Type type = typeof(T);
        string typestr;
        if (type == typeof(Guid))
            typestr = SqlTypes.GUID;
        else if (type == typeof(ulong))
            typestr = SqlTypes.ULONG;
        else if (type == typeof(byte[]))
            typestr = length < 1 ? SqlTypes.BYTES_255 : "binary(" + length + ")";
        else if (type == typeof(string))
            typestr = length < 1 ? SqlTypes.STRING_255 : "varchar(" + length + ")";
        else if (type == typeof(float))
            typestr = SqlTypes.FLOAT;
        else if (type == typeof(double))
            typestr = SqlTypes.DOUBLE;
        else if (type == typeof(long))
            typestr = SqlTypes.LONG;
        else if (type == typeof(uint))
            typestr = SqlTypes.UINT;
        else if (type == typeof(int))
            typestr = SqlTypes.INT;
        else if (type == typeof(short))
            typestr = SqlTypes.SHORT;
        else if (type == typeof(ushort))
            typestr = SqlTypes.USHORT;
        else if (type == typeof(byte))
            typestr = SqlTypes.BYTE;
        else if (type == typeof(sbyte))
            typestr = SqlTypes.SBYTE;
        else if (type == typeof(bool))
            typestr = SqlTypes.BOOLEAN;
        else if (type == typeof(PrimaryKey))
            typestr = SqlTypes.INCREMENT_KEY;
        else
        {
            MethodInfo info = type.GetMethod("GetDefaultSchema", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (info != null && typeof(Schema).IsAssignableFrom(info.ReturnType))
            {
                ParameterInfo[] parameters = info.GetParameters();
                if (parameters.Length > 3 &&
                    parameters[0].ParameterType == typeof(string) && parameters[0].Name.Equals("tableName", StringComparison.OrdinalIgnoreCase) &&
                    parameters[1].ParameterType == typeof(string) && parameters[1].Name.Equals("fkColumn", StringComparison.OrdinalIgnoreCase) &&
                    parameters[2].ParameterType == typeof(string) && parameters[2].Name.Equals("mainTable", StringComparison.OrdinalIgnoreCase) &&
                    parameters[3].ParameterType == typeof(string) && parameters[3].Name.Equals("mainPkColumn", StringComparison.OrdinalIgnoreCase) &&
                    (parameters.Length == 4 || parameters[4].IsOptional))
                {
                    int oneToOneIndex = -1;
                    int hasPkIndex = -1;
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        ParameterInfo p = parameters[i];
                        if (p.Name.Equals(nameof(oneToOne)))
                            oneToOneIndex = i;
                        else if (p.Name.Equals("hasPk"))
                            hasPkIndex = i;
                    }

                    object[] objs = new object[parameters.Length];
                    for (int i = 4; i < parameters.Length; ++i)
                        objs[i] = Type.Missing;
                    if (oneToOneIndex != -1)
                        objs[oneToOneIndex] = oneToOne;
                    if (hasPkIndex != -1)
                        objs[hasPkIndex] = hasPk;
                    objs[0] = tableName;
                    objs[1] = pkColumn;
                    objs[2] = primaryTableName;
                    objs[3] = primaryTablePkColumn;
                    try
                    {
                        if (info.Invoke(null, objs) is Schema s)
                            return s;
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(nameof(T), type.Name + " is not a valid type for GetListSchema<T>(...).", ex);
                    }
                }
            }
            throw new ArgumentException(nameof(T), type.Name + " is not a valid type for GetListSchema<T>(...).");
        }
        Schema.Column[] columns = new Schema.Column[hasPk ? 3 : 2];
        int index = 0;
        if (hasPk)
        {
            columns[0] = new Schema.Column(pkName, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(pkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = !hasPk && oneToOne,
            AutoIncrement = !hasPk && oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = primaryTablePkColumn,
            ForeignKeyTable = primaryTableName
        };
        columns[++index] = new Schema.Column(valueColumn, typestr)
        {
            Nullable = nullable,
            UniqueKey = unique
        };
        return new Schema(tableName, columns, false, type);
    }

    public static Schema GetTranslationListSchema(string tableName, string pkColumn, string mainTable, string mainPkColumn, int length)
    {
        return new Schema(tableName, new Schema.Column[]
        {
            new Schema.Column(pkColumn, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = mainTable,
                ForeignKeyColumn = mainPkColumn
            },
            new Schema.Column(COLUMN_LANGUAGE, "char(5)")
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_VALUE, "varchar(" + length.ToString(CultureInfo.InvariantCulture) + ")")
        }, false, typeof(KeyValuePair<string, string>));
    }
}