using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uncreated.Warfare.Profiling;

/// <summary>
/// Helpers for manually profiling functions or scopes.
/// </summary>
public static class ProfilerUtil
{
    /// <summary>
    /// Profile the calling function, saving information about it's running time.
    /// <code>
    /// <example>
    /// // Usage
    /// public void OnUpdate()
    /// {
    ///     using IDisposable? profiler = ProfilerUtil.Profile();
    /// }
    /// 
    /// </example>
    /// </code>
    /// </summary>
    /// <remarks>Thread-safe. Requires <c>Uncreated.Warfare</c> is compiled with the <c>PROFILING</c> define constant.</remarks>
    /// <returns>A value that, when disposed, will stop tracking.</returns>
#if !PROFILING
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static IDisposable? Profile(
#if PROFILING // avoid extra string allocations when not compiling with profiling enabled.
        [CallerFilePath] string? filePath = null, [CallerMemberName] string? methodName = null, [CallerLineNumber] int lineNumber = 0
#endif
    )
    {
#if !PROFILING
        return null;
#else
        return ProfileIntl(null, filePath ?? string.Empty, methodName ?? string.Empty, lineNumber);
#endif
    }

    /// <inheritdoc cref="Profile"/>
#if PROFILING
    // required since the methods are ambiguous
    [OverloadResolutionPriority(1)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static IDisposable? Profile(string description
#if PROFILING
        , [CallerFilePath] string? filePath = null, [CallerMemberName] string? methodName = null, [CallerLineNumber] int lineNumber = 0
#endif
    )
    {
#if !PROFILING
        return null;
#else
        return ProfileIntl(description, filePath ?? string.Empty, methodName ?? string.Empty, lineNumber);
#endif
    }

#if PROFILING

    /// <summary>
    /// Smallest fraction of a second that can be measured.
    /// </summary>
    internal const long MeasurableResolutionPerSecond = 10000;

    /// <summary>
    /// Max number of pooled profilers.
    /// </summary>
    private const int MaxPoolSize = 128;

    // Stopwatch.ElapsedTicks is divided by this value, basically ticks per measurable unit
    internal static readonly long FrequencyResolution = Stopwatch.Frequency / MeasurableResolutionPerSecond;

    private static readonly object FileLock = new object();
    private static readonly ConcurrentBag<UncreatedProfiler> ProfilerPool = new ConcurrentBag<UncreatedProfiler>();
    private static ConcurrentDictionary<UncreatedProfilerKey, ProfilerRecord> _records = new ConcurrentDictionary<UncreatedProfilerKey, ProfilerRecord>();
    private static DateTime _lastFlush = DateTime.UtcNow;
    private static bool _enabled;


    /// <summary>
    /// Master switch for profiling.
    /// </summary>
    public static bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value)
            {
                Flush();
            }

