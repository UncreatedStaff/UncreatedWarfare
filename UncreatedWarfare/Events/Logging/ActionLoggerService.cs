using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Logging;

public sealed class ActionLoggerService : IEventListener<IActionLoggableEvent>, IDisposable
{
    private FileStream _currentLogStream;
    private StreamWriter _currentLogWriter;
    private readonly object _sync;

    private readonly string _baseDir;

    private readonly SpanAction<char, DateTime> _formatLogFileNameCallback;
    private readonly SpanAction<char, DateTime> _formatMetaFileNameCallback;

    private ActionLogMeta _pendingMetaFile;
    private readonly List<ulong> _pendingMetaPlayers;
    private readonly List<ulong> _pendingMetaReferencedPlayers;
    private readonly List<ActionLogType> _pendingMetaTypes;
    private DateTime _currentLog;
    private DateTime _nextLog;
    private bool _needsNewLine;

    internal const string FileNameFormat = "yyyy-MM-dd_HH-mm-ss";
    internal const string DateLineFormat = "s";

    public ActionLoggerService(WarfareModule module)
    {
        _sync = new object();
        _baseDir = Path.Combine(module.HomeDirectory, "Action Logs");

        _formatLogFileNameCallback = (span, logTime) =>
        {
            _baseDir.AsSpan().CopyTo(span);
            int index = _baseDir.Length;

            span[index] = Path.DirectorySeparatorChar;

            logTime.TryFormat(span.Slice(index + 1), out _, FileNameFormat, CultureInfo.InvariantCulture);
            index += FileNameFormat.Length + 1;
            span[index] = '.';
            span[index + 1] = 't';
            span[index + 2] = 'x';
            span[index + 3] = 't';
        };

        _formatMetaFileNameCallback = (span, logTime) =>
        {
            _baseDir.AsSpan().CopyTo(span);
            int index = _baseDir.Length;

            span[index] = Path.DirectorySeparatorChar;

            logTime.TryFormat(span.Slice(index + 1), out _, FileNameFormat, CultureInfo.InvariantCulture);
            index += FileNameFormat.Length + 1;
            span[index] = '.';
            span[index + 1] = 'm';
            span[index + 2] = 'e';
            span[index + 3] = 't';
            span[index + 4] = 'a';
        };

        _currentLogStream = null!;
        _currentLogWriter = null!;
        _pendingMetaFile = null!;
        _pendingMetaPlayers = new List<ulong>(64);
        _pendingMetaReferencedPlayers = new List<ulong>(64);
        _pendingMetaTypes = new List<ActionLogType>(32);
        StartNextLog();
    }

    private string GetLogPath(DateTime logTime)
    {
        return string.Create(FileNameFormat.Length + _baseDir.Length + 5, logTime, _formatLogFileNameCallback);
    }

    private string GetMetaPath(DateTime logTime)
    {
        return string.Create(FileNameFormat.Length + _baseDir.Length + 6, logTime, _formatMetaFileNameCallback);
    }

    private static string GetMetaPath(string logFile)
    {
        ReadOnlySpan<char> fileName = Path.GetFileNameWithoutExtension(logFile.AsSpan());
        ReadOnlySpan<char> dir = Path.GetDirectoryName(logFile.AsSpan());
        return dir.Concat(Path.DirectorySeparatorChar, fileName, ".meta");
    }

    private static DateTime GetCurrentLogTime()
    {
        DateTime logTime = DateTime.UtcNow;
        DateTime hourStart = new DateTime(logTime.Year, logTime.Month, logTime.Day, logTime.Hour, 0, 0);
        if ((logTime - hourStart).TotalMinutes < 1)
        {
            hourStart = hourStart.AddHours(1d);
        }

        return hourStart;
    }

    private void StartNextLog()
    {
        if (_currentLog != DateTime.MinValue && _pendingMetaFile != null)
        {
            string meta = GetMetaPath(_currentLog);
            _pendingMetaFile.Types = _pendingMetaTypes.ToArray();
            _pendingMetaFile.Players = _pendingMetaPlayers.ToArray();
            _pendingMetaFile.DataReferencedPlayers = _pendingMetaReferencedPlayers.ToArray();
            _pendingMetaFile.WriteToFile(meta);
        }

        _currentLog = GetCurrentLogTime();
        _nextLog = _currentLog.AddHours(1d);
        if (_currentLogWriter != null)
        {
            _currentLogWriter.Flush();
            _ = _currentLogWriter.DisposeAsync();
        }

        if (_currentLogStream != null)
        {
            _ = _currentLogStream.DisposeAsync();
        }

        Directory.CreateDirectory(_baseDir);
        _currentLogStream = new FileStream(GetLogPath(_currentLog), FileMode.Append, FileAccess.Write, FileShare.Read, 8192, FileOptions.SequentialScan);
        _currentLogWriter = new StreamWriter(_currentLogStream, Encoding.UTF8, 8192, leaveOpen: true);
        _needsNewLine = _currentLogStream.Length > 0;

        _pendingMetaFile = new ActionLogMeta { Start = _currentLog, End = _currentLog };
        _pendingMetaPlayers.Clear();
        _pendingMetaReferencedPlayers.Clear();
        _pendingMetaTypes.Clear();
    }

