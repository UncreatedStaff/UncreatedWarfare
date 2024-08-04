using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Uses a Unity <see cref="Coroutine"/> with the <see cref="TimeUtility"/> singleton.
/// </summary>
public class UnityLoopTicker<TState> : ILoopTicker<TState>
{
    private readonly DateTime _createdAt;
    private DateTime _lastInvokedAt;
    private Coroutine? _coroutine;

    /// <inheritdoc />
    public TimeSpan InitialDelay { get; }

    /// <inheritdoc />
    public TimeSpan PeriodicDelay { get; }

    /// <inheritdoc />
    public event TickerCallback<ILoopTicker<TState>>? OnTick;

    /// <inheritdoc />
    public TState? State { get; }

    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="periodicDelay">How often to invoke the timer.</param>
    /// <param name="invokeImmediately">If the timer should be invoked now or wait a period.</param>
    /// <param name="onTick">Callback since the timer being invoked now would mean you couldn't subscribe to the event first.</param>
    public UnityLoopTicker(TimeSpan periodicDelay, bool invokeImmediately, TState? state, TickerCallback<ILoopTicker<TState>>? onTick = null)
        : this(invokeImmediately ? TimeSpan.Zero : periodicDelay, periodicDelay <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : periodicDelay, state, onTick) { }

    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="initialDelay">How long to wait to initially invoke the timer.</param>
    /// <param name="periodicDelay">How often to invoke the timer.</param>
    /// <param name="onTick">Callback since the timer being invoked now would mean you couldn't subscribe to the event first.</param>
    public UnityLoopTicker(TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        if (periodicDelay <= TimeSpan.Zero)
            periodicDelay = Timeout.InfiniteTimeSpan;

        InitialDelay = initialDelay;
        PeriodicDelay = periodicDelay;
        State = state;

        if (onTick != null)
            OnTick += onTick;

        _createdAt = DateTime.UtcNow;

        if (InitialDelay < TimeSpan.Zero)
            return;

        _lastInvokedAt = _createdAt;
        bool isGameThread = Thread.CurrentThread.IsGameThread();
        if (isGameThread)
        {
            if (initialDelay == TimeSpan.Zero)
            {
                InvokeTimer();
                if (periodicDelay <= TimeSpan.Zero)
                    return;
            }

            _coroutine = TimeUtility.InvokeAfterDelay(InvokeTimer, (float)(initialDelay == TimeSpan.Zero ? periodicDelay : initialDelay).TotalSeconds);
        }
        else
        {
            DateTime st = DateTime.UtcNow;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                TimeSpan dif = DateTime.UtcNow - st;
                TimeSpan newInitialDelay = InitialDelay - dif;
                bool invokedAlready = false;
                if (newInitialDelay <= TimeSpan.Zero)
                {
                    invokedAlready = true;
                    InvokeTimer();
                    if (PeriodicDelay <= TimeSpan.Zero)
                        return;
                }

                _coroutine = TimeUtility.InvokeAfterDelay(InvokeTimer, (float)(invokedAlready ? PeriodicDelay : newInitialDelay).TotalSeconds);
            });
        }
    }

    ~UnityLoopTicker()
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
        Coroutine? coroutine = Interlocked.Exchange(ref _coroutine, null);
        if (coroutine == null)
            return;

        if (Thread.CurrentThread.IsGameThread())
        {
            TimeUtility.StaticStopCoroutine(coroutine);
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                TimeUtility.StaticStopCoroutine(coroutine);
            });
        }

        if (disposing)
            GC.SuppressFinalize(this);
    }

    private void InvokeTimer()
    {
        DateTime utcNow = DateTime.UtcNow;
        try
        {
            OnTick?.Invoke(this, utcNow - _createdAt, utcNow - _lastInvokedAt);
        }
        catch (Exception ex)
        {
            L.LogError("Error invoking ticker.");
            L.LogError(ex);
        }
        finally
        {
            _lastInvokedAt = utcNow;
        }
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
/// Creates <see cref="UnityLoopTicker{TState}"/>'s.
/// </summary>
public class UnityLoopTickerFactory : ILoopTickerFactory
{
    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan periodicDelay, bool invokeImmediately, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new UnityLoopTicker<object>(periodicDelay, invokeImmediately, null, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan initialDelay, TimeSpan periodicDelay, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new UnityLoopTicker<object>(periodicDelay, periodicDelay, null, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan periodicDelay, bool invokeImmediately, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new UnityLoopTicker<TState>(periodicDelay, invokeImmediately, state, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new UnityLoopTicker<TState>(periodicDelay, periodicDelay, state, onTick);
    }
}