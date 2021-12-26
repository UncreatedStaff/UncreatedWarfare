﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
{
    class UnbanOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "unban";

        public string Help => "Unban players who have served their time.";

        public string Syntax => "/unban <player ID>";

        public List<string> Aliases => new List<string>(0);

        public List<string> Permissions => new List<string>(1) { "uc.unban" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!(caller is UnturnedPlayer player))
            {
                if (!Provider.isServer)
                    L.LogError(Translation.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length != 1)
                        L.LogError(Translation.Translate("unban_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            L.LogError(Translation.Translate("unban_no_player_found_console", 0, out _, command[0]));
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            L.LogError(Translation.Translate("unban_player_not_banned_console", 0, out _, command[0]));
                        else
                        {
                            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                            {
                                Data.DatabaseManager.AddUnban(steamplayer.m_SteamID, 0UL);
                                Invocations.Shared.LogUnbanned.NetInvoke(steamplayer.m_SteamID, 0UL, DateTime.Now);
                            }
                            FPlayerName names = Data.DatabaseManager.GetUsernames(steamplayer.m_SteamID);
                            if (names.Steam64.ToString(Data.Locale) == names.PlayerName)
                            {
                                L.Log(Translation.Translate("unban_unbanned_console_id_operator", 0, out _, steamplayer.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                Chat.Broadcast("unban_unbanned_broadcast_id_operator", steamplayer.m_SteamID.ToString(Data.Locale));
                            }
                            else
                            {
                                L.Log(Translation.Translate("unban_unbanned_console_name_operator", 0, out _, names.PlayerName, steamplayer.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                Chat.Broadcast("unban_unbanned_broadcast_name_operator", names.CharacterName);
                            }
                        }
                    }
                }
            }
            else
            {
                if (!Provider.isServer)
                    Chat.SendChat(player, "server_not_running");
                else
                {
                    if (command.Length != 1)
                        Chat.SendChat(player, "unban_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamID(command[0], out CSteamID steamplayer))
                            Chat.SendChat(player, "unban_no_player_found", command[0]);
                        else if (!Provider.requestUnbanPlayer(Provider.server, steamplayer))
                            Chat.SendChat(player, "unban_player_not_banned", command[0]);
                        else
                        {
                            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                            {
                                Data.DatabaseManager.AddUnban(steamplayer.m_SteamID, player.CSteamID.m_SteamID);
                                Invocations.Shared.LogUnbanned.NetInvoke(steamplayer.m_SteamID, player.CSteamID.m_SteamID, DateTime.Now);
                            }
                            FPlayerName names = Data.DatabaseManager.GetUsernames(steamplayer.m_SteamID);
                            FPlayerName callerNames = F.GetPlayerOriginalNames(player.Player);
                            if (names.Steam64.ToString(Data.Locale) == names.PlayerName)
                            {
                                L.Log(Translation.Translate("unban_unbanned_console_id", 0, out _, steamplayer.m_SteamID.ToString(Data.Locale), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                Chat.SendChat(player, "unban_unbanned_feedback_id", steamplayer.m_SteamID.ToString(Data.Locale));
                                Chat.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_unbanned_broadcast_id", steamplayer.m_SteamID.ToString(Data.Locale), callerNames.CharacterName);
                            }
                            else
                            {
                                L.Log(Translation.Translate("unban_unbanned_console_name", 0, out _, names.PlayerName, steamplayer.m_SteamID.ToString(Data.Locale), callerNames.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                                Chat.SendChat(player, "unban_unbanned_feedback_name", names.CharacterName);
                                Chat.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "unban_unbanned_broadcast_name", names.CharacterName, callerNames.CharacterName);
                            }
                        }
                    }
                }
            }
        }
        public static void UnbanPlayer(ulong Violator, ulong Admin)
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
            if (violator == null)
            {
                CSteamID id = new CSteamID(Violator);
                if (!Provider.requestUnbanPlayer(Provider.server, id))
                    SharedInvocations.PrintText.NetInvoke(DateTime.Now, "UNBAN: Player not banned", ConsoleColor.Red);
                else
                {
                    if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
                    {
                        Data.DatabaseManager.AddUnban(Violator, Admin);
                        Invocations.Shared.LogUnbanned.NetInvoke(Violator, Admin, DateTime.Now);
                    }
                    if (names.Steam64.ToString(Data.Locale) == names.PlayerName)
                    {
                        L.Log(Translation.Translate("unban_unbanned_console_id", 0, out _, Violator.ToString(Data.Locale), callerName.PlayerName, Admin.ToString(Data.Locale)), ConsoleColor.Cyan);
                        if (admin != null)
                            Chat.SendChat(admin, "unban_unbanned_feedback_id", Violator.ToString(Data.Locale));
                        Chat.BroadcastToAllExcept(new List<CSteamID> { admin == null ? new CSteamID(Admin) : admin.playerID.steamID }, "unban_unbanned_broadcast_id", Violator.ToString(Data.Locale), callerName.CharacterName);
                    }
                    else
                    {
                        L.Log(Translation.Translate("unban_unbanned_console_name", 0, out _, names.PlayerName, Violator.ToString(Data.Locale), callerName.PlayerName, Admin.ToString(Data.Locale)), ConsoleColor.Cyan);
                        if (admin != null)
                            Chat.SendChat(admin, "unban_unbanned_feedback_name", names.CharacterName);
                        Chat.BroadcastToAllExcept(new List<CSteamID> { admin == null ? new CSteamID(Admin) : admin.playerID.steamID }, "unban_unbanned_broadcast_name", names.CharacterName, callerName.CharacterName);
                    }
                }
            }
            else
                SharedInvocations.PrintText.NetInvoke(DateTime.Now, "UNBAN: Player not banned", ConsoleColor.Red);
        }
    }
}