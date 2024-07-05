using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Iterate through all <see cref="Regions"/> in a set.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
public struct RegionsIterator : IEnumerator<RegionCoord>, IEnumerable<RegionCoord>
{
    private int _x;
    private int _y;
    private readonly bool _yPrimary;
    private RegionCoord _current;
    public bool MoveNext()
    {
        if (_yPrimary)
        {
            ++_x;
            if (_x >= Regions.WORLD_SIZE)
            {
                _x = 0;
                ++_y;
                if (_y >= Regions.WORLD_SIZE)
                    return false;
            }
        }
        else
        {
            ++_y;
            if (_y >= Regions.WORLD_SIZE)
            {
                _y = 0;
                ++_x;
                if (_x >= Regions.WORLD_SIZE)
                    return false;
            }
        }
        _current = new RegionCoord((byte)_x, (byte)_y);
        return true;
    }

    public RegionsIterator()
    {
        _yPrimary = false;
        _x = 0;
        _y = -1;
    }
    public RegionsIterator(bool yPrimary = false)
    {
        _yPrimary = yPrimary;
        _x = yPrimary ? -1 : 0;
        _y = yPrimary ? 0 : -1;
    }
    public void Reset()
    {
        _x = _yPrimary ? -1 : 0;
        _y = _yPrimary ? 0 : -1;
    }

    public RegionCoord Current => _current;
    object IEnumerator.Current => Current;

    readonly void IDisposable.Dispose() { }
    public readonly RegionsIterator GetEnumerator() => new RegionsIterator(_yPrimary);
    readonly IEnumerator<RegionCoord> IEnumerable<RegionCoord>.GetEnumerator() => new RegionsIterator(_yPrimary);
    readonly IEnumerator IEnumerable.GetEnumerator() => new RegionsIterator(_yPrimary);
}
