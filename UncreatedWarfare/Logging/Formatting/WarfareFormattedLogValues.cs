using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using StackCleaner;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Logging.Formatting;

using ColorSetting = (int ExtendedColor, ConsoleColor BasicColor);

/// <summary>
/// A list of formatted values with a message from a log function.
/// </summary>
internal struct WarfareFormattedLogValues
{
    [ThreadStatic]
    private static char[] _formatBuffer;

    internal static readonly ColorSetting NumberDefault = (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow);
    internal static readonly ColorSetting StructDefault = (TerminalColorHelper.ToArgb(new Color32(134, 198, 145, 255)), ConsoleColor.DarkGreen);
    internal static readonly ColorSetting EnumDefault = (TerminalColorHelper.ToArgb(new Color32(184, 215, 163, 255)), ConsoleColor.DarkYellow);
    internal static readonly ColorSetting InterfaceDefault = (TerminalColorHelper.ToArgb(new Color32(184, 215, 163, 255)), ConsoleColor.DarkYellow);
    internal static readonly ColorSetting ObjectDefault = (TerminalColorHelper.ToArgb(new Color32(78, 201, 176, 255)), ConsoleColor.DarkCyan);
    internal static readonly ColorSetting SymbolsDefault = (TerminalColorHelper.ToArgb(new Color32(220, 220, 220, 255)), ConsoleColor.Gray);

    internal static readonly Dictionary<Type, ColorSetting> Colors = new Dictionary<Type, (int, ConsoleColor)>(36)
    {
        // number types
        { typeof(byte),     NumberDefault },
        { typeof(sbyte),    NumberDefault },
        { typeof(short),    NumberDefault },
        { typeof(ushort),   NumberDefault },
        { typeof(int),      NumberDefault },
        { typeof(uint),     NumberDefault },
        { typeof(long),     NumberDefault },
        { typeof(ulong),    NumberDefault },
        { typeof(float),    NumberDefault },
        { typeof(double),   NumberDefault },
        { typeof(decimal),  NumberDefault },
        { typeof(nint),     NumberDefault },
        { typeof(nuint),    NumberDefault },

        { typeof(MemberInfo), SymbolsDefault },
        { typeof(IMemberDefinition), SymbolsDefault },
        { typeof(IVariable), SymbolsDefault },
        { typeof(ParameterInfo), SymbolsDefault },
        
        { typeof(string),   (TerminalColorHelper.ToArgb(new Color32(214, 157, 133, 255)), ConsoleColor.DarkRed) },
        { typeof(char),     (TerminalColorHelper.ToArgb(new Color32(214, 157, 133, 255)), ConsoleColor.DarkRed) },

        { typeof(bool),     (TerminalColorHelper.ToArgb(new Color32(86, 156, 214, 255)), ConsoleColor.Blue) },
        { typeof(CSteamID), (TerminalColorHelper.ToArgb(new Color32(255, 153, 204, 255)), ConsoleColor.DarkMagenta) }
    };

    public readonly StringParameterList Parameters;
    public readonly string Message;
    public ITranslationValueFormatter? ValueFormatter;
    public WarfareFormattedLogValues(string message, object?[] args)
    {
        Parameters = new StringParameterList(args);
        Message = message;
    }

    public WarfareFormattedLogValues(string message, object? arg1)
    {
        Parameters = new StringParameterList(arg1);
        Message = message;
    }

    public WarfareFormattedLogValues(string message, object? arg1, object? arg2)
    {
        Parameters = new StringParameterList(arg1, arg2);
        Message = message;
    }

    public WarfareFormattedLogValues(string message, object? arg1, object? arg2, object? arg3)
    {
        Parameters = new StringParameterList(arg1, arg2, arg3);
        Message = message;
    }

    public WarfareFormattedLogValues(string message, object? arg1, object? arg2, object? arg3, object? arg4)
    {
        Parameters = new StringParameterList(arg1, arg2, arg3, arg4);
        Message = message;
    }

    public string Format(bool useColor)
    {
        return Parameters.Count == 0 ? Message : FormatIntl(useColor);
    }

