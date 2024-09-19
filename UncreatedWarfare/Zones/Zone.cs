using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.ValueFormatters;

namespace Uncreated.Warfare.Zones;
public class Zone : IDeployable
{
    private List<uint> _gridObjects = [];
    private List<UpstreamZone> _upstreamZones = [];
    
    /// <summary>
    /// Unique name of the zone.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// If this zone is the 'primary' zone. Zones can have non-primary zones which are just extensions of themselves.
    /// </summary>
    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; } = true;

    /// <summary>
    /// Shortened <see cref="Name"/>.
    /// </summary>
    [JsonPropertyName("short_name")]
    public string? ShortName { get; set; }

    /// <summary>
    /// User that original created the zone in the editor.
    /// </summary>
    [JsonPropertyName("creator")]
    public CSteamID Creator { get; set; }

    /// <summary>
    /// 3D center of the zone.
    /// </summary>
    [JsonPropertyName("center_pos")]
    public Vector3 Center { get; set; }

    /// <summary>
    /// Position where someone teleporting to the zone should spawn.
    /// </summary>
    [JsonPropertyName("spawn_pos")]
    public Vector3 Spawn { get; set; }

    /// <summary>
    /// Yaw angle at which someone teleporting to the zone should spawn.
    /// </summary>
    [JsonPropertyName("spawn_yaw")]
    public float SpawnYaw { get; set; }

    /// <summary>
    /// Shape of the bounds of the zone.
    /// </summary>
    [JsonPropertyName("shape")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZoneShape Shape { get; set; }

    /// <summary>
    /// Used when <see cref="Shape"/> is <see cref="ZoneShape.Cylinder"/> or <see cref="ZoneShape.Sphere"/>.
    /// </summary>
    [JsonPropertyName("circle")]
    public ZoneCircleInfo? CircleInfo { get; set; }

    /// <summary>
    /// Used when <see cref="Shape"/> is <see cref="ZoneShape.AABB"/>.
    /// </summary>
    [JsonPropertyName("aabb")]
    public ZoneAABBInfo? AABBInfo { get; set; }

    /// <summary>
    /// Used when <see cref="Shape"/> is <see cref="ZoneShape.Polygon"/>.
    /// </summary>
    [JsonPropertyName("polygon")]
    public ZonePolygonInfo? PolygonInfo { get; set; }
    
    /// <summary>
    /// Instance IDs of all interactable powered objects that should be enabled when the zone is in rotation.
    /// </summary>
    [JsonPropertyName("grid_objects")]
    public List<uint> GridObjects
    {
        get => _gridObjects;
        set => _gridObjects = value ?? [];
    }

    /// <summary>
    /// List of zones 'upstream' from the current zone. Used for pathing.
    /// </summary>
    [JsonPropertyName("upstream_zones")]
    public List<UpstreamZone> UpstreamZones
    {
        get => _upstreamZones;
        set => _upstreamZones = value ?? [];
    }

    /// <summary>
    /// Type or use case of the zone.
    /// </summary>
    [JsonPropertyName("use_case")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZoneType Type { get; set; }

    /// <summary>
    /// Linked faction name, only used for main base and anti-main-camp zones.
    /// </summary>
    [JsonPropertyName("faction")]
    public string? Faction { get; set; }


    [JsonIgnore]
    Vector3 IDeployable.SpawnPosition => Spawn;

    [JsonIgnore]
    float IDeployable.Yaw => SpawnYaw;

    TimeSpan IDeployable.GetDelay(WarfarePlayer player)
    {
        return TimeSpan.FromSeconds(30); // todo TimeSpan.FromSeconds(FOBManager.Config.DeployMainDelay);
    }

    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return Name;
    }

    bool IDeployable.CheckDeployableTo(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        return true;
    }

    bool IDeployable.CheckDeployableFrom(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        return true;
    }

    bool IDeployable.CheckDeployableToTick(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        return true;
    }

    /// <summary>
    /// Gets the lower height bound, if it exists, otherwise <see langword="NaN"/>.
    /// </summary>
    public float GetMinHeight()
    {
        return Shape switch
        {
            ZoneShape.AABB     when AABBInfo != null    => AABBInfo.Size.y >= Level.HEIGHT - 0.5f ? float.NaN : Center.y - AABBInfo.Size.y / 2f,
            ZoneShape.Cylinder when CircleInfo != null  => CircleInfo.MinimumHeight.GetValueOrDefault(float.NaN),
            ZoneShape.Sphere   when CircleInfo != null  => Center.y - CircleInfo.Radius,
            ZoneShape.Polygon  when PolygonInfo != null => PolygonInfo.MinimumHeight.GetValueOrDefault(float.NaN),
            _ => float.NaN
        };
    }

    /// <summary>
    /// Gets the upper height bound, if it exists, otherwise <see langword="NaN"/>.
    /// </summary>
    public float GetMaxHeight()
    {
        return Shape switch
        {
            ZoneShape.AABB     when AABBInfo != null    => AABBInfo.Size.y >= Level.HEIGHT - 0.5f ? float.NaN : Center.y + AABBInfo.Size.y / 2f,
            ZoneShape.Cylinder when CircleInfo != null  => CircleInfo.MaximumHeight.GetValueOrDefault(float.NaN),
            ZoneShape.Sphere   when CircleInfo != null  => Center.y + CircleInfo.Radius,
            ZoneShape.Polygon  when PolygonInfo != null => PolygonInfo.MaximumHeight.GetValueOrDefault(float.NaN),
            _ => float.NaN
        };
    }
}

public class ZoneCircleInfo
{
    /// <summary>
    /// Radius of the circle or sphere.
    /// </summary>
    [JsonPropertyName("radius")]
    public float Radius { get; set; }

    /// <summary>
    /// Minimum Y value the zone takes effect in.
    /// </summary>
    [JsonPropertyName("min_height")]
    public float? MinimumHeight { get; set; }

    /// <summary>
    /// Maximum Y value the zone takes effect in.
    /// </summary>
    [JsonPropertyName("max_height")]
    public float? MaximumHeight { get; set; }
}

public class ZoneAABBInfo
{
    /// <summary>
    /// 3D bounds of the rectangle.
    /// </summary>
    [JsonPropertyName("size")]
    public Vector3 Size { get; set; }
}

public class ZonePolygonInfo
{
    /// <summary>
    /// List of all points of the zone relative to the center.
    /// </summary>
    [JsonPropertyName("points")]
    public Vector2[] Points { get; set; } = Array.Empty<Vector2>();

    /// <summary>
    /// Minimum Y value the zone takes effect in.
    /// </summary>
    [JsonPropertyName("min_height")]
    public float? MinimumHeight { get; set; }

    /// <summary>
    /// Maximum Y value the zone takes effect in.
    /// </summary>
    [JsonPropertyName("max_height")]
    public float? MaximumHeight { get; set; }
}

public class UpstreamZone
{
    /// <summary>
    /// The unique name of the upstream zone.
    /// </summary>
    [JsonPropertyName("name")]
    public string ZoneName { get; set; }

    /// <summary>
    /// The chance of it being picked, where 1 is the standard value.
    /// </summary>
    [JsonPropertyName("weight")]
    public float Weight { get; set; }
}