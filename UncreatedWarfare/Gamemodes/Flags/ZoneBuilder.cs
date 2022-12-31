using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
/// <summary>
/// Used to build a <see cref="ZoneModel"/> from the public API. Performs error checking.<br/>
/// Any fields and properties can also be safely set without using "With" methods.
/// </summary>
public class ZoneBuilder
{
    private float _spawnX;
    private float _spawnZ;
    /// <summary>Unique numerical Id of the zone. Used for adjacencies.</summary>
    public int Id;
    /// <summary>Name of the zone.</summary>
    public string? Name;
    /// <summary>Short name of the zone.</summary>
    public string? ShortName;

    /// <summary>X position of the spawn point of the zone.</summary>
    public float SpawnX
    {
        get => _spawnX;
        set
        {
            _spawnX = value;
            if (ZoneType == ZoneType.Polygon)
                ZoneData.X = _spawnX;
        }
    }
    /// <summary>Z position of the spawn point of the zone.</summary>
    public float SpawnZ
    {
        get => _spawnZ;
        set
        {
            _spawnZ = value;
            if (ZoneType == ZoneType.Polygon)
                ZoneData.Z = _spawnZ;
        }
    }

    /// <summary>X position of the center of the zone.</summary>
    public float CenterX
    {
        get => ZoneType == ZoneType.Polygon ? SpawnX : ZoneData.X;
        set
        {
            if (ZoneType == ZoneType.Polygon)
                SpawnX = value;
            else ZoneData.X = value;
        }
    }
    /// <summary>Z position of the center of the zone.</summary>
    public float CenterZ
    {
        get => ZoneType == ZoneType.Polygon ? SpawnZ : ZoneData.Z;
        set
        {
            if (ZoneType == ZoneType.Polygon)
                SpawnZ = value;
            else ZoneData.Z = value;
        }
    }
    /// <summary>Tells the deserializer whether or not to convert the coordinates from pixel coordinates on the Map.png image or just use world coordinates.</summary>
    public bool UseMapCoordinates;
    /// <summary>Lower limit of the Y of the zone.</summary>
    /// <remarks>Set to <see cref="float.NaN"/> to not have a lower limit.</remarks>
    public float MinHeight = float.NaN;
    /// <summary>Upper limit of the Y of the zone.</summary>
    /// <remarks>Set to <see cref="float.NaN"/> to not have an upper limit.</remarks>
    public float MaxHeight = float.NaN;
    /// <summary>How the zone will be used (flag, lobby, main base, amc, etc)</summary>
    public ZoneUseCase UseCase;
    /// <summary>Declare flags that are adjacent to each other for custom flag pathing.</summary>
    public AdjacentFlagData[]? Adjacencies;
    /// <summary>Declare grid objects to be used as electrical components.</summary>
    public GridObject[]? GridObjects;
    internal ZoneModel.Data ZoneData;
    /// <summary>Sets the zone type to <see cref="CircleZone"/> and sets the radius to <see langword="value"/>.</summary>
    public float Radius
    {
        set
        {
            this.WithRadius(value);
        }
    }
    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to <see langword="value"/>: (<see cref="Vector2.x"/>, <see cref="Vector2.y"/>).</summary>
    public Vector2 RectSize
    {
        set
        {
            this.WithRectSize(value.x, value.y);
        }
    }
    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to value: (<see langword="value"/>.x, <see langword="value"/>.z).</summary>
    public (float x, float z) RectSizeTuple
    {
        set
        {
            this.WithRectSize(value.x, value.z);
        }
    }
    /// <summary>Sets the zone type to <see cref="PolygonZone"/> and sets the corners of the rectangle to <see langword="value"/>.</summary>
    public Vector2[] Points
    {
        set
        {
            this.WithPoints(value);
        }
    }
    internal ZoneType ZoneType = ZoneType.Invalid;
    public ZoneBuilder() { }
    internal ZoneBuilder(in ZoneModel model)
    {
        ZoneType = model.ZoneType;
        ZoneData = model.ZoneData;
        Id = model.Id;
        Name = model.Name;
        ShortName = model.ShortName;
        SpawnX = model.SpawnX;
        SpawnZ = model.SpawnZ;
        Adjacencies = model.Adjacencies;
        GridObjects = model.GridObjects;
        UseCase = model.UseCase;
        UseMapCoordinates = model.UseMapCoordinates;
        MinHeight = model.MinimumHeight;
        MaxHeight = model.MaximumHeight;
    }
    /// <summary>Set the ID.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithID(int id)
    {
        this.Id = id;
        return this;
    }
    /// <summary>Set the name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithName(string name)
    {
        this.Name = name;
        return this;
    }
    /// <summary>Set the name and short name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithName(string name, string shortName)
    {
        this.Name = name;
        this.ShortName = shortName;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(float x, float z)
    {
        this.CenterX = x;
        this.CenterZ = z;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(Vector2 position)
    {
        this.CenterX = position.x;
        this.CenterZ = position.y;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(Vector3 position)
    {
        this.CenterX = position.x;
        this.CenterZ = position.z;
        return this;
    }
    /// <summary>Set the spawn position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithSpawn(float x, float z)
    {
        this.SpawnX = x;
        this.SpawnZ = z;
        return this;
    }
    /// <summary>Set the spawn position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithSpawn(Vector2 position)
    {
        this.SpawnX = position.x;
        this.SpawnZ = position.y;
        return this;
    }
    /// <summary>Set the spawn position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithSpawn(Vector3 position)
    {
        this.SpawnX = position.x;
        this.SpawnZ = position.z;
        return this;
    }
    /// <summary>Already default, tells the deserializer to convert the coordinates from pixel coordinates on the Map.png image.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder FromMapCoordinates()
    {
        this.UseMapCoordinates = true;
        return this;
    }
    /// <summary>Already default, tells the deserializer to leave the coordinates as-is.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder FromWorldCoordinates()
    {
        this.UseMapCoordinates = false;
        return this;
    }
    /// <summary>Default <see cref="float.NaN"/>, sets a lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithMinHeight(float minHeight)
    {
        this.MinHeight = minHeight;
        return this;
    }
    /// <summary>Default <see cref="float.NaN"/>, sets an upper limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithMaxHeight(float maxHeight)
    {
        this.MaxHeight = maxHeight;
        return this;
    }
    /// <summary>Removes the lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithoutMinHeight() => WithMinHeight(float.NaN);

    /// <summary>Removes the lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithoutMaxHeight() => WithMaxHeight(float.NaN);
    /// <summary>Sets how the zone will be used (flag, lobby, main base, amc, etc).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithUseCase(ZoneUseCase useCase)
    {
        this.UseCase = useCase;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="CircleZone"/> and sets the radius to <paramref name="radius"/>.</summary>
    public ZoneBuilder WithRadius(float radius)
    {
        this.ZoneType = ZoneType.Circle;
        this.ZoneData.Radius = radius;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to (<paramref name="sizeX"/>, <paramref name="sizeZ"/>).</summary>
    public ZoneBuilder WithRectSize(float sizeX, float sizeZ)
    {
        this.ZoneType = ZoneType.Rectangle;
        this.ZoneData.SizeX = sizeX;
        this.ZoneData.SizeZ = sizeZ;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="PolygonZone"/> and sets the corners of the rectangle to <paramref name="corners"/>.</summary>
    public ZoneBuilder WithPoints(params Vector2[] corners)
    {
        this.ZoneType = ZoneType.Polygon;
        SpawnX = ZoneData.X;
        SpawnZ = ZoneData.Z;
        this.ZoneData.Points = corners;
        return this;
    }

    /// <summary>Declare flags that are adjacent to each other for custom flag pathing.</summary>
    public ZoneBuilder WithAdjacencies(params AdjacentFlagData[] adjacencies)
    {
        this.Adjacencies = adjacencies;
        return this;
    }

    /// <summary>Declare grid objects to be used as electrical components.</summary>
    public ZoneBuilder WithGridObjects(params GridObject[] gridObjects)
    {
        this.GridObjects = gridObjects;
        return this;
    }

    /// <summary>
    /// Runs a check on all data.
    /// </summary>
    /// <returns><see cref="ZoneModel"/> representing the converted save for the zone.</returns>
    /// <exception cref="ZoneAPIException">Thrown if there are any problems with the data.</exception>
    /// <exception cref="ZoneReadException">Also thrown if there are any problems with the data.</exception>
    internal ZoneModel Build()
    {
        if (Name == null)
            throw new ZoneAPIException("Zone name not provided.");
        if (ZoneType != ZoneType.Circle && ZoneType != ZoneType.Rectangle && ZoneType != ZoneType.Polygon)
            throw new ZoneAPIException("Zones must declare at least one type-specific property (Radius, SizeX & SizeY, Points).");
        if (Id < 0)
            throw new ZoneAPIException("Zones must declare an ID greater than 0.");
        ZoneModel mdl = new ZoneModel
        {
            IsValid = false,
            MaximumHeight = MaxHeight,
            MinimumHeight = MinHeight,
            Name = Name,
            ShortName = ShortName,
            ZoneType = ZoneType,
            UseMapCoordinates = UseMapCoordinates,
            SpawnX = SpawnX,
            SpawnZ = SpawnZ,
            ZoneData = ZoneData,
            UseCase = UseCase,
            Id = Id,
            Adjacencies = Adjacencies ?? Array.Empty<AdjacentFlagData>(),
            GridObjects = GridObjects ?? Array.Empty<GridObject>(),
            Map = MapScheduler.Current
        };
        mdl.ValidateRead();
        return mdl;
    }
}