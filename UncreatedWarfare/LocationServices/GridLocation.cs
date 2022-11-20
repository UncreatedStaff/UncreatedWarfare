using SDG.Unturned;
using System;
using UnityEngine;

namespace Uncreated.Warfare.Locations;

public readonly struct GridLocation : ITranslationArgument
{
    public readonly byte X;
    public readonly byte Y;
    public readonly byte Index;
    private readonly string _toStringCache;
    /// <exception cref="ArgumentOutOfRangeException"/>
    private unsafe GridLocation(byte x, byte y, byte index)
    {
        if (index is > 9)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must either be 0 or 1-9 inclusive.");
        if (x > 11)
            throw new ArgumentOutOfRangeException(nameof(x), "X must be 0-11 inclusive.");
        if (y > 11)
            throw new ArgumentOutOfRangeException(nameof(y), "X must be 0-11 inclusive.");
        X = x;
        Y = y;
        Index = index;

        _toStringCache = ToString(x, y, index);
    }
    private static unsafe string ToString(byte x, byte y, byte index)
    {
        ++y;
        int len = y > 9 ? 5 : 4;
        if (index == 0)
            len -= 2;
        char* ptr = stackalloc char[len];

        ptr[0] = (char)(x + 65);
        if (y > 9)
        {
            ptr[1] = '1';
            ptr[2] = (char)(y % 10 + 48);
        }
        else
            ptr[1] = (char)(y + 48);
        if (index != 0)
        {
            int a = len == 5 ? 3 : 2;
            ptr[a] = '-';
            ptr[a + 1] = (char)(index + 48);
        }
        return new string(ptr, 0, len);
    }
    /// <returns>A cached string representation of the grid, formatted like A1-1.</returns>
    public override readonly string ToString() => _toStringCache;
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags) => _toStringCache;
    public static bool TryParse(string value, out GridLocation location)
    {
        if (value.Length is not > 1 and < 8)
        {
            location = default;
            return false;
        }
        char xc = value[0];
        if (xc > 96) xc -= (char)32;
        byte x = (byte)(xc - 65);
        byte y;
        if ((int)value[1] is > 47 and < 58)
        {
            if (value.Length > 2 && (int)value[2] is > 47 and < 58)
                y = (byte)((value[1] - 48) * 10 + (value[2] - 49));
            else
                y = (byte)(value[1] - 49);
        }
        else
        {
            location = default;
            return false;
        }
        byte index = 0;
        if (value.Length < 5)
            goto rtnTrue;

        if (value[3] is ' ' or '-')
        {
            for (int i = 4; value.Length > i; ++i)
            {
                if ((int)value[4] is > 47 and < 58)
                {
                    index = (byte)(value[4] - 48);
                    goto rtnTrue;
                }
                else if (value[4] is not ' ' and not '-')
                    break;
            }
        }
        location = new GridLocation(x, y, 0);
        return false;
    rtnTrue:
        location = new GridLocation(x, y, index);
        return true;
    }
    /// <exception cref="FormatException"/>
    public static GridLocation Parse(string value)
    {
        if (!TryParse(value, out GridLocation location))
            throw new FormatException("Unable to parse GridLocation.");
        return location;
    }
    private static bool _setGridConstants = false;
    private static float _toMapCoordsMultiplier;
    private static float _gridSize;
    private const int _sqrsTotal = 36;
    private static float _sqrSize;
    public GridLocation(in Vector3 pos)
    {
        if (!_setGridConstants)
        {
            _toMapCoordsMultiplier = Level.size / (Level.size - Level.border * 2f);
            _gridSize = Level.size - Level.border * 2;
            _sqrSize = Mathf.Floor(_gridSize / 36f);
            _setGridConstants = true;
        }
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float x = Level.size / 2 + _toMapCoordsMultiplier * pos.x;
        float y = Level.size / 2 - _toMapCoordsMultiplier * pos.z;

        int xSqr;
        bool isOut = false;
        if (x < Level.border)
        {
            isOut = true;
            xSqr = 0;
        }
        else if (x > Level.border + _gridSize)
        {
            isOut = true;
            xSqr = _sqrsTotal - 1;
        }
        else
            xSqr = Mathf.FloorToInt((x - Level.border) / _sqrSize);
        int ySqr;
        if (y < Level.border)
        {
            isOut = true;
            ySqr = 0;
        }
        else if (y > Level.border + _gridSize)
        {
            isOut = true;
            ySqr = _sqrsTotal - 1;
        }
        else
            ySqr = Mathf.FloorToInt((y - Level.border) / _sqrSize);
        int bigsqrx = Mathf.FloorToInt(xSqr / 3f);
        int smlSqrDstX = xSqr % 3;
        int bigsqry = Mathf.FloorToInt(ySqr / 3f);
        int smlSqrDstY = ySqr % 3;
        this.X = (byte)bigsqrx;
        this.Y = (byte)bigsqry;
        if (!isOut)
            this.Index = (byte)(smlSqrDstX + (2 - smlSqrDstY) * 3 + 1);

        _toStringCache = ToString(X, Y, Index);
    }
}