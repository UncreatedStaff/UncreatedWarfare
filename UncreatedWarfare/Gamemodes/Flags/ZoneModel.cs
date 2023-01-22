using System;
using System.Linq;
using System.Runtime.CompilerServices;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
internal struct ZoneModel : IListItem
{
    internal int Id = -1;
    internal int Map;
    internal string Name = null!;
    internal string? ShortName;
    internal float SpawnX = float.NaN;
    internal float SpawnZ = float.NaN;
    internal bool UseMapCoordinates;
    internal float MinimumHeight = float.NaN;
    internal float MaximumHeight = float.NaN;
    internal ZoneType ZoneType = ZoneType.Invalid;
    internal ZoneUseCase UseCase = ZoneUseCase.Other;
    internal AdjacentFlagData[] Adjacencies;
    internal GridObject[] GridObjects;
    internal Data ZoneData = new Data();
    PrimaryKey IListItem.PrimaryKey { get => Id; set => Id = value; }
    internal struct Data
    {
        internal Vector2[] Points = null!;
        internal float SizeX = float.NaN;
        internal float SizeZ = float.NaN;
        internal float Radius = float.NaN;
        internal float X = float.NaN;
        internal float Z = float.NaN;
        public Data() { }
    }
    internal bool IsValid = false;
    internal static readonly PropertyData[] ValidProperties = new PropertyData[]
    {
        new PropertyData("size-x", ZoneType.Rectangle, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeX  = v)),
        new PropertyData("size-z", ZoneType.Rectangle, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeZ  = v)),
        new PropertyData("radius", ZoneType.Circle,    (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.Radius = v)),
        new PropertyData("points", ZoneType.Polygon,   (PropertyData.ModData<Vector2[]>)((ref Data d, Vector2[] v) => d.Points = v))
    };
    internal readonly struct PropertyData
    {
        public readonly string Name;
        public readonly ZoneType ZoneType;
        public readonly Delegate Modifier;
        public PropertyData(string name, ZoneType zoneType, Delegate modifier)
        {
            Name = name;
            ZoneType = zoneType;
            Modifier = modifier;
        }

        public delegate void ModData<in T>(ref Data d, T v);
    }
    public ZoneModel()
    {
        ShortName = null;
        SpawnX = float.NaN;
        SpawnZ = float.NaN;
        UseMapCoordinates = false;
        Id = -1;
        Adjacencies = Array.Empty<AdjacentFlagData>();
        GridObjects = Array.Empty<GridObject>();
        Map = -1;
    }

    internal Zone GetZone()
    {
        if (IsValid)
        {
            switch (ZoneType)
            {
                case ZoneType.Rectangle:
                    return new RectZone(in this);
                case ZoneType.Circle:
                    return new CircleZone(in this);
                case ZoneType.Polygon:
                    return new PolygonZone(in this);
            }
        }
        throw new ZoneReadException("Failure when creating a zone object. This JSONZoneData was not read properly.");
    }
    /// <returns><see langword="false"/> if <paramref name="fl"/> is <see cref="float.NaN"/> or <see cref="float.PositiveInfinity"/> or <see cref="float.NegativeInfinity"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBadFloat(float fl) => float.IsNaN(fl) || float.IsInfinity(fl);
    /// <summary>
    /// Validates all data in this model.
    /// </summary>
    /// <exception cref="ZoneReadException">If data is invalid.</exception>
    internal void ValidateRead()
    {
        if (string.IsNullOrEmpty(Name))
            throw new ZoneReadException("Zones are required to define: name (string, max. 128 char), and optionally short-name (string, max. 64 char).") { Data = this };
        if (Name.Length > ZoneList.MaxNameLength)
            throw new ZoneReadException("Name must be " + ZoneList.MaxNameLength.ToString(Warfare.Data.LocalLocale) + " characters or less.") { Data = this };
        if (ShortName is { Length: > ZoneList.MaxShortNameLength })
            throw new ZoneReadException("Short name must be " + ZoneList.MaxShortNameLength.ToString(Warfare.Data.LocalLocale) + " characters or less.") { Data = this };
        if (ZoneType == ZoneType.Invalid)
        {
            throw new ZoneReadException("Zone JSON data should have at least one valid data property: " + string.Join(", ", ValidProperties.Select(x => x.Name))) { Data = this };
        }
        if (ZoneType == ZoneType.Rectangle)
        {
            if (IsBadFloat(ZoneData.X) || IsBadFloat(ZoneData.Z))
                throw new ZoneReadException("Rectangle zones are required to define: x (float), z (float).") { Data = this };
            if (IsBadFloat(ZoneData.SizeX) || IsBadFloat(ZoneData.SizeZ) || ZoneData.SizeX <= 0 || ZoneData.SizeZ <= 0)
                throw new ZoneReadException("Rectangle zones are required to define: size-x (float, > 0), size-z (float, > 0).") { Data = this };
        }
        else if (ZoneType == ZoneType.Circle)
        {
            if (IsBadFloat(ZoneData.X) || IsBadFloat(ZoneData.Z))
                throw new ZoneReadException("Circle zones are required to define: x (float), z (float).") { Data = this };
            if (IsBadFloat(ZoneData.Radius) || ZoneData.Radius <= 0)
                throw new ZoneReadException("Circle zones are required to define: radius (float, > 0).") { Data = this };
        }
        else if (ZoneType == ZoneType.Polygon)
        {
            if (IsBadFloat(SpawnX) || IsBadFloat(SpawnZ))
                throw new ZoneReadException("Polygon zones are required to define: x (float), z (float).") { Data = this };
            if (ZoneData.Points == null || ZoneData.Points.Length < 3)
                throw new ZoneReadException("Polygon zones are required to define at least 3 points: points ({ \"x\", \"z\" } array).") { Data = this };
        }
        else
        {
            throw new ZoneReadException("Zone JSON data should have at least one valid data property: " + string.Join(", ", ValidProperties.Select(x => x.Name))) { Data = this };
        }
        if (UseCase is < ZoneUseCase.Other or > ZoneUseCase.Lobby)
            throw new ZoneReadException("Use case is out of range, must be: OTHER (0), FLAG (1), T1_MAIN (2), T2_MAIN (3), T1_AMC (4), T2_AMC (5), LOBBY (6).");
        if (Map < 0 || Map >= MapScheduler.MapCount)
            throw new ZoneReadException("Map index is out of range, must be between 0 and " + (MapScheduler.MapCount - 1).ToString(Warfare.Data.AdminLocale)) { Data = this };
        if (!IsBadFloat(MinimumHeight) && !IsBadFloat(MaximumHeight) && MaximumHeight <= MinimumHeight)
            throw new ZoneReadException("Max height is less than or equal to min height, it must be greater than it and vice versa (or not defined).") { Data = this };
        GridObjects ??= Array.Empty<GridObject>();
        Adjacencies ??= Array.Empty<AdjacentFlagData>();
        for (int i = 0; i < GridObjects.Length; ++i)
        {
            GridObject obj = GridObjects[i];
            if (obj == null)
            {
                Util.RemoveFromArray(ref GridObjects, i--);
                continue;
            }

            Vector3 pos = new Vector3(obj.X, obj.Y, obj.Z);
            obj.Object ??= UCBarricadeManager.FindObject(obj.ObjectInstanceId, pos, obj.Guid);
            if (obj.Object == null)
            {
                obj.Object = UCBarricadeManager.GetObjectFromPosition(obj.Guid, pos);
                if (obj.Object == null)
                {
                    Util.RemoveFromArray(ref GridObjects, i--);
                    L.LogWarning("[ZONE MODEL] Invalid grid object: " + obj + ".");
                    continue;
                }
            }

            pos = obj.Object.GetPosition();

            obj.ObjectInstanceId = obj.Object.instanceID;
            obj.X = pos.x;
            obj.Y = pos.y;
            obj.Z = pos.z;

            if (obj.Object.interactable is null)
            {
                Util.RemoveFromArray(ref GridObjects, i--);
                L.LogWarning("[ZONE MODEL] Grid object: " + obj + " is not powered.");
            }
        }
        IsValid = true;
    }
}

[Translatable]
public enum ZoneType : byte
{
    Invalid = 0,
    Circle = 1,
    Rectangle = 2,
    Polygon = 4
}
[Translatable("Use Case")]
public enum ZoneUseCase : byte
{
    [Translatable("Unknown")]
    Other = 0,
    Flag = 1,
    [Translatable("Main Base: Team 1")]
    Team1Main = 2,
    [Translatable("Main Base: Team 2")]
    Team2Main = 3,
    [Translatable("AMC Zone: Team 1")]
    Team1MainCampZone = 4,
    [Translatable("AMC Zone: Team 2")]
    Team2MainCampZone = 5,
    Lobby = 6
}