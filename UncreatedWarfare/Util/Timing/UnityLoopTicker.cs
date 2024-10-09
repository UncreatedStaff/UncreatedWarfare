using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Logging;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Uses a Unity <see cref="Coroutine"/> with the <see cref="TimeUtility"/> singleton.
/// </summary>
public class UnityLoopTicker<TState> : ILoopTicker<TState>
{
    private readonly ILogger _logger;
    private readonly DateTime _createdAt;
    private DateTime _lastInvokedAt;
    private Coroutine? _coroutine;
    private MonoBehaviour? _component;
    private bool _isDisposed;

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
    public UnityLoopTicker(MonoBehaviour component, ILogger logger, TimeSpan periodicDelay, bool invokeImmediately, TState? state, TickerCallback<ILoopTicker<TState>>? onTick = null)
        : this(component, logger, invokeImmediately ? TimeSpan.Zero : periodicDelay, periodicDelay <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : periodicDelay, state, onTick) { }

    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="initialDelay">How long to wait to initially invoke the timer.</param>
    /// <param name="periodicDelay">How often to invoke the timer.</param>
    /// <param name="onTick">Callback since the timer being invoked now would mean you couldn't subscribe to the event first.</param>
    public UnityLoopTicker(MonoBehaviour component, ILogger logger, TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        _logger = logger;
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
        bool isGameThread = GameThread.IsCurrent;
        if (isGameThread)
        {
            if (initialDelay == TimeSpan.Zero)
            {
                InvokeTimer();
                if (periodicDelay <= TimeSpan.Zero)
                    return;
            }

            _component = component;
            _coroutine = _component.StartCoroutine(Coroutine(initialDelay));
        }
        else
        {
            DateTime st = DateTime.UtcNow;
            _component = component;
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

                _coroutine = _component.StartCoroutine(Coroutine(invokedAlready ? TimeSpan.Zero : newInitialDelay));
            });
        }
    }

    private IEnumerator Coroutine(TimeSpan initialDelay)
    {
        if (_isDisposed)
            yield break;

        if (initialDelay > TimeSpan.Zero)
        {
            yield return new WaitForSecondsRealtime((float)initialDelay.TotalSeconds);
            InvokeTimer();
        }

        if (PeriodicDelay <= TimeSpan.Zero)
            yield break;

        while (!_isDisposed)
        {
            yield return new WaitForSecondsRealtime((float)PeriodicDelay.TotalSeconds);
            InvokeTimer();
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
        _isDisposed = true;

        if (GameThread.IsCurrent)
        {
            if (_coroutine != null && _component != null)
                _component.StopCoroutine(_coroutine);

            _coroutine = null;
            _component = null;
        }
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (_coroutine != null && _component != null)
                    _component.StopCoroutine(_coroutine);

                _coroutine = null;
                _component = null;
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
            _logger.LogError(ex, "Error invoking loop ticker.");
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
    private readonly WarfareLifetimeComponent _component;
    private readonly ILogger<UnityLoopTickerFactory> _logger;

    public UnityLoopTickerFactory(WarfareLifetimeComponent component, ILogger<UnityLoopTickerFactory> logger)
    {
        _component = component;
        _logger = logger;
    }

    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan periodicDelay, bool invokeImmediately, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new UnityLoopTicker<object>(_component, _logger, periodicDelay, invokeImmediately, null, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker CreateTicker(TimeSpan initialDelay, TimeSpan periodicDelay, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null)
    {
        return new UnityLoopTicker<object>(_component, _logger, periodicDelay, periodicDelay, null, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan periodicDelay, bool invokeImmediately, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new UnityLoopTicker<TState>(_component, _logger, periodicDelay, invokeImmediately, state, onTick);
    }

    /// <inheritdoc />
    public ILoopTicker<TState> CreateTicker<TState>(TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null)
    {
        return new UnityLoopTicker<TState>(_component, _logger, periodicDelay, periodicDelay, state, onTick);
    }
}