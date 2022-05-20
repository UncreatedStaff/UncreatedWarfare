using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Networking;
using UnityEngine;

namespace Uncreated.Warfare;

public static class OffenseManager
{
    private const int HWIDS_COLUMN_SIZE = 161;
    public static async Task BanPlayer(ulong offender, ulong banner, int duration, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (duration == 0)
        {
            UCPlayer? admin2 = UCPlayer.FromID(banner);
            if (admin2 == null)
                L.Log(Translation.Translate("ban_invalid_number_console", 0, "0"));
            else
                admin2.SendChat("ban_invalid_number", "0");
            return;
        }
        UCPlayer? admin = UCPlayer.FromID(banner);

        if (!IsValidSteam64ID(offender))
        {
            if (admin == null)
                L.Log(Translation.Translate("ban_no_player_found_console", 0, offender.ToString(Data.Locale)));
            else
                admin.SendChat("ban_no_player_found", offender.ToString(Data.Locale));
            return;
        }

        UCPlayer? bannedPlayer = UCPlayer.FromID(offender);
        
        if (bannedPlayer == null)
        {
            uint ip = 0;
            List<byte[][]> hwidses = new List<byte[][]>(8);
            byte[] buffer = new byte[161];
            await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `ban_offenses` WHERE `Violator` = @0;",
                new object[1] { offender },
                (R) =>
                {
                    ip = R.GetUInt32(0);
                    long i = R.GetBytes(1, 0L, buffer, 0, HWIDS_COLUMN_SIZE);
                    if (i != 0)
                    {
                        i = buffer[0];
                        byte[][] hwids = new byte[i][];
                        i--;
                        for (; i >= 0L; i--)
                        {
                            hwids[i] = new byte[20];
                            Buffer.BlockCopy(buffer, 1 + (int)i * 20, hwids[i], 0, 20);
                        }
                        hwidses.Add(hwids);
                    }
                });
            if (ip == 0)
            {
                await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `logindata` WHERE `Steam64` = @0 LIMIT 1;",
                    new object[1] { offender },
                    (R) =>
                    {
                        ip = R.GetUInt32(0);
                        long i = R.GetBytes(1, 0L, buffer, 0, HWIDS_COLUMN_SIZE);
                        if (i != 0)
                        {
                            i = buffer[0];
                            byte[][] hwids = new byte[i][];
                            for (; i >= 0L; i--)
                            {
                                hwids[i] = new byte[20];
                                Buffer.BlockCopy(buffer, 1 + (int)i * 20, hwids[i], 0, 20);
                            }
                            hwidses.Add(hwids);
                        }
                    });
            }
            if (hwidses.Count == 0) hwidses.Add(new byte[0][]);
            await UCWarfare.ToUpdate();
            
            Provider.requestBanPlayer(new CSteamID(banner), new CSteamID(offender), ip, reason, duration < 0 ? uint.MaxValue : (uint)duration * 60u);

