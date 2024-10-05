using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Util.Timing.Collectors;
public class ProximityCollector<T> : LoopTickerCollector<T>
{
    private GetObjects<T> _getObjects;
    private GetPosition<T> _getPosition;
    public IProximity Proximity { get; private set; }

    public ProximityCollector(ProximityCollectorOptions options) : base(options.Ticker, options.OnItemAdded, options.OnItemRemoved)
    {
        Proximity = options.Proximity;
        _getObjects = options.ObjectsToCollect;
        _getPosition = options.PositionFunction;
    }

    protected override IEnumerable<T> ItemsToAdd()
    {
        return _getObjects.Invoke().Where(i => Proximity.containsPoint(_getPosition.Invoke(i)));
    }

    protected override IEnumerable<T> ItemsToRemove()
    {
        return _getObjects.Invoke().Where(i => !Proximity.containsPoint(_getPosition.Invoke(i)));
    }
    public struct ProximityCollectorOptions
    {
        required public ILoopTicker Ticker;
        required public IProximity Proximity;
        required public GetObjects<T> ObjectsToCollect;
        required public GetPosition<T> PositionFunction;
        public Action<T> OnItemAdded;
        public Action<T> OnItemRemoved;

        public ProximityCollectorOptions()
        {
        }
    }
}
