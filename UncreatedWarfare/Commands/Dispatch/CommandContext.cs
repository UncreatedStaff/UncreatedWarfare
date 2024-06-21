using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Util;
using UnityEngine;

namespace Uncreated.Warfare.Commands.Dispatch;

/// <summary>
/// Command interaction helper inheriting <see cref="ControlException"/>, allowing an action to be taken and this to be thrown in the same line.
/// </summary>
public class CommandContext : ControlException
{
    public const string Default = "-";
    private readonly UserPermissionStore _permissionsStore;

    /// <summary>
    /// Useful for sub-commands, offsets any parsing methods.
    /// </summary>
    /// <remarks>Increment this to skip one argument, for example.</remarks>
    public int ArgumentOffset { get; set; }

#nullable disable
    /// <summary>
    /// Player that called the command. Will be <see langword="null"/> if the command was called from console or some other source.
    /// </summary>
    public UCPlayer Player { get; }
#nullable restore

    /// <summary>
    /// User that called the command. Will never be <see langword="null"/>.
    /// </summary>
    public ICommandUser Caller { get; }

    /// <summary>
    /// Whether or not the console was the caller of the command.
    /// </summary>
    public bool IsConsole { get; }

    /// <summary>
    /// Command arguments (including flags) not including the name.
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public string[] Parameters { get; }

    /// <summary>
    /// Command arguments not including flags or the name.
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public string[] NonFlagParameters { get; }

    /// <summary>
    /// All command flags.
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public string[] Flags { get; }

    /// <summary>
    /// Number of arguments and flags provided.
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public int ArgumentCount { get; }

    /// <summary>
    /// Number of arguments provided (not including flags).
    /// </summary>
    /// <remarks>This is not affected by <see cref="ArgumentOffset"/>.</remarks>
    public int NonFlagArgumentCount { get; }

    /// <summary>
    /// Steam 64 id of the caller.
    /// </summary>
    /// <remarks><see cref="CSteamID.Nil"/> when called by console.</remarks>
    public CSteamID CallerId { get; }

    /// <summary>
    /// Original command message sent by the caller.
    /// </summary>
    public string OriginalMessage { get; }

    /// <summary>
    /// If this interaction has been responded to yet.
    /// </summary>
    public bool Responded { get; private set; }

    /// <summary>
    /// The locale to use for this command.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// The language to use for this command.
    /// </summary>
    public LanguageInfo Language { get; }

    /// <summary>
    /// Format used to parse numbers for this command.
    /// </summary>
    public NumberFormatInfo ParseFormat { get; }

    /// <summary>
    /// If the player has the <see cref="PlayerSave.IMGUI"/> setting ticked.
    /// </summary>
    public bool IMGUI { get; }

    /// <summary>
    /// Base cooldown time for a command.
    /// </summary>
    public float? CommandCooldownTime { get; set; }

    /// <summary>
    /// Manually set cooldown time for a command. Should be set before exiting the command.
    /// </summary>
    public float? IsolatedCommandCooldownTime { get; set; }

    /// <summary>
    /// If this command is already on the isolated cooldown.
    /// </summary>
    public bool OnIsolatedCooldown { get; private set; }

    /// <summary>
    /// The isolated cooldown this command is already on.
    /// </summary>
    public Cooldown? IsolatedCooldown { get; private set; }

    /// <summary>
    /// Command instance being executed.
    /// </summary>
    public IExecutableCommand Command { get; internal set; }

