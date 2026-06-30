using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Logging;
public class ActionLog : MonoBehaviour
{
    #if FALSE
    public const string DateHeaderFormat = "yyyy-MM-dd_HH-mm-ss";
    public const string DateLineFormat = "s";
    public const int DateLineFormatLength = 19;
    public const string SteamIDFormat = "D17";
    public const int SteamIDLength = 17;
    public const int WriteBufferSize = 288;
    private readonly ConcurrentQueue<ActionLogItem> _items = new ConcurrentQueue<ActionLogItem>();
    private volatile bool _sendingLog;
    private readonly byte[] _writeBuffer = new byte[WriteBufferSize];
    private ActionLogMeta? _current;
    private FileStream? _stream;
    private string? _activeFileName;
    private static ActionLog? _instance;
    private static ByteWriter? _metaWriter;
    private static ByteReader? _metaReader;
    private static char[][]? _types;
    private static char[]? _nl;
    public static readonly DateTimeOffset MinDatetime = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero); // jan 1st, 2021
    private static ByteReader MetaReader => _metaReader ??= new ByteReader();
    private static ByteWriter MetaWriter => _metaWriter ??= new ByteWriter(ActionLogMeta.Capacity);
    public static ActionLog? Instance => _instance;
#pragma warning disable CS0618
    private static char[][] Types
    {
        get
        {
            if (_types is null)
            {
                FieldInfo[] fields = typeof(ActionLogType).GetFields(BindingFlags.Static | BindingFlags.Public);
                _types = new char[(int)ActionLogType.Max + 1][];
                for (int i = 0; i < _types.Length; ++i)
                {
                    FieldInfo? enumField = fields.FirstOrDefault(x => (byte)x.GetValue(null) == (byte)i && !x.Name.Equals(nameof(ActionLogType.Max)));
                    if (enumField == null || Attribute.GetCustomAttribute(enumField, typeof(TranslatableAttribute)) is not TranslatableAttribute { Default.Length: > 0 } tr)
                        _types[i] = ((ActionLogType)i).ToString().ToCharArray();
                    else
                    {
                        _types[i] = tr.Default.ToCharArray();
                    }
                }
            }

            return _types;
        }
    }

