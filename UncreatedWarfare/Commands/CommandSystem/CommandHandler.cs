using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Networking;
using UnityEngine;

namespace Uncreated.Warfare.Commands.CommandSystem;
public static class CommandHandler
{
    private static readonly List<IExecutableCommand> _commands = new List<IExecutableCommand>(8);

    private static readonly char[] PREFIXES = new char[] { '/', '@' };
    private static readonly char[] CONTINUE_ARG_CHARS = new char[] { '\'', '"' };
    private const int MAX_ARG_COUNT = 16;
    private static readonly ArgumentInfo[] _argList = new ArgumentInfo[MAX_ARG_COUNT];
    private static CommandInteraction? _activeVanillaCmd = null;
    static CommandHandler()
    {
        ChatManager.onCheckPermissions += OnChatProcessing;
        CommandWindow.onCommandWindowInputted += OnCommandInput;
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
        RegisterVanillaCommands();
        Type t = typeof(IExecutableCommand);
        Type v = typeof(VanillaCommand);
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
        for (int i = 0; i < _commands.Count; ++i)
        {
            if (name.Equals(_commands[i].CommandName, StringComparison.OrdinalIgnoreCase))
            {
                if (_commands[i].Priority < priority)
                {
                    _commands.Insert(i, cmd);
                    goto regCmd;
                }
                else if (_commands[i].Priority == priority)
                {
                    L.LogWarning("Duplicate command /" + name.ToLower() + " with same priority from assembly: " + cmd.GetType().Assembly.GetName().Name);
                    return;
                }
            }
        }

        _commands.Add(cmd);
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
            _ => EAdminType.VANILLA_ADMIN,
        };
    }
    private static unsafe bool CheckRunCommand(UCPlayer? player, string message, bool requirePrefix)
    {
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
                    else
                    {
                        for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                        {
                            if (c == PREFIXES[j])
                            {
                                foundPrefix = true;
                                break;
                            }
                        }
                        if (!foundPrefix) goto notCommand;
                        continue;
                    }
                }
                else
                {
                    if (cmdStart == -1)
                    {
                        if (c == ' ') goto c;
                        if (!requirePrefix)
                        {
                            for (int j = 0; j < PREFIXES.Length; ++j)
                            {
                                if (c == PREFIXES[j]) goto c;
                            }
                        }
                        cmdStart = i;
                    c:
                        continue;
                    }
                    else if (cmdEnd == -1)
                    {
                        if (i != len - 1)
                        {
                            if (c != ' ') continue;
                            else
                            {
                                char next = message[i + 1];
                                if (next != ' ')
                                {
                                    for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                                    {
                                        if (next == CONTINUE_ARG_CHARS[j])
                                            goto c;
                                    }
                                    ref ArgumentInfo info = ref _argList[++argCt];
                                    info.end = -1;
                                    info.start = i + 1;
                                }
                            c:
                                cmdEnd = i - 1;
                            }
                        }
                        else
                            cmdEnd = i;
                        goto getCommand;
                    }
                    else
                    {
                        for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                        {
                            if (c == CONTINUE_ARG_CHARS[j])
                                goto contArgChr;
                        }

                        if (c == ' ' && !inArg)
                        {
                            if (i == len - 1)
                            {
                                if (argCt != -1)
                                {
                                    ref ArgumentInfo info2 = ref _argList[argCt];
                                    if (info2.end == -1)
                                        info2.end = i - 1;
                                }
                                break;
                            }

                            char next = message[i + 1];
                            if (next == ' ')
                            {
                                if (argCt != -1)
                                {
                                    ref ArgumentInfo info2 = ref _argList[argCt];
                                    if (info2.end == -1)
                                        info2.end = i - 1;
                                }
                                continue;
                            }
                            else
                            {
                                for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                                {
                                    if (next == CONTINUE_ARG_CHARS[j])
                                        goto c;
                                }
                                goto n;
                            c:
                                continue;
                            }
                        n:
                            if (argCt != -1)
                            {
                                ref ArgumentInfo info2 = ref _argList[argCt];
                                if (info2.end == -1)
                                    info2.end = i - 1;
                            }
                            if (i == len - 1) break;
                            if (argCt >= MAX_ARG_COUNT - 1)
                                goto runCommand;
                            ref ArgumentInfo info = ref _argList[++argCt];
                            info.end = -1;
                            info.start = i + 1;
                        }
                        continue;

                    contArgChr:
                        if (inArg)
                        {
                            ref ArgumentInfo info = ref _argList[argCt];
                            info.end = i - 1;
                            inArg = false;
                        }
                        else
                        {
                            if (argCt != -1)
                            {
                                ref ArgumentInfo info2 = ref _argList[argCt];
                                if (info2.end == -1)
                                {
                                    if (message[i - 1] == ' ')
                                        info2.end = i - 2;
                                    else
                                        info2.end = i - 1;
                                }
                            }
                            if (i == len - 1) break;
                            if (argCt >= MAX_ARG_COUNT - 1)
                                goto runCommand;
                            ref ArgumentInfo info = ref _argList[++argCt];
                            info.start = i + 1;
                            info.end = -1;
                            inArg = true;
                        }
                    }
                }
                continue;
            getCommand:
                for (int k = 0; k < _commands.Count; ++k)
                {
                    string c2 = _commands[k].CommandName;
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
                    for (int k = 0; k < _commands.Count; ++k)
                    {
                        IExecutableCommand cmd = _commands[k];
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
                ref ArgumentInfo info = ref _argList[argCt];
                if (info.end == -1)
                {
                    bool endIsC = false;
                    char end = message[len - 1];
                    for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                    {
                        if (end == CONTINUE_ARG_CHARS[j])
                            endIsC = true;
                    }
                    if (endIsC)
                    {
                        info.end = len - 2;
                    }
                    else
                    {
                        info.end = len;
                        do --info.end;
                        while (message[info.end] == ' ' && info.end > -1);
                        if (info.end > 0)
                        {
                            endIsC = false;
                            end = message[info.end];
                            for (int j = 0; j < CONTINUE_ARG_CHARS.Length; ++j)
                            {
                                if (end == CONTINUE_ARG_CHARS[j])
                                    endIsC = true;
                            }
                            if (endIsC) --info.end;
                        }
                    }
                }
            }
        runCommand:
            if (cmdInd == -1) goto notCommand;
            int ct2 = 0;
            for (int i = 0; i <= argCt; ++i)
            {
                ref ArgumentInfo ai = ref _argList[i];
                if (ai.end > 0) ct2++;
            }

            int i3 = -1;
            string[] args = argCt == -1 ? Array.Empty<string>() : new string[ct2];
            for (int i = 0; i <= argCt; ++i)
            {
                ref ArgumentInfo ai = ref _argList[i];
                if (ai.end < 1) continue;
                args[++i3] = new string(ptr, ai.start, ai.end - ai.start + 1);
            }
            RunCommand(cmdInd, player, args, message, message[message.Length - 1] == '\\');
        }
        return true;
    notCommand:
        return false;
    }
    internal static void OnLog(string message)
    {
        if (_activeVanillaCmd is not null)
            _activeVanillaCmd.Reply(message);
    }

    private static void RunCommand(int index, UCPlayer? player, string[] args, string message, bool keepSlash)
    {
        IExecutableCommand cmd = _commands[index];
        CommandInteraction interaction = cmd.SetupCommand(player, args, message, keepSlash);
        if (cmd.CheckPermission(interaction))
        {
            try
            {
                if (player is not null && cmd is VanillaCommand)
                    _activeVanillaCmd = interaction;
#if DEBUG
                IDisposable profiler = ProfilingUtils.StartTracking("Execute command: " + cmd.CommandName);
#endif
                cmd.Execute(interaction);
#if DEBUG
                profiler.Dispose();
#endif
                if (!interaction.Responded)
                {
                    interaction.Reply(Translation.Common.UNKNOWN_ERROR);
                    interaction.MarkComplete();
                }

                if (player is not null)
                    CommandWaitTask.OnCommandExecuted(player, cmd);
            }
            catch (BaseCommandInteraction i)
            {
                i.MarkComplete();
                if (player is not null)
                    CommandWaitTask.OnCommandExecuted(player, cmd);
                return;
            }
            catch (Exception ex)
            {
                interaction.Reply(Translation.Common.UNKNOWN_ERROR);
                interaction.MarkComplete();
                L.LogError(ex);
            }
            finally
            {
                _activeVanillaCmd = null;
            }
        }
        else
        {
            interaction.Reply(Translation.Common.NO_PERMISSIONS);
            interaction.MarkComplete();
        }
    }
    private static void OnCommandInput(string text, ref bool shouldExecuteCommand)
    {
        if (shouldExecuteCommand && CheckRunCommand(null, text, false))
            shouldExecuteCommand = false;
    }
    private struct ArgumentInfo
    {
        public int start;
        public int end;
        public ArgumentInfo()
        {
            start = -1;
            end = -1;
        }
        public ArgumentInfo(int start, int end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
public abstract class BaseCommandInteraction : Exception
{
    public readonly IExecutableCommand Command;
    protected bool _responded = false;
    public bool Responded => _responded;
    protected BaseCommandInteraction(IExecutableCommand cmd, string message) : base (message)
    {
        this.Command = cmd;
    }
    internal virtual void MarkComplete()
    {
        _responded = true;
    }
}

/// <summary>Provides helpful information and helper functions relating to the currently executing command.</summary>
public class CommandInteraction : BaseCommandInteraction
{
    private readonly ContextData _ctx;
    private int offset;
    public ContextData Context => _ctx;
    public UCPlayer Caller => _ctx.Caller;
    public bool IsConsole => _ctx.IsConsole;
    public string[] Parameters => _ctx.Parameters;
    public int ArgumentCount => _ctx.ArgumentCount - offset;
    public ulong CallerID => _ctx.CallerID;
    public CSteamID CallerCSteamID => _ctx.CallerCSteamID;
    public string OriginalMessage => _ctx.OriginalMessage;
    public int Offset { get => offset; set => offset = value; }
    public CommandInteraction(ContextData ctx, IExecutableCommand cmd)
        : base(cmd, ctx.IsConsole ? ("Console Command: " + ctx.OriginalMessage) :
            ("Command ran by " + ctx.CallerID + ": " + ctx.OriginalMessage))
    {
        this._ctx = ctx;
        _responded = false;
        offset = 0;
    }

    public Exception Defer()
    {
        _responded = true;
        return this;
    }

    public Exception Reply(string key, params string[] formatting)
    {
        _responded = true;
        _ctx.Reply(key, formatting);
        return this;
    }


    /// <summary>Zero based. Checks if the argument at index <paramref name="position"/> exists.</summary>
    public bool HasArg(int position)
    {
        position += offset;
        return position > -1 && position < _ctx.ArgumentCount;
    }
    /// <summary>One based. Checks if there are at least <paramref name="count"/> arguments.</summary>
    public bool HasArgs(int count)
    {
        count += offset;
        return count > -1 && count <= _ctx.ArgumentCount;
    }
    /// <summary>One based. Checks if there are exactly <paramref name="count"/> argument(s).</summary>
    public bool HasArgsExact(int count)
    {
        count += offset;
        return count == _ctx.ArgumentCount;
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/>. Case insensitive.</summary>
    public bool MatchParameter(int parameter, string value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        return Parameters[parameter].Equals(value, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/> and <paramref name="alternate"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, string value, string alternate)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with <paramref name="value"/>, <paramref name="alternate1"/>, and <paramref name="alternate2"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, string value, string alternate1, string alternate2)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return false;
        string v = Parameters[parameter];
        return v.Equals(value, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate1, StringComparison.OrdinalIgnoreCase) || v.Equals(alternate2, StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>Zero based, compare the value of argument <paramref name="parameter"/> with all <paramref name="alternates"/>. Case insensitive.</summary>
    /// <returns><see langword="true"/> if one of the parameters match.</returns>
    public bool MatchParameter(int parameter, params string[] alternates)
    {
        parameter += offset;
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
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
            return null;
        return Parameters[parameter];
    }
    public string? GetRange(int start, int length = -1)
    {
        start += offset;
        if (length == 1) return Get(start);
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
        parameter += offset;
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
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        return Enum.TryParse(GetParamForParse(parameter), true, out value);
    }
    public bool TryGet(int parameter, out int value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return int.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out byte value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return byte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out sbyte value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return sbyte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out Guid value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = default;
            return false;
        }
        return Guid.TryParse(GetParamForParse(parameter), out value);
    }
    public bool TryGet(int parameter, out uint value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return uint.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out ushort value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ushort.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out ulong value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return ulong.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGetTeam(int parameter, out ulong value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }

        string p = GetParamForParse(parameter);
        if (ulong.TryParse(p, NumberStyles.Number, Warfare.Data.Locale, out value))
        {
            return value is > 0 and < 4;
        }
        else if (p.Equals(Teams.TeamManager.Team1Code, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.Team1Name, StringComparison.OrdinalIgnoreCase) || p.Equals("t1", StringComparison.OrdinalIgnoreCase))
        {
            value = 1ul;
            return true;
        }
        else if (p.Equals(Teams.TeamManager.Team2Code, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.Team2Name, StringComparison.OrdinalIgnoreCase) || p.Equals("t2", StringComparison.OrdinalIgnoreCase))
        {
            value = 2ul;
            return true;
        }
        else if (p.Equals(Teams.TeamManager.AdminCode, StringComparison.OrdinalIgnoreCase) ||
                 p.Equals(Teams.TeamManager.AdminName, StringComparison.OrdinalIgnoreCase) || p.Equals("t3", StringComparison.OrdinalIgnoreCase))
        {
            value = 3ul;
            return true;
        }
        else
        {
            value = 0ul;
            return false;
        }
    }
    public bool TryGet(int parameter, out float value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return float.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
    public bool TryGet(int parameter, out double value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return double.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    public bool TryGet(int parameter, out decimal value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out value);
    }
    // the ref ones are so you can count on your already existing variable not being overwritten
    public bool TryGetRef<TEnum>(int parameter, ref TEnum value) where TEnum : unmanaged, Enum
    {
        parameter += offset;
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
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (int.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out int value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref byte value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (byte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out byte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref sbyte value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (sbyte.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out sbyte value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref Guid value)
    {
        parameter += offset;
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
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (uint.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out uint value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref ushort value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ushort.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out ushort value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref ulong value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (ulong.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out ulong value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref float value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (float.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out float value2) && !float.IsNaN(value2) && !float.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref double value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (double.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out double value2) && !double.IsNaN(value2) && !double.IsInfinity(value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGetRef(int parameter, ref decimal value)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            value = 0;
            return false;
        }
        if (decimal.TryParse(GetParamForParse(parameter), NumberStyles.Number, Warfare.Data.Locale, out decimal value2))
        {
            value = value2;
            return true;
        }
        return false;
    }
    public bool TryGet(int parameter, out ulong steam64, out UCPlayer? onlinePlayer)
    {
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null;
            return false;
        }

        string s = GetParamForParse(parameter);
        if (ulong.TryParse(s, NumberStyles.Number, Warfare.Data.Locale, out steam64) && OffenseManager.IsValidSteam64ID(steam64))
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
        parameter += offset;
        if (parameter < 0 || parameter >= _ctx.ArgumentCount)
        {
            steam64 = 0;
            onlinePlayer = null!;
            return false;
        }

        string s = GetParamForParse(parameter);
        if (ulong.TryParse(s, NumberStyles.Number, Warfare.Data.Locale, out steam64) && OffenseManager.IsValidSteam64ID(steam64))
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
        parameter += offset;
        if (!TryGetRange(parameter, out string p, remainder ? -1 : len))
        {
            multipleResultsFound = false;
            asset = null!;
            return false;
        }
        if ((remainder || parameter == _ctx.ArgumentCount - 1) && p.EndsWith("\\"))
            p = p.Substring(0, p.Length - 1);
        if (Guid.TryParse(p, out Guid guid))
        {
            asset = Assets.find<TAsset>(guid);
            multipleResultsFound = false;
            return asset is not null && (selector is null || selector(asset));
        }
        EAssetType type = JsonAssetReference<TAsset>.AssetTypeHelper.Type;
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
                else if (results.Count > 1)
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
                else if (results.Count > 1)
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
        else if (typeof(InteractableForage).IsAssignableFrom(typeof(T)))
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
    public bool AssertOnDuty(string noPermissionMessageKey = Translation.Common.NO_PERMISSIONS)
    {
        bool perm = IsConsole || Caller is not null && Caller.OnDuty();
        if (!perm)
            Reply(noPermissionMessageKey);
        return perm;
    }
    public bool OnDutyOrReply(string[] formatting, string noPermissionMessageKey = Translation.Common.NO_PERMISSIONS)
    {
        bool perm = IsConsole || Caller is not null && Caller.OnDuty();
        if (!perm)
            Reply(noPermissionMessageKey, formatting);
        return perm;
    }
    public void LogAction(EActionLogType type, string? data = null)
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
    public void AssertGamemode<T>() where T : IGamemode
    {
        if (!Warfare.Data.Is<T>())
            throw SendGamemodeError();
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertGamemode<T>(out T gamemode) where T : IGamemode
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
            throw JSONMethods.DefaultTranslations.ContainsKey(usage) ? Reply(usage) : SendCorrectUsage(usage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertArgs(int count, string usage, params string[] formatting)
    {
        if (!HasArgs(count))
            throw Reply(usage, formatting);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertArgsExact(int count, string usage, params string[] formatting)
    {
        if (!HasArgsExact(count))
            throw Reply(usage, formatting);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertArgsExact(int count, string usage)
    {
        if (!HasArgsExact(count))
            throw JSONMethods.DefaultTranslations.ContainsKey(usage) ? Reply(usage) : SendCorrectUsage(usage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertOnDuty()
    {
        if (!IsConsole && !Caller.OnDuty())
            throw Reply(Translation.Common.NO_PERMISSIONS_ON_DUTY);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertOnDuty(string key, params string[] formatting)
    {
        if (!IsConsole && !Caller.OnDuty())
            throw Reply(key, formatting);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheck(int parameter, string helpMessage, params string[] formatting)
    {
        if (MatchParameter(parameter, "help"))
            throw Reply(helpMessage);
    }
    /// <exception cref="CommandInteraction"/>
    public void AssertHelpCheck(int parameter, string usage)
    {
        if (MatchParameter(parameter, "help"))
            throw SendCorrectUsage(usage);
    }
    
    public Exception SendNotImplemented()   => Reply(Translation.Common.NOT_IMPLEMENTED);
    public Exception SendNotEnabled()       => Reply(Translation.Common.NOT_ENABLED);
    public Exception SendGamemodeError()    => Reply(Translation.Common.GAMEMODE_ERROR);
    public Exception SendPlayerOnlyError()  => Reply(Translation.Common.PLAYERS_ONLY);
    public Exception SendConsoleOnlyError() => Reply(Translation.Common.CONSOLE_ONLY);
    public Exception SendUnknownError()     => Reply(Translation.Common.UNKNOWN_ERROR);
    public Exception SendNoPermission()     => Reply(Translation.Common.NO_PERMISSIONS);
    public Exception SendCorrectUsage(string usage)
                                            => Reply(Translation.Common.CORRECT_USAGE, usage);
    public Exception SendPlayerNotFound()   => Reply(Translation.Common.PLAYER_NOT_FOUND);

    // this is a struct so it can be passed as a value type to asyc 
    public struct ContextData
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
        public void Reply(string translationKey, params string[] formatting)
        {
            if (translationKey is null) throw new ArgumentNullException(nameof(translationKey));
            if (formatting is null) formatting = Array.Empty<string>();
            if (IsConsole || Caller is null)
            {
                string message = Translation.Translate(translationKey, JSONMethods.DEFAULT_LANGUAGE, out Color color, formatting);
                message = F.RemoveRichText(message);
                ConsoleColor clr = F.GetClosestConsoleColor(color);
                L.Log(message, clr);
            }
            else
            {
                Caller.SendChat(translationKey, formatting);
            }
        }
    }
}