    /// <summary>
    /// Type information about the command.
    /// </summary>
    public CommandType CommandInfo { get; }
    public CommandContext(ICommandUser user, string[] args, string originalMessage, CommandType commandInfo, UserPermissionStore permissionsStore)
    {
        Command = null!;
        Caller = user;
        CommandInfo = commandInfo;
        Player = user as UCPlayer;
        _permissionsStore = permissionsStore;

        IsConsole = user is null; // todo make console user

        OriginalMessage = originalMessage;
        Parameters = args ?? Array.Empty<string>();

        // flag parsing
        int flags = 0;
        for (int i = 0; i < Parameters.Length; ++i)
        {
            if (IsFlag(Parameters[i]))
                ++flags;
        }

        NonFlagParameters = flags == 0 ? Parameters : new string[Parameters.Length - flags];

        if (flags > 0)
        {
            Flags = new string[flags];
            for (int i = Parameters.Length - 1; i >= 0; --i)
            {
                if (IsFlag(Parameters[i]))
                    Flags[--flags] = Parameters[i][1..];
            }
            flags = Parameters.Length - Flags.Length;
            for (int i = Parameters.Length - 1; i >= 0; --i)
            {
                if (!IsFlag(Parameters[i]))
                    NonFlagParameters[--flags] = Parameters[i];
            }
        }
        else Flags = Array.Empty<string>();

        for (int i = 0; i < Parameters.Length; ++i)
        {
            if (IsEscapedFlag(Parameters[i]))
                Parameters[i] = Parameters[i][1..];
        }

        if (flags != 0)
        {
            for (int i = 0; i < NonFlagParameters.Length; ++i)
            {
                if (IsEscapedFlag(NonFlagParameters[i]))
                    NonFlagParameters[i] = NonFlagParameters[i][1..];
            }
        }

        ArgumentCount = Parameters.Length;
        NonFlagArgumentCount = NonFlagParameters.Length;
        if (user is null)
        {
            IsConsole = true;
            CallerId = CSteamID.Nil;
        }
        else
        {
            IsConsole = false;
            CallerId = user.Steam64;
        }

        if (Player == null)
        {
            Language = Localization.GetDefaultLanguage();
            Culture = Warfare.Data.AdminLocale;
            ParseFormat = Culture.NumberFormat;
        }
        else
        {
            Language = Player.Locale.LanguageInfo;
            Culture = Player.Locale.CultureInfo;
            ParseFormat = Player.Locale.ParseFormat;
            IMGUI = Player is { Save.IMGUI: true };
        }
    }
    private static bool IsFlag(string arg)
    {
        return arg.Length > 1 && arg[0] == '-' && !char.IsDigit(arg[1]) && arg[1] != '.';
    }
    private static bool IsEscapedFlag(string arg)
    {
        return arg.Length > 2 && arg[0] == '\\' && arg[1] == '-' && !char.IsDigit(arg[2]) && arg[2] != '.';
    }

    /// <summary>
    /// Keep the command from sending the 'no response' message without sending anything.
    /// </summary>
    /// <returns>The instance of this <see cref="CommandContext"/> for chaining or throwing.</returns>
    public CommandContext Defer()
    {
        Responded = true;
        return this;
    }

    /// <summary>
    /// Check if there is an argument at <paramref name="position"/>.
    /// </summary>
    /// <param name="position">Zero-based argument index not including the command name.</param>
    public bool HasArgument(int position)
    {
        position -= ArgumentOffset;
        return position > -1 && position < NonFlagArgumentCount;
    }

    /// <summary>
    /// Check if there are at least <paramref name="count"/> arguments.
    /// </summary>
    /// <param name="count">One-based argument index not including the command name.</param>
    public bool HasArgs(int count)
    {
        count -= ArgumentOffset;
        return count > -1 && count <= NonFlagArgumentCount;
    }

