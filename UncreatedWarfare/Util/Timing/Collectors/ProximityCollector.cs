using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Proximity;

namespace Uncreated.Warfare.Util.Timing.Collectors;

public class ProximityCollector<T> : LoopTickerCollector<T>
{
    private readonly GetObjects<T> _getObjects;
    private readonly GetPosition<T> _getPosition;
    public IProximity Proximity { get; private set; }

    public ProximityCollector(ProximityCollectorOptions options) : base(options.Ticker, options.OnItemAdded, options.OnItemRemoved)
    {
        Proximity = options.Proximity;
        _getObjects = options.ObjectsToCollect;
        _getPosition = options.PositionFunction;
    }

    protected override IEnumerable<T> ItemsToAdd(out bool pooled)
    {
        pooled = false;
        return _getObjects().Where(i => Proximity.TestPoint(_getPosition.Invoke(i)));
    }

    protected override IEnumerable<T> ItemsToRemove(out bool pooled)
    {
        // _getObjects().Where ... doesn't include offline players
        List<T> listPool = ListPool<T>.claim();
        pooled = true;
        foreach (T c in Collection.Where(i => !Proximity.TestPoint(_getPosition.Invoke(i))))
        {
            listPool.Add(c);
        }
        return listPool;
    }
    public struct ProximityCollectorOptions
    {
        public required ILoopTicker Ticker;
        public required IProximity Proximity;
        public required GetObjects<T> ObjectsToCollect;
        public required GetPosition<T> PositionFunction;
        public Action<T> OnItemAdded;
        public Action<T> OnItemRemoved;
    }
}
