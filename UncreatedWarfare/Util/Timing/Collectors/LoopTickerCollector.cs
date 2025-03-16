using System;
using System.Collections.Generic;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Util.Timing.Collectors;

public abstract class LoopTickerCollector<T> : IDisposable
{
    private readonly ILoopTicker _ticker;
    private readonly Action<T>? _onItemAdded;
    private readonly Action<T>? _onItemRemove;
    private readonly TrackingList<T> _collection;
    public ReadOnlyTrackingList<T> Collection => _collection.AsReadOnly();

    protected LoopTickerCollector(ILoopTicker ticker, Action<T>? onItemAdded, Action<T>? onItemRemove)
    {
        _ticker = ticker;
        _onItemAdded = onItemAdded;
        _onItemRemove = onItemRemove;
        _collection = new TrackingList<T>();
        ticker.OnTick += OnTick;
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        foreach (T item in ItemsToAdd())
        {
            if (_collection.AddIfNotExists(item))
                _onItemAdded?.Invoke(item);
        }
        foreach (T item in ItemsToRemove())
        {
            if (_collection.Remove(item))
                _onItemRemove?.Invoke(item);
        }
    }

    protected LoopTickerCollector(ILoopTicker ticker) : this(ticker, null, null) { }

    protected abstract IEnumerable<T> ItemsToAdd();
    protected abstract IEnumerable<T> ItemsToRemove();

    /// <inheritdoc />
    public void Dispose()
    {
        _ticker.OnTick -= OnTick;
    }
}

public delegate IEnumerable<T> GetObjects<out T>();
public delegate Vector3 GetPosition<in T>(T item);