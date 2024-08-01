using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Uncreated.Warfare.Util;
public static class FormattingUtility
{
    private static char[][]? _tags;
    private static RemoveRichTextOptions[]? _tagFlags;
    private static readonly char[] SplitChars = [ ',' ];
    private static KeyValuePair<string, Color>[]? _presets;
    public static Regex TimeRegex { get; } = new Regex(@"([\d\.]+)\s{0,1}([a-z]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Format a minute/second timer using the specified culture.
    /// </summary>
    public static string GetTimerString(CultureInfo culture, TimeSpan timer) => timer.Minutes.ToString(culture) + culture.DateTimeFormat.TimeSeparator + timer.Seconds.ToString("D2", culture);

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
    public static bool TryParseAny(string input, CultureInfo culture, Type type, out object? value)
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

                if (ushort.TryParse(input, NumberStyles.Any, culture, out ushort id))
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
                bool res = ulong.TryParse(input, NumberStyles.Any, culture, out ulong v2);
                value = v2;
                return res;
            }

            if (type == typeof(float))
            {
                bool res = float.TryParse(input, NumberStyles.Any, culture, out float v2);
                value = v2;
                return res;
            }

            if (type == typeof(long))
            {
                bool res = long.TryParse(input, NumberStyles.Any, culture, out long v2);
                value = v2;
                return res;
            }

            if (type == typeof(ushort))
            {
                bool res = ushort.TryParse(input, NumberStyles.Any, culture, out ushort v2);
                value = v2;
                return res;
            }

            if (type == typeof(short))
            {
                bool res = short.TryParse(input, NumberStyles.Any, culture, out short v2);
                value = v2;
                return res;
            }

            if (type == typeof(byte))
            {
                bool res = byte.TryParse(input, NumberStyles.Any, culture, out byte v2);
                value = v2;
                return res;
            }

            if (type == typeof(int))
            {
                bool res = int.TryParse(input, NumberStyles.Any, culture, out int v2);
                value = v2;
                return res;
            }

            if (type == typeof(uint))
            {
                bool res = uint.TryParse(input, NumberStyles.Any, culture, out uint v2);
                value = v2;
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
                bool res = sbyte.TryParse(input, NumberStyles.Any, culture, out sbyte v2);
                value = v2;
                return res;
            }

            if (type == typeof(double))
            {
                bool res = double.TryParse(input, NumberStyles.Any, culture, out double v2);
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
            if (!@internal.IsGenericType && TryParseAny(input, culture, @internal, out object? val) && val is not null)
            {
                value = Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(type), val);
                return value is not null;
            }

