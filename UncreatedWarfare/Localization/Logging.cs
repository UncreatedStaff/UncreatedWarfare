using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.VanillaRework;

namespace Uncreated.Warfare;

public static class L
{
    //public const int MAX_LOGS = 1000;
    //internal static List<LogMessage> Logs;
    private static bool _init = false;
    private static int indention = 0;
    private static FileStream _log;
    private static bool inL = false;
    private static void Init()
    {
        if (_init) return;
        _init = true;
        if (File.Exists(Data.Paths.CurrentLog))
        {
            string n = Path.Combine(Data.Paths.Logs, File.GetCreationTime(Data.Paths.CurrentLog).ToString(ActionLogger.DATE_HEADER_FORMAT) + ".txt");
            if (File.Exists(n))
                File.Delete(n);
            File.Move(Data.Paths.CurrentLog, n);
        }
        _log = new FileStream(Data.Paths.CurrentLog, FileMode.Create, FileAccess.Write, FileShare.Read);
        Patches.Patcher.Patch(typeof(Logs).GetMethod(nameof(Logs.printLine)),
            prefix: new HarmonyMethod(typeof(L).GetMethod(nameof(PrintLinePatch),
                BindingFlags.Static | BindingFlags.NonPublic)));
    }
    private static void PrintLinePatch(string message)
    {
        if (!inL)
        {
            if (Data.OutputToConsoleMethod is not null)
                AddLog(message);
            CommandHandler.OnLog(message);
            if (message.StartsWith("Detected newer game version: ", StringComparison.Ordinal))
            {
                ShutdownCommand.ShutdownAfterGame("Unturned update v" + message.Substring(29), false);
                throw new Exception("Why Nelson (auto-update stopper)");
            }
        }
    }

    /// <summary>Indents the log by <paramref name="amount"/> spaces until the returned <see cref="IDisposable"/> is disposed of. Doesn't apply to <see cref="LogError(Exception, ConsoleColor, string, string, int)"/></summary>
    /// <remarks><code>using LogIndent log = IndentLog(2);</code></remarks>
    public static IDisposable IndentLog(uint amount) => new LogIndent(amount);
    private struct LogIndent : IDisposable
    {
        public uint Indent;
        public LogIndent(uint amount)
        {
            Indent = amount;
            indention += (int)amount;
        }
        public void Dispose()
        {
            if (indention < Indent)
                indention = 0;
            else
                indention -= (int)Indent;
        }
    }