            ProfilerPool.Clear();
        }
    }

    private static IDisposable? ProfileIntl(string? description, string filePath, string methodName, int lineNumber)
    {
        if (!_enabled)
        {
            return null;
        }

        if (!ProfilerPool.TryTake(out UncreatedProfiler profiler))
        {
            profiler = new UncreatedProfiler();
        }

        profiler.Load(description, filePath, methodName, lineNumber);
        return profiler;
    }

    internal static void ReturnProfiler(UncreatedProfiler profiler)
    {
        if (ProfilerPool.Count < MaxPoolSize)
        {
            ProfilerPool.Add(profiler);
        }
    }

    internal static void RecordResults(long ticks, string? description, string filePath, string methodName, int lineNumber)
    {
        UncreatedProfilerKey k = new UncreatedProfilerKey(description, filePath, methodName, lineNumber);
        ProfilerRecord record = _records.GetOrAdd(k, static k => new ProfilerRecord(k));
        record.AddSample(ticks);
    }

    private const long MaxProfilerDataSize = 1024 * 1024 * 16; // 16 MiB

    private static void FlushIntl(ConcurrentDictionary<UncreatedProfilerKey, ProfilerRecord> records, DateTime startTime)
    {
        string baseDir = WarfareModule.IsActive ? WarfareModule.Singleton.HomeDirectory : Environment.CurrentDirectory;

        string directory = Path.Combine(baseDir, "Profiling");

        Directory.CreateDirectory(directory);

        FileInfo?[] allFiles = new DirectoryInfo(directory).GetFiles("*.csv", SearchOption.TopDirectoryOnly);
        // delete old files
        while (true)
        {
            long totalLength = 0;
            FileInfo? oldest = null;
            DateTime oldestDate = DateTime.MaxValue;
            for (int i = 0; i < allFiles.Length; i++)
            {
                FileInfo? file = allFiles[i];
                if (file == null)
                    continue;

                totalLength += file.Length;

                DateTime utc = file.LastWriteTimeUtc;
                if (utc >= oldestDate)
                    continue;

                oldest = file;
                oldestDate = utc;
            }

            if (totalLength < MaxProfilerDataSize - 32767 /* rough estimate including current file size */)
                break;

            if (oldest == null)
                break;

            WarfareModule.Singleton.GlobalLogger.LogInformation($"Profiler deleting old data file: \"{oldest.Name}\".");
            oldest.Delete();
        }

        DateTime endTime = DateTime.UtcNow;
        KeyValuePair<UncreatedProfilerKey, ProfilerRecord>[] recordsArray = records.ToArray();

        // sort by sample count
        Array.Sort(recordsArray, (a, b) =>
        {
            double aVal = a.Value.SampleCount; // * a.Value.Average;
            double bVal = b.Value.SampleCount; // * b.Value.Average;
            return bVal.CompareTo(aVal);
        });

        using FileStream fs = new FileStream(
            Path.Combine(directory, $"{startTime:yy-MM-dd_HHmmss} - {endTime:yy-MM-dd_HHmmss}.csv"),
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            8192,
            FileOptions.SequentialScan
        );

        using StreamWriter sw = new StreamWriter(fs, Encoding.UTF8, 8192, leaveOpen: false);

        sw.Write("Description,Method,File,Line,Samples,Average (ms),Minimum (ms),Maximum (ms)");

        foreach (KeyValuePair<UncreatedProfilerKey, ProfilerRecord> row in recordsArray)
        {
            sw.WriteLine();

            ProfilerRecord r = row.Value;
            double avg = r.Average / (MeasurableResolutionPerSecond / 1000d);
            double min = r.Minimum / (MeasurableResolutionPerSecond / 1000d);
            double max = r.Maximum / (MeasurableResolutionPerSecond / 1000d);

            sw.Write(EscapeCsv(r.Info.Description));
            sw.Write(',');
            sw.Write(r.Info.MethodName);
            sw.Write(',');
            sw.Write(EscapeCsv(r.Info.FilePath));
            sw.Write(',');
            sw.Write(r.Info.LineNumber);
            sw.Write(',');
            sw.Write(r.SampleCount);
            sw.Write(',');
            sw.Write(avg.ToString("F8", CultureInfo.InvariantCulture));
            sw.Write(',');
            sw.Write(min.ToString("F8", CultureInfo.InvariantCulture));
            sw.Write(',');
            sw.Write(max.ToString("F8", CultureInfo.InvariantCulture));
        }

        return;

        [return: NotNullIfNotNull(nameof(str))]
        static string? EscapeCsv(string? str)
        {
            return str?.Replace(',', '_');
        }
    }

    /// <summary>
    /// Flush profiling data to the disk and reset it.
    /// </summary>
    /// <remarks>Best to run this on a worker thread.</remarks>
    public static void Flush()
    {
        lock (FileLock)
        {
            ConcurrentDictionary<UncreatedProfilerKey, ProfilerRecord> oldRecords
                = Interlocked.Exchange(ref _records, new ConcurrentDictionary<UncreatedProfilerKey, ProfilerRecord>());

            // reduce chance of record getting skipped
            Thread.Sleep(16);

            DateTime startTime = _lastFlush;
            _lastFlush = DateTime.UtcNow;
            FlushIntl(oldRecords, startTime);
        }
    }
#endif
}