    private readonly string FormatIntl(bool useColor)
    {
        _formatBuffer ??= new char[64];
        Span<int> indexBuffer = stackalloc int[Parameters.Count - 1];

        ArgumentFormat fmt = default;
        ValueFormatParameters parameters = new ValueFormatParameters(
            -1,
            CultureInfo.InvariantCulture,
            ValueFormatter?.LanguageService.GetDefaultLanguage()!,
            useColor ? TranslationOptions.ForTerminal : TranslationOptions.NoRichText,
            in fmt,
            null,
            null,
            null,
            Parameters.Count
        );

        StackColorFormatType color = !useColor || ValueFormatter == null
            ? StackColorFormatType.None
            : ValueFormatter.TranslationService.TerminalColoring;

        bool extended = color == StackColorFormatType.ExtendedANSIColor;

        int index = 0;
        for (int i = 0; i < Parameters.Count; ++i)
        {
            object? value = Parameters[i];
            int prefixSize, suffixSize, argb;
            scoped ReadOnlySpan<char> formatted;

            // lists
            if (value is IEnumerable enumerable and not string)
            {
                StringBuilder sb = new StringBuilder();
                string punctuationColor = TerminalColorHelper.GetTerminalColorSequence(GetArgb(extended, in SymbolsDefault), false);
                if (enumerable is ICollection col)
                {
                    if (useColor)
                        sb.Append(TerminalColorHelper.GetTerminalColorSequence(GetArgb(extended, in NumberDefault), false));
                    sb.Append(col.Count);
                }
                if (useColor)
                    sb.Append(punctuationColor);
                sb.Append("[ ");
                bool first = true;
                foreach (object? subValue in enumerable)
                {
                    if (first)
                        first = false;
                    else
                    {
                        if (useColor)
                            sb.Append(punctuationColor);

                        sb.Append(", ");
                    }

                    formatted = ValueFormatter?.Format(subValue, in parameters, null) ?? subValue?.ToString() ?? default;
                    if (useColor && ValueFormatter != null)
                    {
                        TryDecideColor(subValue, out argb, out _, out _, color);
                        sb.Append(TerminalColorHelper.GetTerminalColorSequence(argb, false));
                    }
                    
                    sb.Append(formatted);
                }
                if (useColor)
                    sb.Append(punctuationColor);
                sb.Append(" ]");
                if (useColor)
                    sb.Append(TerminalColorHelper.ForegroundResetSequence);

                formatted = sb.ToString();
                prefixSize = 0;
                suffixSize = 0;
                argb = 0;
            }
            else
            {
                formatted = ValueFormatter?.Format(value, in parameters, null) ?? value?.ToString() ?? default;

                if (useColor && ValueFormatter != null)
                    TryDecideColor(value, out argb, out prefixSize, out suffixSize, color);
                else
                {
                    prefixSize = 0;
                    suffixSize = 0;
                    argb = 0;
                }
            }

            int ttlSize = formatted.Length + prefixSize + suffixSize;

            if (_formatBuffer.Length <= index + ttlSize)
            {
                char[] newArray = new char[Math.Max(_formatBuffer.Length * 2, index + ttlSize + ((Parameters.Count - i - 1) * 16))];

                if (index > 0)
                    Buffer.BlockCopy(_formatBuffer, 0, newArray, 0, index * sizeof(char));

                _formatBuffer = newArray;
            }

            Span<char> format = _formatBuffer.AsSpan(index);

            formatted.CopyTo(format[prefixSize..]);

            if (prefixSize > 0)
            {
                TerminalColorHelper.WriteTerminalColorSequence(format[..prefixSize], argb, false);
                TerminalColorHelper.ForegroundResetSequence.AsSpan().CopyTo(format.Slice(prefixSize + formatted.Length, suffixSize));
            }

            if (index != 0)
                indexBuffer[i - 1] = index;

            index += ttlSize;
        }

        return TranslationFormattingUtility.FormatString(Message, _formatBuffer.AsSpan(0, index), indexBuffer);
    }

    internal static void TryDecideColor(object? parameter, out int argb, out int prefixSize, out int suffixSize, StackColorFormatType coloring)
    {
        bool extended = coloring == StackColorFormatType.ExtendedANSIColor;
                                                                       
        if (!extended && coloring != StackColorFormatType.ANSIColor
            // null is auto-formatted for terminal
            || parameter == null || parameter.Equals(null))
        {
            argb = 0;
            prefixSize = 0;
            suffixSize = 0;
            return;
        }

        suffixSize = TerminalColorHelper.ForegroundResetSequence.Length;

        argb = 0;
        Type type = parameter.GetType();

        if (parameter is Type type2)
        {
            type = type2;
        }

        Type? nullableType = Nullable.GetUnderlyingType(type);
        bool found = false;
        if (Colors.TryGetValue(nullableType ?? type, out ColorSetting setting))
        {
            found = true;
            argb = GetArgb(extended, in setting);
        }
        else if (!type.IsValueType)
        {
            foreach (KeyValuePair<Type, ColorSetting> settingRow in Colors)
            {
                if (!settingRow.Key.IsAssignableFrom(type))
                    continue;

                setting = settingRow.Value;
                argb = GetArgb(extended, in setting);
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (type.IsEnum)
            {
                argb = GetArgb(extended, in EnumDefault);
            }
            else if (type.IsValueType)
            {
                argb = GetArgb(extended, in StructDefault);
            }
            else if (type.IsInterface)
            {
                argb = GetArgb(extended, in InterfaceDefault);
            }
            else
            {
                argb = GetArgb(extended, in ObjectDefault);
            }
        }

        prefixSize = TerminalColorHelper.GetTerminalColorSequenceLength(argb, false);
    }

    internal static int GetArgb(bool extended, in ColorSetting setting)
    {
        return extended ? setting.ExtendedColor : (int)setting.BasicColor;
    }
}