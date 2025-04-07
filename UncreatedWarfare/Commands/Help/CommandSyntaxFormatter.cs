using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Util;

// ReSharper disable once CheckNamespace
namespace Uncreated.Warfare.Interaction.Commands.Syntax;

public class CommandSyntaxFormatter : IDisposable
{
    private readonly string? _prefix;
    private readonly bool _leaveOpen;
    private readonly UserPermissionStore? _permissionStore;

    public const string Verbatim = "Verbatim";

    public ISyntaxWriter SyntaxWriter { get; }

    [ActivatorUtilitiesConstructor]
    public CommandSyntaxFormatter(ISyntaxWriter syntaxWriter, UserPermissionStore permissionStore) : this(syntaxWriter, permissionStore, "/", true) { }

    public CommandSyntaxFormatter(ISyntaxWriter syntaxWriter, UserPermissionStore? permissionStore, string? prefix, bool leaveOpen = false)
    {
        _prefix = prefix;
        _leaveOpen = leaveOpen;
        _permissionStore = permissionStore;

        SyntaxWriter = syntaxWriter;
    }

    /// <summary>
    /// Enriches a description string with tags like caller, target, param, etc.
    /// </summary>
    public string? GetRichDescription(ICommandParameterDescriptor parameter, ICommandFlagDescriptor? flag, ISyntaxWriter writer, LanguageInfo? language)
    {
        string? desc = flag?.Description?.Translate(language) ?? parameter.Description?.Translate(language);

        if (desc == null || !desc.Contains('<'))
            return desc;

        ReadOnlySpan<char> description = desc;

        StringBuilder sb = new StringBuilder(description.Length);
        writer.BeginWrite(sb, SyntaxStringType.RichDescription);
        int lastIndex = -1;
        while (true)
        {
            int nextIndex = description.IndexOf('<', lastIndex + 1);
            if (nextIndex == -1)
                break;

            int startLookingForEndTagIndex = nextIndex + 1;
            int endTagIndex = -1;
            int endTagSlashIndex = -1;

            // find tag end
            while (startLookingForEndTagIndex < description.Length)
            {
                endTagSlashIndex = description.IndexOf('/', startLookingForEndTagIndex);
                if (endTagSlashIndex == -1)
                    break;
                endTagIndex = description.IndexOf('>', endTagSlashIndex);
                if (endTagIndex == -1)
                    break;

                bool isValidEnd = endTagIndex == endTagSlashIndex + 1;
                if (!isValidEnd)
                {
                    // '/ >' is valid with any number of spaces
                    bool foundNonSpace = false;
                    for (int i = endTagSlashIndex + 1; i < endTagIndex; ++i)
                    {
                        if (char.IsWhiteSpace(description[i]))
                            continue;

                        foundNonSpace = true;
                        break;
                    }

                    if (foundNonSpace)
                    {
                        startLookingForEndTagIndex = endTagIndex + 1;
                        continue;
                    }
                }

                break;
            }

            if (endTagIndex == -1)
                break;

            // find start tag closest to end tag
            int laterStartIndex = nextIndex;
            do
            {
                nextIndex = laterStartIndex;
                laterStartIndex = description.IndexOf('<', nextIndex + 1);
            } while (laterStartIndex != -1 && laterStartIndex < endTagIndex);

            TagType type = TagType.Unknown;
            ReadOnlySpan<char> tagData = default;
            ReadOnlySpan<char> tag = description.Slice(nextIndex + 1, endTagSlashIndex - nextIndex - 1).Trim();
            int space = tag.IndexOf(' ');
            ReadOnlySpan<char> tagName = space != -1 ? tag[..space] : tag;
            bool isSentenceStarter = tagName.Length > 0 && char.IsUpper(tagName[0]);
            if (tagName.Equals("param", StringComparison.OrdinalIgnoreCase))
            {
                type = TagType.ParameterName;
                tagData = FindTagData(tag, tagName.Length);
            }
            else if (tagName.Equals("flag", StringComparison.OrdinalIgnoreCase))
            {
                type = TagType.FlagName;
                tagData = FindTagData(tag, tagName.Length);
                if (tagData.Length > 0 && tagData[0] == '-')
                    tagData = tagData[1..];
            }
            else if (tagName.Equals("caller", StringComparison.OrdinalIgnoreCase))
            {
                type = TagType.Caller;
            }
            else if (tagName.Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                type = TagType.Target;
            }

            if (type == TagType.Unknown || type == TagType.ParameterName && tagData.Length == 0)
                continue;

            sb.Append(description.Slice(lastIndex + 1, nextIndex - lastIndex - 1));
            if (type == TagType.ParameterName)
            {
                ICommandParameterDescriptor? paramMatch = FindParameter(tagData, parameter);
                if (paramMatch == null)
                    writer.WriteBasicTag(sb, type, tagData);
                else
                    writer.WriteParameter(sb, paramMatch, null, false, false);
            }
            else if (type == TagType.FlagName)
            {
                ICommandFlagDescriptor? flagMatch = FindFlag(tagData, parameter);
                if (flagMatch == null)
                    writer.WriteBasicTag(sb, type, tagData);
                else
                    writer.WriteFlag(sb, flagMatch);
            }
            else
            {
                writer.WriteBasicTag(sb, type, GetTagName(type, isSentenceStarter));
            }
            lastIndex = endTagIndex;
        }

        if (lastIndex != description.Length - 1)
            sb.Append(description.Slice(lastIndex + 1, description.Length - lastIndex - 1));

        return writer.EndWrite(sb, SyntaxStringType.RichDescription);

        static ReadOnlySpan<char> FindTagData(ReadOnlySpan<char> tag, int tagNameLen)
        {
            if (tagNameLen >= tag.Length)
                return default;

            tag = tag[tagNameLen..];

            int quoteIndex1 = tag.IndexOf('\'');
            if (quoteIndex1 == -1 || quoteIndex1 == tag.Length - 1)
                return default;

            int quoteIndex2 = tag.IndexOf('\'', quoteIndex1 + 1);
            if (quoteIndex2 == -1 || quoteIndex2 == quoteIndex1 + 1)
                return default;

            return tag.Slice(quoteIndex1 + 1, quoteIndex2 - quoteIndex1 - 1);
        }
    }

