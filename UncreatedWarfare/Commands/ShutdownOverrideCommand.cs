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
                    F.LogError(F.Translate("shutdown_not_server", 0, out _), ConsoleColor.Red);
                    return;
                }
                if (command.Length == 0)
                {
                    await Networking.Client.SendShuttingDown(0, "None specified.");
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
                {
                    if (i != 1) sb.Append(' ');
                    sb.Append(command[i]);
                }
                string reason = sb.ToString();
                if (option == "instant" || option == "inst" || option == "now")
                {
                    await Networking.Client.SendShuttingDown(0, reason);
                    Provider.shutdown(0, reason);
                } else if (option == "aftergame" || option == "after" || option == "game")
                {
                    F.Broadcast("shutdown_broadcast_after_game", reason);
                    F.Log(F.Translate("shutdown_broadcast_after_game_console", 0, out _, reason), ConsoleColor.Cyan);
                    Data.Gamemode.ShutdownAfterGame(reason, 0);
                    if (Messager != null)
                    {
                        try
                        {
                            UCWarfare.I.StopCoroutine(Messager);
                        }
                        catch { }
                    }
                    Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
                } else if (option == "cancel" || option == "abort")
                {
                    Data.Gamemode.CancelShutdownAfterGame();
                    F.Broadcast("shutdown_broadcast_after_game_canceled");
                    F.Log(F.Translate("shutdown_broadcast_after_game_canceled_console", 0, out _), ConsoleColor.Cyan);
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
                    F.Broadcast("shutdown_broadcast_after_time", time, reason);
                    F.Log(F.Translate("shutdown_broadcast_after_time_console", 0, out _, time, reason), ConsoleColor.Cyan);
                    await Networking.Client.SendShuttingDown(0, reason);
                    Provider.shutdown(unchecked((int)seconds), reason);
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
                    player.SendChat("shutdown_not_server");
                    return;
                }
                if (command.Length == 0)
                {
                    await Networking.Client.SendShuttingDown(0, "None specified.");
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
                    await Networking.Client.SendShuttingDown(player.playerID.steamID.m_SteamID, reason);
                    Provider.shutdown(0, reason);
                }
                else if (option == "aftergame" || option == "after" || option == "game")
                {
                    Data.Gamemode.ShutdownAfterGame(reason, player.playerID.steamID.m_SteamID);
                    F.Broadcast("shutdown_broadcast_after_game", reason);
                    F.Log(F.Translate("shutdown_broadcast_after_game_console_player", 0, out _, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
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
                    Data.Gamemode.CancelShutdownAfterGame();
                    F.Broadcast("shutdown_broadcast_after_game_canceled");
                    F.Log(F.Translate("shutdown_broadcast_after_game_canceled_console_player", 0, out _, F.GetPlayerOriginalNames(player).PlayerName), ConsoleColor.Cyan);
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
                    F.Broadcast("shutdown_broadcast_after_time", time, reason);
                    F.Log(F.Translate("shutdown_broadcast_after_time_console_player", 0, out _, time, F.GetPlayerOriginalNames(player).PlayerName, reason), ConsoleColor.Cyan);
                    await Networking.Client.SendShuttingDown(player.playerID.steamID.m_SteamID, reason);
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
            yield return new WaitForSeconds(UCWarfare.Config.AdminLoggerSettings.TimeBetweenShutdownMessages);
            foreach (SteamPlayer player in Provider.clients)
                player.SendChat("shutdown_broadcast_after_game_reminder", reason);
            Messager = UCWarfare.I.StartCoroutine(ShutdownMessageSender(reason));
        }
    }
}
