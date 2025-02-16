using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Enumerates through all structures.
/// </summary>
public struct DroppedItemIterator : IEnumerable<ItemInfo>, IEnumerator<ItemInfo>
{
    private readonly byte _cx, _cy;
    private SurroundingRegionsIterator _xyIterator;
    private ItemInfo _current;
    private int _index;
    private RegionCoord _coord;
    private bool _hasInited;
    private List<ItemData>? _region;
    public DroppedItemIterator(byte x, byte y, byte maxRegionDistance = 255)
    {
        _cx = x;
        _cy = y;
        _index = -1;
        _hasInited = false;

        _xyIterator = new SurroundingRegionsIterator(x, y, maxRegionDistance);
    }

    public bool MoveNext()
    {
        if (!_hasInited)
        {
            if (!_xyIterator.MoveNext())
                return false;

            RegionCoord coord = _xyIterator.Current;
            _coord = coord;
            _region = ItemManager.regions[coord.x, coord.y].items;
            _hasInited = true;
        }

        while (true)
        {
            ++_index;
            if (_index >= _region!.Count)
            {
                if (!_xyIterator.MoveNext())
                    return false;

                RegionCoord coord = _xyIterator.Current;
                _coord = coord;
                _region = ItemManager.regions[coord.x, coord.y].items;
                _index = -1;
                continue;
            }

            _current = new ItemInfo(_region![_index], _index, _coord);
            return true;
        }
    }

    public void Reset()
    {
        _xyIterator.Reset();
        _current = default;
        _index = -1;
        _hasInited = false;
    }

    public ItemInfo Current => _current;
    object IEnumerator.Current => _current;
    public readonly DroppedItemIterator GetEnumerator() => new DroppedItemIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly IEnumerator<ItemInfo> IEnumerable<ItemInfo>.GetEnumerator() => new DroppedItemIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly IEnumerator IEnumerable.GetEnumerator() => new DroppedItemIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly void IDisposable.Dispose() { }
}