#pragma warning restore CS0618
    private static char[] NewLineChars => _nl ??= Environment.NewLine.ToCharArray();
    [UsedImplicitly]
    void Awake()
    {
        if (_instance != null)
            Destroy(_instance);
        _instance = this;
        //F.CheckDir(Data.Paths.ActionLog, out _, true);
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
    // the brackets are separated on purpose
    public static string AsAsset(Asset? asset) => asset == null ? "{null asset}" : ("{" + $"{asset.FriendlyName} / {asset.id} / {asset.GUID:N}" + "}");
    public static void Add(ActionLogType type, string? data, WarfarePlayer? player) => Add(type, data, player == null ? 0ul : player.Steam64.m_SteamID);
    public static void Add(ActionLogType type, string? data = null, ulong player = 0)
    {
        if (player != 0 && Unsafe.As<ulong, CSteamID>(ref player).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            player = 0;

        _instance!._items.Enqueue(new ActionLogItem(player, type, data, DateTimeOffset.UtcNow));
    }
    public static void Add(ActionLogType type, string? data, CSteamID player)
    {
        if (player.m_SteamID != 0 && player.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            player.m_SteamID = 0;

        _instance!._items.Enqueue(new ActionLogItem(player.m_SteamID, type, data, DateTimeOffset.UtcNow));
    }
    /// <exception cref="NotSupportedException"/>
    public static void AddPriority(ActionLogType type, string? data = null, ulong player = 0)
    {
        if (player != 0 && Unsafe.As<ulong, CSteamID>(ref player).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
            player = 0;

        GameThread.AssertCurrent();
        _instance!._items.Enqueue(new ActionLogItem(player, type, data, DateTimeOffset.UtcNow));
        // todo _instance.Update();
    }

    private static string GetMetaPath(ActionLogMeta meta)
    {
        return Path.Combine(UnturnedPaths.RootDirectory.FullName, "Servers", Provider.serverID, "Warfare", "Action Logs", meta.FirstTimestamp.ToString(DateHeaderFormat, CultureInfo.InvariantCulture) + ".meta");
    }


    private static string GetLogPath(ActionLogMeta meta)
    {
        return Path.Combine(UnturnedPaths.RootDirectory.FullName, "Servers", Provider.serverID, "Warfare", "Action Logs", meta.FirstTimestamp.ToString(DateHeaderFormat, CultureInfo.InvariantCulture) + ".txt");
    }

    private void SaveMeta(ActionLogMeta meta)
    {
        using FileStream str = new FileStream(GetMetaPath(meta), FileMode.Create, FileAccess.Write, FileShare.Read);
        MetaWriter.Stream = str;
        try
        {
            meta.Write(MetaWriter);
        }
        finally
        {
            MetaWriter.Flush();
        }
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
#if false
    private Task<bool> SendCurrentLogAsync(MessageContext context, CancellationToken token = default) =>
        _current == null
        ? Task.FromResult(false)
        : SendLogAsync(_current, context, token);
    private async Task<bool> SendLogAsync(ActionLogMeta meta, MessageContext context, CancellationToken token = default)
    {
        if (!UCWarfare.CanUseNetCall)
            return false;
        token.ThrowIfCancellationRequested();
        int c = 0;
        while (_sendingLog)
        {
            if (c > 400)
                return false;
            await Task.Delay(25, token).ConfigureAwait(false);
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
                    await UniTask.SwitchToMainThread(token);
                token.ThrowIfCancellationRequested();
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
                token.ThrowIfCancellationRequested();
                if (!UCWarfare.CanUseNetCall)
                    return false;
                L.LogDebug("Sending log " + meta.FirstTimestamp.ToString("s") + "...");
                const int timeoutMs = 10000;
                if (context.Connection != null)
                    context.Reply(NetCalls.SendLog, meta, bytes);
                else
                {
                    NetTask netTask = NetCalls.SendLog.RequestAck(UCWarfare.I.NetClient!, meta, bytes, timeoutMs);

                    RequestResponse response = await netTask;
                    L.LogDebug("  ... Done, " + (response.Responded ? "Response: " + response.Context : "No response."));
                    if (response.Responded && response.ErrorCode is (int)StandardErrorCode.Success && context.Connection == null)
                    {
                        if (meta != _current)
                            DeleteLog(meta);
                        return true;
                    }
                    else
                    {
                        L.LogWarning("UCHB failed to acknoledge SendLog request: " + response.Context + ".");
                        return false;
                    }
                }
            }
        }
        finally
        {
            _sendingLog = false;
        }

        return false;
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
                _activeFileName = null;
                if (UCWarfare.CanUseNetCall)
                    UCWarfare.RunTask(SendLogAsync, _current, MessageContext.Nil, ctx: "Sending log \"" + _current.FirstTimestamp.ToString("s") + "\" to homebase.");
                _current = null;
            }
            _current ??= new ActionLogMeta
            {
                FirstTimestamp = item.Timestamp,
                LoggedPlayers = new List<ulong>(64),
                LoggedDataTypes = new List<ActionLogType>(32),
                DataReferencedPlayers = new List<ulong>(48),
                UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)
            };
            if (new CSteamID(item.Player).GetEAccountType() == EAccountType.k_EAccountTypeIndividual && !_current.LoggedPlayers.Contains(item.Player))
                _current.LoggedPlayers.Add(item.Player);

            if (!string.IsNullOrEmpty(item.Data))
                ActionLogMeta.FindReferencesInLine(item.Data!, _current.DataReferencedPlayers);

            if (!_current.LoggedDataTypes.Contains(item.Type))
                _current.LoggedDataTypes.Add(item.Type);

            _current.LastTimestamp = item.Timestamp;
            _activeFileName ??= GetLogPath(_current);
            _stream ??= new FileStream(_activeFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
            WriteItemToStream(in item, _stream, _writeBuffer);
            _stream.Flush();
            SaveMeta(_current);
        }
    }
#endif
    public static void WriteItemToStream(in ActionLogItem item, Stream stream, byte[] buffer)
    {
        char[] cs = item.ToCharArr(true);
        int ct = System.Text.Encoding.UTF8.GetByteCount(cs);
        byte[] bytes = ct <= WriteBufferSize ? buffer : new byte[ct];
        ct = System.Text.Encoding.UTF8.GetBytes(cs, 0, cs.Length, bytes, 0);
        stream.Write(buffer, 0, ct);
    }
    public static bool TryParseLogType(string text, out ActionLogType type)
    {
        for (int i = 0; i < Types.Length; ++i)
        {
            char[]? val = Types[i];
            if (val is null || val.Length != text.Length)
                continue;
            for (int j = 0; j < text.Length; ++j)
            {
                if (char.ToUpperInvariant(text[j]) != char.ToUpperInvariant(val[j]))
                    goto g;
            }

            type = (ActionLogType)i;
            return true;
        g:;
        }
        return Enum.TryParse(text, true, out type);
    }
    public static string GetLogTypeString(ActionLogType type) => (int)type >= Types.Length ? type.ToString() : new string(_types![(int)type]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ValidDateTimeOffset(in DateTimeOffset o) => o <= DateTimeOffset.UtcNow && o >= MinDatetime;
#if false
    internal async Task OnConnected(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!UCWarfare.Config.SendActionLogs)
            return;
        F.CheckDir(Data.Paths.ActionLog, out bool success);
        if (!success)
            return;
        if (!UCWarfare.CanUseNetCall)
        {
            L.Log("Cancelled action log sending...", ConsoleColor.Magenta);
            return;
        }
        token.ThrowIfCancellationRequested();
        string[] files = Directory.GetFiles(Data.Paths.ActionLog, "*.txt");
        int ttl = files.Length - (_current == null ? 0 : 1);
        L.Log("Found " + ttl + " log" + ttl.S() + " to send to homebase...", ConsoleColor.Magenta);
        foreach (string file in files)
        {
            string logName = Path.GetFileNameWithoutExtension(file);
            string? c = _activeFileName;
            if (c != null && logName.Equals(c, StringComparison.OrdinalIgnoreCase))
                continue; // active log, skip
            try
            {
                if (!File.Exists(file))
                {
#if DEBUG
                    L.LogWarning("[ACT LOG] File not found: \"" + file + "\".");
#endif
                    continue;
                }

                string metaPath = Path.Combine(Path.GetDirectoryName(file)!, logName + ".meta");
                bool genMeta = false;
                ActionLogMeta meta = null!;
                if (!File.Exists(metaPath))
                {
                    genMeta = true;
                }
                else
                {
                    meta = ActionLogMeta.FromMetaFile(metaPath);
                    if (!ValidDateTimeOffset(in meta.FirstTimestamp) || !ValidDateTimeOffset(in meta.LastTimestamp))
                    {
                        L.LogWarning("[ACT LOG] Invalid data detected in meta file \"" + logName + "\".");
                        genMeta = true;
                    }
                }

                if (genMeta) // meta file missing or invalid, generate one
                {
                    if ((meta = ActionLogMeta.FromLogFile(file)!) is null)
                    {
#if DEBUG
                        L.LogWarning(
                            "[ACT LOG] Log file is does not contain any valid logs, meta file will not be generated \"" +
                            logName + "\".");
#endif
                        continue;
                    }

                    try
                    {
                        SaveMeta(meta);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("[ACT LOG] Error saving meta file \"" + logName + "\".");
                        L.LogError(ex);
                    }
                }

                if (_current != null && meta.FirstTimestamp == _current.FirstTimestamp)
                    continue; // log in progress.

                CancellationTokenSource src = new CancellationTokenSource();
                CancellationToken tk2 = token;
                Task<bool> t;
                using (_ = tk2.CombineTokensIfNeeded(src.Token))
                {
                    t = SendLogAsync(meta, MessageContext.Nil, tk2);
                    try
                    {
                        await Task.WhenAny(t,
                            new
                                Func<Task>( // while send is not complete and and can use net call and < 20 seconds since start
                                    async () =>
                                    {
                                        int counter = 0;
                                        while (!t.IsCompleted && UCWarfare.CanUseNetCall && ++counter < 801)
                                        {
                                            await Task.Delay(25, tk2);
                                        }
                                    })());
                        src.Cancel();
                    }
                    catch (OperationCanceledException) when (src.IsCancellationRequested)
                    {
                    }
                }
                if (!UCWarfare.CanUseNetCall)
                {
                    L.Log("Cancelled action log sending...", ConsoleColor.Magenta);
                    return;
                }

                if (t is { IsCompleted: true, Result: true })
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error deleting log file \"" + file + "\".");
                        L.LogError(ex);
                    }

                    try
                    {
                        File.Delete(metaPath);
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Error deleting meta file \"" + metaPath + "\".");
                        L.LogError(ex);
                    }
                }
                else
                {
                    L.LogWarning("[ACT LOG] Send task timed out or failed on log \"" + logName + "\".");
                }
            }
            catch (Exception ex)
            {
                L.LogError("[ACT LOG] Error reading meta file of action log \"" + logName + "\".");
                L.LogError(ex);
            }
        }
    }
