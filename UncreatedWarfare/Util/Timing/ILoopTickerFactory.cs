using System;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Creates <see cref="ILoopTicker"/>'s.
/// </summary>
public interface ILoopTickerFactory
{
    /// <summary>
    /// Create a new ticker.
    /// </summary>
    /// <param name="periodicDelay">How often to invoke the ticker.</param>
    /// <param name="invokeImmediately">If the ticker should be invoked now or wait a period.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the event being invoked now would mean you couldn't subscribe to the event first.</param>
    ILoopTicker CreateTicker(TimeSpan periodicDelay, bool invokeImmediately, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null);

    /// <summary>
    /// Create a new ticker.
    /// </summary>
    /// <param name="initialDelay">How long to wait to initially invoke the ticker.</param>
    /// <param name="periodicDelay">How often to invoke the ticker.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the event being invoked now would mean you couldn't subscribe to the event first.</param>
    ILoopTicker CreateTicker(TimeSpan initialDelay, TimeSpan periodicDelay, bool queueOnGameThread, TickerCallback<ILoopTicker>? onTick = null);

    /// <summary>
    /// Create a new ticker.
    /// </summary>
    /// <param name="periodicDelay">How often to invoke the ticker.</param>
    /// <param name="invokeImmediately">If the ticker should be invoked now or wait a period.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the event being invoked now would mean you couldn't subscribe to the event first.</param>
    ILoopTicker<TState> CreateTicker<TState>(TimeSpan periodicDelay, bool invokeImmediately, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null);

    /// <summary>
    /// Create a new ticker.
    /// </summary>
    /// <param name="initialDelay">How long to wait to initially invoke the ticker.</param>
    /// <param name="periodicDelay">How often to invoke the ticker.</param>
    /// <param name="queueOnGameThread">Callback will always be fired on the game thread (if it exists).</param>
    /// <param name="onTick">Callback since the event being invoked now would mean you couldn't subscribe to the event first.</param>
    ILoopTicker<TState> CreateTicker<TState>(TimeSpan initialDelay, TimeSpan periodicDelay, TState? state, bool queueOnGameThread, TickerCallback<ILoopTicker<TState>>? onTick = null);
}