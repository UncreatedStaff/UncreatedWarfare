using System;
using Uncreated.Warfare.Database.Abstractions;
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
    private readonly IStatsDbContext _dbContext;
    private readonly ILogger<DatabaseStatsBuffer> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _loopTicker;

    public IStatsDbContext DbContext => _dbContext;
    public DatabaseStatsBuffer(IStatsDbContext dbContext, ILogger<DatabaseStatsBuffer> logger, ILoopTickerFactory loopTickerFactory)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _logger = logger;
        _loopTickerFactory = loopTickerFactory;
    }

    public async Task FlushAsync(CancellationToken token = default)
    {
        if (!_dbContext.ChangeTracker.HasChanges())
        {
            return;
        }

        await _semaphore.WaitAsync(10000, token).ConfigureAwait(false);
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
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        _logger.LogConditional("Flushed stats data.");
    }

    public Task WaitAsync(CancellationToken token = default)
    {
        return _semaphore.WaitAsync(10000, token);
    }

    public void Release()
    {
        _semaphore.Release();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token) => UniTask.CompletedTask;

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(10000, token).ConfigureAwait(false);
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
        TimeSpan delay = TimeSpan.FromSeconds(5);
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
