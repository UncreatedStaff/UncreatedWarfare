﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Maps;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;
public struct ZoneModel : IListItem
{
    public uint Id = 0;
    public int Map;
    public string Name = null!;
    public string? ShortName;
    public float SpawnX = float.NaN;
    public float SpawnZ = float.NaN;
    public bool UseMapCoordinates;
    public float MinimumHeight = float.NaN;
    public float MaximumHeight = float.NaN;
    public ZoneType ZoneType = ZoneType.Invalid;
    public ZoneUseCase UseCase = ZoneUseCase.Other;
    public ZoneFlags Flags = ZoneFlags.None;
    public AdjacentFlagData[] Adjacencies;
    public GridObject[] GridObjects;
    public ModelData ZoneData = new ModelData();
    PrimaryKey IListItem.PrimaryKey { get => Id; set => Id = value; }
    public struct ModelData
    {
        internal Vector2[] Points = null!;
        internal float SizeX = float.NaN;
        internal float SizeZ = float.NaN;
        internal float Radius = float.NaN;
        internal float X = float.NaN;
        internal float Z = float.NaN;
        public ModelData() { }
    }
    public bool IsValid = false;
    internal static readonly PropertyData[] ValidProperties = new PropertyData[]
    {
        new PropertyData("size-x", ZoneType.Rectangle, (PropertyData.ModData<float>)    ((ref ModelData d, float v)     => d.SizeX  = v)),
        new PropertyData("size-z", ZoneType.Rectangle, (PropertyData.ModData<float>)    ((ref ModelData d, float v)     => d.SizeZ  = v)),
        new PropertyData("radius", ZoneType.Circle,    (PropertyData.ModData<float>)    ((ref ModelData d, float v)     => d.Radius = v)),
        new PropertyData("points", ZoneType.Polygon,   (PropertyData.ModData<Vector2[]>)((ref ModelData d, Vector2[] v) => d.Points = v))
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

        public delegate void ModData<in T>(ref ModelData d, T v);
    }
    public ZoneModel()
    {
        ShortName = null;
        SpawnX = float.NaN;
        SpawnZ = float.NaN;
        UseMapCoordinates = false;
        Id = 0;
        Adjacencies = Array.Empty<AdjacentFlagData>();
        GridObjects = Array.Empty<GridObject>();
        Map = -1;
    }

    public Zone GetZone()
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
    public void ValidateRead()
    {
        if (string.IsNullOrEmpty(Name))
            throw new ZoneReadException("Zones are required to define: name (string, max. 128 char), and optionally short-name (string, max. 64 char).") { Data = this };
        if (Name.Length > ZoneList.MaxNameLength)
            throw new ZoneReadException("Name must be " + ZoneList.MaxNameLength.ToString(Data.LocalLocale) + " characters or less.") { Data = this };
        if (ShortName is { Length: > ZoneList.MaxShortNameLength })
            throw new ZoneReadException("Short name must be " + ZoneList.MaxShortNameLength.ToString(Data.LocalLocale) + " characters or less.") { Data = this };
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
            if (IsBadFloat(SpawnX))
                SpawnX = ZoneData.X;
            if (IsBadFloat(SpawnZ))
                SpawnZ = ZoneData.Z;
        }
        else if (ZoneType == ZoneType.Circle)
        {
            if (IsBadFloat(ZoneData.X) || IsBadFloat(ZoneData.Z))
                throw new ZoneReadException("Circle zones are required to define: x (float), z (float).") { Data = this };
            if (IsBadFloat(ZoneData.Radius) || ZoneData.Radius <= 0)
                throw new ZoneReadException("Circle zones are required to define: radius (float, > 0).") { Data = this };
            if (IsBadFloat(SpawnX))
                SpawnX = ZoneData.X;
            if (IsBadFloat(SpawnZ))
                SpawnZ = ZoneData.Z;
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
        if (UseCase > ZoneUseCase.Lobby)
            throw new ZoneReadException("Use case is out of range, must be: Other (0), Flag (1), Team1Main (2), Team2Main (3), Team1MainCampZone (4), Team2MainCampZone (5), Lobby (6).");
        if (Map < 0 || Map >= MapScheduler.MapCount)
            throw new ZoneReadException("Map index is out of range, must be between 0 and " + (MapScheduler.MapCount - 1).ToString(Data.AdminLocale)) { Data = this };
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

[Flags]
[Translatable(IsPrioritizedTranslation = false)]
public enum ZoneFlags : ulong
{
    None = 0,
    Safezone = 1,
    [Translatable("No Building")]
    NoBuilding = 1 << 1,
    [Translatable("No FOB Building")]
    NoFOBBuilding = 1 << 2,
    [Translatable("No Building Radios")]
    NoRadios = 1 << 3,
    [Translatable("No Building Bunkers")]
    NoBunkers = 1 << 4,
    [Translatable("No Building Rallies")]
    NoRallies = 1 << 5,
    [Translatable("No Traps")]
    NoTraps = 1 << 6,
    [Translatable("No Dropping Items")]
    NoDropItems = 1 << 7,
    [Translatable("No Picking Up Items")]
    NoPickItems = 1 << 8
}

[Translatable]
public enum ZoneType : byte
{
    [Translatable(Languages.ChineseSimplified, "无效")]
    Invalid = 0,
    [Translatable(Languages.ChineseSimplified, "圆圈")]
    Circle = 1,
    [Translatable(Languages.ChineseSimplified, "长方形")]
    Rectangle = 2,
    [Translatable(Languages.ChineseSimplified, "多边形")]
    Polygon = 4
}
[Translatable("Use Case", IsPrioritizedTranslation = false)]
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