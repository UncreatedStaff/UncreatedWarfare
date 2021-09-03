using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

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

        public void Execute(IRocketPlayer caller, string[] command)
        {
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
                                {
                                    Data.DatabaseManager.AddWarning(player.playerID.steamID.m_SteamID, 0, reason);
                                    Invocations.Shared.LogWarned.NetInvoke(player.playerID.steamID.m_SteamID, 0UL, reason, DateTime.Now);
                                }
                                F.SendChat(player.playerID.steamID, "warn_warned_private_operator", reason);
                                ToastMessage.QueueMessage(player, F.Translate("warn_warned_private_operator", player, out _, reason), ToastMessageSeverity.WARNING);
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
                                {
                                    Data.DatabaseManager.AddWarning(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason);
                                    Invocations.Shared.LogWarned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, DateTime.Now);
                                }
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
        public static void WarnPlayer(ulong Violator, ulong Admin, string Reason)
        {
            SteamPlayer violator = PlayerTool.getSteamPlayer(Violator);
            SteamPlayer admin = PlayerTool.getSteamPlayer(Admin);
            FPlayerName callerName;
            if (admin == null)
                callerName = Data.DatabaseManager.GetUsernames(Admin);
            else
                callerName = F.GetPlayerOriginalNames(admin);
            FPlayerName names;
            if (violator == null)
                names = Data.DatabaseManager.GetUsernames(Violator);
            else
                names = F.GetPlayerOriginalNames(admin);
            if (violator != null)
            {
                F.Log(F.Translate("warn_warned_console" + (admin == null ? "_operator" : string.Empty), 0, out _, names.PlayerName,
                Violator.ToString(), callerName.PlayerName,
                Admin.ToString(), Reason),
                ConsoleColor.Cyan);
                if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
                {
                    Data.DatabaseManager.AddWarning(Violator, Admin, Reason);
                    Invocations.Shared.LogWarned.NetInvoke(Violator, Admin, Reason, DateTime.Now);
                }
                if (admin != null)
                    F.SendChat(admin, "warn_warned_feedback", names.CharacterName);
                ToastMessage.QueueMessage(violator,
                    F.Translate("warn_warned_private" + (admin == null ? "_operator" : string.Empty), Admin, out _, callerName.CharacterName, Reason),
                    ToastMessageSeverity.WARNING);
                F.SendChat(violator, "warn_warned_private" + (admin == null ? "_operator" : string.Empty), callerName.CharacterName, Reason);
                F.BroadcastToAllExcept(new List<CSteamID> { violator.playerID.steamID, admin == null ? new CSteamID(Admin) : admin.playerID.steamID },
                    "warn_warned_broadcast" + (admin == null ? "_operator" : string.Empty), names.CharacterName, callerName.CharacterName);
            }
        }
    }
}