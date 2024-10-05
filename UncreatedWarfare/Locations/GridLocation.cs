using DanielWillett.SpeedBytes;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Locations;

/// <summary>
/// Handles translating coordinates to/from grid coordinates.
/// </summary>
[JsonConverter(typeof(GridLocationConverter))]
[TypeConverter(typeof(GridLocationTypeConverter))]
public readonly struct GridLocation : ITranslationArgument, IEquatable<GridLocation>, IComparable<GridLocation>
#if NET6_0_OR_GREATER
    , ISpanFormattable
#else
    , IFormattable
#endif
#if NET7_0_OR_GREATER
    , ISpanParsable<GridLocation>
#endif
{
    private readonly uint _data;

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
    /// <exception cref="NotSupportedException">Not ran on an active server.</exception>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Vector2 Center
    {
        get
        {
            if (!Level.isInitialized)
                throw new NotSupportedException("Not ran on an active server.");

            int index = Index is 0 or > SubgridAmount * SubgridAmount
                ? Mathf.CeilToInt(SubgridAmount * SubgridAmount / 2f) // middle subgrid
                : Index;

            int subgridPosX = X * SubgridAmount;
            int subgridPosY = Y * SubgridAmount;

            // offset the index to be linear instead of spread out like a numpad
            subgridPosX += (index - 1) % SubgridAmount;
            subgridPosY += SubgridAmount - Mathf.CeilToInt(index / (float)SubgridAmount);

            Vector2 center = default;

            if (!CartographyUtility.UsesLegacyCartography)
            {
                Vector2Int mapImageSize = CartographyUtility.MapImageSize;

                GetMapMetrics(mapImageSize.x, mapImageSize.y, out int gridSizeX, out int gridSizeY, out _, out _, out double borderSize);

                Vector3 mapLocalPos = default;
                Vector2 areaSize = CartographyUtility.WorldCaptureAreaDimensions;

                // add half of subgrid offset and scale to grid relative coords [0,1]
                mapLocalPos.x = (subgridPosX + 0.5f) / (gridSizeX * SubgridAmount);
                mapLocalPos.y = (subgridPosY + 0.5f) / (gridSizeY * SubgridAmount);

                // scale to [-1,1]
                mapLocalPos.x = mapLocalPos.x * 2f - 1f;
                mapLocalPos.y = mapLocalPos.y * 2f - 1f;

                // scale up to capture area size
                mapLocalPos.x *= (areaSize.x - (float)borderSize * 2f) / areaSize.x;
                mapLocalPos.y *= -(areaSize.y - (float)borderSize * 2f) / areaSize.y;

                Vector3 point3d = CartographyUtility.MapToWorld.MultiplyPoint3x4(mapLocalPos);

                center.x = point3d.x;
                center.y = point3d.z;
                return center;
            }

            int subgridCount = GetLegacyGridSize() * SubgridAmount;

            // clamp to grid
            subgridPosX = Math.Min(subgridPosX, subgridCount - 1);
            subgridPosY = Math.Min(subgridPosY, subgridCount - 1);

            // size of the mapped area (world)
            float captureAreaSize = Level.size - Level.border * 2;

            // size of the area between the edge of the map and the start of the grid
            int gridBorderSize = Mathf.RoundToInt(captureAreaSize * BorderPercentage);

            float gridSize = captureAreaSize - gridBorderSize * 2;
            float sqrSize = gridSize / subgridCount;

            float relativeGridPosX = (float)subgridPosX / subgridCount;
            float relativeGridPosY = (float)subgridPosY / subgridCount;

            // offset to middle of subgrid
            float halfOffset = sqrSize / 2f;

            // border + grid position + half of subgrid - center point (y is flipped)
            center.x = gridBorderSize + gridSize * relativeGridPosX + halfOffset - captureAreaSize / 2f;
            center.y = captureAreaSize / 2f - (gridBorderSize + gridSize * relativeGridPosY + halfOffset);

            return center;
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
    /// <exception cref="NotSupportedException">Not ran on an active server.</exception>
    public GridLocation(in Vector3 pos)
    {
        if (!Level.isInitialized)
            throw new NotSupportedException("Not ran on an active server.");

        int subgridPosX, subgridPosY;
        bool isOutOfGridBounds;

        if (!CartographyUtility.UsesLegacyCartography)
        {
            Vector2Int mapImageSize = CartographyUtility.MapImageSize;
            GetMapMetrics(mapImageSize.x, mapImageSize.y, out int gridSizeX, out int gridSizeY, out _, out _, out double borderSize);

            Vector2 areaSize = CartographyUtility.WorldCaptureAreaDimensions;
            Vector2 mapLocalPos = CartographyUtility.WorldToMap.MultiplyPoint3x4(pos);

            // rescale to inside border and flip vertical axis
            mapLocalPos.x /= (areaSize.x - (float)borderSize * 2f) / areaSize.x;
            mapLocalPos.y /= -((areaSize.y - (float)borderSize * 2f) / areaSize.y);

            // rescale [0,1] inside grid
            mapLocalPos.x = (mapLocalPos.x + 1) / 2f;   
            mapLocalPos.y = (mapLocalPos.y + 1) / 2f;

            subgridPosX = Math.Clamp((int)(mapLocalPos.x * (gridSizeX * SubgridAmount)), 0, gridSizeX * SubgridAmount - 1);
            subgridPosY = Math.Clamp((int)(mapLocalPos.y * (gridSizeY * SubgridAmount)), 0, gridSizeY * SubgridAmount - 1);

            isOutOfGridBounds = mapLocalPos.x < 0 || mapLocalPos.x > 1 || mapLocalPos.y < 0 || mapLocalPos.y > 1;
        }
        else
        {
            int subgridCount = GetLegacyGridSize() * SubgridAmount;

            // size of the mapped area (world)
            float captureAreaSize = Level.size - Level.border * 2;

            // size of the area between the edge of the map and the start of the grid
            int gridBorderSize = Mathf.RoundToInt(captureAreaSize * BorderPercentage);

            float gridAreaSize = captureAreaSize - gridBorderSize * 2;

            float subgridSize = gridAreaSize / subgridCount;

            float gridRelativeX = captureAreaSize / 2 + pos.x;
            float gridRelativeY = captureAreaSize / 2 - pos.z;

            isOutOfGridBounds = gridRelativeX < gridBorderSize || gridRelativeX > gridBorderSize + gridAreaSize || gridRelativeY < gridBorderSize || gridRelativeY > gridBorderSize + gridAreaSize;

            subgridPosX = Math.Clamp((int)((gridRelativeX - gridBorderSize) / subgridSize), 0, subgridCount - 1);
            subgridPosY = Math.Clamp((int)((gridRelativeY - gridBorderSize) / subgridSize), 0, subgridCount - 1);
        }

        byte gridPosX = (byte)(subgridPosX / SubgridAmount);
        byte gridPosY = (byte)(subgridPosY / SubgridAmount);

        byte subGridIndex = isOutOfGridBounds ? (byte)0 : (byte)((subgridPosX % SubgridAmount) + (SubgridAmount - 1 - (subgridPosY % SubgridAmount)) * SubgridAmount + 1);

        _data = 0xFF000000u | (uint)(gridPosX << 16 | gridPosY << 8 | subGridIndex);
    }

    /// <summary>
    /// Check if two locations are in the same grid.
    /// </summary>
    public bool GridEquals(GridLocation other) => (_data >> 8) == (other._data >> 8);

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

    /// <inheritdoc />
    public override int GetHashCode() => unchecked ( (int)_data );

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
    /// Convert this grid location to a string representation, formatted like A1-1.
    /// </summary>
    public override string ToString()
    {
        int x = (int)((_data >> 16) & 0xFF);
        int y = (int)((_data >> 8) & 0xFF) + 1;
        int index = (int)(_data & 0xFF);

        int len = y > 9 ? 5 : 4;
        if (index == 0)
            len -= 2;

        int state = x << 16 | y << 8 | index;
        return string.Create(len, state, (span, state) =>
        {
            int x = state >> 16;
            int y = (state >> 8) & 0xFF;
            int index = state & 0xFF;

            WriteSpan(span, span.Length, x, y, index);
        });
    }

    /// <inheritdoc />
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return ToString();
    }

    /// <summary>
    /// Convert a grid location with the given <paramref name="x"/>, <paramref name="y"/>, and <paramref name="index"/> to a string representation, formatted like A1-1.
    /// </summary>
    public static string ToString(byte x, byte y, byte index)
    {
        ++y;
        int len = y > 9 ? 5 : 4;
        if (index == 0)
            len -= 2;

        int state = x << 16 | y << 8 | index;
        return string.Create(len, state, (span, state) =>
        {
            int x = state >> 16;
            int y = (state >> 8) & 0xFF;
            int index = state & 0xFF;

            WriteSpan(span, span.Length, x, y, index);
        });
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    /// <summary>
    /// Convert this grid location to a string representation, formatted like A1-1.
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        int x = (int)((_data >> 16) & 0xFF);
        int y = (int)((_data >> 8) & 0xFF) + 1;
        int index = (int)(_data & 0xFF);

        int len = y > 9 ? 5 : 4;
        if (index == 0)
            len -= 2;

        if (destination.Length < len)
        {
            charsWritten = len;
            return false;
        }

        charsWritten = len;
        WriteSpan(destination, len, x, y, index);
        return true;
    }

    private static void WriteSpan(Span<char> span, int len, int x, int y, int index)
    {
        span[0] = (char)(x + 65);
        if (y > 9)
        {
            span[1] = (char)(y / 10 + 48);
            span[2] = (char)(y % 10 + 48);
        }
        else
            span[1] = (char)(y + 48);

        if (index == 0)
            return;

        int a = len == 5 ? 3 : 2;
        span[a] = '-';
        span[a + 1] = (char)(index + 48);
    }

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <returns><see langword="True"/> if a valid <see cref="GridLocation"/> was parsed, otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, out GridLocation location)
    {
        value = value.Trim(' ');
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

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <exception cref="FormatException"/>
    [Obsolete]
    public static GridLocation Parse(string s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <returns><see langword="True"/> if a valid <see cref="GridLocation"/> was parsed, otherwise <see langword="false"/>.</returns>
    [Obsolete]
    public static bool TryParse(string? s, IFormatProvider? provider, out GridLocation result)
    {
        return TryParse(s, out result);
    }


    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <exception cref="FormatException"/>
    [Obsolete]
    public static GridLocation Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s);
    }

    /// <summary>
    /// Parse a case-insensitive string representing a <see cref="GridLocation"/>, ignoring whitespace.
    /// </summary>
    /// <returns><see langword="True"/> if a valid <see cref="GridLocation"/> was parsed, otherwise <see langword="false"/>.</returns>
    [Obsolete]
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out GridLocation result)
    {
        return TryParse(s, out result);
    }

    internal static int GetLegacyGridSize() => Level.info == null ? 1 : GetLegacyGridSize(Level.info.size);

    // not using constants to prevent loading Level class in third party references

    /// <summary>
    /// Get the size of the map's image (and total size) based on a legacy size.
    /// </summary>
    public static int GetLegacyMapSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => 512,
        ELevelSize.SMALL => 1024,
        ELevelSize.MEDIUM => 2048,
        ELevelSize.LARGE => 4096,
        ELevelSize.INSANE => 8192,
        _ => 0,
    };

    /// <summary>
    /// Get the expected legacy size from a map's image size (or total size), or <see langword="null"/> if it doesn't match any legacy sizes.
    /// </summary>
    public static ELevelSize? GetLegacySizeFromMapSize(int mapSize)
    {
        if (mapSize == 512)
            return ELevelSize.TINY;
        if (mapSize == 1024)
            return ELevelSize.SMALL;
        if (mapSize == 2048)
            return ELevelSize.MEDIUM;
        if (mapSize == 4096)
            return ELevelSize.LARGE;
        if (mapSize == 8192)
            return ELevelSize.INSANE;

        return null;
    }

    /// <summary>
    /// Get the size of the map's border based on a legacy size.
    /// </summary>
    public static int GetLegacyBorderSize(ELevelSize size) => size switch
    {
        ELevelSize.TINY => 16,
        ELevelSize.SMALL or ELevelSize.MEDIUM or ELevelSize.LARGE => 64,
        ELevelSize.INSANE => 128,
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
    /// <param name="borderSize">world scale</param>
    public static void GetMapMetrics(ELevelSize size, out int gridSize, out double sectionWidth, out double borderSize)
    {
        int fullSize = (int)Math.Pow(2, (int)size + 9);
        float actualSize = fullSize - GetLegacyBorderSize(size) * 2f;
        borderSize = actualSize * BorderPercentage;
        gridSize = GetLegacyGridSize(size);
        sectionWidth = (actualSize - borderSize * 2d) / gridSize;
    }

    /// <summary>
    /// Only works with maps with a cartography volume.
    /// </summary>
    /// <remarks>Used by grid generator in Discord bot.</remarks>
    public static void GetMapMetrics(int imgSizeX, int imgSizeY, out int gridSizeX, out int gridSizeY, out double sectionWidthX, out double sectionWidthY, out double borderSize)
    {
        borderSize = Math.Min(imgSizeX, imgSizeY) * BorderPercentage;

        double gridAreaX = imgSizeX - borderSize * 2;
        double gridAreaY = imgSizeY - borderSize * 2;

        gridSizeX = (int)(gridAreaX / OptimalGridSizeWorldScale);

        int remX = (int)gridAreaX % OptimalGridSizeWorldScale;
        if (remX != 0)
        {
            if (remX > OptimalGridSizeWorldScale / 2)
                ++gridSizeX;
            sectionWidthX = gridAreaX / gridSizeX;
        }
        else
            sectionWidthX = OptimalGridSizeWorldScale;

        gridSizeY = (int)(gridAreaY / OptimalGridSizeWorldScale);

        int remY = (int)gridAreaY % OptimalGridSizeWorldScale;
        if (remY != 0)
        {
            if (remX > OptimalGridSizeWorldScale / 2)
                ++gridSizeY;
            sectionWidthY = gridAreaY / gridSizeY;
        }
        else
            sectionWidthY = OptimalGridSizeWorldScale;

        if (gridSizeX > 26)
        {
            gridSizeX = 26;
            sectionWidthX = gridAreaX / gridSizeX;
        }

        if (gridSizeY <= 26)
            return;
        
        gridSizeY = 26;
        sectionWidthY = gridAreaY / gridSizeY;
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
}

public class GridLocationTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return value == null ? "null" : ((GridLocation)value).ToString();
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        return value is not string str ? base.ConvertFrom(context, culture, value) : GridLocation.Parse(str);
    }
}