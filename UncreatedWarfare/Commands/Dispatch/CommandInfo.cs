using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using SDG.Unturned;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Commands.Permissions;

namespace Uncreated.Warfare.Commands.Dispatch;
public class CommandType
{
    /// <summary>
    /// Type that contains the code for the command.
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// Name of the command.
    /// </summary>
    /// <remarks>Example: 'home' for /home.</remarks>
    public string CommandName { get; set; }

    /// <summary>
    /// Higher numbers will be executed over lower numbers.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Aliases for <see cref="CommandName"/>.
    /// </summary>
    public string[] Aliases { get; set; }

    /// <summary>
    /// Optional reference to the vanilla <see cref="Command"/>.
    /// </summary>
    public Command? VanillaCommand { get; set; }

    /// <summary>
    /// Information about how to display the command in /help.
    /// </summary>
    public CommandStructure? Structure { get; set; }

    /// <summary>
    /// If this command is hid from /help by a <see cref="HideFromHelpAttribute"/>.
    /// </summary>
    public bool HideFromHelp { get; set; }

    /// <summary>
    /// Commands marked as synchronized all use this semaphore to synchronize their execution.
    /// </summary>
    public SemaphoreSlim? SynchronizedSemaphore { get; set; }

    /// <summary>
    /// Default permission generated from the command name.
    /// </summary>
    /// <remarks><c>command.[command name]</c></remarks>
    public PermissionLeaf DefaultPermission { get; set; }

    /// <summary>
    /// A list of other permissions the caller must have or could have instead (depending on <see cref="OtherPermissionsAreAnd"/>.
    /// </summary>
    public PermissionLeaf[] OtherPermissions { get; set; }

    /// <summary>
    /// If <see cref="OtherPermissions"/> represents a list of permissions the user must have including <see cref="DefaultPermission"/>, instead of a list of alternative permissions the user could have.
    /// </summary>
    public bool OtherPermissionsAreAnd { get; set; }

    /// <summary>
    /// Create a new vanilla command.
    /// </summary>
    internal CommandType(Command vanillaCommand)
    {
        Type = vanillaCommand.GetType();
        VanillaCommand = vanillaCommand;
        CommandName = vanillaCommand.command;
        Aliases = Array.Empty<string>();
        Structure = new CommandStructure
        {
            Description = vanillaCommand.info
        };
        OtherPermissionsAreAnd = true;
        OtherPermissions = Array.Empty<PermissionLeaf>();
        DefaultPermission = new PermissionLeaf("commands." + CommandName, unturned: true, warfare: false);
    }

    /// <summary>
    /// Create a new custom command.
    /// </summary>
    /// <param name="classType">The type containing the command's code. Must implement <see cref="IExecutableCommand"/>.</param>
    internal CommandType(Type classType)
    {
        if (!typeof(IExecutableCommand).IsAssignableFrom(classType))
        {
            throw new ArgumentException("Must implement IExecutableCommand.", nameof(classType));
        }

        Type = classType;
        string? permission;
        if (classType.TryGetAttributeSafe(out CommandAttribute metadata))
        {
            CommandName = metadata.CommandName;
            Aliases = metadata.Aliases ?? Array.Empty<string>();
            permission = metadata.PermissionOverride;
        }
        else
        {
            CommandName = classType.Name;
            Aliases = Array.Empty<string>();
            permission = null;
            L.LogWarning($"Command {Accessor.Formatter.Format(classType)} is missing a CommandAttribute.");
        }

        permission ??= "commands." + CommandName;
        DefaultPermission = permission.IndexOf("::", StringComparison.Ordinal) != -1
            ? new PermissionLeaf(permission)
            : new PermissionLeaf(permission, unturned: false, warfare: true);

        Priority = classType.GetPriority();

        HideFromHelp = classType.IsDefinedSafe<HideFromHelpAttribute>();

        if (classType.TryGetAttributeSafe(out HelpMetadataAttribute helpMeta) && !string.IsNullOrEmpty(helpMeta.StructureGetter))
        {
            MethodInfo? method = classType.GetMethod(helpMeta.StructureGetter, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, CallingConventions.Any, Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(CommandStructure))
            {
                Structure = (CommandStructure)method.Invoke(null, Array.Empty<object>());
                Structure.Permission = DefaultPermission;
            }
            else
            {
                L.LogWarning($"Failed to find help metadata method: \"{Accessor.Formatter.Format(new MethodDefinition(helpMeta.StructureGetter)
                    .DeclaredIn(classType, isStatic: true)
                    .WithNoParameters()
                    .Returning<CommandStructure>())
                }\".");
            }
        }
        else if (!HideFromHelp)
        {
            L.LogWarning($"Command {Accessor.Formatter.Format(classType)} is missing a HelpMetadataAttribute.");
        }

        if (classType.IsDefinedSafe<SynchronizedCommandAttribute>())
        {
            SynchronizedSemaphore = new SemaphoreSlim(1, 1);
        }

        PermissionLeaf[] orPerms = classType.GetAttributesSafe<OrNeedsPermissionAttribute>()
            .Select(x => x.Permission)
            .Where(x => x.Valid)
            .Distinct()
            .ToArray();

        PermissionLeaf[] andPerms = orPerms.Length > 0
            ? Array.Empty<PermissionLeaf>()
            : classType.GetAttributesSafe<AndNeedsPermissionAttribute>()
                .Select(x => x.Permission)
                .Where(x => x.Valid)
                .Distinct()
                .ToArray();

        if (orPerms.Length > 0)
        {
            OtherPermissions = orPerms;
        }
        else if (andPerms.Length > 0)
        {
            OtherPermissionsAreAnd = true;
            OtherPermissions = andPerms;
        }
        else
        {
            OtherPermissionsAreAnd = true;
            OtherPermissions = Array.Empty<PermissionLeaf>();
        }
    }
}
