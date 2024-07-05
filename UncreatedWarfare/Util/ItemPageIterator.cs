using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Enumerates through a page of items in 'order' of XY coordinates, either forward or backwards.
/// </summary>
public struct ItemPageIterator : IEnumerable<ItemJar>, IEnumerator<ItemJar>
{
    private readonly Items _page;
    private readonly bool _reversed;
    private ItemJar? _current;

    private int _x = -1;
    private int _y = -1;

    public ItemPageIterator(Items page, bool reversed)
    {
        _page = page;
        _reversed = reversed;
    }

    public bool MoveNext()
    {
        return _reversed
            ? MoveNextReversed()
            : MoveNextForward();
    }

    private bool MoveNextReversed()
    {
        int startY = _y - (_x <= 0 ? 1 : 0);

        for (int y = startY; y >= 0; --y)
        {
            int startX = _x > 0 ? _x - 1 : _page.width;

            for (int x = startX; x >= 0; --x)
            {
                byte ind = _page.getIndex((byte)x, (byte)y);
                if (ind == byte.MaxValue)
                    continue;

                _x = x;
                _y = y;
                _current = _page.getItem(ind);
                return true;
            }
        }

        return false;
    }

    private bool MoveNextForward()
    {
        int startY = _x == _page.width ? _y + 1 : _y;

        for (int y = startY; y < _page.height; ++y)
        {
            int startX = _x == _page.width ? 0 : _x + 1;

            for (int x = startX; x < _page.width; ++x)
            {
                byte ind = _page.getIndex((byte)x, (byte)y);
                if (ind == byte.MaxValue)
                    continue;

                _x = x;
                _y = y;
                _current = _page.getItem(ind);
                return true;
            }
        }

        return false;
    }

    public void Reset()
    {
        _current = null;
        if (_reversed)
        {
            _x = _page.width;
            _y = _page.height;
        }
        else
        {
            _x = -1;
            _y = -1;
        }
    }

    public ItemJar Current => _current!;
    object IEnumerator.Current => _current!;
    public readonly ItemPageIterator GetEnumerator() => new ItemPageIterator(_page, _reversed);
    readonly IEnumerator<ItemJar> IEnumerable<ItemJar>.GetEnumerator() => new ItemPageIterator(_page, _reversed);
    readonly IEnumerator IEnumerable.GetEnumerator() => new ItemPageIterator(_page, _reversed);
    readonly void IDisposable.Dispose() { }
}