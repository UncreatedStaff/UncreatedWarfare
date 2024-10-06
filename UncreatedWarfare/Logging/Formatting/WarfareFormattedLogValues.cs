using StackCleaner;
using System;
using System.Collections.Generic;
using System.Globalization;
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

    private static readonly Dictionary<Type, ColorSetting> Colors = new Dictionary<Type, (int, ConsoleColor)>(36)
    {
        // number types
        { typeof(byte),     (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(sbyte),    (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(short),    (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(ushort),   (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(int),      (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(uint),     (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(long),     (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(ulong),    (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(float),    (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(double),   (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(decimal),  (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(nint),     (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        { typeof(nuint),    (TerminalColorHelper.ToArgb(new Color32(181, 206, 168, 255)), ConsoleColor.DarkYellow) },
        
        { typeof(string),   (TerminalColorHelper.ToArgb(new Color32(214, 157, 133, 255)), ConsoleColor.DarkRed) },
        { typeof(char),     (TerminalColorHelper.ToArgb(new Color32(214, 157, 133, 255)), ConsoleColor.DarkRed) },

        { typeof(bool),     (TerminalColorHelper.ToArgb(new Color32(86, 156, 214, 255)), ConsoleColor.Blue) }
    };

    private static readonly ColorSetting StructDefault    = (TerminalColorHelper.ToArgb(new Color32(134, 198, 145, 255)), ConsoleColor.DarkGreen);
    private static readonly ColorSetting EnumDefault      = (TerminalColorHelper.ToArgb(new Color32(184, 215, 163, 255)), ConsoleColor.DarkYellow);
    private static readonly ColorSetting InterfaceDefault = (TerminalColorHelper.ToArgb(new Color32(184, 215, 163, 255)), ConsoleColor.DarkYellow);
    private static readonly ColorSetting ObjectDefault    = (TerminalColorHelper.ToArgb(new Color32(78, 201, 176, 255)), ConsoleColor.DarkCyan);
    private static readonly ColorSetting SymbolsDefault   = (TerminalColorHelper.ToArgb(new Color32(220, 220, 220, 255)), ConsoleColor.Gray);

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

        int index = 0;
        for (int i = 0; i < Parameters.Count; ++i)
        {
            ReadOnlySpan<char> formatted = ValueFormatter?.Format(Parameters[i], in parameters) ?? Parameters[i]?.ToString() ?? default;

            int prefixSize, suffixSize, argb;
            if (useColor && ValueFormatter != null)
                TryDecideColor(Parameters[i], out argb, out prefixSize, out suffixSize);
            else
            {
                prefixSize = 0;
                suffixSize = 0;
                argb = 0;
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
                FormatColor(argb, format[..prefixSize], format.Slice(prefixSize + formatted.Length, suffixSize));

            if (index != 0)
                indexBuffer[i - 1] = index;

            index += ttlSize;
        }

        return TranslationFormattingUtility.FormatString(Message, _formatBuffer.AsSpan(0, index), indexBuffer);
    }

    private readonly void TryDecideColor(object parameter, out int argb, out int prefixSize, out int suffixSize)
    {
        StackColorFormatType coloring = ValueFormatter!.TranslationService.TerminalColoring;
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

        Type type = parameter.GetType();
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (Colors.TryGetValue(nullableType ?? type, out ColorSetting setting))
        {
            argb = GetArgb(extended, in setting);
        }
        else if (type.IsEnum)
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
        else if (parameter is IEnumerable)
        {
            argb = GetArgb(extended, in SymbolsDefault);
        }
        else
        {
            argb = GetArgb(extended, in ObjectDefault);
        }

        prefixSize = TerminalColorHelper.GetTerminalColorSequenceLength(argb, false);
    }

    private static void FormatColor(int argb, Span<char> prefix, Span<char> suffix)
    {
        unchecked
        {
            if ((byte)(argb >> 24) == 0) // console color
            {
                ConsoleColor color = (ConsoleColor)argb;
                TerminalColorHelper.WriteTerminalColorSequenceCode(prefix, 0, color, false);
            }
            else
            {
                TerminalColorHelper.WriteTerminalColorSequenceCode(prefix, 0, (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, false);
            }
        }

        TerminalColorHelper.ForegroundResetSequence.AsSpan().CopyTo(suffix);
    }

    private static int GetArgb(bool extended, in ColorSetting setting)
    {
        return extended ? setting.ExtendedColor : (int)setting.BasicColor;
    }
}