using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Enumerates through all barricades optionally including planted and/or non-planted barricades.
/// </summary>
public struct BarricadeIterator : IEnumerable<BarricadeInfo>, IEnumerator<BarricadeInfo>
{
    private readonly byte _cx, _cy;
    private readonly bool _nonPlanted, _planted;
    private SurroundingRegionsIterator _xyIterator;
    private BarricadeInfo _current;
    private int _index;
    private ushort _plant;
    private RegionCoord _coord;
    private int _state; // 0 = try init non planted, 1 = try init planted, 2 = loop non planted, 3 = loop planted
    private List<BarricadeDrop>? _region;
    public BarricadeIterator(byte x, byte y, bool nonPlanted, bool planted, byte maxRegionDistance = 255)
    {
        _cx = x;
        _cy = y;
        _nonPlanted = nonPlanted;
        _planted = planted;
        _plant = ushort.MaxValue;
        _index = -1;
        _state = nonPlanted ? 0 : 1;

        _xyIterator = new SurroundingRegionsIterator(x, y, maxRegionDistance);
    }

    public bool MoveNext()
    {
        switch (_state)
        {
            // try init non planted
            case 0:
                if (!_xyIterator.MoveNext())
                    goto case 1;

                RegionCoord coord = _xyIterator.Current;
                _coord = coord;
                _region = BarricadeManager.regions[coord.x, coord.y].drops;
                _state = 2;
                goto case 2;

            // try init planted
            case 1:
                if (!_planted)
                    return false;

                if (BarricadeManager.vehicleRegions.Count == 0)
                    return false;

                _plant = 0;
                _region = BarricadeManager.vehicleRegions[0].drops;
                _state = 3;
                goto case 3;

            // loop non planted
            case 2:
                ++_index;
                if (_index >= _region!.Count)
                {
                    if (!_xyIterator.MoveNext())
                        goto case 1;

                    coord = _xyIterator.Current;
                    _coord = coord;
                    _region = BarricadeManager.regions[coord.x, coord.y].drops;
                    _index = -1;
                    goto case 2;
                }

                _current = new BarricadeInfo(_region![_index], _index, _coord);
                return true;

            // loop planted
            case 3:
                ++_index;
                if (_index >= _region!.Count)
                {
                    unchecked { ++_plant; }
                    if (_plant == 0 || _plant >= BarricadeManager.vehicleRegions.Count)
                        return false;

                    _region = BarricadeManager.vehicleRegions[_plant].drops;
                    _index = -1;
                    goto case 3;
                }

                _current = new BarricadeInfo(_region![_index], _index, _plant);
                return true;

            default:
                return false;
        }
    }

    public void Reset()
    {
        _xyIterator.Reset();
        _current = default;
        _index = -1;
        _state = _nonPlanted ? 0 : 1;
        _region = null;
    }

    public BarricadeInfo Current => _current;
    object IEnumerator.Current => _current;
    public readonly BarricadeIterator GetEnumerator() => new BarricadeIterator(_cx, _cy, _nonPlanted, _planted, _xyIterator.MaxRegionDistance);
    readonly IEnumerator<BarricadeInfo> IEnumerable<BarricadeInfo>.GetEnumerator() => new BarricadeIterator(_cx, _cy, _nonPlanted, _planted, _xyIterator.MaxRegionDistance);
    readonly IEnumerator IEnumerable.GetEnumerator() => new BarricadeIterator(_cx, _cy, _nonPlanted, _planted, _xyIterator.MaxRegionDistance);
    readonly void IDisposable.Dispose() { }
}
