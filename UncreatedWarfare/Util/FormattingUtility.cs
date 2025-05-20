using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Steam;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Util;
public static class FormattingUtility
{
    internal static char[][]? AllRichTextTags;
    internal static RemoveRichTextOptions[]? AllRichTextTagFlags;
    public static Regex TimeRegex { get; } = new Regex(@"([\d\.]+)\s{0,1}([a-z]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
    public static string ToTimeString(TimeSpan timeSpan, int figures = -1, bool space = false)
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
        bool needsSpace = false;
        if (y != 0)
        {
            sb.Append(y).Append('y');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();

            needsSpace = space;
        }
        if (mo > 0)
        {
            if (needsSpace)
                sb.Append(' ');
            sb.Append(mo).Append("mo");
            if (figures != -1 && --figures <= 0)
                return sb.ToString();
            d %= 30;
            if (d != 0)
            {
                if (space)
                    sb.Append(' ');
                sb.Append(d).Append('d');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();
            }
            needsSpace = space;
        }
        else
        {
            int w = (d / 7) % 52;
            d %= 7;
            if (w != 0)
            {
                if (needsSpace)
                    sb.Append(' ');
                sb.Append(w).Append('w');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();

                needsSpace = space;
            }
            if (d != 0)
            {
                if (needsSpace)
                    sb.Append(' ');
                sb.Append(d).Append('d');
                if (figures != -1 && --figures <= 0)
                    return sb.ToString();

                needsSpace = space;
            }
        }
        if (h != 0)
        {
            if (needsSpace)
                sb.Append(' ');

            sb.Append(h).Append('h');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();

            needsSpace = space;
        }
        if (m != 0)
        {
            if (needsSpace)
                sb.Append(' ');

            sb.Append(m).Append('m');
            if (figures != -1 && --figures <= 0)
                return sb.ToString();

            needsSpace = space;
        }
        if (seconds != 0)
        {
            if (needsSpace)
                sb.Append(' ');

            sb.Append(seconds).Append('s');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Replaces newline constants like '/n', '\n', '&lt;br&gt;', etc with the actual newline character.
    /// </summary>
    [return: NotNullIfNotNull("str")]
    public static string? ReplaceNewLineSubstrings(string? str)
    {
        return str?.Replace("\\n", "\n").Replace("/n", "\n").Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
    }

    /// <summary>
    /// Parse any common type.
    /// </summary>
    public static bool TryParseAny(string input, IFormatProvider provider, Type type, out object? value)
    {
        value = null!;

        if (input is null || type is null)
            return false;

        if (type.IsClass)
        {
            if (type == typeof(string))
            {
                value = input;
                return true;
            }

            if (input.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                value = null;
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
                    value = Assets.find(AssetUtility.GetAssetCategory(type), id);
                    if (!type.IsInstanceOfType(value))
                        value = null!;
                    return value is not null;
                }
            }
            return false;
        }

        if (input.Equals("null", StringComparison.InvariantCultureIgnoreCase))
        {
            value = null;
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
                value = null;
                return true;
            }
            
            Type @internal = type.GetGenericArguments()[0];
            if (!@internal.IsGenericType && TryParseAny(input, provider, @internal, out object? val))
            {
                value = val;
                return true;
            }

            return false;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAssetLink<>))
        {
            Type assetType = type.GetGenericArguments()[0];
            if (input.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                value = AssetLink.Empty(assetType);
                return value is not null;
            }

            if (Guid.TryParse(input, out Guid guid))
            {
                value = AssetLink.Create(guid, assetType);
                return true;
            }

            if (ushort.TryParse(input, NumberStyles.Any, provider, out ushort id))
            {
                value = AssetLink.Create(id, assetType);
                return true;
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
            if (!SteamIdHelper.TryParseSteamId(input, out CSteamID steam64))
                return false;
            
            value = steam64;
            return true;
        }

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
            UniTaskStatus status = tasks[i].Status;
            if (status is not UniTaskStatus.Faulted and not UniTaskStatus.Canceled)
            {
                if (status == UniTaskStatus.Pending)
                {
                    logger.LogWarning(Accessor.Formatter.Format(hostedServices[i].GetType()) + " - not completed");
                }
                continue;
            }

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