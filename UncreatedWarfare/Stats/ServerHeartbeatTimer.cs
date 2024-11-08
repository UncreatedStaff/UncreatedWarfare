using System;
using System.IO;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Stats;

/// <summary>
/// Saves the last time the server was able to write to a file every 10 seconds. Used to fix sessions that don't get ended properly.
/// </summary>
public class ServerHeartbeatTimer : IHostedService, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<ServerHeartbeatTimer> _logger;
    private readonly ILoopTicker _ticker;
    private DateTimeOffset? _lastBeat;

    public ServerHeartbeatTimer(ILoopTickerFactory tickerFactory, ILogger<ServerHeartbeatTimer> logger)
    {
        _logger = logger;
        _ticker = tickerFactory.CreateTicker(TimeSpan.FromSeconds(10d), true, false, Beat);
    }

    UniTask IHostedService.StartAsync(CancellationToken token) => UniTask.CompletedTask;
    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        _ticker.Dispose();
        return UniTask.CompletedTask;
    }

    public DateTimeOffset? GetLastBeat()
    {
        if (_lastBeat.HasValue)
            return _lastBeat;

        _semaphore.Wait();
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
                    _logger.LogWarning("Heartbeat file not valid.");
            }
            else
                _logger.LogInformation("Heartbeat file not present.");

            if (File.Exists(Data.Paths.HeartbeatBackup))
            {
                using FileStream stream = new FileStream(Data.Paths.Heartbeat, FileMode.Open, FileAccess.Read, FileShare.Read);
                int bytes = stream.Read(dtInfo);
                if (bytes >= sizeof(long))
                    return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(dtInfo));
                else
                    _logger.LogWarning("Heartbeat backup file not valid.");
            }
            else
                _logger.LogInformation("Heartbeat backup file not present.");

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading last heartbeat.");
            return null;
        }
        finally
        {
            _semaphore.Release();
            Thread.EndCriticalRegion();
        }
    }

    private void Beat(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _lastBeat = now;

        _semaphore.Wait();
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
                    _logger.LogError(ex, "Failed to copy backup heartbeat.");
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
            _logger.LogError(ex, "Error writing heartbeat.");
        }
        finally
        {
            _semaphore.Release();
            Thread.EndCriticalRegion();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}