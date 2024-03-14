using SDG.Unturned;
using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Locations;

[JsonConverter(typeof(GridLocationConverter))]
public readonly struct GridLocation : ITranslationArgument, IEquatable<GridLocation>, IComparable<GridLocation>
{
    private readonly uint _data;

    private static LevelData? _lvl;

    /// <summary>
    /// Square root of the amount of subgrids in a grid. Ex. in a 3x3 = 9 subgrid, this value will be 3.
    /// </summary>
    /// <remarks>Must be within the interval [1, 9].</remarks>
    public const int SubgridAmount = 3;

    /// <summary>
    /// The preferred distance between grids in meters. This will be scaled slightly to ensure the grid looks squared.
    /// </summary>
    public const int OptimalGridSizeWorldScale = 150;

    /// <summary>
    /// What percentage of the image is border for the grid?
    /// </summary>
    public const float BorderPercentage = 1f / 30f;

    /// <summary>
    /// The X coordinate of the grid.
    /// </summary>
    [JsonPropertyName("x")]
    [Newtonsoft.Json.JsonProperty("x")]
    public byte X => (byte)((_data >> 16) & 0xFF);

    /// <summary>
    /// The Y coordinate of the grid.
    /// </summary>
    [JsonPropertyName("y")]
    [Newtonsoft.Json.JsonProperty("y")]
    public byte Y => (byte)((_data >> 8) & 0xFF);

    /// <summary>
    /// The sub-grid index of in the current grid.
    /// </summary>
    [JsonPropertyName("index")]
    [Newtonsoft.Json.JsonProperty("index")]
    public byte Index => (byte)(_data & 0xFF);

    /// <summary>
    /// Was this grid location verified to be a valid location? This will always be true unless the <see cref="GridLocation"/> hasn't been initialized by a constructor.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsChecked => _data >> 24 == 0xFF;

    /// <summary>
    /// Is this a valid <see cref="GridLocation"/>? This will always be true unless the <see cref="GridLocation"/> hasn't been initialized by a constructor.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsValid => _data >> 24 == 0xFF || (((_data >> 16) & 0xFF) <= 25 && ((_data >> 8) & 0xFF) <= 25 && (_data & 0xFF) <= SubgridAmount * SubgridAmount);

    /// <summary>
    /// The captialized letter of the current X coordinate of the grid.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public char LetterX
    {
        get
        {
            uint x = (_data >> 16) & 0xFF;
            return x > 25 ? default : (char)(x + 65);
        }
    }

    /// <summary>
    /// The center of the referenced grid or sub-grid.
    /// </summary>
    /// <exception cref="SingletonUnloadedException">Not ran on an active server.</exception>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 Center
    {
        get
        {
            if (!UCWarfare.IsLoaded)
                throw new SingletonUnloadedException(typeof(UCWarfare));

            int index = Index is 0 or > SubgridAmount * SubgridAmount ? Mathf.CeilToInt(SubgridAmount * SubgridAmount / 2f) : Index;
            int subgridx = X * SubgridAmount;
            int subgridy = Y * SubgridAmount;
            subgridx += (index - 1) % SubgridAmount;
            subgridy += SubgridAmount - Mathf.CeilToInt(index / (float)SubgridAmount);
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
                    bdrx + sizeX * ((float)subgridx / (sectionsX * SubgridAmount)) - 0.5f + sizeX / (sectionsX * SubgridAmount * 2f), 0,
                    -(bdry + sizeY * ((float)subgridy / (sectionsY * SubgridAmount)) - 0.5f + sizeY / (sectionsY * SubgridAmount * 2f)));
                return new Vector2(pos.x, pos.z);
            }
            int sqrCt = GetLegacyGridSize() * SubgridAmount;
            if (subgridx >= sqrCt)
                subgridx = sqrCt - 1;
            if (subgridy >= sqrCt)
                subgridy = sqrCt - 1;
            int size = Level.size;
            float actualSize = size / (size / (size - Level.border * 2f));
            int bdr = Mathf.RoundToInt(actualSize * BorderPercentage);
            float gridSize = actualSize - bdr * 2;
            float sqrSize = gridSize / sqrCt;
            return new Vector2(
                bdr + gridSize * ((float)subgridx / sqrCt) + sqrSize / 2f - actualSize / 2f,
                actualSize / 2f - (bdr + gridSize * ((float)subgridy / sqrCt) + sqrSize / 2f));
        }
    }

    /// <summary>
    /// Create a grid location from an X-coordinate, a Y-coordinate, and a subgrid index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [Newtonsoft.Json.JsonConstructor]
    public GridLocation(byte x, byte y, byte index)
    {
        if (index > SubgridAmount * SubgridAmount)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must either be 0 or 1-" + (SubgridAmount * SubgridAmount) + " inclusive.");
        if (x > 25)
            throw new ArgumentOutOfRangeException(nameof(x), "X must be 0-25 inclusive.");
        if (y > 25)
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be 0-25 inclusive.");

        _data = 0xFF000000u | (uint)(x << 16 | y << 8 | index);
    }

    /// <summary>
    /// Create a grid location from an X-coordinate letter, a Y-coordinate, and a subgrid index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public GridLocation(char x, byte y, byte index)
    {
        if (index > SubgridAmount * SubgridAmount)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must either be 0 or 1-" + (SubgridAmount * SubgridAmount) + " inclusive.");
        if (x is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z'))
            throw new ArgumentOutOfRangeException(nameof(x), "X must be A-Z inclusive.");
        if (y > 25)
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be 0-25 inclusive.");

        byte xVal = x is >= 'A' and <= 'Z' ? (byte)(x - 'A') : (byte)(x - 'a');
        _data = 0xFF000000u | (uint)(xVal << 16 | y << 8 | index);
    }

    /// <summary>
    /// Create a <see cref="GridLocation"/> from any given point inside a grid and subgrid.
    /// </summary>
    /// <exception cref="SingletonUnloadedException">Not ran on an active server.</exception>
    public GridLocation(in Vector3 pos)
    {
        if (!UCWarfare.IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));

        byte xVal, yVal, indexVal;
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
            int subgridx = Mathf.FloorToInt((local.x - bdrx) / ((1f - bdrx * 2f) / (sectionsX * SubgridAmount)));
            int subgridy = Mathf.FloorToInt((local.z - bdry) / ((1f - bdry * 2f) / (sectionsY * SubgridAmount)));
            xVal = (byte)(subgridx / SubgridAmount);
            yVal = (byte)(subgridy / SubgridAmount);
            if (local.x < bdrx || local.z < bdry || local.x > 1f - bdrx || local.z > 1f - bdry)
                indexVal = 0;
            else
                indexVal = (byte)(subgridx % SubgridAmount + ((SubgridAmount - 1) - subgridy % SubgridAmount) * SubgridAmount + 1);

            _data = 0xFF000000u | (uint)(xVal << 16 | yVal << 8 | indexVal);
            return;
        }

        int sqrCt = GetLegacyGridSize() * SubgridAmount;
        int size = Level.size;
        float actualSize = size / (size / (size - Level.border * 2f)); // size of the mapped area (world)
        int bdr = Mathf.RoundToInt(actualSize * BorderPercentage);
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
        xVal = (byte)(xSqr / SubgridAmount);
        yVal = (byte)(ySqr / SubgridAmount);
        if (!isOut)
            indexVal = (byte)((xSqr % SubgridAmount) + ((SubgridAmount - 1) - (ySqr % SubgridAmount)) * SubgridAmount + 1);
        else indexVal = 0;

        _data = 0xFF000000u | (uint)(xVal << 16 | yVal << 8 | indexVal);
    }

    /// <summary>
    /// Check if two locations are in the same grid.
    /// </summary>
    public bool GridEquals(GridLocation other) => (_data & 0xFFFFFF00u) == (other._data & 0xFFFFFF00u);

    /// <summary>
    /// Check if two locations are in the same grid and sub-grid.
    /// </summary>
    public bool Equals(GridLocation other) => _data == other._data;

    /// <summary>
    /// Compare two locations to each other. Sorted from most significant to least significant: <see cref="IsChecked"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Index"/>.
    /// </summary>
    public int CompareTo(GridLocation other) => _data.CompareTo(other._data);

    /// <summary>
    /// Check if two locations are in the same grid and sub-grid.
    /// </summary>
    public override bool Equals(object? obj) => obj is GridLocation location && Equals(location);
    public override int GetHashCode() => unchecked((int)(_data & 0xFFFFFF | ~(_data & 0xFF000000)));

    /// <summary>
    /// Check if two locations are in the same grid and sub-grid.
    /// </summary>
    public static bool operator ==(GridLocation left, GridLocation right) => left._data == right._data;

    /// <summary>
    /// Check if two locations are in the same grid and sub-grid.
    /// </summary>
    public static bool operator !=(GridLocation left, GridLocation right) => left._data != right._data;

    /// <summary>
    /// Compare two locations to each other. Sorted from most significant to least significant: <see cref="IsChecked"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Index"/>.
    /// </summary>
    public static bool operator <(GridLocation left, GridLocation right) => left._data < right._data;

    /// <summary>
    /// Compare two locations to each other. Sorted from most significant to least significant: <see cref="IsChecked"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Index"/>.
    /// </summary>
    public static bool operator >(GridLocation left, GridLocation right) => left._data > right._data;

    /// <summary>
    /// Compare two locations to each other. Sorted from most significant to least significant: <see cref="IsChecked"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Index"/>.
    /// </summary>
    public static bool operator <=(GridLocation left, GridLocation right) => left._data <= right._data;

    /// <summary>
    /// Compare two locations to each other. Sorted from most significant to least significant: <see cref="IsChecked"/>, <see cref="X"/>, <see cref="Y"/>, <see cref="Index"/>.
    /// </summary>
    public static bool operator >=(GridLocation left, GridLocation right) => left._data >= right._data;

    /// <summary>
    /// Convert a grid location with the given <paramref name="x"/>, <paramref name="y"/>, and <paramref name="index"/> to a string representation, formatted like A1-1.
    /// </summary>
    public static unsafe string ToString(byte x, byte y, byte index)
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

    /// <returns>String representation of the grid, formatted like A1-1.</returns>
    public override string ToString() => ToString(X, Y, Index);

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => ToString();

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <returns><see langword="True"/> if a valid <see cref="GridLocation"/> was parsed, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, out GridLocation location)
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
            location = default;
            return false;
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
                    if (index > SubgridAmount * SubgridAmount)
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

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <exception cref="FormatException"/>
    public static GridLocation Parse(ReadOnlySpan<char> value)
    {
        if (!TryParse(value, out GridLocation location))
            throw new FormatException("Unable to parse GridLocation.");
        return location;
    }
    internal static int GetLegacyGridSize() => Level.info == null ? 1 : GetLegacyGridSize(Level.info.size);

    /// <summary>
    /// Get the size of the map's image (and total size) based on a legacy size.
    /// </summary>
    public static int GetLegacyMapSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => Level.TINY_SIZE,
        ELevelSize.SMALL => Level.SMALL_SIZE,
        ELevelSize.MEDIUM => Level.MEDIUM_SIZE,
        ELevelSize.LARGE => Level.LARGE_SIZE,
        ELevelSize.INSANE => Level.INSANE_SIZE,
        _ => 0,
    };

    /// <summary>
    /// Get the expected legacy size from a map's image size (or total size), or <see langword="null"/> if it doesn't match any legacy sizes.
    /// </summary>
    public static ELevelSize? GetLegacySizeFromMapSize(int mapSize)
    {
        if (mapSize == Level.TINY_SIZE)
            return ELevelSize.TINY;
        if (mapSize == Level.SMALL_SIZE)
            return ELevelSize.SMALL;
        if (mapSize == Level.MEDIUM_SIZE)
            return ELevelSize.MEDIUM;
        if (mapSize == Level.LARGE_SIZE)
            return ELevelSize.LARGE;
        if (mapSize == Level.INSANE_SIZE)
            return ELevelSize.INSANE;

        return null;
    }

    /// <summary>
    /// Get the size of the map's border based on a legacy size.
    /// </summary>
    public static int GetLegacyBorderSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => Level.TINY_BORDER,
        ELevelSize.SMALL => Level.SMALL_BORDER,
        ELevelSize.MEDIUM => Level.MEDIUM_BORDER,
        ELevelSize.LARGE => Level.LARGE_BORDER,
        ELevelSize.INSANE => Level.INSANE_BORDER,
        _ => 0,
    };

    /// <summary>
    /// Get the ideal grid size from a legacy size.
    /// </summary>
    public static int GetLegacyGridSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => 3,    // A-C
        ELevelSize.SMALL => 6,   // A-F
        ELevelSize.MEDIUM => 12, // A-L
        ELevelSize.LARGE => 24,  // A-X
        ELevelSize.INSANE => 26, // A-Z
        _ => 1                   // A
    };

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
        border = Mathf.RoundToInt(actualSize * BorderPercentage);
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
        border = Mathf.RoundToInt(Math.Min(length, width) * BorderPercentage);
        int l2 = length - border * 2;
        int w2 = width - border * 2;
        sectionsX = l2 / OptimalGridSizeWorldScale;
        int xmod = l2 % OptimalGridSizeWorldScale;
        if (xmod != 0)
        {
            if (xmod > OptimalGridSizeWorldScale / 2)
                ++sectionsX;
            sectionWidthX = l2 / sectionsX;
        }
        else
            sectionWidthX = OptimalGridSizeWorldScale;
        sectionsY = w2 / OptimalGridSizeWorldScale;
        int ymod = w2 % OptimalGridSizeWorldScale;
        if (ymod != 0)
        {
            if (xmod > OptimalGridSizeWorldScale / 2)
                ++sectionsY;
            sectionWidthY = w2 / sectionsY;
        }
        else
            sectionWidthY = OptimalGridSizeWorldScale;
        if (sectionsX > 26)
        {
            sectionsX = 26;
            sectionWidthX = l2 / sectionsX;
        }

        if (sectionsY <= 26)
            return;
        
        sectionsY = 26;
        sectionWidthY = w2 / sectionsY;
    }

    /// <summary>
    /// Write this location to a <see cref="ByteWriter"/>.
    /// </summary>
    public void Write(ByteWriter writer) => writer.Write(unchecked((int)(_data & 0xFFFFFF)));

    /// <summary>
    /// Write a location to a <see cref="ByteWriter"/>.
    /// </summary>
    public static void WriteLocation(ByteWriter writer, GridLocation gridLocation) => gridLocation.Write(writer);

    /// <summary>
    /// Read a location from a <see cref="ByteReader"/>.
    /// </summary>
    public static GridLocation ReadLocation(ByteReader reader)
    {
        int data = reader.ReadInt32();
        return new GridLocation((byte)((data >> 16) & 0xFF), (byte)((data >> 8) & 0xFF), (byte)(data & 0xFF));
    }

    public static Vector2Int ImageSize => (_lvl ??= new LevelData()).ImageSizeIntl;
    public static bool LegacyMapping => (_lvl ??= new LevelData()).LegacyMappingIntl;
    public static Vector2 CaptureSize => (_lvl ??= new LevelData()).CaptureSizeIntl;
    public static Vector2 DistanceScale => (_lvl ??= new LevelData()).DistanceScaleIntl;
    public static Matrix4x4 TransformMatrix => (_lvl ??= new LevelData()).TransformMatrixIntl;
    public static Matrix4x4 TransformMatrixInverse => (_lvl ??= new LevelData()).TransformMatrixInverseIntl;
    public static float WorldDistanceToMapDistanceX(float x)
    {
        _lvl ??= new LevelData();
        
        return x / _lvl.DistanceScaleIntl.x;
    }
    public static float WorldDistanceToMapDistanceY(float y)
    {
        _lvl ??= new LevelData();
        
        return y / _lvl.DistanceScaleIntl.x;
    }
    public static float MapDistanceToWorldDistanceX(float x)
    {
        _lvl ??= new LevelData();
        
        return x * _lvl.DistanceScaleIntl.x;
    }
    public static float MapDistanceToWorldDistanceY(float y)
    {
        _lvl ??= new LevelData();
        
        return y * _lvl.DistanceScaleIntl.x;
    }
    public static Vector2 WorldCoordsToMapCoords(Vector3 worldPos)
    {
        _lvl ??= new LevelData();

        Vector3 n = new Vector3((worldPos.x / _lvl.CaptureSizeIntl.x + 0.5f) * _lvl.ImageSizeIntl.x, 0f, (worldPos.z / _lvl.CaptureSizeIntl.y + 0.5f) * _lvl.ImageSizeIntl.y);
        return _lvl.TransformMatrixInverseIntl.MultiplyPoint3x4(n);
    }
    public static Vector3 MapCoordsToWorldCoords(Vector2 mapPos)
    {
        _lvl ??= new LevelData();

        Vector3 n = new Vector3((mapPos.x / _lvl.ImageSizeIntl.x - 0.5f) * _lvl.CaptureSizeIntl.x, (mapPos.y / _lvl.ImageSizeIntl.y - 0.5f) * _lvl.CaptureSizeIntl.y, 0f);
        return _lvl.TransformMatrixIntl.MultiplyPoint3x4(n);
    }
    private sealed class LevelData
    {
        public bool LegacyMappingIntl;
        public Matrix4x4 TransformMatrixIntl;
        public Matrix4x4 TransformMatrixInverseIntl;
        public Vector2Int ImageSizeIntl;
        public Vector2 CaptureSizeIntl;
        public Vector2 DistanceScaleIntl; // * = map to world, / = world to map
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
                LegacyMappingIntl = false;
                TransformMatrixIntl = Matrix4x4.TRS(vol.transform.position, vol.transform.rotation * Quaternion.Euler(90f, 0.0f, 0.0f), Vector3.one);
                TransformMatrixInverseIntl = TransformMatrixIntl.inverse;
                Vector3 size = vol.CalculateLocalBounds().size;
                ImageSizeIntl = new Vector2Int(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.z));
                CaptureSizeIntl = new Vector2(size.x, size.z);
                DistanceScaleIntl = new Vector2(CaptureSizeIntl.x / ImageSizeIntl.x, CaptureSizeIntl.y / ImageSizeIntl.y);
            }
            else
            {
                LegacyMappingIntl = true;
                TransformMatrixIntl = Matrix4x4.TRS(new Vector3(0.0f, 1028f, 0.0f), Quaternion.Euler(90f, 0.0f, 0.0f), Vector3.one);
                TransformMatrixInverseIntl = TransformMatrixIntl.inverse;
                ushort s = Level.size;
                float w = s - Level.border * 2f;
                ImageSizeIntl = new Vector2Int(s, s);
                CaptureSizeIntl = new Vector2(w, w);
                DistanceScaleIntl = new Vector2(w / s, w / s);
            }

            Level.onPrePreLevelLoaded -= Reset;
        }
    }
}