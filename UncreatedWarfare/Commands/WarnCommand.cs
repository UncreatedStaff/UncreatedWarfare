using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands
{
    public class WarnCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "warn";

        public string Help => "Warn players who are misbehaving.";

        public string Syntax => "/warn <player> <reason>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.warn" };

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            if (!Dedicator.isDedicated)
                return;
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    F.LogError(F.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length < 1)
                        F.LogError(F.Translate("warn_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer player))
                            F.LogError(F.Translate("warn_no_player_found_console", 0, out _, command[0]));
                        else
                        {
                            if (command.Length == 1)
                                F.LogError(F.Translate("warn_no_reason_provided", 0, out _));
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(player);
                                F.Log(F.Translate("warn_warned_console_operator", 0, out _, 
                                    name.PlayerName, player.playerID.steamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                    await Client.LogPlayerWarned(player.playerID.steamID.m_SteamID, Provider.server.m_SteamID, reason, DateTime.Now);
                                F.SendChat(player.playerID.steamID, "warn_warned_private_operator", reason);
                                ToastMessage.QueueMessage(player, F.Translate("warn_warned_private_operator", player, out _, reason),  ToastMessageSeverity.WARNING);
                                F.BroadcastToAllExcept(new List<CSteamID> { player.playerID.steamID }, "warn_warned_broadcast_operator", name.CharacterName);
                            }
                        }
                    }
                }
            }
            else
            {
                UnturnedPlayer player = caller as UnturnedPlayer;
                if (!Provider.isServer)
                    F.SendChat(player, "server_not_running");
                else
                {
                    if (command.Length < 1)
                        F.SendChat(player, "warn_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                            F.SendChat(player, "warn_no_player_found", command[0]);
                        else
                        {
                            if (command.Length == 1)
                                F.SendChat(player, "warn_no_reason_provided");
                            else if (command.Length > 1)
                            {
                                string reason = command.MakeRemainder(1);
                                FPlayerName name = F.GetPlayerOriginalNames(steamplayer);
                                FPlayerName callerName = F.GetPlayerOriginalNames(player.Player);
                                F.Log(F.Translate("warn_warned_console", 0, out _, name.PlayerName, 
                                    steamplayer.playerID.steamID.m_SteamID.ToString(), callerName.PlayerName, 
                                    player.CSteamID.m_SteamID.ToString(), reason), 
                                    ConsoleColor.Cyan);
                                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                                    await Client.LogPlayerWarned(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, DateTime.Now);
                                F.SendChat(player, "warn_warned_feedback", name.CharacterName);
                                ToastMessage.QueueMessage(steamplayer, 
                                    F.Translate("warn_warned_private", player, out _, callerName.CharacterName, reason), 
                                    ToastMessageSeverity.WARNING);
                                F.SendChat(steamplayer.playerID.steamID, "warn_warned_private", callerName.CharacterName, reason);
                                F.BroadcastToAllExcept(new List<CSteamID> { steamplayer.playerID.steamID, player.CSteamID },
                                    "warn_warned_broadcast", name.CharacterName, callerName.CharacterName);
                            }
                        }
                    }
                }
            }
        }
    }
}