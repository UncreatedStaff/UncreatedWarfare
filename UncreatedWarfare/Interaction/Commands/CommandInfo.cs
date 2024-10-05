using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;
public class CommandInfo
{
    private static readonly ServiceContainer EmptyServiceProvider = new ServiceContainer();

    internal List<CommandWaitTask> WaitTasks = new List<CommandWaitTask>(0);
    private readonly List<CommandInfo> _subCommands;

    /// <summary>
    /// Type that contains the code for the command.
    /// </summary>
    [JsonIgnore]
    public Type Type { get; }

    /// <summary>
    /// If the command can be executed. Some parent commands may not be executable.
    /// </summary>
    public bool IsExecutable { get; internal set; }

    /// <summary>
    /// If this command has a parent command.
    /// </summary>
    public bool IsSubCommand => ParentCommand != null;

    /// <summary>
    /// Name of the command.
    /// </summary>
    /// <remarks>Example: 'home' for /home.</remarks>
    public string CommandName { get; }

    /// <summary>
    /// Higher numbers will be executed over lower numbers.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Aliases for <see cref="CommandName"/>.
    /// </summary>
    public string[] Aliases { get; }

    /// <summary>
    /// Optional reference to the vanilla <see cref="Command"/>.
    /// </summary>
    [JsonIgnore]
    public Command? VanillaCommand { get; }

    /// <summary>
    /// Information about how to display the command in /help.
    /// </summary>
    [JsonIgnore]
    public CommandMetadata Metadata { get; }

    /// <summary>
    /// If this command is hid from /help by a <see cref="HideFromHelpAttribute"/>.
    /// </summary>
    public bool HideFromHelp { get; }

    /// <summary>
    /// Typing '/command help' will not switch to /help if this is <see langword="true"/>, which can be set using the <see cref="DisableAutoHelpAttribute"/>.
    /// </summary>
    /// <remarks>This is not propagated to sub-commands.</remarks>
    public bool AutoHelpDisabled { get; }

    /// <summary>
    /// Commands marked as synchronized with the <see cref="SynchronizedCommandAttribute"/> all use this semaphore to synchronize their execution.
    /// </summary>
    /// <remarks>Sub-commands share this with their parents but not with their siblings unless the parent defines it.</remarks>
    [JsonIgnore]
    public SemaphoreSlim? SynchronizedSemaphore { get; }

    /// <summary>
    /// Default permission generated from the command name. Can be overridden with <see cref="CommandAttribute.PermissionOverride"/>.
    /// </summary>
    /// <remarks><c>command.[command name]</c></remarks>
    public PermissionLeaf DefaultPermission { get; }

    /// <summary>
    /// A list of other permissions the caller must have or could have instead (depending on <see cref="OtherPermissionsAreAnd"/>.
    /// </summary>
    public PermissionLeaf[] OtherPermissions { get; }

    /// <summary>
    /// If <see cref="OtherPermissions"/> represents a list of permissions the user must have including <see cref="DefaultPermission"/>, instead of a list of alternative permissions the user could have.
    /// </summary>
    public bool OtherPermissionsAreAnd { get; }

    /// <summary>
    /// List of all sub-commands for a command. These commands may also have sub-commands.
    /// </summary>
    public IReadOnlyList<CommandInfo> SubCommands { get; private set; }

    /// <summary>
    /// If this command is a sub-command, this is the info for the parent command.
    /// </summary>
    [JsonIgnore]
    public CommandInfo? ParentCommand { get; }

    /// <summary>
    /// The command that will automatically take over if this command is called. It should usually be a child command.
    /// </summary>
    public CommandInfo? RedirectCommandInfo { get; set; }

    /// <summary>
    /// This command's position in it's family tree. 0 = parent command, 1 = child, 2 = child's child, etc.
    /// </summary>
    public int HierarchyLevel { get; }

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
    /// <param name="classType">The type containing the command's code. Must implement <see cref="ICommand"/>.</param>
    internal CommandInfo(Type classType, ILogger logger, CommandInfo? parent)
    {
        if (!typeof(ICommand).IsAssignableFrom(classType))
        {
            throw new ArgumentException("Must implement ICommand.", nameof(classType));
        }

        IsExecutable = typeof(IExecutableCommand).IsAssignableFrom(classType);

        Type = classType;

        _subCommands = new List<CommandInfo>();
        SubCommands = new ReadOnlyCollection<CommandInfo>(_subCommands);
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

            parent._subCommands.Add(this);

            permission ??= parent.DefaultPermission.Path + "." + CommandName;
            logger.LogDebug("Found parent {0} for command {1}, added to subcommands. Now equal to {2}.", parent.CommandName, CommandName, string.Join(',', parent.SubCommands.Select(x => x.CommandName)));
        }
        else
        {
            permission ??= "commands." + CommandName;
        }

        for (CommandInfo? p = ParentCommand; p != null; p = p.ParentCommand)
        {
            ++HierarchyLevel;
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

            resource = typeof(WarfareModule).Namespace + "." + relativePath.Replace(Path.DirectorySeparatorChar, '.').Replace(".cs", ".meta.yml");

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

            CommandMetadata meta = ReflectionUtility.CreateInstanceFixed<CommandMetadata>(EmptyServiceProvider, [ config ]);
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
