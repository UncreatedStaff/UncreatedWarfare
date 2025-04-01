using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Iterate through a region list.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
public struct ListRegionsEnumerator<T> : IEnumerable<T>, IEnumerator<T>
{
    private readonly List<T>[,] _regions;

    private SurroundingRegionsIterator _xyIterator;
    private List<T>? _currentRegion;
    private int _index;
    private T? _current;

    public RegionCoord Coordinate = RegionCoord.ZERO;
    public int Index => _index;
    public List<T>[,] Regions => _regions;
    public List<T>? Region => _currentRegion;
    public ListRegionsEnumerator(List<T>[,] regions) : this(regions, (byte)(SDG.Unturned.Regions.WORLD_SIZE / 2), (byte)(SDG.Unturned.Regions.WORLD_SIZE / 2), 255) { }
    public ListRegionsEnumerator(List<T>[,] regions, byte centerX, byte centerY, byte maxRegionDistance = 255)
    {
        _regions = regions;
        _xyIterator = new SurroundingRegionsIterator(centerX, centerY, maxRegionDistance);
        _currentRegion = null;
        _current = default;
    }

    public bool MoveNext()
    {
        --_index;
        while (_currentRegion == null || _index < 0)
        {
            if (_regions == null)
                return false;
            if (!_xyIterator.MoveNext())
            {
                _current = default;
                return false;
            }
            Coordinate = _xyIterator.Current;
            _currentRegion = _regions[Coordinate.x, Coordinate.y];
            if (_currentRegion.Count == 0)
                continue;
            _index = _currentRegion.Count - 1;
        }

        _current = _currentRegion[_index];
        return true;
    }

    public void Reset()
    {
        _xyIterator.Reset();
        _currentRegion = null;
        _current = default;
    }

    public T Current => _current!;
    object IEnumerator.Current => _current!;
    public readonly ListRegionsEnumerator<T> GetEnumerator() => new ListRegionsEnumerator<T>(_regions, _xyIterator.StartX, _xyIterator.StartY, _xyIterator.MaxRegionDistance);
    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => new ListRegionsEnumerator<T>(_regions, _xyIterator.StartX, _xyIterator.StartY, _xyIterator.MaxRegionDistance);
    readonly IEnumerator IEnumerable.GetEnumerator() => new ListRegionsEnumerator<T>(_regions, _xyIterator.StartX, _xyIterator.StartY, _xyIterator.MaxRegionDistance);
    readonly void IDisposable.Dispose() { }
}
