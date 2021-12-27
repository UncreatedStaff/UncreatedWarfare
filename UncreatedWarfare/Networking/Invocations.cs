using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uncreated.Networking;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.XP;
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
                    PlayerManager.AddSave(new PlayerSave(player, 0, string.Empty, string.Empty, status, 0, false, false));
                }
            }

            internal static readonly NetCall<ushort, EAssetType> RequestAssetName = new NetCall<ushort, EAssetType>(1025);
            [NetCall(ENetCall.FROM_SERVER, 1025)]
            internal static void ReceiveRequestAssetName(in IConnection connection, ushort id, EAssetType type)
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



            internal static readonly NetCall RequestFullLog = new NetCall(1029);
            [NetCall(ENetCall.FROM_SERVER, 1029)]
            internal static void ReceiveRequestFullLog(in IConnection connection)
            {
                SendFullLog.Invoke(connection, Data.Logs.ToArray(), 0);
                L.Log(Data.Logs.Count.ToString());
            }
            internal static readonly NetCallRaw<Log, byte> SendLogMessage =
                new NetCallRaw<Log, byte>(1030, Log.Read, R => R.ReadUInt8(), Log.Write, (W, B) => W.Write(B));
            internal static readonly NetCallRaw<Log[], byte> SendFullLog =
                new NetCallRaw<Log[], byte>(1031, Log.ReadMany, R => R.ReadUInt8(), Log.WriteMany, (W, B) => W.Write(B));
            internal static readonly NetCall<string> SendCommand = new NetCall<string>(1032);
            [NetCall(ENetCall.FROM_SERVER, 1032)]
            internal static void ReceiveCommand(in IConnection connection, string command)
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
            internal static readonly NetCallRaw<Kit> CreateKit = new NetCallRaw<Kit>(1109, Kit.Read, Kit.Write);
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

            internal static readonly NetCall<string> RequestKitClass = new NetCall<string>(1113);
            [NetCall(ENetCall.FROM_SERVER, 1113)]
            internal static void ReceiveRequestKitClass(in IConnection connection, string kitID)
            {
                if (KitManager.KitExists(kitID, out Kit kit))
                {
                    string signtext = kit.Name;
                    if (!kit.SignTexts.TryGetValue(JSONMethods.DefaultLanguage, out signtext))
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

        internal static readonly NetCall<string> RequestKit = new NetCall<string>(1115);
        [NetCall(ENetCall.FROM_SERVER, 1115)]
        internal static void ReceiveKitRequest(in IConnection connection, string kitID)
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
        internal static readonly NetCallRaw<string[]> RequestKits = new NetCallRaw<string[]>(1116, F.ReadStringArray, F.WriteStringArray);
        [NetCall(ENetCall.FROM_SERVER, 1116)]
        internal static void ReceiveKitsRequest(in IConnection connection, string[] kitIDs)
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

        internal static readonly NetCall<ushort> RequestItemInfo = new NetCall<ushort>(1119);
        [NetCall(ENetCall.FROM_SERVER, 1119)]
        internal static void ReceiveItemInfoRequest(in IConnection connection, ushort item)
        {
            if (Assets.find(EAssetType.ITEM, item) is ItemAsset asset)
                SendItemInfo.Invoke(connection, ItemData.FromAsset(asset));
            else 
                SendItemInfo.Invoke(connection, null);
        }
        internal static readonly NetCallRaw<ItemData> SendItemInfo = new NetCallRaw<ItemData>(1120, ItemData.Read, ItemData.Write);

        internal static readonly NetCall<ushort[]> RequestItemInfos = new NetCall<ushort[]>(1121);
        [NetCall(ENetCall.FROM_SERVER, 1121)]
        internal static void ReceiveItemInfosRequest(in IConnection connection, ushort[] items)
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

        internal static readonly NetCall RequestAllItemInfos = new NetCall(1123);

        [NetCall(ENetCall.FROM_SERVER, 1123)]
        internal static void ReceiveAllItemInfosRequest(in IConnection connection)
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


        internal static readonly NetCall<ushort, byte[]> RequestIcon = new NetCall<ushort, byte[]>(2102);
        internal static readonly NetCall<ushort, byte[], byte[]> SendIcon = new NetCall<ushort, byte[], byte[]>(2103);
        [NetCall(ENetCall.FROM_SERVER, 2102)]
        internal static void ReceiveIconRequest(ushort id, byte[] state)
        {
            if (!(Assets.find(EAssetType.ITEM, id) is ItemAsset asset)) return;
            Item item = new Item(id, asset.amount, asset.qualityMax, state);
            L.Log("starting");
            DateTime dt = DateTime.Now;
            ItemTool.getIcon(id, asset.qualityMax, state, (txt) => SendRenderedIcon(id, state, txt, dt));
        }
        private static void SendRenderedIcon(ushort id, byte[] state, Texture2D texture, DateTime start)
        {
            byte[] png;
            try
            {
                png = texture.EncodeToPNG();
                UnityEngine.Object.Destroy(texture);
            }
            catch (ArgumentException)
            {
                // Reading unreadable textures: https://fargesportfolio.com/unity-texture-texture2d-rendertexture/
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height);
                Texture2D newText = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false)
                {
                    name = texture.name
                };
                RenderTexture currentActiveRT = RenderTexture.active;
                RenderTexture.active = rt;
                Graphics.Blit(texture, rt);
                newText.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                newText.Apply(false);
                RenderTexture.active = currentActiveRT;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(texture);
                png = newText.EncodeToPNG();
                UnityEngine.Object.Destroy(newText);
            }
            L.Log($"done rendering {png.Length} bytes in {(DateTime.Now - start).TotalMilliseconds}ms");
            System.IO.File.WriteAllBytes(@"C:\Users\danny\OneDrive\Desktop\txt.png", png);
        }
    }
}
