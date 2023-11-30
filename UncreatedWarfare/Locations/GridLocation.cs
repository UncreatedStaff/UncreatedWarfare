using SDG.Unturned;
using System;
using System.Globalization;
using Uncreated.Warfare.Models.Localization;
using Unity.Jobs;
using UnityEngine;

namespace Uncreated.Warfare.Locations;

public readonly struct GridLocation : ITranslationArgument
{
    private static LevelData? _lvl;
    public const int SUBGRID_AMOUNT = 3; // dont set at 10 or higher
    public const int OPTIMAL_GRID_SIZE_WORLD_SCALE = 150;
    public const float BORDER_PERCENTAGE = 1f / 30f;
    public readonly byte X;
    public readonly byte Y;
    public readonly byte Index;
    private readonly string _toStringCache;
    public Vector2 Center
    {
        get
        {
            int index = Index == 0 || Index > SUBGRID_AMOUNT * SUBGRID_AMOUNT ? Mathf.CeilToInt(SUBGRID_AMOUNT * SUBGRID_AMOUNT / 2f) : Index;
            int subgridx = X * SUBGRID_AMOUNT;
            int subgridy = Y * SUBGRID_AMOUNT;
            subgridx += (index - 1) % SUBGRID_AMOUNT;
            subgridy += SUBGRID_AMOUNT - Mathf.CeilToInt(index / (float)SUBGRID_AMOUNT);
            CartographyVolume? cartographyVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get()?.GetMainVolume();
            if (cartographyVolume != null)
            {
                Vector3 box = cartographyVolume.GetBoxSize();
                GetMapMetrics(Mathf.RoundToInt(box.x), Mathf.RoundToInt(box.z), out int sectionsX, out int sectionsY, out _, out _, out int border);
                float bdrx = (border / box.x);
                float bdry = (border / box.z);
                float sizeX = 1f - bdrx * 2f;
                float sizeY = 1f - bdry * 2f;
                Vector3 pos = cartographyVolume.transform.TransformPoint(
                    bdrx + sizeX * ((float)subgridx / (sectionsX * SUBGRID_AMOUNT)) - 0.5f + sizeX / (sectionsX * SUBGRID_AMOUNT * 2f), 0,
                    -(bdry + sizeY * ((float)subgridy / (sectionsY * SUBGRID_AMOUNT)) - 0.5f + sizeY / (sectionsY * SUBGRID_AMOUNT * 2f)));
                return new Vector2(pos.x, pos.z);
            }
            int sqrCt = GetLegacyGridSize() * SUBGRID_AMOUNT;
            if (subgridx >= sqrCt)
                subgridx = sqrCt - 1;
            if (subgridy >= sqrCt)
                subgridy = sqrCt - 1;
            int size = Level.size;
            float actualSize = size / (size / (size - Level.border * 2f));
            int bdr = Mathf.RoundToInt(actualSize * BORDER_PERCENTAGE);
            float gridSize = actualSize - bdr * 2;
            float sqrSize = gridSize / sqrCt;
            return new Vector2(
                bdr + gridSize * ((float)subgridx / sqrCt) + sqrSize / 2f - actualSize / 2f,
                actualSize / 2f - (bdr + gridSize * ((float)subgridy / sqrCt) + sqrSize / 2f));
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"/>
    private GridLocation(byte x, byte y, byte index)
    {
        if (index > SUBGRID_AMOUNT * SUBGRID_AMOUNT)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must either be 0 or 1-" + (SUBGRID_AMOUNT * SUBGRID_AMOUNT) + " inclusive.");
        if (x > 25)
            throw new ArgumentOutOfRangeException(nameof(x), "X must be 0-25 inclusive.");
        if (y > 25)
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be 0-25 inclusive.");
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
            ptr[1] = (char)(y / 10 + 48);
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
    public override string ToString() => _toStringCache;
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => _toStringCache;
    public static bool TryParse(string value, out GridLocation location)
    {
        if (value.Length is < 2 or > 8)
        {
            location = default;
            return false;
        }
        char xc = value[0];
        if (xc > 96) xc -= (char)32;
        byte x = (byte)(xc - 65);
        byte y;
        int l;
        if ((int)value[1] is > 47 and < 58)
        {
            if (value.Length > 2 && (int)value[2] is > 47 and < 58)
            {
                l = 3;
                y = (byte)((value[1] - 48) * 10 + (value[2] - 49));
            }
            else
            {
                l = 2;
                y = (byte)(value[1] - 49);
            }
        }
        else
        {
            location = default;
            return false;
        }
        if (x > 25 || y > 25)
        {
            goto rtnFalse;
        }
        byte index = 0;

        if (value.Length > l)
        {
            for (int i = l; value.Length > i; ++i)
            {
                if ((int)value[i] is > 47 and < 58)
                {
                    if (value.Length > i + 1 && (int)value[i + 1] is > 47 and < 58)
                        index = (byte)((value[i] - 48) * 10 + (value[i + 1] - 48));
                    else
                        index = (byte)(value[i] - 48);
                    if (index > SUBGRID_AMOUNT * SUBGRID_AMOUNT)
                        goto rtnFalse;
                    goto rtnTrue;
                }
                //                              hyphen      em dash     en dash
                if (value[i] is not ' ' and not '-' and not '–' and not '—' and not ':' and not '_' and not '=')
                    break;
            }
        }
        else goto rtnTrue;
        rtnFalse:
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
    internal static int GetLegacyGridSize() => Level.info == null ? 1 : GetLegacyGridSize(Level.info.size);
    public static int GetLegacyGridSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => 3,    // A-C
        ELevelSize.SMALL => 6,   // A-F
        ELevelSize.MEDIUM => 12, // A-L
        ELevelSize.LARGE => 24,  // A-X
        ELevelSize.INSANE => 26, // A-Z
        _ => 1                   // A
    };
    public GridLocation(in Vector3 pos)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CartographyVolume? cartographyVolume = VolumeManager<CartographyVolume, CartographyVolumeManager>.Get()?.GetMainVolume();
        if (cartographyVolume != null)
        {
            Vector3 local = cartographyVolume.transform.InverseTransformPoint(pos);
            Vector3 box = cartographyVolume.GetBoxSize();
            GetMapMetrics(Mathf.RoundToInt(box.x), Mathf.RoundToInt(box.z), out int sectionsX, out int sectionsY, out _, out _, out int border);
            float bdrx = border / box.x;
            float bdry = border / box.z;
            local.x += 0.5f;
            local.z = 0.5f - local.z;
            int subgridx = Mathf.FloorToInt((local.x - bdrx) / ((1f - bdrx * 2f) / (sectionsX * SUBGRID_AMOUNT)));
            int subgridy = Mathf.FloorToInt((local.z - bdry) / ((1f - bdry * 2f) / (sectionsY * SUBGRID_AMOUNT)));
            X = (byte)(subgridx / SUBGRID_AMOUNT);
            Y = (byte)(subgridy / SUBGRID_AMOUNT);
            if (local.x < bdrx || local.z < bdry || local.x > 1f - bdrx || local.z > 1f - bdry)
                Index = 0;
            else
                Index = (byte)(subgridx % SUBGRID_AMOUNT + ((SUBGRID_AMOUNT - 1) - subgridy % SUBGRID_AMOUNT) * SUBGRID_AMOUNT + 1);
            _toStringCache = ToString(X, Y, Index);
            return;
        }

        int sqrCt = GetLegacyGridSize() * SUBGRID_AMOUNT;
        int size = Level.size;
        float actualSize = size / (size / (size - Level.border * 2f)); // size of the mapped area (world)
        int bdr = Mathf.RoundToInt(actualSize * BORDER_PERCENTAGE);
        float gridSize = actualSize - bdr * 2;
        float sqrSize = gridSize / sqrCt;
        float x = actualSize / 2 + pos.x;
        float y = actualSize / 2 - pos.z;

        int xSqr;
        bool isOut = false;
        if (x < bdr)
        {
            isOut = true;
            xSqr = 0;
        }
        else if (x > bdr + gridSize)
        {
            isOut = true;
            xSqr = sqrCt - 1;
        }
        else
            xSqr = Mathf.FloorToInt((x - bdr) / sqrSize);
        int ySqr;
        if (y < bdr)
        {
            isOut = true;
            ySqr = 0;
        }
        else if (y > bdr + gridSize)
        {
            isOut = true;
            ySqr = sqrCt - 1;
        }
        else
            ySqr = Mathf.FloorToInt((y - bdr) / sqrSize);
        X = (byte)(xSqr / SUBGRID_AMOUNT);
        Y = (byte)(ySqr / SUBGRID_AMOUNT);
        if (!isOut)
            Index = (byte)((xSqr % SUBGRID_AMOUNT) + ((SUBGRID_AMOUNT - 1) - (ySqr % SUBGRID_AMOUNT)) * SUBGRID_AMOUNT + 1);
        else Index = 0;

        _toStringCache = ToString(X, Y, Index);
    }
    /// <summary>
    /// Only works with maps without a cartography volume.
    /// </summary>
    /// <param name="sectionWidth">world scale</param>
    /// <param name="border">world scale</param>
    public static void GetMapMetrics(ELevelSize size, out int sections, out int sectionWidth, out int border)
    {
        int fullSize = (int)Math.Pow(2, (int)size + 9);
        float actualSize = fullSize / (fullSize / (fullSize - (size switch
        {
            ELevelSize.TINY => 16f,
            ELevelSize.SMALL or ELevelSize.MEDIUM or ELevelSize.LARGE => 64f,
            ELevelSize.INSANE => 128f,
            _ => 0f
        }) * 2f));
        border = Mathf.RoundToInt(actualSize * BORDER_PERCENTAGE);
        sections = GetLegacyGridSize(size);
        sectionWidth = Mathf.RoundToInt((actualSize - border * 2) / sections);
    }
    /// <summary>
    /// Only works with maps with a cartography volume.
    /// </summary>
    /// <param name="length">world scale</param>
    /// <param name="width">world scale</param>
    /// <param name="sectionWidthX">world scale</param>
    /// <param name="sectionWidthY">world scale</param>
    /// <param name="border">world scale</param>
    public static void GetMapMetrics(int length, int width, out int sectionsX, out int sectionsY, out int sectionWidthX, out int sectionWidthY, out int border)
    {
        border = Mathf.RoundToInt(Math.Min(length, width) * BORDER_PERCENTAGE);
        int l2 = length - border * 2;
        int w2 = width - border * 2;
        sectionsX = l2 / OPTIMAL_GRID_SIZE_WORLD_SCALE;
        int xmod = l2 % OPTIMAL_GRID_SIZE_WORLD_SCALE;
        if (xmod != 0)
        {
            if (xmod > OPTIMAL_GRID_SIZE_WORLD_SCALE / 2)
                ++sectionsX;
            sectionWidthX = l2 / sectionsX;
        }
        else
            sectionWidthX = OPTIMAL_GRID_SIZE_WORLD_SCALE;
        sectionsY = w2 / OPTIMAL_GRID_SIZE_WORLD_SCALE;
        int ymod = w2 % OPTIMAL_GRID_SIZE_WORLD_SCALE;
        if (ymod != 0)
        {
            if (xmod > OPTIMAL_GRID_SIZE_WORLD_SCALE / 2)
                ++sectionsY;
            sectionWidthY = w2 / sectionsY;
        }
        else
            sectionWidthY = OPTIMAL_GRID_SIZE_WORLD_SCALE;
        if (sectionsX > 26)
        {
            sectionsX = 26;
            sectionWidthX = l2 / sectionsX;
        }
        if (sectionsY > 26)
        {
            sectionsY = 26;
            sectionWidthY = w2 / sectionsY;
        }
    }

    public static (int X, int Y) ImageSize => (_lvl ??= new LevelData()).MapImageSize;
    public static bool LegacyMapping => (_lvl ??= new LevelData()).LegacyMapping;
    public static Vector2 CaptureSize => (_lvl ??= new LevelData()).CaptureSize;
    public static Vector2 DistanceScale => (_lvl ??= new LevelData()).DistanceScale;
    public static Matrix4x4 TransformMatrix => (_lvl ??= new LevelData()).TransformMatrix;
    public static Matrix4x4 TransformMatrixInverse => (_lvl ??= new LevelData()).TransformMatrixInverse;
    public static float WorldDistanceToMapDistanceX(float x)
    {
        _lvl ??= new LevelData();
        
        return x / _lvl.DistanceScale.x;
    }
    public static float WorldDistanceToMapDistanceY(float y)
    {
        _lvl ??= new LevelData();
        
        return y / _lvl.DistanceScale.x;
    }
    public static float MapDistanceToWorldDistanceX(float x)
    {
        _lvl ??= new LevelData();
        
        return x * _lvl.DistanceScale.x;
    }
    public static float MapDistanceToWorldDistanceY(float y)
    {
        _lvl ??= new LevelData();
        
        return y * _lvl.DistanceScale.x;
    }
    public static Vector2 WorldCoordsToMapCoords(Vector3 worldPos)
    {
        _lvl ??= new LevelData();

        Vector3 n = new Vector3((worldPos.x / _lvl.CaptureSize.x + 0.5f) * _lvl.MapImageSize.X, 0f, (worldPos.z / _lvl.CaptureSize.y + 0.5f) * _lvl.MapImageSize.Y);
        return _lvl.TransformMatrixInverse.MultiplyPoint3x4(n);
    }
    public static Vector3 MapCoordsToWorldCoords(Vector2 mapPos)
    {
        _lvl ??= new LevelData();

        Vector3 n = new Vector3((mapPos.x / _lvl.MapImageSize.X - 0.5f) * _lvl.CaptureSize.x, (mapPos.y / _lvl.MapImageSize.Y - 0.5f) * _lvl.CaptureSize.y, 0f);
        return _lvl.TransformMatrix.MultiplyPoint3x4(n);
    }
    private sealed class LevelData
    {
        public bool LegacyMapping;
        public Matrix4x4 TransformMatrix;
        public Matrix4x4 TransformMatrixInverse;
        public (int X, int Y) MapImageSize;
        public Vector2 CaptureSize;
        public Vector2 DistanceScale; // * = map to world, / = world to map
        public LevelData()
        {
            if (Level.isLoaded)
                Reset(Level.BUILD_INDEX_GAME);
            else
                Level.onPrePreLevelLoaded += Reset;
        }
        private void Reset(int lvlLoaded)
        {
            if (lvlLoaded != Level.BUILD_INDEX_GAME)
                return;
            CartographyVolume vol = CartographyVolumeManager.Get().GetMainVolume();
            if (vol != null)
            {
                LegacyMapping = false;
                TransformMatrix = Matrix4x4.TRS(vol.transform.position, vol.transform.rotation * Quaternion.Euler(90f, 0.0f, 0.0f), Vector3.one);
                TransformMatrixInverse = TransformMatrix.inverse;
                Vector3 size = vol.CalculateLocalBounds().size;
                MapImageSize = (Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.z));
                CaptureSize = new Vector2(size.x, size.z);
                DistanceScale = new Vector2(CaptureSize.x / MapImageSize.X, CaptureSize.y / MapImageSize.Y);
            }
            else
            {
                LegacyMapping = true;
                TransformMatrix = Matrix4x4.TRS(new Vector3(0.0f, 1028f, 0.0f), Quaternion.Euler(90f, 0.0f, 0.0f), Vector3.one);
                TransformMatrixInverse = TransformMatrix.inverse;
                ushort s = Level.size;
                float w = s - Level.border * 2f;
                MapImageSize = (s, s);
                CaptureSize = new Vector2(w, w);
                DistanceScale = new Vector2(w / s, w / s);
            }

            Level.onPrePreLevelLoaded -= Reset;
        }
    }
}