using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Commands.Permissions;

namespace Uncreated.Warfare.Commands.Dispatch;

/// <summary>
/// Metadata for a <see cref="ICommand"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(ICommand))]
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
/// Defines the parent type of the command, as well as marks this command as a sub-command.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(ICommand))]
public sealed class SubCommandOfAttribute : Attribute
{
    public Type ParentType { get; }
    public SubCommandOfAttribute(Type parentType)
    {
        ParentType = parentType;
    }
}

/// <summary>
/// Define the name of the root project folder for this assembly, mainly if it's not equal to the assembly name or the assembly name without periods.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class MetadataFileRootAttribute : Attribute
{
    /// <summary>
    /// The name of the root folder of the project.
    /// </summary>
    /// <remarks>This is just a name, not a full path.</remarks>
    public string FolderName { get; }
    public MetadataFileRootAttribute(string folderName)
    {
        FolderName = folderName;
    }
}

/// <summary>
/// Define the name of the static method that returns help metadata about a command.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(ICommand))]
public sealed class MetadataFileAttribute : Attribute
{
    /// <summary>
    /// Override the raw value used to retreive the embedded resource.
    /// </summary>
    /// <remarks>Example: "EmbeddedResource.Commands.BlankCommand.meta.yml"</remarks>
    public string? ResourcePathOverride { get; set; }

    /// <summary>
    /// Automatically retreived file name of the command. Should be in a folder with the same name as the assembly name.
    /// </summary>
    public string? FileName { get; }
    public MetadataFileAttribute([CallerFilePath] string? fileName = null)
    {
        FileName = fileName;
    }
}

/// <summary>
/// Define another permission the caller must have.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof(ICommand))]
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
[BaseTypeRequired(typeof(ICommand))]
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
[BaseTypeRequired(typeof(ICommand))]
public sealed class SynchronizedCommandAttribute : Attribute;

/// <summary>
/// Indicates that the command should be hidden from /help and command lists.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(ICommand))]
sealed class HideFromHelpAttribute : Attribute;

/// <summary>
/// Indicates that typing '/command help' should run the command normally instead of switching to /help.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(ICommand))]
sealed class DisableAutoHelpAttribute : Attribute;