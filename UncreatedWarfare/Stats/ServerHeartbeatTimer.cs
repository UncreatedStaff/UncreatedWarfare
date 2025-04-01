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
    private static readonly DateTime MinDateTime = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<ServerHeartbeatTimer> _logger;
    private readonly ILoopTicker _ticker;
    private DateTimeOffset? _lastBeat;
    private readonly string _path;
    private readonly string _backupPath;

    public ServerHeartbeatTimer(ILoopTickerFactory tickerFactory, ILogger<ServerHeartbeatTimer> logger)
    {
        _logger = logger;
        
        string folder = Path.Combine(UnturnedPaths.RootDirectory.FullName, ServerSavedata.directoryName, Provider.serverID);
        _path = Path.Combine(folder, "heartbeat.dat");
        _backupPath = Path.Combine(folder, "heartbeat_backup.dat");

        _ticker = tickerFactory.CreateTicker(TimeSpan.FromSeconds(10d), false, false, Beat);
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        DateTimeOffset? lastHost = GetLastBeat();
        if (lastHost.HasValue)
        {
            _logger.LogInformation("Server last online: {0}.", lastHost.Value);
        }
        else
        {
            _logger.LogInformation("Unknown server last online timestamp.");
        }
        return UniTask.CompletedTask;
    }

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
            if (File.Exists(_path))
            {
                using FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.SequentialScan);
                int bytes = stream.Read(dtInfo);
                if (bytes >= sizeof(long))
                {
                    DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds(MemoryMarshal.Read<long>(dtInfo));
                    DateTime date = dt.UtcDateTime;
                    if (date < MinDateTime || date > DateTime.UtcNow)
                    {
                        _logger.LogWarning("Heartbeat file corrupted.");
                    }
                    else
                    {
                        return _lastBeat = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(dtInfo));
                    }
                }
                else
                    _logger.LogWarning("Heartbeat file not valid.");
            }
            else
                _logger.LogInformation("Heartbeat file not present.");

            if (File.Exists(_backupPath))
            {
                using FileStream stream = new FileStream(_backupPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.SequentialScan);
                int bytes = stream.Read(dtInfo);
                if (bytes >= sizeof(long))
                {
                    DateTimeOffset dt = DateTimeOffset.FromUnixTimeSeconds(MemoryMarshal.Read<long>(dtInfo));
                    DateTime date = dt.UtcDateTime;
                    if (date < MinDateTime || date > DateTime.UtcNow)
                    {
                        _logger.LogWarning("Heartbeat backup file corrupted.");
                    }
                    else
                    {
                        return _lastBeat = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(dtInfo));
                    }
                }
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
        _semaphore.Wait();
        Thread.BeginCriticalRegion();
        try
        {
            string? dir = Path.GetDirectoryName(_path);
            if (dir != null)
                Directory.CreateDirectory(dir);

            if (File.Exists(_path))
            {
                try
                {
                    File.Copy(_path, _backupPath, true);
                    try
                    {
                        File.SetAttributes(_backupPath, FileAttributes.Hidden);
                    }
                    catch { /* ignored */ }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy backup heartbeat.");
                }
            }

            using (FileStream stream = new FileStream(_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1, FileOptions.SequentialScan))
            {
                Span<byte> dtInfo = stackalloc byte[sizeof(long)];

                long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                MemoryMarshal.Write(dtInfo, ref unix);

                stream.Write(dtInfo);
                stream.SetLength(8);
            }

            TrySetHidden(_path, true);
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

    private void TrySetHidden(string file, bool isHidden)
    {
        try
        {
            if (!File.Exists(file))
                return;

            FileAttributes oldAttributes = File.GetAttributes(file);
            FileAttributes attributes;
            if (isHidden)
                attributes = oldAttributes | FileAttributes.Hidden;
            else
                attributes = oldAttributes & ~FileAttributes.Hidden;

            if (attributes != oldAttributes)
                File.SetAttributes(file, attributes);
        }
#if DEBUG
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed setting hidden attribute on {0}.", file);
        }
#else
        catch { /* ignored */ }
#endif
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}