    /// <summary>
    /// Check if there are exactly <paramref name="count"/> arguments.
    /// </summary>
    /// <param name="count">One-based argument index not including the command name.</param>
    public bool HasArgsExact(int count)
    {
        count -= ArgumentOffset;
        return count == NonFlagArgumentCount;
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/>.</returns>
    public bool MatchParameter(int parameter, string value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
            return false;

        return Parameters[parameter].Equals(value, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/> and <paramref name="alternate"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/> or <paramref name="alternate"/>.</returns>
    public bool MatchParameter(int parameter, string value, string alternate)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
            return false;

        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="value"/>, <paramref name="alternate1"/>, and <paramref name="alternate2"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches <paramref name="value"/>, <paramref name="alternate1"/>, or <paramref name="alternate2"/>.</returns>
    public bool MatchParameter(int parameter, string value, string alternate1, string alternate2)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
            return false;

        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate1, StringComparison.InvariantCultureIgnoreCase) || v.Equals(alternate2, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Compares the value of argument <paramref name="parameter"/> with <paramref name="alternates"/>. Case and culture insensitive.
    /// </summary>
    /// <param name="parameter">Zero-based argument index.</param>
    /// <returns><see langword="true"/> if <paramref name="parameter"/> matches one of the values in <paramref name="alternates"/>.</returns>
    public bool MatchParameter(int parameter, params string[] alternates)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
            return false;

        string v = Parameters[parameter];
        for (int i = 0; i < alternates.Length; ++i)
        {
            if (v.Equals(alternates[i], StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compare the value of all flags with <paramref name="value"/>. Case and culture insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if the parameter matches.</returns>
    public bool MatchFlag(string value)
    {
        if (value.Length < 1 || value[0] == '-' && value.Length < 2)
            return false;

        if (value[0] == '-')
            value = value.Substring(1);

        for (int i = 0; i < Flags.Length; ++i)
        {
            if (Flags[i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compare the value of all flags with <paramref name="value"/> and <paramref name="alternate"/>. Case and culture insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchFlag(string value, string alternate)
    {
        if (value.Length >= 1 && (value[0] != '-' || value.Length >= 2))
        {
            if (value[0] == '-')
                value = value.Substring(1);
            for (int i = 0; i < Flags.Length; ++i)
            {
                if (Flags[i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        if (alternate.Length < 1 || alternate[0] == '-' && alternate.Length < 2)
            return false;

        if (alternate[0] == '-')
            alternate = alternate.Substring(1);

        for (int i = 0; i < Flags.Length; ++i)
        {
            if (Flags[i].Equals(alternate, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compare the value of all flags with <paramref name="value"/>, <paramref name="alternate1"/>, and <paramref name="alternate2"/>. Case and culture insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchFlag(string value, string alternate1, string alternate2)
    {
        if (value.Length >= 1 && (value[0] != '-' || value.Length >= 2))
        {
            if (value[0] == '-')
                value = value.Substring(1);
            for (int i = 0; i < Flags.Length; ++i)
            {
                if (Flags[i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        if (alternate1.Length >= 1 && (alternate1[0] != '-' || alternate1.Length >= 2))
        {
            if (alternate1[0] == '-')
                alternate1 = alternate1.Substring(1);
            for (int i = 0; i < Flags.Length; ++i)
            {
                if (Flags[i].Equals(alternate1, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        if (alternate2.Length < 1 || alternate2[0] == '-' && alternate2.Length < 2)
            return false;

        if (alternate2[0] == '-')
            alternate2 = alternate2.Substring(1);

        for (int i = 0; i < Flags.Length; ++i)
        {
            if (Flags[i].Equals(alternate2, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compare the value of all flags with all <paramref name="alternates"/>. Case insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchFlag(params string[] alternates)
    {
        for (int i = 0; i < alternates.Length; ++i)
        {
            string value = alternates[i];
            if (value.Length < 1 || value[0] == '-' && value.Length < 2)
                continue;
            if (value[0] == '-')
                value = value.Substring(1);
            for (int j = 0; j < Flags.Length; ++j)
            {
                if (Flags[j].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Returns the <paramref name="parameter"/> at a given index, or <see langword="null"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public string? Get(int parameter)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
            return null;
        return NonFlagParameters[parameter];
    }

    /// <summary>
    /// Returns a range of parameters from a given <paramref name="start"/> index along a given <paramref name="length"/> (joined by spaces), or <see langword="null"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public string? GetRange(int start, int length = -1)
    {
        if (length == 1) return Get(start);
        start += ArgumentOffset;
        if (start < 0 || start >= NonFlagArgumentCount)
            return null;
        if (start == NonFlagArgumentCount - 1)
            return NonFlagParameters[start];
        if (length == -1)
            return string.Join(" ", NonFlagParameters, start, NonFlagArgumentCount - start);
        if (length < 1) return null;
        if (start + length >= NonFlagArgumentCount)
            length = NonFlagArgumentCount - start;
        return string.Join(" ", NonFlagParameters, start, length);
    }

    /// <summary>
    /// Gets a range of parameters from a given <paramref name="start"/> index along a given <paramref name="length"/> (joined by spaces), or returns <see langword="false"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGetRange(int start, [MaybeNullWhen(false)] out string value, int length = -1)
    {
        value = GetRange(start, length);
        return value is not null;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, or returns <see langword="false"/> if out of range.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out string value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = null!;
            return false;
        }
        value = NonFlagParameters[parameter];
        return true;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <typeparamref name="TEnum"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet<TEnum>(int parameter, out TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = default;
            return false;
        }

        return Enum.TryParse(NonFlagParameters[parameter], true, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="Color"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Color value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = Color.white;
            return false;
        }

        return FormattingUtility.TryParseColor(NonFlagParameters[parameter], out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="Color32"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Color32 value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = Color.white;
            return false;
        }

        return FormattingUtility.TryParseColor32(NonFlagParameters[parameter], out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="int"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out int value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return int.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="byte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out byte value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return byte.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="short"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out short value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return short.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="sbyte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out sbyte value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return sbyte.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="Guid"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out Guid value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = default;
            return false;
        }
        return Guid.TryParse(NonFlagParameters[parameter], out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="uint"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out uint value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return uint.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ushort"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out ushort value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return ushort.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ulong"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use <see cref="TryGet(int,out ulong,out EditorUser?, bool)"/> instead for Steam64 IDs.</remarks>
    public bool TryGet(int parameter, out ulong value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return ulong.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="bool"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out bool value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = false;
            return false;
        }

        string p = NonFlagParameters[parameter];
        if (p.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("1", StringComparison.InvariantCultureIgnoreCase) ||
            p.Equals("y", StringComparison.InvariantCultureIgnoreCase))
        {
            value = true;
        }
        else if (p.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("0", StringComparison.InvariantCultureIgnoreCase) ||
                 p.Equals("n", StringComparison.InvariantCultureIgnoreCase))
        {
            value = false;
        }
        else
        {
            value = false;
            return false;
        }


        return true;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="float"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out float value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return float.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="double"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out double value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return double.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="decimal"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing.</remarks>
    public bool TryGet(int parameter, out decimal value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out value);
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <typeparamref name="TEnum"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef<TEnum>(int parameter, ref TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = default;
            return false;
        }
        if (Enum.TryParse(NonFlagParameters[parameter], true, out TEnum value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as an <see cref="int"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref int value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (int.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out int value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="byte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref byte value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (byte.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out byte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="sbyte"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref sbyte value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (sbyte.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out sbyte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="Guid"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref Guid value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = default;
            return false;
        }
        if (Guid.TryParse(NonFlagParameters[parameter], out Guid value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="uint"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref uint value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (uint.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out uint value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ushort"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref ushort value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ushort.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out ushort value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="ulong"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref ulong value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ulong.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out ulong value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="float"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref float value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (float.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out float value2) && !float.IsNaN(value2) && !float.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="double"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref double value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (double.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out double value2) && !double.IsNaN(value2) && !double.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a <paramref name="parameter"/> at a given index, parses it as a <see cref="decimal"/>, or returns <see langword="false"/> if out of range or unable to parse.
    /// </summary>
    /// <remarks>Zero based indexing. Use the 'ref' set of TryGet methods to ensure the original <paramref name="value"/> isn't overwritten.</remarks>
    public bool TryGetRef(int parameter, ref decimal value)
    {
        parameter += ArgumentOffset;
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            value = 0;
            return false;
        }
        if (decimal.TryParse(NonFlagParameters[parameter], NumberStyles.Number, ParseFormat, out decimal value2))
        {
            value = value2;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Find a user or Steam64 ID from an argument. Will take either Steam64 or name. Will only find offline users by Steam64 ID.
    /// </summary>
    /// <param name="steam64">Parsed steam ID.</param>
    /// <param name="onlinePlayer">Will be set to the <see cref="EditorUser"/> instance if they're online.</param>
    /// <param name="remainder">Select the rest of the arguments instead of just one.</param>
    /// <remarks>Zero based indexing.</remarks>
    /// <returns><see langword="true"/> if a valid Steam64 id is parsed (even when the user is offline).</returns>
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer? onlinePlayer, bool remainder = false, UCPlayer.NameSearch searchType = UCPlayer.NameSearch.CharacterName)
    {
        parameter += ArgumentOffset;
        if (!IsConsole && MatchParameter(parameter, "me"))
        {
            onlinePlayer = Player;
            steam64 = CallerId.m_SteamID;
            return true;
        }
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null;
            return false;
        }

        string? s = remainder ? GetRange(parameter - ArgumentOffset) : NonFlagParameters[parameter];
        if (s != null)
        {
            if (FormattingUtility.TryParseSteamId(s, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            {
                steam64 = steamId.m_SteamID;
                onlinePlayer = UCPlayer.FromID(steam64);
                return true;
            }
            onlinePlayer = UCPlayer.FromName(s, searchType);
            if (onlinePlayer is { IsOnline: true })
            {
                steam64 = onlinePlayer.Steam64;
                return true;
            }
        }

        steam64 = default;
        onlinePlayer = null;
        return false;
    }

    /// <summary>
    /// Find a user or Steam64 ID from an argument. Will take either Steam64 or name. Searches online players in <paramref name="selection"/>.
    /// </summary>
    /// <param name="steam64">Parsed steam ID.</param>
    /// <param name="onlinePlayer">Will be set to the <see cref="EditorUser"/> instance when <see langword="true"/> is returned.</param>
    /// <param name="remainder">Select the rest of the arguments instead of just one.</param>
    /// <remarks>Zero based indexing.</remarks>
    /// <returns><see langword="true"/> if a valid Steam64 id is parsed and that player is in <paramref name="selection"/>.</returns>
    public bool TryGet(int parameter, out ulong steam64, [MaybeNullWhen(false)] out UCPlayer onlinePlayer, IEnumerable<UCPlayer> selection, bool remainder = false, UCPlayer.NameSearch searchType = UCPlayer.NameSearch.CharacterName)
    {
        parameter += ArgumentOffset;
        if (!IsConsole && MatchParameter(parameter, "me"))
        {
            onlinePlayer = Player;
            steam64 = CallerId.m_SteamID;
            return selection.Contains(Caller);
        }
        if (parameter < 0 || parameter >= NonFlagArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null;
            return false;
        }

        string? s = remainder ? GetRange(parameter - ArgumentOffset) : NonFlagParameters[parameter];
        if (s == null)
        {
            steam64 = default;
            onlinePlayer = default;
            return false;
        }
        if (FormattingUtility.TryParseSteamId(s, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            steam64 = steamId.m_SteamID;
            foreach (UCPlayer player in selection)
            {
                if (player.Steam64 == steam64)
                {
                    onlinePlayer = player;
                    return true;
                }
            }
        }
        onlinePlayer = UCPlayer.FromName(s, selection, searchType)!;
        if (onlinePlayer is { IsOnline: true })
        {
            steam64 = onlinePlayer.Steam64;
            return true;
        }

        steam64 = default;
        return false;
    }


    /// <summary>
    /// Get an asset based on a <see cref="Guid"/> search, <see cref="ushort"/> search, then <see cref="Asset.FriendlyName"/> search.
    /// </summary>
    /// <typeparam name="TAsset"><see cref="Asset"/> type to find.</typeparam>
    /// <param name="len">Set to 1 to only get one parameter (default), set to -1 to get any remaining Arguments.</param>
    /// <param name="multipleResultsFound"><see langword="true"/> if <paramref name="allowMultipleResults"/> is <see langword="false"/> and multiple results were found.</param>
    /// <param name="allowMultipleResults">Set to <see langword="false"/> to make the function return <see langword="false"/> if multiple results are found. <paramref name="asset"/> will still be set.</param>
    /// <param name="selector">Filter assets to pick from.</param>
    /// <remarks>Zero based indexing. Do not use <see cref="ushort"/>s to search for objects, this is a deprecated feature by Unturned.</remarks>
    /// <returns><see langword="true"/> If a <typeparamref name="TAsset"/> is found or multiple are found and <paramref name="allowMultipleResults"/> is <see langword="true"/>.</returns>
    public bool TryGet<TAsset>(int parameter, [NotNullWhen(true)] out TAsset? asset, out bool multipleResultsFound, bool remainder = false, int len = 1, bool allowMultipleResults = false, Predicate<TAsset>? selector = null) where TAsset : Asset
    {
        if (!TryGetRange(parameter, out string? p, remainder ? -1 : len) || p.Length == 0)
        {
            multipleResultsFound = false;
            asset = null;
            return false;
        }

        if ((remainder || parameter == ArgumentCount - 1) && p[^1] == '\\')
        {
            p = p[..^1];
        }

        return UCAssetManager.TryGetAsset(p, out asset, out multipleResultsFound, allowMultipleResults, selector);
    }

    /// <summary>
    /// Get the transform the caller is looking at.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>. Defaults to <see cref="RayMasks.PLAYER_INTERACT"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetTargetTransform([MaybeNullWhen(false)] out Transform transform, int mask = 0, float distance = 4)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            transform = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? RayMasks.PLAYER_INTERACT : mask, Player.Player);
        transform = info.transform;
        return transform != null;
    }

    /// <summary>
    /// Get <see cref="RaycastInfo"/> from the user.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetTargetInfo([MaybeNullWhen(false)] out RaycastInfo info, int mask, float distance = 4)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            info = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? RayMasks.PLAYER_INTERACT : mask, Player.Player);
        return info.transform != null;
    }

    /// <summary>
    /// Get the <see cref="Interactable"/> the user is looking at.
    /// </summary>
    /// <param name="mask">Raycast mask, could also use <see cref="ERayMask"/>. Defaults to <see cref="RayMasks.PLAYER_INTERACT"/>.</param>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetInteractableTarget<T>([MaybeNullWhen(false)] out T interactable, int mask = 0, float distance = 4f) where T : Interactable
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            interactable = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, mask == 0 ? RayMasks.PLAYER_INTERACT : mask, Player.Player);
        if (info.transform == null)
        {
            interactable = null!;
            return false;
        }

        if (typeof(InteractableVehicle).IsAssignableFrom(typeof(T)))
        {
            interactable = (T)(object)info.vehicle;
            return interactable != null;
        }

        if (typeof(InteractableForage).IsAssignableFrom(typeof(T)))
        {
            if (info.transform.TryGetComponent(out InteractableForage forage))
            {
                interactable = (T)(object)forage;
                return interactable != null;
            }
        }

        if (ObjectManager.tryGetRegion(info.transform, out byte objX, out byte objY, out ushort index))
        {
            LevelObject obj = LevelObjects.objects[objX, objY][index];
            interactable = obj.interactable as T;
            return interactable != null;
        }

        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        interactable = drop?.interactable as T;
        return interactable != null;
    }

    /// <summary>
    /// Get the <see cref="BarricadeDrop"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetBarricadeTarget([MaybeNullWhen(false)] out BarricadeDrop drop, float distance = 4f)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            drop = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.BARRICADE, Player.Player);
        if (info.transform == null)
        {
            drop = null;
            return false;
        }

        drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        return drop != null;
    }

    /// <summary>
    /// Get the <see cref="StructureDrop"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetStructureTarget([MaybeNullWhen(false)] out StructureDrop drop, float distance = 4f)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            drop = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.STRUCTURE, Player.Player);
        if (info.transform == null)
        {
            drop = null;
            return false;
        }

        drop = StructureManager.FindStructureByRootTransform(info.transform);
        return drop != null;
    }

    /// <summary>
    /// Get the <see cref="InteractableVehicle"/> the user is looking at.
    /// </summary>
    /// <param name="distance">Default distance is 4m.</param>
    public bool TryGetVehicleTarget([MaybeNullWhen(false)] out InteractableVehicle vehicle, float distance = 4f, bool tryCallersVehicleFirst = true, bool allowDead = false)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            vehicle = null;
            return false;
        }

        if (tryCallersVehicleFirst)
        {
            vehicle = Player.Player.movement.getVehicle();
            if (vehicle != null && (allowDead || !vehicle.isDead))
                return true;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.VEHICLE, Player.Player);
        if (info.transform == null)
        {
            vehicle = null;
            return false;
        }

        vehicle = info.vehicle;
        return vehicle != null && (allowDead || !vehicle.isDead);
    }
    public bool TryGetPlayerTarget([MaybeNullWhen(false)] out UCPlayer player, float distance = 4f)
    {
        if (IsConsole || Player is null || !Player.IsOnline)
        {
            player = null;
            return false;
        }

        Transform aim = Player.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), distance, RayMasks.PLAYER, Player.Player);
        player = (info.player == null ? null : UCPlayer.FromPlayer(info.player))!;
        return player != null && player.IsOnline;
    }

    /// <summary>
    /// Add an entry to the <see cref="ActionLog"/>.
    /// </summary>
    public void LogAction(ActionLogType type, string? data = null)
    {
        ActionLog.Add(type, data, CallerId.m_SteamID);
    }

    /// <summary>
    /// Check if <see cref="Caller"/> has <paramref name="permission"/>. Always returns <see langword="true"/> when ran with console.
    /// </summary>
    public ValueTask<bool> HasPermission(PermissionLeaf permission, CancellationToken token = default)
    {
        return _permissionsStore.HasPermissionAsync(Caller, permission, token);
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have <paramref name="permission"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissions(PermissionLeaf permission, CancellationToken token = default)
    {
        ValueTask<bool> vt = HasPermission(permission, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, permission));
        }

        if (!vt.Result)
            throw SendNoPermission(permission);
            
        return default;

        async Task Core(ValueTask<bool> vt, PermissionLeaf permission)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
                throw SendNoPermission(permission);
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token = default)
    {
        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, true, permission1, permission2, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, false, permission1, permission2, token));
        }

        if (vt.Result)
            return default;

        throw SendNoPermission(permission1);

        async Task Core(ValueTask<bool> vt, bool isFirst, PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (hasPerm)
                return;
            
            if (!isFirst)
                return;

            hasPerm = await HasPermission(permission2, token);
            if (!hasPerm)
                throw SendNoPermission(permission1);
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token = default)
    {
        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 0, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 1, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

        vt = HasPermission(permission3, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 2, permission1, permission2, permission3, token));
        }

        if (vt.Result)
            return default;

        throw SendNoPermission(permission1);

        async Task Core(ValueTask<bool> vt, int ctDone, PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (hasPerm)
                return;

            if (ctDone == 2)
                return;

            hasPerm = await HasPermission(ctDone == 1 ? permission3 : permission2, token);
            if (hasPerm)
                return;

            if (ctDone == 1)
                return;

            hasPerm = await HasPermission(permission3, token);
            if (!hasPerm)
                throw SendNoPermission(permission1);
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <remarks>If <paramref name="permissions"/> is empty, nothing will happen.</remarks>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsOr(params PermissionLeaf[] permissions) => AssertPermissionsOr(default, permissions);

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have at least one of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <remarks>If <paramref name="permissions"/> is empty, nothing will happen.</remarks>
    /// <exception cref="CommandContext"/>
    public async ValueTask AssertPermissionsOr(CancellationToken token, params PermissionLeaf[] permissions)
    {
        if (permissions.Length == 0)
            return;

        for (int i = 0; i < permissions.Length; i++)
        {
            if (await HasPermission(permissions[i], token))
                return;
        }

        throw SendNoPermission(permissions[0]);
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token = default)
    {
        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, true, permission1, permission2, token));
        }

        if (!vt.Result)
            throw SendNoPermission(permission1);

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, false, permission1, permission2, token));
        }

        if (!vt.Result)
            throw SendNoPermission(permission2);

        return default;

        async Task Core(ValueTask<bool> vt, bool isFirst, PermissionLeaf permission1, PermissionLeaf permission2, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
                throw SendNoPermission(permission1);

            if (!isFirst)
                return;

            hasPerm = await HasPermission(permission2, token);
            if (!hasPerm)
                throw SendNoPermission(permission2);
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided permissions.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token = default)
    {
        ValueTask<bool> vt = HasPermission(permission1, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 0, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
            throw SendNoPermission(permission1);

        vt = HasPermission(permission2, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 1, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
            throw SendNoPermission(permission2);

        vt = HasPermission(permission3, token);
        if (!vt.IsCompleted)
        {
            return new ValueTask(Core(vt, 2, permission1, permission2, permission3, token));
        }

        if (!vt.Result)
            throw SendNoPermission(permission3);

        return default;

        async Task Core(ValueTask<bool> vt, int ctDone, PermissionLeaf permission1, PermissionLeaf permission2, PermissionLeaf permission3, CancellationToken token)
        {
            bool hasPerm = await vt.ConfigureAwait(false);
            if (!hasPerm)
                throw SendNoPermission(permission1);

            if (ctDone == 2)
                return;

            hasPerm = await HasPermission(ctDone == 1 ? permission3 : permission2, token);
            if (!hasPerm)
                throw SendNoPermission(permission2);

            if (ctDone == 1)
                return;

            hasPerm = await HasPermission(permission3, token);
            if (!hasPerm)
                throw SendNoPermission(permission3);
        }
    }

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public ValueTask AssertPermissionsAnd(params PermissionLeaf[] permissions) => AssertPermissionsAnd(default, permissions);

    /// <summary>
    /// Throws an exception and sends the generic 'no permission' message if the caller doesn't have all of the provided <paramref name="permissions"/>.
    /// </summary>
    /// <exception cref="CommandContext"/>
    public async ValueTask AssertPermissionsAnd(CancellationToken token, params PermissionLeaf[] permissions)
    {
        for (int i = 0; i < permissions.Length; i++)
        {
            if (!await HasPermission(permissions[i], token))
                throw SendNoPermission(permissions[i]);
        }
    }

    /// <summary>
    /// Throws an exception if the isolated cooldown was still active on <see cref="Player"/> when the command was first started.
    /// </summary>
    public void AssertCommandNotOnIsolatedCooldown()
    {
        if (!OnIsolatedCooldown)
            return;

        if (Command is ICompoundingCooldownCommand compounding)
        {
            IsolatedCooldown!.Duration *= compounding.CompoundMultiplier;
            if (compounding.MaxCooldown > 0 && IsolatedCooldown.Duration > compounding.MaxCooldown)
                IsolatedCooldown.Duration = compounding.MaxCooldown;
        }

        throw Reply(T.CommandCooldown, IsolatedCooldown!, CommandInfo.CommandName);
    }

    /// <exception cref="CommandContext"/>
    public void AssertGamemode<T>() where T : class, IGamemode
    {
        if (!Warfare.Data.Is<T>())
            throw SendGamemodeError();
    }

    /// <exception cref="CommandContext"/>
    public void AssertGamemode<T>(out T gamemode) where T : class, IGamemode
    {
        if (!Warfare.Data.Is(out gamemode))
            throw SendGamemodeError();
    }

    /// <exception cref="CommandContext"/>
    public void AssertRanByPlayer()
    {
        if (IsConsole || Player == null || !Player.IsOnline)
            throw SendPlayerOnlyError();
    }

    /// <exception cref="CommandContext"/>
    public void AssertRanByConsole()
    {
        if (!IsConsole)
            throw SendConsoleOnlyError();
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgs(int count, string usage)
    {
        if (!HasArgs(count))
            throw SendCorrectUsage(usage);
    }

    /// <exception cref="CommandContext"/>
    public void AssertArgsExact(int count, string usage)
    {
        if (!HasArgsExact(count))
            throw SendCorrectUsage(usage);
    }

    /// <exception cref="CommandContext"/>
    public void AssertOnDuty()
    {
        if (!IsConsole && Player != null && !Player.OnDuty())
            throw Reply(T.NotOnDuty);
    }

    /// <exception cref="CommandContext"/>
    public void AssertHelpCheckNoUsage(int parameter, string helpMessage)
    {
        if (MatchParameter(parameter, "help"))
            throw ReplyString(helpMessage);
    }

    /// <exception cref="CommandContext"/>
    public void AssertHelpCheck(int parameter, string usage)
    {
        if (MatchParameter(parameter, "help"))
            throw SendCorrectUsage(usage);
    }

    /// <exception cref="CommandContext"/>
    public void AssertHelpCheckNoUsage(int parameter, Translation helpMessage)
    {
        if (MatchParameter(parameter, "help"))
            throw Reply(helpMessage);
    }

    /// <exception cref="CommandContext"/>
    public void AssertHelpCheck(int parameter, Translation usage)
    {
        if (MatchParameter(parameter, "help"))
            throw SendCorrectUsage(usage.Translate(Player, canUseIMGUI: true));
    }

    /// <remarks>Thread Safe</remarks>
    public Exception SendNotImplemented() => Reply(T.NotImplemented);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNotEnabled() => Reply(T.NotEnabled);

    /// <remarks>Thread Safe</remarks>
    public Exception SendGamemodeError() => Reply(T.GamemodeError);

    /// <remarks>Thread Safe</remarks>
    public Exception SendPlayerOnlyError() => Reply(T.PlayersOnly);

    /// <remarks>Thread Safe</remarks>
    public Exception SendConsoleOnlyError() => Reply(T.ConsoleOnly);

    /// <remarks>Thread Safe</remarks>
    public Exception SendUnknownError() => Reply(T.UnknownError);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNoPermission() => Reply(T.NoPermissions);

    /// <remarks>Thread Safe</remarks>
    public Exception SendNoPermission(PermissionLeaf permission) => Reply(T.NoPermissionsSpecific, permission);

    /// <remarks>Thread Safe</remarks>
    public Exception SendPlayerNotFound() => Reply(T.PlayerNotFound);

    /// <remarks>Thread Safe</remarks>
    public Exception SendCorrectUsage(string usage) => Reply(T.CorrectUsage, usage);

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (IsConsole || Caller is null)
        {
            message = FormattingUtility.RemoveRichText(message);
            L.Log(message, ConsoleColor.Gray);
        }
        else
        {
            Player.SendString(message);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, Color color)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (IsConsole || Player is null)
        {
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendString(message, color);
        }
        Responded = true;

        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, ConsoleColor color)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        if (IsConsole || Caller is null)
        {
            message = FormattingUtility.RemoveRichText(message);
            L.Log(message, color);
        }
        else
        {
            Player.SendString(message, FormattingUtility.FromConsoleColor(color));
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception ReplyString(string message, string hex)
    {
        FormattingUtility.TryParseColor(hex, out Color color);
        return ReplyString(message, color);
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply(Translation translation)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, Culture, out Color color, false);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0>(Translation<T0> translation, T0 arg)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, arg5, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4, arg5);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, arg5, arg6, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        Responded = true;
        return this;
    }

    /// <remarks>Thread Safe</remarks>
    public Exception Reply<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(Language, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, out Color color);
            message = FormattingUtility.RemoveRichText(message);
            ConsoleColor clr = FormattingUtility.ToConsoleColor(FormattingUtility.ToArgb(color));
            L.Log(message, clr);
        }
        else
        {
            Player.SendChat(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        Responded = true;
        return this;
    }

    /// <summary>
    /// Parse <paramref name="value"/> into the type of <paramref name="property"/> using <see cref="Culture"/> (a field or property in <see cref="TItem"/>) and set the property if it has the <see cref="CommandSettableAttribute"/>.
    /// </summary>
    /// <param name="instance">Object to set the property for.</param>
    /// <param name="property">Name of a property, field, custom handler, or an alias defined in <see cref="CommandSettableAttribute"/>.</param>
    /// <param name="value">The value to set in string format.</param>
    /// <param name="actualPropertyName">Actual name of the discovered member. Never <see langword="null"/> unless <paramref name="property"/> is <see langword="null"/>.</param>
    /// <param name="propertyType">Actual type of the discovered member. Never <see langword="null"/>, defaults to <see cref="string"/> if a property isn't found..</param>
    /// <returns>A success/error code.</returns>
    public SetPropertyResult SetProperty<TItem>(TItem instance, string property, string value, [NotNullIfNotNull(nameof(property))] out string? actualPropertyName, out Type propertyType) where TItem : class
    {
        return SettableUtil<TItem>.SetProperty(instance, property, value, Culture, out actualPropertyName, out propertyType);
    }

    internal void CheckIsolatedCooldown()
    {
        if (Player != null
            && !Player.OnDuty()
            && CooldownManager.IsLoaded
            && CommandInfo != null
            && CooldownManager.HasCooldown(Player, CooldownType.IsolatedCommand, out Cooldown cooldown, CommandInfo))
        {
            OnIsolatedCooldown = true;
            IsolatedCooldown = cooldown;
        }
        else
        {
            OnIsolatedCooldown = false;
            IsolatedCooldown = null;
        }
    }
}
