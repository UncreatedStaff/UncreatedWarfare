﻿using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using System;
using System.Collections.Generic;
using Uncreated.Networking;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.XP;

namespace Uncreated.Warfare.Networking
{
    public static class Invocations
    {
        /// <summary>1000 - 1099</summary>
        public static class Shared
        {
            internal static readonly NetCallRaw<FPlayerList[]> PlayerList = new NetCallRaw<FPlayerList[]>(1000, FPlayerList.ReadArray, FPlayerList.WriteArray);

            internal static readonly NetCall<ulong, ulong, string, uint, DateTime> LogBanned = new NetCall<ulong, ulong, string, uint, DateTime>(1001);

            internal static readonly NetCall<ulong, ulong, DateTime> LogUnbanned = new NetCall<ulong, ulong, DateTime>(1002);

            internal static readonly NetCall<ulong, ulong, string, DateTime> LogKicked = new NetCall<ulong, ulong, string, DateTime>(1003);

            internal static readonly NetCall<ulong, ulong, string, DateTime> LogWarned = new NetCall<ulong, ulong, string, DateTime>(1004);

            internal static readonly NetCall<ulong, string, DateTime> LogBattleyeKicked = new NetCall<ulong, string, DateTime>(1005);

            internal static readonly NetCall<ulong, ulong, string, string, DateTime> LogTeamkilled = new NetCall<ulong, ulong, string, string, DateTime>(1006);


            internal static readonly NetCall<ulong, ulong, string, uint, DateTime> TellBan = new NetCall<ulong, ulong, string, uint, DateTime>(1007);
            [NetCall(ENetCall.FROM_SERVER, 1007)]
            internal static void ReceiveBanRequest(in IConnection connection, ulong Violator, ulong Admin, string Reason, uint DurationMins, DateTime timestamp)
            {
                Commands.BanOverrideCommand.BanPlayer(Violator, Admin, Reason, DurationMins);
            }

            internal static readonly NetCall<ulong, ulong, string, uint, DateTime> TellIPBan = new NetCall<ulong, ulong, string, uint, DateTime>(1008);
            [NetCall(ENetCall.FROM_SERVER, 1008)]
            internal static void ReceiveIPBanRequest(in IConnection connection, ulong Violator, ulong Admin, string Reason, uint DurationMins, DateTime timestamp)
            {
                Commands.IPBanCommand.IPBanPlayer(Violator, Admin, Reason, DurationMins);
            }

            internal static readonly NetCall<ulong, ulong, DateTime> TellUnban = new NetCall<ulong, ulong, DateTime>(1009);
            [NetCall(ENetCall.FROM_SERVER, 1009)]
            internal static void ReceiveUnbanRequest(in IConnection connection, ulong Violator, ulong Admin, DateTime timestamp)
            {
                Commands.UnbanOverrideCommand.UnbanPlayer(Violator, Admin);
            }

