﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;

namespace Uncreated.Warfare.Commands.CommandSystem;
public static class CommandHandler
{
    public const int CommandMaxTimeoutAtGameEndMs = 11000;
    private static readonly List<IExecutableCommand> Commands = new List<IExecutableCommand>(8);
    public static readonly IReadOnlyList<IExecutableCommand> RegisteredCommands;
    private static readonly char[] Prefixes = { '/', '@', '\\' };
    private static readonly char[] ContinueArgChars = { '\'', '"', '`', '“', '”', '‘', '’' };
    private const int MaxArgCount = 16;
    private static readonly ArgumentInfo[] ArgBuffer = new ArgumentInfo[MaxArgCount];
    internal static CancellationTokenSource GlobalCommandCancel = new CancellationTokenSource();
    internal static List<CommandInteraction> ActiveCommands = new List<CommandInteraction>(8);
    internal static bool TryingToCancel;
    static CommandHandler()
    {
        RegisteredCommands = Commands.AsReadOnly();
        ChatManager.onCheckPermissions += OnChatProcessing;
        CommandWindow.onCommandWindowInputted += OnCommandInput;
    }
    private static readonly List<KeyValuePair<KeyValuePair<UCPlayer?, bool>, string>> PendingMessages = new List<KeyValuePair<KeyValuePair<UCPlayer?, bool>, string>>(32);
    public static async Task LetCommandsFinish()
    {
        TryingToCancel = true;
        GlobalCommandCancel.CancelAfter(CommandMaxTimeoutAtGameEndMs);
        foreach (CommandInteraction intx in ActiveCommands.ToList())
        {
            if (intx.Task != null && !intx.Task.IsCompleted)
            {
                try
                {
                    await intx.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }
        }

        GlobalCommandCancel = new CancellationTokenSource();
        await UCWarfare.ToUpdate(GlobalCommandCancel.Token);
        TryingToCancel = false;
        for (int i = 0; i < PendingMessages.Count; i++)
        {
            KeyValuePair<KeyValuePair<UCPlayer?, bool>, string> msg = PendingMessages[i];
            if (msg.Key.Key == null || msg.Key.Key.IsOnline)
            {
                CheckRunCommand(msg.Key.Key, msg.Value, msg.Key.Value);
            }
        }

        PendingMessages.Clear();
    }
    private static void OnChatProcessing(SteamPlayer player, string text, ref bool shouldExecuteCommand, ref bool shouldList)
    {
        UCPlayer? pl = UCPlayer.FromSteamPlayer(player);
        if (pl is null) return;
        shouldExecuteCommand = false;
        if (CheckRunCommand(pl, text, true))
            shouldList = false;
    }
    public static void LoadCommands()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Commands.Clear();
        RegisterVanillaCommands();
        Type t = typeof(IExecutableCommand);
        foreach (Type cmdType in Assembly.GetCallingAssembly().GetTypes().Where(x => !x.IsAbstract && !x.IsGenericType && !x.IsSpecialName && !x.IsNested && t.IsAssignableFrom(x)))
        {
            if (cmdType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Any(x => x.GetParameters().Length == 0))
            {
                IExecutableCommand cmd = (IExecutableCommand)Activator.CreateInstance(cmdType, true);
                RegisterCommand(cmd);
            }
        }
    }
    internal static void RegisterVanillaCommands()
    {
        for (int i = 0; i < Commander.commands.Count; ++i)
        {
            VanillaCommand cmd = new VanillaCommand(Commander.commands[i]);
            RegisterCommand(cmd);
        }
    }
    public static void RegisterCommand(IExecutableCommand cmd)
    {
        int priority = cmd.Priority;
        string name = cmd.CommandName;
        for (int i = 0; i < Commands.Count; ++i)
        {
            if (name.Equals(Commands[i].CommandName, StringComparison.OrdinalIgnoreCase))
            {
                if (Commands[i].Priority < priority)
                {
                    Commands.Insert(i, cmd);
                    goto regCmd;
                }
                else if (Commands[i].Priority == priority)
                {
                    L.LogWarning("Duplicate command /" + name.ToLower() + " with same priority from assembly: " + cmd.GetType().Assembly.GetName().Name);
                    return;
                }
            }
        }

        Commands.Add(cmd);
    regCmd:
        if (cmd is VanillaCommand)
            L.Log("Command /" + name.ToLower() + " registered from Unturned.", ConsoleColor.DarkGray);
        else
            L.Log("Command /" + name.ToLower() + " registered from assembly: " + cmd.GetType().Assembly.GetName().Name, ConsoleColor.DarkGray);
    }
    internal static EAdminType GetVanillaPermissions(SDG.Unturned.Command command)
    {
        return command switch
        {
            CommandTeleport => EAdminType.TRIAL_ADMIN_ON_DUTY,
            CommandSave => EAdminType.TRIAL_ADMIN_ON_DUTY,
            CommandSpy => EAdminType.ADMIN_ON_DUTY,
            CommandShutdown => EAdminType.ADMIN_ON_DUTY,
            CommandDay => EAdminType.ADMIN_ON_DUTY,
            CommandNight => EAdminType.ADMIN_ON_DUTY,
            CommandWeather => EAdminType.ADMIN_ON_DUTY,
            CommandAdmin => EAdminType.CONSOLE,
            CommandUnadmin => EAdminType.CONSOLE,
            _ => EAdminType.VANILLA_ADMIN,
        };
    }
    private static unsafe bool CheckRunCommand(UCPlayer? player, string message, bool requirePrefix)
    {
        ThreadUtil.assertIsGameThread();
        if (TryingToCancel)
        {
            PendingMessages.Add(new KeyValuePair<KeyValuePair<UCPlayer?, bool>, string>(new KeyValuePair<UCPlayer?, bool>(player, requirePrefix), message));
            return true;
        }
            
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (message == null || message.Length < 2) goto notCommand;
        int cmdStart = -1;
        int cmdEnd = -1;
        int argCt = -1;
        bool foundPrefix = false;
        int len = message.Length;
        int cmdInd = -1;
        bool inArg = false;
        fixed (char* ptr = message)
        {
            for (int i = 0; i < len; ++i)
            {
                char c = *(ptr + i);
                if (!foundPrefix && requirePrefix)
                {
                    if (c == ' ') continue;
                    for (int j = 0; j < Prefixes.Length; ++j)
                    {
                        if (c == Prefixes[j])
                        {
                            foundPrefix = true;
                            break;
                        }
                    }
                    if (!foundPrefix) goto notCommand;
                    continue;
                }

                if (cmdStart == -1)
                {
                    if (c == ' ') goto c;
                    if (!requirePrefix)
                    {
                        for (int j = 0; j < Prefixes.Length; ++j)
                        {
                            if (c == Prefixes[j]) goto c;
                        }
                    }
                    cmdStart = i;
                    c:
                    continue;
                }

                if (cmdEnd == -1)
                {
                    if (i != len - 1)
                    {
                        if (c != ' ') continue;
                        else
                        {
                            char next = message[i + 1];
                            if (next != ' ')
                            {
                                for (int j = 0; j < ContinueArgChars.Length; ++j)
                                {
                                    if (next == ContinueArgChars[j])
                                        goto c;
                                }
                                ref ArgumentInfo info = ref ArgBuffer[++argCt];
                                info.End = -1;
                                info.Start = i + 1;
                            }
                            c:
                            cmdEnd = i - 1;
                        }
                    }
                    else
                        cmdEnd = i;
                    goto getCommand;
                }

                for (int j = 0; j < ContinueArgChars.Length; ++j)
                {
                    if (c == ContinueArgChars[j])
                        goto contArgChr;
                }

                if (c == ' ' && !inArg)
                {
                    if (i == len - 1)
                    {
                        if (argCt != -1)
                        {
                            ref ArgumentInfo info2 = ref ArgBuffer[argCt];
                            if (info2.End == -1)
                                info2.End = i - 1;
                        }
                        break;
                    }

                    char next = message[i + 1];
                    if (next == ' ')
                    {
                        if (argCt != -1)
                        {
                            ref ArgumentInfo info2 = ref ArgBuffer[argCt];
                            if (info2.End == -1)
                                info2.End = i - 1;
                        }
                        continue;
                    }

                    for (int j = 0; j < ContinueArgChars.Length; ++j)
                    {
                        if (next == ContinueArgChars[j])
                            goto c;
                    }
                    goto n;
                    c:
                    continue;
                    n:
                    if (argCt != -1)
                    {
                        ref ArgumentInfo info2 = ref ArgBuffer[argCt];
                        if (info2.End == -1)
                            info2.End = i - 1;
                    }
                    if (i == len - 1) break;
                    if (argCt >= MaxArgCount - 1)
                        goto runCommand;
                    ref ArgumentInfo info = ref ArgBuffer[++argCt];
                    info.End = -1;
                    info.Start = i + 1;
                }
                continue;

                contArgChr:
                if (inArg)
                {
                    ref ArgumentInfo info = ref ArgBuffer[argCt];
                    info.End = i - 1;
                    inArg = false;
                }
                else
                {
                    if (argCt != -1)
                    {
                        ref ArgumentInfo info2 = ref ArgBuffer[argCt];
                        if (info2.End == -1)
                        {
                            if (message[i - 1] == ' ')
                                info2.End = i - 2;
                            else
                                info2.End = i - 1;
                        }
                    }
                    if (i == len - 1) break;
                    if (argCt >= MaxArgCount - 1)
                        goto runCommand;
                    ref ArgumentInfo info = ref ArgBuffer[++argCt];
                    info.Start = i + 1;
                    info.End = -1;
                    inArg = true;
                }
                continue;
                getCommand:
                for (int k = 0; k < Commands.Count; ++k)
                {
                    string c2 = Commands[k].CommandName;
                    fixed (char* ptr2 = c2)
                    {
                        if (cmdEnd - cmdStart + 1 != c2.Length)
                            continue;
                        for (int i2 = cmdStart; i2 <= cmdEnd; ++i2)
                        {
                            char c1 = *(ptr + i2);
                            char c3 = *(ptr2 + i2 - cmdStart);
                            if (!(c1 == c3 ||
                                (c1 < 91 && c1 > 64 && c1 + 32 == c3) ||
                                (c3 < 91 && c3 > 64 && c3 + 32 == c1))) goto nxt;
                        }
                        cmdInd = k;
                        break;
                    nxt:;
                    }
                }

                if (cmdInd == -1)
                {
                    for (int k = 0; k < Commands.Count; ++k)
                    {
                        IExecutableCommand cmd = Commands[k];
                        if (cmd.Aliases is not null && cmd.Aliases.Count > 0)
                        {
                            for (int a = 0; a < cmd.Aliases.Count; ++a)
                            {
                                string c2 = cmd.Aliases[a];
                                fixed (char* ptr2 = c2)
                                {
                                    if (cmdEnd - cmdStart + 1 != c2.Length)
                                        continue;
                                    for (int i2 = cmdStart; i2 <= cmdEnd; ++i2)
                                    {
                                        char c1 = *(ptr + i2);
                                        char c3 = *(ptr2 + i2 - cmdStart);
                                        if (!(c1 == c3 ||
                                              (c1 < 91 && c1 > 64 && c1 + 32 == c3) ||
                                              (c3 < 91 && c3 > 64 && c3 + 32 == c1))) goto nxt;
                                    }
                                    cmdInd = k;
                                    goto brk;
                                nxt:;
                                }
                            }
                        }
                        continue;
                    brk:
                        break;
                    }

                    if (cmdInd == -1)
                        goto notCommand;
                }
                if (i == len - 1) goto runCommand;
            }
            if (argCt != -1)
            {
                ref ArgumentInfo info = ref ArgBuffer[argCt];
                if (info.End == -1)
                {
                    bool endIsC = false;
                    char end = message[len - 1];
                    for (int j = 0; j < ContinueArgChars.Length; ++j)
                    {
                        if (end == ContinueArgChars[j])
                            endIsC = true;
                    }
                    if (endIsC)
                    {
                        info.End = len - 2;
                    }
                    else
                    {
                        info.End = len;
                        do --info.End;
                        while (message[info.End] == ' ' && info.End > -1);
                        if (info.End > 0)
                        {
                            endIsC = false;
                            end = message[info.End];
                            for (int j = 0; j < ContinueArgChars.Length; ++j)
                            {
                                if (end == ContinueArgChars[j])
                                    endIsC = true;
                            }
                            if (endIsC) --info.End;
                        }
                    }
                }
            }
            runCommand:
            if (cmdInd == -1) goto notCommand;
            int ct2 = 0;
            for (int i = 0; i <= argCt; ++i)
            {
                ref ArgumentInfo ai = ref ArgBuffer[i];
                if (ai.End > 0) ct2++;
            }

            int i3 = -1;
            string[] args = argCt == -1 ? Array.Empty<string>() : new string[ct2];
            for (int i = 0; i <= argCt; ++i)
            {
                ref ArgumentInfo ai = ref ArgBuffer[i];
                if (ai.End < 1) continue;
                args[++i3] = new string(ptr, ai.Start, ai.End - ai.Start + 1);
            }
            RunCommand(cmdInd, player, args, message, message[message.Length - 1] == '\\');
        }
        return true;
        notCommand:
        return false;
    }
    internal static void OnLog(string message)
    {
        ActiveCommands.FindLast(x => x.Command is VanillaCommand)?.ReplyString("<#bfb9ac>" + message + "</color>");
    }
    private static void RunCommand(int index, UCPlayer? player, string[] args, string message, bool keepSlash)
    {
        IExecutableCommand cmd = Commands[index];
        CommandInteraction interaction = cmd.SetupCommand(player, args, message, keepSlash);
        ActiveCommands.Add(interaction);
        if (cmd.ExecuteAsynchronously)
        {
            UCWarfare.RunTask(interaction.ExecuteCommandAsync, ctx: (player == null ? "Console" : player.Steam64.ToString(Data.AdminLocale)) + " executing /" + cmd.CommandName + " with " + args.Length + " args.");
        }
        else
        {
            interaction.ExecuteCommandSync();
        }
    }
    
