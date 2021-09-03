﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands
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
                    F.LogError(F.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length < 2)
                        F.LogError(F.Translate("ban_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, Data.Locale, out ulong result))
                            {
                                if (command.Length < 3)
                                    F.LogError(F.Translate("ban_no_reason_provided", 0, out _));
                                else if (command.Length > 2)
                                {
                                    if (command[1].StartsWith("perm"))
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, SteamBlacklist.PERMANENT);
                                        if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        {
                                            Data.DatabaseManager.AddBan(result, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                            Invocations.Shared.LogBanned.NetInvoke(result, Provider.server.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                        }
                                        FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                        F.Log(F.Translate("ban_permanent_console_operator", 0, out _, names.PlayerName, result.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                        F.Broadcast("ban_permanent_broadcast_operator", names.PlayerName);
                                    }
                                    else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint duration))
                                        F.LogError(F.Translate("ban_invalid_number_console", 0, out _, command[1]));
                                    else
                                    {
                                        string reason = command.MakeRemainder(2);
                                        F.OfflineBan(result, 0U, Provider.server, reason, duration * 60);
                                        if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                        {
                                            Data.DatabaseManager.AddBan(result, 0, duration, reason);
                                            Invocations.Shared.LogBanned.NetInvoke(result, Provider.server.m_SteamID, reason, duration, DateTime.Now);
                                        }
                                        FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                        string time = F.GetTimeFromMinutes(duration, 0);
                                        F.Log(F.Translate("ban_console_operator", 0, out _, names.PlayerName, result.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
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
                                F.LogError(F.Translate("ban_no_player_found_console", 0, out _, command[0]));
                            }
                        }
                        else
                        {
                            uint ipv4AddressOrZero = SDG.Unturned.SteamGameServerNetworkingUtils.getIPv4AddressOrZero(steamplayer.playerID.steamID);
                            if (command.Length < 3)
                                F.LogError(F.Translate("ban_no_reason_provided", 0, out _));
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
                                        Invocations.Shared.LogBanned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, 0UL, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                    }
                                    F.Log(F.Translate("ban_permanent_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    F.Broadcast("ban_permanent_broadcast_operator", steamplayer.playerID.playerName);
                                }
                                else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint result))
                                    F.LogError(F.Translate("ban_invalid_number_console", 0, out _, command[1]));
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60u);
                                    string time = F.GetTimeFromMinutes(result, 0);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                        Invocations.Shared.LogBanned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, 0UL, reason, result, DateTime.Now);
                                    }
                                    F.Log(F.Translate("ban_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
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
                        player.SendChat("ban_syntax");
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (command[0].Length == 17 && command[0].StartsWith("765") && ulong.TryParse(command[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong result))
                            {
                                try
                                {
                                    if (command.Length < 3)
                                        player.SendChat("ban_no_reason_provided");
                                    else if (command.Length > 2)
                                    {
                                        if (command[1].StartsWith("perm"))
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, SteamBlacklist.PERMANENT);
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                            {
                                                Data.DatabaseManager.AddBan(result, player.CSteamID.m_SteamID, SteamBlacklist.PERMANENT / 60u, reason);
                                                Invocations.Shared.LogBanned.NetInvoke(result, player.CSteamID.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                            }
                                            FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                            F.Log(F.Translate("ban_permanent_console", 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName,
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                            player.SendChat("ban_permanent_feedback", names.CharacterName);
                                            F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_permanent_broadcast",
                                                names.CharacterName, callerName.CharacterName);
                                        }
                                        else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint duration))
                                            player.SendChat("ban_invalid_number", command[2]);
                                        else
                                        {
                                            string reason = command.MakeRemainder(2);
                                            F.OfflineBan(result, 0U, player.CSteamID, reason, duration * 60);
                                            if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                            {
                                                Data.DatabaseManager.AddBan(result, player.CSteamID.m_SteamID, duration, reason);
                                                Invocations.Shared.LogBanned.NetInvoke(result, player.CSteamID.m_SteamID, reason, duration, DateTime.Now);
                                            }
                                            FPlayerName names = Data.DatabaseManager.GetUsernames(result);
                                            string timeLocalized = F.GetTimeFromMinutes(duration, 0);
                                            F.Log(F.Translate("ban_console", 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName,
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                            player.SendChat("ban_feedback", names.CharacterName, timeLocalized);
                                            foreach (SteamPlayer pl in Provider.clients)
                                            {
                                                if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                                {
                                                    timeLocalized = F.GetTimeFromMinutes(duration, pl.playerID.steamID.m_SteamID);
                                                    pl.SendChat("ban_broadcast", names.CharacterName, callerName.CharacterName, timeLocalized);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    player.SendChat("ban_no_player_found", command[0]);
                                }
                            }
                            else
                            {
                                player.SendChat("ban_no_player_found", command[0]);
                            }
                        }
                        else
                        {
                            uint ipv4AddressOrZero = SDG.Unturned.SteamGameServerNetworkingUtils.getIPv4AddressOrZero(steamplayer.playerID.steamID);
                            if (command.Length < 3)
                                player.SendChat("ban_no_reason_provided");
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
                                        Invocations.Shared.LogBanned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, SteamBlacklist.PERMANENT / 60u, DateTime.Now);
                                    }
                                    F.Log(F.Translate("ban_permanent_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale),
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    player.SendChat("ban_permanent_feedback", names.CharacterName, callerName.CharacterName);
                                    F.BroadcastToAllExcept(new List<CSteamID> { player.CSteamID }, "ban_permanent_broadcast",
                                        names.CharacterName, callerName.CharacterName);
                                }
                                else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint result))
                                    player.SendChat("ban_invalid_number", command[2]);
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    FPlayerName names = F.GetPlayerOriginalNames(steamplayer);
                                    Provider.requestBanPlayer(player.CSteamID, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60);
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, result, reason);
                                        Invocations.Shared.LogBanned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, player.CSteamID.m_SteamID, reason, result, DateTime.Now);
                                    }
                                    string timeLocalized = F.GetTimeFromMinutes(result, 0);
                                    F.Log(F.Translate("ban_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale),
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                    player.SendChat("ban_feedback", names.CharacterName, callerName.CharacterName, timeLocalized);
                                    foreach (SteamPlayer pl in Provider.clients)
                                    {
                                        if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                        {
                                            timeLocalized = F.GetTimeFromMinutes(result, pl.playerID.steamID.m_SteamID);
                                            pl.SendChat("ban_broadcast", names.CharacterName, callerName.CharacterName, timeLocalized);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void BanPlayer(ulong Violator, ulong Admin, string Reason, uint DurationMins)
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
                F.OfflineBan(Violator, 0U, admin == null ? new CSteamID(Admin) : admin.playerID.steamID, Reason, DurationMins * 60);
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    Data.DatabaseManager.AddBan(Violator, Admin, DurationMins, Reason);
                    Invocations.Shared.LogBanned.NetInvoke(Violator, Admin, Reason, DurationMins, DateTime.Now);
                }
                string timeLocalized = F.GetTimeFromMinutes(DurationMins, 0);
                F.Log(F.Translate("ban_console" + (Admin == 0 ? "_operator" : string.Empty), 0, out _, names.PlayerName, Violator.ToString(Data.Locale), callerName.PlayerName,
                    Admin.ToString(Data.Locale), Reason, timeLocalized), ConsoleColor.Cyan);
                if (admin != null)
                    admin.SendChat("ban_feedback", names.CharacterName, timeLocalized);
                foreach (SteamPlayer pl in Provider.clients)
                {
                    if (pl.playerID.steamID.m_SteamID != Admin)
                    {
                        timeLocalized = F.GetTimeFromMinutes(DurationMins, pl.playerID.steamID.m_SteamID);
                        pl.SendChat("ban_broadcast" + (Admin == 0 ? "_operator" : string.Empty), names.CharacterName, callerName.CharacterName, timeLocalized);
                    }
                }
            }
            else
            {
                if (!violator.transportConnection.TryGetIPv4Address(out uint ip)) ip = 0;
                Provider.requestBanPlayer(violator.playerID.steamID, admin == null ? new CSteamID(Admin) : admin.playerID.steamID, ip, Reason, DurationMins * 60);
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    Data.DatabaseManager.AddBan(Violator, Admin, DurationMins, Reason);
                    Invocations.Shared.LogBanned.NetInvoke(Violator, Admin, Reason, DurationMins, DateTime.Now);
                }
                string timeLocalized = F.GetTimeFromMinutes(DurationMins, 0);
                F.Log(F.Translate("ban_console" + (Admin == 0 ? "_operator" : string.Empty), 0, out _, names.PlayerName, Violator.ToString(Data.Locale),
                    callerName.PlayerName, Admin.ToString(Data.Locale), Reason, timeLocalized), ConsoleColor.Cyan);
                if (admin != null)
                    admin.SendChat("ban_feedback", names.CharacterName, callerName.CharacterName, timeLocalized);
                foreach (SteamPlayer pl in Provider.clients)
                {
                    if (pl.playerID.steamID.m_SteamID != Admin)
                    {
                        timeLocalized = F.GetTimeFromMinutes(DurationMins, pl.playerID.steamID.m_SteamID);
                        pl.SendChat("ban_broadcast" + (Admin == 0 ? "_operator" : string.Empty), names.CharacterName, callerName.CharacterName, timeLocalized);
                    }
                }
            }
        }
    }
}