            internal static readonly NetCall<ulong, ulong, string, DateTime> TellKick = new NetCall<ulong, ulong, string, DateTime>(1010);
            [NetCall(ENetCall.FROM_SERVER, 1010)]
            internal static void ReceiveUnbanRequest(in IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
            {
                Commands.KickOverrideCommand.KickPlayer(Violator, Admin, Reason);
            }

            internal static readonly NetCall<ulong, ulong, string, DateTime> TellWarn = new NetCall<ulong, ulong, string, DateTime>(1011);
            [NetCall(ENetCall.FROM_SERVER, 1011)]
            internal static void ReceiveWarnRequest(in IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
            {
                Commands.WarnCommand.WarnPlayer(Violator, Admin, Reason);
            }


            internal static readonly NetCall<ulong, string> ShuttingDown = new NetCall<ulong, string>(1012);

            internal static readonly NetCall<ulong, string> ShuttingDownAfter = new NetCall<ulong, string>(1013);

            internal static readonly NetCall<ulong> ShuttingDownCancel = new NetCall<ulong>(1014);

            internal static readonly NetCall<ulong, string, uint> ShuttingDownTime = new NetCall<ulong, string, uint>(1015);

            internal static readonly NetCallRaw<FPlayerList> PlayerJoined = new NetCallRaw<FPlayerList>(1016, FPlayerList.Read, FPlayerList.Write);

            internal static readonly NetCall<ulong> PlayerLeft = new NetCall<ulong>(1017);

            internal static readonly NetCall<ulong, bool> DutyChanged = new NetCall<ulong, bool>(1018);

            internal static readonly NetCall<ulong, byte> TeamChanged = new NetCall<ulong, byte>(1019);

            internal static readonly NetCall ShuttingDownAfterComplete = new NetCall(1020);

            internal static readonly NetCall RequestPlayerList = new NetCall(1021);
            [NetCall(ENetCall.FROM_SERVER, 1021)]
            internal static void ReceiveRequestPlayerList(in IConnection connection)
            {
                PlayerList.Invoke(connection, PlayerManager.GetPlayerList());
            }

            internal static readonly NetCall RequestPing = new NetCall(1022);
            [NetCall(ENetCall.FROM_SERVER, 1022)]
            internal static void ReceivePingRequest(in IConnection connection)
            {
                SendPing.Invoke(connection, DateTime.Now);
            }
            internal static readonly NetCall<DateTime> SendPing = new NetCall<DateTime>(1023);

            internal static readonly NetCall<ulong, bool> SetQueueSkip = new NetCall<ulong, bool>(1024);
            [NetCall(ENetCall.FROM_SERVER, 1024)]
            internal static void ReceiveSetQueueSkip(in IConnection connection, ulong player, bool status)
            {
                if (PlayerManager.HasSaveRead(player, out PlayerSave save))
                {
                    save.HasQueueSkip = status;
                }
                else
                {
                    PlayerManager.AddSave(new PlayerSave(player, 0, string.Empty, string.Empty, status, 0, false, false, false));
                }
            }
        }
        /// <summary>1100 - 1199</summary>
        public static class Warfare
        {
            internal static readonly NetCall<ulong, string> GiveKitAccess = new NetCall<ulong, string>(1100);
            [NetCall(ENetCall.FROM_SERVER, 1100)]
            internal static void ReceiveGiveKitAccess(in IConnection connection, ulong player, string kit) => KitManager.GiveAccess(player, kit);
            internal static readonly NetCall<ulong, string> RemoveKitAccess = new NetCall<ulong, string>(1101);
            [NetCall(ENetCall.FROM_SERVER, 1101)]
            internal static void ReceiveRemoveKitAccess(in IConnection connection, ulong player, string kit) => KitManager.RemoveAccess(player, kit);

            internal static readonly NetCall<ulong, int, EBranch> SetOfficerLevel = new NetCall<ulong, int, EBranch>(1102);
            [NetCall(ENetCall.FROM_SERVER, 1102)]
            internal static void ReceiveSetOfficerLevel(in IConnection connection, ulong player, int level, EBranch branch)
            {
                if (level == 0)
                {
                    if (OfficerManager.IsOfficer(player, out Officer officer))
                    {
                        Rank rank = OfficerManager.GetRankFromLevel(officer.officerLevel);
                        if (rank != null)
                            OfficerManager.DischargeOfficer(player, rank);
                    }
                    return;
                }
                Rank lvl = OfficerManager.GetRankFromLevel(level);
                if (lvl != null)
                {
                    OfficerManager.ChangeOfficerRank(player, lvl, branch);
                }
            }
            internal static readonly NetCall<ulong> GiveAdmin = new NetCall<ulong>(1103);

            [NetCall(ENetCall.FROM_SERVER, 1103)]
            internal static void ReceiveGiveAdmin(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
                {
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                    }
                    R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                }
            }

            internal static readonly NetCall<ulong> RevokeAdmin = new NetCall<ulong>(1104);

            [NetCall(ENetCall.FROM_SERVER, 1104)]
            internal static void ReceiveRevokeAdmin(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                }
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
                }
            }
            internal static readonly NetCall<ulong> GiveIntern = new NetCall<ulong>(1105);
            [NetCall(ENetCall.FROM_SERVER, 1105)]
            internal static void ReceiveGiveIntern(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
                {
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                    }
                    R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                }
            }
            internal static readonly NetCall<ulong> RevokeIntern = new NetCall<ulong>(1106);
            [NetCall(ENetCall.FROM_SERVER, 1106)]
            internal static void ReceiveRevokeIntern(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
                }
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
                }
            }
            internal static readonly NetCall<ulong> GiveHelper = new NetCall<ulong>(1107);
            [NetCall(ENetCall.FROM_SERVER, 1107)]
            internal static void ReceiveGiveHelper(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (!groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
                {
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                    }
                    if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup))
                    {
                        R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup, pl);
                    }
                    R.Permissions.AddPlayerToGroup(UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup, pl);
                }
            }
            internal static readonly NetCall<ulong> RevokeHelper = new NetCall<ulong>(1108);
            [NetCall(ENetCall.FROM_SERVER, 1108)]
            internal static void ReceiveRevokeHelper(in IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                }
            }
            internal static readonly NetCallRaw<Kit> CreateKit = new NetCallRaw<Kit>(1109, Kit.ReadKit, Kit.WriteKit);
            [NetCall(ENetCall.FROM_SERVER, 1109)]
            internal static void ReceiveCreateKit(in IConnection connection, Kit kit) => KitManager.CreateKit(kit);

            internal static readonly NetCall RequestRankInfo = new NetCall(1110);

            [NetCall(ENetCall.FROM_SERVER, 1110)]
            internal static void ReceiveRequestRankInfo(in IConnection connection)
            {
                SendRankInfo.Invoke(connection, XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks,
                    OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
            }

            internal static readonly NetCallRaw<Rank[], Rank[], int, int> SendRankInfo =
                new NetCallRaw<Rank[], Rank[], int, int>(1111, Rank.ReadMany, Rank.ReadMany,
                    (R) => R.ReadInt32(), (R) => R.ReadInt32(),
                    Rank.WriteMany, Rank.WriteMany,
                    (W, I) => W.Write(I), (W, I) => W.Write(I));


            internal static readonly NetCall<ulong, ushort, string, DateTime> LogFriendlyVehicleKill = new NetCall<ulong, ushort, string, DateTime>(1112);
        }
    }
}
