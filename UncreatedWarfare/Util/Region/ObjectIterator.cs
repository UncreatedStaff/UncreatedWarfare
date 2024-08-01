using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Enumerates through all objects using <see cref="ListRegionsEnumerator{T}"/>.
/// </summary>
public struct ObjectIterator : IEnumerable<ObjectInfo>, IEnumerator<ObjectInfo>
{
    private ListRegionsEnumerator<LevelObject> _listEnumerator;
    private ObjectInfo _current;
    public ObjectIterator(byte x, byte y, byte maxRegionDistance = 255)
    {
        _listEnumerator = new ListRegionsEnumerator<LevelObject>(LevelObjects.objects, x, y, maxRegionDistance);
    }

    public bool MoveNext()
    {
        if (!_listEnumerator.MoveNext())
        {
            return false;
        }

        LevelObject current = _listEnumerator.Current;
        _current = new ObjectInfo(current, _listEnumerator.Index, _listEnumerator.Coordinate);
        return true;
    }

    public void Reset()
    {
        _listEnumerator.Reset();
        _current = default;
    }

    public ObjectInfo Current => _current;
    object IEnumerator.Current => _current;
    public readonly ObjectIterator GetEnumerator()
    {
        ObjectIterator iterator = this;
        iterator.Reset();
        return iterator;
    }

    readonly IEnumerator<ObjectInfo> IEnumerable<ObjectInfo>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    readonly void IDisposable.Dispose() { }
}
