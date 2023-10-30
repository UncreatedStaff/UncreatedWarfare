using Cysharp.Threading.Tasks;
using MySqlConnector;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Types = SDG.Unturned.Types;

namespace Uncreated.Warfare;

public static class F
{
    internal static readonly char[] SpaceSplit = { ' ' };
    public const string COLUMN_LANGUAGE = "Language";
    public const string COLUMN_VALUE = "Value";
    public static bool IsMono { get; } = Type.GetType("Mono.Runtime") != null;
#if DEBUG
    public static CancellationToken DebugTimeout => new CancellationTokenSource(10000).Token;
#else
    public static CancellationToken DebugTimeout => default;
#endif
    public static string AssetToString(Guid guid)
    {
        if (Assets.find(guid) is { } asset)
            return "{" + asset.FriendlyName + " / " + asset.name + " / " + asset.GUID.ToString("N") + "}";
        return guid.ToString("N");
    }

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
        return !int.TryParse(rtn, NumberStyles.HexNumber, Data.AdminLocale, out _) ? UCWarfare.GetColorHex("default") : rtn;
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
        if (!Data.Is<ITeams>())
        {
            SteamPlayer pl2 = PlayerTool.getSteamPlayer(s64);
            return pl2 == null ? 0ul : pl2.player.quests.groupID.m_SteamID;
        }
        SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
        return pl == null ? PlayerManager.HasSave(s64, out PlayerSave save) ? save.Team : 0ul : pl.GetTeam();
    }
    public static ulong GetTeam(this UCPlayer player) => player.IsOnline ? GetTeam(player.Player.quests.groupID.m_SteamID) : 0;
    public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
    public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
    public static ulong GetTeam(this IPlayer player) => player is UCPlayer ucp ? ucp.GetTeam() : GetTeamFromPlayerSteam64ID(player.Steam64);
    public static ulong GetTeam(this ulong groupID)
    {
        if (!Data.Is<ITeams>(out _)) return groupID;
        return groupID switch
        {
            TeamManager.Team1ID => 1,
            TeamManager.Team2ID => 2,
            TeamManager.AdminID => 3,
            _ => 0
        };
    }
    public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
    public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
    public static byte GetTeamByte(this ulong groupID)
    {
        if (!Data.Is<ITeams>(out _)) return groupID > byte.MaxValue ? byte.MaxValue : (byte)groupID;
        return groupID switch
        {
            TeamManager.Team1ID => 1,
            TeamManager.Team2ID => 2,
            TeamManager.AdminID => 3,
            _ => 0
        };
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
    public static float GetTerrainHeightAt2DPoint(Vector3 position, float above = 0)
    {
        return LevelGround.getHeight(position) + above;
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
            return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
        }

        height = LevelGround.getHeight(point);
        return !float.IsNaN(minHeight) ? Mathf.Max(height, minHeight) : height;
    }
    public static float GetHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
    {
        if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
            return h.point.y + above;
        return defaultY;
    }
    public static bool TryGetHeight(float x, float z, out float height, float add = 0f)
    {
        if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            height = h.point.y + add;
            return true;
        }

        height = 0f;
        return false;
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
    public static bool ArrayContains(this byte[] array, byte value)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (array[i] == value)
                return true;
        }

        return false;
    }
    public static bool ArrayContains<T>(this T[] array, T value)
    {
        Comparer<T> comparison = Comparer<T>.Default;
        for (int i = 0; i < array.Length; ++i)
        {
            if (comparison.Compare(array[i], value) == 0)
                return true;
        }

        return false;
    }
    public static void TryTriggerSupplyEffect(SupplyType type, Vector3 position)
    {
        if ((type is SupplyType.Build ? Gamemode.Config.EffectUnloadBuild : Gamemode.Config.EffectUnloadAmmo).ValidReference(out EffectAsset effect))
            TriggerEffectReliable(effect, EffectManager.MEDIUM, position);
    }
    public static void TriggerEffectReliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        ThreadUtil.assertIsGameThread();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = true
        };
        p.SetRelevantPlayer(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectReliable(EffectAsset asset, float range, Vector3 position)
        => TriggerEffectReliable(asset, Provider.GatherRemoteClientConnectionsWithinSphere(position, range), position);
    public static void TriggerEffectReliable(EffectAsset asset, PooledTransportConnectionList connection, Vector3 position)
    {
        ThreadUtil.assertIsGameThread();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = true
        };
        p.SetRelevantTransportConnections(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectUnreliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        ThreadUtil.assertIsGameThread();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = false
        };
        p.SetRelevantPlayer(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectUnreliable(EffectAsset asset, float range, Vector3 position)
        => TriggerEffectUnreliable(asset, Provider.GatherRemoteClientConnectionsWithinSphere(position, range), position);
    public static void TriggerEffectUnreliable(EffectAsset asset, PooledTransportConnectionList connection, Vector3 position)
    {
        ThreadUtil.assertIsGameThread();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = false
        };
        p.SetRelevantTransportConnections(connection);
        EffectManager.triggerEffect(p);
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
        if (player == null || player.transform == null)
        {
            success = false;
            return null;
        }
        if (player.transform.TryGetComponent(out UCPlayerData playtimeObj))
        {
            success = true;
            return playtimeObj;
        }
        success = false;
        return null;
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
    public static ValueTask<PlayerNames> GetPlayerOriginalNamesAsync(ulong player, CancellationToken token = default)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null)
        {
            Data.ModerationSql.UpdateUsernames(player, pl.Name);
            return new ValueTask<PlayerNames>(pl.Name);
        }

        return Util.IsValidSteam64Id(player)
            ? new ValueTask<PlayerNames>(Data.DatabaseManager.GetUsernamesAsync(player, token))
            : new ValueTask<PlayerNames>(PlayerNames.Nil);
    }
    public static bool IsInMain(this Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is<ITeams>()) return false;
        ulong team = player.GetTeam();
        return team switch
        {
            1 => TeamManager.Team1Main.IsInside(player.transform.position),
            2 => TeamManager.Team2Main.IsInside(player.transform.position),
            _ => false
        };
    }
    public static bool IsInMain(Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.Is<ITeams>()) return false;
        return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
    }
    public static bool IsOnFlag(this Player player) => player != null && Data.Is(out IFlagRotation fg) && fg.OnFlag != null && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
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
            if (fg.OnFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int index))
            {
                flag = fg.Rotation[index];
                return flag != null;
            }
        }
        flag = null!;
        return false;
    }
    public static string Colorize(this string inner, string colorhex, bool imgui) => imgui ? Colorize(inner, colorhex) : ColorizeTMPro(inner, colorhex);
    public static string Colorize(this string inner, string colorhex) => "<color=#" + colorhex + ">" + inner + "</color>";
    public static string ColorizeTMPro(this string inner, string colorhex, bool endTag = true) =>
        endTag ? "<#" +colorhex + ">" + inner + "</color>" : "<#" + colorhex + ">" + inner;
    public static string ColorizeName(string innerText, ulong team)
    {
        if (!Data.Is<ITeams>(out _)) return innerText;
        return team switch
        {
            TeamManager.ZombieTeamID => $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>",
            TeamManager.Team1ID => $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>",
            TeamManager.Team2ID => $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>",
            TeamManager.AdminID => $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>",
            _ => $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>"
        };
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
                    UCWarfare.RunTask(Gamemode.FailToLoadGame, ex, ctx: "Checking directory \"" + path + "\" failed, unloading game.");
                    throw new SingletonLoadException(SingletonLoadType.Load, null, ex);
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
    public static void SendSteamURL(this SteamPlayer player, string message, ulong s64) =>
        player.SendURL(message, $"https://steamcommunity.com/profiles/{s64}/");
    public static void SendURL(this SteamPlayer player, string message, string url)
    {
        if (player == null || url == null) return;
        player.player.sendBrowserRequest(message, url);
    }
    public static bool CanStandAtLocation(Vector3 source) => PlayerStance.hasStandingHeightClearanceAtPosition(source);
    /// <remarks>Write-locks <see cref="ZoneList"/> if <paramref name="zones"/> is <see langword="true"/>.</remarks>
    public static string GetClosestLocationName(Vector3 point, bool zones = false, bool shortNames = false)
    {
        if (zones)
        {
            ZoneList? list = Data.Singletons.GetSingleton<ZoneList>();
            if (list != null)
            {
                list.WriteWait();
                try
                {
                    string? val = null;
                    float smallest = -1f;
                    Vector2 point2d = new Vector2(point.x, point.z);
                    for (int i = 0; i < list.Items.Count; ++i)
                    {
                        SqlItem<Zone> proxy = list.Items[i];
                        Zone? z = proxy.Item;
                        if (z != null)
                        {
                            float amt = (point2d - z.Center).sqrMagnitude;
                            if (smallest < 0f || amt < smallest)
                            {
                                val = shortNames ? z.ShortName ?? z.Name : z.Name;
                                smallest = amt;
                            }
                        }
                    }

                    if (val != null)
                        return val;
                }
                finally
                {
                    list.WriteRelease();
                }
            }
        }
        LocationDevkitNode? node = GetClosestLocation(point);
        return node == null ? new GridLocation(in point).ToString() : node.locationName;
    }
    public static LocationDevkitNode? GetClosestLocation(Vector3 point)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IReadOnlyList<LocationDevkitNode> list = LocationDevkitNodeSystem.Get().GetAllNodes();
        int index = -1;
        float smallest = -1f;
        for (int i = 0; i < list.Count; ++i)
        {
            float amt = (point - list[i].transform.position).sqrMagnitude;
            if (smallest < 0f || amt < smallest)
            {
                index = i;
                smallest = amt;
            }
        }

        if (index == -1)
            return null;
        return list[index];
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
    public static bool HasGuid<T>(this JsonAssetReference<T>[] assets, Guid guid) where T : Asset
    {
        if (assets == null)
            return false;
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
    public static bool HasGuid<T>(this RotatableConfig<JsonAssetReference<T>[]> assets, Guid guid) where T : Asset
    {
        if (assets is null || !assets.HasValue)
            return false;
        return assets.Value.HasGuid(guid);
    }
    public static bool HasID<T>(this RotatableConfig<JsonAssetReference<T>[]> assets, ushort id) where T : Asset
    {
        if (assets is null || !assets.HasValue)
            return false;
        return assets.Value.HasID(id);
    }
    public static TAsset? GetAsset<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference) where TAsset : Asset
    {
        if (reference.ValidReference(out TAsset asset))
            return asset;
        return null;
    }
    public static TAsset? GetAsset<TAsset>(this JsonAssetReference<TAsset>? reference) where TAsset : Asset
    {
        if (reference.ValidReference(out TAsset asset))
            return asset;
        return null;
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
    public static bool MatchGuid<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? reference, TAsset match) where TAsset : Asset
    {
        return reference.ValidReference(out Guid guid) && guid == match.GUID;
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
    public static bool AnyMapsContainGuid<TAsset>(this RotatableConfig<JsonAssetReference<TAsset>>? config, Guid guid) where TAsset : Asset
    {
        if (config is null) return false;
        foreach(JsonAssetReference<TAsset>? asset in config.Values)
        {
            if (asset.MatchGuid(guid))
                return true;
        }

        return false;
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
    public static void EnsureCorrectGroupAndOwner(ref byte[] state, ItemBarricadeAsset asset, ulong owner, ulong group)
    {
        if (state == null)
        {
            state = Array.Empty<byte>();
            return;
        }

        switch (asset.build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
                if (state.Length < sizeof(ulong) * 2)
                    state = new byte[17];
                Buffer.BlockCopy(BitConverter.GetBytes(owner), 0, state, 0, sizeof(ulong));
                Buffer.BlockCopy(BitConverter.GetBytes(group), 0, state, sizeof(ulong), sizeof(ulong));
                break;
            case EBuild.BED:
                state = BitConverter.GetBytes(owner);
                break;
            case EBuild.STORAGE:
            case EBuild.SENTRY:
            case EBuild.SENTRY_FREEFORM:
            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
            case EBuild.LIBRARY:
            case EBuild.MANNEQUIN:
                if (state.Length < sizeof(ulong) * 2)
                    state = new byte[16];
                Buffer.BlockCopy(BitConverter.GetBytes(owner), 0, state, 0, sizeof(ulong));
                Buffer.BlockCopy(BitConverter.GetBytes(group), 0, state, sizeof(ulong), sizeof(ulong));
                break;
        }
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
                    BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
            }
        }
        else if (drop.interactable is InteractableSign sign)
        {
            if (oldSt.Length < sizeof(ulong) * 2 + 1)
                oldSt = new byte[sizeof(ulong) * 2 + 1];
            Buffer.BlockCopy(BitConverter.GetBytes(o), 0, oldSt, 0, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(g), 0, oldSt, sizeof(ulong), sizeof(ulong));
            if (sign.text.StartsWith(Signs.Prefix, StringComparison.Ordinal) && Data.SendUpdateBarricadeState != null && BarricadeManager.tryGetRegion(drop.model, out byte x, out byte y, out ushort plant, out _))
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
                        byte[] text = System.Text.Encoding.UTF8.GetBytes(Signs.GetClientText(drop, pl));
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
            if (!state.CompareBytes(oldSt))
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
    // ReSharper disable InconsistentNaming
    public static void EulerToBytes(Vector3 euler, out byte angle_x, out byte angle_y, out byte angle_z)
    {
        angle_x = MeasurementTool.angleToByte(euler.x);
        angle_y = MeasurementTool.angleToByte(euler.y);
        angle_z = MeasurementTool.angleToByte(euler.z);
    }
    public static Vector3 BytesToEuler(byte angle_x, byte angle_y, byte angle_z) =>
        new Vector3(MeasurementTool.byteToAngle(angle_x), MeasurementTool.byteToAngle(angle_y),
            MeasurementTool.byteToAngle(angle_z));
    public static Vector3 BytesToEulerForVehicle(byte angle_x, byte angle_y, byte angle_z) =>
        new Vector3(MeasurementTool.byteToAngle(angle_x) + 90f, MeasurementTool.byteToAngle(angle_y),
            MeasurementTool.byteToAngle(angle_z));
    public static (byte angle_x, byte angle_y, byte angle_z) EulerToBytes(Vector3 euler)
        => (MeasurementTool.angleToByte(euler.x), MeasurementTool.angleToByte(euler.y),
            MeasurementTool.angleToByte(euler.z));

    // ReSharper restore InconsistentNaming
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
    public static bool AlmostEquals(this float left, float right, float tolerance = 0.05f)
    {
        return Mathf.Abs(left - right) < tolerance;
    }
    public static Schema GetForeignKeyListSchema(string tableName, string pkColumn, string valueColumn, string primaryTableName, string primaryTablePkColumn, string? foreignTableName, string? foreignTablePkColumn, bool hasPk = false, bool oneToOne = false, bool nullable = false, bool unique = false, string pkName = "pk", ConstraintBehavior deleteBehavior = ConstraintBehavior.NoAction, ConstraintBehavior updateBehavior = ConstraintBehavior.NoAction)
    {
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
        columns[++index] = new Schema.Column(valueColumn, SqlTypes.INCREMENT_KEY)
        {
            Nullable = nullable,
            UniqueKey = unique,
            ForeignKey = foreignTableName != null && foreignTablePkColumn != null,
            ForeignKeyColumn = foreignTablePkColumn,
            ForeignKeyTable = foreignTableName,
            ForeignKeyUpdateBehavior = updateBehavior,
            ForeignKeyDeleteBehavior = deleteBehavior
        };
        return new Schema(tableName, columns, false, typeof(PrimaryKey));
    }
    public static Schema GetListSchema<T>(string tableName, string pkColumn, string valueColumn, string primaryTableName, string primaryTablePkColumn, bool hasPk = false, bool oneToOne = false, int length = -1, bool nullable = false, bool unique = false, string pkName = "pk")
    {
        Type type = typeof(T);
        string typeStr;
        if (type == typeof(Guid))
            typeStr = SqlTypes.GUID;
        else if (type == typeof(ulong))
            typeStr = SqlTypes.ULONG;
        else if (type == typeof(byte[]))
            typeStr = length < 1 ? SqlTypes.BYTES_255 : "binary(" + length + ")";
        else if (type == typeof(string))
            typeStr = length < 1 ? SqlTypes.STRING_255 : "varchar(" + length + ")";
        else if (type == typeof(float))
            typeStr = SqlTypes.FLOAT;
        else if (type == typeof(double))
            typeStr = SqlTypes.DOUBLE;
        else if (type == typeof(long))
            typeStr = SqlTypes.LONG;
        else if (type == typeof(uint))
            typeStr = SqlTypes.UINT;
        else if (type == typeof(int))
            typeStr = SqlTypes.INT;
        else if (type == typeof(short))
            typeStr = SqlTypes.SHORT;
        else if (type == typeof(ushort))
            typeStr = SqlTypes.USHORT;
        else if (type == typeof(byte))
            typeStr = SqlTypes.BYTE;
        else if (type == typeof(sbyte))
            typeStr = SqlTypes.SBYTE;
        else if (type == typeof(bool))
            typeStr = SqlTypes.BOOLEAN;
        else if (type == typeof(PrimaryKey))
            typeStr = SqlTypes.INCREMENT_KEY;
        else
        {
            MethodInfo? info = type.GetMethod("GetDefaultSchema", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
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

        return GetListSchema(typeStr, tableName, pkColumn, valueColumn, primaryTableName, primaryTablePkColumn, typeof(T), hasPk, oneToOne, nullable, unique, pkName);
    }
    public static Schema GetListSchema(string typeStr, string tableName, string pkColumn, string valueColumn, string primaryTableName, string primaryTablePkColumn, Type? type, bool hasPk = false, bool oneToOne = false, bool nullable = false, bool unique = false, string pkName = "pk")
    {
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
        columns[++index] = new Schema.Column(valueColumn, typeStr)
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
    public static void ReadToTranslationList(MySqlDataReader reader, TranslationList list, int colOffset = 0)
    {
        if (list is null)
            throw new ArgumentNullException(nameof(list));
        string lang = reader.GetString(colOffset + 1).ToLowerInvariant();
        if (list.ContainsKey(lang))
        {
            L.LogWarning("Duplicate language entry found for TranslationList with entry #" + reader.GetInt32(0) +
                         " (" + reader.GetColumnSchema().FirstOrDefault()?.ColumnName + "). " +
                         "Value (\"" + reader.GetString(colOffset + 2) + "\") is being ignored.");
        }
        else list.Add(lang, reader.GetString(colOffset + 2));
    }
    public static ItemJarData[] GetItemsFromStorageState(ItemStorageAsset storage, byte[] state, out ItemDisplayData? displayData, PrimaryKey parent, bool clientState = false)
    {
        if (!Level.isLoaded)
            throw new Exception("Level not loaded.");
        ThreadUtil.assertIsGameThread();
        if (state.Length < 17)
        {
            displayData = null;
            return Array.Empty<ItemJarData>();
        }
        Block block = new Block(state);
        block.step += sizeof(ulong) * 2;
        ItemJarData[] rtn;
        if (!clientState)
        {
            int ct = block.readByte();
            rtn = new ItemJarData[ct];
            for (int i = 0; i < ct; ++i)
            {
                if (BarricadeManager.version > 7)
                {
                    object[] objArray = block.read(Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE);
                    Guid guid = Assets.find(EAssetType.ITEM, (ushort)objArray[3]) is ItemAsset asset ? asset.GUID : new Guid((ushort)objArray[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    rtn[i] = new ItemJarData(PrimaryKey.NotAssigned, parent, guid,
                        (byte)objArray[0], (byte)objArray[1], (byte)objArray[2], (byte)objArray[4], (byte)objArray[5],
                        (byte[])objArray[6]);
                }
                else
                {
                    object[] objArray = block.read(Types.BYTE_TYPE, Types.BYTE_TYPE, Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE);
                    Guid guid = Assets.find(EAssetType.ITEM, (ushort)objArray[2]) is ItemAsset asset ? asset.GUID : new Guid((ushort)objArray[2], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    rtn[i] = new ItemJarData(PrimaryKey.NotAssigned, parent, guid,
                        (byte)objArray[0], (byte)objArray[1], 0, (byte)objArray[3], (byte)objArray[4],
                        (byte[])objArray[5]);
                }
            }
        }
        else rtn = Array.Empty<ItemJarData>();

        if (storage.isDisplay)
        {
            if (clientState)
            {
                object[] objArray = block.read(Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE, Types.UINT16_TYPE, Types.UINT16_TYPE, Types.STRING_TYPE, Types.STRING_TYPE, Types.BYTE_TYPE);
                displayData = new ItemDisplayData(parent, (ushort)objArray[3], (ushort)objArray[4], (byte)objArray[7], (string)objArray[5], (string)objArray[6]);
            }
            else
            {
                ushort skin = block.readUInt16();
                ushort mythic = block.readUInt16();
                string? tags;
                string? dynProps;
                if (BarricadeManager.version > 12)
                {
                    tags = block.readString();
                    if (tags.Length == 0)
                        tags = null;
                    dynProps = block.readString();
                    if (dynProps.Length == 0)
                        dynProps = null;
                }
                else
                {
                    tags = null;
                    dynProps = null;
                }
                byte rot = BarricadeManager.version > 8 ? block.readByte() : (byte)0;
                displayData = new ItemDisplayData(parent, skin, mythic, rot, tags, dynProps);
            }
        }
        else displayData = null;

        return rtn;
    }
    internal static void AppendPropertyList(StringBuilder builder, int startIndex, int length)
    {
        if (startIndex != 0)
            builder.Append(',');
        builder.Append('(');
        for (int j = startIndex; j < startIndex + length; ++j)
        {
            if (j != startIndex)
                builder.Append(',');
            builder.Append('@').Append(j.ToString(CultureInfo.InvariantCulture));
        }
        builder.Append(')');
    }
    internal static void AppendPropertyList(StringBuilder builder, int startIndex, int length, int i)
    {
        if (i != 0)
            builder.Append(',');
        builder.Append('(');
        for (int j = startIndex; j < startIndex + length; ++j)
        {
            if (j != startIndex)
                builder.Append(',');
            builder.Append('@').Append(j.ToString(CultureInfo.InvariantCulture));
        }
        builder.Append(')');
    }
    internal static void AppendPropertyList(StringBuilder builder, int startIndex, int length, int i, int clampLen)
    {
        if (i != 0)
            builder.Append(',');
        builder.Append('(');
        for (int j = 0; j < clampLen; ++j)
        {
            if (j != 0)
                builder.Append(',');
            builder.Append('@').Append(j.ToString(CultureInfo.InvariantCulture));
        }
        for (int j = startIndex; j < startIndex + length; ++j)
        {
            if (clampLen != 0 || j != startIndex)
                builder.Append(',');
            builder.Append('@').Append(j.ToString(CultureInfo.InvariantCulture));
        }
        builder.Append(')');
    }
    public static bool NullOrEmpty<T>(this ICollection<T>? collection)
    {
        return collection == null || collection.Count == 0;
    }
    public static int StringIndexOf<T>(IReadOnlyList<T> collection, Func<T, string?> selector, string input, bool equalsOnly = false)
    {
        if (input == null)
            return -1;

        for (int i = 0; i < collection.Count; ++i)
        {
            if (string.Equals(selector(collection[i]), input, StringComparison.InvariantCultureIgnoreCase))
                return i;
        }
        if (!equalsOnly)
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                string? n = selector(collection[i]);
                if (n != null && n.IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return i;
            }

            string[] inSplits = input.Split(SpaceSplit);
            for (int i = 0; i < collection.Count; ++i)
            {
                string? name = selector(collection[i]);
                if (name != null && inSplits.All(l => name.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1))
                    return i;
            }
        }

        return -1;
    }

    public static bool RoughlyEquals(string? a, string? b) => string.Compare(a, b, CultureInfo.InvariantCulture,
        CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace |
        CompareOptions.IgnoreSymbols) == 0;
    public static bool RoughlyContains(string? a, string? b) => a != null && b != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(a, b,
        CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols) >= 0;
    public static T? StringFind<T>(IReadOnlyList<T> collection, Func<T, string?> selector, string input, bool equalsOnly = false)
    {
        if (input == null)
            return default;

        for (int i = 0; i < collection.Count; ++i)
        {
            if (string.Equals(selector(collection[i]), input, StringComparison.InvariantCultureIgnoreCase))
                return collection[i];
        }
        if (!equalsOnly)
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                string? n = selector(collection[i]);
                if (n != null && n.IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return collection[i];
            }

            string[] inSplits = input.Split(SpaceSplit);
            for (int i = 0; i < collection.Count; ++i)
            {
                string? name = selector(collection[i]);
                if (name != null && inSplits.All(l => name.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1))
                    return collection[i];
            }
        }

        return default;
    }
    public static T? StringFind<T, TKey>(IReadOnlyList<T> collection, Func<T, string?> selector, Func<T, TKey> orderBy, string input, bool equalsOnly = false, bool descending = false)
    {
        if (input == null)
            return default;
        T[] buffer = (descending ? collection.OrderByDescending(orderBy) : collection.OrderBy(orderBy)).ToArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (string.Equals(selector(buffer[i]), input, StringComparison.InvariantCultureIgnoreCase))
                return buffer[i];
        }

        if (!equalsOnly)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                string? n = selector(buffer[i]);
                if (n != null && n.IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return buffer[i];
            }

            string[] inSplits = input.Split(SpaceSplit);
            for (int i = 0; i < buffer.Length; i++)
            {
                string? name = selector(buffer[i]);
                if (name != null && inSplits.All(l => name.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1))
                    return buffer[i];
            }
        }

        return default;
    }
    public static T? StringFind<T, TKey, TKey2>(IReadOnlyList<T> collection, Func<T, string?> selector, Func<T, TKey> orderBy, Func<T, TKey2> thenBy, string input, bool equalsOnly = false, bool descending = false, bool thenDescending = false)
    {
        if (input == null)
            return default;
        IOrderedEnumerable<T> e = descending ? collection.OrderByDescending(orderBy) : collection.OrderBy(orderBy);
        e = thenDescending ? e.ThenByDescending(thenBy) : e.ThenBy(thenBy);
        T[] buffer = e.ToArray();
        for (int i = 0; i < buffer.Length; i++)
        {
            if (string.Equals(selector(buffer[i]), input, StringComparison.InvariantCultureIgnoreCase))
                return buffer[i];
        }

        if (!equalsOnly)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                string? n = selector(buffer[i]);
                if (n != null && n.IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                    return buffer[i];
            }

            string[] inSplits = input.Split(SpaceSplit);
            for (int i = 0; i < buffer.Length; i++)
            {
                string? name = selector(buffer[i]);
                if (name != null && inSplits.All(l => name.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1))
                    return buffer[i];
            }
        }

        return default;
    }
    public static void StringSearch<T>(IReadOnlyList<T> collection, IList<T> output, Func<T, string?> selector, string input, bool equalsOnly = false)
    {
        if (input == null)
            return;

        for (int i = 0; i < collection.Count; ++i)
        {
            T value = collection[i];
            if (string.Equals(selector(value), input, StringComparison.InvariantCultureIgnoreCase) && !output.Contains(value))
                output.Add(value);
        }
        if (!equalsOnly)
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                T value = collection[i];
                string? name = selector(value);
                if (name != null && !output.Contains(value) && name.IndexOf(input, StringComparison.InvariantCultureIgnoreCase) != -1)
                    output.Add(value);
            }

            string[] inSplits = input.Split(SpaceSplit);
            for (int i = 0; i < collection.Count; ++i)
            {
                T value = collection[i];
                string? name = selector(value);
                if (name != null && !output.Contains(value) && inSplits.All(l => name.IndexOf(l, StringComparison.InvariantCultureIgnoreCase) != -1))
                    output.Add(value);
            }
        }
    }
    public static string ActionLogDisplay(this Asset asset) =>
        $"{asset.FriendlyName} / {asset.id.ToString(Data.AdminLocale)} / {asset.GUID:N}";
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="type"/> is not a valid value.</exception>
    public static EItemType GetItemType(this ClothingType type) => type switch
    {
        ClothingType.Shirt => EItemType.SHIRT,
        ClothingType.Pants => EItemType.PANTS,
        ClothingType.Vest => EItemType.VEST,
        ClothingType.Hat => EItemType.HAT,
        ClothingType.Mask => EItemType.MASK,
        ClothingType.Backpack => EItemType.BACKPACK,
        ClothingType.Glasses => EItemType.GLASSES,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
    public static T[] CloneArray<T>(T[] source, int index = 0, int length = -1) where T : ICloneable
    {
        if (source == null)
            return null!;
        if (source.Length == 0)
            return Array.Empty<T>();
        if (index >= source.Length)
            index = source.Length - 1;
        if (length < 0 || length + index > source.Length)
            length = source.Length - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        for (int i = 0; i < length; ++i)
            result[i] = (T)source[i + index].Clone();
        return result;
    }
    public static T[] CloneStructArray<T>(T[] source, int index = 0, int length = -1) where T : struct
    {
        if (source == null)
            return null!;
        if (source.Length == 0)
            return Array.Empty<T>();
        if (index >= source.Length)
            index = source.Length - 1;
        if (length < 0 || length + index > source.Length)
            length = source.Length - index;
        if (length == 0)
            return Array.Empty<T>();
        T[] result = new T[length];
        Array.Copy(source, index, result, 0, length);
        return result;
    }
    public static bool ServerTrackQuest(this UCPlayer player, QuestAsset quest)
    {
        ThreadUtil.assertIsGameThread();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset? current = player.Player.quests.GetTrackedQuest();
        if (current != null && quest != null)
        {
            if (current.GUID == quest.GUID && !player.Save.TrackQuests)
                player.Player.quests.ServerAddQuest(quest);

            return false;
        }
        if (player.Save.TrackQuests)
            player.Player.quests.ServerAddQuest(quest);
        return true;
    }
    public static bool ServerUntrackQuest(this UCPlayer player, QuestAsset quest)
    {
        ThreadUtil.assertIsGameThread();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset current = player.Player.quests.GetTrackedQuest();
        if (current == quest)
            return false;

        if (player.Player.quests.GetTrackedQuest() == quest)
            player.Player.quests.TrackQuest(null);
        return true;
    }
    public static void CombineIfNeeded(this ref CancellationToken token, CancellationToken other)
    {
        if (token == other)
            return;
        token = token.CanBeCanceled && token != other ? CancellationTokenSource.CreateLinkedTokenSource(token, other).Token : token;
    }
    public static void CombineIfNeeded(this ref CancellationToken token, CancellationToken other1, CancellationToken other2)
    {
        if (token.CanBeCanceled)
        {
            if (other1.CanBeCanceled)
            {
                if (other1 == other2)
                {
                    if (token == other1)
                        return;

                    token = other2.CanBeCanceled
                        ? CancellationTokenSource.CreateLinkedTokenSource(token).Token : token;
                    return;
                }
                token = other2.CanBeCanceled
                    ? CancellationTokenSource.CreateLinkedTokenSource(token, other1, other2).Token
                    : CancellationTokenSource.CreateLinkedTokenSource(token, other1).Token;
            }
            else
            {
                if (token == other2)
                    return;

                token = other2.CanBeCanceled
                    ? CancellationTokenSource.CreateLinkedTokenSource(token, other2).Token : token;
            }
        }
        else
        {
            if (other1.CanBeCanceled)
            {
                if (other1 == other2)
                {
                    token = other1;
                }
                else
                {
                    token = other2.CanBeCanceled
                        ? CancellationTokenSource.CreateLinkedTokenSource(other1, other2).Token : other1;
                }
            }
            else if (other2.CanBeCanceled)
            {
                if (token == other2)
                    return;
                token = other2;
            }
        }
    }

    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>[,`<paramref name="columnPk"/>`]) VALUES (parameters[,LAST_INSERT_ID(@pk)]) ON DUPLICATE KEY UPDATE (<paramref name="columns"/>,`<paramref name="columnPk"/>`=LAST_INSERT_ID(`<paramref name="columnPk"/>`);<br/>SET @pk := (SELECT LAST_INSERT_ID() as `pk`);<br/>SELECT @pk</summary>
    public static string BuildInitialInsertQuery(string table, string columnPk, bool hasPk, string? extPk, string[]? deleteTables, params string[] columns)
    {
        return "INSERT INTO `" + table + "` (" + SqlTypes.ColumnList(columns) +
            (hasPk ? $",`{columnPk}`" : string.Empty) +
            ") VALUES (" + SqlTypes.ParameterList(0, columns.Length) +
            (hasPk ? ",LAST_INSERT_ID(@" + columns.Length.ToString(Data.AdminLocale) + ")" : string.Empty) +
            ") ON DUPLICATE KEY UPDATE " +
            SqlTypes.ColumnUpdateList(0, columns) +
            $",`{columnPk}`=LAST_INSERT_ID(`{columnPk}`);" +
            "SET @pk := (SELECT LAST_INSERT_ID() as `pk`);" + (hasPk && extPk != null && deleteTables != null ? GetDeleteText(deleteTables, extPk, columns.Length) : string.Empty) +
            " SELECT @pk;";
    }
    private static string GetDeleteText(string[] deleteTables, string columnPk, int pk)
    {
        StringBuilder sb = new StringBuilder(deleteTables.Length * 15);
        for (int i = 0; i < deleteTables.Length; ++i)
            sb.Append("DELETE FROM `").Append(deleteTables[i]).Append("` WHERE `").Append(columnPk).Append("`=@").Append(pk.ToString(Data.AdminLocale)).Append(';');
        return sb.ToString();
    }
    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>) VALUES (parameters);</summary>
    public static string BuildOtherInsertQueryNoUpdate(string table, params string[] columns)
    {
        return $"INSERT INTO `{table}` (" +
               SqlTypes.ColumnList(columns) +
               ") VALUES (" + SqlTypes.ParameterList(0, columns.Length) + ");";
    }
    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>) VALUES </summary>
    public static string StartBuildOtherInsertQueryNoUpdate(string table, params string[] columns)
    {
        return $"INSERT INTO `{table}` (" + SqlTypes.ColumnList(columns) + ") VALUES ";
    }
    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>) VALUES (parameters) ON DUPLICATE KEY UPDATE (<paramref name="columns"/> without first column);</summary>
    /// <remarks>Assumes pk is first column.</remarks>
    public static string BuildOtherInsertQueryUpdate(string table, params string[] columns)
    {
        return $"INSERT INTO `{table}` (" +
               SqlTypes.ColumnList(columns) +
               ") VALUES (" + SqlTypes.ParameterList(0, columns.Length) + ") ON DUPLICATE KEY UPDATE " + 
               SqlTypes.ColumnUpdateList(1, 1, columns) + ";";
    }
    /// <summary> ON DUPLICATE KEY UPDATE (<paramref name="columns"/> without first column);</summary>
    /// <remarks>Assumes pk is first column.</remarks>
    public static string EndBuildOtherInsertQueryUpdate(params string[] columns)
    {
        return " ON DUPLICATE KEY UPDATE " + SqlTypes.ColumnUpdateList(1, 1, columns) + ";";
    }
    /// <summary>SELECT <paramref name="columns"/> FROM `<paramref name="table"/>` WHERE `<paramref name="checkColumnEquals"/>`=@<paramref name="parameter"/>;</summary>
    public static string BuildSelectWhere(string table, string checkColumnEquals, int parameter, params string[] columns)
    {
        return "SELECT " + SqlTypes.ColumnList(columns) +
               $" FROM `{table}` WHERE `{checkColumnEquals}`=@" + parameter.ToString(Data.AdminLocale) + ";";
    }
    /// <summary>SELECT <paramref name="columns"/> FROM `<paramref name="table"/>` WHERE `<paramref name="checkColumnEquals"/>` IN (</summary>
    public static string BuildSelectWhereIn(string table, string checkColumnEquals, params string[] columns)
    {
        return "SELECT " + SqlTypes.ColumnList(columns) + $" FROM `{table}` WHERE `{checkColumnEquals}` IN (";
    }
    /// <summary>SELECT <paramref name="columns"/> FROM `<paramref name="table"/>` WHERE `<paramref name="checkColumnEquals"/>`=@<paramref name="parameter"/> LIMIT 1;</summary>
    public static string BuildSelectWhereLimit1(string table, string checkColumnEquals, int parameter, params string[] columns)
    {
        return "SELECT " + SqlTypes.ColumnList(columns) +
               $" FROM `{table}` WHERE `{checkColumnEquals}`=@" + parameter.ToString(Data.AdminLocale) + " LIMIT 1;";
    }
    /// <summary>SELECT <paramref name="columns"/> FROM `<paramref name="table"/>`;</summary>
    public static string BuildSelect(string table, params string[] columns)
    {
        return "SELECT " + SqlTypes.ColumnList(columns) + $" FROM `{table}`;";
    }
    public static Task<int> DeleteItem(MySqlDatabase data, PrimaryKey pk, string tableMain, string columnPk, CancellationToken token = default)
    {
        if (!pk.IsValid)
            throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
        return data.NonQueryAsync($"DELETE FROM `{tableMain}` WHERE `{columnPk}`=@0;", new object[] { pk.Key }, token);
    }
    public static bool IsDefault(this string str) => str.Equals(L.Default, StringComparison.OrdinalIgnoreCase);
    
    public static T[] AsArrayFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (enumerable == null)
            return Array.Empty<T>();
        if (enumerable is T[] arr1)
        {
            if (arr1.Length == 0)
                return Array.Empty<T>();
            if (copy)
            {
                T[] arr2 = new T[arr1.Length];
                Array.Copy(arr1, 0, arr2, 0, arr1.Length);
                return arr2;
            }

            return arr1;
        }
        if (enumerable is List<T> list1)
            return list1.Count == 0 ? Array.Empty<T>() : list1.ToArray();
        if (enumerable is ICollection<T> col1)
        {
            if (col1.Count == 0)
                return Array.Empty<T>();
            T[] arr2 = new T[col1.Count];
            col1.CopyTo(arr2, 0);
            return arr2;
        }

        return enumerable.ToArray();
    }
    public static List<T> ToListFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (!copy && enumerable is List<T> list)
            return list;
        return new List<T>(enumerable);
    }
    /// <summary>
    /// Takes a list of primary key pairs and calls <paramref name="action"/> for each array of values per id.
    /// </summary>
    /// <remarks>The values should be sorted by ID, if not set <paramref name="sort"/> to <see langword="true"/>.</remarks>
    public static void ApplyQueriedList<T>(List<PrimaryKeyPair<T>> list, Action<int, T[]> action, bool sort = true)
    {
        if (list.Count == 0) return;

        if (sort && list.Count != 1)
            list.Sort((a, b) => a.Key.CompareTo(b.Key));

        T[] arr;
        int key;
        int last = -1;
        for (int i = 0; i < list.Count; i++)
        {
            PrimaryKeyPair<T> val = list[i];
            if (i <= 0 || list[i - 1].Key == val.Key)
                continue;

            arr = new T[i - 1 - last];
            for (int j = 0; j < arr.Length; ++j)
                arr[j] = list[last + j + 1].Value;
            last = i - 1;

            key = list[i - 1].Key;
            action(key, arr);
        }

        arr = new T[list.Count - 1 - last];
        for (int j = 0; j < arr.Length; ++j)
            arr[j] = list[last + j + 1].Value;

        key = list[list.Count - 1].Key;
        action(key, arr);
    }

    public static T? FindIndexed<T>(this T[] array, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (predicate(array[i], i))
                return array[i];
        }

        return default;
    }
    public static T? FindIndexed<T>(this T[] array, out int index, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (predicate(array[i], i))
            {
                index = i;
                return array[i];
            }
        }

        index = -1;
        return default;
    }
    public static T? FindIndexed<T>(this IList<T> array, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Count; ++i)
        {
            if (predicate(array[i], i))
                return array[i];
        }

        return default;
    }
    public static T? FindIndexed<T>(this IList<T> array, out int index, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Count; ++i)
        {
            if (predicate(array[i], i))
            {
                index = i;
                return array[i];
            }
        }

        index = -1;
        return default;
    }
    public static async UniTask<string?> GetProfilePictureURL(ulong steam64, AvatarSize size, CancellationToken token = default)
    {
        if (!UCWarfare.IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));
        if (UCPlayer.FromID(steam64) is { } player)
        {
            return await player.GetProfilePictureURL(size, token);
        }

        if (Data.ModerationSql.TryGetAvatar(steam64, size, out string url))
            return url;

        PlayerSummary summary = await GetPlayerSummary(steam64, token: token);

        return size switch
        {
            AvatarSize.Full => summary.AvatarUrlFull,
            AvatarSize.Medium => summary.AvatarUrlMedium,
            _ => summary.AvatarUrlSmall
        };
    }
    public static async UniTask<PlayerSummary> GetPlayerSummary(ulong steam64, bool allowCache = true, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded && UCPlayer.FromID(steam64) is { } player)
        {
            return await player.GetPlayerSummary(allowCache, token);
        }

        PlayerSummary? playerSummary = await Networking.SteamAPI.GetPlayerSummary(steam64, token);
        await UniTask.SwitchToMainThread(token);
