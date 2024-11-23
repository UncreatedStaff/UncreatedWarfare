using System;

namespace Uncreated.Warfare.Interaction.Commands;

public class CommandParser(CommandDispatcher dispatcher)
{
    private static readonly ParsedCommandInfo Failure = new ParsedCommandInfo(null, Array.Empty<string>(), Array.Empty<CommandFlagInfo>());
    public CommandDispatcher Dispatcher { get; } = dispatcher;

    public ParsedCommandInfo ParseCommandInput(ReadOnlySpan<char> originalMessage, bool requirePrefix)
    {
        // remove slash that gets put at the end a lot since its right next to enter.
        originalMessage = originalMessage.TrimEnd('\\').TrimStart();

        if (originalMessage.Length < (requirePrefix ? 1 : 0) + 1)
            return Failure;

        ReadOnlySpan<char> startArgChars = [ '"', '\'', '`', '´', '“', '‘' ];
        ReadOnlySpan<char> endArgChars = [ '"', '\'', '`', '´', '”', '’' ];

        // yes these are all different characters
        ReadOnlySpan<char> flagPrefixes = [ '-', '–', '—', '−' ];

        if (requirePrefix)
        {
            ReadOnlySpan<char> prefixes = [ '/', '@', '\\' ];
            char prefix = originalMessage[0];
            if (prefixes.IndexOf(prefix) < 0)
                return Failure;

            originalMessage = originalMessage[1..].TrimStart();
        }

        if (originalMessage.Length < 1)
            return Failure;

        ReadOnlySpan<char> command = null;

        // rare but supported case of something like: '/"command name" [args..]'
        int startQuote = startArgChars.IndexOf(originalMessage[0]);
        if (startQuote >= 0 && originalMessage.Length > 1)
        {
            int endQuote = originalMessage[1..].IndexOf(endArgChars[startQuote]);

            if (endQuote == 0)
            {
                originalMessage = originalMessage[2..].TrimStart();
            }
            else
            {
                if (endQuote < 0)
                    endQuote = originalMessage.Length - 1;

                command = originalMessage[1..(endQuote + 1)];
                originalMessage = endQuote == originalMessage.Length - 1 ? ReadOnlySpan<char>.Empty : originalMessage[(endQuote + 2)..].TrimStart();
            }
        }

        if (command.IsEmpty)
        {
            int firstWhiteSpace = 0;
            while (firstWhiteSpace < originalMessage.Length && !char.IsWhiteSpace(originalMessage[firstWhiteSpace]))
                ++firstWhiteSpace;

            command = originalMessage[..firstWhiteSpace];
            originalMessage = originalMessage[firstWhiteSpace..].TrimStart();
        }

        if (originalMessage.Length == 0)
            return new ParsedCommandInfo(command.IsEmpty ? null : new string(command), Array.Empty<string>(), Array.Empty<CommandFlagInfo>());

        // count args
        int argCt = 0, flagCt = 0;

        ReadOnlySpan<char> args = originalMessage;
        while (!args.IsEmpty)
        {
            ReadOnlySpan<char> next = GetNextArg(ref args, startArgChars, endArgChars, flagPrefixes, out bool isEmpty, out int flagDashCt);
            if (!isEmpty && next.IsEmpty)
                break;

            if (flagDashCt > 0)
                ++flagCt;
            else
                ++argCt;
        }

        string[] argOutput = argCt == 0 ? Array.Empty<string>() : new string[argCt];
        CommandFlagInfo[] flagOutput = flagCt == 0 ? Array.Empty<CommandFlagInfo>() : new CommandFlagInfo[flagCt];
        argCt = -1;
        flagCt = -1;
        while (!originalMessage.IsEmpty)
        {
            ReadOnlySpan<char> next = GetNextArg(ref originalMessage, startArgChars, endArgChars, flagPrefixes, out bool isEmpty, out int flagDashCt);
            if (!isEmpty && next.IsEmpty)
                break;

            string str = new string(next);
            if (flagDashCt > 0)
                flagOutput[++flagCt] = new CommandFlagInfo(str, flagDashCt, argCt);
            else
                argOutput[++argCt] = str;
        }

        return new ParsedCommandInfo(new string(command), argOutput, flagOutput);
    }

