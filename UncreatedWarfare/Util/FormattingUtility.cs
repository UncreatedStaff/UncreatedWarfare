using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Util;
public static class FormattingUtility
{
    internal static char[][]? AllRichTextTags;
    internal static RemoveRichTextOptions[]? AllRichTextTagFlags;
    public static Regex TimeRegex { get; } = new Regex(@"([\d\.]+)\s{0,1}([a-z]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Truncates a string if it's over a certain <paramref name="length"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? Truncate(this string? str, int length)
    {
        if (str is null)
            return null;

        return str.Length <= length ? str : str[..length];
    }

    /// <summary>
    /// Truncates a string if it's over a certain <paramref name="length"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(str))]
    public static string? TruncateWithEllipses(this string? str, int length)
    {
        if (str is null || str.Length <= length)
            return str;

        return string.Create(length + 3, str, (span, state) =>
        {
            state.AsSpan(0, span.Length - 3).CopyTo(span);
            span.Slice(span.Length - 3, 3).Fill('.');
        });
    }

    /// <summary>
    /// Compare two strings without worrying non-alphanumeric characters.
    /// </summary>
    /// <returns>The number of matching characters in <paramref name="searching"/> found in <paramref name="actual"/>.</returns>
    public static int CompareStringsFuzzy(ReadOnlySpan<char> searching, ReadOnlySpan<char> actual, bool caseSensitive)
    {
        int searchIndex = 0, actualIndex = 0;
        CultureInfo invariant = CultureInfo.InvariantCulture;
        int charsMatched = 0;
        while (searchIndex < searching.Length && actualIndex < actual.Length)
        {
            char actualChar = actual[actualIndex];
            if (ExcludeChar(actualChar))
            {
                ++actualIndex;
                continue;
            }

            char searchingChar = searching[searchIndex];
            if (ExcludeChar(searchingChar))
            {
                ++searchIndex;
                continue;
            }

            int cmp = caseSensitive
                ? actualChar.CompareTo(searchingChar)
                : char.ToUpper(actualChar, invariant).CompareTo(char.ToUpper(searchingChar, invariant));

            if (cmp != 0)
            {
                ++actualIndex;
                continue;
            }

            ++charsMatched;
            ++searchIndex;
            ++actualIndex;
        }

        return charsMatched;
    }

    private static bool ExcludeChar(char c)
    {
        unchecked
        {
            // is a-z or A-Z or 0-9
            return !(c - (uint)'a' <= 'z' - (uint)'a' || c - (uint)'A' <= 'Z' - (uint)'A' || c - (uint)'0' <= '9' - (uint)'0');
        }
    }

    /// <summary>
    /// Format a minute/second timer.
    /// </summary>
    /// <remarks><c>[HH:]MM:SS</c></remarks>
    public static string ToCountdownString(TimeSpan span, bool withHours)
    {
        return ToCountdownString((int)Math.Round(span.TotalSeconds), withHours);
    }

    /// <summary>
    /// Format a minute/second timer.
    /// </summary>
    /// <remarks><c>[HH:]MM:SS</c></remarks>
    public static string ToCountdownString(int seconds, bool withHours)
    {
        // tested 09/13/2024
        int minutes = seconds / 60;
        seconds %= 60;
        int hours = withHours ? minutes / 60 : 0;
        if (withHours)
            minutes %= 60;

        int len = 3 + (withHours || minutes < 100 ? 2 : MathUtility.CountDigits(minutes));
        if (withHours)
            len += 1 + (hours < 100 ? 2 : MathUtility.CountDigits(hours));

        CountdownState state = default;
        state.Seconds = seconds;
        state.Minutes = minutes;
        state.Hours = withHours ? hours : -1;

        return string.Create(len, state, (span, state) =>
        {
            bool withHours = state.Hours != -1;
            int index = -1;
            if (withHours)
            {
                if (state.Hours < 100)
                {
                    span[++index] = (char)((state.Hours / 10) + 48);
                    span[++index] = (char)((state.Hours % 10) + 48);
                }
                else
                {
                    state.Hours.TryFormat(span[(index + 1)..], out int charsWritten, "D2", CultureInfo.InvariantCulture);
                    index += charsWritten;
                }

                span[++index] = ':';
            }

            if (state.Minutes < 100)
            {
                span[++index] = (char)((state.Minutes / 10) + 48);
                span[++index] = (char)((state.Minutes % 10) + 48);
            }
            else
            {
                state.Minutes.TryFormat(span[(index + 1)..], out int charsWritten, "D2", CultureInfo.InvariantCulture);
                index += charsWritten;
            }

            span[++index] = ':';

            span[++index] = (char)((state.Seconds / 10) + 48);
            span[++index] = (char)((state.Seconds % 10) + 48);
        });
    }

    private struct CountdownState
    {
        public int Seconds;
        public int Minutes;
        public int Hours;
    }

    /// <summary>
    /// Truncates <paramref name="text"/> so that it's UTF-8 byte count is less than or equal to <paramref name="maximumBytes"/>.
    /// </summary>
    /// <param name="byteLength">The length in UTF-8 bytes of the truncated text.</param>
    public static ReadOnlySpan<char> TruncateUtf8Bytes(ReadOnlySpan<char> text, int maximumBytes, out int byteLength)
    {
        if (maximumBytes < 0)
        {
            byteLength = Encoding.UTF8.GetByteCount(text);
            return text;
        }

        if (maximumBytes == 0)
        {
            byteLength = 0;
            return default;
        }

        int byteCt = Encoding.UTF8.GetByteCount(text);
        if (byteCt <= maximumBytes)
        {
            byteLength = byteCt;
            return text;
        }

        Encoder encoder = Encoding.UTF8.GetEncoder();
        byte[] buffer = new byte[maximumBytes];
        encoder.Convert(text, buffer, false, out int charsUsed, out byteLength, out _);
        return text.Slice(0, charsUsed);
    }


    /// <summary>
    /// Parses a timespan string in the form '3d 4hr 21min etc'. Can also be 'perm[anent]'.
    /// </summary>
    /// <returns>Total amount of time. <see cref="Timeout.InfiniteTimeSpan"/> is returned if <paramref name="input"/> is permanent.</returns>
    public static TimeSpan ParseTimespan(string input)
    {
        if (input.StartsWith("perm", StringComparison.OrdinalIgnoreCase))
            return Timeout.InfiniteTimeSpan;

        if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int mins) && mins > -1)
            return TimeSpan.FromMinutes(mins);

