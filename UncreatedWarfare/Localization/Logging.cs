#define FUNCTION_LOG

using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using StackCleaner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.CommandSystem;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Uncreated.Warfare;

public static class L
{
    /// <summary>Default Language (previously <see cref="JSONMethods"/>.DEFAULT_LANGUAGE)</summary>
    public const string Default = LanguageAliasSet.ENGLISH;
    private const char ConsoleEscapeCharacter = '\u001B';
    private static readonly byte[] NewLineBytes = System.Text.Encoding.UTF8.GetBytes(Environment.NewLine);
    private static bool _init;
    private static bool _bufferBadLogs;
    private static int _indention;
    private static FileStream _log;
    private static readonly List<LogMessage> BadLogBuffer = new List<LogMessage>(0);
#if DEBUG && FUNCTION_LOG
    private static FileStream _flog;
#endif
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
    public static bool IsBufferingLogs { get; set; }
    public static void FlushBadLogs()
    {
        lock (BadLogBuffer)
        {
            if (BadLogBuffer is not { Count: > 0 })
                return;
            LogWarning("Errors:");
            using (IndentLog(1))
            {
                foreach (LogMessage msg in BadLogBuffer.Where(x => x.Error))
                {
                    AddLine(msg.Message, msg.Color);
                }
            }

            Log(string.Empty);
            LogWarning("Warnings:");
            using (IndentLog(1))
            {
                foreach (LogMessage msg in BadLogBuffer.Where(x => !x.Error))
                {
                    AddLine(msg.Message, msg.Color);
                }
            }

            Log(string.Empty);

            BadLogBuffer.Clear();
        }
    }

