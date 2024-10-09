using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Uncreated.Warfare.Stats;

/// <summary>
/// Saves the last time the server was able to write to a file every 10 seconds. Used to fix sessions that don't get ended properly.
/// </summary>
public static class ServerHeartbeatTimer
{
    private static DateTimeOffset? _lastBeat;
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    public static void Beat(ILogger logger)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _lastBeat = now;

        Semaphore.Wait();
        Thread.BeginCriticalRegion();
        try
        {
            string path = Data.Paths.Heartbeat;

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(path))
            {
                try
                {
                    File.Copy(path, Data.Paths.HeartbeatBackup, true);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to copy backup heartbeat.");
                }
            }

            using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            Span<byte> dtInfo = stackalloc byte[sizeof(long)];

            long unix = now.ToUnixTimeSeconds();
            MemoryMarshal.Write(dtInfo, ref unix);

            stream.Write(dtInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing heartbeat.");
        }
        finally
        {
            Semaphore.Release();
            Thread.EndCriticalRegion();
        }
    }
    public static DateTimeOffset? GetLastBeat(ILogger logger)
    {
        if (_lastBeat.HasValue)
            return _lastBeat;

        Semaphore.Wait();
        Thread.BeginCriticalRegion();
        try
        {
            Span<byte> dtInfo = stackalloc byte[sizeof(long)];
            if (File.Exists(Data.Paths.Heartbeat))
            {
                using FileStream stream = new FileStream(Data.Paths.Heartbeat, FileMode.Open, FileAccess.Read, FileShare.Read);
                int bytes = stream.Read(dtInfo);
                if (bytes >= sizeof(long))
                    return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(dtInfo));
                else
                    logger.LogWarning("Heartbeat file not valid.");
            }
            else
                logger.LogInformation("Heartbeat file not present.");

            if (File.Exists(Data.Paths.HeartbeatBackup))
            {
                using FileStream stream = new FileStream(Data.Paths.Heartbeat, FileMode.Open, FileAccess.Read, FileShare.Read);
                int bytes = stream.Read(dtInfo);
                if (bytes >= sizeof(long))
                    return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(dtInfo));
                else
                    logger.LogWarning("Heartbeat backup file not valid.");
            }
            else
                logger.LogInformation("Heartbeat backup file not present.");

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading last heartbeat.");
            return null;
        }
        finally
        {
            Semaphore.Release();
            Thread.EndCriticalRegion();
        }
    }
}