    private static ReadOnlySpan<char> GetNextArg(ref ReadOnlySpan<char> args, ReadOnlySpan<char> startArgChars, ReadOnlySpan<char> endArgChars, ReadOnlySpan<char> flagPrefixes, out bool isEmpty, out int flagDashCt)
    {
        while (!args.IsEmpty)
        {
            ReadOnlySpan<char> arg;
            char c = args[0];
            int startQuote = startArgChars.IndexOf(c);
            if (startQuote >= 0)
            {
                int endQuote = args[1..].IndexOf(endArgChars[startQuote]);

                isEmpty = endQuote == 0;

                if (endQuote < 0)
                    endQuote = args.Length - 1;

                arg = args[1..(endQuote + 1)];
                flagDashCt = 0;
                args = endQuote == args.Length - 1 ? ReadOnlySpan<char>.Empty : args[(endQuote + 2)..].TrimStart();

                while (!arg.IsEmpty && endArgChars.IndexOf(arg[^1]) >= 0)
                    arg = arg[..^1];

                return arg;
            }

            isEmpty = false;
            int flagPrefix = flagPrefixes.IndexOf(c);
            if (flagPrefix >= 0)
            {
                int firstNonFlag = 1;
                while (firstNonFlag < args.Length && flagPrefixes[flagPrefix] == args[firstNonFlag])
                    ++firstNonFlag;

                if (firstNonFlag is 1 or 2)
                {
                    args = args[firstNonFlag..];
                    ReadOnlySpan<char> span = GetNextArg(ref args, startArgChars, endArgChars, flagPrefixes, out isEmpty, out flagDashCt);
                    flagDashCt = firstNonFlag;
                    return span;
                }
            }

            int firstWhiteSpace = 0;
            while (firstWhiteSpace < args.Length && !(char.IsWhiteSpace(args[firstWhiteSpace]) || startArgChars.IndexOf(args[firstWhiteSpace]) >= 0))
                ++firstWhiteSpace;

            arg = args[..firstWhiteSpace];
            args = args[firstWhiteSpace..].TrimStart();

            flagDashCt = 0;
            while (!arg.IsEmpty && endArgChars.IndexOf(arg[^1]) >= 0)
                arg = arg[..^1];

            return arg;
        }

        flagDashCt = 0;
        isEmpty = false;
        return ReadOnlySpan<char>.Empty;
    }
}