    /// <summary>
    /// Creates a syntax string that describes the requires arguments from the meta file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public async ValueTask<SyntaxStringInfo> GetSyntaxString(ICommandDescriptor command, IReadOnlyList<string> args, string? flag,
        ICommandUser? user, string? aliasOverride = null, CancellationToken token = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (flag is { Length: > 0 } && flag[0] == '-')
            flag = flag[1..];

        if (string.IsNullOrWhiteSpace(flag))
            flag = null;

        ICommandParameterDescriptor meta = command.Metadata;
        ISyntaxWriter writer = SyntaxWriter;
        StringBuilder bldr = new StringBuilder();

        writer.BeginWrite(bldr, SyntaxStringType.SyntaxString);

        // write [/]command
        if (!string.IsNullOrEmpty(_prefix))
        {
            writer.WritePrefixCharacter(bldr, _prefix);
        }

        string commandName = string.IsNullOrEmpty(aliasOverride) ? command.CommandName : aliasOverride;
        writer.WriteCommandName(bldr, commandName, !string.Equals(commandName, command.CommandName));

        // no perms for any parameters or the command itself
        ICommandFlagDescriptor? flagDesc;
        if (meta == null || !await HasPermissionAsync(user, meta.Permission, token))
        {
            flagDesc = meta?.Flags?.FirstOrDefault(x => x.Name.Equals(flag, StringComparison.Ordinal));
            return new SyntaxStringInfo(
                writer.EndWrite(bldr, SyntaxStringType.SyntaxString),
                meta ?? new CommandMetadata
                {
                    Name = command.CommandName, Aliases = command.Aliases.ToArray(), Permission = command.DefaultPermission
                },
                flagDesc
            );
        }

