using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void Awake()
    {
        SetTimeToNow();
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
    }
    private static void SetTimeToNow()
    {
        CurrentLogSt = DateTime.UtcNow;
        CurrentFileName = CurrentLogSt.ToString(DATE_HEADER_FORMAT, Data.Locale) + ".txt";
    }

    public static void Add(EActionLogType type, string? data, UCPlayer player) =>
        Add(type, data, player.Steam64);
    public static void Add(EActionLogType type, string? data = null, ulong player = 0)
    {
        Instance.items.Enqueue(new ActionLogItem(player, type, data, DateTime.UtcNow));
    }
    public static void AddPriority(EActionLogType type, string? data = null, ulong player = 0)
    {
        Instance.items.Enqueue(new ActionLogItem(player, type, data, DateTime.UtcNow));
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
                    if ((DateTime.UtcNow - CurrentLogSt).TotalHours > 1d)
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
                                        if (UCWarfare.Config.SendActionLogs && UCWarfare.CanUseNetCall)
                                        {
                                            NetCalls.SendLogs.NetInvoke(writer =>
                                            {
                                                writer.Write((DateTimeOffset)CurrentLogSt);
                                                writer.WriteLong(bytes);
                                            });
                                        }
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
                            WriteItem(in item, stream);
                        }
                    }
                }
            }
        }
    }
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnDestroy()
    {
        if (Instance != null)
        {
            Update();
            Instance = null!;
        }
    }
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnApplicationQuit()
    {
        if (Instance != null)
        {
            Update();
            Instance = null!;
        }
    }
    private void WriteItem(in ActionLogItem item, FileStream stream)
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
    private void SendCurrentLog(in MessageContext ctx)
    {
        if (!UCWarfare.Config.SendActionLogs)
            return;
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
                    ctx.Reply(NetCalls.SendLogs, writer =>
                    {
                        writer.Write((DateTimeOffset)CurrentLogSt);
                        writer.WriteLong(bytes);
                    });
                }
            }
        }
    }
    internal static void OnConnected()
    {
#if aDEBUG
        return;
#endif
        if (!UCWarfare.Config.SendActionLogs)
            return;
        if (Instance != null)
        {
            F.CheckDir(Data.Paths.ActionLog, out bool success);
            if (success)
            {
                lock (Instance)
                {
                    List<string> files = new List<string>();
                    foreach (string file in Directory.EnumerateFiles(Data.Paths.ActionLog, "*.txt"))
                    {
                        string name = Path.GetFileName(file);
                        if (name.Equals(CurrentFileName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        name = Path.GetFileNameWithoutExtension(name);
                        if (DateTimeOffset.TryParseExact(name, DATE_HEADER_FORMAT, Data.Locale, DateTimeStyles.AssumeLocal, out DateTimeOffset offs) && offs != CurrentLogSt)
                        {
                            files.Add(file);
                        }
                    }

                    if (files.Count > 0)
                    {
                        NetCalls.SendLogs.NetInvoke(writer =>
                        {
                            writer.Write(files.Count);
                            for (int i = 0; i < files.Count; i++)
                            {
                                L.Log("Sending old log: \"" + files[i] + "\".", ConsoleColor.Magenta);
                                if (DateTimeOffset.TryParseExact(Path.GetFileNameWithoutExtension(files[i]), DATE_HEADER_FORMAT, Data.Locale, DateTimeStyles.AssumeLocal, out DateTimeOffset dto))
                                {
                                    using (FileStream str = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        writer.Write(dto);
                                        int len = (int)Math.Min(str.Length, int.MaxValue);
                                        byte[] bytes = new byte[len];
                                        str.Read(bytes, 0, len);
                                        writer.WriteLong(bytes);
                                    }
                                }
                                else
                                {
                                    writer.Write(DateTimeOffset.MinValue);
                                    writer.WriteLong(Array.Empty<byte>());
                                }
                            }
                        });
                    }
                }
            }
        }
    }

    public static class NetCalls
    {
        public static readonly NetCallCustom SendLogs = new NetCallCustom(1127, 32768);
        public static readonly NetCall<DateTimeOffset[]> AckLogs = new NetCall<DateTimeOffset[]>(ReceiveAckLogs);
        public static readonly NetCall RequestCurrentLog = new NetCall(ReceiveCurrentLogRequest);

        [NetCall(ENetCall.FROM_SERVER, 1128)]
        internal static void ReceiveAckLogs(MessageContext context, DateTimeOffset[] files)
        {
            for (int i = 0; i < files.Length; ++i)
            {
                string path = Path.Combine(Data.Paths.ActionLog, files[i].UtcDateTime.ToString(DATE_HEADER_FORMAT, Data.Locale) + ".txt");
                L.LogDebug("Action Log \"" + path + "\" acknowledged.");
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
        }
        [NetCall(ENetCall.FROM_SERVER, 1129)]
        internal static void ReceiveCurrentLogRequest(MessageContext context)
        {
            if (UCWarfare.Config.SendActionLogs && Instance != null)
            {
                Instance.SendCurrentLog(in context);
            }
            else
            {
                context.Reply(SendLogs, writer =>
                {
                    writer.Write(DateTimeOffset.MinValue);
                    writer.WriteLong(Array.Empty<byte>());
                });
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
    CLEAR_TRAITS,
    MAIN_CAMP_ATTEMPT
}