using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Networking;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class ShutdownOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shutdown";
        public string Help => "does something";
        public string Syntax => "/shutdown <aftergame|cancel|*seconds*|instant> <reason (except cancel)>";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.shutdown" };
        public static Coroutine? Messager = null;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller is ConsolePlayer)
            {
                if (command.Length == 0)
                {
                    ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, $"INSTANT");
                    Invocations.Shared.ShuttingDown.NetInvoke(0UL, "None specified.");
                    Provider.shutdown(0);
                    return;
                }
                string option = command[0].ToLower();
                if (command.Length < 2 && option != "cancel" && option != "abort")
                {
                    L.LogError(Translation.Translate("shutdown_syntax", 0), ConsoleColor.Red);
                    return;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < command.Length; i++)
                {
                    if (i != 1) sb.Append(' ');
                    sb.Append(command[i]);
                }
                string reason = sb.ToString();
                if (option == "instant" || option == "inst" || option == "now")
                {
                    ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, $"INSTANT: " + reason);
                    Invocations.Shared.ShuttingDown.NetInvoke(0UL, reason);
                    Provider.shutdown(0, reason);
                }
                else if (option == "aftergame" || option == "after" || option == "game")
                {
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.Locale)) + ": " + reason);
                    Chat.Broadcast("shutdown_broadcast_after_game", reason);
                    L.Log(Translation.Translate("shutdown_broadcast_after_game_console", 0, out _, reason), ConsoleColor.Cyan);
                    Data.Gamemode?.ShutdownAfterGame(reason, 0);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                    Invocations.Shared.ShuttingDownAfter.NetInvoke(0UL, reason);
                }
                else if (option == "cancel" || option == "abort")
                {
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"CANCELLED");
                    Data.Gamemode.CancelShutdownAfterGame();
                    Chat.Broadcast("shutdown_broadcast_after_game_canceled");
                    L.Log(Translation.Translate("shutdown_broadcast_after_game_canceled_console", 0, out _), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Invocations.Shared.ShuttingDownCancel.NetInvoke(0UL);
                }
                else if (uint.TryParse(option, System.Globalization.NumberStyles.Any, Data.Locale, out uint seconds))
                {
                    string time;
                    foreach (SteamPlayer player in Provider.clients)
                    {
                        time = seconds.GetTimeFromSeconds(player.playerID.steamID.m_SteamID);
                        player.SendChat("shutdown_broadcast_after_time", time, reason);
                    }
                    time = seconds.GetTimeFromSeconds(0);
                    L.Log(Translation.Translate("shutdown_broadcast_after_time_console", 0, out _, time, reason), ConsoleColor.Cyan);
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason);
                    Invocations.Shared.ShuttingDownTime.NetInvoke(0UL, reason, seconds);
                    Provider.shutdown(unchecked((int)seconds), reason);
                }
                else
                {
                    L.LogError(Translation.Translate("shutdown_syntax", 0), ConsoleColor.Red);
                    return;
                }
            }
            else
            {
                SteamPlayer player = ((UnturnedPlayer)caller).Player.channel.owner;
                if (command.Length == 0)
                {
                    ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, $"INSTANT", player.playerID.steamID.m_SteamID);
                    Invocations.Shared.ShuttingDown.NetInvoke(0UL, "None specified.");
                    Provider.shutdown(0);
                    return;
                }
                string option = command[0].ToLower();
                if (command.Length < 2 && option != "cancel" && option != "abort")
                {
                    player.SendChat("shutdown_syntax");
                    return;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < command.Length; i++)
                {
                    if (i != 1) sb.Append(' ');
                    sb.Append(command[i]);
                }
                string reason = sb.ToString();
                if (option == "instant" || option == "inst" || option == "now")
                {
                    ActionLog.AddPriority(EActionLogType.SHUTDOWN_SERVER, $"INSTANT: " + reason, player.playerID.steamID.m_SteamID);
                    Invocations.Shared.ShuttingDown.NetInvoke(0UL, reason);
                    Provider.shutdown(0, reason);
                }
                else if (option == "aftergame" || option == "after" || option == "game")
                {
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"AFTER GAME " + (Data.Gamemode == null ? "null" : Data.Gamemode.GameID.ToString(Data.Locale)) + ": " + reason, player.playerID.steamID.m_SteamID);
                    Data.Gamemode?.ShutdownAfterGame(reason, player.playerID.steamID.m_SteamID);
                    Chat.Broadcast("shutdown_broadcast_after_game", reason);
                    L.Log(Translation.Translate("shutdown_broadcast_after_game_console_player", 0, out _, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                    Invocations.Shared.ShuttingDownAfter.NetInvoke(0UL, reason);
                }
                else if (option == "cancel" || option == "abort")
                {
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"CANCELLED", player.playerID.steamID.m_SteamID);
                    Data.Gamemode.CancelShutdownAfterGame();
                    Chat.Broadcast("shutdown_broadcast_after_game_canceled");
                    L.Log(Translation.Translate("shutdown_broadcast_after_game_canceled_console_player", 0, out _, F.GetPlayerOriginalNames(player).PlayerName), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Invocations.Shared.ShuttingDownCancel.NetInvoke(0UL);
                }
                else if (uint.TryParse(option, System.Globalization.NumberStyles.Any, Data.Locale, out uint seconds))
                {
                    string time;
                    foreach (SteamPlayer pl in Provider.clients)
                    {
                        time = seconds.GetTimeFromSeconds(pl.playerID.steamID.m_SteamID);
                        pl.SendChat("shutdown_broadcast_after_time", time, reason);
                    }
                    time = seconds.GetTimeFromSeconds(0);
                    L.Log(Translation.Translate("shutdown_broadcast_after_time_console_player", 0, out _, time, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
                    ActionLog.Add(EActionLogType.SHUTDOWN_SERVER, $"IN " + time.ToUpper() + ": " + reason, player.playerID.steamID.m_SteamID);
                    Invocations.Shared.ShuttingDownTime.NetInvoke(0UL, reason, seconds);
                    Provider.shutdown(unchecked((int)seconds), reason);
                }
                else
                {
                    player.SendChat("shutdown_syntax");
                    return;
                }
            }
        }
        public static IEnumerator<WaitForSeconds> ShutdownMessageSender(string reason)
        {
            if (UCWarfare.Config.AdminLoggerSettings.TimeBetweenShutdownMessages == 0) yield break;
            while (true)
            {
                yield return new WaitForSeconds(UCWarfare.Config.AdminLoggerSettings.TimeBetweenShutdownMessages);
                foreach (SteamPlayer player in Provider.clients)
                    player.SendChat("shutdown_broadcast_after_game_reminder", reason);
            }
        }
    }
}
