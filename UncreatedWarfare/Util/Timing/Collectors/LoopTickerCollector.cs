using SDG.Framework.Utilities;
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
        IEnumerable<T> itemsToAdd = ItemsToAdd(out bool pooled);
        foreach (T item in itemsToAdd)
        {
            if (_collection.AddIfNotExists(item))
                _onItemAdded?.Invoke(item);
        }

        if (itemsToAdd is List<T> list && pooled)
            ListPool<T>.release(list);

        IEnumerable<T> itemsToRemove = ItemsToRemove(out pooled);
        foreach (T item in itemsToRemove)
        {
            if (_collection.Remove(item))
                _onItemRemove?.Invoke(item);
        }

        if (itemsToRemove is List<T> list2 && pooled)
            ListPool<T>.release(list2);
    }

    protected LoopTickerCollector(ILoopTicker ticker) : this(ticker, null, null) { }

    protected abstract IEnumerable<T> ItemsToAdd(out bool pooled);
    protected abstract IEnumerable<T> ItemsToRemove(out bool pooled);

    /// <inheritdoc />
    public void Dispose()
    {
        _ticker.OnTick -= OnTick;
    }
}

public delegate IEnumerable<T> GetObjects<out T>();
public delegate Vector3 GetPosition<in T>(T item);