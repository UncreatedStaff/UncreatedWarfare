using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Networking;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands
{
    class UnbanOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "unban";

        public string Help => "Unban players who have served their time.";

        public string Syntax => "/unban <player ID>";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string> { "uc.unban" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!Dedicator.isDedicated)
                return;
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    F.LogError(F.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length != 1)
                        F.LogError(F.Translate("unban_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            F.LogError(F.Translate("unban_no_player_found_console", 0, out _, command[0]));
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            F.LogError(F.Translate("unban_player_not_banned_console", 0, out _, command[0]));
                        else
                        {
                            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                                Client.LogPlayerUnbanned(steamplayer.m_SteamID, Provider.server.m_SteamID, DateTime.Now);
                            FPlayerName names = Data.DatabaseManager.GetUsernames(steamplayer.m_SteamID);
                            if (names.Steam64.ToString(Data.Locale) == names.PlayerName)
                            {
                                F.Log(F.Translate("unban_unbanned_console_id_operator", 0, out _, steamplayer.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                F.Broadcast("unban_unbanned_broadcast_id_operator", steamplayer.m_SteamID.ToString(Data.Locale));
                            } else
                            {
                                F.Log(F.Translate("unban_unbanned_console_name_operator", 0, out _, names.PlayerName, steamplayer.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                F.Broadcast("unban_unbanned_broadcast_name_operator", names.CharacterName);
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
                    if (command.Length != 1)
                        F.SendChat(player, "unban_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            F.SendChat(player, "unban_no_player_found", command[0]);
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            F.SendChat(player, "unban_player_not_banned", command[0]);
                        else
                        {
                            /*if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                                Client.LogPlayerUnbanned(steamplayer.m_SteamID, player.CSteamID.m_SteamID, DateTime.Now);*/
                            FPlayerName names = Data.DatabaseManager.GetUsernames(steamplayer.m_SteamID);
                            FPlayerName callerNames = F.GetPlayerOriginalNames(player.Player);
                            if (names.Steam64.ToString(Data.Locale) == names.PlayerName)
                            {
                                F.Log(F.Translate("unban_unbanned_console_id", 0, out _, steamplayer.m_SteamID.ToString(Data.Locale), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                F.SendChat(player, "unban_unbanned_feedback_id", steamplayer.m_SteamID.ToString(Data.Locale));
                                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_unbanned_broadcast_id", steamplayer.m_SteamID.ToString(Data.Locale), callerNames.CharacterName);
                            } else
                            {
                                F.Log(F.Translate("unban_unbanned_console_name", 0, out _, names.PlayerName, steamplayer.m_SteamID.ToString(Data.Locale), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                F.SendChat(player, "unban_unbanned_feedback_name", names.CharacterName);
                                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_unbanned_broadcast_name", names.CharacterName, callerNames.CharacterName);
                            }
                        }
                    }
                }
            }
        }
    }
}