        List<ICommandParameterDescriptor> permissiveParams = new List<ICommandParameterDescriptor>();

        ICommandParameterDescriptor argMeta = meta;
        // resolve subcommand/parameter name
        int nextChain = 0;
        for (int i = -1; i < args.Count; ++i)
        {
            string? arg = i < 0 ? null : args[i];

            // try to match a verbatim parameter.
            // Ex. "/help tp jump" will just print jump instead of showing it as an option, then show the parameters for jump
            ICommandParameterDescriptor matchedMeta = meta;

            if (arg != null && TryMatchParameter(ref matchedMeta, arg, (user as WarfarePlayer)?.Locale.CultureInfo ?? CultureInfo.InvariantCulture) && await HasPermissionAsync(user, matchedMeta.Permission, token))
            {
                meta = matchedMeta;
                if (argMeta.Description != null)
                    argMeta = meta;
                writer.WriteParameterSeparator(bldr);
                writer.WriteParameter(bldr, meta, arg, false, false);
                if (meta.Parameters.Count == 1 && meta.Chain > 0)
                    nextChain += meta.Chain;
                nextChain = Math.Max(nextChain - 1, 0);
                if (i != args.Count - 1)
                    continue;
            }

            if (arg == null && i < args.Count - 1)
                continue;

            if (meta.Parameters.Count == 0)
                break;

            bool hasSameNextParameters;

            do
            {
                // collect a list of parameters which the user has permission to use
                permissiveParams.Clear();
                if (meta.Parameters != null)
                {
                    for (int p = 0; p < meta.Parameters.Count; ++p)
                    {
                        ICommandParameterDescriptor parameter = meta.Parameters[p];
                        if (await HasPermissionAsync(user, parameter.Permission, token))
                            permissiveParams.Add(parameter);
                    }
                }

                if (permissiveParams.Count == 0)
                    break;

                bool allParametersAreOptional = permissiveParams.All(x => x.Optional);
                IReadOnlyList<ICommandParameterDescriptor>? pendingNextParameter = null;
                hasSameNextParameters = permissiveParams.Count > 1;

                writer.WriteParameterSeparator(bldr);
                writer.WriteOpenParameterTemplate(bldr, optional: allParametersAreOptional);

                for (int p = 0; p < permissiveParams.Count; p++)
                {
                    if (p != 0)
                        writer.WriteParameterTemplateSeparator(bldr);

                    ICommandParameterDescriptor parameter = permissiveParams[p];

                    bool useOptionalBrackets = !allParametersAreOptional && parameter.Optional;
                    if (useOptionalBrackets)
                    {
                        writer.WriteOpenParameterTemplate(bldr, true);
                    }

                    WriteParameterName(bldr, writer, parameter);

                    if (parameter.Remainder)
                        writer.WriteParameterTemplateRemainder(bldr);

                    int chainLength = parameter.Chain + nextChain;
                    while (chainLength > 1 && parameter.Parameters.Count == 1)
                    {
                        --chainLength;
                        parameter = parameter.Parameters[0];
                        writer.WriteParameterTemplateChainSeparator(bldr);
                        WriteParameterName(bldr, writer, parameter);
                        if (parameter.Remainder)
                            writer.WriteParameterTemplateRemainder(bldr);
                    }

                    if (useOptionalBrackets)
                    {
                        writer.WriteCloseParameterTemplate(bldr, true);
                    }

                    // check to see if all parameters have the same following parameters so they can be displayed after
                    // ex. kit hotkey <add|remove> <slot> where both add and remove have equal slots
                    if (hasSameNextParameters)
                    {
                        if (parameter.Chain > 0)
                            hasSameNextParameters = false;

                        if (pendingNextParameter == null)
                        {
                            if (parameter.Parameters.Count > 0)
                                pendingNextParameter = parameter.Parameters;
                            else
                                hasSameNextParameters = false;
                        }
                        else if (parameter.Parameters.Count != 1 || !CompareParameters(parameter.Parameters, pendingNextParameter))
                        {
                            hasSameNextParameters = false;
                        }
                    }
                }

                writer.WriteCloseParameterTemplate(bldr, optional: allParametersAreOptional);

                meta = permissiveParams[0];
            } while (hasSameNextParameters || permissiveParams.Count == 1);

            break;
        }