#endif
    // ReSharper disable once StructCanBeMadeReadOnly
    public record struct ActionLogItem(ulong Player, ActionLogType Type, string? Data, DateTimeOffset Timestamp)
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
                    DateTimeStyles.AssumeUniversal, out DateTimeOffset timestamp) ||
                !ulong.TryParse(line.Substring(3 + DateLineFormatLength, SteamIDLength), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64) ||
                !TryParseLogType(line.Substring(DateLineFormatLength + 5 + SteamIDLength, endbracket - (DateLineFormatLength + 5 + SteamIDLength)), out ActionLogType type))
                return null;
            return new ActionLogItem(steam64, type, endbracket == line.Length - 1 ? string.Empty : line.Substring(endbracket + 1), timestamp);
        }

        public readonly string ToString(bool newLine) => new string(ToCharArr(newLine));
        public readonly override string ToString() => new string(ToCharArr(false));
        public readonly unsafe char[] ToCharArr(bool newLine)
        {
            char[] t = Types.Length <= (int)Type ? Type.ToString().ToCharArray() : Types[(int)Type];
            int l2 = 6;
            if (Data is not null)
                l2 += 1 + Data!.Length;
            if (newLine) l2 += NewLineChars.Length;
            l2 += DateLineFormatLength + SteamIDLength + t.Length;
            char[] chars = new char[l2];
            fixed (char* outPtr = chars)
            {
                outPtr[0] = '[';
                outPtr[DateLineFormatLength + 1] = ']';
                outPtr[DateLineFormatLength + 2] = '[';
                outPtr[DateLineFormatLength + 3 + SteamIDLength] = ']';
                outPtr[DateLineFormatLength + 4 + SteamIDLength] = '[';
                outPtr[DateLineFormatLength + 5 + SteamIDLength + t.Length] = ']';
                fixed (char* ptr = Timestamp.ToString(DateLineFormat))
                    Buffer.MemoryCopy(ptr, outPtr + 1, DateLineFormatLength * 2, DateLineFormatLength * 2);
                fixed (char* ptr = Player.ToString(SteamIDFormat))
                    Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength + 3, SteamIDLength * 2, SteamIDLength * 2);
                fixed (char* ptr = t)
                    Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength + 5 + SteamIDLength, t.Length * 2, t.Length * 2);
                if (Data is not null)
                {
                    outPtr[DateLineFormatLength + 6 + SteamIDLength + t.Length] = ' ';
                    fixed (char* ptr = Data)
                        Buffer.MemoryCopy(ptr, outPtr + DateLineFormatLength + 7 + SteamIDLength + t.Length, Data.Length * 2, Data.Length * 2);
                }
                if (newLine)
                {
                    char[] nlc = NewLineChars;
                    for (int i = 0; i < nlc.Length; ++i)
                        outPtr[l2 - (nlc.Length - i)] = nlc[i];
                }
            }
            return chars;
        }
    }
    public class ActionLogMeta
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
                    MetaReader.LoadNew(Array.Empty<byte>());
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
                    if (new CSteamID(item.Player).GetEAccountType() == EAccountType.k_EAccountTypeIndividual && !logged.Contains(item.Player))
                    {
                        logged.Add(item.Player);
                    }
                    if (!string.IsNullOrEmpty(item.Data))
                        FindReferencesInLine(item.Data!, refed);
                    if (!types.Contains(item.Type))
                        types.Add(item.Type);
                    if (first > item.Timestamp)
                        first = item.Timestamp;
                    if (last < item.Timestamp)
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
                if (end - index == s64Lm1 && ulong.TryParse(data.Substring(index, SteamIDLength), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64) && new CSteamID(steam64).GetEAccountType() == EAccountType.k_EAccountTypeIndividual && !output.Contains(steam64))
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
        //public static readonly NetCallRaw<ActionLogMeta, byte[]> SendLog = new NetCallRaw<ActionLogMeta, byte[]>(KnownNetMessage.SendLog, ActionLogMeta.ReadMeta,
        //    reader => reader.ReadLongBytes(), ActionLogMeta.WriteMeta, (writer, b) => writer.WriteLong(b));
        //public static readonly NetCall RequestCurrentLog = new NetCall(ReceiveCurrentLogRequest);

        //[NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.RequestCurrentLog)]
        //internal static Task ReceiveCurrentLogRequest(MessageContext context)
        //{
        //    if (UCWarfare.Config.SendActionLogs && _instance != null)
        //    {
        //        return _instance.SendCurrentLogAsync(context);
        //    }

        //    SendLog.Invoke(context.Connection, new ActionLogMeta(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null!, null!, null!), Array.Empty<byte>());
        //    return Task.CompletedTask;
        //}
    }
