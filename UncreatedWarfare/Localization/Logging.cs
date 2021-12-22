using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare
{
    public static class L
    {
        private static void AddLine(string text, ConsoleColor color)
        {
            try
            {
                if (Data.AppendConsoleMethod != null && Data.defaultIOHandler != null)
                {
                    Data.AppendConsoleMethod.Invoke(Data.defaultIOHandler, new object[] { text, color });
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
        public static void LogDebug(string info, ConsoleColor color = ConsoleColor.DarkGray)
        {
            if (UCWarfare.Config.Debug)
                Log(info, color);
        }
        public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Gray || Data.AppendConsoleMethod == default)
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
        public static void LogWarning(string warning, ConsoleColor color = ConsoleColor.Yellow)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Yellow || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogWarning(warning);
                }
                else
                {
                    AddLine(warning, color);
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
        public static void LogError(string error, ConsoleColor color = ConsoleColor.Red)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogError(error);
                }
                else
                {
                    AddLine(error, color);
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
        public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            string message = $"EXCEPTION - {ex.GetType().Name}\n\n{ex.Message}\n{ex.StackTrace}\n\nFINISHED";
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogError(message);
                }
                else
                {
                    AddLine(message, color);
                    UnturnedLog.warn($"[EX] {ex.Message}");
                    UnturnedLog.warn($"[ST] {ex.StackTrace}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = message, RCON = true, Severity = Rocket.Core.Logging.ELogType.Exception });
                }
            }
            catch (Exception ex2)
            {
                CommandWindow.LogError($"{message}\nEXCEPTION LOGGING \n\n{ex2.Message}\n{ex2.StackTrace}\n\nFINISHED");
            }
        }
    }
}
