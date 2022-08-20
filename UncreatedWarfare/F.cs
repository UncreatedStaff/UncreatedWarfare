using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare;

public static class F
{
    private static readonly Regex RemoveRichTextRegex = new Regex("(?<!(?:\\<noparse\\>(?!\\<\\/noparse\\>)).*)\\<\\/{0,1}(?:(?:color=\\\"{0,1}[#a-z]{0,9}\\\"{0,1})|(?:color)|(?:size=\\\"{0,1}\\d+\\\"{0,1})|(?:size)|(?:alpha)|(?:alpha=#[0-f]{1,2})|(?:#.{3,8})|(?:[isub])|(?:su[pb])|(?:lowercase)|(?:uppercase)|(?:smallcaps))\\>", RegexOptions.IgnoreCase);
    private static readonly Regex RemoveTMProRichTextRegex = new Regex("(?<!(?:\\<noparse\\>(?!\\<\\/noparse\\>)).*)\\<\\/{0,1}(?:(?:noparse)|(?:alpha)|(?:alpha=#[0-f]{1,2})|(?:[su])|(?:su[pb])|(?:lowercase)|(?:uppercase)|(?:smallcaps))\\>", RegexOptions.IgnoreCase);
    private static readonly Regex TimeRegex = new Regex(@"(\d+)\s{0,1}([a-z]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    public static readonly char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u' };
    /// <summary>Convert an HTMLColor string to a actual color.</summary>
    /// <param name="htmlColorCode">A hexadecimal/HTML color key.</param>
    public static Color Hex(this string htmlColorCode)
    {
        if (htmlColorCode.Length == 0) return Color.white;
        if (htmlColorCode[0] != '#')
            htmlColorCode = "#" + htmlColorCode;

        if (ColorUtility.TryParseHtmlString(htmlColorCode, out Color color))
            return color;
        else return Color.white;
    }
    public static string Hex(this Color color)
    {
        string hex = ((byte)Mathf.Clamp(color.r * byte.MaxValue, 0, byte.MaxValue)).ToString("X2", Data.Locale) + ((byte)Mathf.Clamp(color.g * byte.MaxValue, 0, byte.MaxValue)).ToString("X2", Data.Locale) + ((byte)Mathf.Clamp(color.b * byte.MaxValue, 0, byte.MaxValue)).ToString("X2", Data.Locale);
        if (color.a < 1f)
            hex += ((byte)Mathf.Clamp(color.a * byte.MaxValue, 0, byte.MaxValue)).ToString("X2", Data.Locale);
        return hex;
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
    public static ConsoleColor GetClosestConsoleColor(Color color)
    {
        int i = color.r > 0.5f || color.g > 0.5f || color.b > 0.5f ? 8 : 0;
        if (color.r > 0.5f) i |= 4;
        if (color.g > 0.5f) i |= 2;
        if (color.b > 0.5f) i |= 1;
        return (ConsoleColor)i;
    }
    public static Color GetColor(ConsoleColor color)
    {
        int c = (int)color;
        float r = 0f, g = 0f, b = 0f;
        if ((c & 8) == 8)
        {
            r += 0.5f;
            g += 0.5f;
            b += 0.5f;
        }
        if ((c & 4) == 4)
            r += 0.25f;
        if ((c & 2) == 2)
            g += 0.25f;
        if ((c & 1) == 1)
            b += 0.25f;
        return new Color(r, g, b);
    }
    public static string RemoveRichText(string text)
    {
        return RemoveRichTextRegex.Replace(text, string.Empty);
    }
    /// <remarks>Does not include &lt;#ffffff&gt; colors.</remarks>
    public static string RemoveTMProRichText(string text)
    {
        return RemoveTMProRichTextRegex.Replace(text, string.Empty);
    }
    public static byte[] CloneBytes(byte[] src)
    {
        int length = src.Length;
        byte[] output = new byte[length];
        Buffer.BlockCopy(src, 0, output, 0, length);
        return output;
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
        if (!int.TryParse(rtn, NumberStyles.HexNumber, Data.Locale, out _))
            return UCWarfare.GetColorHex("default");
        else return rtn;
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
    /// <summary>
    /// Check if <paramref name="permission"/> meets permission requirement <paramref name="check"/>.
    /// Set <paramref name="comparison"/> to specify how to compare the two permission values.<br/>Defaults to checking if <paramref name="permission"/> is at least <paramref name="check"/>. Each <see cref="PermissionComparison"/> has a description of how it works.</summary>
    /// <returns><see langword="true"/> if <paramref name="permission"/> meets the requirements of <paramref name="check"/> based on <paramref name="comparison"/>.</returns>
    public static bool IsOfPermission(this EAdminType permission, EAdminType check, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        switch (comparison)
        {
            case PermissionComparison.Exact:
                return permission == check;

            case PermissionComparison.MaskOverlaps:
                return check == EAdminType.MEMBER || (permission & check) > 0;

            default:
            case PermissionComparison.AtLeast:
                if (check is <= EAdminType.MEMBER || check == permission)
                    return true;

                if (check is EAdminType.CONSOLE)
                    return permission is >= EAdminType.CONSOLE;

                if (check is EAdminType.MODERATOR)
                    return (permission & EAdminType.MODERATOR) > 0;

                if (check is EAdminType.STAFF)
                    return permission > 0;

                if (check is EAdminType.VANILLA_ADMIN)
                    return permission is >= EAdminType.VANILLA_ADMIN;

                if (check is EAdminType.HELPER)
                    return permission is >= EAdminType.HELPER;

                if (check is EAdminType.ADMIN)
                    return (permission & (EAdminType.ADMIN_ON_DUTY | EAdminType.ADMIN_OFF_DUTY)) >= EAdminType.ADMIN_OFF_DUTY
                           || permission >= EAdminType.VANILLA_ADMIN;

                if (check is EAdminType.TRIAL_ADMIN)
                    return (permission & (EAdminType.TRIAL_ADMIN_ON_DUTY | EAdminType.TRIAL_ADMIN_OFF_DUTY | EAdminType.ADMIN_ON_DUTY | EAdminType.ADMIN_OFF_DUTY)) >= EAdminType.TRIAL_ADMIN_OFF_DUTY
                           || permission >= EAdminType.VANILLA_ADMIN;

                if (check is EAdminType.TRIAL_ADMIN_OFF_DUTY)
                    return permission is EAdminType.ADMIN_OFF_DUTY or EAdminType.TRIAL_ADMIN_OFF_DUTY;

                if (check is EAdminType.TRIAL_ADMIN_ON_DUTY)
                    return permission is EAdminType.ADMIN_ON_DUTY or EAdminType.TRIAL_ADMIN_ON_DUTY;

                if (check is EAdminType.ADMIN_OFF_DUTY)
                    return permission is EAdminType.ADMIN_OFF_DUTY;

                if (check is EAdminType.ADMIN_ON_DUTY)
                    return permission is EAdminType.ADMIN_ON_DUTY;

                goto case PermissionComparison.MaskOverlaps;

            case PermissionComparison.AtMost:
                if (permission is <= EAdminType.MEMBER || check == permission || check is >= EAdminType.CONSOLE || check is EAdminType.MODERATOR || check is EAdminType.STAFF)
                    return true;
                
                if (check is EAdminType.VANILLA_ADMIN)
                    return permission is <= EAdminType.VANILLA_ADMIN;

                if (check is EAdminType.ADMIN)
                    return permission is <= EAdminType.ADMIN_ON_DUTY;

                if (check is EAdminType.TRIAL_ADMIN)
                    return permission is <= EAdminType.TRIAL_ADMIN_ON_DUTY;

                if (check is EAdminType.HELPER or EAdminType.TRIAL_ADMIN_OFF_DUTY or EAdminType.TRIAL_ADMIN_ON_DUTY or EAdminType.ADMIN_OFF_DUTY or EAdminType.ADMIN_ON_DUTY)
                    return permission <= check;

                goto case PermissionComparison.MaskOverlaps;
        }
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
    public static unsafe string ToProperCase(this string input)
    {
        char[] output = new char[input.Length];
        fixed (char* p = input)
        {
            char last = ' ';
            for (int i = 0; i < input.Length; ++i)
            {
                char current = *(p + i);
                if (current is '_') output[i] = ' ';
                else if (last is ' ' or '_' or ',' or '.')
                    output[i] = char.ToUpperInvariant(current);
                else
                    output[i] = char.ToLowerInvariant(current);
                last = current;
            }
        }
        return new string(output);
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
    public static bool OffDuty(this ulong player)       => !OnDuty(player);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool OffDuty(this UCPlayer player)    => !OnDuty(player);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntern(this ulong player)      => PermissionCheck(player, EAdminType.TRIAL_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntern(this UCPlayer player)   => player.PermissionCheck(EAdminType.TRIAL_ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAdmin(this ulong player)       => PermissionCheck(player, EAdminType.ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAdmin(this UCPlayer player)    => player.PermissionCheck(EAdminType.ADMIN, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHelper(this ulong player)      => PermissionCheck(player, EAdminType.HELPER, PermissionComparison.MaskOverlaps);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHelper(this UCPlayer player)   => player.PermissionCheck(EAdminType.HELPER, PermissionComparison.MaskOverlaps);

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
    public static string An(this string word)
    {
        if (word.Length > 0)
        {
            char first = char.ToLower(word[0]);
            for (int i = 0; i < vowels.Length; i++)
                if (vowels[i] == first)
                    return "n";
        }
        return string.Empty;
    }
    public static string An(this char letter)
    {
        char let = char.ToLower(letter);
        for (int i = 0; i < vowels.Length; i++)
            if (vowels[i] == let)
                return "n";
        return string.Empty;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string S(this int number)   => number == 1 ? string.Empty : "s";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string S(this float number) => number == 1 ? string.Empty : "s";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string S(this uint number)  => number == 1 ? string.Empty : "s";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void S(this int number, ref string str, int index = 0)
    {
        if (number is 1) str = str.Insert(index, "s");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void S(this float number, ref string str, int index = 0)
    {
        if (number is 1) str = str.Insert(index, "s");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void S(this uint number, ref string str, int index = 0)
    {
        if (number is 1) str = str.Insert(index, "s");
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
                        newString.Append(buffer.ToString());
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
            if (pl == default)
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
                    throw new SingletonLoadException(ESingletonLoadType.LOAD, UCWarfare.I, ex);
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
                if (smallest == -1 || amt < smallest)
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
            call.Invoke(Data.NetClient!);
    }
    public static void NetInvoke<T>(this NetCallRaw<T> call, T arg)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg);
    }
    public static void NetInvoke<T1, T2>(this NetCallRaw<T1, T2> call, T1 arg1, T2 arg2)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2);
    }
    public static void NetInvoke<T1, T2, T3>(this NetCallRaw<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3);
    }
    public static void NetInvoke<T1, T2, T3, T4>(this NetCallRaw<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4);
    }
    public static void NetInvoke<T1>(this NetCall<T1> call, T1 arg1)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1);
    }
    public static void NetInvoke<T1, T2>(this NetCall<T1, T2> call, T1 arg1, T2 arg2)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2);
    }
    public static void NetInvoke<T1, T2, T3>(this NetCall<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3);
    }
    public static void NetInvoke<T1, T2, T3, T4>(this NetCall<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5>(this NetCall<T1, T2, T3, T4, T5> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6>(this NetCall<T1, T2, T3, T4, T5, T6> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7>(this NetCall<T1, T2, T3, T4, T5, T6, T7> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (UCWarfare.CanUseNetCall)
            call.Invoke(Data.NetClient!, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
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
        int counter = 0;
        int alphanumcount = 0;
        while (charenum.MoveNext())
        {
            counter++;
            char ch = charenum.Current;
            int c = ch;
            if (c > 31 && c < 127)
            {
                if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                {
                    final = original;
                    charenum.Dispose();
                    return false;
                }
                else
                {
                    alphanumcount++;
                }
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
    public static DateTime FromUnityTime(this float realtimeSinceStartup) => 
        DateTime.Now - TimeSpan.FromSeconds(Time.realtimeSinceStartup) + TimeSpan.FromSeconds(realtimeSinceStartup);

    /// <summary>
    /// Finds the 2D distance between two Vector3's x and z components.
    /// </summary>
    public static float SqrDistance2D(Vector3 a, Vector3 b) => Mathf.Pow(b.x - a.x, 2) + Mathf.Pow(b.z - a.z, 2);
    public static bool TryParseAny(string input, Type type, out object value)
    {
        value = null!;
        if (input is null || type is null || string.IsNullOrEmpty(input)) return false;
        if (type.IsClass)
        {
            if (type == typeof(string))
            {
                value = input;
                return true;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonAssetReference<>))
            {
                if (Guid.TryParse(input, out Guid guid))
                {
                    value = Activator.CreateInstance(type, guid);
                    return value is not null;
                }
                else if (ushort.TryParse(input, NumberStyles.Any, Data.Locale, out ushort id))
                {
                    value = Activator.CreateInstance(type, id);
                    return value is not null;
                }
                else if (input.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    value = Activator.CreateInstance(type);
                    return value is not null;
                }
            }
            return false;
        }
        if (type.IsEnum)
        {
            try
            {
                value = Enum.Parse(type, input, true);
                return value is not null;
            }
            catch
            {
                return false;
            }
        }
        if (type.IsPrimitive)
        {
            if (type == typeof(ulong))
            {
                bool res = ulong.TryParse(input, NumberStyles.Any, Data.Locale, out ulong v2);
                value = v2;
                return res;
            }
            else if (type == typeof(float))
            {
                bool res = float.TryParse(input, NumberStyles.Any, Data.Locale, out float v2);
                value = v2;
                return res;
            }
            else if (type == typeof(long))
            {
                bool res = long.TryParse(input, NumberStyles.Any, Data.Locale, out long v2);
                value = v2;
                return res;
            }
            else if (type == typeof(ushort))
            {
                bool res = ushort.TryParse(input, NumberStyles.Any, Data.Locale, out ushort v2);
                value = v2;
                return res;
            }
            else if (type == typeof(short))
            {
                bool res = short.TryParse(input, NumberStyles.Any, Data.Locale, out short v2);
                value = v2;
                return res;
            }
            else if (type == typeof(byte))
            {
                bool res = byte.TryParse(input, NumberStyles.Any, Data.Locale, out byte v2);
                value = v2;
                return res;
            }
            else if (type == typeof(int))
            {
                bool res = int.TryParse(input, NumberStyles.Any, Data.Locale, out int v2);
                value = v2;
                return res;
            }
            else if (type == typeof(uint))
            {
                bool res = uint.TryParse(input, NumberStyles.Any, Data.Locale, out uint v2);
                value = v2;
                return res;
            }
            else if (type == typeof(bool))
            {
                if (
                    input.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("t", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }
                else if (
                    input.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("f", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }
                return false;
            }
            else if (type == typeof(char))
            {
                if (input.Length == 1)
                {
                    value = input[0];
                    return true;
                }
                return false;
            }
            else if (type == typeof(sbyte))
            {
                bool res = sbyte.TryParse(input, NumberStyles.Any, Data.Locale, out sbyte v2);
                value = v2;
                return res;
            }
            else if (type == typeof(double))
            {
                bool res = double.TryParse(input, NumberStyles.Any, Data.Locale, out double v2);
                value = v2;
                return res;
            }
            return false;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (input.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                value = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(type));
                return value is not null;
            }
            Type @internal = type.GenericTypeArguments[0];
            if (!@internal.IsGenericType && TryParseAny(input, @internal, out object val) && val is not null)
            {
                value = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(type), val);
                return value is not null;
            }
            return false;
        }
        if (type == typeof(decimal))
        {
            bool res = decimal.TryParse(input, NumberStyles.Any, Data.Locale, out decimal v2);
            value = v2;
            return res;
        }
        else if (type == typeof(DateTime))
        {
            bool res = DateTime.TryParse(input, Data.Locale, DateTimeStyles.AssumeLocal, out DateTime v2);
            value = v2;
            return res;
        }
        else if (type == typeof(TimeSpan))
        {
            bool res = TimeSpan.TryParse(input, Data.Locale, out TimeSpan v2);
            value = v2;
            return res;
        }
        else if (type == typeof(Guid))
        {
            bool res = Guid.TryParse(input, out Guid v2);
            value = v2;
            return res;
        }
        else if (type == typeof(Vector2))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, Data.Locale, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 2)
            {
                value = new Vector2(vals[0], vals[1]);
                return true;
            }
            return false;
        }
        else if (type == typeof(Vector3))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, Data.Locale, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 3)
            {
                value = new Vector3(vals[0], vals[1], vals[2]);
                return true;
            }
            return false;
        }
        else if (type == typeof(Vector4))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, Data.Locale, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Vector4(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            return false;
        }
        else if (type == typeof(Quaternion))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, Data.Locale, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Quaternion(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            return false;
        }
        else if (type == typeof(Color))
        {
            if (ColorUtility.TryParseHtmlString(input, out Color color))
            {
                value = color;
                return true;
            }
            if (input[0] != '#')
            {
                input = "#" + input;
                if (ColorUtility.TryParseHtmlString(input, out color))
                {
                    value = color;
                    return true;
                }
            }
            return false;
        }
        return false;
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
    public static InstanceSetter<TInstance, TValue> GenerateInstanceSetter<TInstance, TValue>(string fieldName, BindingFlags flags)
    {
        flags |= BindingFlags.Instance;
        flags &= ~BindingFlags.Static;
        FieldInfo? field = typeof(TInstance).GetField(fieldName, flags);
        if (field is null || field.IsStatic || !field.FieldType.IsAssignableFrom(typeof(TValue)))
            throw new FieldAccessException("Field not found or invalid.");
        MethodAttributes attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, CallingConventions.HasThis, typeof(void), new Type[] { typeof(TInstance), field.FieldType }, typeof(TInstance), true);
        ILGenerator il = method.GetILGenerator();
        method.DefineParameter(1, ParameterAttributes.None, "value");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
        return (InstanceSetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceSetter<TInstance, TValue>));
    }
    public static InstanceGetter<TInstance, TValue> GenerateInstanceGetter<TInstance, TValue>(string fieldName, BindingFlags flags)
    {
        flags |= BindingFlags.Instance;
        flags &= ~BindingFlags.Static;
        FieldInfo? field = typeof(TInstance).GetField(fieldName, flags);
        if (field is null || field.IsStatic || !field.FieldType.IsAssignableFrom(typeof(TValue)))
            throw new FieldAccessException("Field not found or invalid.");
        MethodAttributes attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, CallingConventions.HasThis, typeof(TValue), new Type[] { typeof(TInstance) }, typeof(TInstance), true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);
        return (InstanceGetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceGetter<TInstance, TValue>));
    }
    public static StaticSetter<TValue> GenerateStaticSetter<TInstance, TValue>(string fieldName, BindingFlags flags)
    {
        flags |= BindingFlags.Static;
        flags &= ~BindingFlags.Instance;
        FieldInfo? field = typeof(TInstance).GetField(fieldName, flags);
        if (field is null || !field.IsStatic || !field.FieldType.IsAssignableFrom(typeof(TValue)))
            throw new FieldAccessException("Field not found or invalid.");
        MethodAttributes attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, CallingConventions.Standard, typeof(void), new Type[] { field.FieldType }, typeof(TInstance), true);
        ILGenerator il = method.GetILGenerator();
        method.DefineParameter(0, ParameterAttributes.None, "value");
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
        return (StaticSetter<TValue>)method.CreateDelegate(typeof(StaticSetter<TValue>));
    }
    public static StaticGetter<TValue> GenerateStaticGetter<TInstance, TValue>(string fieldName, BindingFlags flags)
    {
        flags |= BindingFlags.Static;
        flags &= ~BindingFlags.Instance;
        FieldInfo? field = typeof(TInstance).GetField(fieldName, flags);
        if (field is null || !field.IsStatic || !field.FieldType.IsAssignableFrom(typeof(TValue)))
            throw new FieldAccessException("Field not found or invalid.");
        MethodAttributes attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, CallingConventions.Standard, typeof(TValue), Array.Empty<Type>(), typeof(TInstance), true);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldsfld, field);
        il.Emit(OpCodes.Ret);
        return (StaticGetter<TValue>)method.CreateDelegate(typeof(StaticGetter<TValue>));
    }

    /// <returns>Total amount of time in seconds. <see langword="-1"/> is returned if <paramref name="input"/> is permanent.</returns>
    public static int ParseTime(string input)
    {
        if (input.StartsWith("perm", StringComparison.OrdinalIgnoreCase))
            return -1;
        if (int.TryParse(input, NumberStyles.Number, Data.Locale, out int time) && time > -1)
            return time * 60;
        foreach (Match match in TimeRegex.Matches(input))
        {
            if (match.Groups.Count != 3) continue;
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Number, Data.Locale, out int t)) continue;
            string key = match.Groups[2].Value;

            if (key.StartsWith("ms", StringComparison.OrdinalIgnoreCase))
                time += Mathf.RoundToInt(t / 1000f);
            else if (key.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                time += t;
            else if (key.StartsWith("mo", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 2565000); // 29.6875 days (356.25 / 12)
            else if (key.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 60);
            else if (key.StartsWith("h", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 3600);
            else if (key.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 86400);
            else if (key.StartsWith("w", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 604800);
            else if (key.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                time += checked(t * 30780000);
            else continue;
        }
        return time;
    }
    /// <remarks>More precise than <see cref="ParseTime(string)"/>, can go down to milliseconds.</remarks>
    /// <returns>Total amount of time. <see cref="TimeSpan.MaxValue"/> is returned if <paramref name="input"/> is permanent.</returns>
    public static TimeSpan ParseTimespan(string input)
    {
        if (input.StartsWith("perm", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.MaxValue;

        if (int.TryParse(input, NumberStyles.Number, Data.Locale, out int mins) && mins > -1)
            return TimeSpan.FromMinutes(mins);

        TimeSpan time = TimeSpan.Zero;
        foreach (Match match in TimeRegex.Matches(input))
        {
            if (match.Groups.Count != 3) continue;
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.Number, Data.Locale, out int t)) continue;
            string key = match.Groups[2].Value;

            if (key.StartsWith("ms", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromMilliseconds(t);
            else if (key.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromSeconds(t);
            else if (key.StartsWith("mo", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromSeconds(checked(t * 2565000)); // 29.6875 days (356.25 / 12)
            else if (key.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromMinutes(t);
            else if (key.StartsWith("h", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromHours(t);
            else if (key.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(t);
            else if (key.StartsWith("w", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(checked(t * 7));
            else if (key.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(checked(t * 365.25));
            else continue;
        }
        return time;
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
}
public delegate void InstanceSetter<T1, T2>(T1 owner, T2 value);
public delegate T2 InstanceGetter<T1, T2>(T1 owner);
public delegate void StaticSetter<T>(T value);
public delegate T StaticGetter<T>();

public enum PermissionComparison : byte
{
    /// <summary>Will match any permission level exactly the same as the provided value.</summary>
    Exact,
    /// <summary>Will match any permission level at or above the provided value.
    /// <br/>Admin types specifying a duty mode will only match those on duty, use <see cref="EAdminType.ADMIN"/> or <see cref="EAdminType.TRIAL_ADMIN"/> to match any at or above admin or trial admin (respectively)</summary>
    AtLeast,
    /// <summary>Will match any permission level up to the provided value.</summary>
    AtMost,
    /// <summary>Will match <code>EAdminType.VANILLA_ADMIN | EAdminType.TRIAL_ADMIN_OFF_DUTY to EAdminType.TRIAL_ADMIN_OFF_DUTY | TRIAL_ADMIN_ON_DUTY</code></summary>
    MaskOverlaps
}