#endif
}





// Add a Translatable attribute in all caps format and update ActionLogType.Max if you add a log type
[Translatable(IsPrioritizedTranslation = false)]
public enum ActionLogTypeOld : byte
{
    [TranslatableValue("DISCONNECT")]
    Disconnect,
    [TranslatableValue("GIVE_ITEM")]
    GiveItem,
    [TranslatableValue("CHANGE_LANGUAGE")]
    ChangeLanguage,
    [TranslatableValue("LOAD_SUPPLIES")]
    LoadSupplies,
    [TranslatableValue("LOAD_OLD_BANS")]
    LoadOldBans,
    [TranslatableValue("MUTE_PLAYER")]
    MutePlayer,
    [TranslatableValue("UNMUTE_PLAYER")]
    UnmutePlayer,
    [TranslatableValue("RELOAD_COMPONENT")]
    ReloadComponent,
    [TranslatableValue("REQUEST_KIT")]
    RequestKit,
    [TranslatableValue("REQUEST_VEHICLE")]
    RequestVehicle,
    [TranslatableValue("SHUTDOWN_SERVER")]
    ShutdownServer,
    [TranslatableValue("POP_STRUCTURE")]
    PopStructure,
    [TranslatableValue("SAVE_STRUCTURE")]
    SaveStructure,
    [TranslatableValue("UNSAVE_STRUCTURE")]
    UnsaveStructure,
    [TranslatableValue("SAVE_REQUEST_SIGN")]
    SaveRequestSign,
    [TranslatableValue("UNSAVE_REQUEST_SIGN")]
    UnsaveRequestSign,
    [TranslatableValue("ADD_WHITELIST")]
    AddWhitelist,
    [TranslatableValue("REMOVE_WHITELIST")]
    RemoveWhitelist,
    [TranslatableValue("SET_WHITELIST_MAX_AMOUNT")]
    SetWhitelistMaxAmount,
    [TranslatableValue("DESTROY_BARRICADE")]
    DestroyBarricade,
    [TranslatableValue("DESTROY_STRUCTURE")]
    DestroyStructure,
    [TranslatableValue("PLACE_BARRICADE")]
    PlaceBarricade,
    [TranslatableValue("PLACE_STRUCTURE")]
    PlaceStructure,
    [TranslatableValue("ENTER_VEHICLE_SEAT")]
    EnterVehicleSeat,
    [TranslatableValue("LEAVE_VEHICLE_SEAT")]
    LeaveVehicleSeat,
    [TranslatableValue("HELP_BUILD_BUILDABLE")]
    HelpBuildBuildable,
    [TranslatableValue("DEPLOY_TO_LOCATION")]
    DeployToLocation,
    [TranslatableValue("TELEPORT")]
    Teleport,
    [TranslatableValue("CHANGE_GAMEMODE_COMMAND")]
    ChangeGamemodeCommand,
    [TranslatableValue("GAMEMODE_CHANGED_AUTO")]
    GamemodeChangedAuto,
    [TranslatableValue("TEAM_WON")]
    TeamWon,
    [TranslatableValue("TEAM_CAPTURED_OBJECTIVE")]
    TeamCapturedObjective,
    [TranslatableValue("BUILD_ZONE_MAP")]
    BuildZoneMap,
    [TranslatableValue("DISCHARGE_OFFICER")]
    DischargeOfficer,
    [TranslatableValue("SET_OFFICER_RANK")]
    SetOfficerRank,
    [TranslatableValue("INJURED")]
    Injured,
    [TranslatableValue("REVIVED_PLAYER")]
    RevivedPlayer,
    [TranslatableValue("DEATH")]
    Death,
    [TranslatableValue("START_QUEST")]
    StartQuest,
    [TranslatableValue("MAKE_QUEST_PROGRESS")]
    MakeQuestProgress,
    [TranslatableValue("COMPLETE_QUEST")]
    CompleteQuest,
    [TranslatableValue("XP_CHANGED")]
    XPChanged,
    [TranslatableValue("CREDITS_CHANGED")]
    CreditsChanged,
    [TranslatableValue("CREATED_SQUAD")]
    CreatedSquad,
    [TranslatableValue("JOINED_SQUAD")]
    JoinedSquad,
    [TranslatableValue("LEFT_SQUAD")]
    LeftSquad,
    [TranslatableValue("DISBANDED_SQUAD")]
    DisbandedSquad,
    [TranslatableValue("LOCKED_SQUAD")]
    LockedSquad,
    [TranslatableValue("UNLOCKED_SQUAD")]
    UnlockedSquad,
    [TranslatableValue("PLACED_RALLY")]
    PlacedRally,
    [TranslatableValue("TELEPORTED_TO_RALLY")]
    TeleportedToRally,
    [TranslatableValue("CREATED_ORDER")]
    CreatedOrder,
    [TranslatableValue("FUFILLED_ORDER")]
    FufilledOrder,
    [TranslatableValue("OWNED_VEHICLE_DIED")]
    OwnedVehicleDied,
    [TranslatableValue("SERVER_STARTUP")]
    ServerStartup,
    [TranslatableValue("CREATE_KIT")]
    CreateKit,
    [TranslatableValue("DELETE_KIT")]
    DeleteKit,
    [TranslatableValue("GIVE_KIT")]
    GiveKit,
    [TranslatableValue("CHANGE_KIT_ACCESS")]
    ChangeKitAccess,
    [TranslatableValue("EDIT_KIT")]
    EditKit,
    [TranslatableValue("SET_KIT_PROPERTY")]
    SetKitProperty,
    [TranslatableValue("CREATE_VEHICLE_DATA")]
    CreateVehicleData,
    [TranslatableValue("DELETE_VEHICLE_DATA")]
    DeleteVehicleData,
    [TranslatableValue("REGISTERED_SPAWN")]
    RegisteredSpawn,
    [TranslatableValue("DEREGISTERED_SPAWN")]
    DeregisteredSpawn,
    [TranslatableValue("LINKED_VEHICLE_BAY_SIGN")]
    LinkedVehicleBaySign,
    [TranslatableValue("UNLINKED_VEHICLE_BAY_SIGN")]
    UnlinkedVehicleBaySign,
    [TranslatableValue("SET_VEHICLE_DATA_PROPERTY")]
    SetVehicleDataProperty,
    [TranslatableValue("VEHICLE_BAY_FORCE_SPAWN")]
    VehicleBayForceSpawn,
    [TranslatableValue("PERMISSION_LEVEL_CHANGED")]
    PermissionLevelChanged,
    [TranslatableValue("CHAT_FILTER_VIOLATION")]
    ChatFilterViolation,
    [TranslatableValue("KICKED_BY_BATTLEYE")]
    KickedByBattlEye,
    [TranslatableValue("TEAMKILL")]
    Teamkill,
    [TranslatableValue("KILL")]
    Kill,
    [TranslatableValue("REQUEST_TRAIT")]
    RequestTrait,
    [TranslatableValue("SET_SAVED_STRUCTURE_PROPERTY")]
    SetSavedStructureProperty,
    [TranslatableValue("SET_TRAIT_PROPERTY")]
    SetTraitProperty,
    [TranslatableValue("GIVE_TRAIT")]
    GiveTrait,
    [TranslatableValue("REVOKE_TRAIT")]
    RevokeTrait,
    [TranslatableValue("CLEAR_TRAITS")]
    ClearTraits,
    [TranslatableValue("MAIN_CAMP_ATTEMPT")]
    MainCampAttempt,
    [TranslatableValue("LEFT_MAIN")]
    LeftMain,
    [TranslatableValue("POSSIBLE_SOLO")]
    PossibleSolo,
    [TranslatableValue("SOLO_RTB")]
    SoloRTB,
    [TranslatableValue("ENTER_MAIN")]
    EnterMain,
    [TranslatableValue("ATTACH")]
    Attach,
    [TranslatableValue("DETACH")]
    Detach,
    [TranslatableValue("SET_AMMO")]
    SetAmmo,
    [TranslatableValue("SET_FIREMODE")]
    SetFiremode,
    [TranslatableValue("ADD_SKILLSET")]
    AddSkillset,
    [TranslatableValue("REMOVE_SKILLSET")]
    RemoveSkillset,
    [TranslatableValue("NITRO_BOOST_STATE_UPDATED")]
    NitroBoostStateUpdated,
    [TranslatableValue("UPGRADE_LOADOUT")]
    UpgradeLoadout,
    [TranslatableValue("UNLOCK_LOADOUT")]
    UnlockLoadout,
    [TranslatableValue("IP_WHITELIST")]
    IPWhitelist,
    [TranslatableValue("CHANGE_CULTURE")]
    ChangeCulture,
    [TranslatableValue("FORGIVE_MOD_ENTRY")]
    ForgiveModerationEntry,
    [TranslatableValue("EDIT_MOD_ENTRY")]
    EditModerationEntry,
    [TranslatableValue("CREATE_MOD_ENTRY")]
    CreateModerationEntry,
    [TranslatableValue("REMOVE_MOD_ENTRY")]
    RemoveModerationEntry,
    [TranslatableValue("LOCK_LOADOUT")]
    LockLoadout,
    [TranslatableValue("REPUTATION_CHANGED")]
    ReputationChanged,

    [TranslatableValue(IsPrioritizedTranslation = false)]
    [Obsolete("Don't use this.")]
    [Ignore]
    Max = ReputationChanged
}
// ReSharper restore InconsistentNaming