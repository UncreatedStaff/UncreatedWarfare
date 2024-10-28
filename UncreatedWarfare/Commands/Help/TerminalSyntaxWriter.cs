using StackCleaner;
using System;
using System.Globalization;
using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

public class TerminalSyntaxWriter : ISyntaxWriter
{
    private static readonly Color32 BackgroundColor = new Color32(179, 179, 179, 255);

    private readonly StackColorFormatType _terminalColor;
    private readonly CultureInfo _culture;
    private readonly bool _needsResets;
    private bool _lastWasRemainder;
    public TerminalSyntaxWriter(bool needsResets, CultureInfo culture, StackColorFormatType terminalColor)
    {
        _culture = culture;
        _needsResets = needsResets;
        _terminalColor = terminalColor;
    }

    public void BeginWrite(StringBuilder bldr, SyntaxStringType type)
    {
        WriteReset(bldr);
    }
    public string EndWrite(StringBuilder bldr, SyntaxStringType type)
    {
        _lastWasRemainder = false;
        bldr.Append(TerminalColorHelper.ForegroundResetSequence);
        return bldr.ToString();
    }

    private void AppendColor(StringBuilder bldr, Color32 color, ReadOnlySpan<char> value)
    {
        switch (_terminalColor)
        {
            case StackColorFormatType.ExtendedANSIColor:
                scoped Span<char> span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(color.r, color.g, color.b)];
                TerminalColorHelper.WriteTerminalColorSequence(span, color.r, color.g, color.b);
                bldr.Append(span).Append(value);
                break;

            case StackColorFormatType.ANSIColor:
                ConsoleColor color8 = TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(color));
                span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(color8)];
                TerminalColorHelper.WriteTerminalColorSequence(span, color8);
                bldr.Append(span).Append(value);
                break;

            default:
                bldr.Append(value);
                return;
        }

        if (_needsResets)
            WriteReset(bldr);
    }

    private void AppendColor(StringBuilder bldr, Color32 color, char value)
    {
        switch (_terminalColor)
        {
            case StackColorFormatType.ExtendedANSIColor:
                scoped Span<char> span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(color.r, color.g, color.b)];
                TerminalColorHelper.WriteTerminalColorSequence(span, color.r, color.g, color.b);
                bldr.Append(span).Append(value);
                break;

            case StackColorFormatType.ANSIColor:
                ConsoleColor color8 = TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(color));
                span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(color8)];
                TerminalColorHelper.WriteTerminalColorSequence(span, color8);
                bldr.Append(span).Append(value);
                break;

            default:
                bldr.Append(value);
                return;
        }

        if (_needsResets)
            WriteReset(bldr);
    }

    private void WriteReset(StringBuilder bldr)
    {
        switch (_terminalColor)
        {
            case StackColorFormatType.ExtendedANSIColor:
                scoped Span<char> span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b)];
                TerminalColorHelper.WriteTerminalColorSequence(span, BackgroundColor.r, BackgroundColor.g, BackgroundColor.b);
                bldr.Append(span);
                break;

            case StackColorFormatType.ANSIColor:
                ConsoleColor color8 = TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(BackgroundColor));
                span = stackalloc char[TerminalColorHelper.GetTerminalColorSequenceLength(color8)];
                TerminalColorHelper.WriteTerminalColorSequence(span, color8);
                bldr.Append(span);
                break;
        }
    }

    public void WritePrefixCharacter(StringBuilder bldr, string prefixCharacter)
    {
        _lastWasRemainder = false;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, prefixCharacter);
    }

    public void WriteCommandName(StringBuilder bldr, string commandName, bool isAlias)
    {
        _lastWasRemainder = false;
        if (isAlias)
            bldr.Append("<b>");
        AppendColor(bldr, SyntaxColorPalette.VerbatimColor, commandName);
        if (isAlias)
            bldr.Append("</b>");
    }

    public void WriteParameter(StringBuilder bldr, ICommandParameterDescriptor parameter, string? overrideParameterName, bool isRequiredLookAtTarget, bool isOptionalLookAtTarget)
    {
        _lastWasRemainder = false;
        bool bold = parameter.Types.Count == 1 && parameter.Types[0] == typeof(VerbatimParameterType) && overrideParameterName == null;

        if (bold)
            bldr.Append("<b>");

        if (isRequiredLookAtTarget)
            AppendColor(bldr, SyntaxColorPalette.Punctuation, SyntaxColorPalette.LookAtTargetInGamePrefix);
        else if (isOptionalLookAtTarget)
            AppendColor(bldr, SyntaxColorPalette.Punctuation, SyntaxColorPalette.LookAtTargetInGameOptionalPrefix);

        AppendColor(bldr, SyntaxColorPalette.GetColor(parameter.Types), overrideParameterName ?? parameter.Name.ToLower(_culture));

        if (bold)
            bldr.Append("</b>");
    }

    public void WriteOpenParameterTemplate(StringBuilder bldr, bool optional)
    {
        _lastWasRemainder = false;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, optional ? '[' : '<');
    }

    public void WriteCloseParameterTemplate(StringBuilder bldr, bool optional)
    {
        if (_lastWasRemainder)
        {
            bldr.Append(' ');
            _lastWasRemainder = false;
        }
        AppendColor(bldr, SyntaxColorPalette.Punctuation, optional ? ']' : '>');
    }

    public void WriteParameterTemplateSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, " | ");
    }

    public void WriteParameterTemplateRemainder(StringBuilder bldr)
    {
        _lastWasRemainder = true;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, "...");
    }

    public void WriteParameterTemplateChainSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, ' ');
    }

    public void WriteParameterSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        AppendColor(bldr, SyntaxColorPalette.Punctuation, ' ');
    }

    public void WriteFlag(StringBuilder bldr, ICommandFlagDescriptor flag)
    {
        AppendColor(bldr, SyntaxColorPalette.Punctuation, '-');
        AppendColor(bldr, SyntaxColorPalette.VerbatimColor, flag.Name);
    }

    public void WriteBasicTag(StringBuilder bldr, CommandSyntaxFormatter.TagType type, ReadOnlySpan<char> tagName)
    {
        AppendColor(bldr, SyntaxColorPalette.GetColor(type), tagName);
    }
}
