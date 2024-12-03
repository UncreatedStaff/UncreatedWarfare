using StackCleaner;
using System;
using System.Globalization;
using System.Text;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Translations.Util;
public static class TranslationFormattingUtility
{
    private const string ColorEndTag = "</color>";
    private const string ColorStartTag = "color=#";
#pragma warning disable CS8500

    /// <summary>
    /// Adds the correct color tags around text for TMPro or Unity rich text and appends it to a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="imgui">Use Unity rich text instead of TMPro.</param>
    public static StringBuilder AppendColorized(this StringBuilder stringBuilder, ReadOnlySpan<char> text, Color32 color, bool imgui = false, bool end = true)
    {
        if (text.Length == 0)
            return stringBuilder;

        Span<char> colorSpan = stackalloc char[color.a == 255 ? 6 : 8];

        HexStringHelper.FormatHexColor(color, colorSpan);

        stringBuilder.Append(imgui ? "<color=#" : "<#")
                     .Append(colorSpan)
                     .Append(">")
                     .Append(text);

        if (end)
        {
            stringBuilder.Append(ColorEndTag);
        }

        return stringBuilder;
    }

    /// <summary>
    /// Adds the correct color tags around text for TMPro or Unity rich text.
    /// </summary>
    /// <param name="imgui">Use Unity rich text instead of TMPro.</param>
    public static string Colorize(ReadOnlySpan<char> text, Color32 color, bool imgui = false)
    {
        return Colorize(text, color, imgui ? TranslationOptions.TranslateWithUnityRichText : TranslationOptions.None, StackColorFormatType.None);
    }

    /// <summary>
    /// Adds the correct color tags around text for TMPro or Unity rich text.
    /// </summary>
    /// <param name="imgui">Use Unity rich text instead of TMPro.</param>
    public static string Colorize(ReadOnlySpan<char> text, string hexColor, bool imgui = false)
    {
        if (!HexStringHelper.TryParseHexColor32(hexColor, out Color32 color))
            color = Color.white;

        return Colorize(text, color, imgui ? TranslationOptions.TranslateWithUnityRichText : TranslationOptions.None, StackColorFormatType.None);
    }

    /// <summary>
    /// Adds the correct color tags around text for TMPro or Unity rich text.
    /// </summary>
    /// <param name="imgui">Use Unity rich text instead of TMPro.</param>
    public static string Colorize(string text, Color32 color, bool imgui = false)
    {
        return Colorize(text, color, imgui ? TranslationOptions.TranslateWithUnityRichText : TranslationOptions.None, StackColorFormatType.None);
    }

    /// <summary>
    /// Adds the correct color tags around text for TMPro or Unity rich text.
    /// </summary>
    /// <param name="imgui">Use Unity rich text instead of TMPro.</param>
    public static string Colorize(string text, string hexColor, bool imgui = false)
    {
        if (!HexStringHelper.TryParseHexColor32(hexColor, out Color32 color))
            color = Color.white;

        return Colorize(text, color, imgui ? TranslationOptions.TranslateWithUnityRichText : TranslationOptions.None, StackColorFormatType.None);
    }

    /// <summary>
    /// Adds the correct color tags around text based on which flags are enabled in <paramref name="options"/>.
    /// </summary>
    public static string Colorize(string text, Color32 color, TranslationOptions options, StackColorFormatType terminalColoring)
    {
        if ((options & TranslationOptions.NoRichText) != 0)
        {
            return text;
        }

        if ((options & TranslationOptions.TranslateWithTerminalRichText) != 0)
        {
            return terminalColoring switch
            {
                StackColorFormatType.ExtendedANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToArgb(color), text),
                StackColorFormatType.ANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(color)), text),
                _ => text
            };
        }

        return Colorize(text.AsSpan(), color, options, terminalColoring);
    }

    /// <summary>
    /// Adds the correct color tags around text based on which flags are enabled in <paramref name="options"/>.
    /// </summary>
    public static unsafe string Colorize(ReadOnlySpan<char> text, Color32 color, TranslationOptions options, StackColorFormatType terminalColoring)
    {
        if ((options & TranslationOptions.NoRichText) != 0)
        {
            return new string(text);
        }

        if ((options & TranslationOptions.TranslateWithTerminalRichText) != 0)
        {
            return terminalColoring switch
            {
                StackColorFormatType.ExtendedANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToArgb(color), text),
                StackColorFormatType.ANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(color)), text),
                _ => new string(text)
            };
        }

        if ((options & TranslationOptions.TranslateWithUnityRichText) != 0)
        {
            ColorizeState state = default;
            state.Color = color;
            state.Text = &text;

            int len = 23 + text.Length;
            if (color.a != 255)
                len += 2;

            return string.Create(len, state, (span, state) =>
            {
                span[0] = '<';
                ColorStartTag.AsSpan().CopyTo(span[1..]);
                int index = color.a != 255 ? 8 : 6;
                HexStringHelper.FormatHexColor(state.Color, span.Slice(8, index));
                index += 8;
                span[index] = '>';
                ++index;
                state.Text->CopyTo(span[index..]);
                index += state.Text->Length;
                ColorEndTag.AsSpan().CopyTo(span[index..]);
            });
        }
        else
        {
            ColorizeState state = default;
            state.Color = color;
            state.Text = &text;

            int len = 17 + text.Length;
            if (color.a != 255)
                len += 2;

            return string.Create(len, state, (span, state) =>
            {
                span[0] = '<';
                span[1] = '#';
                int index = color.a != 255 ? 8 : 6;
                HexStringHelper.FormatHexColor(state.Color, span.Slice(2, index));
                index += 2;
                span[index] = '>';
                ++index;
                state.Text->CopyTo(span[index..]);
                index += state.Text->Length;
                ColorEndTag.AsSpan().CopyTo(span[index..]);
            });
        }
    }
    private unsafe struct ColorizeState
    {
        public Color32 Color;
        public ReadOnlySpan<char>* Text;
    }

    /// <summary>
    /// Converts a translation that was written with TMPro tags for convenience to IMGUI (Unity) rich text.
    /// </summary>
    public static unsafe string CreateIMGUIString(ReadOnlySpan<char> original)
    {
        if (original.Length < 6)
            return new string(original);

        int newStrLen = GetIMGUIStringLength(original, out int finalDepth);

        CreateIMGUIStringState state = default;
        state.FinalDepth = finalDepth;
        state.Original = &original;

        return string.Create(newStrLen, state, (span, state) =>
        {
            FormatIMGUIString(*state.Original, span, state.FinalDepth);
        });
    }

    private unsafe struct CreateIMGUIStringState
    {
        public ReadOnlySpan<char>* Original;
        public int FinalDepth;
    }

#pragma warning restore CS8500

    /// <summary>
    /// Isolates the color tag that surrounds the entire message, if any. Also returns span arguments to be able to reconstruct that.
    /// </summary>
    public static Color32? ExtractColor(ReadOnlySpan<char> text, out int innerStartIndex, out int innerLength)
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

        return firstColor;
    }

    private static void FormatIMGUIString(ReadOnlySpan<char> original, Span<char> output, int finalDepth)
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
            return;

        ReadOnlySpan<char> endTag = ColorEndTag.AsSpan();

        for (int i = 0; i < finalDepth; ++i)
        {
            endTag.CopyTo(output[(index + 1)..]);
            index += endTag.Length;
        }
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