using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players.Permissions;

// ReSharper disable once CheckNamespace
namespace Uncreated.Warfare.Interaction.Commands.Syntax;

/// <summary>
/// Describes a command or parameter for a command (including sub-commands).
/// </summary>
public interface ICommandParameterDescriptor
{
    /// <summary>
    /// Name of the parameter in proper-case format.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The parent of this parameter descriptor, if any.
    /// </summary>
    ICommandParameterDescriptor? Parent { get; }

    /// <summary>
    /// Translatable description of the command or sub-command.
    /// </summary>
    TranslationList? Description { get; }

    /// <summary>
    /// If the parameter is optional. All other parameters on this level must also be marked optional.
    /// </summary>
    bool Optional { get; }

    /// <summary>
    /// If the parameter spans the rest of the command arguments, including spaces.
    /// </summary>
    bool Remainder { get; }

    /// <summary>
    /// The permission needed to use this parameter.
    /// </summary>
    PermissionLeaf Permission { get; }

    /// <summary>
    /// Number of sub-parameters to chain into one 'parameter' including this one. Requires that all child parameters up to that amount have only one parameter.
    /// </summary>
    /// <remarks>Example: <c>/tp [x y z|location|player]</c> where <c>x.ChainDisplayAmount = 3</c>.</remarks>
    int Chain { get; }

    /// <summary>
    /// Values that can be entered instead of <see cref="Name"/>.
    /// </summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// List of valid types that can be used for this parameter.
    /// </summary>
    /// <remarks>Verbatim parameter types will have the type of <see cref="VerbatimParameterType"/>.</remarks>
    IReadOnlyList<Type> Types { get; }

    /// <summary>
    /// List of sub-parameters.
    /// </summary>
    IReadOnlyList<ICommandParameterDescriptor> Parameters { get; }

    /// <summary>
    /// List of valid flags for this command or parameter.
    /// </summary>
    IReadOnlyList<ICommandFlagDescriptor> Flags { get; }
}