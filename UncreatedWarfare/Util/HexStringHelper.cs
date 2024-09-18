using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Uncreated.Warfare.Util;
public static class HexStringHelper
{
    // stole these from DevkitServer
    //
    // I'm not using unity's ColorUtility for two reasons:
    //  * it doesn't work on 3 length hex strings
    //  * it needs a native call, which won't work with tests or external use.

    private static KeyValuePair<string, Color>[]? _presets;
    private static void CheckPresets()
    {
        if (_presets != null)
            return;

        // get all color properties from Color for presets.

        PropertyInfo[] props = typeof(Color)
            .GetProperties(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.PropertyType == typeof(Color) && x.GetMethod != null)
            .ToArray();

        _presets = new KeyValuePair<string, Color>[props.Length];
        for (int i = 0; i < props.Length; ++i)
            _presets[i] = new KeyValuePair<string, Color>(props[i].Name.ToLowerInvariant(), (Color)props[i].GetMethod.Invoke(null, Array.Empty<object>()));
    }

    /// <summary>
    /// Parses a string in the following formats: '[#]rrggbb[aa]', '[#]rgb[a]', '[#]g[a]'.
    /// </summary>
    public static unsafe bool TryParseHexColor32(ReadOnlySpan<char> hex, out Color32 color)
    {
        hex = hex.Trim();

        if (hex.IsEmpty)
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

    /// <summary>
    /// Parses a string in the following formats: '[#]rrggbb[aa]', '[#]rgb[a]', '[#]g[a]', 'colorname', 'rgb(0, 0, 0[, 0])', 'hsv(0, 0, 0[, 0])', '(0, 0, 0[, 0])'.
    /// </summary>
    public static bool TryParseColor(ReadOnlySpan<char> str, IFormatProvider? formatProvider, out Color color)
    {
        str = str.Trim();

        formatProvider ??= CultureInfo.CurrentCulture;
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

        int commaCt = str.Count(',');
        if (commaCt is 2 or 3)
        {
            // parse rgb(0,0,0,0)
            Span<Range> splitResults = stackalloc Range[commaCt + 1];
            int sections = str.Split(splitResults, ',', trimOuter: true, trimEachEntry: true);
            splitResults = splitResults[..sections];

            ReadOnlySpan<char> starterSection = str[splitResults[0]];
            bool hsv = starterSection.StartsWith("hsv", StringComparison.InvariantCultureIgnoreCase);
            bool rgb = starterSection.StartsWith("rgb", StringComparison.InvariantCultureIgnoreCase);
            bool rgba = rgb && starterSection.StartsWith("rgba", StringComparison.InvariantCultureIgnoreCase);
            if (!hsv && starterSection[0] != '(' && !(rgb || rgba))
                goto fail;

            if (starterSection[0] != '(')
                starterSection = starterSection[(rgba ? 4 : 3)..];

            if (starterSection.Length == 0)
                goto fail;

            float a = 255f;
            if (!float.TryParse(starterSection[1..], NumberStyles.Number, formatProvider, out float r))
                goto fail;
            if (!float.TryParse(str[splitResults[1]], NumberStyles.Number, formatProvider, out float g))
                goto fail;
            ReadOnlySpan<char> lastSection = str[splitResults[2]];
            if (commaCt == 2 && lastSection[^1] == ')')
                lastSection = lastSection[..^1];
            if (!float.TryParse(lastSection, NumberStyles.Number, formatProvider, out float b))
                goto fail;
            if (commaCt == 3)
            {
                lastSection = str[splitResults[3]];
                if (lastSection[^1] == ')')
                    lastSection = lastSection[..^1];
                if (!float.TryParse(lastSection, NumberStyles.Number, formatProvider, out a))
                    goto fail;
            }

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

        if (commaCt > 0)
        {
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
            if (_presets[i].Key.AsSpan().CompareTo(str, StringComparison.InvariantCultureIgnoreCase) != 0)
                continue;

            color = _presets[i].Value;
            return true;
        }

        color = default;
        return false;
    }

    /// <summary>
    /// Parses a string in the following formats: '[#]rrggbb[aa]', '[#]rgb[a]', '[#]g[a]', 'colorname', 'rgb(0, 0, 0[, 0])', 'hsv(0, 0, 0[, 0])', '(0, 0, 0[, 0])'.
    /// </summary>
    public static bool TryParseColor32(ReadOnlySpan<char> str, IFormatProvider? formatProvider, out Color32 color)
    {
        str = str.Trim();

        formatProvider ??= CultureInfo.CurrentCulture;
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

        int commaCt = str.Count(',');
        if (commaCt is 2 or 3)
        {
            // parse rgb(0,0,0,0)
            Span<Range> splitResults = stackalloc Range[commaCt + 1];
            int sections = str.Split(splitResults, ',', trimOuter: true, trimEachEntry: true);
            splitResults = splitResults[..sections];

            ReadOnlySpan<char> starterSection = str[splitResults[0]];
            bool hsv = starterSection.StartsWith("hsv", StringComparison.InvariantCultureIgnoreCase);
            bool rgb = starterSection.StartsWith("rgb", StringComparison.InvariantCultureIgnoreCase);
            bool rgba = rgb && starterSection.StartsWith("rgba", StringComparison.InvariantCultureIgnoreCase);
            if (!hsv && starterSection[0] != '(' && !(rgb || rgba))
                goto fail;

            if (starterSection[0] != '(')
                starterSection = starterSection[(rgba ? 4 : 3)..];

            if (starterSection.Length == 0)
                goto fail;

            int a = 255;
            if (!int.TryParse(starterSection[1..], NumberStyles.Number, formatProvider, out int r))
                goto fail;
            if (!int.TryParse(str[splitResults[1]], NumberStyles.Number, formatProvider, out int g))
                goto fail;
            ReadOnlySpan<char> lastSection = str[splitResults[2]];
            if (commaCt == 2 && lastSection[^1] == ')')
                lastSection = lastSection[..^1];
            if (!int.TryParse(lastSection, NumberStyles.Number, formatProvider, out int b))
                goto fail;
            if (commaCt == 3)
            {
                lastSection = str[splitResults[3]];
                if (lastSection[^1] == ')')
                    lastSection = lastSection[..^1];
                if (!int.TryParse(lastSection, NumberStyles.Number, formatProvider, out a))
                    goto fail;
            }

            if (hsv)
            {
                color = Color.HSVToRGB(r / 360f, g / 100f, b / 100f, false) with { a = a / 255f };
                return true;
            }

            color = new Color32((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255), (byte)Math.Clamp(a, 0, 255));
            return true;
        fail:
            color = default;
            return false;
        }

        if (commaCt > 0)
        {
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
            if (_presets[i].Key.AsSpan().CompareTo(str, StringComparison.InvariantCultureIgnoreCase) != 0)
                continue;

            color = _presets[i].Value;
            return true;
        }

        color = default;
        return false;
    }

    /// <summary>
    /// Formats a hex color, where <paramref name="size"/> represents the amount of letters used to describe the color, defaulting to 6 or 8 depending on if the alpha channel is 255.
    /// </summary>
    public static string FormatHexColor(Color32 color, int size = -1)
    {
        if (size == -1)
            size = color.a == 255 ? 6 : 8;
        if (size is 0 or 5 or 7 or > 8 or < 0)
            throw new ArgumentException("Color size must be 1, 2, 3, 4, 6, or 8.");

        return string.Create(size, color, (span, color) => FormatHexColor(color, span));
    }

    /// <summary>
    /// Formats a hex color, the length/format of which is based on the size of <paramref name="output"/>.
    /// </summary>
    public static int FormatHexColor(Color32 color, Span<char> output)
    {
        switch (output.Length)
        {
            case 0:
                return 0;
            case 1:
                WriteNibbles(output, color.r, false);
                return 1;
            case 2:
                WriteNibbles(output, color.r, false);
                WriteNibbles(output[1..], color.a, false);
                return 2;
            case 3:
                WriteNibbles(output, color.r, false);
                WriteNibbles(output[1..], color.g, false);
                WriteNibbles(output[2..], color.b, false);
                return 3;
            case 4 or 5:
                WriteNibbles(output, color.r, false);
                WriteNibbles(output[1..], color.g, false);
                WriteNibbles(output[2..], color.b, false);
                WriteNibbles(output[3..], color.a, false);
                return 4;
            case 6 or 7:
                WriteNibbles(output, color.r, true);
                WriteNibbles(output[2..], color.g, true);
                WriteNibbles(output[4..], color.b, true);
                return 6;
            default:
                WriteNibbles(output, color.r, true);
                WriteNibbles(output[2..], color.g, true);
                WriteNibbles(output[4..], color.b, true);
                WriteNibbles(output[6..], color.a, true);
                return 8;
        }
    }

    private static void WriteNibbles(Span<char> span, byte b, bool dbl)
    {
        if (!dbl)
        {
            b = (byte)(b / 16);
            span[0] = b > 9 ? (char)(b + 87) : (char)(b + 48);
        }
        else
        {
            int nibl = (b & 0xF0) >> 4;
            span[0] = nibl > 9 ? (char)(nibl + 87) : (char)(nibl + 48);
            nibl = b & 0xF;
            span[1] = nibl > 9 ? (char)(nibl + 87) : (char)(nibl + 48);
        }
    }
}
