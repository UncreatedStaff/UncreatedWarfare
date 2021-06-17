using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
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
                    F.LogError(F.Translate("NotRunningErrorText", 0));
                else
                {
                    if (command.Length != 1)
                        F.LogError(F.Translate("InvalidParameterErrorText", 0));
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            F.LogError(F.Translate("unban_NoPlayerErrorText_Console", 0, command[0]));
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            F.LogError(F.Translate("unban_PlayerIsNotBanned_Console", 0, command[0]));
                        else
                        {
                            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                                Server.LogPlayerUnbanned(steamplayer.m_SteamID, Provider.server.m_SteamID, DateTime.Now);
                            Data.DatabaseManager.GetUsernameAsync(steamplayer.m_SteamID, (names, success) => 
                            {
                                if(success)
                                {
                                    F.Log(F.Translate("unban_UnbanTextFromConsole_WithName_Console", 0, names.PlayerName, steamplayer.m_SteamID.ToString()), ConsoleColor.Cyan);
                                    F.Broadcast("unban_UnbanTextFromConsole_WithName_Broadcast", UCWarfare.GetColor("unban_broadcast"), names.CharacterName);
                                } else
                                {
                                    F.Log(F.Translate("unban_UnbanTextFromConsole_NoName_Console", 0, steamplayer.m_SteamID.ToString()), ConsoleColor.Cyan);
                                    F.Broadcast("unban_UnbanTextFromConsole_NoName_Broadcast", UCWarfare.GetColor("unban_broadcast"), steamplayer.m_SteamID.ToString());
                                }
                            });
                        }
                    }
                }
            }
            else
            {
                UnturnedPlayer player = caller as UnturnedPlayer;
                if (!Provider.isServer)
                    F.SendChat(player, "NotRunningErrorText", UCWarfare.GetColor("defaulterror"));
                else
                {
                    if (command.Length != 1)
                        F.SendChat(player, "InvalidParameterErrorText", UCWarfare.GetColor("defaulterror"));
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            F.SendChat(player, "unban_NoPlayerErrorText", UCWarfare.GetColor("defaulterror"), command[0]);
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            F.SendChat(player, "unban_PlayerIsNotBanned", UCWarfare.GetColor("defaulterror"), command[0]);
                        else
                        {
                            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                                Server.LogPlayerUnbanned(steamplayer.m_SteamID, player.CSteamID.m_SteamID, DateTime.Now);
                            Data.DatabaseManager.GetUsernameAsync(steamplayer.m_SteamID, (names, success) =>
                            {
                                FPlayerName callerNames = F.GetPlayerOriginalNames(player.Player);
                                if (success)
                                {
                                    F.Log(F.Translate("unban_UnbanText_WithName_Console", 0, names.PlayerName, steamplayer.m_SteamID.ToString(), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                                    F.SendChat(player, "unban_UnbanText_WithName_Feedback", UCWarfare.GetColor("unban_feedback"), names.CharacterName);
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_UnbanText_WithName_Broadcast", UCWarfare.GetColor("unban_broadcast"), names.CharacterName, callerNames.CharacterName);
                                }
                                else
                                {
                                    F.Log(F.Translate("unban_UnbanText_NoName_Console", 0, steamplayer.m_SteamID.ToString(), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString()), ConsoleColor.Cyan);
                                    F.SendChat(player, "unban_UnbanText_NoName_Feedback", UCWarfare.GetColor("unban_feedback"), steamplayer.m_SteamID.ToString());
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_UnbanText_NoName_Broadcast", UCWarfare.GetColor("unban_broadcast"), steamplayer.m_SteamID.ToString(), callerNames.CharacterName);
                                }
                            });
                        }
                    }
                }
            }
        }
    }
}