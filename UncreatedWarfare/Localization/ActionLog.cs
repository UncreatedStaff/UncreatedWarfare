using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Uncreated.Encoding;
using Uncreated.Networking;
using UnityEngine;

namespace Uncreated.Warfare;
public class ActionLogger : MonoBehaviour
{
    private readonly Queue<ActionLogItem> items = new Queue<ActionLogItem>(16);
    public const string DATE_HEADER_FORMAT = "yyyy-MM-dd_HH-mm-ss";
    private static ActionLogger Instance;
    private static DateTime CurrentLogSt;
    private static string CurrentFileName;
    private void Awake()
    {
        SetTimeToNow();
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
    }
    private static void SetTimeToNow()
    {
        CurrentLogSt = DateTime.Now;
        CurrentFileName = CurrentLogSt.ToString(DATE_HEADER_FORMAT, Data.Locale) + ".txt";
    }

    public static void Add(EActionLogType type, string? data, UCPlayer player) =>
        Add(type, data, player.Steam64);
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
            F.CheckDir(Data.Paths.ActionLog, out bool success);
            if (success)
            {
                lock (Instance)
                {
                    string outputFile = Path.Combine(Data.Paths.ActionLog, CurrentFileName);
                    if ((DateTime.Now - CurrentLogSt).TotalHours > 1d)
                    {
                        if (UCWarfare.CanUseNetCall && File.Exists(outputFile))
                        {
                            try
                            {
                                using (FileStream str = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    if (str.Length <= int.MaxValue)
                                    {
                                        int len = (int)str.Length;
                                        byte[] bytes = new byte[len];
                                        str.Read(bytes, 0, len);
                                        NetCalls.SendLog.NetInvoke(bytes, CurrentLogSt);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                L.LogError("Error sending log to homebase:");
                                L.LogError(ex);
                            }
                        }

                        SetTimeToNow();
                        outputFile = Path.Combine(Data.Paths.ActionLog, CurrentFileName);
                    }
                    using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        stream.Seek(0, SeekOrigin.End);
                        while (items.Count > 0)
                        {
                            ActionLogItem item = items.Dequeue();
                            WriteItem(ref item, stream);
                        }
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        if (Instance != null)
        {
            Update();
            Instance = null!;
        }
    }
    private void OnApplicationQuit()
    {
        if (Instance != null)
        {
            Update();
            Instance = null!;
        }
    }
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
    private void SendCurrentLog(ref MessageContext ctx)
    {
        lock (Instance)
        {
            string outputFile = Path.Combine(Data.Paths.ActionLog, CurrentFileName);
            if (File.Exists(outputFile))
            {
                using (FileStream str = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int len = (int)Math.Min(str.Length, int.MaxValue);
                    byte[] bytes = new byte[len];
                    str.Read(bytes, 0, len);
                    ctx.Reply(NetCalls.SendCurrentLog, bytes, CurrentLogSt);
                }
            }
        }
    }
    internal static void OnConnected()
    {
#if DEBUG
        return;
#endif
        if (Instance != null)
        {
            F.CheckDir(Data.Paths.ActionLog, out bool success);
            if (success)
            {
                lock (Instance)
                {
                    foreach (string file in Directory.EnumerateFiles(Data.Paths.ActionLog, "*.txt"))
                    {
                        try
                        {
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (name.Equals(CurrentFileName, StringComparison.Ordinal)) continue;
                            if (DateTime.TryParseExact(name, DATE_HEADER_FORMAT, Data.Locale, DateTimeStyles.AssumeLocal, out DateTime dt))
                            {
                                using (FileStream str = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    int len = (int)Math.Min(str.Length, int.MaxValue);
                                    byte[] bytes = new byte[len];
                                    str.Read(bytes, 0, len);
                                    NetCalls.SendLog.NetInvoke(bytes, dt);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError("Error sending ActionLog: ");
                            L.LogError(ex);
                        }
                    }
                }
            }
        }
    }

    public static class NetCalls
    {
        public static readonly NetCallRaw<byte[], DateTime> SendLog = new NetCallRaw<byte[], DateTime>(1127, ReadLog, null, WriteLog, null, 65535);
        public static readonly NetCall<DateTime> AckLog = new NetCall<DateTime>(ReceiveAckLog);
        public static readonly NetCall RequestCurrentLog = new NetCall(ReceiveCurrentLogRequest);
        public static readonly NetCallRaw<byte[], DateTime> SendCurrentLog = new NetCallRaw<byte[], DateTime>(1130, ReadLog, null, WriteLog, null, 65535);
        private static byte[] ReadLog(ByteReader reader)
        {
            int length = reader.ReadInt32();
            return reader.ReadBlock(length);
        }
        private static void WriteLog(ByteWriter writer, byte[] logData)
        {
            writer.Write(logData.Length);
            writer.WriteBlock(logData);
        }
        [NetCall(ENetCall.FROM_SERVER, 1128)]
        internal static void ReceiveAckLog(MessageContext context, DateTime fileReceived)
        {
            string path = Path.Combine(Data.Paths.ActionLog, fileReceived.ToString(DATE_HEADER_FORMAT, Data.Locale) + ".txt");
            if (Instance == null)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            else
            {
                lock (Instance)
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
        }
        [NetCall(ENetCall.FROM_SERVER, 1129)]
        internal static void ReceiveCurrentLogRequest(MessageContext context)
        {
            if (Instance != null)
            {
                Instance.SendCurrentLog(ref context);
            }
            else
            {
                context.Reply(SendCurrentLog, Array.Empty<byte>(), default);
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
    UNMUTE_PLAYER,
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
    VEHICLE_BAY_FORCE_SPAWN,
    PERMISSION_LEVEL_CHANGED,
    CHAT_FILTER_VIOLATION,
    KICKED_BY_BATTLEYE,
    TEAMKILL,
    KILL,
    REQUEST_TRAIT,
    SET_SAVED_STRUCTURE_PROPERTY,
    SET_TRAIT_PROPERTY,
    GIVE_TRAIT,
    REVOKE_TRAIT,
    CLEAR_TRAITS
}