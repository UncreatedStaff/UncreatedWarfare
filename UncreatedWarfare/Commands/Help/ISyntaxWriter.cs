using System;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Uncreated.Warfare.Interaction.Commands.Syntax;

/// <summary>
/// Non-thread-safe command syntax formatter/writer.
/// </summary>
public interface ISyntaxWriter
{
    /// <summary>
    /// Runs any set-up needed before creating a syntax string.
    /// </summary>
    void BeginWrite(StringBuilder bldr, SyntaxStringType type);

    /// <summary>
    /// Runs any clean-up needed after creating a syntax string and returns the final string.
    /// </summary>
    string EndWrite(StringBuilder bldr, SyntaxStringType type);

    /// <summary>
    /// Write the slash character (or whatever) to the <see cref="StringBuilder"/>.
    /// </summary>
    void WritePrefixCharacter(StringBuilder stringBuilder, string prefixCharacter);

    /// <summary>
    /// Write the command name (help, home, etc) to the <see cref="StringBuilder"/>.
    /// </summary>
    void WriteCommandName(StringBuilder stringBuilder, string commandName, bool isAlias);

    /// <summary>
    /// Write a parameter (with an optional <paramref name="overrideParameterName"/> which should be used as the name instead of the actual name) to the <see cref="StringBuilder"/>.
    /// </summary>
    void WriteParameter(StringBuilder stringBuilder, ICommandParameterDescriptor parameter, string? overrideParameterName, bool isRequiredLookAtTarget, bool isOptionalLookAtTarget);

    /// <summary>
    /// Write the characters that open a set of parameter options to the <see cref="StringBuilder"/>. This is usually '&lt;' for required parameters and '[' for optional parameters.
    /// </summary>
    void WriteOpenParameterTemplate(StringBuilder stringBuilder, bool optional);

    /// <summary>
    /// Write the characters that close a set of parameter options to the <see cref="StringBuilder"/>. This is usually '&gt;' for required parameters and ']' for optional parameters.
    /// </summary>
    void WriteCloseParameterTemplate(StringBuilder stringBuilder, bool optional);

    /// <summary>
    /// Write the characters that separate two parameter names in a parameter template set to the <see cref="StringBuilder"/>. This is usually ' | '
    /// </summary>
    void WriteParameterTemplateSeparator(StringBuilder stringBuilder);

    /// <summary>
    /// Write the characters that indicate a parameter which doesn't care about the spaces after it to the <see cref="StringBuilder"/>. This is usually '... '.
    /// </summary>
    void WriteParameterTemplateRemainder(StringBuilder stringBuilder);

    /// <summary>
    /// Write the characters that separate chained parameters to the <see cref="StringBuilder"/>. This is usually just a space.
    /// </summary>
    void WriteParameterTemplateChainSeparator(StringBuilder stringBuilder);

    /// <summary>
    /// Write the characters that separate parameter template sets to the <see cref="StringBuilder"/>. This is usually just a space.
    /// </summary>
    void WriteParameterSeparator(StringBuilder stringBuilder);

    /// <summary>
    /// Write a flag to the <see cref="StringBuilder"/>.
    /// </summary>
    void WriteFlag(StringBuilder stringBuilder, ICommandFlagDescriptor flag);

    /// <summary>
    /// Write text for a tag to the <see cref="StringBuilder"/>.
    /// </summary>
    void WriteBasicTag(StringBuilder stringBuilder, CommandSyntaxFormatter.TagType type, ReadOnlySpan<char> tagName);
}

public enum SyntaxStringType
{
    SyntaxString,
    RichDescription
}