using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public delegate void NetLog(string message, ConsoleColor color);
    public delegate void NetLogEx(Exception ex, ConsoleColor color);
    /// <summary>Subscribe to these events to catch logging messages.</summary>
    public static class Logging
    {
        public static event NetLog OnLog;
        public static event NetLog OnLogWarning;
        public static event NetLog OnLogError;
        public static event NetLogEx OnLogException;

        internal static void Log(string message, ConsoleColor color = ConsoleColor.DarkGray) => OnLog?.Invoke(message, color);
        internal static void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow) => OnLogWarning?.Invoke(message, color);
        internal static void LogError(string message, ConsoleColor color = ConsoleColor.Red) => OnLogError?.Invoke(message, color);
        internal static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red) => OnLogException?.Invoke(ex, color);
    }
}
