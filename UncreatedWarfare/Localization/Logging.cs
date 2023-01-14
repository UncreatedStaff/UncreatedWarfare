using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using StackCleaner;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine.Assertions;

namespace Uncreated.Warfare;

public static class L
{
    /// <summary>Default Language (previously <see cref="JSONMethods"/>.DEFAULT_LANGUAGE)</summary>
    public const string Default = LanguageAliasSet.ENGLISH;
    private static readonly byte[] NewLineBytes = System.Text.Encoding.UTF8.GetBytes(Environment.NewLine);
    private static bool _init;
    private static int _indention;
    private static FileStream _log;
    private static bool _inL;
    private static ICommandInputOutput? _defaultIOHandler;
    private delegate void OutputToConsole(string value, ConsoleColor color);
    private static OutputToConsole? _outputToConsoleMethod;
    private static readonly StackTraceCleaner Cleaner = new StackTraceCleaner(new StackCleanerConfiguration()
    {
        ColorFormatting = StackColorFormatType.ExtendedANSIColor,
        Colors = UnityColor32Config.Default,
        IncludeNamespaces = false,
        IncludeFileData = true
    });
    internal static void Init()
    {
        try
        {
            if (_init) return;
            _init = true;
            F.CheckDir(Data.Paths.Logs, out _, true);
            if (File.Exists(Data.Paths.CurrentLog))
            {
                string n = Path.Combine(Data.Paths.Logs, File.GetCreationTime(Data.Paths.CurrentLog).ToString(ActionLog.DateHeaderFormat) + ".txt");
                if (File.Exists(n))
                    File.Delete(n);
                File.Move(Data.Paths.CurrentLog, n);
            }
            _log = new FileStream(Data.Paths.CurrentLog, FileMode.Create, FileAccess.Write, FileShare.Read);
            try
            {
                FieldInfo? defaultIoHandlerFieldInfo = typeof(CommandWindow).GetField("defaultIOHandler", BindingFlags.Instance | BindingFlags.NonPublic);
                if (defaultIoHandlerFieldInfo != null)
                {
                    _defaultIOHandler = (ICommandInputOutput)defaultIoHandlerFieldInfo.GetValue(Dedicator.commandWindow);
                    MethodInfo? appendConsoleMethod = _defaultIOHandler.GetType().GetMethod("outputToConsole", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (appendConsoleMethod != null)
                    {
                        _outputToConsoleMethod = (OutputToConsole)appendConsoleMethod.CreateDelegate(typeof(OutputToConsole), _defaultIOHandler);
                        CommandWindow.Log("Gathered IO Methods for Colored Console Messages");
                        return;
                    }
                }
                _outputToConsoleMethod = null;
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("Couldn't get defaultIOHandler from CommandWindow:");
                CommandWindow.LogError(ex);
                _outputToConsoleMethod = null;
            }
            try
            {
                Harmony.Patches.Patcher.Patch(typeof(Logs).GetMethod(nameof(Logs.printLine)),
                    prefix: new HarmonyMethod(typeof(L).GetMethod(nameof(PrintLinePatch),
                        BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("Error patching Logs.printLine.");
                CommandWindow.LogError(ex);
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError("Error initializing logs.");
            CommandWindow.LogError(ex);
        }
    }
    [OperationTest(DisplayName = "Colored Console Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestColoredConsole()
    {
        Assert.IsNotNull(_outputToConsoleMethod);
        Assert.IsNotNull(_defaultIOHandler);
    }
    private static void PrintLinePatch(string message)
    {
        if (!_inL)
        {
            if (_outputToConsoleMethod is not null)
                AddLog(message);
            CommandHandler.OnLog(message);
        }
    }

    /// <summary>Indents the log by <paramref name="amount"/> spaces until the returned <see cref="IDisposable"/> is disposed of. Doesn't apply to <see cref="LogError(string,ConsoleColor,string)"/></summary>
    /// <remarks><code>using LogIndent log = IndentLog(2);</code></remarks>
    public static IDisposable IndentLog(uint amount) => new LogIndent(amount);
    private readonly struct LogIndent : IDisposable
    {
        public readonly uint Indent;
        public LogIndent(uint amount)
        {
            Indent = amount;
            _indention += (int)amount;
        }
        public void Dispose()
        {
            if (_indention < Indent)
                _indention = 0;
            else
                _indention -= (int)Indent;
        }
    }
    private static void AddLine(string text, ConsoleColor color)
    {
        if (_outputToConsoleMethod is null)
        {
            switch (color)
            {
                default:
                    CommandWindow.Log(text);
                    return;
                case ConsoleColor.Yellow:
                case ConsoleColor.DarkYellow:
                    CommandWindow.LogWarning(text);
                    return;
                case ConsoleColor.Red:
                case ConsoleColor.DarkRed:
                    CommandWindow.LogError(text);
                    return;
            }
        }

        if (_indention == 0)
        {
            _outputToConsoleMethod.Invoke(text, color);
            AddLog(text);
        }
        else if (text.IndexOf('\n') < 0)
        {
            AddLog(text = new string(' ', _indention) + text);
            _outputToConsoleMethod.Invoke(text, color);
        }
        else
        {
            string[] lines = text.Split(SplitChars);
            string ind = new string(' ', _indention);
            lock (_log)
            {
                for (int i = 0; i < lines.Length; ++i)
                {
                    string l = ind + lines[i].Trim(TrimChars);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(l + Environment.NewLine);
                    _log.Write(bytes, 0, bytes.Length);
                    _outputToConsoleMethod.Invoke(l, color);
                }
                _log.Flush();
            }
        }
    }
    [Conditional("DEBUG")]
    public static void LogDebug(string info, ConsoleColor color = ConsoleColor.DarkGray)
    {
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[DEBUG] " + info, color);
        else if (UCWarfare.Config.Debug)
            Log(info, color);
    }
    internal static void NetLogInfo(string message, ConsoleColor color) => LogDebug(message, color);
    internal static void NetLogWarning(string message, ConsoleColor color) => LogWarning(message, color, method: "UncreatedNetworking");
    internal static void NetLogError(string message, ConsoleColor color) => LogError(message, color, method: "UncreatedNetworking");
    internal static void NetLogException(Exception ex) => LogError(ex, method: "UncreatedNetworking");
    public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[INFO]  " + info, color);
        else
        {
            AddLine("[INFO]  " + info, color);
            if (_outputToConsoleMethod is not null)
            {
                _inL = true;
                UnturnedLog.info($"[IN] {info}");
                _inL = false;
            }
        }
    }
    private static void LogAsLibrary(string message, ConsoleColor color)
    {
        ConsoleColor clr = Console.ForegroundColor;
        if (clr != color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = clr;
        }
        else
            Console.WriteLine(message);
    }
    public static void LogWarning(string warning, ConsoleColor color = ConsoleColor.Yellow, [CallerMemberName] string method = "")
    {
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[WARN]  [" + method.ToUpper() + "] " + warning, color);
        else
        {
            AddLine("[WARN]  [" + method.ToUpper() + "] " + warning, color);
            if (_outputToConsoleMethod is not null)
            {
                _inL = true;
                UnturnedLog.warn($"[WA] {warning}");
                _inL = false;
            }
        }
    }
    public static void LogError(string error, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "")
    {
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[ERROR] [" + method.ToUpper() + "] " + error, color);
        else
        {
            AddLine("[ERROR] [" + method.ToUpper() + "] " + error, color);
            if (_outputToConsoleMethod is not null)
            {
                _inL = true;
                UnturnedLog.warn($"[ER] {error}");
                _inL = false;
            }
        }
    }
    public static void LogError(Exception ex, bool cleanStack = true, [CallerMemberName] string method = "")
    {
        if (!UCWarfare.IsLoaded)
            Logging.LogException(ex, cleanStack);
        else WriteExceptionIntl(ex, cleanStack, _indention, method);
    }

    private static readonly char[] TrimChars = { '\n', '\r' };
    private static readonly char[] SplitChars = { '\n' };
    private static void WriteExceptionIntl(Exception ex, bool cleanStack, int indent, string? method = null)
    {
        string ind = indent == 0 ? string.Empty : new string(' ', indent);
        bool inner = false;
        if (indent == 0)
            Monitor.Enter(_log);
        try
        {
            for (; ex != null; ex = ex.InnerException!)
            {
                if (inner)
                {
                    AddLine(string.Empty, ConsoleColor.Red);
                }
                AddLine(ind + (inner ? "Inner Exception: " : ((string.IsNullOrEmpty(method) ? string.Empty : ("[" + method!.ToUpper() + "] ")) + "Exception: ")) + ex.GetType().Name, ConsoleColor.Red);
                AddLine(ind + (ex.Message ?? "No message"), ConsoleColor.DarkRed);
                if (ex is TypeLoadException t)
                {
                    AddLine(ind + "Type: " + t.TypeName, ConsoleColor.DarkRed);
                }
                else if (ex is ReflectionTypeLoadException t2)
                {
                    AddLine(ind + "Type load exceptions:", ConsoleColor.DarkRed);
                    foreach (Exception ex2 in t2.LoaderExceptions)
                    {
                        WriteExceptionIntl(ex2, cleanStack, indent + 1);
                    }
                }
                else if (ex is AggregateException t3)
                {
                    AddLine(ind + "Inner exceptions:", ConsoleColor.DarkRed);
                    foreach (Exception ex2 in t3.InnerExceptions)
                    {
                        WriteExceptionIntl(ex2, cleanStack, indent + 1);
                    }
                }
                if (ex.StackTrace != null)
                {
                    if (cleanStack)
                    {
                        StackTrace trace = new StackTrace(ex, true);
                        string str = Cleaner.GetString(trace);
                        AddLine(str, ConsoleColor.DarkGray);
                    }
                    else
                    {
                        AddLine(indent != 0
                            ? string.Join(Environment.NewLine, ex.StackTrace.Split(SplitChars).Select(x => ind + x.Trim(TrimChars)))
                            : ex.StackTrace, ConsoleColor.DarkGray);
                    }
                }
                if (ex is AggregateException) break;

                inner = true;
            }
            _log.Flush();
        }
        finally
        {
            if (indent == 0)
                Monitor.Exit(_log);
        }
    }
    internal static void RunCommand(string command)
    {
        L.Log(command, ConsoleColor.White);
        bool shouldExecuteCommand = true;
        try
        {
            CommandWindow.onCommandWindowInputted?.Invoke(command, ref shouldExecuteCommand);
        }
        catch (Exception ex)
        {
            L.LogError("Plugin threw an exception from onCommandWindowInputted:");
            L.LogError(ex);
        }
        if (!shouldExecuteCommand || Commander.execute(Steamworks.CSteamID.Nil, command))
            return;
        L.LogError($"Unable to match \"{command}\" with any built-in commands");
    }
    internal static bool IsRequestingLog = false;
    private static void AddLog(string log)
    {
        lock (_log)
        {
            int c = System.Text.Encoding.UTF8.GetByteCount(log);
            byte[] bytes = new byte[c + NewLineBytes.Length];
            Buffer.BlockCopy(NewLineBytes, 0, bytes, c, NewLineBytes.Length);
            System.Text.Encoding.UTF8.GetBytes(log, 0, log.Length, bytes, 0);
            _log.Write(bytes, 0, bytes.Length);
            _log.Flush();
        }
    }

    public static class NetCalls
    {
        //public static readonly NetCall RequestFullLog = new NetCall(ReceiveRequestFullLog);
        public static readonly NetCall<string> RequestRunCommand = new NetCall<string>(ReceiveCommand);
        //public static readonly NetCall<bool> SetRequestLogState = new NetCall<bool>(ReceiveRequestsLogState);

        //public static readonly NetCallRaw<LogMessage, byte> SendLogMessage = new NetCallRaw<LogMessage, byte>(1030, LogMessage.Read, null, LogMessage.Write, null);
        //public static readonly NetCallRaw<LogMessage[], byte> SendFullLog = new NetCallRaw<LogMessage[], byte>(1031, LogMessage.ReadMany, null, LogMessage.WriteMany, null);
        public static readonly NetCall<string> SendFatalException = new NetCall<string>(1131);

        /*[NetCall(ENetCall.FROM_SERVER, 1029)]
        internal static void ReceiveRequestFullLog(MessageContext context) => context.Reply(SendFullLog, Logs.ToArray(), (byte)0);*/
        [NetCall(ENetCall.FROM_SERVER, 1032)]
        internal static void ReceiveCommand(MessageContext context, string command)
        {
            if (UCWarfare.IsMainThread)
                RunCommand(command);
            else
                UCWarfare.RunOnMainThread(() => RunCommand(command));
        }
        /*[NetCall(ENetCall.FROM_SERVER, 1023)]
        private static void ReceiveRequestsLogState(MessageContext context, bool state)
        {
            isRequestingLog = state;
        }*/
    }
}

public enum LogSeverity : byte
{
    Debug,
    Info,
    Warning,
    Error,
    Exception
}