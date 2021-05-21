using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UncreatedWarfare.Commands
{
    class BanOverrideCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ban";
        public string Help => "Ban players who are misbehaving.";
        public string Syntax => "/ban <player> <duration minutes> [reason] ";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.ban" };
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
                    if (command.Length < 2)
                        F.LogError(F.Translate("InvalidParameterErrorText", 0));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong result))
                            {
                                if (command.Length < 3)
                                    F.LogError(F.Translate("ban_ErrorNoReasonProvided_Console", 0));
                                else if (command.Length > 2)
                                {
                                    if (command[1].StartsWith("perm"))
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, SteamBlacklist.PERMANENT);
                                        Data.DatabaseManager.GetUsernameAsync(result, (names, success) =>
                                        {
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                                Data.WebInterface?.LogBan(result, Provider.server.m_SteamID, names.PlayerName, "Console", 0, reason, SteamBlacklist.PERMANENT / 60u);
                                            F.Log(F.Translate("ban_BanTextPermanentFromConsole_Console", 0, names.PlayerName, result.ToString(), reason), ConsoleColor.Cyan);
                                            F.Broadcast("ban_BanTextPermanentFromConsole_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.PlayerName);
                                        });
                                    }
                                    else if (!uint.TryParse(command[1], out uint duration))
                                        F.LogError(F.Translate("ban_InvalidNumberErrorText_Console", 0, command[1]));
                                    else
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, duration * 60);
                                        Data.DatabaseManager.GetUsernameAsync(result, (names, success) =>
                                        {
                                            string time = F.GetTimeFromMinutes(duration);
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                                Data.WebInterface?.LogBan(result, Provider.server.m_SteamID, names.PlayerName, "Console", 0, reason, duration);
                                            F.Log(F.Translate("ban_BanTextFromConsole_Console", 0, names.PlayerName, result, reason, time), ConsoleColor.Cyan);
                                            F.Broadcast("ban_BanTextFromConsole_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.PlayerName, time);
                                        });
                                    }
                                }
                            }
                            else
                            {
                                F.LogError(F.Translate("ban_NoPlayerErrorText_Console", 0, command[0]));
                            }
                        }
                        else
                        {
                            uint ipv4AddressOrZero = SDG.Unturned.SteamGameServerNetworkingUtils.getIPv4AddressOrZero(steamplayer.playerID.steamID);
                            if (command.Length < 3)
                                F.Log(F.Translate("ban_ErrorNoReasonProvided_Console", 0), ConsoleColor.Cyan);
                            else if (command.Length > 2)
                            {
                                if (command[1].StartsWith("perm"))
                                {
                                    string reason = command.MakeRemainder(2);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, SteamBlacklist.PERMANENT);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        Data.WebInterface?.LogBan(steamplayer.playerID.steamID.m_SteamID, Provider.server.m_SteamID, name, "Console", F.GetTeamByte(steamplayer), reason, SteamBlacklist.PERMANENT / 60u);
                                    F.Log(F.Translate("ban_BanTextPermanentFromConsole_Console", 0, name, steamplayer.playerID.steamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                    F.Broadcast("ban_BanTextPermanentFromConsole_Broadcast", UCWarfare.GetColor("ban_broadcast"), steamplayer.playerID.playerName);
                                }
                                else if (!uint.TryParse(command[1], out uint result))
                                    F.Log(F.Translate("ban_InvalidNumberErrorText_Console", 0, command[1]), ConsoleColor.Cyan);
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60u);
                                    string time = F.GetTimeFromMinutes(result);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        Data.WebInterface?.LogBan(steamplayer.playerID.steamID.m_SteamID, Provider.server.m_SteamID, name, "Console", F.GetTeamByte(steamplayer), reason, result);
                                    F.Log(F.Translate("ban_BanTextFromConsole_Console", 0, name, steamplayer.playerID.steamID, reason, time), ConsoleColor.Cyan);
                                    F.Broadcast("ban_BanTextFromConsole_Broadcast", UCWarfare.GetColor("ban_broadcast"), steamplayer.playerID.playerName, time);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                UnturnedPlayer player = caller as UnturnedPlayer;
                FPlayerName callerName = F.GetPlayerOriginalNames(player.Player);
                if (!Provider.isServer)
                    player.SendChat("NotRunningErrorText", UCWarfare.GetColor("defaulterror"));
                else
                {
                    if (command.Length != 1 && command.Length != 2 && command.Length != 3)
                        F.SendChat(player, "InvalidParameterErrorText", UCWarfare.GetColor("defaulterror"));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong result))
                            {
                                try
                                {
                                    if (command.Length < 3)
                                        F.SendChat(player, "ban_ErrorNoReasonProvided", UCWarfare.GetColor("defaulterror"));
                                    else if (command.Length > 2)
                                    {
                                        if (command[1].StartsWith("perm"))
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, SteamBlacklist.PERMANENT);
                                            Data.DatabaseManager.GetUsernameAsync(result, (names, success) =>
                                            {
                                                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                                    Data.WebInterface?.LogBan(result, player.CSteamID.m_SteamID, names.PlayerName, callerName.PlayerName, F.GetTeamByte(result), reason, SteamBlacklist.PERMANENT / 60u);
                                                F.Log(F.Translate("ban_BanTextPermanent_Console", 0, names.PlayerName, result.ToString(), callerName.PlayerName, player.CSteamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                                F.SendChat(player, "ban_BanTextPermanent", UCWarfare.GetColor("ban_feedback"), names.CharacterName);
                                                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_BanTextPermanent_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.CharacterName, callerName.CharacterName);
                                            });
                                        }
                                        else if (!uint.TryParse(command[1], out uint duration))
                                            F.SendChat(player, "ban_InvalidNumberErrorText", UCWarfare.GetColor("defaulterror"), command[2]);
                                        else
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, duration * 60);
                                            Data.DatabaseManager.GetUsernameAsync(result, (names, success) =>
                                            {
                                                string timeLocalized = F.GetTimeFromMinutes(duration);
                                                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                                    Data.WebInterface?.LogBan(result, player.CSteamID.m_SteamID, names.PlayerName, callerName.PlayerName, F.GetTeamByte(result), reason, duration);
                                                F.Log(F.Translate("ban_BanText_Console", 0, names.PlayerName, result.ToString(), callerName.PlayerName, player.CSteamID.m_SteamID.ToString(), reason, timeLocalized), ConsoleColor.Cyan);
                                                F.SendChat(player, "ban_BanText", UCWarfare.GetColor("ban_feedback"), names.CharacterName, timeLocalized);
                                                F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_BanText_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.CharacterName, callerName.CharacterName, timeLocalized);
                                            });
                                        }
                                    }
                                }
                                catch
                                {
                                    F.SendChat(player, "ban_NoPlayerErrorText", UCWarfare.GetColor("defaulterror"), command[0]);
                                }
                            }
                            else
                            {
                                F.SendChat(player, "ban_NoPlayerErrorText", UCWarfare.GetColor("defaulterror"), command[0]);
                            }
                        }
                        else
                        {
                            uint ipv4AddressOrZero = SDG.Unturned.SteamGameServerNetworkingUtils.getIPv4AddressOrZero(steamplayer.playerID.steamID);
                            if (command.Length < 3)
                                F.SendChat(player, "ban_ErrorNoReasonProvided", UCWarfare.GetColor("defaulterror"));
                            else if (command.Length > 2)
                            {
                                if (command[1].StartsWith("perm"))
                                {
                                    string reason = command.MakeRemainder(2);
                                    FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                    Provider.requestBanPlayer(player.CSteamID, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, SteamBlacklist.PERMANENT);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        Data.WebInterface?.LogBan(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, names.PlayerName, callerName.PlayerName, F.GetTeamByte(steamplayer), reason, SteamBlacklist.PERMANENT / 60u);
                                    F.Log(F.Translate("ban_BanTextPermanent_Console", 0, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(), callerName.PlayerName, player.CSteamID.m_SteamID.ToString(), reason), ConsoleColor.Cyan);
                                    F.SendChat(player, "ban_BanTextPermanent", UCWarfare.GetColor("ban_feedback"), names.CharacterName, callerName.CharacterName);
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_BanTextPermanent_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.CharacterName, callerName.CharacterName);
                                }
                                else if (!uint.TryParse(command[1], out uint result))
                                    F.SendChat(player, "ban_InvalidNumberErrorText", UCWarfare.GetColor("defaulterror"), command[2]);
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                    Provider.requestBanPlayer(player.CSteamID, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        Data.WebInterface?.LogBan(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, names.PlayerName, callerName.PlayerName, F.GetTeamByte(steamplayer), reason, result);
                                    string timeLocalized = F.GetTimeFromMinutes(result);
                                    F.Log(F.Translate("ban_BanText_Console", 0, names.PlayerName, steamplayer.playerID.steamID, callerName.PlayerName, player.CSteamID.m_SteamID.ToString(), reason, timeLocalized), ConsoleColor.Cyan);
                                    F.SendChat(player, "ban_BanText", UCWarfare.GetColor("ban_feedback"), names.CharacterName, callerName.CharacterName, timeLocalized);
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_BanText_Broadcast", UCWarfare.GetColor("ban_broadcast"), names.CharacterName, callerName.CharacterName, timeLocalized);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}