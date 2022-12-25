using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using UnityEngine;

namespace Uncreated.Warfare;
public class ActionLog : MonoBehaviour
{
    public const string DateHeaderFormat = "yyyy-MM-dd_HH-mm-ss";
    public const string DateLineFormat = "s";
    public const int DateLineFormatLength = 19;
    public const string SteamIDFormat = "D17";
    public const int SteamIDLength = 17;
    public const int WriteBufferSize = 288;
    private readonly ConcurrentQueue<ActionLogItem> _items = new ConcurrentQueue<ActionLogItem>();
    private volatile bool _sendingLog;
    private ActionLogMeta? _current;
    private FileStream? _stream;
    private static ActionLog _instance;
    private static ByteWriter? _metaWriter;
    private static ByteReader? _metaReader;
    private static char[][]? _types;
    private CancellationTokenSource _src = new CancellationTokenSource();
    private static ByteReader MetaReader => _metaReader ??= new ByteReader();
    private static ByteWriter MetaWriter => _metaWriter ??= new ByteWriter(false, ActionLogMeta.Capacity);
    private static char[][] Types
    {
        get
        {
            if (_types is null)
            {
                _types = new char[(int)ActionLogType.Max][];
                for (int i = 0; i < _types.Length; ++i)
                    _types[i] = ((ActionLogType)i).ToString().ToCharArray();
            }

            return _types;
        }
    }
    [UsedImplicitly]
    void Awake()
    {
        if (_instance != null)
            Destroy(_instance);
        _instance = this;
    }
    [UsedImplicitly]
    void OnDestroy() => OnApplicationQuit();
    void OnApplicationQuit()
    {
        if (_stream is not null)
        {
            try
            {
                _stream.Flush();
                _stream.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsAsset(Asset asset) => $"{{{asset.FriendlyName} / {asset.id} / {asset.GUID:N}}}";
    public static void Add(ActionLogType type, string? data, UCPlayer? player) => Add(type, data, player == null ? 0ul : player.Steam64);
    public static void Add(ActionLogType type, string? data = null, ulong player = 0)
    {
        _instance._items.Enqueue(new ActionLogItem(player, type, data, DateTimeOffset.UtcNow));
    }
    public static void AddPriority(ActionLogType type, string? data = null, ulong player = 0)
    {
        _instance._items.Enqueue(new ActionLogItem(player, type, data, DateTimeOffset.UtcNow));
        _instance.Update();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetMetaPath(ActionLogMeta meta) => Path.Combine(Data.Paths.ActionLog, meta.FirstTimestamp.ToString(DateHeaderFormat, CultureInfo.InvariantCulture) + ".meta");
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetLogPath(ActionLogMeta meta) => Path.Combine(Data.Paths.ActionLog, meta.FirstTimestamp.ToString(DateHeaderFormat, CultureInfo.InvariantCulture) + ".txt");
    private void SaveMeta(string path)
    {
        using FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        MetaWriter.Stream = str;
        try
        {
            _current!.Write(MetaWriter);
        }
        finally
        {
            MetaWriter.Flush();
        }
    }
    private void SendCurrentLog() => SendLog(_current!);
    private void SendLog(ActionLogMeta meta)
    {
        UCWarfare.RunTask(SendLogAsync, meta, MessageContext.Nil, ctx: "Sending log " + meta.FirstTimestamp.ToString("s") + " to homebase.");
    }
    private static void DeleteLog(ActionLogMeta meta)
    {
        string path = GetLogPath(meta);
        if (File.Exists(path))
            File.Delete(path);
        path = GetMetaPath(meta);
        if (File.Exists(path))
            File.Delete(path);
    }
    private Task SendCurrentLogAsync(MessageContext context, CancellationToken token = default) =>
        _current == null
        ? Task.FromException(new Exception("No current log loaded."))
        : SendLogAsync(_current, context, token);
    private async Task SendLogAsync(ActionLogMeta meta, MessageContext context, CancellationToken token = default)
    {
        if (!UCWarfare.CanUseNetCall)
            return;
        int c = 0;
        while (_sendingLog)
        {
            if (c > 4000)
                return;
            await Task.Delay(25, token);
            ++c;
        }
        _sendingLog = true;
        try
        {
            string path = GetLogPath(meta);
            if (File.Exists(path))
            {
                byte[] bytes;
                if (meta == _current) // switch to main thread if sending current so it's not overwritten by the update loop.
                    await UCWarfare.ToUpdate(token);
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int l = (int)Math.Min(stream.Length, int.MaxValue);
                    bytes = new byte[l];
                    l = await stream.ReadAsync(bytes, 0, l, token);
                    if (l != bytes.Length)
                    {
                        byte[] old = bytes;
                        bytes = new byte[l];
                        Buffer.BlockCopy(old, 0, bytes, 0, l);
                    }
                }
                if (!UCWarfare.CanUseNetCall)
                    return;

                RequestResponse response = await NetCalls.SendLog.RequestAck(UCWarfare.I.NetClient!, meta, bytes, 10000);
                if (response.Responded && response.ErrorCode.HasValue && response.ErrorCode.Value == MessageContext.CODE_SUCCESS)
                {
                    if (meta != _current)
                        DeleteLog(meta);
                }
                else
                {
                    L.LogWarning("UCHB failed to acknoledge SendLog request: " + response.Context + ".");
                }
            }
        }
        finally
        {
            _sendingLog = false;
        }
    }
    [UsedImplicitly]
    void Update()
    {
        while (_items.TryDequeue(out ActionLogItem item))
        {
            if (_current != null && (item.Timestamp - _current.FirstTimestamp).TotalHours > 1d)
            {
                _stream?.Dispose();
                _stream = null;
                SendLog(_current);
                _current = null;
            }
            _current ??= new ActionLogMeta
            {
                FirstTimestamp = item.Timestamp,
                LoggedPlayers = new List<ulong>(64),
                LoggedDataTypes = new List<ActionLogType>(32),
                DataReferencedPlayers = new List<ulong>(48),
                UtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now)
            };
            if (Util.IsValidSteam64Id(item.Player) && !_current.LoggedPlayers.Contains(item.Player))
                _current.LoggedPlayers.Add(item.Player);

            if (!string.IsNullOrEmpty(item.Data))
                ActionLogMeta.FindReferencesInLine(item.Data!, _current.DataReferencedPlayers);

            if (!_current.LoggedDataTypes.Contains(item.Type))
                _current.LoggedDataTypes.Add(item.Type);

            _current.LastTimestamp = item.Timestamp;
            _stream ??= new FileStream(GetLogPath(_current), FileMode.Append, FileAccess.Write, FileShare.Read);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(item.ToCharArr());
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Flush();
            SaveMeta(GetMetaPath(_current));
        }
    }
    public static bool TryParseLogType(string text, out ActionLogType type) => Enum.TryParse(text, true, out type);
    internal void OnConnected()
    {
        if (!UCWarfare.Config.SendActionLogs)
            return;
        F.CheckDir(Data.Paths.ActionLog, out bool success);
        if (success)
        {
            lock (_instance)
            {
                List<string> files = new List<string>();
                foreach (string file in Directory.EnumerateFiles(Data.Paths.ActionLog, "*.txt"))
                {
                    string name = Path.GetFileName(file);
                    if (name.Equals(_currentFileName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    name = Path.GetFileNameWithoutExtension(name);
                    if (DateTimeOffset.TryParseExact(name, DateHeaderFormat, Data.AdminLocale, DateTimeStyles.AssumeLocal, out DateTimeOffset offs) && offs != _currentLogSt)
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
                            if (DateTimeOffset.TryParseExact(Path.GetFileNameWithoutExtension(files[i]), DateHeaderFormat, Data.AdminLocale, DateTimeStyles.AssumeLocal, out DateTimeOffset dto))
                            {
                                using FileStream str = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                                writer.Write(dto);
                                int len = (int)Math.Min(str.Length, int.MaxValue);
                                byte[] bytes = new byte[len];
                                str.Read(bytes, 0, len);
                                writer.WriteLong(bytes);
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
    // ReSharper disable once StructCanBeMadeReadOnly
    private record struct ActionLogItem(ulong Player, ActionLogType Type, string? Data, DateTimeOffset Timestamp)
    {
        public static ActionLogItem? FromLine(string line)
        {
            if (line.Length <= DateLineFormatLength + 6 + SteamIDLength)
                return null;
            int endbracket = line.IndexOf(']', DateLineFormatLength + 6 + SteamIDLength);
            if (endbracket == -1)
                return null;
            if (!DateTimeOffset.TryParseExact(
                    line.Substring(1, DateLineFormatLength), DateLineFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out DateTimeOffset timestamp) ||
                !ulong.TryParse(line.Substring(3 + DateLineFormatLength, SteamIDLength), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64) ||
                !TryParseLogType(line.Substring(DateLineFormatLength + 6 + SteamIDLength, endbracket - (DateLineFormatLength + 6 + SteamIDLength)), out ActionLogType type))
                return null;
            return new ActionLogItem(steam64, type, endbracket == line.Length - 1 ? string.Empty : line.Substring(endbracket + 1), timestamp);
        }

        public readonly override string ToString() => new string(ToCharArr());
        public readonly unsafe char[] ToCharArr()
        {
            char[] t = Types.Length <= (int)Type ? Type.ToString().ToCharArray() : Types[(int)Type];
            int l2 = 6;
            if (Data is not null)
                l2 += 1 + Data!.Length;
            char[] chars = new char[l2 + DateLineFormatLength + SteamIDLength + t.Length];
            fixed (char* outPtr = chars)
            {
                outPtr[0] = '[';
                outPtr[DateLineFormatLength + 1] = ']';
                outPtr[DateLineFormatLength + 2] = '[';
                outPtr[DateLineFormatLength + 3 + SteamIDLength] = ']';
                outPtr[DateLineFormatLength + 4 + SteamIDLength] = '[';
                outPtr[DateLineFormatLength + 5 + SteamIDLength + t.Length] = ']';
                fixed (char* ptr = Timestamp.ToString(DateLineFormat))
                    Buffer.MemoryCopy(ptr, outPtr + 2, DateLineFormatLength * 2, DateLineFormatLength * 2);
                fixed (char* ptr = Player.ToString(SteamIDFormat))
                    Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength * 2 + 6, SteamIDLength * 2, SteamIDLength * 2);
                fixed (char* ptr = t)
                    Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength * 2 + 10 + SteamIDLength * 2, t.Length * 2, t.Length * 2);
                if (Data is not null)
                {
                    outPtr[DateLineFormatLength + 6 + SteamIDLength + t.Length] = ' ';
                    fixed (char* ptr = Data)
                        Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength * 2 + 14 + SteamIDLength * 2 + t.Length * 2, Data.Length * 2, Data.Length * 2);
                }
            }
            return chars;
        }
    }
    public class ActionLogMeta : IVersionableReadWrite
    {
        public const int Capacity = 2048;
        private const byte DataVersion = 1;
        public TimeSpan UtcOffset;
        public DateTimeOffset FirstTimestamp = DateTimeOffset.MaxValue;
        public DateTimeOffset LastTimestamp = DateTimeOffset.MinValue;
        public List<ulong> LoggedPlayers;
        public List<ulong> DataReferencedPlayers;
        public List<ActionLogType> LoggedDataTypes;
        public byte Version { get; set; } = DataVersion;
        public ActionLogMeta() { }
        public ActionLogMeta(DateTimeOffset start, DateTimeOffset end, List<ulong> players, List<ulong> refPlayers, List<ActionLogType> types)
        {
            FirstTimestamp = start;
            LastTimestamp = end;
            LoggedPlayers = players;
            DataReferencedPlayers = refPlayers;
            LoggedDataTypes = types;
        }
        /// <exception cref="ByteBufferOverflowException"/>
        public static ActionLogMeta FromMetaFile(string file)
        {
            lock (MetaReader)
            {
                bool f = MetaReader.ThrowOnError;
                MetaReader.ThrowOnError = true;
                ActionLogMeta meta;
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    MetaReader.LoadNew(stream);
                    meta = ReadMeta(MetaReader);
                }

                MetaReader.ThrowOnError = f;
                return meta;
            }
        }
        public static ActionLogMeta? FromLogFile(string file)
        {
            using StreamReader reader = new StreamReader(file, System.Text.Encoding.UTF8);
            List<ulong> logged = new List<ulong>(48);
            List<ulong> refed = new List<ulong>(48);
            List<ActionLogType> types = new List<ActionLogType>(48);
            DateTimeOffset first = DateTimeOffset.MaxValue;
            DateTimeOffset last = DateTimeOffset.MinValue;
            bool readOneLine = false;
            while (reader.ReadLine() is { } line)
            {
                ActionLogItem? n = ActionLogItem.FromLine(line);
                if (n.HasValue)
                {
                    readOneLine = true;
                    ActionLogItem item = n.Value;
                    if (Util.IsValidSteam64Id(item.Player) && !logged.Contains(item.Player))
                    {
                        logged.Add(item.Player);
                    }
                    if (!string.IsNullOrEmpty(item.Data))
                        FindReferencesInLine(item.Data!, refed);
                    if (!types.Contains(item.Type))
                        types.Add(item.Type);
                    if (first > item.Timestamp)
                        first = item.Timestamp;
                    if (last > item.Timestamp)
                        last = item.Timestamp;
                }
            }

            return !readOneLine ? null : new ActionLogMeta(first, last, logged, refed, types);
        }
        public static ActionLogMeta ReadMeta(ByteReader reader)
        {
            ActionLogMeta meta = new ActionLogMeta();
            meta.Read(reader);
            return meta;
        }
        public static void WriteMeta(ByteWriter writer, ActionLogMeta meta) => meta.Write(writer);
        public static void FindReferencesInLine(string data, ICollection<ulong> output)
        {
            int index = -1;
            const int s64Lm1 = SteamIDLength - 1;
            while (true)
            {
                index = data.IndexOf('7', index + 1);
                if (index == -1 || data.Length - index < SteamIDLength)
                    break;
                int end = index;
                while (end - index < s64Lm1 && char.IsDigit(data[end + 1])) ++end;
                if (end - index == s64Lm1 && ulong.TryParse(data.Substring(index, SteamIDLength), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64) && Util.IsValidSteam64Id(steam64) && !output.Contains(steam64))
                    output.Add(steam64);

                index = end;
            }
        }
        public void Read(ByteReader reader)
        {
            byte version = reader.ReadUInt8();
            Version = DataVersion;
            if (version > 0)
            {
                FirstTimestamp = reader.ReadDateTimeOffset();
                LastTimestamp = reader.ReadDateTimeOffset();
                int len = reader.ReadUInt16();
                LoggedPlayers = new List<ulong>(len);
                for (int i = 0; i < len; ++i)
                    LoggedPlayers.Add(reader.ReadUInt64());
                len = reader.ReadUInt16();
                LoggedDataTypes = new List<ActionLogType>(len);
                for (int i = 0; i < len; ++i)
                    LoggedDataTypes.Add(reader.ReadEnum<ActionLogType>());
                len = reader.ReadUInt16();
                DataReferencedPlayers = new List<ulong>(len);
                for (int i = 0; i < len; ++i)
                    DataReferencedPlayers.Add(reader.ReadUInt64());
            }
        }
        public void Write(ByteWriter writer)
        {
            if (writer.Stream == null)
            {
                writer.ExtendBuffer(27 +
                                    (LoggedPlayers == null ? 0 : LoggedPlayers.Count) +
                                    (LoggedDataTypes == null ? 0 : LoggedDataTypes.Count) +
                                    (DataReferencedPlayers == null ? 0 : DataReferencedPlayers.Count));
            }
            writer.Write(Version);
            if (Version > 0)
            {
                writer.Write(FirstTimestamp);
                writer.Write(LastTimestamp);
                if (LoggedPlayers is { Count: > 0 })
                {
                    writer.Write((ushort)LoggedPlayers.Count);
                    for (int i = 0; i < LoggedPlayers.Count; ++i)
                        writer.Write(LoggedPlayers[i]);
                }
                else
                {
                    writer.Write((ushort)0);
                }
                if (LoggedDataTypes is { Count: > 0 })
                {
                    writer.Write((ushort)LoggedDataTypes.Count);
                    for (int i = 0; i < LoggedDataTypes.Count; ++i)
                        writer.Write(LoggedDataTypes[i]);
                }
                else
                {
                    writer.Write((ushort)0);
                }
                if (DataReferencedPlayers is { Count: > 0 })
                {
                    writer.Write((ushort)DataReferencedPlayers.Count);
                    for (int i = 0; i < DataReferencedPlayers.Count; ++i)
                        writer.Write(DataReferencedPlayers[i]);
                }
                else
                {
                    writer.Write((ushort)0);
                }
            }
        }
    }
    public static class NetCalls
    {
        public static readonly NetCallRaw<ActionLogMeta, byte[]> SendLog = new NetCallRaw<ActionLogMeta, byte[]>(1127, ActionLogMeta.ReadMeta,
            reader => reader.ReadLongBytes(), ActionLogMeta.WriteMeta, (writer, b) => writer.WriteLong(b));
        public static readonly NetCall<DateTimeOffset> AckLog = new NetCall<DateTimeOffset>(1128);
        public static readonly NetCall RequestCurrentLog = new NetCall(ReceiveCurrentLogRequest);

        [NetCall(ENetCall.FROM_SERVER, 1129)]
        internal static Task ReceiveCurrentLogRequest(MessageContext context)
        {
            if (UCWarfare.Config.SendActionLogs && _instance != null)
            {
                return _instance.SendCurrentLogAsync(context);
            }

            return Task.CompletedTask;
        }
    }
}
public class ActionLogger : MonoBehaviour
{
    private readonly Queue<ActionLogItem> _items = new Queue<ActionLogItem>(16);
    public const string DateHeaderFormat = "yyyy-MM-dd_HH-mm-ss";
    private static ActionLogger _instance;
    private static DateTime _currentLogSt;
    private static string _currentFileName;

    [UsedImplicitly]
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void Awake()
    {
        SetTimeToNow();
        if (_instance != null)
            Destroy(_instance);
        _instance = this;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsAsset(Asset asset) => $"{{{asset.FriendlyName} / {asset.id} / {asset.GUID:N}}}";
    private static void SetTimeToNow()
    {
        _currentLogSt = DateTime.UtcNow;
        _currentFileName = _currentLogSt.ToString(DateHeaderFormat, Data.AdminLocale) + ".txt";
    }

    public static void Add(ActionLogType type, string? data, UCPlayer? player) =>
        Add(type, data, player == null ? 0ul : player.Steam64);
    public static void Add(ActionLogType type, string? data = null, ulong player = 0)
    {
        _instance._items.Enqueue(new ActionLogItem(player, type, data, DateTime.UtcNow));
    }
    public static void AddPriority(ActionLogType type, string? data = null, ulong player = 0)
    {
        _instance._items.Enqueue(new ActionLogItem(player, type, data, DateTime.UtcNow));
        _instance.Update();
    }
    private void Update()
    {
        if (_items.Count > 0)
        {
            F.CheckDir(Data.Paths.ActionLog, out bool success);
            if (success)
            {
                lock (_instance)
                {
                    string outputFile = Path.Combine(Data.Paths.ActionLog, _currentFileName);
                    if ((DateTime.UtcNow - _currentLogSt).TotalHours > 1d)
                    {
                        if (UCWarfare.CanUseNetCall && File.Exists(outputFile))
                        {
                            try
                            {
                                using FileStream str = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                                if (str.Length <= int.MaxValue)
                                {
                                    int len = (int)str.Length;
                                    byte[] bytes = new byte[len];
                                    str.Read(bytes, 0, len);
                                    if (UCWarfare.Config.SendActionLogs && UCWarfare.CanUseNetCall)
                                    {
                                        NetCalls.SendLogs.NetInvoke(writer =>
                                        {
                                            writer.Write((DateTimeOffset)_currentLogSt);
                                            writer.WriteLong(bytes);
                                        });
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
                        outputFile = Path.Combine(Data.Paths.ActionLog, _currentFileName);
                    }

                    using FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                    stream.Seek(0, SeekOrigin.End);
                    while (_items.Count > 0)
                    {
                        ActionLogItem item = _items.Dequeue();
                        WriteItem(in item, stream);
                    }
                }
            }
        }
    }
    [UsedImplicitly]
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnDestroy()
    {
        if (_instance != null)
        {
            Update();
            _instance = null!;
        }
    }
    [UsedImplicitly]
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnApplicationQuit()
    {
        if (_instance != null)
        {
            Update();
            _instance = null!;
        }
    }
    private void WriteItem(in ActionLogItem item, FileStream stream)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(item.ToString() + "\n");
        stream.Write(data, 0, data.Length);
    }
    private void SendCurrentLog(in MessageContext ctx)
    {
        if (!UCWarfare.Config.SendActionLogs)
            return;
        lock (_instance)
        {
            string outputFile = Path.Combine(Data.Paths.ActionLog, _currentFileName);
            if (File.Exists(outputFile))
            {
                using FileStream str = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                int len = (int)Math.Min(str.Length, int.MaxValue);
                byte[] bytes = new byte[len];
                str.Read(bytes, 0, len);
                ctx.Reply(NetCalls.SendLogs, writer =>
                {
                    writer.Write((DateTimeOffset)_currentLogSt);
                    writer.WriteLong(bytes);
                });
            }
        }
    }

}

// ReSharper disable InconsistentNaming
public enum ActionLogType : byte
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
    MAIN_CAMP_ATTEMPT,
    LEFT_MAIN,
    POSSIBLE_SOLO,
    SOLO_RTB,
    ENTER_MAIN,

    Max = ENTER_MAIN
}
// ReSharper restore InconsistentNaming