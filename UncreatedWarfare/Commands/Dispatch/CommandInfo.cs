using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Commands.Permissions;

namespace Uncreated.Warfare.Commands.Dispatch;
public class CommandInfo
{
    private static readonly ServiceContainer EmptyServiceProvider = new ServiceContainer();

    /// <summary>
    /// Type that contains the code for the command.
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// If the command can be executed. Some parent commands may not be executable.
    /// </summary>
    public bool IsExecutable { get; set; }

    /// <summary>
    /// If this command has a parent command.
    /// </summary>
    public bool IsSubCommand => ParentCommand != null;

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
    public CommandMetadata Metadata { get; set; }

    /// <summary>
    /// If this command is hid from /help by a <see cref="HideFromHelpAttribute"/>.
    /// </summary>
    public bool HideFromHelp { get; set; }

    /// <summary>
    /// Typing '/command help' will not switch to /help if this is <see langword="true"/>, which can be set using the <see cref="DisableAutoHelpAttribute"/>.
    /// </summary>
    /// <remarks>This is not propagated to sub-commands.</remarks>
    public bool AutoHelpDisabled { get; set; }

    /// <summary>
    /// Commands marked as synchronized with the <see cref="SynchronizedCommandAttribute"/> all use this semaphore to synchronize their execution.
    /// </summary>
    /// <remarks>Sub-commands share this with their parents but not with their siblings unless the parent defines it.</remarks>
    public SemaphoreSlim? SynchronizedSemaphore { get; set; }

    /// <summary>
    /// Default permission generated from the command name. Can be overridden with <see cref="CommandAttribute.PermissionOverride"/>.
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
    /// List of all sub-commands for a command. These commands may also have sub-commands.
    /// </summary>
    public CommandInfo[] SubCommands { get; set; }

    /// <summary>
    /// If this command is a sub-command, this is the info for the parent command.
    /// </summary>
    public CommandInfo? ParentCommand { get; set; }

    /// <summary>
    /// Create a new vanilla command.
    /// </summary>
    internal CommandInfo(Command vanillaCommand)
    {
        Type = vanillaCommand.GetType();
        VanillaCommand = vanillaCommand;
        CommandName = vanillaCommand.command;
        Aliases = Array.Empty<string>();
        Metadata = DefaultMetadata(vanillaCommand.command, vanillaCommand.info);
        OtherPermissionsAreAnd = true;
        OtherPermissions = Array.Empty<PermissionLeaf>();
        DefaultPermission = new PermissionLeaf("commands." + CommandName, unturned: true, warfare: false);
        SubCommands = Array.Empty<CommandInfo>();
        IsExecutable = true;
    }

    /// <summary>
    /// Create a new custom command.
    /// </summary>
    /// <param name="classType">The type containing the command's code. Must implement <see cref="IExecutableCommand"/>.</param>
    internal CommandInfo(Type classType, ILogger logger, CommandInfo? parent)
    {
        if (!typeof(IExecutableCommand).IsAssignableFrom(classType))
        {
            throw new ArgumentException("Must implement IExecutableCommand.", nameof(classType));
        }

        Type = classType;
        SubCommands = Array.Empty<CommandInfo>();
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
            logger.LogWarning("Command {0} is missing a CommandAttribute.", Accessor.Formatter.Format(classType));
        }

        if (parent != null)
        {
            ParentCommand = parent;

            CommandInfo[] parentSubs = parent.SubCommands;
            Array.Resize(ref parentSubs, parentSubs.Length + 1);
            parentSubs[^1] = this;
            parent.SubCommands = parentSubs;

            permission ??= parent.DefaultPermission.Path + "." + CommandName;
        }
        else
        {
            permission ??= "commands." + CommandName;
        }

        DefaultPermission = permission.IndexOf("::", StringComparison.Ordinal) != -1
            ? new PermissionLeaf(permission)
            : new PermissionLeaf(permission, unturned: false, warfare: true);

        Priority = classType.GetPriority();