/// <summary>
/// Output data for <see cref="ICommandParser"/> implementations.
/// </summary>
public readonly struct ParsedCommandInfo(string? commandName, string[] arguments, CommandFlagInfo[] flags)
{
    /// <summary>
    /// The first 'word' in the command string.
    /// </summary>
    public readonly string? CommandName = commandName;

    /// <summary>
    /// List of all arguments entered by the user.
    /// </summary>
    public readonly string[] Arguments = arguments;

    /// <summary>
    /// List of all flags entered by the user.
    /// </summary>
    public readonly CommandFlagInfo[] Flags = flags;

    /// <summary>
    /// Format a round-trip string that could be used later to accurately re-parse the command.
    /// </summary>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(CommandName))
            return "{Command not found}";

        int argCt = Arguments.Length;
        int ct = argCt + Flags.Length;
        int len = 1 + CommandName.Length + ct;

        if (ContainsWhiteSpaceOrQuotes(CommandName))
            len += 2;

        for (int i = 0; i < ct; ++i)
        {
            string str;
            if (i >= argCt)
            {
                ref CommandFlagInfo info = ref Flags[i - argCt];
                str = info.FlagName;
                len += info.DashCount;
            }
            else
            {
                str = Arguments[i];
            }

            len += str.Length;

            // quotes
            if (ContainsWhiteSpaceOrQuotes(str) || IsFlag(str))
                len += 2;
        }

        return string.Create(len, this, (span, state) =>
        {
            char quote1, quote2 = '\0';
            int writeIndex = 1;
            span[0] = '/';
            bool ws = ContainsWhiteSpaceOrQuotes(state.CommandName!);
            if (ws)
            {
                ChooseQuotes(state.CommandName!, out quote1, out quote2);
                span[writeIndex] = quote1;
                ++writeIndex;
            }
            state.CommandName.AsSpan().CopyTo(span[writeIndex..]);
            writeIndex += state.CommandName!.Length;
            if (ws)
            {
                span[writeIndex] = quote2;
                ++writeIndex;
            }

            int argIndex = 0;
            int flagIndex = 0;
            while (true)
            {
                for (; flagIndex < state.Flags.Length; ++flagIndex)
                {
                    ref CommandFlagInfo info = ref state.Flags[flagIndex];
                    if (info.ArgumentPosition >= argIndex)
                    {
                        break;
                    }

                    ++flagIndex;
                    span[writeIndex] = ' ';
                    ++writeIndex;
                    ws = ContainsWhiteSpaceOrQuotes(info.FlagName) || IsFlag(info.FlagName);
                    span.Slice(writeIndex, info.DashCount).Fill('-');
                    writeIndex += info.DashCount;
                    if (ws)
                    {
                        ChooseQuotes(info.FlagName, out quote1, out quote2);
                        span[writeIndex] = quote1;
                        ++writeIndex;
                    }
                    info.FlagName.AsSpan().CopyTo(span[writeIndex..]);
                    writeIndex += info.FlagName.Length;
                    if (ws)
                    {
                        span[writeIndex] = quote2;
                        ++writeIndex;
                    }
                }

                if (argIndex >= state.Arguments.Length)
                {
                    break;
                }

                string arg = state.Arguments[argIndex];
                span[writeIndex] = ' ';
                ++writeIndex;
                ws = ContainsWhiteSpaceOrQuotes(arg) || IsFlag(arg);
                if (ws)
                {
                    ChooseQuotes(arg, out quote1, out quote2);
                    span[writeIndex] = quote1;
                    ++writeIndex;
                }
                arg.AsSpan().CopyTo(span[writeIndex..]);
                writeIndex += arg.Length;
                if (ws)
                {
                    span[writeIndex] = quote2;
                    ++writeIndex;
                }
                ++argIndex;
            }
        });
    }

    private static void ChooseQuotes(string str, out char start, out char end)
    {
        ReadOnlySpan<char> startArgChars = [ '"', '\'', '`', '´', '“', '‘' ];
        ReadOnlySpan<char> endArgChars = [ '"', '\'', '`', '´', '”', '’' ];

        ReadOnlySpan<char> span = str.AsSpan();

        int candidate = 0;
        for (int i = candidate; i < startArgChars.Length; ++i)
        {
            char c1 = startArgChars[i];
            char c2 = endArgChars[i];
            if (span.IndexOf(c1) < 0 && (c2 == c1 || span.IndexOf(c2) < 0))
            {
                start = c1;
                end = c2;
                return;
            }
        }

        start = '"';
        end = '"';
    }

    private static bool IsFlag(string str)
    {
        if (str.Length < 2)
            return false;

        ReadOnlySpan<char> flagPrefixes = [ '-', '–', '—', '−' ];
        if (flagPrefixes.IndexOf(str[0]) < 0)
            return false;

        return str.Length < 2 && flagPrefixes.IndexOf(str[1]) >= 0 || flagPrefixes.IndexOf(str[1]) < 0 || flagPrefixes.IndexOf(str[2]) < 0;
    }

    private static bool ContainsWhiteSpaceOrQuotes(string str)
    {
        if (str.Length == 0)
            return true;

        ReadOnlySpan<char> allQuoteChars = [ '"', '\'', '`', '´', '“', '”', '‘', '’' ];
        for (int j = 0; j < str.Length; ++j)
        {
            char c = str[j];
            if (char.IsWhiteSpace(c) || allQuoteChars.IndexOf(c) > 0)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Provides basic information about command flags.
/// </summary>
/// <param name="dashCount">Number of dashes, either 1 or 2.</param>
/// <param name="argumentPosition">The argument before the flag, where 1 is the first argument. A value of zero indicates this flag was before the first argument.</param>
public readonly struct CommandFlagInfo(string name, int dashCount, int argumentPosition)
{
    /// <summary>
    /// Name of the flag not including the dashes.
    /// </summary>
    public readonly string FlagName = name;

    /// <summary>
    /// Number of dashes, either 1 or 2.
    /// </summary>
    public readonly int DashCount = dashCount;

    /// <summary>
    /// The argument before the flag, where 1 is the first argument. A value of zero indicates this flag was before the first argument.
    /// </summary>
    /// <remarks>Note that there can be more than one flag with the same argument position.</remarks>
    public readonly int ArgumentPosition = argumentPosition;

    /// <summary>
    /// Returns the flag as it was entered.
    /// </summary>
    public override string ToString()
    {
        return string.Create(DashCount + FlagName.Length, this, (span, state) =>
        {
            span[..state.DashCount].Fill('-');
            state.FlagName.AsSpan().CopyTo(span[state.DashCount..]);
        });
    }
}