    private static void OnCommandInput(string text, ref bool shouldExecuteCommand)
    {
        if (shouldExecuteCommand && CheckRunCommand(null, text, false))
            shouldExecuteCommand = false;
    }
    private struct ArgumentInfo
    {
        public int Start;
        public int End;
    }
}
public abstract class BaseCommandInteraction : Exception
{
    public readonly IExecutableCommand Command;
    public bool Responded { get; protected set; }
    protected BaseCommandInteraction(IExecutableCommand cmd, string message) : base(message)
    {
        this.Command = cmd;
    }
    internal virtual void MarkComplete()
    {
        Responded = true;
    }
}

/// <summary>Provides helpful information and helper functions relating to the currently executing command.</summary>
public sealed class CommandInteraction : BaseCommandInteraction
{
    public const string Default = "-";
    internal Task? Task;
    private readonly ContextData _ctx;
    private int _offset;
    public ContextData Context => _ctx;
    public UCPlayer Caller => _ctx.Caller;
    public bool IsConsole => _ctx.IsConsole;
    public string[] Parameters => _ctx.Parameters;
    public int ArgumentCount => _ctx.ArgumentCount - _offset;
    public ulong CallerID => _ctx.CallerID;
    public CSteamID CallerCSteamID => _ctx.CallerCSteamID;
    public string OriginalMessage => _ctx.OriginalMessage;
    public int Offset { get => _offset; set => _offset = value; }
    public CommandInteraction(ContextData ctx, IExecutableCommand cmd)
        : base(cmd, ctx.IsConsole ? ("Console Command: " + ctx.OriginalMessage) :
            ("Command ran by " + ctx.CallerID + ": " + ctx.OriginalMessage))
    {
        this._ctx = ctx;
        _offset = 0;
    }

