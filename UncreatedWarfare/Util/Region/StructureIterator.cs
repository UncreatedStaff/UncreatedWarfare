using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Enumerates through all structures.
/// </summary>
public struct StructureIterator : IEnumerable<StructureInfo>, IEnumerator<StructureInfo>
{
    private readonly byte _cx, _cy;
    private SurroundingRegionsIterator _xyIterator;
    private StructureInfo _current;
    private int _index;
    private RegionCoord _coord;
    private bool _hasInited;
    private List<StructureDrop>? _region;
    public StructureIterator(byte x, byte y, byte maxRegionDistance = 255)
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
            _region = StructureManager.regions[coord.x, coord.y].drops;
            _index = _region.Count;
            _hasInited = true;
        }

        while (true)
        {
            --_index;
            if (_index < 0)
            {
                if (!_xyIterator.MoveNext())
                    return false;

                RegionCoord coord = _xyIterator.Current;
                _coord = coord;
                _region = StructureManager.regions[coord.x, coord.y].drops;
                _index = _region.Count;
                continue;
            }

            _current = new StructureInfo(_region![_index], _index, _coord);
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

    public StructureInfo Current => _current;
    object IEnumerator.Current => _current;
    public readonly StructureIterator GetEnumerator() => new StructureIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly IEnumerator<StructureInfo> IEnumerable<StructureInfo>.GetEnumerator() => new StructureIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly IEnumerator IEnumerable.GetEnumerator() => new StructureIterator(_cx, _cy, _xyIterator.MaxRegionDistance);
    readonly void IDisposable.Dispose() { }
}