    public void AddAction(in ActionLogEntry entry)
    {
        lock (_sync)
        {
            if (_currentLogWriter == null)
                throw new ObjectDisposedException(nameof(ActionLoggerService));

            DateTime timestamp = DateTime.UtcNow;
            if (timestamp > _nextLog)
            {
                StartNextLog();
            }

            _pendingMetaFile.End = timestamp;
            ++_pendingMetaFile.RowCount;

            if (!_pendingMetaTypes.Contains(entry.Type))
                _pendingMetaTypes.Add(entry.Type);
            if (entry.Player != 0 && !_pendingMetaPlayers.Contains(entry.Player))
                _pendingMetaPlayers.Add(entry.Player);
            if (!string.IsNullOrEmpty(entry.Message))
                FindReferencesInLine(entry.Message, _pendingMetaReferencedPlayers);

            // [2024-12-07T05:33:45][00000000000000000][LOG_TYPE] ...
            if (_needsNewLine)
                _currentLogWriter.WriteLine();
            else
                _needsNewLine = true;
            _currentLogWriter.Write('[');

            Span<char> formatBuffer = stackalloc char[19];
            timestamp.TryFormat(formatBuffer, out _, DateLineFormat, CultureInfo.InvariantCulture);
            _currentLogWriter.Write(formatBuffer);

            _currentLogWriter.Write("][");

            entry.Player.TryFormat(formatBuffer, out _, "D17", CultureInfo.InvariantCulture);
            _currentLogWriter.Write(formatBuffer.Slice(0, 17));
            _currentLogWriter.Write("][");

            _currentLogWriter.Write(entry.Type.LogName);
            _currentLogWriter.Write(']');

            if (!string.IsNullOrEmpty(entry.Message))
            {
                _currentLogWriter.Write(' ');
                _currentLogWriter.Write(entry.Message);
            }

            _currentLogWriter.Flush();
        }
    }

    /// <inheritdoc />
    public void HandleEvent(IActionLoggableEvent e, IServiceProvider serviceProvider)
    {
        ActionLogEntry[]? multipleEntries = null;

        ActionLogEntry entry = e.GetActionLogEntry(serviceProvider, ref multipleEntries);

        if (multipleEntries != null)
        {
            foreach (ActionLogEntry entry2 in multipleEntries)
            {
                AddAction(in entry2);
            }
        }

        if (entry.Message != null)
        {
            AddAction(in entry);
        }
    }

    /// <summary>
    /// Gets the meta for a specified log file.
    /// </summary>
    public ActionLogMeta? GetLogFileMetadata(string logFile)
    {
        string metaPath = GetMetaPath(logFile);
        ActionLogMeta? meta;
        if (File.Exists(metaPath))
        {
            meta = ActionLogMeta.FromFile(metaPath);
            return meta;
        }

        meta = CreateMetaForLogFile(logFile);
        meta?.WriteToFile(metaPath);
        return meta;
    }

    private static ActionLogMeta? CreateMetaForLogFile(string logFile)
    {
        using StreamReader reader = new StreamReader(logFile, Encoding.UTF8);
        List<ulong> logged = new List<ulong>(48);
        List<ulong> refed = new List<ulong>(48);
        List<ActionLogType> types = new List<ActionLogType>(48);
        DateTime first = DateTime.MaxValue;
        DateTime last = DateTime.MinValue;
        bool readOneLine = false;
        int rowCt = 0;
        while (reader.ReadLine() is { } line)
        {
            ParsedActionLogEntry n = ActionLogEntry.FromLine(line);
            if (n.Type == null)
                continue;

            ++rowCt;

            readOneLine = true;
            if (new CSteamID(n.Player).GetEAccountType() == EAccountType.k_EAccountTypeIndividual && !logged.Contains(n.Player))
            {
                logged.Add(n.Player);
            }
            if (!n.Message.IsEmpty)
                FindReferencesInLine(n.Message, refed);
            if (!types.Contains(n.Type))
                types.Add(n.Type);
            if (first > n.Time)
                first = n.Time;
            if (last < n.Time)
                last = n.Time;
        }

        return !readOneLine ? null : new ActionLogMeta
        {
            Types = types.ToArray(),
            DataReferencedPlayers = refed.ToArray(),
            Players = logged.ToArray(),
            Start = first,
            End = last,
            RowCount = rowCt
        };
    }

