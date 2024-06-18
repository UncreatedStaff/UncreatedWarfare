using System;
using Uncreated.Warfare.Commands.Permissions;

namespace Uncreated.Warfare.Commands.Dispatch;

/// <summary>
/// Metadata for a <see cref="IExecutableCommand"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    public string CommandName { get; }
    public string[] Aliases { get; }
    public string? PermissionOverride { get; set; }
    public CommandAttribute(string commandName, params string[] aliases)
    {
        CommandName = commandName;
        Aliases = aliases;
    }
}

/// <summary>
/// Define the name of the static method that returns help metadata about a command.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HelpMetadataAttribute : Attribute
{
    public string StructureGetter { get; }
    public HelpMetadataAttribute(string structureGetter)
    {
        StructureGetter = structureGetter;
    }
}

/// <summary>
/// Define another permission the caller must have.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AndNeedsPermissionAttribute : Attribute
{
    public PermissionLeaf Permission { get; }
    public AndNeedsPermissionAttribute(string permission)
    {
        Permission = permission.IndexOf("::", StringComparison.Ordinal) == -1
            ? new PermissionLeaf(permission, unturned: false, warfare: true)
            : PermissionLeaf.Parse(permission);
    }
}

/// <summary>
/// Define another permission the caller could have.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class OrNeedsPermissionAttribute : Attribute
{
    public PermissionLeaf Permission { get; }
    public OrNeedsPermissionAttribute(string permission)
    {
        Permission = permission.IndexOf("::", StringComparison.Ordinal) == -1
            ? new PermissionLeaf(permission, unturned: false, warfare: true)
            : PermissionLeaf.Parse(permission);
    }
}


/// <summary>
/// Indicates that only one execution of this command can run at once.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SynchronizedCommandAttribute : Attribute;

/// <summary>
/// Indicates that the command should be hidden from /help and command lists.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
sealed class HideFromHelpAttribute : Attribute;