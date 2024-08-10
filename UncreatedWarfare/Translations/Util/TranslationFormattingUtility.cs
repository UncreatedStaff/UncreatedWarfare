using System;
using System.Globalization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.Util;
public static class TranslationFormattingUtility
{
    private const string ColorEndTag = "</color>";
    private const string ColorStartTag = "color=#";

    /// <summary>
    /// Converts a translation that was written with TMPro tags for convenience to IMGUI (Unity) rich text.
    /// </summary>
    public static string CreateIMGUIString(ReadOnlySpan<char> original)
    {
        if (original.Length < 6)
            return new string(original);

        int newStrLen = GetIMGUIStringLength(original, out int finalDepth);
        Span<char> newStr = stackalloc char[newStrLen];

        int len = FormatIMGUIString(original, newStr, finalDepth);

        return new string(newStr[..len]);
    }

    /// <summary>
    /// Isolates the color tag that surrounds the entire message, if any. Also returns span arguments to be able to reconstruct that.
    /// </summary>
    public static Color ExtractColor(ReadOnlySpan<char> text, out int innerStartIndex, out int innerLength)
    {
        innerStartIndex = 0;
        innerLength = text.Length;

        // trim
        int stInd = 0;
        for (int i = 0; i < text.Length; ++i)
        {
            if (char.IsWhiteSpace(text[i]))
                continue;

            stInd = i;
            break;
        }

        int endInd = 0;
        for (int i = text.Length - 1; i >= 0; --i)
        {
            if (char.IsWhiteSpace(text[i]))
                continue;

            endInd = i;
            break;
        }

        int depth = 0;
        Color32 color = default;
        Color32? firstColor = null;
        for (int i = stInd; i <= endInd; ++i)
        {
            if (text[i] != '<')
                continue;

            int newInd = i;
            if (TryReadTMProColorTag(text, i, ref color, ref newInd, out _))
            {
                if (i == stInd)
                {
                    innerStartIndex = newInd + 1;
                    innerLength = text.Length - innerStartIndex;
                    firstColor = color;
                }

                i = newInd;

                depth++;
            }
            else if (TryMatchEndColorTag(text, i, ref newInd))
            {
                depth = Math.Max(0, depth - 1);
                if (depth != 0)
                {
                    i = newInd;
                    continue;
                }

                if (newInd != endInd)
                {
                    innerStartIndex = 0;
                    innerLength = text.Length;
                    return Color.white;
                }

                innerLength = i - innerStartIndex;
                i = newInd;
            }
        }

        return firstColor ?? Color.white;
    }

    private static int FormatIMGUIString(ReadOnlySpan<char> original, Span<char> output, int finalDepth)
    {
        ReadOnlySpan<char> startTag = ColorStartTag.AsSpan();

        int index = -1;
        Color32 color = default;
        for (int i = 0; i < original.Length; ++i)
        {
            output[++index] = original[i];

            int endPos = i;
            if (original[i] != '<' || !TryReadTMProColorTag(original, i, ref color, ref endPos, out _))
                continue;

            startTag.CopyTo(output[(index + 1)..]);
            index += startTag.Length;
            int clrSize = HexStringHelper.FormatHexColor(color, output.Slice(++index, color.a == 255 ? 6 : 8));
            index += clrSize - 1;
            output[++index] = '>';
            i = endPos;
        }

        if (finalDepth == 0)
            return index + 1;

        ReadOnlySpan<char> endTag = ColorEndTag.AsSpan();

        for (int i = 0; i < finalDepth; ++i)
        {
            endTag.CopyTo(output[(index + 1)..]);
            index += endTag.Length;
        }
        return index + 1;
    }

    private static int GetIMGUIStringLength(ReadOnlySpan<char> original, out int finalDepth)
    {
        int depth = 0;
        int lenToAdd = 0;
        Color32 color = default;
        for (int i = 0; i < original.Length; ++i)
        {
            if (original[i] != '<')
                continue;

            if (TryReadTMProColorTag(original, i, ref color, ref i, out int clrSize))
            {
                lenToAdd += 6 /* color= */
                            + clrSize switch
                            {
                                // difference in the number of characters used by the shortcut and the number needed by unity rich text (6 or 8)
                                1 => 5,
                                2 => 6,
                                3 => 3,
                                4 => 4,
                                _ => 0
                            };
                depth++;
            }
            else if (TryMatchEndColorTag(original, i, ref i))
            {
                depth = Math.Max(0, depth - 1);
            }
        }

        lenToAdd += depth * 8;
        finalDepth = depth;
        return lenToAdd + original.Length;
    }

    private static bool TryReadTMProColorTag(ReadOnlySpan<char> original, int stInd, ref Color32 color, ref int endPos, out int clrSize)
    {
        clrSize = 0;

        // assume character 0 is already '<'.
        int textSt = original.IndexOf('#', stInd + 1);
        if (textSt == -1)
            return false;

        for (int i = textSt + 1; i < original.Length; ++i)
        {
            if (char.IsWhiteSpace(original[i]))
                continue;

            textSt = i;
            break;
        }

        int end = original.IndexOf('>', textSt + 1);
        int origEnd = end;
        if (end == -1)
            return false;

        for (int i = end - 1; i >= textSt; --i)
        {
            if (char.IsWhiteSpace(original[i]))
                continue;

            end = i;
            break;
        }

        clrSize = end - textSt + 1;
        if (!HexStringHelper.TryParseHexColor32(original.Slice(textSt, clrSize), out color))
            return false;

        endPos = origEnd;
        return true;
    }

