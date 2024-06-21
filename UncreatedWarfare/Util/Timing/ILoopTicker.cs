using System;

namespace Uncreated.Warfare.Util.Timing;

/// <summary>
/// Handles a <see cref="ILoopTicker"/> going off.
/// </summary>
/// <param name="ticker">Ticker object that was invoked.</param>
/// <param name="timeSinceStart">Total time since the ticker was created.</param>
/// <param name="deltaTime">Total time since the last time the ticker was invoked (or since it was created if this is the first time).</param>
public delegate void TickerCallback<in TTicker>(TTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime) where TTicker : ILoopTicker;


public interface ILoopTicker : IDisposable
{
    /// <summary>
    /// Delay before the first invocation.
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Delay between each subsequent invocation.
    /// </summary>
    TimeSpan PeriodicDelay { get; }

    /// <summary>
    /// Generic state that can be used to store information.
    /// </summary>
    object? State { get; }

    /// <summary>
    /// Invoked every <see cref="PeriodicDelay"/> seconds.
    /// </summary>
    event TickerCallback<ILoopTicker> OnTick;
}

public interface ILoopTicker<out TState> : ILoopTicker
{
    /// <summary>
    /// Generic state that can be used to store information.
    /// </summary>
    new TState? State { get; }

    /// <summary>
    /// Invoked every <see cref="Delay"/> seconds.
    /// </summary>
    new event TickerCallback<ILoopTicker<TState>> OnTick;
}