#if DEBUG
        ThreadUtil.assertIsGameThread();
#endif

        if (playerSummary != null && UCWarfare.IsLoaded)
        {
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlSmall))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Small, playerSummary.AvatarUrlSmall);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlMedium))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Medium, playerSummary.AvatarUrlMedium);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlFull))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Full, playerSummary.AvatarUrlFull);
        }

        return playerSummary ?? new PlayerSummary
        {
            Steam64 = steam64,
            PlayerName = steam64.ToString()
        };
    }
    public static async UniTask CacheAvatars(this DatabaseInterface db, IEnumerable<ulong> players, CancellationToken token = default)
    {
        List<ulong> pls = new List<ulong>();
        foreach (ulong pl in players)
        {
            if (db.TryGetAvatar(pl, AvatarSize.Small, out _) && db.TryGetAvatar(pl, AvatarSize.Medium, out _) && db.TryGetAvatar(pl, AvatarSize.Full, out _))
                continue;

            pls.Add(pl);
        }

        if (pls.Count <= 0)
            return;
        PlayerSummary[] summaries = await Networking.SteamAPI.GetPlayerSummaries(pls, token);
        for (int i = 0; i < summaries.Length; ++i)
        {
            PlayerSummary summary = summaries[i];
            db.UpdateAvatar(summary.Steam64, AvatarSize.Small, summary.AvatarUrlSmall);
            db.UpdateAvatar(summary.Steam64, AvatarSize.Medium, summary.AvatarUrlMedium);
            db.UpdateAvatar(summary.Steam64, AvatarSize.Full, summary.AvatarUrlFull);
        }
    }
}

public readonly struct PrimaryKeyPair<T>
{
    public int Key { get; }
    public T Value { get; }
    public PrimaryKeyPair(int key, T value)
    {
        Key = key;
        Value = value;
    }

    public override string ToString() => $"({{{Key}}}, {(Value is null ? "NULL" : Value.ToString())})";
}