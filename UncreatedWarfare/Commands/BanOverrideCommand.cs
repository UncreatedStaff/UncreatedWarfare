using Rocket.API;
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
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.ban" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller.DisplayName == "Console")
            {
                if (!Provider.isServer)
                    L.LogError(Translation.Translate("server_not_running", 0, out _));
                else
                {
                    if (command.Length < 2)
                        L.LogError(Translation.Translate("ban_syntax", 0, out _));
                    else
                    {
                        if (!PlayerTool.tryGetSteamPlayer(command[0], out SteamPlayer steamplayer))
                        {
                            if (ulong.TryParse(command[0], NumberStyles.Any, Data.Locale, out ulong result) && OffenseManager.IsValidSteam64ID(result))
                            {
                                if (command.Length < 3)
                                    L.LogError(Translation.Translate("ban_no_reason_provided", 0, out _));
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
                                        L.Log(Translation.Translate("ban_permanent_console_operator", 0, out _, names.PlayerName, result.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                        Chat.Broadcast("ban_permanent_broadcast_operator", names.PlayerName);
                                    }
                                    else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint duration))
                                        L.LogError(Translation.Translate("ban_invalid_number_console", 0, out _, command[1]));
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
                                        string time = duration.GetTimeFromMinutes(0);
                                        L.Log(Translation.Translate("ban_console_operator", 0, out _, names.PlayerName, result.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
                                        foreach (SteamPlayer player in Provider.clients)
                                        {
                                            time = duration.GetTimeFromMinutes(player.playerID.steamID.m_SteamID);
                                            player.SendChat("ban_broadcast_operator", names.PlayerName, time);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                L.LogError(Translation.Translate("ban_no_player_found_console", 0, out _, command[0]));
                            }
                        }
                        else
                        {
                            uint ipv4AddressOrZero = SDG.Unturned.SteamGameServerNetworkingUtils.getIPv4AddressOrZero(steamplayer.playerID.steamID);
                            if (command.Length < 3)
                                L.LogError(Translation.Translate("ban_no_reason_provided", 0, out _));
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
                                    L.Log(Translation.Translate("ban_permanent_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    Chat.Broadcast("ban_permanent_broadcast_operator", steamplayer.playerID.playerName);
                                }
                                else if (!uint.TryParse(command[1], NumberStyles.Any, Data.Locale, out uint result))
                                    L.LogError(Translation.Translate("ban_invalid_number_console", 0, out _, command[1]));
                                else
                                {
                                    string reason = command.MakeRemainder(2);
                                    Provider.requestBanPlayer(Provider.server, steamplayer.playerID.steamID, ipv4AddressOrZero, reason, result * 60u);
                                    string time = result.GetTimeFromMinutes(0);
                                    string name = F.GetPlayerOriginalNames(steamplayer).PlayerName;
                                    if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                                    {
                                        Data.DatabaseManager.AddBan(steamplayer.playerID.steamID.m_SteamID, 0, SteamBlacklist.PERMANENT / 60u, reason);
                                        Invocations.Shared.LogBanned.NetInvoke(steamplayer.playerID.steamID.m_SteamID, 0UL, reason, result, DateTime.Now);
                                    }
                                    L.Log(Translation.Translate("ban_console_operator", 0, out _, name, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale), reason, time), ConsoleColor.Cyan);
                                    foreach (SteamPlayer player in Provider.clients)
                                    {
                                        time = result.GetTimeFromMinutes(player.playerID.steamID.m_SteamID);
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
                UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
                if (player == null) return;
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
                            if (ulong.TryParse(command[0], NumberStyles.Any, CultureInfo.InvariantCulture, out ulong result) && OffenseManager.IsValidSteam64ID(result))
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
                                            L.Log(Translation.Translate("ban_permanent_console", 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName,
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                            player.SendChat("ban_permanent_feedback", names.CharacterName);
                                            Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "ban_permanent_broadcast",
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
                                            string timeLocalized = duration.GetTimeFromMinutes(0);
                                            L.Log(Translation.Translate("ban_console", 0, out _, names.PlayerName, result.ToString(Data.Locale), callerName.PlayerName,
                                                player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                            player.SendChat("ban_feedback", names.CharacterName, timeLocalized);
                                            foreach (SteamPlayer pl in Provider.clients)
                                            {
                                                if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                                {
                                                    timeLocalized = duration.GetTimeFromMinutes(pl.playerID.steamID.m_SteamID);
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
                                    L.Log(Translation.Translate("ban_permanent_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale),
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason), ConsoleColor.Cyan);
                                    player.SendChat("ban_permanent_feedback", names.CharacterName, callerName.CharacterName);
                                    Chat.BroadcastToAllExcept(new ulong[1] { player.CSteamID.m_SteamID }, "ban_permanent_broadcast", names.CharacterName, callerName.CharacterName);
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
                                    string timeLocalized = result.GetTimeFromMinutes(0);
                                    L.Log(Translation.Translate("ban_console", 0, out _, names.PlayerName, steamplayer.playerID.steamID.m_SteamID.ToString(Data.Locale),
                                        callerName.PlayerName, player.CSteamID.m_SteamID.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
                                    player.SendChat("ban_feedback", names.CharacterName, callerName.CharacterName, timeLocalized);
                                    foreach (SteamPlayer pl in Provider.clients)
                                    {
                                        if (pl.playerID.steamID.m_SteamID != player.CSteamID.m_SteamID)
                                        {
                                            timeLocalized = result.GetTimeFromMinutes(pl.playerID.steamID.m_SteamID);
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
            SteamPlayer? violator = PlayerTool.getSteamPlayer(Violator);
            SteamPlayer? admin = PlayerTool.getSteamPlayer(Admin);
            FPlayerName callerName;
            if (admin == null)
                callerName = Data.DatabaseManager.GetUsernames(Admin);
            else
                callerName = F.GetPlayerOriginalNames(admin);
            FPlayerName names;
            if (violator == null)
                names = Data.DatabaseManager.GetUsernames(Violator);
            else
                names = F.GetPlayerOriginalNames(Violator);
            if (violator == null)
            {
                F.OfflineBan(Violator, 0U, admin == null ? new CSteamID(Admin) : admin.playerID.steamID, Reason, DurationMins * 60);
                if (UCWarfare.Config.AdminLoggerSettings.LogBans)
                {
                    Data.DatabaseManager.AddBan(Violator, Admin, DurationMins, Reason);
                    Invocations.Shared.LogBanned.NetInvoke(Violator, Admin, Reason, DurationMins, DateTime.Now);
                }
                string timeLocalized = DurationMins.GetTimeFromMinutes(0);
                L.Log(Translation.Translate("ban_console" + (Admin == 0 ? "_operator" : string.Empty), 0, out _, names.PlayerName, Violator.ToString(Data.Locale), callerName.PlayerName,
                    Admin.ToString(Data.Locale), Reason, timeLocalized), ConsoleColor.Cyan);
                if (admin != null)
                    admin.SendChat("ban_feedback", names.CharacterName, timeLocalized);
                foreach (SteamPlayer pl in Provider.clients)
                {
                    if (pl.playerID.steamID.m_SteamID != Admin)
                    {
                        timeLocalized = DurationMins.GetTimeFromMinutes(pl.playerID.steamID.m_SteamID);
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
                string timeLocalized = DurationMins.GetTimeFromMinutes(0);
                L.Log(Translation.Translate("ban_console" + (Admin == 0 ? "_operator" : string.Empty), 0, out _, names.PlayerName, Violator.ToString(Data.Locale),
                    callerName.PlayerName, Admin.ToString(Data.Locale), Reason, timeLocalized), ConsoleColor.Cyan);
                if (admin != null)
                    admin.SendChat("ban_feedback", names.CharacterName, callerName.CharacterName, timeLocalized);
                foreach (SteamPlayer pl in Provider.clients)
                {
                    if (pl.playerID.steamID.m_SteamID != Admin)
                    {
                        timeLocalized = DurationMins.GetTimeFromMinutes(pl.playerID.steamID.m_SteamID);
                        pl.SendChat("ban_broadcast" + (Admin == 0 ? "_operator" : string.Empty), names.CharacterName, callerName.CharacterName, timeLocalized);
                    }
                }
            }
        }
    }
}