        AutoHelpDisabled = classType.IsDefinedSafe<DisableAutoHelpAttribute>();
        HideFromHelp = parent is { HideFromHelp: true } || classType.IsDefinedSafe<HideFromHelpAttribute>();

        if (!HideFromHelp)
        {
            Metadata = ReadHelpMetadata(classType, logger, parent == null)!;

            // add metadata to parent's parameters
            if (Metadata != null && parent != null)
            {
                bool foundAny = false;
                for (int i = 0; i < parent.Metadata.Parameters.Length; ++i)
                {
                    CommandMetadata paramMeta = parent.Metadata.Parameters[i];
                    if (!paramMeta.Name.Equals(CommandName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    foundAny = true;
                    break;
                }
                
                if (!foundAny)
                {
                    CommandMetadata[] parentMeta = parent.Metadata.Parameters;
                    Array.Resize(ref parentMeta, parentMeta.Length + 1);
                    parentMeta[^1] = Metadata;
                    parent.Metadata.Parameters = parentMeta;
                }
            }

            // get metadata from parent's parameters
            if (Metadata == null && parent?.Metadata != null)
            {
                for (int i = 0; i < parent.Metadata.Parameters.Length; ++i)
                {
                    CommandMetadata paramMeta = parent.Metadata.Parameters[i];
                    if (!paramMeta.Name.Equals(CommandName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    Metadata = paramMeta;
                    break;
                }
            }

            if (Metadata == null)
            {
                logger.LogWarning("Missing help metadata for command type {0}.", Accessor.Formatter.Format(classType));
            }
        }

        if (Metadata == null)
        {
            Metadata = DefaultMetadata(CommandName, null);
        }
        else if (Metadata.Name == null)
        {
            Metadata.Name = CommandName;
        }

        if (parent?.SynchronizedSemaphore != null || classType.IsDefinedSafe<SynchronizedCommandAttribute>())
        {
            SynchronizedSemaphore = parent?.SynchronizedSemaphore ?? new SemaphoreSlim(1, 1);
        }

        PermissionLeaf[] orPerms, andPerms;
        if (parent == null || parent.OtherPermissionsAreAnd)
        {
            orPerms = classType.GetAttributesSafe<OrNeedsPermissionAttribute>()
                .Select(x => x.Permission)
                .Where(x => x.Valid)
                .Distinct()
                .ToArray();
        }
        else
        {
            orPerms = classType.GetAttributesSafe<OrNeedsPermissionAttribute>()
                .Select(x => x.Permission)
                .Where(x => x.Valid)
                .Concat(parent.OtherPermissions)
                .Distinct()
                .ToArray();
        }
        if (parent == null || !parent.OtherPermissionsAreAnd)
        {
            andPerms = orPerms.Length > 0
                ? Array.Empty<PermissionLeaf>()
                : classType.GetAttributesSafe<AndNeedsPermissionAttribute>()
                    .Select(x => x.Permission)
                    .Where(x => x.Valid)
                    .Distinct()
                    .ToArray();
        }
        else
        {
            andPerms = orPerms.Length > 0
                ? Array.Empty<PermissionLeaf>()
                : classType.GetAttributesSafe<AndNeedsPermissionAttribute>()
                    .Select(x => x.Permission)
                    .Where(x => x.Valid)
                    .Concat(parent.OtherPermissions)
                    .Distinct()
                    .ToArray();
        }
        

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

        IsExecutable = typeof(IExecutableCommand).IsAssignableFrom(classType);
        if (!IsExecutable && SubCommands.Length == 0)
        {
            logger.LogWarning("Command type {0} isn't executable and has no sub-commands, which is practically useless.", Accessor.Formatter.Format(classType));
        }
    }

    private static CommandMetadata DefaultMetadata(string name, string? desc)
    {
        return new CommandMetadata
        {
            Name = name,
            Description = desc == null ? null : new TranslationList(desc),
            Parameters =
            [
                new CommandMetadata
                {
                    Name = "Args",
                    Remainder = true,
                    Optional = true,
                    ResolvedTypes = [ typeof(object) ],
                    Type = typeof(object).AssemblyQualifiedName,
                    Types = [ typeof(object).AssemblyQualifiedName ]
                }
            ]
        };
    }

    /// <summary>
    /// Reads help metadata as an embedded resource in the assembly the command is defined in.
    /// </summary>
    private static CommandMetadata? ReadHelpMetadata(Type classType, ILogger logger, bool logNoAttrAsError)
    {
        if (!classType.TryGetAttributeSafe(out MetadataFileAttribute fileAttr))
        {
            if (logNoAttrAsError)
            {
                logger.LogWarning("Command type {0} is missing a MetadataFileAttribute.", Accessor.Formatter.Format(classType));
            }
            else
            {
                logger.LogDebug("Command type {0} is missing a MetadataFileAttribute.", Accessor.Formatter.Format(classType));
            }

            return null;
        }

        Assembly asm = classType.Assembly;

        Stream? stream;
        string? resource;
        if (!string.IsNullOrWhiteSpace(fileAttr.ResourcePathOverride))
        {
            resource = fileAttr.ResourcePathOverride;
            stream = asm.GetManifestResourceStream(resource);

            if (stream == null)
            {
                logger.LogWarning("No embedded resource available with the specified ID: \"{0}\" for command type {1}.", fileAttr.ResourcePathOverride, Accessor.Formatter.Format(classType));
                return null;
            }
        }
        else if (string.IsNullOrWhiteSpace(fileAttr.FileName))
        {
            logger.LogWarning("Command type {0} is missing a file name in MetadataFileAttribute.", Accessor.Formatter.Format(classType));
            return null;
        }
        else
        {
            MetadataFileRootAttribute? root = asm.GetAttributeSafe<MetadataFileRootAttribute>();

            string path = fileAttr.FileName;
            string asmName = root?.FolderName ?? asm.GetName().Name;
            string asmNameWithNoDots = asmName.Replace(".", string.Empty);

            int sectionLength = asmName.Length;
            int index = path.LastIndexOf(asmName, StringComparison.Ordinal);
            if (index == -1)
            {
                index = path.LastIndexOf(asmNameWithNoDots, StringComparison.Ordinal);
                sectionLength = asmNameWithNoDots.Length;
            }

            if (index == -1 || index + sectionLength + 1 >= path.Length)
            {
                logger.LogWarning("Unable to identify relative path of command file: \"{0}\" for command type {1}. Expected \"{2}\" or \"{3}\" folder.", path, Accessor.Formatter.Format(classType), asmName, asmNameWithNoDots);
                return null;
            }

            string relativePath = path.Substring(index + sectionLength + 1);

            resource = "EmbeddedResource." + relativePath.Replace(Path.DirectorySeparatorChar, '.').Replace(".cs", ".meta.yml");

            stream = asm.GetManifestResourceStream(resource);

            if (stream == null)
            {
                logger.LogWarning("No embedded resource available with the ID: \"{0}\" for command type {1}.", resource, Accessor.Formatter.Format(classType));
                return null;
            }
        }

        IConfiguration? config = null;
        try
        {
            config = new ConfigurationBuilder()
                .AddYamlStream(stream)
                .Build();

            CommandMetadata meta = ActivatorUtilities.CreateInstance<CommandMetadata>(EmptyServiceProvider, [ config ]);
            config.Bind(meta);
            meta.Clean(classType);
            logger.LogDebug("Read command metadata for command type {0} from resource \"{1}\" in assembly {2}.", Accessor.Formatter.Format(classType), resource, asm.FullName);
            return meta;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read command metadata for command type {0} from resource \"{1}\" in assembly {2}.", Accessor.Formatter.Format(classType), resource, asm.FullName);
            return null;
        }
        finally
        {
            stream.Dispose();
            if (config is IDisposable disp)
                disp.Dispose();
        }
    }
}
