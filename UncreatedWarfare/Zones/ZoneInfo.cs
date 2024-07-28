using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Uncreated.Warfare.Zones;
public class Zone : ICloneable
{
    private List<uint> _gridObjects = [];
    private List<UpstreamZone> _upstreamZones = [];

    [JsonIgnore]
    public int Index { get; internal set; }

    [JsonIgnore]
    public NetId NetId { get; internal set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; } = true;

    [JsonPropertyName("short_name")]
    public string? ShortName { get; set; }

    [JsonPropertyName("creator")]
    public CSteamID Creator { get; set; }

    [JsonPropertyName("center_pos")]
    public Vector3 Center { get; set; }

    [JsonPropertyName("spawn_pos")]
    public Vector3 Spawn { get; set; }

    [JsonPropertyName("spawn_yaw")]
    public float SpawnYaw { get; set; }

    [JsonPropertyName("shape")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZoneShape Shape { get; set; }

    [JsonPropertyName("circle")]
    public ZoneCircleInfo? CircleInfo { get; set; }

    [JsonPropertyName("aabb")]
    public ZoneAABBInfo? AABBInfo { get; set; }

    [JsonPropertyName("polygon")]
    public ZonePolygonInfo? PolygonInfo { get; set; }

    [JsonPropertyName("grid_objects")]
    public List<uint> GridObjects
    {
        get => _gridObjects;
        set => _gridObjects = value ?? [];
    }

    [JsonPropertyName("upstream_zones")]
    public List<UpstreamZone> UpstreamZones
    {
        get => _upstreamZones;
        set => _upstreamZones = value ?? [];
    }

    [JsonPropertyName("use_case")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZoneType Type { get; set; }

    [JsonPropertyName("faction")]
    public string? Faction { get; set; }

#if CLIENT
    [JsonIgnore]
    public BaseZoneComponent? Component { get; set; }
#endif
    public object Clone()
    {
        return new Zone
        {
            CircleInfo = (ZoneCircleInfo?)CircleInfo?.Clone(),
            PolygonInfo = (ZonePolygonInfo?)PolygonInfo?.Clone(),
            AABBInfo = (ZoneAABBInfo?)AABBInfo?.Clone(),
            Name = Name,
            ShortName = ShortName,
            Creator = Creator,
            Center = Center,
            Spawn = Spawn,
            SpawnYaw = SpawnYaw,
            Shape = Shape,
            Type = Type,
            IsPrimary = IsPrimary,
            Faction = Faction,
            GridObjects = new List<uint>(GridObjects),
            UpstreamZones = new List<UpstreamZone>(UpstreamZones.Select(x => new UpstreamZone { Weight = x.Weight, ZoneName = x.ZoneName }))
        };
    }
}

public class ZoneCircleInfo : ICloneable
{
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    [JsonPropertyName("min_height")]
    public float? MinimumHeight { get; set; }

    [JsonPropertyName("max_height")]
    public float? MaximumHeight { get; set; }

    public object Clone() => new ZoneCircleInfo { Radius = Radius, MaximumHeight = MaximumHeight, MinimumHeight = MinimumHeight };
}

public class ZoneAABBInfo : ICloneable
{
    [JsonPropertyName("size")]
    public Vector3 Size { get; set; }

    public object Clone() => new ZoneAABBInfo { Size = Size };
}

public class ZonePolygonInfo : ICloneable
{
    [JsonPropertyName("points")]
    public Vector2[] Points { get; set; } = Array.Empty<Vector2>();

    [JsonPropertyName("min_height")]
    public float? MinimumHeight { get; set; }

    [JsonPropertyName("max_height")]
    public float? MaximumHeight { get; set; }

    public object Clone()
    {
        Vector2[] oldPoints = Points;
        Vector2[] newPoints = oldPoints.Length == 0 ? Array.Empty<Vector2>() : new Vector2[oldPoints.Length];
        for (int i = 0; i < newPoints.Length; ++i)
            newPoints[i] = oldPoints[i];
        return new ZonePolygonInfo { Points = newPoints, MaximumHeight = MaximumHeight, MinimumHeight = MinimumHeight };
    }
}

public class UpstreamZone
{
    [JsonPropertyName("name")]
    public string ZoneName { get; set; }

    [JsonPropertyName("weight")]
    public float Weight { get; set; }
}