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
public class DatabaseStatsBuffer : IDisposable, IHostedService
{
    private readonly IStatsDbContext _dbContext;
    private readonly ILogger<DatabaseStatsBuffer> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _loopTicker;

    public bool IsDirty { get; set; }
    public IStatsDbContext DbContext => _dbContext;
    public DatabaseStatsBuffer(IStatsDbContext dbContext, ILogger<DatabaseStatsBuffer> logger, ILoopTickerFactory loopTickerFactory)
    {
        _dbContext = dbContext;
        _logger = logger;
        _loopTickerFactory = loopTickerFactory;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public async Task FlushAsync(CancellationToken token = default)
    {
        _logger.LogConditional("Flushing...");
        if (!IsDirty)
        {
            _logger.LogConditional("dirty");
            return;
        }

        await _semaphore.WaitAsync(10000, token).ConfigureAwait(false);
        try
        {
            _logger.LogConditional("in...");
            await FlushIntl(token).ConfigureAwait(false);
            _logger.LogConditional("done");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task FlushIntl(CancellationToken token)
    {
        await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        _dbContext.ChangeTracker.Clear();
        IsDirty = false;
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

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        _loopTicker?.Dispose();
        _loopTicker = _loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(30), invokeImmediately: false, queueOnGameThread: false, onTick: (_, _, _) =>
        {
            _logger.LogConditional("Flushing...");
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

    /// <inheritdoc />
    public async UniTask StopAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(1000, token).ConfigureAwait(false);
        await FlushAsync(token).ConfigureAwait(false);

        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
        _semaphore.Dispose();
    }
}
