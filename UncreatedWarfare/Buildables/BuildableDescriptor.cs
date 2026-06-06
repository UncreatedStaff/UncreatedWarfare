using System;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Serializable descriptor for a buildable object.
/// </summary>
internal readonly struct BuildableDescriptor : IEquatable<BuildableDescriptor>
{
    // [instanceId:32b][isStructure:1b][Y:15b][isRegionUnknown:1b][X:15b]
    private readonly ulong _data;

    /// <summary>
    /// Instance ID of the buildable.
    /// </summary>
    public uint InstanceId => (uint)(_data >> 32);

    /// <summary>
    /// Whether or not the buildable is a structure instead of a barricade.
    /// </summary>
    public bool IsStructure => ((uint)(_data >> 31) & 1u) == 1u;

    /// <summary>
    /// Whether or not the buildable's region isn't known.
    /// </summary>
    public bool IsRegionUnknown => ((uint)(_data >> 15) & 1u) == 1u;

    /// <summary>
    /// The coordinate of the buildable's region.
    /// </summary>
    public Vector2Int RegionCoord => IsRegionUnknown
        ? Vector2Int.zero
        : new Vector2Int((int)(_data & 0b0111_1111_1111_1111u) - 16383, (int)((_data >> 16) & 0b0111_1111_1111_1111u) - 16383);

    /// <summary>
    /// The X-coordinate of the buildable's region.
    /// </summary>
    public int RegionX => (!IsRegionUnknown ? 1 : 0) * ((int)(_data & 0b0111_1111_1111_1111u) - 16383);

    /// <summary>
    /// The Y-coordinate of the buildable's region.
    /// </summary>
    public int RegionY => (!IsRegionUnknown ? 1 : 0) * ((int)((_data >> 16) & 0b0111_1111_1111_1111u) - 16383);

    /// <summary>
    /// Attempt to find a barricade from this descriptor.
    /// </summary>
    /// <param name="barricade">The found barricade, or <see langword="null"/> if not found (or it was a structure).</param>
    /// <returns>Whether or not a barricade was found. Also returns <see langword="false"/> if this describes a structure.</returns>
    public bool TryGetBarricade([NotNullWhen(true)] out BarricadeDrop? barricade)
    {
        if (IsStructure)
        {
            barricade = null;
            return false;
        }

        BarricadeInfo barricadeInfo;
        if (IsRegionUnknown)
        {
            barricadeInfo = BarricadeUtility.FindBarricade(InstanceId);
            if (barricadeInfo.HasValue)
            {
                barricade = barricadeInfo.Drop;
                return true;
            }

            barricade = null;
            return false;
        }

        int x = RegionX;
        int y = RegionY;
        if (x < 0 || x > Regions.WORLD_SIZE || y < 0 || y > Regions.WORLD_SIZE)
        {
            barricade = null;
            return false;
        }

        barricadeInfo = BarricadeUtility.FindBarricade(InstanceId, (byte)x, (byte)y);
        if (barricadeInfo.HasValue)
        {
            barricade = barricadeInfo.Drop;
            return true;
        }

        barricade = null;
        return false;
    }

    /// <summary>
    /// Attempt to find a structure from this descriptor.
    /// </summary>
    /// <param name="structure">The found structure, or <see langword="null"/> if not found (or it was a barricade).</param>
    /// <returns>Whether or not a structure was found. Also returns <see langword="false"/> if this describes a barricade.</returns>
    public bool TryGetStructure([NotNullWhen(true)] out StructureDrop? structure)
    {
        if (IsStructure)
        {
            structure = null;
            return false;
        }

        StructureInfo structureInfo;
        if (IsRegionUnknown)
        {
            structureInfo = StructureUtility.FindStructure(InstanceId);
            if (structureInfo.HasValue)
            {
                structure = structureInfo.Drop;
                return true;
            }

            structure = null;
            return false;
        }

        int x = RegionX;
        int y = RegionY;
        if (x < 0 || x > Regions.WORLD_SIZE || y < 0 || y > Regions.WORLD_SIZE)
        {
            structure = null;
            return false;
        }

        structureInfo = StructureUtility.FindStructure(InstanceId, (byte)x, (byte)y);
        if (structureInfo.HasValue)
        {
            structure = structureInfo.Drop;
            return true;
        }

        structure = null;
        return false;
    }

    /// <summary>
    /// Attempt to find a buildable from this descriptor.
    /// </summary>
    /// <param name="buildable">The found buildable, or <see langword="null"/> if not found.</param>
    /// <returns>Whether or not a buildable was found.</returns>
    public bool TryGetBuildable([NotNullWhen(true)] out IBuildable? buildable)
    {
        StructureInfo structure;
        BarricadeInfo barricade;
        if (IsRegionUnknown)
        {
            if (IsStructure)
            {
                structure = StructureUtility.FindStructure(InstanceId);
                if (structure.HasValue)
                {
                    buildable = new BuildableStructure(structure.Drop);
                    return true;
                }

                buildable = null;
                return false;
            }

            barricade = BarricadeUtility.FindBarricade(InstanceId);
            if (barricade.HasValue)
            {
                buildable = new BuildableBarricade(barricade.Drop);
                return true;
            }

            buildable = null;
            return false;
        }

        int x = RegionX;
        int y = RegionY;
        if (x < 0 || x > Regions.WORLD_SIZE || y < 0 || y > Regions.WORLD_SIZE)
        {
            buildable = null;
            return false;
        }

        if (IsStructure)
        {
            structure = StructureUtility.FindStructure(InstanceId, (byte)x, (byte)y);
            if (structure.HasValue)
            {
                buildable = new BuildableStructure(structure.Drop);
                return true;
            }

            buildable = null;
            return false;
        }

        barricade = BarricadeUtility.FindBarricade(InstanceId, (byte)x, (byte)y);
        if (barricade.HasValue)
        {
            buildable = new BuildableBarricade(barricade.Drop);
            return true;
        }

        buildable = null;
        return false;
    }

    public BuildableDescriptor(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < sizeof(ulong))
        {
            throw new ArgumentException("Expected at least 8 bytes.", nameof(bytes));
        }

        _data = MemoryMarshal.Read<ulong>(bytes);
    }

    public BuildableDescriptor(uint instanceId, bool isStructure)
    {
        _data = ((ulong)instanceId << 32)
                | (isStructure
                    ? 0b1000_0000_0000_0000_1000_0000_0000_0000u
                    : 0b0000_0000_0000_0000_1000_0000_0000_0000u
                );
    }

    public BuildableDescriptor(uint instanceId, bool isStructure, Vector2Int regionCoords)
    {
        regionCoords.x += 16383;
        regionCoords.y += 16383;
        if (regionCoords.x > 32767u || regionCoords.y > 32767u || regionCoords.x < 0 || regionCoords.y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(regionCoords));
        }

        ulong regionInfo = (ulong)regionCoords.x | (ulong)(regionCoords.y << 16);
        if (isStructure)
        {
            regionInfo |= 0b1000_0000_0000_0000_0000_0000_0000_0000u;
        }

        _data = ((ulong)instanceId << 32) | regionInfo;
    }

    public BuildableDescriptor(IBuildable buildable)
    {
        Vector3 pos = buildable.Position;
        if (Regions.tryGetCoordinate(pos, out byte x, out byte y))
        {
            this = new BuildableDescriptor(buildable.InstanceId, buildable.IsStructure, new Vector2Int(x, y));
        }
        else
        {
            this = new BuildableDescriptor(buildable.InstanceId, buildable.IsStructure);
        }
    }

    /// <inheritdoc />
    public bool Equals(BuildableDescriptor other)
    {
        return other._data == _data;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is BuildableDescriptor d && Equals(d);
    }

    /// <summary>
    /// Compare two <see cref="BuildableDescriptor"/> values to see if they're the same.
    /// </summary>
    public static bool operator ==(BuildableDescriptor left, BuildableDescriptor right)
    {
        return left._data == right._data;
    }

    /// <summary>
    /// Compare two <see cref="BuildableDescriptor"/> values to see if they're different.
    /// </summary>
    public static bool operator !=(BuildableDescriptor left, BuildableDescriptor right)
    {
        return left._data != right._data;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _data.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsRegionUnknown)
        {
            return IsStructure ? $"Structure #{InstanceId}" : $"Barricade #{InstanceId}";
        }

        return IsStructure
            ? $"Structure@({RegionX},{RegionY}) #{InstanceId}"
            : $"Barricade@({RegionX},{RegionY}) #{InstanceId}";
    }

    /// <summary>
    /// Write bytes to a span at least 8 bytes long.
    /// </summary>
    /// <param name="span">Span of data to write to, at least 8 bytes.</param>
    /// <param name="bytesWritten">Number of bytes written to the span. Always 8 when data is written and 0 when there's not enough room.</param>
    /// <returns>Whether or not there was enough room to write to the span.</returns>
    public bool TryWriteBytes(Span<byte> span, out int bytesWritten)
    {
        if (span.Length < sizeof(ulong))
        {
            bytesWritten = 0;
            return false;
        }

        ulong d = _data;
        MemoryMarshal.Write(span, ref d);
        bytesWritten = sizeof(ulong);
        return true;
    }
}
