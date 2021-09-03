﻿using Google.Protobuf.WellKnownTypes;
using MySqlX.XDevAPI.Common;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Framework.Translations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Packaging;
using Uncreated.Networking;
using Uncreated.Players;

namespace Uncreated.Warfare.Commands
{
    public class IPBanCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ipban";
        public string Help => "Ban players who are misbehaving by IP.";
        public string Syntax => "/ipban <player> <duration minutes> [reason]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "uc.ipban" };
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
                    if (command.Length < 2)
                        F.LogError(F.Translate("ip_ban_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, Data.Locale, out ulong result))
                            {
                                if (command.Length < 3)
                                    F.LogError(F.Translate("ip_ban_no_reason_provided", 0, out _));
                                else if (command.Length > 2)
                                {
                                    if (command[1].StartsWith("perm"))
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, SteamBlacklist.PERMANENT);
                                        string translate = "ip_ban_permanent_console_operator";
                                        if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        {
                                            Client.LogPlayerBanned(result, Provider.server.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                            Data.DatabaseManager.AddBan(result, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                            string ip = Data.DatabaseManager.GetIP(result);
                                            if (ip == "255.255.255.255")
                                            {
                                                translate = "ip_ban_permanent_console_operator_noip";
                                            } else
                                            {
                                                uint packaged = Parser.getUInt32FromIP(ip);
                                                Data.DatabaseManager.AddIPBan(result, packaged, ip, -1, reason);
                                            }
                                        }
                                        FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                        F.Log(F.Translate(translate, 0, out _, names.PlayerName, result.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                        F.Broadcast("ban_permanent_broadcast_operator", names.PlayerName);
                                    }
                                    else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint duration))
                                        F.LogError(F.Translate("ip_ban_invalid_number_console", 0, out _, command[1]));
                                    else
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, duration * 60);
                                        string translate = "ip_ban_console_operator";
                                        if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        {
                                            Data.DatabaseManager.AddBan(result, 0, duration, reason);
                                            Client.LogPlayerBanned(result, Provider.server.m_SteamID, reason, duration, DateTime.Now);
                                            string ip = Data.DatabaseManager.GetIP(result);
                                            if (ip == "255.255.255.255")
{
                                                translate = "ip_ban_console_operator_noip";
                                            }
                                            else
                                            {
                                                uint packaged = Parser.getUInt32FromIP(ip);
                                                Data.DatabaseManager.AddIPBan(result, packaged, ip, (int)duration, reason);
                                            }
                                        }
                                        FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                        string time = F.GetTimeFromMinutes(duration, 0);
                                        F.Log(F.Translate(translate, 0, out _, names.PlayerName, result.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
                                        foreach (SteamPlayer player in Provider.clients)
                                        {
                                            time = F.GetTimeFromMinutes(duration, player.playerID.steamID.m_SteamID);
                                            player.SendChat("ban_broadcast_operator", names.PlayerName, time);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                F.LogError(F.Translate("ip_ban_no_player_found_console", 0, out _, command[0]));
                            }
                        }
                        else
                        {
                            if (!steamplayer.transportConnection.TryGetIPv4Address(out uint ipv4AddressOrZero))
                            {
                                F.LogError(F.Translate("ip_ban_no_player_found_console", 0, out _, command[0]));
                                return;
                            }
                            string ip = Parser.getIPFromUInt32(ipv4AddressOrZero);
                            if (command.Length < 3)
                                F.LogError(F.Translate("ip_ban_no_reason_provided", 0, out _));
                            else if (command.Length > 2)
                            {
                                if (command[1].StartsWith("perm"))
                                {
                                    string reason = command.MakeRemainder(2);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, SteamBlacklist.PERMANENT);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                        Client.LogPlayerBanned(steamplayer.playerID.steamID.m_SteamID, Provider.server.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                        Data.DatabaseManager.AddIPBan(steamplayer.playerID.steamID.m_SteamID, ipv4AddressOrZero, ip, -1, reason);
                                    }
                                    F.Log(F.Translate("ip_ban_permanent_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    F.Broadcast("ban_permanent_broadcast_operator", steamplayer.playerID.playerName);
                                }
                                else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint result))
                                    F.LogError(F.Translate("ip_ban_invalid_number_console", 0, out _, command[1]));
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60u);
                                    string time = F.GetTimeFromMinutes(result, 0);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                        Client.LogPlayerBanned(steamplayer.playerID.steamID.m_SteamID, Provider.server.m_SteamID, reason, result, DateTime.Now);
                                        Data.DatabaseManager.AddIPBan(steamplayer.playerID.steamID.m_SteamID, ipv4AddressOrZero, ip, (int)result, reason);
                                    }
                                    F.Log(F.Translate("ip_ban_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
                                    foreach (SteamPlayer player in Provider.clients)
                                    {
                                        time = F.GetTimeFromMinutes(result, player.playerID.steamID.m_SteamID);
                                        player.SendChat("ban_broadcast_operator", steamplayer.playerID.playerName, time);
                                    }
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
                    player.SendChat("server_not_running");
                else
                {
                    if (command.Length != 1 && command.Length != 2 && command.Length != 3)
                        player.SendChat("ip_ban_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong result))
                            {
                                try
                                {
                                    if (command.Length < 3)
                                        player.SendChat("ip_ban_no_reason_provided");
                                    else if (command.Length > 2)
                                    {
                                        if (command[1].StartsWith("perm"))
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, SteamBlacklist.PERMANENT);
                                            string translate = "ip_ban_permanent_feedback";
                                            string translate2 = "ip_ban_permanent_console";
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                            {
                                                Data.DatabaseManager.AddBan(result, player.CSteamID.m_SteamID, SteamBlacklist.PERMANENT / 60u, reason);
                                                Client.LogPlayerBanned(result, player.CSteamID.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                                string ip = Data.DatabaseManager.GetIP(result);
                                                if (ip == "255.255.255.255")
                                                {
                                                    translate = "ip_ban_permanent_feedback_noip";
                                                    translate2 = "ip_ban_permanent_console_noip";
                                                }
                                                else
                                                {
                                                    uint packaged = Parser.getUInt32FromIP(ip);
                                                    Data.DatabaseManager.AddIPBan(result, packaged, ip, -1, reason);
                                                }
                                            }
                                            FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                            F.Log(F.Translate(translate2, 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName, 
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                            player.SendChat(translate, names.CharacterName);
                                            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_permanent_broadcast", 
                                                names.CharacterName, callerName.CharacterName);
                                        }
                                        else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint duration))
                                            player.SendChat("ip_ban_invalid_number", command[2]);
                                        else
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, duration * 60);
                                            string translate = "ip_ban_feedback";
                                            string translate2 = "ip_ban_console";
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                            {
                                                Data.DatabaseManager.AddBan(result, player.CSteamID.m_SteamID, duration, reason);
                                                Client.LogPlayerBanned(result, player.CSteamID.m_SteamID, reason, duration, DateTime.Now);
                                                string ip = Data.DatabaseManager.GetIP(result);
                                                if (ip == "255.255.255.255")
                                                {
                                                    translate = "ip_ban_feedback_noip";
                                                    translate2 = "ip_ban_console_noip";
                                                }
                                                else
                                                {
                                                    uint packaged = Parser.getUInt32FromIP(ip);
                                                    Data.DatabaseManager.AddIPBan(result, packaged, ip, (int)duration, reason);
                                                }
                                            }
                                            FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                            string timeLocalized = F.GetTimeFromMinutes(duration, 0);
                                            F.Log(F.Translate(translate2, 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName, 
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                            player.SendChat(translate, names.CharacterName, timeLocalized);
                                            foreach (SteamPlayer pl in Provider.clients)
                                            {
                                                if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                                {
                                                    timeLocalized = F.GetTimeFromMinutes(duration, pl.playerID.steamID.m_SteamID);
                                                    player.SendChat("ban_broadcast", names.CharacterName, callerName.CharacterName, timeLocalized);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    player.SendChat("ip_ban_no_player_found", command[0]);
                                }
                            }
                            else
                            {
                                player.SendChat("ip_ban_no_player_found", command[0]);
                            }
                        }
                        else
                        {
                            if (!steamplayer.transportConnection.TryGetIPv4Address(out uint ipv4AddressOrZero))
                            {
                                F.LogError(F.Translate("ip_ban_no_player_found", 0, out _, command[0]));
                                return;
                            }
                            string ip = Parser.getIPFromUInt32(ipv4AddressOrZero);
                            if (command.Length < 3)
                                player.SendChat("ip_ban_no_reason_provided");
                            else if (command.Length > 2)
                            {
                                if (command[1].StartsWith("perm"))
                                {
                                    string reason = command.MakeRemainder(2);
                                    FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                    Provider.requestBanPlayer(player.CSteamID, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, SteamBlacklist.PERMANENT);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, SteamBlacklist.PERMANENT / 60u, reason);
                                        Client.LogPlayerBanned(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                        Data.DatabaseManager.AddIPBan(steamplayer.playerID.steamID.m_SteamID, ipv4AddressOrZero, ip, -1, reason);
                                    }
                                    F.Log(F.Translate("ip_ban_permanent_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), 
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    player.SendChat("ip_ban_permanent_feedback", names.CharacterName, callerName.CharacterName);
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_permanent_broadcast",
                                        names.CharacterName, callerName.CharacterName);
                                }
                                else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint result))
                                    player.SendChat("ip_ban_invalid_number", command[2]);
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                    Provider.requestBanPlayer(player.CSteamID, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, result, reason);
                                        Client.LogPlayerBanned(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, result, DateTime.Now);
                                        Data.DatabaseManager.AddIPBan(steamplayer.playerID.steamID.m_SteamID, ipv4AddressOrZero, ip, (int)result, reason);
                                    }
                                    string timeLocalized = F.GetTimeFromMinutes(result, 0);
                                    F.Log(F.Translate("ip_ban_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), 
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                    player.SendChat("ip_ban_feedback", names.CharacterName, callerName.CharacterName, timeLocalized);
                                    foreach (SteamPlayer pl in Provider.clients)
                                    {
                                        if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                        {
                                            timeLocalized = F.GetTimeFromMinutes(result, pl.playerID.steamID.m_SteamID);
                                            player.SendChat("ban_broadcast", names.CharacterName, callerName.CharacterName, timeLocalized);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}