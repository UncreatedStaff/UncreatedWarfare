using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using SteamGameServerNetworkingUtils = SDG.Unturned.SteamGameServerNetworkingUtils;

namespace Uncreated.Warfare;

public static class OffenseManager
{
    private const int HWIDS_COLUMN_SIZE = 161;
    public enum EBanResponse : byte
    {
        ALL_GOOD,
        BANNED_ON_CONNECTING_ACCOUNT,
        BANNED_ON_SAME_IP,
        BANNED_ON_SAME_HWID,
        UNABLE_TO_GET_IP
    }
    public static unsafe EBanResponse VerifyJoin(SteamPending player, ref string reason, ref int remainingDuration)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        EBanResponse state = EBanResponse.ALL_GOOD;
        byte[][] hwids = (byte[][])player.playerID.GetHwids();
        string? banreason = null;
        if (!player.transportConnection.TryGetIPv4Address(out uint ipv4))
        {
            state = EBanResponse.UNABLE_TO_GET_IP;
            goto State;
        }
        byte[] buffer = new byte[HWIDS_COLUMN_SIZE];
        remainingDuration = -2;
        Data.DatabaseManager.Query("SELECT `Violator`, `HWIDs`, `IP`, `Reason` FROM `ban_offenses` WHERE `Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60 > `Duration`;",
            new object[1] { player.playerID.steamID.m_SteamID },
            (R) =>
            {
                ulong steam64 = R.GetUInt64(0);
                banreason = R.GetString(3);
                if (player.playerID.steamID.m_SteamID == steam64)
                {
                    state = EBanResponse.BANNED_ON_CONNECTING_ACCOUNT;
                    return true;
                }
                long i = R.GetBytes(1, 0, buffer, 0, HWIDS_COLUMN_SIZE);
                if (i == 0)
                    return false;
                i = buffer[0];
                if (i == 0) return false;
                i--;
                for (; i >= 0L; i--)
                {
                    fixed (byte* ptr = &buffer[1 + i * 20])
                    {
                        fixed (byte* ptr2 = hwids[i])
                        {
                            for (int i2 = 0; i2 < 20; i2++)
                                if (*(ptr + i2) != *(ptr2 + i2))
                                    goto DifferentHWIDs;
                        }
                    }
                }
                state = EBanResponse.BANNED_ON_SAME_HWID;
                return true;
                DifferentHWIDs:
                uint ip = R.GetUInt32(2);
                if (ip == ipv4)
                {
                    state = EBanResponse.BANNED_ON_SAME_IP;
                    return true;
                }
                return false;
            });
        State:  
        switch (state)
        {
            default:
            case EBanResponse.ALL_GOOD:
                reason = string.Empty;
                AssertLoginInformation(player, ipv4, hwids);
                break;
            case EBanResponse.BANNED_ON_CONNECTING_ACCOUNT:
                string dur;
                if (remainingDuration < 0)
                    dur = "permanently";
                else 
                    dur = "for another " + Translation.GetTimeFromMinutes(remainingDuration, player.playerID.steamID.m_SteamID);
                if (!string.IsNullOrEmpty(banreason))
                    dur = "because \"" + banreason + "\" " + dur;
                reason = "You're currently banned " + dur + ", talk to the Directors in discord to appeal at: \"https://discord.gg/" + UCWarfare.Config.DiscordInviteCode + "\"";
                AssertLoginInformation(player, ipv4, hwids);
                break;
            case EBanResponse.BANNED_ON_SAME_IP:
            case EBanResponse.BANNED_ON_SAME_HWID:
                if (remainingDuration < 0)
                    dur = "permanently";
                else
                    dur = "for another " + Translation.GetTimeFromMinutes(remainingDuration, player.playerID.steamID.m_SteamID);
                if (!string.IsNullOrEmpty(banreason))
                    dur = "because \"" + banreason + "\" " + dur;
                reason = "You're currently banned " + dur + " on another account, talk to the Directors in discord to appeal at: \"https://discord.gg/" + UCWarfare.Config.DiscordInviteCode + "\"";
                AssertLoginInformation(player, ipv4, hwids);
                break;
            case EBanResponse.UNABLE_TO_GET_IP:
                reason = "We were unable to verify your ban information, contact a Director if this keeps happening at: \"https://discord.gg/" + UCWarfare.Config.DiscordInviteCode + "\"";
                break;
        }
        return state;
    }
    public static unsafe void AssertLoginInformation(SteamPending player, uint ipv4, byte[][] hwids)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        byte[] hwidsList = new byte[HWIDS_COLUMN_SIZE];
        byte[] searchBuffer = new byte[HWIDS_COLUMN_SIZE];
        int len = hwids.Length;
        hwidsList[0] = (byte)len;
        for (int i = 0; i < len; i++)
            Buffer.BlockCopy(hwids[i], 0, hwidsList, 1 + i * 20, 20);
        int ct = 0;
        bool entered = false;
        List<ulong> S64s = new List<ulong>(2);
        Data.DatabaseManager.Query(
            "SELECT `HWIDs`, `IP` FROM `anti_alt` WHERE `Steam64` = @0;",
            new object[1] { player.playerID.steamID.m_SteamID },
            (R) =>
            {
                ct++;
                if (!entered)
                {
                    uint ip = R.GetUInt32(1);
                    if (ip == 0) return;
                    if (ip == ipv4)
                    {
                        long length = R.GetBytes(0, 0L, searchBuffer, 0, HWIDS_COLUMN_SIZE);
                        if (length == 0) return;
                        length = searchBuffer[0];
                        length--;
                        for (; length >= 0L; length--)
                        {
                            fixed (byte* ptr = &searchBuffer[1 + length * 20])
                            {
                                fixed (byte* ptr2 = hwids[length])
                                {
                                    for (int i2 = 0; i2 < 20; i2++)
                                        if (*(ptr + i2) != *(ptr2 + i2))
                                            return;
                                }
                            }
                        }
                        entered = true;
                    }
                }
                return;
            });
        if (entered) return;
        Data.DatabaseManager.NonQuery(
            "INSERT INTO `anti_alt` (`Steam64`, `EntryID`, `HWIDs`, `IP`) VALUES (@0, @1, @2, @3) ON DUPLICATE KEY UPDATE `HWIDs` = @2, `IP` = @3;",
            new object[4]
            {
                player.playerID.steamID.m_SteamID, ct, hwidsList, ipv4
            });
    }
    public static async Task ApplyMuteSettings(UCPlayer joining)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (joining == null) return;
        string? reason = null;
        int duration = -2;
        DateTime timestamp = DateTime.MinValue;
        EMuteType type = EMuteType.NONE;
        await Data.DatabaseManager.QueryAsync(
            "SELECT `Reason`, `Duration`, `Timestamp`, `Type` FROM `muted` WHERE `Steam64` = @0 AND `Deactivated` = 0 AND " +
            "(`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60 > `Duration`) ORDER BY (TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60) - `Duration` LIMIT 1;",
            new object[1] { joining.Steam64 },
            R =>
            {
                reason = R.GetString(0);
                duration = R.GetInt32(1);
                timestamp = R.GetDateTime(2);
                type = (EMuteType)R.GetByte(3);
            }
        );
        if (type == EMuteType.NONE) return;
        DateTime now = DateTime.Now;
        DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : timestamp + TimeSpan.FromMinutes(duration);
        joining.TimeUnmuted = unmutedTime;
        joining.MuteReason = reason;
        joining.MuteType = type;
    }

    public static bool IsValidSteam64ID(ulong id)
    {
        return id / 100000000000000ul == 765;
    }
    public static bool IsValidSteam64ID(CSteamID id)
    {
        return id.m_SteamID / 100000000000000ul == 765;
    }
    public static void BanPlayer(ulong targetId, ulong callerId, string reason, int duration)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        UCPlayer? caller = UCPlayer.FromID(callerId);
        FPlayerName name;
        FPlayerName callerName;
        uint ipv4;
        if (target is not null) // player is online
        {
            CSteamID id = target.Player.channel.owner.playerID.steamID;
            ipv4 = SteamGameServerNetworkingUtils.getIPv4AddressOrZero(id);
            name = F.GetPlayerOriginalNames(target);
            Provider.requestBanPlayer(Provider.server, id, ipv4, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration) * 60);
        }
        else
        {
            ipv4 = Data.DatabaseManager.GetPackedIP(targetId);
            name = F.GetPlayerOriginalNames(targetId);
            F.OfflineBan(targetId, ipv4, caller is null ? CSteamID.Nil : caller.Player.channel.owner.playerID.steamID,
                reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration) * 60);
        }
        if (callerId != 0)
            callerName = F.GetPlayerOriginalNames(callerId);
        else
            callerName = FPlayerName.Console;
        ActionLog.Add(EActionLogType.BAN_PLAYER, $"BANNED {targetId.ToString(Data.Locale)} FOR \"{reason}\" DURATION: " +
            (duration == -1 ? "PERMANENT" : duration.ToString(Data.Locale)), callerId);
        if (UCWarfare.Config.AdminLoggerSettings.LogBans)
        {
            Data.DatabaseManager.AddBan(targetId, callerId, duration, reason!);
            NetCalls.SendPlayerBanned.NetInvoke(targetId, callerId, reason!, duration, DateTime.Now);
        }
        if (duration == -1)
        {
            if (callerId == 0)
            {
                L.Log(Translation.Translate("ban_permanent_console_operator", JSONMethods.DEFAULT_LANGUAGE, out _, name.PlayerName, targetId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                Chat.Broadcast("ban_permanent_broadcast_operator", name.CharacterName);
            }
            else
            {
                L.Log(Translation.Translate("ban_permanent_console", 0, out _, name.PlayerName, targetId.ToString(Data.Locale), callerName.PlayerName,
                    callerId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                Chat.BroadcastToAllExcept(callerId, "ban_permanent_broadcast", name.CharacterName, callerName.CharacterName);
                caller?.SendChat("ban_permanent_feedback", name.CharacterName);
            }
        }
        else
        {
            string time = Translation.GetTimeFromMinutes(duration, JSONMethods.DEFAULT_LANGUAGE);
            if (callerId == 0)
            {
                L.Log(Translation.Translate("ban_console_operator", JSONMethods.DEFAULT_LANGUAGE, out _, name.PlayerName, targetId.ToString(Data.Locale), reason!, time), ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in Translation.EnumerateLanguageSets())
                {
                    if (f || !set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromMinutes(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, "ban_broadcast_operator", name.PlayerName, time);
                }
            }
            else
            {
                L.Log(Translation.Translate("ban_console", 0, out _, name.PlayerName, targetId.ToString(Data.Locale), callerName.PlayerName,
                    callerId.ToString(Data.Locale), reason!, time), ConsoleColor.Cyan);
                bool f = false;
                foreach (LanguageSet set in Translation.EnumerateLanguageSetsExclude(callerId))
                {
                    if (f || !set.Language.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                    {
                        time = duration.GetTimeFromMinutes(set.Language);
                        f = true;
                    }
                    Chat.Broadcast(set, "ban_broadcast", name.CharacterName, callerName.CharacterName, time);
                }
                if (f)
                    time = duration.GetTimeFromMinutes(callerId);
                else if (Data.Languages.TryGetValue(callerId, out string lang) && !lang.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
                    time = duration.GetTimeFromMinutes(lang);
                caller?.SendChat("ban_feedback", name.CharacterName, time);
            }
        }
    }
    public static void KickPlayer(ulong target, ulong caller, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? targetPlayer = UCPlayer.FromID(target);
        if (targetPlayer is null)
            return;
        FPlayerName names = F.GetPlayerOriginalNames(target);
        Provider.kick(targetPlayer.Player.channel.owner.playerID.steamID, reason);
        if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
        {
            NetCalls.SendPlayerKicked.NetInvoke(target, caller, reason, DateTime.Now);
            Data.DatabaseManager.AddKick(target, caller, reason);
        }
        ActionLog.Add(EActionLogType.KICK_PLAYER, $"KICKED {target.ToString(Data.Locale)} FOR \"{reason}\"", caller);
        if (caller == 0)
        {
            L.Log(Translation.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName, target.ToString(Data.Locale), reason), ConsoleColor.Cyan);
            Chat.Broadcast("kick_kicked_broadcast_operator", names.CharacterName);
        }
        else
        {
            FPlayerName callerNames = caller == 0 ? FPlayerName.Console : F.GetPlayerOriginalNames(caller);
            L.Log(Translation.Translate("kick_kicked_console", 0, out _, names.PlayerName, target.ToString(Data.Locale),
                callerNames.PlayerName, caller.ToString(Data.Locale), reason), ConsoleColor.Cyan);
            Chat.BroadcastToAllExcept(caller, "kick_kicked_broadcast", names.CharacterName, callerNames.CharacterName);
            UCPlayer? callerPlayer = UCPlayer.FromID(caller);
            callerPlayer?.SendChat("kick_kicked_feedback", names.CharacterName);
        }
    }
    public static void UnbanPlayer(ulong targetId, ulong callerId)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName targetNames = F.GetPlayerOriginalNames(targetId);
        if (!Provider.requestUnbanPlayer(callerId == 0 ? CSteamID.Nil : new CSteamID(callerId), new CSteamID(targetId)))
            return;
        if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
        {
            Data.DatabaseManager.AddUnban(targetId, callerId);
            NetCalls.SendPlayerUnbanned.NetInvoke(targetId, callerId, DateTime.Now);
        }
        string tid = targetId.ToString(Data.Locale);
        ActionLog.Add(EActionLogType.UNBAN_PLAYER, $"UNBANNED {tid}", callerId);
        if (callerId == 0)
        {
            if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
            {
                L.Log(Translation.Translate("unban_unbanned_console_id_operator", 0, out _, tid), ConsoleColor.Cyan);
                Chat.Broadcast("unban_unbanned_broadcast_id_operator", tid);
            }
            else
            {
                L.Log(Translation.Translate("unban_unbanned_console_name_operator", 0, out _, targetNames.PlayerName, tid.ToString(Data.Locale)), ConsoleColor.Cyan);
                Chat.Broadcast("unban_unbanned_broadcast_name_operator", targetNames.CharacterName);
            }
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(callerId);
            UCPlayer? caller = UCPlayer.FromID(callerId);
            if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
            {
                L.Log(Translation.Translate("unban_unbanned_console_id", 0, out _, tid, callerNames.PlayerName, callerId.ToString(Data.Locale)), ConsoleColor.Cyan);
                caller?.SendChat("unban_unbanned_feedback_id", tid);
                Chat.BroadcastToAllExcept(callerId, "unban_unbanned_broadcast_id", tid, callerNames.CharacterName);
            }
            else
            {
                L.Log(Translation.Translate("unban_unbanned_console_name", 0, out _, targetNames.PlayerName, tid, callerNames.PlayerName, callerId.ToString(Data.Locale)), ConsoleColor.Cyan);
                caller?.SendChat("unban_unbanned_feedback_name", targetNames.CharacterName);
                Chat.BroadcastToAllExcept(callerId, "unban_unbanned_broadcast_name", targetNames.CharacterName, callerNames.CharacterName);
            }
        }
    }
    public static void WarnPlayer(ulong targetId, ulong callerId, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        if (target is null)
            return;
        UCPlayer? caller = UCPlayer.FromID(callerId);
        FPlayerName targetNames = F.GetPlayerOriginalNames(target);
        Provider.kick(target.Player.channel.owner.playerID.steamID, reason!);
        if (UCWarfare.Config.AdminLoggerSettings.LogWarning)
        {
            Data.DatabaseManager.AddWarning(targetId, callerId, reason!);
            NetCalls.SendPlayerWarned.NetInvoke(targetId, callerId, reason!, DateTime.Now);
        }
        string tid = targetId.ToString(Data.Locale);
        ActionLog.Add(EActionLogType.WARN_PLAYER, $"WARNED {tid} FOR \"{reason}\"", callerId);
        if (callerId == 0)
        {
            L.Log(Translation.Translate("warn_warned_console_operator", 0, out _, targetNames.PlayerName, tid, reason!), ConsoleColor.Cyan);
            Chat.BroadcastToAllExcept(targetId, "warn_warned_broadcast_operator", targetNames.CharacterName);
            ToastMessage.QueueMessage(target, new ToastMessage(Translation.Translate("warn_warned_private_operator", target, out _, reason!), EToastMessageSeverity.WARNING));
            target.SendChat("warn_warned_private_operator", reason!);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(callerId);
            L.Log(Translation.Translate("warn_warned_console", 0, out _, targetNames.PlayerName, tid, callerNames.PlayerName, callerId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
            Chat.BroadcastToAllExcept(new ulong[2] { targetId, callerId }, "warn_warned_broadcast", targetNames.CharacterName, callerNames.CharacterName);
            caller?.SendChat("warn_warned_feedback", targetNames.CharacterName);
            ToastMessage.QueueMessage(target, new ToastMessage(Translation.Translate("warn_warned_private", target, out _, callerNames.CharacterName, reason!), EToastMessageSeverity.WARNING));
            target.SendChat("warn_warned_private", callerNames.CharacterName, reason!);
        }
    }
    public static void MutePlayer(ulong target, ulong admin, EMuteType type, int duration, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? muted = UCPlayer.FromID(target);
        UCPlayer? muter = UCPlayer.FromID(admin);
        Task.Run(async () => {
            DateTime now = DateTime.Now;
            await Data.DatabaseManager.NonQueryAsync(
                "INSERT INTO `muted` (`Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`) VALUES (@0, @1, @2, @3, @4);",
                new object[] { target, admin, reason, duration, now, (byte)type });
            FPlayerName names = await F.GetPlayerOriginalNamesAsync(target);
            FPlayerName names2 = await F.GetPlayerOriginalNamesAsync(admin);
            await UCWarfare.ToUpdate();
            DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : now + TimeSpan.FromMinutes(duration);
            if (muted is not null && muted.TimeUnmuted < unmutedTime)
            {
                muted.TimeUnmuted = unmutedTime;
                muted.MuteReason = reason;
                muted.MuteType = type;
            }
            NetCalls.SendPlayerMuted.NetInvoke(target, admin, type, duration, reason, now);
            string dur = duration == -1 ? "PERMANENT" : duration.GetTimeFromMinutes(0);
            ActionLog.Add(EActionLogType.MUTE_PLAYER, $"MUTED {target} FOR \"{reason}\" DURATION: " + dur);

            if (muter == null)
            {
                if (duration == -1)
                {
                    foreach (LanguageSet set in Translation.EnumerateLanguageSets(target, admin))
                    {
                        string e = Translation.TranslateEnum(type, set.Language);
                        while (set.MoveNext())
                        {
                            Chat.Broadcast(set, "mute_broadcast_operator_permanent", names.CharacterName, e);
                        }
                    }
                    L.Log(Translation.Translate("mute_feedback", 0, out _, names.PlayerName, target.ToString(),
                        dur, Translation.TranslateEnum(type, 0), reason));
                }
                else
                {
                    foreach (LanguageSet set in Translation.EnumerateLanguageSets(target, admin))
                    {
                        string e = Translation.TranslateEnum(type, set.Language);
                        while (set.MoveNext())
                        {
                            Chat.Broadcast(set, "mute_broadcast_operator", names.CharacterName, e, dur);
                        }
                    }
                    L.Log(Translation.Translate("mute_feedback_permanent", 0, out _, names.PlayerName, target.ToString(),
                        Translation.TranslateEnum(type, 0), reason));
                }
            }
            else
            {
                if (duration == -1)
                {
                    foreach (LanguageSet set in Translation.EnumerateLanguageSets(target, admin))
                    {
                        string e = Translation.TranslateEnum(type, set.Language);
                        while (set.MoveNext())
                        {
                            Chat.Broadcast(set, "mute_broadcast_permanent", names.CharacterName, names2.CharacterName, e);
                        }
                    }
                    muter.SendChat("mute_feedback_permanent", names.PlayerName, target.ToString(), Translation.TranslateEnum(type, admin));
                }
                else
                {
                    foreach (LanguageSet set in Translation.EnumerateLanguageSets(target, admin))
                    {
                        string e = Translation.TranslateEnum(type, set.Language);
                        while (set.MoveNext())
                        {
                            Chat.Broadcast(set, "mute_broadcast", names.CharacterName, names2.CharacterName, e, dur);
                        }
                    }
                    muter.SendChat("mute_feedback", names.PlayerName, target.ToString(), dur, Translation.TranslateEnum(type, admin));
                }
            }
            if (muted != null)
            {
                if (admin == 0)
                {
                    if (duration == -1)
                        muted.SendChat("mute_dm_operator_permanent", reason, Translation.TranslateEnum(type, muted));
                    else
                        muted.SendChat("mute_dm_operator", reason, dur, Translation.TranslateEnum(type, muted));
                }
                else
                {
                    if (duration == -1)
                        muted.SendChat("mute_dm_permanent", names2.CharacterName, reason, Translation.TranslateEnum(type, muted));
                    else
                        muted.SendChat("mute_dm", names2.CharacterName, reason, dur, Translation.TranslateEnum(type, muted));
                }
            }
        }).ConfigureAwait(false);
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, int, DateTime> SendBanRequest = new NetCall<ulong, ulong, string, int, DateTime>(ReceiveBanRequest);
        public static readonly NetCall<ulong, ulong, DateTime> SendUnbanRequest = new NetCall<ulong, ulong, DateTime>(ReceiveUnbanRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendKickRequest = new NetCall<ulong, ulong, string, DateTime>(ReceieveKickRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendWarnRequest = new NetCall<ulong, ulong, string, DateTime>(ReceiveWarnRequest);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTime> SendMuteRequest = new NetCall<ulong, ulong, EMuteType, int, string, DateTime>(ReceieveMuteRequest);
        public static readonly NetCall<ulong> GrantAdminRequest = new NetCall<ulong>(ReceiveGrantAdmin);
        public static readonly NetCall<ulong> RevokeAdminRequest = new NetCall<ulong>(ReceiveRevokeAdmin);
        public static readonly NetCall<ulong> GrantInternRequest = new NetCall<ulong>(ReceiveGrantIntern);
        public static readonly NetCall<ulong> RevokeInternRequest = new NetCall<ulong>(ReceiveRevokeIntern);
        public static readonly NetCall<ulong> GrantHelperRequest = new NetCall<ulong>(ReceiveGrantHelper);
        public static readonly NetCall<ulong> RevokeHelperRequest = new NetCall<ulong>(ReceiveRevokeHelper);

        public static readonly NetCall<ulong, ulong, string, int, DateTime> SendPlayerBanned = new NetCall<ulong, ulong, string, int, DateTime>(1001);
        public static readonly NetCall<ulong, ulong, DateTime> SendPlayerUnbanned = new NetCall<ulong, ulong, DateTime>(1002);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerKicked = new NetCall<ulong, ulong, string, DateTime>(1003);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerWarned = new NetCall<ulong, ulong, string, DateTime>(1004);
        public static readonly NetCall<ulong, string, DateTime> SendPlayerBattleyeKicked = new NetCall<ulong, string, DateTime>(1005);
        public static readonly NetCall<ulong, ulong, string, string, DateTime> SendTeamkill = new NetCall<ulong, ulong, string, string, DateTime>(1006);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTime> SendPlayerMuted = new NetCall<ulong, ulong, EMuteType, int, string, DateTime>(1027);
        public static readonly NetCall<ulong, ushort, string, DateTime> SendVehicleTeamkilled = new NetCall<ulong, ushort, string, DateTime>(1112);

        [NetCall(ENetCall.FROM_SERVER, 1007)]
        internal static async Task ReceiveBanRequest(MessageContext context, ulong target, ulong admin, string reason, int duration, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            BanPlayer(target, admin, reason, duration);
        }

        [NetCall(ENetCall.FROM_SERVER, 1009)]
        internal static async Task ReceiveUnbanRequest(MessageContext context, ulong target, ulong admin, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            UnbanPlayer(target, admin);
        }

        [NetCall(ENetCall.FROM_SERVER, 1010)]
        internal static async Task ReceieveKickRequest(MessageContext context, ulong target, ulong admin, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            KickPlayer(target, admin, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1028)]
        internal static async Task ReceieveMuteRequest(MessageContext context, ulong target, ulong admin, EMuteType type, int duration, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            MutePlayer(target, admin, type, duration, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1011)]
        internal static async Task ReceiveWarnRequest(MessageContext context, ulong target, ulong admin, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            WarnPlayer(target, admin, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1103)]
        internal static void ReceiveGrantAdmin(MessageContext context, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
            if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
            {
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1104)]
        internal static void ReceiveRevokeAdmin(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1105)]
        internal static void ReceiveGrantIntern(MessageContext context, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
            if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
            {
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1106)]
        internal static void ReceiveRevokeIntern(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1107)]
        internal static void ReceiveGrantHelper(MessageContext context, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
            if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
            {
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
                R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
            }
        }

        [NetCall(ENetCall.FROM_SERVER, 1108)]
        internal static void ReceiveRevokeHelper(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }
        private static void RevokeAll(ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString(Data.Locale));
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
        }
    }
}