    private static void AddLine(string text, ConsoleColor color)
    {
        if (!_init) Init();
        if (indention == 0)
        {
            Data.OutputToConsoleMethod!.Invoke(text, color);
            AddLog(text);
        }
        else if (text.IndexOf('\n') < 1)
        {
            AddLog(text = new string(' ', indention) + text);
            Data.OutputToConsoleMethod!.Invoke(text, color);
        }
        else
        {
            string[] lines = text.Split(splitChars);
            string ind = new string(' ', indention);
            string l;
            for (int i = 0; i < lines.Length; ++i)
            {
                l = ind + lines[i].Trim(trimChars);
                lock (_log)
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(l + Environment.NewLine);
                    _log.Write(bytes, 0, bytes.Length);
                }
                Data.OutputToConsoleMethod!.Invoke(l, color);
            }
            _log.Flush();
        }
    }
    [Conditional("DEBUG")]
    public static void LogDebug(string info, ConsoleColor color = ConsoleColor.DarkGray)
    {
        if (UCWarfare.Config.Debug)
            Log(info, color);
    }
    internal static void NetLogInfo(string message) => Log(message);
    internal static void NetLogWarning(string message) => LogWarning(message, method: "UncreatedNetworking");
    internal static void NetLogError(string message) => LogError(message, method: "UncreatedNetworking");
    internal static void NetLogException(Exception ex) => LogError(ex, method: "UncreatedNetworking", filepath: "unknown");
    public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[INFO]  " + info, color);
        else if (Data.OutputToConsoleMethod is null)
        {
            CommandWindow.Log(info);
        }
        else
        {
            AddLine("[INFO]  " + info, color);
            inL = true;
            UnturnedLog.info($"[IN] {info}");
            inL = false;
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
        else if (Data.OutputToConsoleMethod is null)
        {
            CommandWindow.LogWarning(warning);
        }
        else
        {
            AddLine("[WARN]  [" + method.ToUpper() + "] " + warning, color);
            inL = true;
            UnturnedLog.warn($"[WA] {warning}");
            inL = false;
        }
    }
    public static void LogError(string error, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "")
    {
        if (!UCWarfare.IsLoaded)
            LogAsLibrary("[ERROR] [" + method.ToUpper() + "] " + error, color);
        else if (Data.OutputToConsoleMethod is null)
        {
            CommandWindow.LogError(error);
        }
        else
        {
            AddLine("[ERROR] [" + method.ToUpper() + "] " + error, color);
            inL = true;
            UnturnedLog.warn($"[ER] {error}");
            inL = false;
        }
    }

    private static readonly char[] trimChars = new char[] { '\n', '\r' };
    private static readonly char[] splitChars = new char[] { '\n' };
    private static readonly List<string> stack = new List<string>(64);
    private static readonly StringBuilder _errorBuilder = new StringBuilder(512);
    private static void CleanStackTrace(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return;
        string[] stacks = stackTrace.Split(splitChars);
        bool isAsync = false;
        lock (stack)
        {
            for (int i = stacks.Length - 1; i >= 0; --i)
            {
                string stack = stacks[i].Trim(trimChars);
                if (stack.StartsWith("  at System.Runtime.CompilerServices", StringComparison.Ordinal) ||
                    stack.StartsWith("  at System.Threading.Tasks.Task", StringComparison.Ordinal) ||
                    stack.StartsWith("  at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw ()", StringComparison.Ordinal))
                {
                    isAsync = true;
                }
                else
                {
                    if (stack.Equals("--- End of stack trace from previous location where exception was thrown ---"))
                    {
                        if (isAsync) continue;
                        else
                            stack = Environment.NewLine + "  \\/ Previous Stack Location \\/" + Environment.NewLine;
                    }
                    if (stack.StartsWith("  at "))
                    {
                        int endingPartSt;
                        int endingPartEnd;
                        int nestedClassSt = stack.IndexOf('+');
                        if (nestedClassSt != -1 && stack.Length > nestedClassSt + 3)
                        {
                            string owner = stack.Substring(5, nestedClassSt - 5);
                            if (stack[nestedClassSt + 1] == '<')
                            {
                                int close = stack.IndexOf('>', nestedClassSt + 2);
                                string methodName = stack.Substring(nestedClassSt + 2, close - nestedClassSt - 2);
                                endingPartSt = stack.IndexOf('(', close);
                                if (endingPartSt != -1)
                                {
                                    endingPartEnd = stack.IndexOf("[0x", endingPartSt, StringComparison.OrdinalIgnoreCase);
                                    if (endingPartEnd == -1)
                                        endingPartEnd = stack.Length - 1;
                                    stack = "  at async " + owner + "." + methodName + " " + stack.Substring(endingPartSt, endingPartEnd - endingPartSt - 1);
                                }
                                else
                                    stack = "  at async " + owner + "." + methodName;
                                goto repl;
                            }
                        }
                        endingPartEnd = stack.IndexOf("[0x", StringComparison.OrdinalIgnoreCase);
                        if (endingPartEnd != -1)
                        {
                            if (endingPartEnd == -1)
                                endingPartEnd = stack.Length - 1;
                            stack = stack.Substring(0, endingPartEnd - 1);
                        }
                    }
                repl:
                    stack = stack
                        .Replace("System.Boolean", "bool")
                        .Replace("System.UInt32", "uint")
                        .Replace("System.Int32", "int")
                        .Replace("System.UInt16", "ushort")
                        .Replace("System.Int16", "short")
                        .Replace("System.UInt64", "ulong")
                        .Replace("System.Int64", "long")
                        .Replace("System.String", "string")
                        .Replace("System.Object", "object");

                    L.stack.Add(stack);
                }
            }

            _errorBuilder.Append(Environment.NewLine);
            for (int i = stack.Count - 1; i >= 0; --i)
                _errorBuilder.AppendLine(stack[i]);
            stack.Clear();

        }
        if (isAsync)
            _errorBuilder.AppendLine("== SOME LINES WERE HIDDEN FOR READABILITY ==");
    }
    public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "", [CallerFilePath] string filepath = "", [CallerLineNumber] int ln = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int i = 0;
        do
        {
            if (i != 0)
                _errorBuilder.Append(Environment.NewLine);
            _errorBuilder
                .Append("EXCEPTION - ")
                .Append(ex.GetType().Name)
                .Append(Environment.NewLine)
                .Append("Source: ")
                .Append(filepath)
                .Append("::")
                .Append(method)
                .Append("( ... ) LN# ")
                .Append(ln.ToString(Data.Locale))
                .Append(Environment.NewLine)
                .Append(Environment.NewLine)
                .Append(ex.Message);
            CleanStackTrace(ex.StackTrace);
            _errorBuilder
                .Append(Environment.NewLine)
                .Append(Environment.NewLine)
                .Append("FINISHED")
                .Append(Environment.NewLine);
            if (ex is TypeLoadException t)
            {
                _errorBuilder.Append("Type: ").Append(t.TypeName);
            }
            else if (ex is ReflectionTypeLoadException t2)
            {
                foreach (Exception ex2 in t2.LoaderExceptions)
                    L.LogError(ex2, color, method, filepath, ln);
            }
            else if (ex is AggregateException t3)
            {
                _errorBuilder.Append(Environment.NewLine).Append("INNER EXCEPTIONS: ");
                int j = 0;
                foreach (Exception ex2 in t3.InnerExceptions)
                {
                    _errorBuilder.Append(" - INNER EXCEPTION #").Append((++j).ToString(Data.Locale));
                    LogError(ex2, color, method, filepath, ln);
                }
                break;
            }
            ++i;
            ex = ex.InnerException;
            if (ex != null)
                _errorBuilder.Append(Environment.NewLine).Append("INNER EXCEPTION");
            else break;
        }
        while (i < 8);

        string err = _errorBuilder.ToString();
        _errorBuilder.Clear();
        if (!UCWarfare.IsLoaded)
            LogAsLibrary(err, color);
        else if (Data.OutputToConsoleMethod is null)
        {
            CommandWindow.LogError(err);
        }
        else
        {
            Data.OutputToConsoleMethod.Invoke(err, color);
            AddLog(err);
        }
    }/*
    public static void AddLog(LogMessage log)
    {
        if (Logs.Count > MAX_LOGS)
        {
            Logs.RemoveRange(MAX_LOGS - 1, Logs.Count - MAX_LOGS + 1);
        }
        else if (Logs.Count == MAX_LOGS) Logs.RemoveAt(MAX_LOGS - 1);
        Logs.Insert(0, log);
    }*/
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
    internal static bool isRequestingLog = false;
    private static void AddLog(string log)
    {
        lock (_log)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(log + Environment.NewLine);
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
