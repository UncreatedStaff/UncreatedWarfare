using System;
using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Commands;

/// <summary>
/// Defines information about a single command or sub-command.
/// </summary>
public interface ICommandDescriptor
{
    /// <summary>
    /// Type that contains the code for the command.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Name of the command.
    /// </summary>
    /// <remarks>Example: 'home' for /home.</remarks>
    string CommandName { get; }

    /// <summary>
    /// Aliases for <see cref="CommandName"/>.
    /// </summary>
    IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// If this command is hid from /help by a <see cref="HideFromHelpAttribute"/>.
    /// </summary>
    bool HideFromHelp { get; }

    /// <summary>
    /// Typing '/command help' will not switch to /help if this is <see langword="true"/>, which can be set using the <see cref="DisableAutoHelpAttribute"/>.
    /// </summary>
    /// <remarks>This is not propagated to sub-commands.</remarks>
    bool AutoHelpDisabled { get; }

    /// <summary>
    /// If the command can be executed. Some parent commands may not be executable.
    /// </summary>
    bool IsExecutable { get; }

    /// <summary>
    /// If this command represents a vanilla command (<see cref="Type"/> will be of type <see cref="Command"/>).
    /// </summary>
    bool IsVanillaCommand { get; }

    /// <summary>
    /// If this command has a parent command.
    /// </summary>
    bool IsSubCommand { get; }

    /// <summary>
    /// If this command is a sub-command, this is the info for the parent command.
    /// </summary>
    ICommandDescriptor? ParentCommand { get; }

    /// <summary>
    /// The command that will automatically take over if this command is called. It should usually be a child command.
    /// </summary>
    ICommandDescriptor? RedirectCommandInfo { get; }

    /// <summary>
    /// This command's position in it's family tree. 0 = parent command, 1 = child, 2 = child's child, etc.
    /// </summary>
    int HierarchyLevel { get; }

    /// <summary>
    /// Information about how to display the command in /help.
    /// </summary>
    ICommandParameterDescriptor Metadata { get; }

    /// <summary>
    /// List of all sub-commands for a command. These commands may also have sub-commands.
    /// </summary>
    IReadOnlyList<ICommandDescriptor> SubCommands { get; }

    /// <summary>
    /// Default permission generated from the command name. Can be overridden with <see cref="CommandAttribute.PermissionOverride"/>.
    /// </summary>
    /// <remarks><c>command.[command name]</c></remarks>
    PermissionLeaf DefaultPermission { get; }

    /// <summary>
    /// A list of other permissions the caller must have or could have instead (depending on <see cref="OtherPermissionsAreAnd"/>.
    /// </summary>
    IReadOnlyList<PermissionLeaf> OtherPermissions { get; }

    /// <summary>
    /// If <see cref="OtherPermissions"/> represents a list of permissions the user must have including <see cref="DefaultPermission"/>, instead of a list of alternative permissions the user could have.
    /// </summary>
    bool OtherPermissionsAreAnd { get; }
}