            await UCWarfare.ToPool();
            await Data.DatabaseManager.NonQueryAsync(
                "INSERT INTO `ban_offenses` (`Violator`, `Admin`, `Reason`, `Duration`, `HWIDs`, `IP`, `Timestamp`) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                new object[7]
                {
                    offender, banner, reason, duration, hwidses[0], ip, DateTime.Now
                });
        }
        else
        {
            List<uint> ips = new List<uint>(2);
            uint ip = bannedPlayer.Player.channel.owner.getIPv4AddressOrZero();
            if (ip == 0)
            {
                await Data.DatabaseManager.QueryAsync("SELECT `IP` FROM `ban_offenses` WHERE `Violator` = @0 LIMIT 1;",
                    new object[1] { offender },
                    (R) =>
                    {
                        ip = R.GetUInt32(0);
                        return true;
                    });
                if (ip == 0)
                {
                    await Data.DatabaseManager.QueryAsync("SELECT `IP`, `HWIDs` FROM `logindata` WHERE `Steam64` = @0 LIMIT 1;",
                        new object[1] { offender },
                        (R) =>
                        {
                            ip = R.GetUInt32(0);
                            return true;
                        });
                }
                await UCWarfare.ToUpdate();
            }

            Provider.requestBanPlayer(new CSteamID(banner), new CSteamID(offender), ip, reason, duration < 0 ? uint.MaxValue : (uint)duration * 60u);

            await UCWarfare.ToPool();
            byte[][] hwids = (byte[][])bannedPlayer.Player.channel.owner.playerID.GetHwids();
            byte[] inhwids = new byte[HWIDS_COLUMN_SIZE];
            inhwids[0] = (byte)hwids.Length;
            for (int i = 0; i < hwids.Length; i++)
                Buffer.BlockCopy(hwids[i], 0, inhwids, 1 + i * 20, 20);
            await Data.DatabaseManager.NonQueryAsync(
                "INSERT INTO `ban_offenses` (`Violator`, `Admin`, `Reason`, `Duration`, `HWIDs`, `IP`, `Timestamp`) VALUES (@0, @1, @2, @3, @4, @5, @6);",
                new object[7]
                {
                    offender, banner, reason, duration, inhwids, ip, DateTime.Now
                });
        }

        NetCalls.SendPlayerBanned.NetInvoke(offender, banner, reason, duration < 0 ? uint.MaxValue : (uint)duration, DateTime.Now);
        string timeLocalized = duration < 0 ? "a long time" : ((uint)duration).GetTimeFromMinutes(0);
        FPlayerName adminNames = await F.GetPlayerOriginalNamesAsync(banner);
        FPlayerName violatorNames = banner == 0 ? FPlayerName.Nil : await F.GetPlayerOriginalNamesAsync(offender);
        await UCWarfare.ToUpdate();
        string translation = "ban_console";
        if (banner == 0) translation += "_operator";
        L.Log(Translation.Translate(translation, 0, out _, violatorNames.PlayerName, offender.ToString(Data.Locale),
            adminNames.PlayerName, banner.ToString(Data.Locale), reason, timeLocalized), ConsoleColor.Cyan);
        if (admin != null)
            admin.SendChat("ban_feedback", violatorNames.CharacterName, timeLocalized);
        translation = "ban_";
        if (duration < 0) translation += "permanent_";
        translation += "broadcast";
        if (banner == 0) translation += "_operator";
        foreach (LanguageSet set in Translation.EnumerateLanguageSets(x => x.Steam64 != banner))
        {
            if (duration < 0)
            {
                while (set.MoveNext())
                {
                    set.Next.SendChat(translation, violatorNames.CharacterName, adminNames.CharacterName);
                }
            }
            else
            {
                timeLocalized = duration < 0 ? "a long time" : ((uint)duration).GetTimeFromMinutes(set.Language);
                while (set.MoveNext())
                {
                    set.Next.SendChat(translation, violatorNames.CharacterName, adminNames.CharacterName, timeLocalized);
                }
            }
        }
    }
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
                    dur = "for another " + Translation.GetTimeFromMinutes((uint)remainingDuration, player.playerID.steamID.m_SteamID);
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
                    dur = "for another " + Translation.GetTimeFromMinutes((uint)remainingDuration, player.playerID.steamID.m_SteamID);
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
    public static async Task KickPlayer(ulong offender, ulong kicker, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? admin = UCPlayer.FromID(kicker);

        if (!IsValidSteam64ID(offender))
            goto NoPlayer;
        UCPlayer? bannedPlayer = UCPlayer.FromID(offender);
        if (bannedPlayer == null)
            goto NoPlayer;


        FPlayerName adminNames = kicker == 0 ? FPlayerName.Nil : await F.GetPlayerOriginalNamesAsync(kicker);
        FPlayerName violatorNames = await F.GetPlayerOriginalNamesAsync(offender);

        await UCWarfare.ToUpdate();

        Provider.kick(bannedPlayer.Player.channel.owner.playerID.steamID, reason);
        string translation = "kick_kicked_console";
        if (kicker == 0) translation += "_operator";
        L.Log(Translation.Translate(translation, 0, out _, violatorNames.PlayerName, offender.ToString(Data.Locale),
            adminNames.PlayerName, kicker.ToString(Data.Locale), reason), ConsoleColor.Cyan);
        if (admin != null)
            admin.SendChat("kick_kicked_feedback", violatorNames.CharacterName);
        translation = "kick_kicked_broadcast";
        if (kicker == 0)
        {
            translation += "_operator";
            Chat.BroadcastToAllExcept(new ulong[1] { kicker }, translation, violatorNames.CharacterName, adminNames.CharacterName);
        }
        else
            Chat.Broadcast(translation, violatorNames.CharacterName, adminNames.CharacterName);
        return;

        NoPlayer:
        if (admin == null)
            L.Log(Translation.Translate("kick_no_player_found_console", 0, offender.ToString(Data.Locale)));
        else
            admin.SendChat("kick_no_player_found", offender.ToString(Data.Locale));
        await UCWarfare.ToPool();

        DateTime now = DateTime.Now;
        await Data.DatabaseManager.NonQueryAsync(
            "INSERT INTO `kicks` (`Kicked`, `Kicker`, `Reason`, `Tiemstamp`) VALUES (@0, @1, @2, @3);",
            new object[4]
            {
                offender, kicker, reason, now
            });
        NetCalls.SendPlayerKicked.NetInvoke(offender, kicker, reason, now);
        await UCWarfare.ToUpdate();
    }

    public static async Task MutePlayer(UCPlayer? muted, ulong mutedS64, ulong adminid, EMuteType type, int duration, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        DateTime now = DateTime.Now;
        await Data.DatabaseManager.NonQueryAsync(
            "INSERT INTO `muted` (`Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`) VALUES (@0, @1, @2, @3, @4);",
            new object[]
                { mutedS64, adminid, reason, duration, now, (byte)type });
        DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : now + TimeSpan.FromMinutes(duration);
        if (muted != null && muted.TimeUnmuted < unmutedTime)
        {
            muted.TimeUnmuted = unmutedTime;
            muted.MuteReason = reason;
            muted.MuteType = type;
        }
        NetCalls.SendPlayerMuted.NetInvoke(mutedS64, adminid, (byte)type, duration, reason, now);
    }
    public static void ApplyMuteSettings(UCPlayer joining)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (joining == null) return;
        string? reason = null;
        int duration = -2;
        DateTime timestamp = DateTime.MinValue;
        EMuteType type = EMuteType.NONE;
        Data.DatabaseManager.Query(
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
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, uint, DateTime> SendBanRequest = new NetCall<ulong, ulong, string, uint, DateTime>(ReceiveBanRequest);
        public static readonly NetCall<ulong, ulong, DateTime> SendUnbanRequest = new NetCall<ulong, ulong, DateTime>(ReceiveUnbanRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendKickRequest = new NetCall<ulong, ulong, string, DateTime>(ReceieveKickRequest);
        public static readonly NetCall<ulong, ulong, byte, int, string, DateTime> SendMuteRequest = new NetCall<ulong, ulong, byte, int, string, DateTime>(ReceieveMuteRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendWarnRequest = new NetCall<ulong, ulong, string, DateTime>(ReceiveWarnRequest);
        public static readonly NetCall<ulong> GrantAdminRequest = new NetCall<ulong>(ReceiveGrantAdmin);
        public static readonly NetCall<ulong> RevokeAdminRequest = new NetCall<ulong>(ReceiveRevokeAdmin);
        public static readonly NetCall<ulong> GrantInternRequest = new NetCall<ulong>(ReceiveGrantIntern);
        public static readonly NetCall<ulong> RevokeInternRequest = new NetCall<ulong>(ReceiveRevokeIntern);
        public static readonly NetCall<ulong> GrantHelperRequest = new NetCall<ulong>(ReceiveGrantHelper);
        public static readonly NetCall<ulong> RevokeHelperRequest = new NetCall<ulong>(ReceiveRevokeHelper);

        public static readonly NetCall<ulong, ulong, string, uint, DateTime> SendPlayerBanned = new NetCall<ulong, ulong, string, uint, DateTime>(1001);
        public static readonly NetCall<ulong, ulong, DateTime> SendPlayerUnbanned = new NetCall<ulong, ulong, DateTime>(1002);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerKicked = new NetCall<ulong, ulong, string, DateTime>(1003);
        public static readonly NetCall<ulong, ulong, byte, int, string, DateTime> SendPlayerMuted = new NetCall<ulong, ulong, byte, int, string, DateTime>(1027);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerWarned = new NetCall<ulong, ulong, string, DateTime>(1004);
        public static readonly NetCall<ulong, string, DateTime> SendPlayerBattleyeKicked = new NetCall<ulong, string, DateTime>(1005);
        public static readonly NetCall<ulong, ulong, string, string, DateTime> SendTeamkill = new NetCall<ulong, ulong, string, string, DateTime>(1006);
        public static readonly NetCall<ulong, ushort, string, DateTime> SendVehicleTeamkilled = new NetCall<ulong, ushort, string, DateTime>(1112);

        [NetCall(ENetCall.FROM_SERVER, 1007)]
        internal static async Task ReceiveBanRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, uint DurationMins, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            BanOverrideCommand.BanPlayer(Violator, Admin, Reason, DurationMins);
        }

        [NetCall(ENetCall.FROM_SERVER, 1009)]
        internal static async Task ReceiveUnbanRequest(IConnection connection, ulong Violator, ulong Admin, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            UnbanOverrideCommand.UnbanPlayer(Violator, Admin);
        }

        [NetCall(ENetCall.FROM_SERVER, 1010)]
        internal static async Task ReceieveKickRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            KickOverrideCommand.KickPlayer(Violator, Admin, Reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1028)]
        internal static async Task ReceieveMuteRequest(IConnection connection, ulong violator, ulong admin, byte type, int duration, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            MuteCommand.MutePlayer(violator, admin, (EMuteType)type, duration, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1011)]
        internal static async Task ReceiveWarnRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            WarnCommand.WarnPlayer(Violator, Admin, Reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1103)]
        internal static void ReceiveGrantAdmin(IConnection connection, ulong player)
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
        internal static void ReceiveRevokeAdmin(IConnection connection, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
        }

        [NetCall(ENetCall.FROM_SERVER, 1105)]
        internal static void ReceiveGrantIntern(IConnection connection, ulong player)
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
        internal static void ReceiveRevokeIntern(IConnection connection, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
        }

        [NetCall(ENetCall.FROM_SERVER, 1107)]
        internal static void ReceiveGrantHelper(IConnection connection, ulong player)
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
        internal static void ReceiveRevokeHelper(IConnection connection, ulong player)
        {
            RocketPlayer pl = new RocketPlayer(player.ToString());
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
            R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
        }
    }
}