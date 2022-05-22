using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using UnityEngine;

namespace Uncreated.Warfare;
public class ActionLog : MonoBehaviour
{
    private readonly Queue<ActionLogItem> items = new Queue<ActionLogItem>(16);
    private const string DATE_HEADER_FORMAT = "yyyy-MM-dd_HH-mm-ss";
    private static ActionLog Instance;
    private void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
    }
    public static void Add(EActionLogType type, string? data = null, ulong player = 0)
    {
        Instance.items.Enqueue(new ActionLogItem(player, type, data, DateTime.Now));
    }
    public static void AddPriority(EActionLogType type, string? data = null, ulong player = 0)
    {
        Instance.items.Enqueue(new ActionLogItem(player, type, data, DateTime.Now));
        Instance.Update();
    }
    private void Update()
    {
        if (items.Count > 0)
        {
            F.CheckDir(Data.LOG_DIRECTORY, out bool success);
            if (success)
            {
                string path2 = Data.LOG_DIRECTORY + "current.txt";
                FileInfo info = new FileInfo(path2);
                bool replaced = false;
                if (info.Exists)
                {
                    DateTime creation = info.CreationTime;
                    if ((DateTime.Now - creation).TotalHours > 1d)
                    {
                        string path = Data.LOG_DIRECTORY + creation.ToString(DATE_HEADER_FORMAT) + ".txt";
                        try
                        {
                            info.CopyTo(path);
                            File.SetCreationTime(path, creation);
                            File.SetLastAccessTime(path, creation);
                            File.SetLastWriteTime(path, creation);

                            if (UCWarfare.CanUseNetCall)
                            {
                                using (FileStream str = info.OpenRead())
                                {
                                    if (str.Length <= int.MaxValue)
                                    {
                                        int len = (int)str.Length;
                                        byte[] bytes = new byte[len];
                                        str.Read(bytes, 0, len);
                                        NetCalls.SendLog.NetInvoke(bytes, creation);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError(ex);
                        }
                        info.Delete();
                        replaced = true;
                    }
                }
                using (FileStream stream = new FileStream(info.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    stream.Seek(0, SeekOrigin.End);
                    while (items.Count > 0)
                    {
                        ActionLogItem item = items.Dequeue();
                        WriteItem(ref item, stream);
                    }
                }

                if (replaced)
                    File.SetCreationTime(path2, DateTime.Now);
            }
        }
    }
    private void OnDestroy() => Update();
    private void OnApplicationQuit() => Update();
    private void WriteItem(ref ActionLogItem item, FileStream stream)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(item.ToString() + "\n");
        stream.Write(data, 0, data.Length);
    }
    private record struct ActionLogItem(ulong Player, EActionLogType Type, string? Data, DateTime Timestamp)
    {
        public override readonly string ToString()
        {
            string v = "[" + Timestamp.ToString("s") + "][" + Player.ToString("D17") + "][" + Type.ToString() + "]";
            if (Data != null)
                return v + " " + Data;
            else return v;
        }
    }

    public static class NetCalls
    {
        public static readonly NetCallRaw<byte[], DateTime> SendLog = new NetCallRaw<byte[], DateTime>(1127, R => R.ReadLongBytes() ?? Array.Empty<byte>(), null, (W, bytes) => W.WriteLong(bytes), null, 65535);
        public static readonly NetCall<DateTime> AckLog = new NetCall<DateTime>(ReceiveAckLog);
        public static readonly NetCall RequestCurrentLog = new NetCall(ReceiveCurrentLogRequest);
        public static readonly NetCallRaw<byte[], DateTime> SendCurrentLog = new NetCallRaw<byte[], DateTime>(1130, R => R.ReadLongBytes() ?? Array.Empty<byte>(), null, (W, bytes) => W.WriteLong(bytes), null, 65535);

        [NetCall(ENetCall.FROM_SERVER, 1128)]
        internal static void ReceiveAckLog(MessageContext context, DateTime fileReceived)
        {
            string path = Data.LOG_DIRECTORY + fileReceived.ToString(DATE_HEADER_FORMAT) + ".txt";
            if (File.Exists(path))
                File.Delete(path);
        }
        [NetCall(ENetCall.FROM_SERVER, 1129)]
        internal static void ReceiveCurrentLogRequest(MessageContext context)
        {
            string path2 = Data.LOG_DIRECTORY + "current.txt";
            FileInfo info = new FileInfo(path2);
            if (!info.Exists)
            {
                context.Reply(SendCurrentLog, Array.Empty<byte>(), default);
                return;
            }
            using (FileStream str = info.OpenRead())
            {
                if (str.Length <= int.MaxValue)
                {
                    int len = (int)str.Length;
                    byte[] bytes = new byte[len];
                    str.Read(bytes, 0, len);
                    context.Reply(SendCurrentLog, bytes, info.CreationTime);
                }
                else
                {
                    byte[] bytes = new byte[int.MaxValue];
                    str.Read(bytes, 0, int.MaxValue);
                    context.Reply(SendCurrentLog, bytes, info.CreationTime);
                    return;
                }
            }
        }
    }
    internal static void OnConnected()
    {
        F.CheckDir(Data.LOG_DIRECTORY, out bool success);
        if (success)
        {
            foreach (string file in Directory.EnumerateFiles(Data.LOG_DIRECTORY))
            {
                try
                {
                    FileInfo info = new FileInfo(file);
                    if (info.Name != "current.txt")
                    {
                        NetCalls.SendLog.NetInvoke(File.ReadAllBytes(file), info.CreationTime);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                }
            }
        }
    }
}

public enum EActionLogType : byte
{
    NONE,
    CHAT_GLOBAL,
    CHAT_AREA_OR_SQUAD,
    CHAT_GROUP,
    REQUEST_AMMO,
    DUTY_CHANGED,
    BAN_PLAYER,
    KICK_PLAYER,
    UNBAN_PLAYER,
    WARN_PLAYER,
    START_REPORT,
    CONFIRM_REPORT,
    BUY_KIT,
    CLEAR_ITEMS,
    CLEAR_INVENTORY,
    CLEAR_VEHICLES,
    CLEAR_STRUCTURES,
    ADD_CACHE,
    ADD_INTEL,
    CHANGE_GROUP_WITH_COMMAND,
    CHANGE_GROUP_WITH_UI,
    TRY_CONNECT,
    CONNECT,
    DISCONNECT,
    GIVE_ITEM,
    CHANGE_LANGUAGE,
    LOAD_SUPPLIES,
    LOAD_OLD_BANS,
    MUTE_PLAYER,
    RELOAD_COMPONENT,
    REQUEST_KIT,
    REQUEST_VEHICLE,
    SHUTDOWN_SERVER,
    POP_STRUCTURE,
    SAVE_STRUCTURE,
    UNSAVE_STRUCTURE,
    SAVE_REQUEST_SIGN,
    UNSAVE_REQUEST_SIGN,
    ADD_WHITELIST,
    REMOVE_WHITELIST,
    SET_WHITELIST_MAX_AMOUNT,
    DESTROY_BARRICADE,
    DESTROY_STRUCTURE,
    PLACE_BARRICADE,
    PLACE_STRUCTURE,
    ENTER_VEHICLE_SEAT,
    LEAVE_VEHICLE_SEAT,
    HELP_BUILD_BUILDABLE,
    DEPLOY_TO_LOCATION,
    TELEPORT,
    CHANGE_GAMEMODE_COMMAND,
    GAMEMODE_CHANGED_AUTO,
    TEAM_WON,
    TEAM_CAPTURED_OBJECTIVE,
    BUILD_ZONE_MAP,
    DISCHARGE_OFFICER,
    SET_OFFICER_RANK,
    INJURED,
    REVIVED_PLAYER,
    DEATH,
    START_QUEST,
    MAKE_QUEST_PROGRESS,
    COMPLETE_QUEST,
    XP_CHANGED,
    CREDITS_CHANGED,
    CREATED_SQUAD,
    JOINED_SQUAD,
    LEFT_SQUAD,
    DISBANDED_SQUAD,
    LOCKED_SQUAD,
    UNLOCKED_SQUAD,
    PLACED_RALLY,
    TELEPORTED_TO_RALLY,
    CREATED_ORDER,
    FUFILLED_ORDER,
    OWNED_VEHICLE_DIED,
    SERVER_STARTUP,
    CREATE_KIT,
    DELETE_KIT,
    GIVE_KIT,
    CHANGE_KIT_ACCESS,
    EDIT_KIT,
    SET_KIT_PROPERTY,
    CREATE_VEHICLE_DATA,
    DELETE_VEHICLE_DATA,
    REGISTERED_SPAWN,
    DEREGISTERED_SPAWN,
    LINKED_VEHICLE_BAY_SIGN,
    UNLINKED_VEHICLE_BAY_SIGN,
    SET_VEHICLE_DATA_PROPERTY,
    VEHICLE_BAY_FORCE_SPAWN
}