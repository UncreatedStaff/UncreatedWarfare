using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Commands
{
    public class ShutdownOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "shutdown";
        public string Help => "does something";
        public string Syntax => "/shutdown <aftergame|cancel|*seconds*|instant> <reason (except cancel)>";
        public List<string> Aliases => new List<string>() { };
        public List<string> Permissions => new List<string>() { "uc.shutdown" };
        public static Coroutine Messager = null;
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller.DisplayName == "Console")
            {
                if (!Dedicator.isDedicated)
                {
                    F.LogError(F.Translate("shutdown_not_server", 0), ConsoleColor.Red);
                    return;
                }
                if (command.Length == 0)
                {
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    await Networking.Client.SendShuttingDown(0, "None specified.");
                    await rtn;
                    Provider.shutdown(0);
                    return;
                }
                string option = command[0].ToLower();
                if (command.Length < 2 && option != "cancel" && option != "abort")
                {
                    F.LogError(F.Translate("shutdown_syntax", 0), ConsoleColor.Red);
                    return;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < command.Length; i++)
                    sb.Append((i == 1 ? '\0' : ' ') + command[i]);
                string reason = sb.ToString();
                if (option == "instant" || option == "inst" || option == "now")
                {
                    await Networking.Client.SendShuttingDown(0, reason);
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    Provider.shutdown(0, reason);
                    await rtn;
                } else if (option == "aftergame" || option == "after" || option == "game")
                {
                    F.Broadcast("shutdown_broadcast_after_game", UCWarfare.GetColor("shutdown_broadcast_after_game"),
                        reason, UCWarfare.GetColorHex("shutdown_broadcast_after_game_reason"));
                    F.Log(F.Translate("shutdown_broadcast_after_game_console", 0, reason), ConsoleColor.Cyan);
                    Data.FlagManager.ShutdownAfterGame(reason, 0);
                } else if (option == "cancel" || option == "abort")
                {
                    Data.FlagManager.CancelShutdownAfterGame();
                    F.Broadcast("shutdown_broadcast_after_game_canceled", UCWarfare.GetColor("shutdown_broadcast_after_game_canceled"));
                    F.Log(F.Translate("shutdown_broadcast_after_game_canceled_console", 0), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                } else if (uint.TryParse(option, System.Globalization.NumberStyles.Any, Data.Locale, out uint seconds))
                {
                    string time = F.GetTimeFromSeconds(seconds);
                    F.Broadcast("shutdown_broadcast_after_time", UCWarfare.GetColor("shutdown_broadcast_after_time"),
                        time, UCWarfare.GetColorHex("shutdown_broadcast_after_time_reason"));
                    F.Log(F.Translate("shutdown_broadcast_after_time_console", 0, time, reason), ConsoleColor.Cyan);
                    await Networking.Client.SendShuttingDown(0, reason);
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    Provider.shutdown(unchecked((int)seconds), reason);
                    await rtn;
                } else
                {
                    F.LogError(F.Translate("shutdown_syntax", 0), ConsoleColor.Red);
                    return;
                }
            } else
            {
                SteamPlayer player = ((UnturnedPlayer)caller).Player.channel.owner;
                if (!Dedicator.isDedicated)
                {
                    player.SendChat("shutdown_not_server", UCWarfare.GetColor("defaulterror"));
                    return;
                }
                string option = command[0].ToLower();
                if (command.Length < 2 && option != "cancel" && option != "abort")
                {
                    player.SendChat("shutdown_syntax", UCWarfare.GetColor("defaulterror"));
                    return;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < command.Length; i++)
                    sb.Append((i == 1 ? '\0' : ' ') + command[i]);
                string reason = sb.ToString();
                if (option == "instant" || option == "inst" || option == "now")
                {
                    await Networking.Client.SendShuttingDown(player.playerID.steamID.m_SteamID, reason);
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    Provider.shutdown(0, reason);
                    await rtn;
                }
                else if (option == "aftergame" || option == "after" || option == "game")
                {
                    Data.FlagManager.ShutdownAfterGame(reason, player.playerID.steamID.m_SteamID);
                    F.Broadcast("shutdown_broadcast_after_game", UCWarfare.GetColor("shutdown_broadcast_after_game"),
                        reason, UCWarfare.GetColorHex("shutdown_broadcast_after_game_reason"));
                    F.Log(F.Translate("shutdown_broadcast_after_game_console_player", 0, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                }
                else if (option == "cancel" || option == "abort")
                {
                    Data.FlagManager.CancelShutdownAfterGame();
                    F.Broadcast("shutdown_broadcast_after_game_canceled", UCWarfare.GetColor("shutdown_broadcast_after_game_canceled"));
                    F.Log(F.Translate("shutdown_broadcast_after_game_canceled_console_player", 0, F.GetPlayerOriginalNames(player).PlayerName), ConsoleColor.Cyan);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                }
                else if (uint.TryParse(option, System.Globalization.NumberStyles.Any, Data.Locale, out uint seconds))
                {
                    string time = F.GetTimeFromSeconds(seconds);
                    F.Broadcast("shutdown_broadcast_after_time", UCWarfare.GetColor("shutdown_broadcast_after_time"),
                        time, UCWarfare.GetColorHex("shutdown_broadcast_after_time_reason"));
                    F.Log(F.Translate("shutdown_broadcast_after_time_console_player", 0, time, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
                    await Networking.Client.SendShuttingDown(player.playerID.steamID.m_SteamID, reason);
                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
                    Provider.shutdown(unchecked((int)seconds), reason);
                    await rtn;
                }
                else
                {
                    player.SendChat("shutdown_syntax", UCWarfare.GetColor("defaulterror"));
                    return;
                }
            }
        }
        public static IEnumerator<WaitForSeconds> ShutdownMessageSender(string reason)
        {
            if (UCWarfare.Config.AdminLoggerSettings.TimeBetweenShutdownMessages == 0) yield break;
            yield return new WaitForSeconds(UCWarfare.Config.AdminLoggerSettings.TimeBetweenShutdownMessages);
            foreach (SteamPlayer player in Provider.clients)
                player.SendChat("shutdown_broadcast_after_game_reminder", UCWarfare.GetColor("shutdown_broadcast_after_game_reminder"), 
                    reason, UCWarfare.GetColorHex("shutdown_broadcast_after_game_reminder_reason"));
            Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
        }
    }
}