            return false;
        }
        if (type == typeof(decimal))
        {
            bool res = decimal.TryParse(input, NumberStyles.Any, culture, out decimal v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(DateTime))
        {
            bool res = DateTime.TryParse(input, culture, DateTimeStyles.AssumeLocal, out DateTime v2);
            value = v2;
            return res;
        }
        
        if (type == typeof(TimeSpan))
        {
            bool res = TimeSpan.TryParse(input, culture, out TimeSpan v2);
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
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, culture, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 2)
            {
                value = new Vector2(vals[0], vals[1]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Vector3))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, culture, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 3)
            {
                value = new Vector3(vals[0], vals[1], vals[2]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Vector4))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, culture, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Vector4(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Quaternion))
        {
            float[] vals = input.Split(',').Select(x => float.TryParse(x.Trim(), NumberStyles.Any, culture, out float res) ? res : float.NaN).Where(x => !float.IsNaN(x)).ToArray();
            if (vals.Length == 4)
            {
                value = new Quaternion(vals[0], vals[1], vals[2], vals[3]);
                return true;
            }
            return false;
        }
        
        if (type == typeof(Color))
        {
            if (!TryParseColor(input, out Color color))
                return false;
            
            value = color;
            return true;
        }
        
        if (type == typeof(Color32))
        {
            if (!TryParseColor32(input, out Color32 color))
                return false;
            
            value = color;
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
    /// Parse a <see cref="Color32"/> from hex with an optional '#' prefix.
    /// </summary>
    public static unsafe bool TryParseHexColor32(string hex, out Color32 color)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            color = default;
            return false;
        }

        bool res = true;
        fixed (char* ptr2 = hex)
        {
            int offset = *ptr2 == '#' ? 1 : 0;
            char* ptr = ptr2 + offset;
            switch (hex.Length - offset)
            {
                case 1: // w
                    byte r = CharToHex(ptr, false);
                    color = new Color32(r, r, r, byte.MaxValue);
                    return res;
                case 2: // wa
                    r = CharToHex(ptr, false);
                    byte a = CharToHex(ptr + 1, false);
                    color = new Color32(r, r, r, a);
                    return res;
                case 3: // rgb
                    r = CharToHex(ptr, false);
                    byte g = CharToHex(ptr + 1, false);
                    byte b = CharToHex(ptr + 2, false);
                    color = new Color32(r, g, b, byte.MaxValue);
                    return res;
                case 4: // rgba
                    r = CharToHex(ptr, false);
                    g = CharToHex(ptr + 1, false);
                    b = CharToHex(ptr + 2, false);
                    a = CharToHex(ptr + 3, false);
                    color = new Color32(r, g, b, a);
                    return res;
                case 6: // rrggbb
                    r = CharToHex(ptr, true);
                    g = CharToHex(ptr + 2, true);
                    b = CharToHex(ptr + 4, true);
                    color = new Color32(r, g, b, byte.MaxValue);
                    return res;
                case 8: // rrggbbaa
                    r = CharToHex(ptr, true);
                    g = CharToHex(ptr + 2, true);
                    b = CharToHex(ptr + 4, true);
                    a = CharToHex(ptr + 6, true);
                    color = new Color32(r, g, b, a);
                    return res;
            }
        }

        color = default;
        return false;

        byte CharToHex(char* c, bool dual)
        {
            if (dual)
            {
                int c2 = *c;
                byte b1;
                if (c2 is > 96 and < 103)
                    b1 = (byte)((c2 - 87) * 0x10);
                else if (c2 is > 64 and < 71)
                    b1 = (byte)((c2 - 55) * 0x10);
                else if (c2 is > 47 and < 58)
                    b1 = (byte)((c2 - 48) * 0x10);
                else
                {
                    res = false;
                    return 0;
                }

                c2 = *(c + 1);
                if (c2 is > 96 and < 103)
                    return (byte)(b1 + (c2 - 87));
                if (c2 is > 64 and < 71)
                    return (byte)(b1 + (c2 - 55));
                if (c2 is > 47 and < 58)
                    return (byte)(b1 + (c2 - 48));
                res = false;
            }
            else
            {
                int c2 = *c;
                if (c2 is > 96 and < 103)
                    return (byte)((c2 - 87) * 0x10 + (c2 - 87));
                if (c2 is > 64 and < 71)
                    return (byte)((c2 - 55) * 0x10 + (c2 - 55));
                if (c2 is > 47 and < 58)
                    return (byte)((c2 - 48) * 0x10 + (c2 - 48));
                res = false;
            }

            return 0;
        }
    }
    private static void CheckPresets()
    {
        if (_presets != null)
            return;
        PropertyInfo[] props = typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.PropertyType == typeof(Color)).Where(x => x.GetMethod != null).ToArray();
        _presets = new KeyValuePair<string, Color>[props.Length];
        for (int i = 0; i < props.Length; ++i)
            _presets[i] = new KeyValuePair<string, Color>(props[i].Name.ToLowerInvariant(), (Color)props[i].GetMethod.Invoke(null, Array.Empty<object>()));
    }

    /// <summary>
    /// Parse a <see cref="Color"/> from hex with an optional '#' prefix, common color name, rgb(r,g,b,a) or hsv(h,s,v).
    /// </summary>
    [Pure]
    public static bool TryParseColor(string str, out Color color)
    {
        Color32 color32;
        if (str.Length > 0 && str[0] == '#')
        {
            if (TryParseHexColor32(str, out color32))
            {
                color = color32;
                return true;
            }

            color = default;
            return false;
        }
        string[] strs = str.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
        if (strs.Length is 3 or 4)
        {
            bool hsv = strs[0].StartsWith("hsv");
            float a = 255f;
            int ind = strs[0].IndexOf('(');
            if (ind != -1 && strs[0].Length > ind + 1) strs[0] = strs[0].Substring(ind + 1);
            if (!float.TryParse(strs[0], NumberStyles.Number, CultureInfo.InvariantCulture, out float r))
                goto fail;
            if (!float.TryParse(strs[1], NumberStyles.Number, CultureInfo.InvariantCulture, out float g))
                goto fail;
            if (!float.TryParse(strs[2].Replace(')', ' '), NumberStyles.Number, CultureInfo.InvariantCulture, out float b))
                goto fail;
            if (strs.Length > 3 && !float.TryParse(strs[3].Replace(')', ' '), NumberStyles.Number, CultureInfo.InvariantCulture, out a))
                goto fail;

            if (hsv)
            {
                color = Color.HSVToRGB(r / 360f, g / 100f, b / 100f, false) with { a = a / 255f };
                return true;
            }

            r = Mathf.Clamp01(r / 255f);
            g = Mathf.Clamp01(g / 255f);
            b = Mathf.Clamp01(b / 255f);
            a = Mathf.Clamp01(a / 255f);
            color = new Color(r, g, b, a);
            return true;
        fail:
            color = default;
            return false;
        }

        if (TryParseHexColor32(str, out color32))
        {
            color = color32;
            return true;
        }

        CheckPresets();
        for (int i = 0; i < _presets!.Length; ++i)
        {
            if (string.Compare(_presets[i].Key, str, CultureInfo.InvariantCulture,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreNonSpace) == 0)
            {
                color = _presets[i].Value;
                return true;
            }
        }

        color = default;
        return false;
    }

    /// <summary>
    /// Parse a <see cref="Color"/> from hex with an optional '#' prefix, common color name, rgb(r,g,b,a) or hsv(h,s,v).
    /// </summary>
    [Pure]
    public static bool TryParseColor32(string str, out Color32 color)
    {
        if (str.Length > 0 && str[0] == '#')
        {
            if (TryParseHexColor32(str, out color))
                return true;

            color = default;
            return false;
        }
        string[] strs = str.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
        if (strs.Length is 3 or 4)
        {
            bool hsv = strs[0].StartsWith("hsv");
            byte a = byte.MaxValue;
            int ind = strs[0].IndexOf('(');
            if (ind != -1 && strs[0].Length > ind + 1) strs[0] = strs[0].Substring(ind + 1);
            if (!int.TryParse(strs[0], NumberStyles.Number, CultureInfo.InvariantCulture, out int r))
                goto fail;
            if (!byte.TryParse(strs[1], NumberStyles.Number, CultureInfo.InvariantCulture, out byte g))
                goto fail;
            if (!byte.TryParse(strs[2].Replace(')', ' '), NumberStyles.Number, CultureInfo.InvariantCulture, out byte b))
                goto fail;
            if (strs.Length > 3 && !byte.TryParse(strs[3].Replace(')', ' '), NumberStyles.Number, CultureInfo.InvariantCulture, out a))
                goto fail;

            if (hsv)
            {
                color = Color.HSVToRGB(r / 360f, g / 100f, b / 100f, false) with { a = a / 255f };
                return true;
            }

            color = new Color32((byte)(r > 255 ? 255 : (r < 0 ? 0 : r)), g, b, a);
            return true;
        fail:
            color = default;
            return false;
        }

        if (TryParseHexColor32(str, out color))
            return true;

        CheckPresets();
        for (int i = 0; i < _presets!.Length; ++i)
        {
            if (string.Compare(_presets[i].Key, str, CultureInfo.InvariantCulture,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreNonSpace) == 0)
            {
                color = _presets[i].Value;
                return true;
            }
        }

        color = default;
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

    /// <summary>
    /// Get the closest <see cref="ConsoleColor"/> to the given ARGB data.
    /// </summary>
    public static ConsoleColor ToConsoleColor(int argb)
    {
        int bits = ((argb >> 16) & byte.MaxValue) > 128 || ((argb >> 8) & byte.MaxValue) > 128 || (argb & byte.MaxValue) > 128 ? 8 : 0;
        if (((argb >> 16) & byte.MaxValue) > 180)
            bits |= 4;
        if (((argb >> 8) & byte.MaxValue) > 180)
            bits |= 2;
        if ((argb & byte.MaxValue) > 180)
            bits |= 1;
        return (ConsoleColor)bits;
    }

    /// <summary>
    /// Get a <see cref="Color"/> estimation of <paramref name="color"/>.
    /// </summary>
    public static Color FromConsoleColor(ConsoleColor color)
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
    internal static void PrintTaskErrors(UniTask[] tasks, IReadOnlyList<object> hostedServices)
    {
        for (int i = 0; i < tasks.Length; ++i)
        {
            if (tasks[i].Status is not UniTaskStatus.Faulted and not UniTaskStatus.Canceled)
                continue;

            L.LogError(Accessor.Formatter.Format(hostedServices[i].GetType()));

            if (tasks[i].AsTask().Exception is { } ex)
            {
                L.LogError(ex);
            }

            L.LogError(string.Empty);
        }
    }
    private static unsafe bool CompareRichTextTag(char* data, int endIndex, int index, RemoveRichTextOptions options)
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
        for (int j = 0; j < _tags!.Length; ++j)
        {
            char[] tag = _tags[j];
            if (tag.Length != length) continue;
            if ((options & _tagFlags![j]) == 0)
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
    private static void CheckTags()
    {
        _tags ??=
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
        _tagFlags ??=
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
    All = Align | Alpha | Bold | LineBreak | CharacterSpacing | Font | FontWeight | Gradient | Italic | Indent |
          LineHeight | LineIndent | Link | Lowercase | Material | Margin | Mark | Monospace | NoLineBreak |
          NoParse | PageBreak | Position | Quad | Rotate | Strikethrough | Size | Smallcaps | Space | Sprite |
          Style | Subscript | Superscript | Underline | Uppercase | VerticalOffset | TextWidth
}