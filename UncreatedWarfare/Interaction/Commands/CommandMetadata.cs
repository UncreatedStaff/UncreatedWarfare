using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction.Commands;

/// <summary>
/// Information about the layout of a command from a configuration file.
/// </summary>
public class CommandMetadata
{
    private CommandMetadata? _parent;

    /// <summary>
    /// Name of the parameter in proper-case format.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Single value that can be entered instead of <see cref="Name"/>.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Values that can be entered instead of <see cref="Name"/>.
    /// </summary>
    public string[] Aliases { get; set; }

    /// <summary>
    /// Value type, or 'Verbatim' if the parameter name itself should be entered.
    /// </summary>
    /// <remarks>Types can be fully qualified type names, namespace names in Uncreated.Warfare or mscorlib, language type keywords, or 'Verbatim'.</remarks>
    public string? Type { get; set; }

    /// <summary>
    /// Value type, or 'Verbatim' if the parameter name itself should be entered.
    /// </summary>
    /// <remarks>Types can be fully qualified type names, namespace names in Uncreated.Warfare or mscorlib, language type keywords, or 'Verbatim'.</remarks>
    public string?[]? Types { get; set; }

    /// <summary>
    /// Array of types after <see cref="Clean"/> is ran.
    /// </summary>
    /// <remarks>Verbatim parameter types will have the type of <see cref="VerbatimParameterType"/>.</remarks>
    public Type[] ResolvedTypes { get; internal set; }

    /// <summary>
    /// Translatable description of the command or sub-command.
    /// </summary>
    public TranslationList? Description { get; set; }

    /// <summary>
    /// List of sub-parameters.
    /// </summary>
    public CommandMetadata[] Parameters { get; set; }

    /// <summary>
    /// List of all valid flags.
    /// </summary>
    public FlagMetadata[] Flags { get; set; }

    /// <summary>
    /// If the parameter is optional. All other parameters on this level must also be marked optional.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// If the parameter spans the rest of the command arguments, including spaces.
    /// </summary>
    public bool Remainder { get; set; }

    /// <summary>
    /// The permission needed to execute the command.
    /// </summary>
    public PermissionLeaf Permission { get; set; }

    /// <summary>
    /// Number of sub-parameters to chain into one 'parameter' including this one. Requires that all child parameters up to that amount have only one parameter.
    /// </summary>
    /// <remarks>Example: <c>/tp [x y z|location|player]</c> where <c>x.ChainDisplayAmount = 3</c>.</remarks>
    public int ChainLength { get; set; }

    /// <summary>
    /// Recursively clean this metadata and all it's parameters.
    /// </summary>
    /// <param name="commandType">The class of the parent command.</param>
    public void Clean(Type commandType)
    {
        if (_parent == null && Name == null)
        {
            Name = commandType.TryGetAttributeSafe(out CommandAttribute command) ? command.CommandName : commandType.Name;
        }

        // aliases
        List<string> tempAliases = new List<string>(Aliases?.Length ?? 1);
        if (Aliases != null)
        {
            foreach (string? alias in Aliases)
            {
                if (alias == null || tempAliases.FindIndex(a => a.Equals(alias, StringComparison.InvariantCultureIgnoreCase)) >= 0)
                    continue;

                tempAliases.Add(alias);
            }
        }
        if (Alias != null && tempAliases.FindIndex(a => a.Equals(Alias, StringComparison.InvariantCultureIgnoreCase)) < 0)
        {
            tempAliases.Add(Alias);
        }

        Aliases = tempAliases.ToArray();
        Alias = Aliases.Length == 1 ? Aliases[0] : null;
        
        // types
        if (_parent == null)
        {
            Types = [ "Verbatim" ];
            Type = "Verbatim";
            ResolvedTypes = [ typeof(VerbatimParameterType) ];
        }
        else
        {
            List<Type> tempTypes = new List<Type>(Types?.Length ?? 1);

            Type? type;
            if (Types != null)
            {
                foreach (string? typeName in Types)
                {
                    if (!ContextualTypeResolver.TryResolveType(typeName, out type) || tempTypes.Contains(type))
                        continue;

                    tempTypes.Add(type);
                }
            }
            if (ContextualTypeResolver.TryResolveType(Type, out type) && !tempTypes.Contains(type))
            {
                tempTypes.Add(type);
            }

            ResolvedTypes = tempTypes.ToArray();
            string[] typeNames = new string[ResolvedTypes.Length];
            for (int i = 0; i < typeNames.Length; ++i)
            {
                typeNames[i] = ResolvedTypes[i].AssemblyQualifiedName!;
            }

            Types = typeNames;
            Type = typeNames.Length == 1 ? typeNames[0] : null;
        }

        int nonNull = 0;
        if (Flags is not { Length: > 0 })
        {
            Flags = Array.Empty<FlagMetadata>();
        }
        else
        {
            for (int i = 0; i < Flags.Length; ++i)
            {
                if (Flags[i]?.Name == null)
                    continue;

                ++nonNull;
            }

            if (nonNull < Flags.Length)
            {
                FlagMetadata[] newFlags = new FlagMetadata[nonNull];
                nonNull = -1;
                for (int i = 0; i < Flags.Length; ++i)
                {
                    FlagMetadata flag = Flags[i];
                    if (flag?.Name == null)
                        continue;

                    newFlags[++nonNull] = flag;
                }

                Flags = newFlags;
            }
        }

        // parameters
        if (Parameters == null)
            return;

        nonNull = 0;
        for (int i = 0; i < Parameters.Length; ++i)
        {
            CommandMetadata? meta = Parameters[i];
            if (meta?.Name == null)
                continue;

            ++nonNull;
            meta._parent = this;
        }

        if (nonNull < Parameters.Length)
        {
            CommandMetadata[] newParameters = new CommandMetadata[nonNull];
            nonNull = -1;
            for (int i = 0; i < Parameters.Length; ++i)
            {
                CommandMetadata? meta = Parameters[i];
                if (meta?.Name == null)
                    continue;

                newParameters[++nonNull] = meta;
            }

            Parameters = newParameters;
        }

        for (int i = 0; i < Parameters.Length; ++i)
        {
            Parameters[i].Clean(commandType);
        }
    }

    public class FlagMetadata
    {
        /// <summary>
        /// Flag name without the dash.
        /// </summary>
        /// <remarks>Example: -e would be "e".</remarks>
        public string Name { get; set; }

        /// <summary>
        /// General description of what the flag does.
        /// </summary>
        public TranslationList Description { get; set; }

        /// <summary>
        /// The permission needed to use the flag.
        /// </summary>
        public PermissionLeaf Permission { get; set; }
    }
}

/// <summary>
/// Type representing the 'verbatim' parameter type.
/// </summary>
public static class VerbatimParameterType;