        TimeSpan time = TimeSpan.Zero;
        foreach (Match match in TimeRegex.Matches(input))
        {
            if (match.Groups.Count != 3) continue;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double t))
                continue;

            string key = match.Groups[2].Value;

            if (key.StartsWith("ms", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromMilliseconds(t);
            else if (key.StartsWith("s", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromSeconds(t);
            else if (key.StartsWith("mo", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromSeconds(t * 2565000); // 29.6875 days (356.25 / 12)
            else if (key.StartsWith("m", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromMinutes(t);
            else if (key.StartsWith("h", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromHours(t);
            else if (key.StartsWith("d", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(t);
            else if (key.StartsWith("w", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(t * 7);
            else if (key.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                time += TimeSpan.FromDays(t * 365.25);
        }
        return time;
    }

    /// <summary>
    /// Converts a timespan to a string in the form '3d 4hr 21min etc'. Will be 'perm[anent]' if <paramref name="timeSpan"/> is <see cref="Timeout.InfiniteTimeSpan"/> (or any negative <see cref="TimeSpan"/>).
    /// </summary>
    public static string ToTimeString(TimeSpan timeSpan, int figures = -1)
    {
        if (timeSpan.Ticks < 0L)
            return "permanent";
        
        if (timeSpan.Ticks == 0)
            return "0s";

        StringBuilder sb = new StringBuilder(12);
        sb.Clear();
        int seconds = (int)Math.Round(timeSpan.TotalSeconds);
        int m = seconds / 60;
        int h = m / 60;
        int d = h / 24;
        int mo = (int)Math.Floor(d / 29.6875);
        int y = (int)Math.Floor(d / 356.25);
        seconds %= 60;
        m %= 60;
        h %= 24;
        mo %= 12;
        if (y != 0)
        {
            sb.Append(y).Append('y');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
        }
        if (mo > 0)
        {
            sb.Append(mo).Append("mo");
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
            d %= 30;
            if (d != 0)
            {
                sb.Append(d).Append('d');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();
            }
        }
        else
        {
            int w = (d / 7) % 52;
            d %= 7;
            if (w != 0)
            {
                sb.Append(w).Append('w');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();
            }
            if (d != 0)
            {
                sb.Append(d).Append('d');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();
            }
        }
        if (h != 0)
        {
            sb.Append(h).Append('h');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
        }
        if (m != 0)
        {
            sb.Append(m).Append('m');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
        }
        if (seconds != 0)
        {
            sb.Append(seconds).Append('s');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Parse any common type.
    /// </summary>
    public static bool TryParseAny(string input, IFormatProvider provider, Type type, out object? value)
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
            
            if (typeof(Asset).IsAssignableFrom(type))
            {
                if (Guid.TryParse(input, out Guid guid))
                {
                    value = Assets.find(guid);
                    if (!type.IsInstanceOfType(value))
                        value = null!;
                    return value is not null;
                }

                if (ushort.TryParse(input, NumberStyles.Any, provider, out ushort id))
                {
                    value = Assets.find(UCAssetManager.GetAssetCategory(type), id);
                    if (!type.IsInstanceOfType(value))
                        value = null!;
                    return value is not null;
                }

                if (!input.Equals("null", StringComparison.OrdinalIgnoreCase))
                    return false;

                value = Activator.CreateInstance(type);
                return true;
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
                bool res = ulong.TryParse(input, NumberStyles.Any, provider, out ulong v2);
                value = v2;
                return res;
            }

            if (type == typeof(float))
            {
                bool res = float.TryParse(input, NumberStyles.Any, provider, out float v2);
                value = v2;
                return res;
            }

            if (type == typeof(long))
            {
                bool res = long.TryParse(input, NumberStyles.Any, provider, out long v2);
                value = v2;
                return res;
            }

            if (type == typeof(ushort))
            {
                bool res = ushort.TryParse(input, NumberStyles.Any, provider, out ushort v2);
                value = v2;
                return res;
            }

            if (type == typeof(short))
            {
                bool res = short.TryParse(input, NumberStyles.Any, provider, out short v2);
                value = v2;
                return res;
            }

            if (type == typeof(byte))
            {
                bool res = byte.TryParse(input, NumberStyles.Any, provider, out byte v2);
                value = v2;
                return res;
            }

            if (type == typeof(int))
            {
                bool res = int.TryParse(input, NumberStyles.Any, provider, out int v2);
                value = v2;
                return res;
            }

            if (type == typeof(uint))
            {
                bool res = uint.TryParse(input, NumberStyles.Any, provider, out uint v2);
                value = v2;
                return res;
            }

            if (type == typeof(nint))
            {
                bool res;
                if (IntPtr.Size == 4)
                {
                    res = int.TryParse(input, NumberStyles.Any, provider, out int v2);
                    value = (nint)v2;
                }
                else
                {
                    res = long.TryParse(input, NumberStyles.Any, provider, out long v2);
                    value = (nint)v2;
                }

                return res;
            }

            if (type == typeof(nuint))
            {
                bool res;

                if (IntPtr.Size == 4)
                {
                    res = uint.TryParse(input, NumberStyles.Any, provider, out uint v2);
                    value = (nuint)v2;
                }
                else
                {
                    res = ulong.TryParse(input, NumberStyles.Any, provider, out ulong v2);
                    value = (nuint)v2;
                }

                return res;
            }

            if (type == typeof(bool))
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
                if (
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

            if (type == typeof(char))
            {
                if (input.Length == 1)
                {
                    value = input[0];
                    return true;
                }
                return false;
            }

            if (type == typeof(sbyte))
            {
                bool res = sbyte.TryParse(input, NumberStyles.Any, provider, out sbyte v2);
                value = v2;
                return res;
            }

            if (type == typeof(double))
            {
                bool res = double.TryParse(input, NumberStyles.Any, provider, out double v2);
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
            if (!@internal.IsGenericType && TryParseAny(input, provider, @internal, out object? val) && val is not null)
            {
                value = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(type), val);
                return value is not null;
            }

            return false;
        }

        if (type == typeof(decimal))
        {
            bool res = decimal.TryParse(input, NumberStyles.Any, provider, out decimal v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(DateTime))
        {
            bool res = DateTime.TryParse(input, provider, DateTimeStyles.AssumeLocal, out DateTime v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(TimeSpan))
        {
            bool res = TimeSpan.TryParse(input, provider, out TimeSpan v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(Guid))
        {
            bool res = Guid.TryParse(input, out Guid v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(Vector2))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x, NumberStyles.Any, provider, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 2)
            {
                value = new Vector2(vals[0], vals[1]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Vector3))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x, NumberStyles.Any, provider, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 3)
            {
                value = new Vector3(vals[0], vals[1], vals[2]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Vector4))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x, NumberStyles.Any, provider, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Vector4(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Quaternion))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x, NumberStyles.Any, provider, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Quaternion(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            if (vals.Length == 3)
            {
                value = Quaternion.Euler(vals[0], vals[1], vals[2]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Color))
        {
            if (!HexStringHelper.TryParseColor(input, provider, out Color color))
                return false;
            
            value = color;
            return true;
        }
        
        if (type == typeof(Color32))
        {
            if (!HexStringHelper.TryParseColor32(input, provider, out Color32 color))
                return false;
            
            value = color;
            return true;
        }
        
        if (type == typeof(CSteamID))
        {
            if (!TryParseSteamId(input, out CSteamID steam64))
                return false;
            
            value = steam64;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parse a Steam ID in any form.
    /// </summary>
    public static bool TryParseSteamId(string str, out CSteamID steamId)
    {
        if (str.Length > 2 && str[0] is 'N' or 'n' or 'O' or 'o' or 'L' or 'l' or 'z' or 'Z')
        {
            if (str.Equals("Nil", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("zero", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.Nil;
                return true;
            }
            if (str.Equals("OutofDateGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out-of-date-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out of date gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("out_of_date_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.OutofDateGS;
                return true;
            }
            if (str.Equals("LanModeGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan-mode-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan mode gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("lan_mode_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.LanModeGS;
                return true;
            }
            if (str.Equals("NotInitYetGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not-init-yet-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not init yet gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("not_init_yet_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.NotInitYetGS;
                return true;
            }
            if (str.Equals("NonSteamGS", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non-steam-gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non steam gs", StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals("non_steam_gs", StringComparison.InvariantCultureIgnoreCase))
            {
                steamId = CSteamID.NonSteamGS;
                return true;
            }
        }

        if (str.Length >= 8 && uint.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out uint acctId1))
        {
            steamId = new CSteamID(new AccountID_t(acctId1), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeIndividual);
            return true;
        }

        if (str.Length >= 17 && ulong.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong id))
        {
            steamId = new CSteamID(id);

            // try parse as hex instead
            if (steamId.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            {
                if (!ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
                    return true;
                CSteamID steamid2 = new CSteamID(id);
                if (steamid2.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                    steamId = steamid2;
            }
            return true;
        }

        if (str.Length >= 15 && ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong acctId2))
        {
            steamId = new CSteamID(acctId2);
            return true;
        }

        if (str.StartsWith("STEAM_", StringComparison.InvariantCultureIgnoreCase) && str.Length > 10)
        {
            if (str[7] != ':' || str[9] != ':')
                goto fail;
            char uv = str[6];
            if (!char.IsDigit(uv))
                goto fail;
            EUniverse universe = (EUniverse)(uv - 48);
            if (universe == EUniverse.k_EUniverseInvalid)
                universe = EUniverse.k_EUniversePublic;

            bool y;
            if (str[8] == '1')
                y = true;
            else if (str[8] == '0')
                y = false;
            else goto fail;
            if (!uint.TryParse(str.Substring(10), NumberStyles.Number, CultureInfo.InvariantCulture, out uint acctId))
                goto fail;

            steamId = new CSteamID(new AccountID_t((uint)(acctId * 2 + (y ? 1 : 0))), universe, EAccountType.k_EAccountTypeIndividual);
            return true;
        }

        if (str.Length > 8 && str[0] == '[')
        {
            if (str[2] != ':' || str[4] != ':' || str[^1] != ']')
                goto fail;
            EAccountType type;
            char c = str[1];
            if (c is 'I' or 'i')
                type = EAccountType.k_EAccountTypeInvalid;
            else if (c == 'U')
                type = EAccountType.k_EAccountTypeIndividual;
            else if (c == 'M')
                type = EAccountType.k_EAccountTypeMultiseat;
            else if (c == 'G')
                type = EAccountType.k_EAccountTypeGameServer;
            else if (c == 'A')
                type = EAccountType.k_EAccountTypeAnonGameServer;
            else if (c == 'P')
                type = EAccountType.k_EAccountTypePending;
            else if (c == 'C')
                type = EAccountType.k_EAccountTypeContentServer;
            else if (c == 'g')
                type = EAccountType.k_EAccountTypeClan;
            else if (c is 'T' or 'L' or 'c')
                type = EAccountType.k_EAccountTypeChat;
            else if (c == 'a')
                type = EAccountType.k_EAccountTypeAnonUser;
            else goto fail;
            char uv = str[3];
            if (!char.IsDigit(uv))
                goto fail;
            uint acctId;
            if (str[^3] != ':')
            {
                if (!uint.TryParse(str.Substring(5, str.Length - 6), NumberStyles.Number, CultureInfo.InvariantCulture, out acctId))
                    goto fail;
            }
            else
            {
                if (!uint.TryParse(str.Substring(5, str.Length - 8), NumberStyles.Number, CultureInfo.InvariantCulture, out acctId))
                    goto fail;
                acctId *= 2;
                uv = str[^2];
                if (uv == '1')
                    ++acctId;
                else if (uv != '0')
                    goto fail;
            }

            EUniverse universe = (EUniverse)(uv - 48);
            if (universe == EUniverse.k_EUniverseInvalid)
                universe = EUniverse.k_EUniversePublic;

            steamId = new CSteamID(new AccountID_t(acctId), universe, type);
            return true;
        }

        fail:
        steamId = CSteamID.Nil;
        return false;
    }

    /// <summary>
    /// Remove rich text, including TextMeshPro and normal Unity tags.
    /// </summary>
    /// <param name="options">Tags to check for and remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [Pure]
    public static unsafe string RemoveRichText(string str, int index = 0, int length = -1, RemoveRichTextOptions options = RemoveRichTextOptions.All)
    {
        CheckTags();
        if (index >= str.Length || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (length < 0)
            length = str.Length - index;
        else if (index + length > str.Length)
            throw new ArgumentOutOfRangeException(nameof(length));
        else if (length == 0)
            return str;

        char[] rtn = new char[str.Length + 16];
        int nextCopyStartIndex = 0;
        int writeIndex = 0;

        fixed (char* mainPtr = str)
        {
            char* ptr = mainPtr + index;
            for (int i = 0; i < length; ++i)
            {
                char current = ptr[i];
                if (current != '<')
                    continue;
                
                bool isEndTag = i != length - 1 && ptr[i + 1] == '/';
                int endIndex = -1;
                for (int j = i + (isEndTag ? 2 : 1); j < length; ++j)
                {
                    if (ptr[j] != '>')
                        continue;

                    endIndex = j;
                    break;
                }

                if (endIndex == -1 || !CompareRichTextTag(ptr, endIndex, i, options))
                    continue;

                Append(ref rtn, ptr + nextCopyStartIndex, writeIndex, i - nextCopyStartIndex);
                writeIndex += i - nextCopyStartIndex;
                nextCopyStartIndex = endIndex + 1;
                i = endIndex;
            }
            Append(ref rtn, ptr + nextCopyStartIndex, writeIndex, str.Length - nextCopyStartIndex);
            writeIndex += str.Length - nextCopyStartIndex;
        }

        return new string(rtn, 0, writeIndex);
    }

    /// <summary>
    /// Convert a <see cref="Color"/> to ARGB data.
    /// </summary>
    public static int ToArgb(Color color)
    {
        return (byte)Math.Min(255, Mathf.RoundToInt(color.a * 255)) << 24 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.r * 255)) << 16 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.g * 255)) << 8 |
               (byte)Math.Min(255, Mathf.RoundToInt(color.b * 255));
    }

    /// <summary>
    /// Convert a <see cref="Color32"/> to ARGB data.
    /// </summary>
    public static int ToArgb(Color32 color)
    {
        return color.a << 24 |
               color.r << 16 |
               color.g << 8 |
               color.b;
    }
    private static unsafe void Append(ref char[] arr, char* data, int index, int length)
    {
        if (length == 0) return;

        if (index + length > arr.Length)
        {
            char[] old = arr;
            arr = new char[index + length];
            Buffer.BlockCopy(old, 0, arr, 0, old.Length * sizeof(char));
        }
        for (int i = 0; i < length; ++i)
            arr[i + index] = data[i];
    }
    internal static void PrintTaskErrors(ILogger logger, UniTask[] tasks, IReadOnlyList<object> hostedServices)
    {
        for (int i = 0; i < tasks.Length; ++i)
        {
            if (tasks[i].Status is not UniTaskStatus.Faulted and not UniTaskStatus.Canceled)
                continue;

            if (tasks[i].AsTask().Exception is { } ex)
            {
                logger.LogError(ex, Accessor.Formatter.Format(hostedServices[i].GetType()));
            }
            else
            {
                logger.LogError(Accessor.Formatter.Format(hostedServices[i].GetType()));
            }

            logger.LogError(string.Empty);
        }
    }
    internal static unsafe bool CompareRichTextTag(char* data, int endIndex, int index, RemoveRichTextOptions options)
    {
        ++index;
        if (data[index] == '/')
            ++index;
        else if (data[index] == '#')
            return true;
        for (int j = index; j < endIndex; ++j)
        {
            if (data[j] is '=' or ' ')
            {
                endIndex = j;
                break;
            }
        }

        int length = endIndex - index;
        bool found = false;
        for (int j = 0; j < AllRichTextTags!.Length; ++j)
        {
            char[] tag = AllRichTextTags[j];
            if (tag.Length != length) continue;
            if ((options & AllRichTextTagFlags![j]) == 0)
                continue;
            bool matches = true;
            for (int k = 0; k < length; ++k)
            {
                char c = data[index + k];
                if ((int)c is > 64 and < 91)
                    c = (char)(c + 32);
                if (tag[k] != c)
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                found = true;
                break;
            }
        }

        return found;
    }
    internal static void CheckTags()
    {
        AllRichTextTags ??=
        [
            "align".ToCharArray(),
            "allcaps".ToCharArray(),
            "alpha".ToCharArray(),
            "b".ToCharArray(),
            "br".ToCharArray(),
            "color".ToCharArray(),
            "cspace".ToCharArray(),
            "font".ToCharArray(),
            "font-weight".ToCharArray(),
            "gradient".ToCharArray(),
            "i".ToCharArray(),
            "indent".ToCharArray(),
            "line-height".ToCharArray(),
            "line-indent".ToCharArray(),
            "link".ToCharArray(),
            "lowercase".ToCharArray(),
            "material".ToCharArray(),
            "margin".ToCharArray(),
            "mark".ToCharArray(),
            "mspace".ToCharArray(),
            "nobr".ToCharArray(),
            "noparse".ToCharArray(),
            "page".ToCharArray(),
            "pos".ToCharArray(),
            "quad".ToCharArray(),
            "rotate".ToCharArray(),
            "s".ToCharArray(),
            "size".ToCharArray(),
            "smallcaps".ToCharArray(),
            "space".ToCharArray(),
            "sprite".ToCharArray(),
            "strikethrough".ToCharArray(),
            "style".ToCharArray(),
            "sub".ToCharArray(),
            "sup".ToCharArray(),
            "u".ToCharArray(),
            "underline".ToCharArray(),
            "uppercase".ToCharArray(),
            "voffset".ToCharArray(),
            "width".ToCharArray()
        ];
        AllRichTextTagFlags ??=
        [
            RemoveRichTextOptions.Align,
            RemoveRichTextOptions.Uppercase,
            RemoveRichTextOptions.Alpha,
            RemoveRichTextOptions.Bold,
            RemoveRichTextOptions.LineBreak,
            RemoveRichTextOptions.Color,
            RemoveRichTextOptions.CharacterSpacing,
            RemoveRichTextOptions.Font,
            RemoveRichTextOptions.FontWeight,
            RemoveRichTextOptions.Gradient,
            RemoveRichTextOptions.Italic,
            RemoveRichTextOptions.Indent,
            RemoveRichTextOptions.LineHeight,
            RemoveRichTextOptions.LineIndent,
            RemoveRichTextOptions.Link,
            RemoveRichTextOptions.Lowercase,
            RemoveRichTextOptions.Material,
            RemoveRichTextOptions.Margin,
            RemoveRichTextOptions.Mark,
            RemoveRichTextOptions.Monospace,
            RemoveRichTextOptions.NoLineBreak,
            RemoveRichTextOptions.NoParse,
            RemoveRichTextOptions.PageBreak,
            RemoveRichTextOptions.Position,
            RemoveRichTextOptions.Quad,
            RemoveRichTextOptions.Rotate,
            RemoveRichTextOptions.Strikethrough,
            RemoveRichTextOptions.Size,
            RemoveRichTextOptions.Smallcaps,
            RemoveRichTextOptions.Space,
            RemoveRichTextOptions.Sprite,
            RemoveRichTextOptions.Strikethrough,
            RemoveRichTextOptions.Style,
            RemoveRichTextOptions.Subscript,
            RemoveRichTextOptions.Superscript,
            RemoveRichTextOptions.Underline,
            RemoveRichTextOptions.Underline,
            RemoveRichTextOptions.Uppercase,
            RemoveRichTextOptions.VerticalOffset,
            RemoveRichTextOptions.TextWidth
        ];
    }
}


[Flags]
public enum RemoveRichTextOptions : ulong
{
    None = 0L,
    /// <summary>
    /// &lt;align&gt;
    /// </summary>
    Align = 1L << 0,
    /// <summary>
    /// &lt;allcaps&gt;, &lt;uppercase&gt;
    /// </summary>
    Uppercase = 1L << 1,
    /// <summary>
    /// &lt;alpha&gt;
    /// </summary>
    Alpha = 1L << 2,
    /// <summary>
    /// &lt;b&gt;
    /// </summary>
    Bold = 1L << 3,
    /// <summary>
    /// &lt;br&gt;
    /// </summary>
    LineBreak = 1L << 4,
    /// <summary>
    /// &lt;color=...&gt;, &lt;#...&gt;
    /// </summary>
    Color = 1L << 5,
    /// <summary>
    /// &lt;cspace&gt;
    /// </summary>
    CharacterSpacing = 1L << 6,
    /// <summary>
    /// &lt;font&gt;
    /// </summary>
    Font = 1L << 7,
    /// <summary>
    /// &lt;font-weight&gt;
    /// </summary>
    FontWeight = 1L << 8,
    /// <summary>
    /// &lt;gradient&gt;
    /// </summary>
    Gradient = 1L << 9,
    /// <summary>
    /// &lt;i&gt;
    /// </summary>
    Italic = 1L << 10,
    /// <summary>
    /// &lt;indent&gt;
    /// </summary>
    Indent = 1L << 11,
    /// <summary>
    /// &lt;line-height&gt;
    /// </summary>
    LineHeight = 1L << 12,
    /// <summary>
    /// &lt;line-indent&gt;
    /// </summary>
    LineIndent = 1L << 13,
    /// <summary>
    /// &lt;link&gt;
    /// </summary>
    Link = 1L << 14,
    /// <summary>
    /// &lt;lowercase&gt;
    /// </summary>
    Lowercase = 1L << 15,
    /// <summary>
    /// &lt;material&gt;
    /// </summary>
    Material = 1L << 16,
    /// <summary>
    /// &lt;margin&gt;
    /// </summary>
    Margin = 1L << 17,
    /// <summary>
    /// &lt;mark&gt;
    /// </summary>
    Mark = 1L << 18,
    /// <summary>
    /// &lt;mspace&gt;
    /// </summary>
    Monospace = 1L << 19,
    /// <summary>
    /// &lt;nobr&gt;
    /// </summary>
    NoLineBreak = 1L << 20,
    /// <summary>
    /// &lt;noparse&gt;
    /// </summary>
    NoParse = 1L << 21,
    /// <summary>
    /// &lt;page&gt;
    /// </summary>
    PageBreak = 1L << 22,
    /// <summary>
    /// &lt;pos&gt;
    /// </summary>
    Position = 1L << 23,
    /// <summary>
    /// &lt;quad&gt;
    /// </summary>
    Quad = 1L << 24,
    /// <summary>
    /// &lt;rotate&gt;
    /// </summary>
    Rotate = 1L << 25,
    /// <summary>
    /// &lt;s&gt;, &lt;strikethrough&gt;
    /// </summary>
    Strikethrough = 1L << 26,
    /// <summary>
    /// &lt;size&gt;
    /// </summary>
    Size = 1L << 27,
    /// <summary>
    /// &lt;smallcaps&gt;
    /// </summary>
    Smallcaps = 1L << 28,
    /// <summary>
    /// &lt;space&gt;
    /// </summary>
    Space = 1L << 29,
    /// <summary>
    /// &lt;sprite&gt;
    /// </summary>
    Sprite = 1L << 30,
    /// <summary>
    /// &lt;style&gt;
    /// </summary>
    Style = 1L << 31,
    /// <summary>
    /// &lt;sub&gt;
    /// </summary>
    Subscript = 1L << 32,
    /// <summary>
    /// &lt;sup&gt;
    /// </summary>
    Superscript = 1L << 33,
    /// <summary>
    /// &lt;u&gt;, &lt;underline&gt;
    /// </summary>
    Underline = 1L << 34,
    /// <summary>
    /// &lt;voffset&gt;
    /// </summary>
    VerticalOffset = 1L << 35,
    /// <summary>
    /// &lt;width&gt;
    /// </summary>
    TextWidth = 1L << 36,

    /// <summary>
    /// All rich text tags.
    /// </summary>
    All = Align | Alpha | Bold | LineBreak | Color | CharacterSpacing | Font | FontWeight | Gradient | Italic | Indent |
          LineHeight | LineIndent | Link | Lowercase | Material | Margin | Mark | Monospace | NoLineBreak |
          NoParse | PageBreak | Position | Quad | Rotate | Strikethrough | Size | Smallcaps | Space | Sprite |
          Style | Subscript | Superscript | Underline | Uppercase | VerticalOffset | TextWidth
}