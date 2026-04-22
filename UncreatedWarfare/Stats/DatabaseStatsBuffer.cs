using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Stats;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Stats;

/// <summary>
/// Keeps track of pending stats and saves them every 30 seconds.
/// </summary>
/// <remarks>
/// Usages of this class should set <see cref="IsDirty"/> and use <see cref="WaitAsync"/> and <see cref="Release"/> to synchronize on <see cref="DbContext"/>.
/// No <see cref="UniTask.SwitchToMainThread"/> function should be used inside the synchronized block.
/// </remarks>
public class DatabaseStatsBuffer : IDisposable, IHostedService, ILayoutHostedService
{
    private readonly ConcurrentQueue<object> _statEntries = new ConcurrentQueue<object>();
    private readonly IStatsDbContext _dbContext;
    private readonly ILogger<DatabaseStatsBuffer> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _loopTicker;

    public bool TrackStats { get; set; }

    public DatabaseStatsBuffer(IStatsDbContext dbContext, ILogger<DatabaseStatsBuffer> logger, ILoopTickerFactory loopTickerFactory)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _logger = logger;
        _loopTickerFactory = loopTickerFactory;
        TrackStats = true;
    }

    public void Enqueue(DamageRecord dmg)
    {
        if (TrackStats)
            _statEntries.Enqueue(dmg);
    }

    public void Enqueue(DeathRecord death)
    {
        if (TrackStats)
            _statEntries.Enqueue(death);
    }

    public void Enqueue(AidRecord aid)
    {
        if (TrackStats)
            _statEntries.Enqueue(aid);
    }

    public void Enqueue(FobRecord fob)
    {
        if (TrackStats)
            _statEntries.Enqueue(fob);
    }

    public void Enqueue(FobItemRecord fobItem)
    {
        if (TrackStats)
            _statEntries.Enqueue(fobItem);
    }

    public async Task FlushAsync(CancellationToken token = default)
    {
        if (_statEntries.Count == 0)
        {
            return;
        }

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FlushAsyncNoLock(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task FlushAsyncNoLock(CancellationToken token)
    {
        try
        {
            _logger.LogConditional("Flushing stats data...");
            while (_statEntries.TryDequeue(out object value))
            {
                switch (value)
                {
                    case DeathRecord death:
                        if (death.Id != 0)
                        {
                            _dbContext.Update(death);
                            break;
                        }

                        _dbContext.DeathRecords.Add(death);
                        if (death.KillShot == null)
                            break;

                        if (_dbContext.Entry(death.KillShot).State == EntityState.Detached)
                        {
                            death.KillShotId = death.KillShot.Id;
                            death.KillShot = null;
                        }
                        break;

                    case DamageRecord dmg:
                        if (dmg.Id == 0)
                            _dbContext.DamageRecords.Add(dmg);
                        else
                            _dbContext.Update(dmg);
                        break;

                    case AidRecord aid:
                        if (aid.Id == 0)
                            _dbContext.AidRecords.Add(aid);
                        else
                            _dbContext.Update(aid);
                        break;

                    case FobRecord fob:
                        if (fob.Id == 0)
                            _dbContext.FobRecords.Add(fob);
                        else
                            _dbContext.Update(fob);
                        break;

                    case FobItemRecord fobItem:
                        if (fobItem.Id == 0)
                            _dbContext.FobItemRecords.Add(fobItem);
                        else
                            _dbContext.Update(fobItem);
                        break;

                    default:
                        _logger.LogWarning($"Unknown record type: {value.GetType()}.");
                        break;
                }
            }

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
            _logger.LogConditional("Flushed stats data.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error flushing stat data.");
            _dbContext.ChangeTracker.Clear();
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token) => UniTask.CompletedTask;

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await FlushAsyncNoLock(token).ConfigureAwait(false);

            // prevent memory leak from old stats
            _dbContext.ChangeTracker.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        _loopTicker?.Dispose();

#if DEBUG
        TimeSpan delay = TimeSpan.FromSeconds(0.5f);
#else
        TimeSpan delay = TimeSpan.FromSeconds(30);
#endif

        _loopTicker = _loopTickerFactory.CreateTicker(delay, invokeImmediately: false, queueOnGameThread: false, onTick: (_, _, _) =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await FlushAsync(token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing stats.");
                }
            }, CancellationToken.None);
        });

        _semaphore.Release();
        return UniTask.CompletedTask;
    }

    async UniTask IHostedService.StopAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(1000, token).ConfigureAwait(false);
        try
        {
            await FlushAsyncNoLock(token).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
            _semaphore.Release();
        }
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
    }
}
