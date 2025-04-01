using System;
using System.Globalization;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Uncreated.Warfare.Interaction.Commands.Syntax;

public class PlainTextSyntaxWriter : ISyntaxWriter
{
    private bool _lastWasRemainder;
    private readonly CultureInfo _culture;
    private readonly bool _forInGame;

    public PlainTextSyntaxWriter(CultureInfo culture, bool forInGame = false)
    {
        _culture = culture;
        _forInGame = forInGame;
    }

    public void BeginWrite(StringBuilder bldr, SyntaxStringType type) { }
    public string EndWrite(StringBuilder bldr, SyntaxStringType type)
    {
        _lastWasRemainder = false;
        return bldr.ToString();
    }

    public void WritePrefixCharacter(StringBuilder bldr, string prefixCharacter)
    {
        _lastWasRemainder = false;
        bldr.Append(prefixCharacter);
    }

    public void WriteCommandName(StringBuilder bldr, string commandName, bool isAlias)
    {
        _lastWasRemainder = false;
        bldr.Append(commandName);
    }

    public void WriteParameter(StringBuilder bldr, ICommandParameterDescriptor parameter, string? overrideParameterName, bool isRequiredLookAtTarget, bool isOptionalLookAtTarget)
    {
        _lastWasRemainder = false;
        bool isVerbatim = parameter.Types.Count == 1 && parameter.Types[0] == typeof(VerbatimParameterType) && overrideParameterName == null;

        if (isRequiredLookAtTarget)
            bldr.Append(_forInGame ? SyntaxColorPalette.LookAtTargetInGamePrefix : SyntaxColorPalette.LookAtTargetTerminalPrefix);
        else if (isOptionalLookAtTarget)
            bldr.Append(_forInGame ? SyntaxColorPalette.LookAtTargetInGameOptionalPrefix : SyntaxColorPalette.LookAtTargetTerminalOptionalPrefix);

        if (isVerbatim && !isRequiredLookAtTarget)
            bldr.Append("'");

        bldr.Append(overrideParameterName ?? parameter.Name.ToLower(_culture));

        if (isVerbatim && !isRequiredLookAtTarget)
            bldr.Append("'");
    }

    public void WriteOpenParameterTemplate(StringBuilder bldr, bool optional)
    {
        _lastWasRemainder = false;
        bldr.Append(optional ? '[' : '<');
    }

    public void WriteCloseParameterTemplate(StringBuilder bldr, bool optional)
    {
        if (_lastWasRemainder)
        {
            bldr.Append(' ');
            _lastWasRemainder = false;
        }
        bldr.Append(optional ? ']' : '>');
    }

    public void WriteParameterTemplateSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        bldr.Append(" | ");
    }

    public void WriteParameterTemplateRemainder(StringBuilder bldr)
    {
        _lastWasRemainder = true;
        bldr.Append("...");
    }

    public void WriteParameterTemplateChainSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        bldr.Append(' ');
    }

    public void WriteParameterSeparator(StringBuilder bldr)
    {
        _lastWasRemainder = false;
        bldr.Append(' ');
    }

    public void WriteFlag(StringBuilder bldr, ICommandFlagDescriptor flag)
    {
        bldr.Append('-');
        bldr.Append(flag.Name);
    }

    public void WriteBasicTag(StringBuilder bldr, CommandSyntaxFormatter.TagType type, ReadOnlySpan<char> tagName)
    {
        bldr.Append(tagName);
    }
}