    private static void FindReferencesInLine(ReadOnlySpan<char> data, List<ulong> output)
    {
        int index = -1;
        while (true)
        {
            index = data.IndexOf('7', index + 1);
            if (index == -1 || data.Length - index < 17)
                break;

            int end = index;
            while (end - index < 16 && char.IsDigit(data[end + 1]))
                ++end;

            if (end - index == 16
                && ulong.TryParse(data.Slice(index, 17), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong steam64)
                && new CSteamID(steam64).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
                && !output.Contains(steam64))
            {
                output.Add(steam64);
            }

            index = end;
        }
    }

    public static string DescribeInput(InputInfo info)
    {
        switch (info.type)
        {
            case ERaycastInfoType.NONE:
                return "No hit";

            case ERaycastInfoType.SKIP:
                return "Skipped";

            case ERaycastInfoType.OBJECT:
                ObjectInfo obj = LevelObjectUtility.FindObject(info.transform);
                if (!obj.HasValue)
                {
                    return $"Hit unknown object at {info.point:F2}.";
                }
                return $"Hit object: {AssetLink.ToDisplayString(obj.Object.asset)}, " +
                       $"Instance ID: {obj.Object.instanceID} @ {obj.Object.transform.position:F2}, {obj.Object.transform.eulerAngles:F2}, " +
                       $"Section: {info.section}, Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.PLAYER:
                if (info.player == null)
                    break;
                PlayerNames names = new PlayerNames(info.player);
                return $"Hit player: {names.ToString()}, Limb: {EnumUtility.GetName(info.limb)}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.ZOMBIE:
                if (info.zombie == null)
                    break;
                return $"Hit zombie, Limb: {EnumUtility.GetName(info.limb)}, Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.ANIMAL:
                if (info.animal == null)
                    break;
                return $"Hit animal: {AssetLink.ToDisplayString(info.animal.asset)}, Limb: {EnumUtility.GetName(info.limb)}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.VEHICLE:
                if (info.vehicle == null)
                    break;
                return $"Hit vehicle: {AssetLink.ToDisplayString(info.vehicle.asset)} owned by {info.vehicle.lockedOwner.m_SteamID} ({info.vehicle.lockedGroup.m_SteamID}), " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.BARRICADE:
                BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(info.transform);
                if (barricade == null)
                    break;

                BarricadeData barricadeData = barricade.GetServersideData();
                return $"Hit barricade: {AssetLink.ToDisplayString(barricade.asset)} owned by {barricadeData.owner} ({barricadeData.group}), " +
                       $"Instance ID: {barricade.instanceID} @ {barricadeData.point:F2}, {barricadeData.rotation:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.STRUCTURE:
                StructureDrop? structure = StructureManager.FindStructureByRootTransform(info.transform);
                if (structure == null)
                    break;

                StructureData structureData = structure.GetServersideData();
                return $"Hit structure: {AssetLink.ToDisplayString(structure.asset)} owned by {structureData.owner} ({structureData.group}), " +
                       $"Instance ID: {structure.instanceID} @ {structureData.point:F2}, {structureData.rotation:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";

            case ERaycastInfoType.RESOURCE:
                Vector2Int c = Regions.GetCoordinateVector2Int(info.transform.position);
                List<ResourceSpawnpoint>? region = LevelGround.GetTreesOrNullInRegion(c);
                if (region == null)
                    break;

                int index = region.FindIndex(x => x.model == info.transform || x.stump == info.transform);
                ResourceSpawnpoint? tree = region[index];
                if (tree == null)
                    break;

                return $"Hit resource: {AssetLink.ToDisplayString(tree.asset)}, " +
                       $"Instance ID: ({c.x}, {c.y}, # {index}) @ {tree.point:F2}, {tree.angle:F2}, " +
                       $"Collider: {info.colliderTransform?.name}, " +
                       $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";
        }

        return $"Hit {info.type}, collider: {info.colliderTransform?.GetSceneHierarchyPath()}, " +
               $"Hit Position: {info.point:F2}, Direction: {info.direction:F2}.";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _currentLogWriter?.Dispose();
        _currentLogWriter = null!;
        _currentLogStream?.Dispose();
        _currentLogStream = null!;
    }
}