    private readonly struct LogMessage
    {
        public readonly bool Error;
        public readonly ConsoleColor Color;
        public readonly string Message;
        public LogMessage(bool error, ConsoleColor color, string message)
        {
            Error = error;
            Color = color;
            Message = message;
        }
    }
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
#if DEBUG && FUNCTION_LOG
            _flog = new FileStream(Data.Paths.FunctionLog, FileMode.Create, FileAccess.Write, FileShare.Read);
            try
            {
                Harmony.Patches.Patcher.Patch(typeof(ProfilingUtils).GetMethod(nameof(ProfilingUtils.StartTracking), BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(L).GetMethod(nameof(StartTracking),
                        BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("Error patching ProfilingUtils.StartTracking.");
                CommandWindow.LogError(ex);
            }
#endif
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
            try
            {
                FieldInfo? logger = typeof(Debug).GetField("s_Logger", BindingFlags.Static | BindingFlags.NonPublic);
                if (logger != null)
                    logger.SetValue(null, new UCUnityLogger());
                //Log(Debug.unityLogger.GetType().Name);
            }
            catch (Exception ex)
            {
                CommandWindow.LogError("Failed to set unity logger:");
                CommandWindow.LogError(ex);
            }
            
            Application.logMessageReceivedThreaded += OnUnityLogMessage;
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        }
        catch (Exception ex)
        {
            CommandWindow.LogError("Error initializing logs.");
            CommandWindow.LogError(ex);
        }
    }
#if DEBUG && FUNCTION_LOG
    private static readonly string[] BlockedLoggingFunction =
    {
        "Update",
        "FixedUpdate",
        "IsInside",
        "IsDelayed",
        "UpdateSign",
        "UpdateSignInternal",
    };
    static void StartTracking(string callerName, string filepath, int linenumber)
    {
        if (callerName.IndexOf("Log", StringComparison.Ordinal) != -1 ||
            callerName.IndexOf("Translate", StringComparison.Ordinal) != -1 ||
            callerName.IndexOf("Simulate", StringComparison.Ordinal) != -1)
            return;
        for (int i = 0; i < BlockedLoggingFunction.Length; ++i)
        {
            if (BlockedLoggingFunction[i].Equals(callerName, StringComparison.Ordinal)) return;
        }
        if (_flog != null)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(DateTime.Now.ToString("s") + " - " + callerName + " - " + linenumber + " - " + filepath + Environment.NewLine);
            _flog.Write(bytes, 0, bytes.Length);
            _flog.Flush();
        }
    }
#endif
    private static void OnUnityLogMessage(string condition, string stacktrace, LogType type)
    {
        switch (type)
        {
            case LogType.Assert:
            case LogType.Warning:
                if (condition.StartsWith("BoxColliders does not support negative scale or size.", StringComparison.Ordinal))
                    return;
                LogWarning(condition + Environment.NewLine + stacktrace);
                break;
            case LogType.Error:
            case LogType.Exception:
                if (condition.StartsWith("Non-convex MeshCollider", StringComparison.Ordinal))
                    return;
                LogError(condition + Environment.NewLine + stacktrace);
                break;
            default:
                string? topstack = stacktrace.Split(SplitChars).FirstOrDefault()?.Trim(TrimChars);
                if (topstack == null)
                    topstack = condition;
                else topstack = condition + " at \"" + topstack + "\".";
                Log(topstack);
                break;
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

        string time = "[" + DateTime.UtcNow.ToString(ActionLog.DateLineFormat) + "] ";
        if (_indention == 0)
        {
            _outputToConsoleMethod.Invoke(text, color);
            AddLog(time + text);
        }
        else if (text.IndexOf('\n') < 0)
        {
            AddLog(time + (text = new string(' ', _indention) + text));
            _outputToConsoleMethod.Invoke(text, color);
        }
        else
        {
            string[] lines = RemoveANSIFormatting(text).Split(SplitChars);
            string ind = new string(' ', _indention);
            lock (_log)
            {
                for (int i = 0; i < lines.Length; ++i)
                {
                    string l = ind + lines[i].Trim(TrimChars);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(time + l + Environment.NewLine);
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
        string msg = "[WARN]  [" + method.ToUpper() + "] " + warning;
        if (IsBufferingLogs)
        {
            lock (BadLogBuffer)
                BadLogBuffer.Add(new LogMessage(false, color, msg));
        }
        if (!UCWarfare.IsLoaded)
            LogAsLibrary(msg, color);
        else
        {
            AddLine(msg, color);
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
        string msg = "[ERROR] [" + method.ToUpper() + "] " + error;
        if (IsBufferingLogs)
        {
            lock (BadLogBuffer)
                BadLogBuffer.Add(new LogMessage(true, color, msg));
        }
        if (!UCWarfare.IsLoaded)
            LogAsLibrary(msg, color);
        else
        {
            AddLine(msg, color);
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
        {
            if (IsBufferingLogs)
            {
                lock (BadLogBuffer)
                    BadLogBuffer.Add(new LogMessage(false, ConsoleColor.Red, "[ERROR] [" + method.ToUpperInvariant() + "]\n" + ex));
            }
            Logging.LogException(ex, cleanStack);
        }
        else
        {
            WriteExceptionIntl(ex, cleanStack, _indention, method);
        }
    }

    private static readonly char[] TrimChars = { '\n', '\r' };
    private static readonly char[] SplitChars = { '\n' };
    private static void WriteExceptionIntl(Exception ex, bool cleanStack, int indent, string? method = null)
    {
        string ind = indent == 0 ? string.Empty : new string(' ', indent);
        bool inner = false;
        void AddLine2(string error, ConsoleColor color)
        {
            if (IsBufferingLogs)
            {
                lock (BadLogBuffer)
                    BadLogBuffer.Add(new LogMessage(true, color, error));
            }
            AddLine(error, color);
        }
        while (ex != null)
        {
            if (inner)
            {
                AddLine2(string.Empty, ConsoleColor.Red);
            }
            AddLine2(ind + (inner ? "Inner Exception: " : ((string.IsNullOrEmpty(method) ? string.Empty : ("[" + method!.ToUpper() + "] ")) + "Exception: ")) + ex.GetType().Name, ConsoleColor.Red);
            AddLine2(ind + (ex.Message ?? "No message"), ConsoleColor.DarkRed);
            if (ex is TypeLoadException t)
            {
                AddLine2(ind + "Type: " + t.TypeName, ConsoleColor.DarkRed);
            }
            else if (ex is ReflectionTypeLoadException t2)
            {
                AddLine2(ind + "Type load exceptions:", ConsoleColor.DarkRed);
                foreach (Exception ex2 in t2.LoaderExceptions)
                {
                    WriteExceptionIntl(ex2, cleanStack, indent + 1);
                }
            }
            else if (ex is AggregateException t3)
            {
                AddLine2(ind + "Inner exceptions:", ConsoleColor.DarkRed);
                foreach (Exception ex2 in t3.InnerExceptions)
                {
                    WriteExceptionIntl(ex2, cleanStack, indent + 1);
                }
            }
            if (ex.StackTrace != null)
            {
                if (cleanStack)
                {
                    string str = Cleaner.GetString(ex);
                    AddLine2(str, ConsoleColor.DarkGray);
                }
                else
                {
                    AddLine2(indent != 0
                        ? string.Join(Environment.NewLine, ex.StackTrace.Split(SplitChars).Select(x => ind + x.Trim(TrimChars)))
                        : ex.StackTrace, ConsoleColor.DarkGray);
                }
            }
            if (ex is AggregateException) break;
            ex = ex.InnerException!;
            inner = true;
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
            log = RemoveANSIFormatting(log);
            int c = System.Text.Encoding.UTF8.GetByteCount(log);
            byte[] bytes = new byte[c + NewLineBytes.Length];
            Buffer.BlockCopy(NewLineBytes, 0, bytes, c, NewLineBytes.Length);
            System.Text.Encoding.UTF8.GetBytes(log, 0, log.Length, bytes, 0);
            _log.Write(bytes, 0, bytes.Length);
            _log.Flush();
        }
    }
    private static unsafe string RemoveANSIFormatting(string orig)
    {
        if (orig.Length < 5)
            return orig;
        bool found = false;
        int l = orig.Length;
        for (int i = 0; i < l; ++i)
        {
            if (orig[i] == ConsoleEscapeCharacter)
            {
                found = true;
            }
        }

        if (!found)
            return orig;

        // regex: \u001B\[[\d;]*m

        int outpInd = 0;
        char* outp = stackalloc char[l - 3];
        fixed (char* chars = orig)
        {
            int lastCpy = -1;
            for (int i = 0; i < l - 2; ++i)
            {
                if (l > i + 3 && chars[i] == ConsoleEscapeCharacter && chars[i + 1] == '[' && char.IsDigit(chars[i + 2]))
                {
                    int st = i;
                    int c = i + 3;
                    for (; c < l; ++c)
                    {
                        if (chars[c] != ';' && !char.IsDigit(chars[c]))
                        {
                            if (chars[c] == 'm')
                                i = c;

                            break;
                        }

                        i = c;
                    }

                    Buffer.MemoryCopy(chars + lastCpy + 1, outp + outpInd, (l - outpInd) * sizeof(char), (st - lastCpy - 1) * sizeof(char));
                    outpInd += st - lastCpy - 1;
                    lastCpy += st - lastCpy + (c - st);
                }
            }
            Buffer.MemoryCopy(chars + lastCpy + 1, outp + outpInd, (l - outpInd) * sizeof(char), (l - lastCpy) * sizeof(char));
            outpInd += l - lastCpy;
        }

        return new string(outp, 0, outpInd - 1);
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
    private class UCUnityLogger : ILogger
    {
        public bool IsLogTypeAllowed(LogType logType) => true;
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            logHandler.LogFormat(logType, context, format, args);
        }
        public void Log(LogType logType, object message)
        {
            OnUnityLogMessage(message as string ?? message.ToString(), null!, logType);
        }
        public void LogException(Exception exception, Object context)
        {
            logHandler.LogException(exception, context);
        }
        public void Log(LogType logType, object message, Object context)
        {
            OnUnityLogMessage(message as string ?? message.ToString(), context.name + " - " + context.GetType().Name, logType);
        }

        public void Log(LogType logType, string tag, object message)
        {
            OnUnityLogMessage(message as string ?? message.ToString(), tag, logType);
        }

        public void Log(LogType logType, string tag, object message, Object context)
        {
            OnUnityLogMessage(message as string ?? message.ToString(), tag + " - " + context.name + " - " + context.GetType().Name, logType);
        }

        public void Log(object message)
        {
            L.Log(message as string ?? message.ToString());
        }

        public void Log(string tag, object message)
        {
            L.Log(message as string ?? message + " - " + tag);
        }

        public void Log(string tag, object message, Object context)
        {
            L.Log(message as string ?? message + " - " + tag + context.name + " - " + context.GetType().Name);
        }

        public void LogWarning(string tag, object message)
        {
            L.LogWarning(message as string ?? message + " - " + tag);
        }

        public void LogWarning(string tag, object message, Object context)
        {
            L.LogWarning(message as string ?? message + " - " + tag + context.name + " - " + context.GetType().Name);
        }

        public void LogError(string tag, object message)
        {
            L.LogError(message as string ?? message + " - " + tag);
        }

        public void LogError(string tag, object message, Object context)
        {
            L.LogError(message as string ?? message + " - " + tag + context.name + " - " + context.GetType().Name);
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            logHandler.LogFormat(logType, null!, format, args);
        }

        public void LogException(Exception exception)
        {
            L.LogError(exception);
        }
        public bool logEnabled { get; set; } = true;
        public LogType filterLogType { get; set; }
        public ILogHandler logHandler { get; set; } = new UCUnityLogHandler();

        private class UCUnityLogHandler : ILogHandler
        {
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                string? str;
                try
                {
                    str = string.Format(format, args);
                }
                catch (FormatException)
                {
                    str = format + " - " + string.Join("|", args);
                }
                OnUnityLogMessage(str, context.name + " - " + context.GetType().Name, logType);
            }
            public void LogException(Exception exception, Object context)
            {
                L.LogError("Exception in " + context.name + " - " + context.GetType());
                L.LogError(exception);
            }
        }
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