        flagDesc = null;
        if (flag != null)
        {
            for (ICommandParameterDescriptor? p = meta; p != null && flagDesc == null; p = p.Parent)
            {
                flagDesc = p.Flags?.FirstOrDefault(x => x.Name.Equals(flag, StringComparison.Ordinal));
            }
        }

        if (flagDesc != null)
            writer.WriteFlag(bldr, flagDesc);

        return new SyntaxStringInfo(writer.EndWrite(bldr, SyntaxStringType.SyntaxString), argMeta, flagDesc);
    }

    private static ICommandParameterDescriptor? FindParameter(ReadOnlySpan<char> parameterName, ICommandParameterDescriptor refParameter)
    {
        for (ICommandParameterDescriptor? parameter = refParameter; parameter.Parent != null; parameter = parameter.Parent)
        {
            if (parameterName.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }

        return FindParameterInChildren(parameterName, refParameter);

        static ICommandParameterDescriptor? FindParameterInChildren(ReadOnlySpan<char> parameterName, ICommandParameterDescriptor refParameter)
        {
            foreach (ICommandParameterDescriptor parameter in refParameter.Parameters)
            {
                if (parameterName.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase))
                    return parameter;
            }

            foreach (ICommandParameterDescriptor parameter in refParameter.Parameters)
            {
                ICommandParameterDescriptor? p = FindParameterInChildren(parameterName, parameter);
                if (p != null)
                    return p;
            }

            return null;
        }
    }

    private static ICommandFlagDescriptor? FindFlag(ReadOnlySpan<char> flagName, ICommandParameterDescriptor refParameter)
    {
        for (ICommandParameterDescriptor? parameter = refParameter; parameter != null; parameter = parameter.Parent)
        {
            foreach (ICommandFlagDescriptor flag in parameter.Flags)
            {
                if (flagName.Equals(flag.Name, StringComparison.OrdinalIgnoreCase))
                    return flag;
            }
        }

        return FindFlagInChildren(flagName, refParameter);

        static ICommandFlagDescriptor? FindFlagInChildren(ReadOnlySpan<char> flagName, ICommandParameterDescriptor refParameter)
        {
            foreach (ICommandParameterDescriptor parameter in refParameter.Parameters)
            {
                foreach (ICommandFlagDescriptor flag in parameter.Flags)
                {
                    if (flagName.Equals(flag.Name, StringComparison.OrdinalIgnoreCase))
                        return flag;
                }
            }

            foreach (ICommandParameterDescriptor parameter in refParameter.Parameters)
            {
                ICommandFlagDescriptor? f = FindFlagInChildren(flagName, parameter);
                if (f != null)
                    return f;
            }

            return null;
        }
    }

    private static string GetTagName(TagType type, bool isSentenceStarter)
    {
        return type switch
        {
            TagType.Caller => isSentenceStarter ? "Caller" : "caller",
            TagType.Target => isSentenceStarter ? "Target" : "target",
            TagType.ParameterName => isSentenceStarter ? "Parameter" : "parameter",
            TagType.FlagName => isSentenceStarter ? "Flag" : "flag",
            _ => "?"
        };
    }

    private static void WriteParameterName(StringBuilder bldr, ISyntaxWriter writer, ICommandParameterDescriptor parameter)
    {
        int ct = parameter.Types.Count(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(LookAtInteractionParameterType<>));
        writer.WriteParameter(bldr, parameter, null, ct > 0 && parameter.Types.Count == ct, ct > 0);
    }

    private static bool CompareParameters(IReadOnlyList<ICommandParameterDescriptor> l1, IReadOnlyList<ICommandParameterDescriptor> l2)
    {
        if (l1.Count != l2.Count)
            return false;

        for (int i = 0; i < l1.Count; ++i)
        {
            ICommandParameterDescriptor p1 = l1[i], p2 = l2[i];
            if (!(p1.Name.Equals(p2.Name, StringComparison.OrdinalIgnoreCase)
                   && p1.Optional == p2.Optional
                   && p1.Remainder == p2.Remainder
                   && p1.Chain == 0
                   && p2.Chain == 0
                   && p1.Types.SequenceEqual(p2.Types)
                   && p1.Permission == p2.Permission))
            {
                return false;
            }

            if (!CompareParameters(p1.Parameters, p2.Parameters))
                return false;
        }

        return true;
    }

    private async ValueTask<bool> HasPermissionAsync(ICommandUser? user, PermissionLeaf perm, CancellationToken token = default)
    {
        if (_permissionStore == null || user == null || !perm.Valid)
            return true;

        return await _permissionStore.HasPermissionAsync(user, perm, token);
    }

    private static bool TryMatchParameter(ref ICommandParameterDescriptor meta, string arg, CultureInfo culture)
    {
        bool found = false;
        int p = 0;

        for (; p < meta.Parameters.Count; ++p)
        {
            if (!string.Equals(meta.Parameters[p].Name, arg, StringComparison.InvariantCultureIgnoreCase))
                continue;

            found = true;
            break;
        }

        if (!found)
        {
            for (p = 0; p < meta.Parameters.Count; ++p)
            {
                ICommandParameterDescriptor parameter = meta.Parameters[p];
                for (int a = 0; a < parameter.Aliases.Count; ++a)
                {
                    if (!string.Equals(parameter.Aliases[a], arg, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    found = true;
                    break;
                }

                if (found)
                    break;
            }
        }

        if (!found)
        {
            int nonVerbatimCt = 0;
            int nonVerbatimIndex = -1;
            for (p = 0; p < meta.Parameters.Count; ++p)
            {
                ICommandParameterDescriptor parameter = meta.Parameters[p];
                if (parameter.Types.Contains(typeof(VerbatimParameterType)))
                    continue;

                ++nonVerbatimCt;
                nonVerbatimIndex = p;

                foreach (Type type in parameter.Types)
                {
                    if (CheckParameterValue(arg, type, culture))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            if (!found && nonVerbatimCt == 1)
            {
                p = nonVerbatimIndex;
                found = true;
            }
        }

        if (found)
            meta = meta.Parameters[p];
        return found;
    }

    private static bool CheckParameterValue(string arg, Type type, CultureInfo culture)
    {
        if (typeof(Asset).IsAssignableFrom(type))
        {
            return Guid.TryParse(arg, out _) || ushort.TryParse(arg, NumberStyles.Number, culture, out _);
        }

        if (FormattingUtility.TryParseAny(arg, culture, type, out _))
        {
            return true;
        }

        if (type == typeof(IPlayer))
        {
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (!_leaveOpen && SyntaxWriter is IDisposable disp)
            disp.Dispose();
    }

    public struct SyntaxStringInfo
    {
        public string Syntax { get; }
        public ICommandParameterDescriptor TargetParameter { get; }
        public ICommandFlagDescriptor? TargetFlag { get; }

        public SyntaxStringInfo(string syntax, ICommandParameterDescriptor targetParameter, ICommandFlagDescriptor? targetFlag)
        {
            Syntax = syntax;
            TargetParameter = targetParameter;
            TargetFlag = targetFlag;
        }
    }

    public enum TagType
    {
        Unknown,
        Caller,
        Target,
        ParameterName,
        FlagName
    }
}