    private static bool TryMatchEndColorTag(ReadOnlySpan<char> original, int stInd, ref int endPos)
    {
        // assume character 0 is already '<'.
        int i = stInd + 1;
        for (; i < original.Length; ++i)
        {
            char c = original[i];
            if (char.IsWhiteSpace(c))
                continue;

            if (c != '/')
                return false;

            break;
        }

        if (i + 6 >= original.Length)
            return false;

        if (!(original[i + 1] == 'c'
           && original[i + 2] == 'o'
           && original[i + 3] == 'l'
           && original[i + 4] == 'o'
           && original[i + 5] == 'r'))
        {
            return false;
        }

        i += 6;
        for (; i < original.Length; ++i)
        {
            char c = original[i];
            if (char.IsWhiteSpace(c))
                continue;

            if (c == '>')
            {
                endPos = i;
                return true;
            }

            break;
        }

        return false;
    }

    /// <summary>
    /// Less error-prone <see cref="string.Format(string,object[])"/> using spans.
    /// Only works with <see cref="string"/>s and doesn't take the format into account.
    /// </summary>
    /// <param name="indices">The first index is assumed to be zero, this is a list of all the following indices.</param>
    public static string FormatString(ReadOnlySpan<char> format, ReadOnlySpan<char> strings, ReadOnlySpan<int> indices)
    {
        if (format.Length < 3)
            return new string(format);

        int formattedStrLen = GetFormattedStringLength(format, strings, indices);
        Span<char> output = stackalloc char[formattedStrLen];

        formattedStrLen = FormatString(output, format, strings, indices);

        return new string(output[..formattedStrLen]);
    }

    private static int FormatString(Span<char> output, ReadOnlySpan<char> format, ReadOnlySpan<char> strings, ReadOnlySpan<int> indices)
    {
        ReadOnlySpan<char> formatVal = default;
        int argIndex = 0,
            index = 0,
            lastFormattingArgEndPt = -1,
            amtToCopy,
            argCt = indices.Length + 1;

        for (int i = 0; i < format.Length; ++i)
        {
            int oldI = i;
            if (!TrySliceFormatArgument(format, ref i, argCt, ref argIndex, ref formatVal))
                continue;

            amtToCopy = oldI - lastFormattingArgEndPt - 1;
            if (amtToCopy != 0)
                format.Slice(lastFormattingArgEndPt + 1, amtToCopy).CopyTo(output[index..]);
            index += amtToCopy;
            int strInd = argIndex == 0 ? 0 : indices[argIndex - 1];
            int strLen = (argIndex >= indices.Length ? strings.Length : indices[argIndex]) - strInd;
            if (strLen != 0)
                strings.Slice(strInd, strLen).CopyTo(output[index..]);
            index += strLen;
            lastFormattingArgEndPt = i;
        }

        amtToCopy = format.Length - lastFormattingArgEndPt - 1;
        format.Slice(lastFormattingArgEndPt + 1, amtToCopy).CopyTo(output[index..]);
        return index + amtToCopy;
    }

    private static int GetFormattedStringLength(ReadOnlySpan<char> format, ReadOnlySpan<char> strings, ReadOnlySpan<int> indices)
    {
        ReadOnlySpan<char> formatVal = default;
        int argIndex = 0,
            len = 0,
            lastFormattingArgEndPt = -1,
            argCt = indices.Length + 1;

        for (int i = 0; i < format.Length; ++i)
        {
            int oldI = i;
            if (!TrySliceFormatArgument(format, ref i, argCt, ref argIndex, ref formatVal))
                continue;

            len += oldI - lastFormattingArgEndPt - 1;
            int strLen = (argIndex >= indices.Length ? strings.Length : indices[argIndex]) - (argIndex == 0 ? 0 : indices[argIndex - 1]);
            len += strLen;
            lastFormattingArgEndPt = i;
        }

        len += format.Length - lastFormattingArgEndPt - 1;
        return len;
    }

    private static bool TrySliceFormatArgument(ReadOnlySpan<char> format, ref int index, int argCt, ref int argIndex, ref ReadOnlySpan<char> formatVal)
    {
        if (format[index] != '{' || index == format.Length - 1 || format[index + 1] == '{')
            return false;

        int endBracket = format.IndexOf('}', index + 1);

        if (endBracket == -1)
            return false;

        int fmtSeparator = format.Slice(index + 1, endBracket - index - 1).LastIndexOf(':');
        if (fmtSeparator != -1)
            fmtSeparator += index + 1;
        ReadOnlySpan<char> argStr = format.Slice(index + 1, (fmtSeparator == -1 ? endBracket : fmtSeparator) - index - 1);
        if (!int.TryParse(argStr, NumberStyles.Number, CultureInfo.InvariantCulture, out argIndex)
            || argIndex < 0
            || argIndex >= argCt)
        {
            return false;
        }

        if (fmtSeparator != -1 && endBracket - fmtSeparator > 1)
        {
            formatVal = format.Slice(fmtSeparator + 1, endBracket - fmtSeparator - 1);
        }

        index = endBracket;
        return true;
    }
}