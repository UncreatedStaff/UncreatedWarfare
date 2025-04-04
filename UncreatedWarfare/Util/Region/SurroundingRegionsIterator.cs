﻿using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util.Region;

/// <summary>
/// Iterate through a region list starting at a position and moving outwards.
/// </summary>
/// <remarks>Stolen from DevkitServer.</remarks>
public struct SurroundingRegionsIterator : IEnumerator<RegionCoord>, IEnumerable<RegionCoord>
{
    private static readonly int[] LayerIndices = new int[Regions.WORLD_SIZE + 1];
    public readonly byte StartX;
    public readonly byte StartY;
    public readonly byte MaxRegionDistance;
    static SurroundingRegionsIterator()
    {
        LayerIndices[1] = 1;
        for (int i = 2; i < LayerIndices.Length; i++)
        {
            LayerIndices[i] = LayerIndices[i - 1] + (i - 1) * 8;
        }
    }
    private RegionCoord _current;
    private int _index;
    private int _layer;
    private int _nextLayerIndex;
    public SurroundingRegionsIterator(byte x, byte y, byte maxRegionDistance = byte.MaxValue)
    {
        MaxRegionDistance = maxRegionDistance;
        StartX = x;
        StartY = y;
        _index = -1;
        _layer = 0;
        _nextLayerIndex = 1;
        _current = default;
    }
    public bool MoveNext()
    {
        while (MoveNextIntl(out int x, out int y))
        {
            if (x < 0 || x >= Regions.WORLD_SIZE || y < 0 || y >= Regions.WORLD_SIZE)
                continue;
            _current = new RegionCoord((byte)x, (byte)y);
            return true;
        }

        return false;
    }
    private bool MoveNextIntl(out int x, out int y)
    {
        ++_index;
        if (_index == 0)
        {
            x = StartX;
            y = StartY;
            return true;
        }

        if (_index >= _nextLayerIndex)
        {
            ++_layer;
            if (_layer >= Regions.WORLD_SIZE || _layer > MaxRegionDistance)
            {
                x = 0;
                y = 0;
                return false;
            }
            _nextLayerIndex = LayerIndices[_layer + 1];
        }

        int layerIndex = _index - LayerIndices[_layer];
        int side = layerIndex % 4;
        int magnitude = layerIndex / 4;
        bool otherSide = magnitude % 2 == 1;
        magnitude = Mathf.CeilToInt(magnitude / 2f);
        switch (side)
        {
            case 0:
                y = _layer;
                if (otherSide)
                    x = magnitude;
                else
                    x = -magnitude;
                break;
            case 1:
                y = -_layer;
                if (otherSide)
                    x = -magnitude;
                else
                    x = magnitude;
                break;
            case 2:
                x = _layer;
                if (otherSide)
                    y = -magnitude;
                else
                    y = magnitude;
                break;
            default:
                x = -_layer;
                if (otherSide)
                    y = magnitude;
                else
                    y = -magnitude;
                break;
        }

        x += StartX;
        y += StartY;

        return true;
    }

    public void Reset()
    {
        _current = RegionCoord.ZERO;
        _index = -1;
        _layer = 0;
        _nextLayerIndex = 1;
    }

    public RegionCoord Current => _current;
    object IEnumerator.Current => Current;
    public readonly SurroundingRegionsIterator GetEnumerator() => new SurroundingRegionsIterator(StartX, StartY, MaxRegionDistance);
    readonly IEnumerator<RegionCoord> IEnumerable<RegionCoord>.GetEnumerator() => new SurroundingRegionsIterator(StartX, StartY, MaxRegionDistance);
    readonly IEnumerator IEnumerable.GetEnumerator() => new SurroundingRegionsIterator(StartX, StartY, MaxRegionDistance);
    readonly void IDisposable.Dispose() { }
}