    public string? this[int index] => Get(index);
    internal void ExecuteCommandSync()
    {
        if (Command.CheckPermission(this))
        {
            try
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking("Execute command: " + Command.CommandName);
#endif
                Command.Execute(this);
#if DEBUG
                profiler.Dispose();
#endif
                CommandHandler.ActiveCommands.Remove(this);
                if (!Responded)
                {
                    Reply(T.UnknownError);
                    MarkComplete();
                }

                if (Caller is not null)
                    CommandWaiter.OnCommandExecuted(Caller, Command);
            }
            catch (BaseCommandInteraction i)
            {
                i.MarkComplete();
                if (Caller is not null)
                    CommandWaiter.OnCommandExecuted(Caller, Command);
            }
            catch (Exception ex)
            {
                Reply(T.UnknownError);
                MarkComplete();
                L.LogError(ex);
            }
        }
        else
        {
            Reply(T.NoPermissions);
            MarkComplete();
        }
    }
    internal async Task ExecuteCommandAsync()
    {
        bool rem = false;
        try
        {
            if (CommandHandler.GlobalCommandCancel.IsCancellationRequested)
                return;
            await UCWarfare.ToUpdate();
            if (CommandHandler.GlobalCommandCancel.IsCancellationRequested)
                return;
            if (Command.CheckPermission(this))
            {
                try
                {
#if DEBUG
                    using IDisposable profiler = ProfilingUtils.StartTracking("Execute command: " + Command.CommandName);
#endif
                    
                    await (Task = Command.Execute(this,
                        (Caller is null || !Caller.IsOnline
                            ? CommandHandler.GlobalCommandCancel
                            : CancellationTokenSource.CreateLinkedTokenSource(CommandHandler.GlobalCommandCancel.Token, Caller.DisconnectToken)).Token)
                        ).ConfigureAwait(false);
#if DEBUG
                    profiler.Dispose();
#endif
                    CommandHandler.ActiveCommands.Remove(this);
                    rem = true;
                    if (CommandHandler.TryingToCancel)
                        return;
                    if (!UCWarfare.IsMainThread)
                        await UCWarfare.ToUpdate();
                    if (CommandHandler.TryingToCancel)
                        return;

                    if (!Responded)
                    {
                        Reply(T.UnknownError);
                        MarkComplete();
                    }

                    if (Caller is not null)
                        CommandWaiter.OnCommandExecuted(Caller, Command);
                }
                catch (TaskCanceledException)
                {
                    if (!UCWarfare.IsMainThread)
                        await UCWarfare.ToUpdate();
                    if (CommandHandler.TryingToCancel)
                        return;
                    Reply(T.ErrorCommandCancelled);
                    MarkComplete();
                    L.Log("Execution of " + Command.CommandName + " was cancelled (" + CallerID + ").");
                }
                catch (BaseCommandInteraction i)
                {
                    if (!UCWarfare.IsMainThread)
                        await UCWarfare.ToUpdate();
                    i.MarkComplete();
                    if (Caller is not null)
                        CommandWaiter.OnCommandExecuted(Caller, Command);
                }
                catch (Exception ex)
                {
                    if (!UCWarfare.IsMainThread)
                        await UCWarfare.ToUpdate();
                    Reply(T.UnknownError);
                    MarkComplete();
                    L.LogError(ex);
                }
            }
            else
            {
                if (!UCWarfare.IsMainThread)
                    await UCWarfare.ToUpdate();
                Reply(T.NoPermissions);
                MarkComplete();
            }
        }
        finally
        {
            if (!rem)
                CommandHandler.ActiveCommands.Remove(this);
        }
    }
    public Exception Defer()
    {
        Responded = true;
        return this;
    }
    /// <summary>Zero based. Checks if the argument at index <paramref name="position"/> exists.</summary>
    public bool HasArg(int position)
    {
        position += _offset;
        return position > -1 && position < _ctx.ArgumentCount;
    }
    /// <summary>One based. Checks if there are at least <paramref name="count"/> arguments.</summary>
    public bool HasArgs(int count)
    {
        count += _offset;
        return count > -1 && count <= _ctx.ArgumentCount;
    }
    /// <summary>One based. Checks if there are exactly <paramref name="count"/> argument(s).</summary>
    public bool HasArgsExact(int count)
    {
        count += _offset;
        return count == _ctx.ArgumentCount;
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/>. Case insensitive.</summary>
    public bool MatchParameter(int parameter, string value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        return Parameters[parameter].Equals(value, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/> and <paramref name="alternate"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, string value, string alternate)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/>, <paramref name="alternate1"/>, and <paramref name="alternate2"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, string value, string alternate1, string alternate2)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate1, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate2, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with all <paramref name="alternates"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, params string[] alternates)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        string v = Parameters[parameter];
        for (int i = 0; i < alternates.Length; ++i)
        {
            if (v.Equals(alternates[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetParamForParse(int index) => Parameters[index];
    public string? Get(int parameter)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return null;
        return Parameters[parameter];
    }
    public string? GetRange(int start, int length = -1)
    {
        if (length == 1) return Get(start);
        start += _offset;
        if (start < 0 || start >= _ctx.ArgumentCount)
            return null;
        if (start == _ctx.ArgumentCount - 1)
            return Parameters[start];
        if (length == -1)
            return string.Join(" ", Parameters, start, _ctx.ArgumentCount - start);
        if (length < 1) return null;
        if (start + length >= _ctx.ArgumentCount)
            length = _ctx.ArgumentCount - start;
        return string.Join(" ", Parameters, start, length);
    }
    public bool TryGetRange(int start, out string value, int length = -1)
    {
        value = GetRange(start, length)!;
        return value is not null;
    }
    public bool TryGet(int parameter, out string value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = null!;
            return false;
        }
        value = Parameters[parameter];
        return true;
    }
    public bool TryGet<TEnum>(int parameter, out TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        return Enum.TryParse(GetParamForParse(parameter), true, out value);
    }
    public bool TryGet(int parameter, out int value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return int.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out byte value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return byte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out short value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return short.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out sbyte value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return sbyte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out Guid value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        return Guid.TryParse(GetParamForParse(parameter), out value);
    }
    public bool TryGet(int parameter, out uint value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return uint.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out ushort value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ushort.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out ulong value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ulong.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGetTeam(int parameter, out ulong value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }

        string p = GetParamForParse(parameter);
        if (ulong.TryParse(p, NumberStyles.Number, Warfare.Data.LocalLocale, out value))
        {
            return value is > 0 and < 4;
        }
        if (p.Equals(Teams.TeamManager.Team1Code, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.Team1Name, StringComparison.OrdinalIgnoreCase) || p.Equals("t1", StringComparison.OrdinalIgnoreCase))
        {
            value = 1ul;
            return true;
        }
        if (p.Equals(Teams.TeamManager.Team2Code, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.Team2Name, StringComparison.OrdinalIgnoreCase) || p.Equals("t2", StringComparison.OrdinalIgnoreCase))
        {
            value = 2ul;
            return true;
        }
        if (p.Equals(Teams.TeamManager.AdminCode, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.AdminName, StringComparison.OrdinalIgnoreCase) || p.Equals("t3", StringComparison.OrdinalIgnoreCase))
        {
            value = 3ul;
            return true;
        }
        value = 0ul;
        return false;
    }
    public bool TryGet(int parameter, out float value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return float.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
    public bool TryGet(int parameter, out double value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return double.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    public bool TryGet(int parameter, out decimal value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out value);
    }
    // the ref ones are so you can count on your already existing variable not being overwritten
    public bool TryGetRef<TEnum>(int parameter, ref TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        if (Enum.TryParse(GetParamForParse(parameter), true, out TEnum value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref int value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (int.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out int value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref byte value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (byte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out byte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref sbyte value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (sbyte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out sbyte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref Guid value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        if (Guid.TryParse(GetParamForParse(parameter), out Guid value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref uint value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (uint.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out uint value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref ushort value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ushort.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out ushort value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref ulong value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ulong.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out ulong value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref float value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (float.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out float value2) && !float.IsNaN(value2) && !float.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref double value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (double.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out double value2) && !double.IsNaN(value2) && !double.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref decimal value)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (decimal.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.LocalLocale, out decimal value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer? onlinePlayer)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null;
            return false;
        }

        string s = GetParamForParse(parameter);
        if (ulong.TryParse(s, NumberStyles.Number, Warfare.Data.LocalLocale, out steam64) && Util.IsValidSteam64Id(steam64))
        {
            onlinePlayer = UCPlayer.FromID(steam64);
            return true;
        }
        onlinePlayer = UCPlayer.FromName(s, true);
        if (onlinePlayer is not null)
        {
            steam64 = onlinePlayer.Steam64;
            return true;
        }
        else
            return false;
    }
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer onlinePlayer, IEnumerable<UCPlayer> selection)
    {
        parameter += _offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null!;
            return false;
        }

        string s = GetParamForParse(parameter);
        if (ulong.TryParse(s, NumberStyles.Number, Warfare.Data.LocalLocale, out steam64) && Util.IsValidSteam64Id(steam64))
        {
            foreach (UCPlayer player in selection)
            {
                if (player.Steam64 == steam64)
                {
                    onlinePlayer = player;
                    return true;
                }
            }
        }
        onlinePlayer = UCPlayer.FromName(s, true, selection)!;
        if (onlinePlayer is not null)
        {
            steam64 = onlinePlayer.Steam64;
            return true;
        }
        else
            return false;
    }
    /// <summary>Get an asset based on a <see cref="Guid"/> search, <see cref="ushort"/> search, then <see cref="Asset.FriendlyName"/> search.</summary>
    /// <typeparam name="TAsset"><see cref="Asset"/> type to find.</typeparam>
    /// <param name="length">Set to 1 to only get one parameter (default), set to -1 to get any remaining parameters.</param>
    /// <param name="multipleResultsFound"><see langword="true"/> if <paramref name="allowMultipleResults"/> is <see langword="false"/> and multiple results were found.</param>
    /// <param name="allowMultipleResults">Set to <see langword="false"/> to make the function return <see langword="false"/> if multiple results are found. <paramref name="asset"/> will still be set.</param>
    /// <returns><see langword="true"/> If a <typeparamref name="TAsset"/> is found or multiple are found and <paramref name="allowMultipleResults"/> is <see langword="true"/>.</returns>
    public bool TryGet<TAsset>(int parameter, out TAsset asset, out bool multipleResultsFound, bool remainder = false, int len = 1, bool allowMultipleResults = false, Func<TAsset, bool>? selector = null) where TAsset : Asset
    {
        if (!TryGetRange(parameter, out string p, remainder ? -1 : len) || p.Length == 0)
        {
            multipleResultsFound = false;
            asset = null!;
            return false;
        }
        if ((remainder || parameter == ArgumentCount - 1) && p[p.Length - 1] == '\\')
            p = p.Substring(0, p.Length - 1);
        if (Guid.TryParse(p, out Guid guid))
        {
            asset = Assets.find<TAsset>(guid);
            multipleResultsFound = false;
            return asset is not null && (selector is null || selector(asset));
        }
        EAssetType type = AssetTypeHelper<TAsset>.Type;
        if (type != EAssetType.NONE)
        {
            if (ushort.TryParse(p, out ushort value))
            {
                if (Assets.find(type, value) is TAsset asset2)
                {
                    if (selector is not null && !selector(asset2))
                    {
                        asset = null!;
                        multipleResultsFound = false;
                        return false;
                    }

                    asset = asset2;
                    multipleResultsFound = false;
                    return true;
                }
            }

            TAsset[] assets = selector is null ? Assets.find(type).OfType<TAsset>().OrderBy(x => x.FriendlyName.Length).ToArray() : Assets.find(type).OfType<TAsset>().Where(selector).OrderBy(x => x.FriendlyName.Length).ToArray();
            if (allowMultipleResults)
            {
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.Equals(p, StringComparison.OrdinalIgnoreCase))
                    {
                        asset = assets[i];
                        multipleResultsFound = false;
                        return true;
                    }
                }
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        asset = assets[i];
                        multipleResultsFound = false;
                        return true;
                    }
                }
            }
            else
            {
                List<TAsset> results = new List<TAsset>(16);
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.Equals(p, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(assets[i]);
                    }
                }
                if (results.Count == 1)
                {
                    asset = results[0];
                    multipleResultsFound = false;
                    return true;
                }
                if (results.Count > 1)
                {
                    multipleResultsFound = true;
                    asset = results[0];
                    return false; // if multiple results match for the full name then a partial will be the same
                }
                for (int i = 0; i < assets.Length; ++i)
                {
                    if (assets[i].FriendlyName.IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        results.Add(assets[i]);
                    }
                }
                if (results.Count == 1)
                {
                    asset = results[0];
                    multipleResultsFound = false;
                    return true;
                }
                if (results.Count > 1)
                {
                    multipleResultsFound = true;
                    asset = results[0];
                    return false;
                }
            }
        }
        multipleResultsFound = false;
        asset = null!;
        return false;
    }
    public bool TryGetTarget(out Transform transform)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            transform = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER_INTERACT, Caller.Player);
        transform = info.transform;
        return transform != null;
    }
    public bool TryGetTarget<T>(out T interactable) where T : Interactable
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            interactable = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER_INTERACT, Caller.Player);
        if (info.transform == null)
        {
            interactable = null!;
            return false;
        }
        if (typeof(InteractableVehicle).IsAssignableFrom(typeof(T)))
        {
            interactable = (info.vehicle as T)!;
            return interactable != null;
        }
        if (typeof(InteractableForage).IsAssignableFrom(typeof(T)))
        {
            if (info.transform.TryGetComponent(out InteractableForage forage))
            {
                interactable = (forage as T)!;
                return interactable != null;
            }
        }
        BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        interactable = (drop?.interactable as T)!;
        return interactable != null;
    }
    public bool TryGetTarget(out BarricadeDrop drop)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            drop = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.BARRICADE, Caller.Player);
        if (info.transform == null)
        {
            drop = null!;
            return false;
        }
        drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
        return drop != null;
    }
    public bool TryGetTarget(out StructureDrop drop)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            drop = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.STRUCTURE, Caller.Player);
        if (info.transform == null)
        {
            drop = null!;
            return false;
        }
        drop = StructureManager.FindStructureByRootTransform(info.transform);
        return drop != null;
    }
    public bool TryGetTarget(out InteractableVehicle vehicle)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            vehicle = null!;
            return false;
        }
        vehicle = Caller.Player.movement.getVehicle();
        if (vehicle != null)
            return true;

        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.VEHICLE, Caller.Player);
        if (info.transform == null)
            return false;

        vehicle = info.vehicle;
        return vehicle != null;
    }
    public bool TryGetTarget(out UCPlayer player)
    {
        if (IsConsole || Caller is null || !Caller.IsOnline)
        {
            player = null!;
            return false;
        }
        Transform aim = Caller.Player.look.aim;
        RaycastInfo info = DamageTool.raycast(new Ray(aim.position, aim.forward), 4f, RayMasks.PLAYER, Caller.Player);
        player = (info.player == null ? null : UCPlayer.FromPlayer(info.player))!;
        return player != null && player.IsOnline;
    }
    public void LogAction(ActionLogType type, string? data = null)
    {
        ActionLog.Add(type, data, CallerID);
    }
    public bool HasPermission(EAdminType level, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        return Caller.PermissionCheck(level, comparison);
    }
    public void AssertPermissions(EAdminType level, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        if (!HasPermission(level, comparison))
            throw SendNoPermission();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertGamemode<T>() where T : class, IGamemode
    {
        if (!Warfare.Data.Is<T>())
            throw SendGamemodeError();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertGamemode<T>(out T gamemode) where T : class, IGamemode
    {
        if (!Warfare.Data.Is(out gamemode))
            throw SendGamemodeError();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertRanByPlayer()
    {
        if (IsConsole || !Caller.IsOnline)
            throw SendPlayerOnlyError();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertRanByConsole()
    {
        if (!IsConsole)
            throw SendConsoleOnlyError();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertArgs(int count, string usage)
    {
        if (!HasArgs(count))
            throw SendCorrectUsage(usage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertArgsExact(int count, string usage)
    {
        if (!HasArgsExact(count))
            throw SendCorrectUsage(usage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertOnDuty()
    {
        if (!IsConsole && !Caller.OnDuty())
            throw Reply(T.NotOnDuty);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheckNoUsage(int parameter, string helpMessage)
    {
        if (MatchParameter(parameter, "help"))
            throw ReplyString(helpMessage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheck(int parameter, string usage)
    {
        if (MatchParameter(parameter, "help"))
            throw SendCorrectUsage(usage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheckNoUsage(int parameter, Translation helpMessage)
    {
        if (MatchParameter(parameter, "help"))
            throw Reply(helpMessage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheck(int parameter, Translation usage)
    {
        if (MatchParameter(parameter, "help"))
            throw SendCorrectUsage(usage.Translate(Caller));
    }

    public Exception SendNotImplemented() => Reply(T.NotImplemented);
    public Exception SendNotEnabled() => Reply(T.NotEnabled);
    public Exception SendGamemodeError() => Reply(T.GamemodeError);
    public Exception SendPlayerOnlyError() => Reply(T.PlayersOnly);
    public Exception SendConsoleOnlyError() => Reply(T.ConsoleOnly);
    public Exception SendUnknownError() => Reply(T.UnknownError);
    public Exception SendNoPermission() => Reply(T.NoPermissions);
    public Exception SendPlayerNotFound() => Reply(T.PlayerNotFound);
    public Exception SendCorrectUsage(string usage)
                                            => Reply(T.CorrectUsage, usage);
    public Exception ReplyString(string message, Color color)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (IsConsole || Caller is null)
        {
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendString(message, color);
        Responded = true;

        return this;
    }
    public Exception ReplyString(string message, ConsoleColor color)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (IsConsole || Caller is null)
        {
            message = Util.RemoveRichText(message);
            L.Log(message, color);
        }
        else
            Caller.SendString(message, Util.GetColor(color));
        Responded = true;
        return this;
    }
    public Exception ReplyString(string message, string hex)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (IsConsole || Caller is null)
        {
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(hex.Hex());
            L.Log(message, clr);
        }
        else
            Caller.SendString(message, hex);
        Responded = true;
        return this;
    }
    public Exception ReplyString(string message)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (IsConsole || Caller is null)
        {
            message = Util.RemoveRichText(message);
            L.Log(message, ConsoleColor.Gray);
        }
        else
            Caller.SendString(message);
        Responded = true;
        return this;
    }
    public Exception Reply(Translation translation)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation);
        Responded = true;
        return this;
    }
    public Exception Reply<T>(Translation<T> translation, T arg)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2>(Translation<T1, T2> translation, T1 arg1, T2 arg2)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3>(Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4>(Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5>(Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5, T6>(Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, arg6, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5, arg6);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5, T6, T7>(Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, arg6, arg7, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        Responded = true;
        return this;
    }
    public Exception Reply<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        if (IsConsole || Caller is null)
        {
            string message = translation.Translate(L.Default, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, out Color color);
            message = Util.RemoveRichText(message);
            ConsoleColor clr = Util.GetClosestConsoleColor(color);
            L.Log(message, clr);
        }
        else
            Caller.SendChat(translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        Responded = true;
        return this;
    }
    public IFormatProvider GetLocale()
    {
        return IsConsole ? Warfare.Data.AdminLocale : Localization.GetLocale(Localization.GetLang(CallerID));
    }
    public readonly struct ContextData
    {
        public readonly UCPlayer Caller;
        public readonly bool IsConsole;
        public readonly string[] Parameters;
        public readonly int ArgumentCount;
        public readonly ulong CallerID;
        public readonly CSteamID CallerCSteamID;
        public readonly string OriginalMessage;
        public ContextData(UCPlayer? caller, string[] args, string message)
        {
            this.OriginalMessage = message;
            Parameters = args ?? Array.Empty<string>();
            ArgumentCount = Parameters.Length;
            if (caller is null)
            {
                Caller = null!;
                IsConsole = true;
                CallerID = 0;
                CallerCSteamID = CSteamID.Nil;
            }
            else
            {
                Caller = caller;
                IsConsole = false;
                CallerID = caller.Steam64;
                CallerCSteamID = caller.CSteamID;
            }
        }
    }
}