using System;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Uses a system <see cref="Timer"/>.
/// </summary>
/// <remarks>For tests mainly as it works without Unity.</remarks>
public class TimerLoopTicker<TState> : ILoopTicker<TState>
{
    private readonly ILogger _logger;
    private readonly DateTime _createdAt;
    private DateTime _lastInvokedAt;
    private Timer? _timer;

    /// <inheritdoc />
    public TimeSpan InitialDelay { get; }

    /// <inheritdoc />
    public TimeSpan PeriodicDelay { get; }

    /// <inheritdoc />
    public event TickerCallback<ILoopTicker<TState>>? OnTick;

    /// <inheritdoc />
    public TState? State { get; }

    /// <summary>
    /// The callback will always be fired on the main thread if it exists.
    /// </summary>
    public bool QueueOnGameThread { get; }

    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="periodicDelay">How often to invoke the timer.</param>
    /// <param name="invokeImmediately">If the timer should be invoked now or wait a period.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the timer being invoked now would mean you couldn't subscribe to the event first.</param>
    public TimerLoopTicker(TimeSpan periodicDelay, ILogger logger, bool invokeImmediately, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick)
    {
        _logger = logger;
        InitialDelay = invokeImmediately ? TimeSpan.Zero : periodicDelay;
        PeriodicDelay = periodicDelay <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : periodicDelay;
        State = state;
        QueueOnGameThread = queueOnGameThread;

        if (onTick != null)
            OnTick += onTick;

        _createdAt = DateTime.UtcNow;
        if (InitialDelay < TimeSpan.Zero)
            return;

        _lastInvokedAt = _createdAt;
        if (InitialDelay == TimeSpan.Zero)
        {
            InvokeTimer(null);
            if (PeriodicDelay <= TimeSpan.Zero)
                return;
        }

        _timer = new Timer(InvokeTimer, null, InitialDelay, PeriodicDelay);
    }

    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="initialDelay">How long to wait to initially invoke the timer.</param>
    /// <param name="periodicDelay">How often to invoke the timer.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the timer being invoked now would mean you couldn't subscribe to the event first.</param>
    public TimerLoopTicker(TimeSpan initialDelay, ILogger logger, TimeSpan periodicDelay, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick)
    {
        _logger = logger;
        if (periodicDelay <= TimeSpan.Zero)
            periodicDelay = Timeout.InfiniteTimeSpan;

        InitialDelay = initialDelay;
        PeriodicDelay = periodicDelay;
        State = state;
        QueueOnGameThread = queueOnGameThread;

        if (onTick != null)
            OnTick += onTick;

        _createdAt = DateTime.UtcNow;
        if (initialDelay < TimeSpan.Zero)
            return;

        _lastInvokedAt = _createdAt;
        if (initialDelay == TimeSpan.Zero)
        {
            InvokeTimer(null);
            if (periodicDelay <= TimeSpan.Zero)
                return;
        }

        _timer = new Timer(InvokeTimer, null, initialDelay, periodicDelay);
    }

    ~TimerLoopTicker()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        Timer? timer = Interlocked.Exchange(ref _timer, null);
        if (timer == null)
            return;

        timer.Change(Timeout.Infinite, Timeout.Infinite);
        timer.Dispose();

        if (disposing)
            GC.SuppressFinalize(this);
    }

    private void InvokeTimer(object? state)
    {
        if (QueueOnGameThread && !GameThread.IsCurrent)
        {
            InvokeMainThread();
        }
        else
        {
            InvokeEvent();
        }
    }
    private void InvokeEvent()
    {
        DateTime utcNow = DateTime.UtcNow;
        try
        {
            OnTick?.Invoke(this, utcNow - _createdAt, utcNow - _lastInvokedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ticker.");
        }
        finally
        {
            _lastInvokedAt = utcNow;
        }
    }

    private void InvokeMainThread()
    {
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            InvokeEvent();
        });
    }

    /// <inheritdoc />
    event TickerCallback<ILoopTicker>? ILoopTicker.OnTick
    {
        add => OnTick += value;
        remove => OnTick -= value;
    }

    /// <inheritdoc />
    object? ILoopTicker.State => State;
}

/// <summary>
/// Creates <see cref="TimerLoopTicker{TState}"/>'s.
/// </summary>
public class TimerLoopTickerFactory : ILoopTickerFactory
{
    private readonly ILogger _logger;
    public TimerLoopTickerFactory(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(TimerLoopTicker<>));
    }

    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan periodicDelay, bool invokeImmediately, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new TimerLoopTicker<object>(periodicDelay, _logger, invokeImmediately, null, queueOnGameThread, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan initialDelay, TimeSpan periodicDelay, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new TimerLoopTicker<object>(initialDelay, _logger, periodicDelay, null, queueOnGameThread, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan periodicDelay, bool invokeImmediately, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new TimerLoopTicker<TState>(periodicDelay, _logger, invokeImmediately, state, queueOnGameThread, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new TimerLoopTicker<TState>(initialDelay, _logger, periodicDelay, state, queueOnGameThread, onTick);
    }
}