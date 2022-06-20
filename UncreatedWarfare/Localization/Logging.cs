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

namespace Uncreated.Warfare;

public static class L
{
    public const int MAX_LOGS = 1000;
    internal static List<LogMessage> Logs;
    private static void AddLine(string text, ConsoleColor color)
    {
        try
        {
            if (Data.OutputToConsoleMethod != null && Data.defaultIOHandler != null)
            {
                Data.OutputToConsoleMethod.Invoke(text, color);
            }
        }
        catch
        {
            switch (color)
            {
                case ConsoleColor.Gray:
                default:
                    CommandWindow.Log(text);
                    break;
                case ConsoleColor.Yellow:
                    CommandWindow.LogWarning(text);
                    break;
                case ConsoleColor.Red:
                    CommandWindow.LogError(text);
                    break;
            }
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
        try
        {
            if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Gray || Data.OutputToConsoleMethod == null)
            {
                CommandWindow.Log(info);
            }
            else
            {
                AddLine(info, color);
                UnturnedLog.info($"[IN] {info}");
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = info, RCON = true, Severity = Rocket.Core.Logging.ELogType.Info });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.Log(info);
            LogError(ex);
        }
    }
    public static void LogWarning(string warning, ConsoleColor color = ConsoleColor.Yellow, [CallerMemberName] string method = "")
    {
        try
        {
            if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Yellow || Data.OutputToConsoleMethod == null)
            {
                CommandWindow.LogWarning(warning);
            }
            else
            {
                AddLine("[" + method.ToUpper() + "] " + warning, color);
                UnturnedLog.warn($"[WA] {warning}");
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = warning, RCON = true, Severity = Rocket.Core.Logging.ELogType.Warning });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogWarning(warning);
            LogError(ex);
        }
    }
    public static void LogError(string error, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "")
    {
        try
        {
            if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || Data.OutputToConsoleMethod == null)
            {
                CommandWindow.LogError(error);
            }
            else
            {
                AddLine("[" + method.ToUpper() + "] " + error, color);
                UnturnedLog.warn($"[ER] {error}");
                Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = error, RCON = true, Severity = Rocket.Core.Logging.ELogType.Error });
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError(error);
            UnturnedLog.error(ex);
        }
    }

    private static readonly char[] trimChars = new char[] { '\n', '\r' };
    private static readonly List<string> stack = new List<string>(64);
    private static string CleanStackTrace(string stackTrace)
    {
        string[] stacks = stackTrace.Split('\n');
        StringBuilder newTrace = new StringBuilder(stackTrace.Length);
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
                                    endingPartEnd = stack.IndexOf('[', endingPartSt);
                                    if (endingPartEnd == -1)
                                        endingPartEnd = stack.Length - 1;
                                    stack = "  at async " + owner + "." + methodName + " " + stack.Substring(endingPartSt, endingPartEnd - endingPartSt - 1);
                                }
                                else
                                    stack = "  at async " + owner + "." + methodName;
                                goto repl;
                            }
                        }
                        endingPartEnd = stack.IndexOf('[');
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

            for (int i = stack.Count - 1; i >= 0; --i)
                newTrace.AppendLine(stack[i]);
            stack.Clear();

        }
        if (isAsync)
                newTrace.AppendLine("== SOME LINES WERE HIDDEN FOR READABILITY ==");

        return newTrace.ToString();
    }
    public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red, [CallerMemberName] string method = "", [CallerFilePath] string filepath = "", [CallerLineNumber] int ln = 0)
    {
        int i = 0;
        do
        {
            string message = $"EXCEPTION - {ex.GetType().Name}\nSource: {filepath}::{method}( ... ) LN# {ln}\n\n{ex.Message}\n{CleanStackTrace(ex.StackTrace)}\n\nFINISHED";
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red ||
                    Data.OutputToConsoleMethod == null)
                {
                    CommandWindow.LogError(message);
                }
                else
                {
                    AddLine(message, color);
                    UnturnedLog.warn($"[EX] {ex.Message}");
                    UnturnedLog.warn($"[ST] {ex.StackTrace}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry()
                        { Message = message, RCON = true, Severity = Rocket.Core.Logging.ELogType.Exception });
                }
            }
            catch (Exception ex2)
            {
                CommandWindow.LogError($"{message}\nEXCEPTION LOGGING \n\n{ex2.Message}\n{ex2.StackTrace}\n\nFINISHED");
            }

            if (ex is TypeLoadException t)
            {
                L.LogError("Type: " + t.TypeName);
            }
            else if (ex is ReflectionTypeLoadException t2)
            {
                foreach (Exception ex2 in t2.LoaderExceptions)
                    L.LogError(ex2, color, method, filepath, ln);
            }
            else if (ex is AggregateException t3)
            {
                L.LogError("INNER EXCEPTIONS: ");
                int j = 0;
                foreach (Exception ex2 in t3.InnerExceptions)
                {
                    LogError(" - INNER EXCEPTION #" + ++j);
                    LogError(ex2, color, method, filepath, ln);
                }
                break;
            }
            ++i;
            ex = ex.InnerException;
            if (ex != null)
                LogError("INNER EXCEPTION");
            else break;
        }
        while (i < 8);
    }
    public static List<LogMessage> ReadRocketLog()
    {
        List<LogMessage> logs = new List<LogMessage>();
        string path = Path.Combine(Rocket.Core.Environment.LogsDirectory, Rocket.Core.Environment.LogFile);
        if (!File.Exists(path))
            return logs;
        using (FileStream str = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            byte[] bytes = new byte[str.Length];
            str.Read(bytes, 0, bytes.Length);
            string file = System.Text.Encoding.UTF8.GetString(bytes);
            string[] lines = file.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (logs.Count >= MAX_LOGS)
                {
                    logs.RemoveRange(MAX_LOGS - 1, logs.Count - MAX_LOGS - 1);
                }
                logs.Insert(0, new LogMessage(lines[i]));
            }
        }
        return logs;
    }
    public static void AddLog(LogMessage log)
    {
        if (Logs.Count > MAX_LOGS)
        {
            Logs.RemoveRange(MAX_LOGS - 1, Logs.Count - MAX_LOGS + 1);
        }
        else if (Logs.Count == MAX_LOGS) Logs.RemoveAt(MAX_LOGS - 1);
        Logs.Insert(0, log);
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
    internal static bool isRequestingLog = false;
    public static class NetCalls
    {
        public static readonly NetCall RequestFullLog = new NetCall(ReceiveRequestFullLog);
        public static readonly NetCall<string> RequestRunCommand = new NetCall<string>(ReceiveCommand);
        public static readonly NetCall<bool> SetRequestLogState = new NetCall<bool>(ReceiveRequestsLogState);

        public static readonly NetCallRaw<LogMessage, byte> SendLogMessage = new NetCallRaw<LogMessage, byte>(1030, LogMessage.Read, null, LogMessage.Write, null);
        public static readonly NetCallRaw<LogMessage[], byte> SendFullLog = new NetCallRaw<LogMessage[], byte>(1031, LogMessage.ReadMany, null, LogMessage.WriteMany, null);
        public static readonly NetCall<string> SendFatalException = new NetCall<string>(1131);

        [NetCall(ENetCall.FROM_SERVER, 1029)]
        internal static void ReceiveRequestFullLog(MessageContext context) => context.Reply(SendFullLog, Logs.ToArray(), (byte)0);
        [NetCall(ENetCall.FROM_SERVER, 1032)]
        internal static void ReceiveCommand(MessageContext context, string command)
        {
            if (UCWarfare.IsMainThread)
                RunCommand(command);
            else
                UCWarfare.RunOnMainThread(() => RunCommand(command));
        }
        [NetCall(ENetCall.FROM_SERVER, 1023)]
        private static void ReceiveRequestsLogState(MessageContext context, bool state)
        {
            isRequestingLog = state;
        }
    }
}
