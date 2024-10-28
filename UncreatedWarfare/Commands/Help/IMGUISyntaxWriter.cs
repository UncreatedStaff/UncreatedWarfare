using System;
using System.Globalization;
using System.Text;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

public class IMGUISyntaxWriter : ISyntaxWriter
{
    private bool _lastWasRemainder;
    private readonly CultureInfo _culture;

    public IMGUISyntaxWriter(CultureInfo culture)
    {
        _culture = culture;
    }

    public void BeginWrite(StringBuilder bldr, SyntaxStringType type) { }
    public string EndWrite(StringBuilder bldr, SyntaxStringType type)
    {
        _lastWasRemainder = false;
        return bldr.ToString();
    }

    private void AppendColor(StringBuilder bldr, Color32 color, ReadOnlySpan<char> value)
    {
        bldr.Append("<color=#")
            .Append(HexStringHelper.FormatHexColor(color))
            .Append('>')
            .Append(value)
            .Append("</color>");
    }

    private void AppendColor(StringBuilder bldr, Color32 color, char value)
    {
        bldr.Append("<color=#")
            .Append(HexStringHelper.FormatHexColor(color))
            .Append('>')
            .Append(value)
            .Append("</color>");
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