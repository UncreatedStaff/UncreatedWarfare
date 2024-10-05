using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;
using static SDG.Provider.SteamGetInventoryResponse;

namespace Uncreated.Warfare.Util.Timing.Collectors;

public abstract class LoopTickerCollector<T>
{
    protected TrackingList<T> _collection;
    public ReadOnlyTrackingList<T> Collection => _collection.AsReadOnly();

    public LoopTickerCollector(ILoopTicker ticker, Action<T>? onItemAdded, Action<T>? onItemRemove)
    {
        _collection = new TrackingList<T>();
        ticker.OnTick += (ticker, timeSinceStart, deltaTime) =>
        {
            Console.WriteLine("Collector count: " + ItemsToAdd().Count());
            foreach (T item in ItemsToAdd())
            {
                _collection.AddIfNotExists(item);
                onItemAdded?.Invoke(item);
                Console.WriteLine("Added to collector: " + item);
            }
            foreach (T item in ItemsToRemove())
            {
                _collection.Remove(item);
                onItemRemove?.Invoke(item);
            }
        };
    }
    LoopTickerCollector(ILoopTicker ticker) : this(ticker, null, null) { }
    protected abstract IEnumerable<T> ItemsToAdd();
    protected abstract IEnumerable<T> ItemsToRemove();
}
public delegate IEnumerable<T> GetObjects<T>();
public delegate Vector3 GetPosition<T>(T item);

