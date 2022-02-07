using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Kits;
using UnityEngine;

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

            internal static readonly NetCall<ulong, ulong, string, uint, DateTime> TellBan = new NetCall<ulong, ulong, string, uint, DateTime>(ReceiveBanRequest);
            [NetCall(ENetCall.FROM_SERVER, 1007)]
            internal static async Task ReceiveBanRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, uint DurationMins, DateTime timestamp)
            {
                await UCWarfare.ToUpdate();
                Commands.BanOverrideCommand.BanPlayer(Violator, Admin, Reason, DurationMins);
            }

            internal static readonly NetCall<ulong, ulong, DateTime> TellUnban = new NetCall<ulong, ulong, DateTime>(ReceiveUnbanRequest);
            [NetCall(ENetCall.FROM_SERVER, 1009)]
            internal static async Task ReceiveUnbanRequest(IConnection connection, ulong Violator, ulong Admin, DateTime timestamp)
            {
                await UCWarfare.ToUpdate();
                Commands.UnbanOverrideCommand.UnbanPlayer(Violator, Admin);
            }

            internal static readonly NetCall<ulong, ulong, string, DateTime> TellKick = new NetCall<ulong, ulong, string, DateTime>(ReceieveKickRequest);
            [NetCall(ENetCall.FROM_SERVER, 1010)]
            internal static async Task ReceieveKickRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
            {
                await UCWarfare.ToUpdate();
                Commands.KickOverrideCommand.KickPlayer(Violator, Admin, Reason);
            }

            internal static readonly NetCall<ulong, ulong, string, DateTime> TellWarn = new NetCall<ulong, ulong, string, DateTime>(ReceiveWarnRequest);
            [NetCall(ENetCall.FROM_SERVER, 1011)]
            internal static async Task ReceiveWarnRequest(IConnection connection, ulong Violator, ulong Admin, string Reason, DateTime timestamp)
            {
                await UCWarfare.ToUpdate();
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

            internal static readonly NetCall RequestPlayerList = new NetCall(ReceiveRequestPlayerList);
            [NetCall(ENetCall.FROM_SERVER, 1021)]
            internal static void ReceiveRequestPlayerList(IConnection connection)
            {
                PlayerList.Invoke(connection, PlayerManager.GetPlayerList());
            }

            internal static readonly NetCall RequestPing = new NetCall(ReceivePingRequest);
            [NetCall(ENetCall.FROM_SERVER, 1022)]
            internal static void ReceivePingRequest(IConnection connection)
            {
                SendPing.Invoke(connection, DateTime.Now);
            }
            internal static readonly NetCall<DateTime> SendPing = new NetCall<DateTime>(1023);

            internal static readonly NetCall<ulong, bool> SetQueueSkip = new NetCall<ulong, bool>(ReceiveSetQueueSkip);
            [NetCall(ENetCall.FROM_SERVER, 1024)]
            internal static void ReceiveSetQueueSkip(IConnection connection, ulong player, bool status)
            {
                if (PlayerSave.TryReadSaveFile(player, out PlayerSave save))
                {
                    save.HasQueueSkip = status;
                    PlayerSave.WriteToSaveFile(save);
                }
                else
                {
                    save = new PlayerSave(player, 0, string.Empty, string.Empty, status, 0, false, false);
                    PlayerSave.WriteToSaveFile(save);
                }
            }

            internal static readonly NetCall<ushort, EAssetType> RequestAssetName = new NetCall<ushort, EAssetType>(ReceiveRequestAssetName);
            [NetCall(ENetCall.FROM_SERVER, 1025)]
            internal static void ReceiveRequestAssetName(IConnection connection, ushort id, EAssetType type)
            {
                switch (type)
                {
                    default:
                    case EAssetType.NONE:
                        SendAssetName.Invoke(connection, id, type, string.Empty);
                        return;
                    case EAssetType.ITEM:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is ItemAsset iasset ? iasset.itemName : string.Empty);
                        return;
                    case EAssetType.VEHICLE:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is VehicleAsset vasset ? vasset.vehicleName : string.Empty);
                        return;
                    case EAssetType.OBJECT:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is ObjectAsset oasset ? oasset.objectName : string.Empty);
                        return;
                    case EAssetType.ANIMAL:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is AnimalAsset aasset ? aasset.animalName : string.Empty);
                        return;
                    case EAssetType.EFFECT:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is EffectAsset easset ? easset.GUID.ToString() : string.Empty);
                        return;
                    case EAssetType.MYTHIC:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is MythicAsset masset ? masset.GUID.ToString() : string.Empty);
                        return;
                    case EAssetType.NPC:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is ObjectNPCAsset npcasset ? npcasset.objectName : string.Empty);
                        return;
                    case EAssetType.RESOURCE:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is ResourceAsset rasset ? rasset.resourceName : string.Empty);
                        return;
                    case EAssetType.SKIN:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is SkinAsset sasset ? sasset.GUID.ToString() : string.Empty);
                        return;
                    case EAssetType.SPAWN:
                        SendAssetName.Invoke(connection, id, type, Assets.find(type, id) is SpawnAsset spasset ? spasset.GUID.ToString() : string.Empty);
                        return;
                }
            }
            internal static readonly NetCall<ushort, EAssetType, string> SendAssetName = new NetCall<ushort, EAssetType, string>(1026);



            internal static readonly NetCall RequestFullLog = new NetCall(ReceiveRequestFullLog);
            [NetCall(ENetCall.FROM_SERVER, 1029)]
            internal static void ReceiveRequestFullLog(IConnection connection)
            {
                SendFullLog.Invoke(connection, Data.Logs.ToArray(), 0);
                L.Log(Data.Logs.Count.ToString());
            }
            internal static readonly NetCallRaw<Log, byte> SendLogMessage =
                new NetCallRaw<Log, byte>(1030, Log.Read, R => R.ReadUInt8(), Log.Write, (W, B) => W.Write(B));
            internal static readonly NetCallRaw<Log[], byte> SendFullLog =
                new NetCallRaw<Log[], byte>(1031, Log.ReadMany, R => R.ReadUInt8(), Log.WriteMany, (W, B) => W.Write(B));
            internal static readonly NetCall<string> SendCommand = new NetCall<string>(ReceiveCommand);
            [NetCall(ENetCall.FROM_SERVER, 1032)]
            internal static void ReceiveCommand(IConnection connection, string command)
            {
                if (Thread.CurrentThread == ThreadUtil.gameThread)
                    RunCommand(command);
                else
                    UCWarfare.RunOnMainThread(() => RunCommand(command));
            }
            private static void RunCommand(string command)
            {
                L.Log(command, ConsoleColor.White);
                bool shouldExecuteCommand = true;
                try
                {
                    CommandWindow.onCommandWindowInputted?.Invoke(command, ref shouldExecuteCommand);
                }
                catch (Exception ex)
                {
                    L.LogError("Plugin threw an exception from onCommandWindowInputted:");
                    L.LogError(ex);
                }
                if (!shouldExecuteCommand || Commander.execute(Steamworks.CSteamID.Nil, command))
                    return;
                L.LogError($"Unable to match \"{command}\" with any built-in commands");
            }

            internal static readonly NetCall<ulong> RequestPermissions = new NetCall<ulong>(ReceivePermissionRequest);
            internal static readonly NetCall<EAdminType> SendPermissions = new NetCall<EAdminType>(1034);
            [NetCall(ENetCall.FROM_SERVER, 1033)]
            internal static void ReceivePermissionRequest(IConnection connection, ulong Steam64)
            {
                RocketPlayer player = new RocketPlayer(Steam64.ToString());
                EAdminType perms = player.GetPermissions();
                SendPermissions.Invoke(connection, perms);
            }

            internal static readonly NetCall<ulong> RequestPlayerIsOnline = new NetCall<ulong>(ReceivePlayerOnlineCheckRequest, 8);
            internal static readonly NetCall<bool> SendPlayerIsOnline = new NetCall<bool>(1036, 1);
            [NetCall(ENetCall.FROM_SERVER, 1035)]
            internal static void ReceivePlayerOnlineCheckRequest(IConnection connection, ulong Steam64)
            {
                SendPlayerIsOnline.Invoke(connection, PlayerTool.getSteamPlayer(Steam64) != null);
            }
        }
        /// <summary>1100 - 1199</summary>
        public static class Warfare
        {
            internal static readonly NetCall<ulong, string> GiveKitAccess = new NetCall<ulong, string>(ReceiveGiveKitAccess);
            [NetCall(ENetCall.FROM_SERVER, 1100)]
            internal static void ReceiveGiveKitAccess(IConnection connection, ulong player, string kit) => KitManager.GiveAccess(player, kit);
            internal static readonly NetCall<ulong, string> RemoveKitAccess = new NetCall<ulong, string>(ReceiveRemoveKitAccess);
            [NetCall(ENetCall.FROM_SERVER, 1101)]
            internal static void ReceiveRemoveKitAccess(IConnection connection, ulong player, string kit) => KitManager.RemoveAccess(player, kit);

            internal static readonly NetCall<ulong, int, EBranch> SetOfficerLevel = new NetCall<ulong, int, EBranch>(ReceiveSetOfficerLevel);
            [NetCall(ENetCall.FROM_SERVER, 1102)]
            internal static void ReceiveSetOfficerLevel(IConnection connection, ulong player, int level, EBranch branch)
            {
                //if (level == 0)
                //{
                //    if (OfficerManager.IsOfficer(player, out Officer officer))
                //    {
                //        Rank rank = OfficerManager.GetRankFromLevel(officer.officerLevel);
                //        if (rank != null)
                //            OfficerManager.DischargeOfficer(player, rank);
                //    }
                //    return;
                //}
                //Rank lvl = OfficerManager.GetRankFromLevel(level);
                //if (lvl != null)
                //{
                //    OfficerManager.ChangeOfficerRank(player, lvl, branch);
                //}
            }
            internal static readonly NetCall<ulong> GiveAdmin = new NetCall<ulong>(ReceiveGiveAdmin);

            [NetCall(ENetCall.FROM_SERVER, 1103)]
            internal static void ReceiveGiveAdmin(IConnection connection, ulong player)
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

            internal static readonly NetCall<ulong> RevokeAdmin = new NetCall<ulong>(ReceiveRevokeAdmin);

            [NetCall(ENetCall.FROM_SERVER, 1104)]
            internal static void ReceiveRevokeAdmin(IConnection connection, ulong player)
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
            internal static readonly NetCall<ulong> GiveIntern = new NetCall<ulong>(ReceiveGiveIntern);
            [NetCall(ENetCall.FROM_SERVER, 1105)]
            internal static void ReceiveGiveIntern(IConnection connection, ulong player)
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
            internal static readonly NetCall<ulong> RevokeIntern = new NetCall<ulong>(ReceiveRevokeIntern);
            [NetCall(ENetCall.FROM_SERVER, 1106)]
            internal static void ReceiveRevokeIntern(IConnection connection, ulong player)
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
            internal static readonly NetCall<ulong> GiveHelper = new NetCall<ulong>(ReceiveGiveHelper);
            [NetCall(ENetCall.FROM_SERVER, 1107)]
            internal static void ReceiveGiveHelper(IConnection connection, ulong player)
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
            internal static readonly NetCall<ulong> RevokeHelper = new NetCall<ulong>(ReceiveRevokeHelper);
            [NetCall(ENetCall.FROM_SERVER, 1108)]
            internal static void ReceiveRevokeHelper(IConnection connection, ulong player)
            {
                RocketPlayer pl = new RocketPlayer(player.ToString());
                List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(pl, false);
                if (groups.Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup))
                {
                    R.Permissions.RemovePlayerFromGroup(UCWarfare.Config.AdminLoggerSettings.HelperGroup, pl);
                }
            }
            internal static readonly NetCallRaw<Kit> CreateKit = new NetCallRaw<Kit>(ReceiveCreateKit, Kit.Read, Kit.Write);
            [NetCall(ENetCall.FROM_SERVER, 1109)]
            internal static void ReceiveCreateKit(IConnection connection, Kit kit) => KitManager.CreateKit(kit);

            internal static readonly NetCall RequestRankInfo = new NetCall(ReceiveRequestRankInfo);

            [NetCall(ENetCall.FROM_SERVER, 1110)]
            internal static void ReceiveRequestRankInfo(IConnection connection)
            {
                //SendRankInfo.Invoke(connection, XPManager.config.Data.Ranks, OfficerManager.config.Data.OfficerRanks,
                //    OfficerManager.config.Data.FirstStarPoints, OfficerManager.config.Data.PointsIncreasePerStar);
            }

            internal static readonly NetCallRaw<Rank[], Rank[], int, int> SendRankInfo =
                new NetCallRaw<Rank[], Rank[], int, int>(1111, Rank.ReadMany, Rank.ReadMany,
                    (R) => R.ReadInt32(), (R) => R.ReadInt32(),
                    Rank.WriteMany, Rank.WriteMany,
                    (W, I) => W.Write(I), (W, I) => W.Write(I));


            internal static readonly NetCall<ulong, ushort, string, DateTime> LogFriendlyVehicleKill = new NetCall<ulong, ushort, string, DateTime>(1112);

            internal static readonly NetCall<string> RequestKitClass = new NetCall<string>(ReceiveRequestKitClass);
            [NetCall(ENetCall.FROM_SERVER, 1113)]
            internal static void ReceiveRequestKitClass(IConnection connection, string kitID)
            {
                if (KitManager.KitExists(kitID, out Kit kit))
                {
                    string signtext = kit.Name;
                    if (!kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out signtext))
                        if (kit.SignTexts.Count > 0)
                            signtext = kit.SignTexts.Values.ElementAt(0);
                
                    SendKitClass.Invoke(connection, kitID, kit.Class, signtext);
                } else
                {
                    SendKitClass.Invoke(connection, kitID, EClass.NONE, kit.Name);
                }
            }
            internal static readonly NetCall<string, EClass, string> SendKitClass = new NetCall<string, EClass, string>(1114);
        }

        internal static readonly NetCall<string> RequestKit = new NetCall<string>(ReceiveKitRequest);
        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(IConnection connection, string kitID)
        {
            if (KitManager.KitExists(kitID, out Kit kit))
            {
                ReceiveKit.Invoke(connection, kit);
            }
            else
            {
                ReceiveKit.Invoke(connection, null);
            }
        }
        internal static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(ReceiveKitsRequest, F.ReadStringArray, F.WriteStringArray);
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static void ReceiveKitsRequest(IConnection connection, string[] kitIDs)
        {
            Kit[] kits = new Kit[kitIDs.Length];
            for (int i = 0; i < kitIDs.Length; i++)
            {
                if (KitManager.KitExists(kitIDs[i], out Kit kit))
                {
                    kits[i] = kit;
                }
                else
                {
                    kits[i] = null;
                }
            }
            ReceiveKits.Invoke(connection, kits);
        }
        internal static readonly NetCallRaw<Kit> ReceiveKit = new NetCallRaw<Kit>(1117, Kit.Read, Kit.Write);
        internal static readonly NetCallRaw<Kit[]> ReceiveKits = new NetCallRaw<Kit[]>(1118, Kit.ReadMany, Kit.WriteMany);

        internal static readonly NetCall<ushort> RequestItemInfo = new NetCall<ushort>(ReceiveItemInfoRequest);
        [NetCall(ENetCall.FROM_SERVER, 1119)]
        internal static void ReceiveItemInfoRequest(IConnection connection, ushort item)
        {
            if (Assets.find(EAssetType.ITEM, item) is ItemAsset asset)
                SendItemInfo.Invoke(connection, ItemData.FromAsset(asset));
            else 
                SendItemInfo.Invoke(connection, null);
        }
        internal static readonly NetCallRaw<ItemData> SendItemInfo = new NetCallRaw<ItemData>(1120, ItemData.Read, ItemData.Write);

        internal static readonly NetCall<ushort[]> RequestItemInfos = new NetCall<ushort[]>(ReceiveItemInfosRequest);
        [NetCall(ENetCall.FROM_SERVER, 1121)]
        internal static void ReceiveItemInfosRequest(IConnection connection, ushort[] items)
        {
            ItemData[] rtn = new ItemData[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                if (Assets.find(EAssetType.ITEM, items[i]) is ItemAsset asset)
                    rtn[i] = ItemData.FromAsset(asset);
            }
            SendItemInfos.Invoke(connection, rtn);
        }
        internal static readonly NetCallRaw<ItemData[]> SendItemInfos = new NetCallRaw<ItemData[]>(1122, ItemData.ReadMany, ItemData.WriteMany);

        internal static readonly NetCall RequestAllItemInfos = new NetCall(ReceiveAllItemInfosRequest);

        [NetCall(ENetCall.FROM_SERVER, 1123)]
        internal static void ReceiveAllItemInfosRequest(IConnection connection)
        {
            Asset[] assets = Assets.find(EAssetType.ITEM);
            ItemData[] rtn = new ItemData[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                try
                {
                    if (assets[i] is ItemAsset asset) rtn[i] = ItemData.FromAsset(asset);
                }
                catch (Exception ex)
                {
                    L.LogError($"Error converting asset of type {assets[i].GetType().FullName} to ItemData ({assets[i].name}).");
                    L.LogError(ex);
                    rtn[i] = null;
                }
            }
            SendItemInfos.Invoke(connection, rtn);
        }       
    }
}
