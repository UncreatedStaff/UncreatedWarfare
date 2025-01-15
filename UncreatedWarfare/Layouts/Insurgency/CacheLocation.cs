using System;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Locations;

namespace Uncreated.Warfare.Layouts.Insurgency;

/// <summary>
/// Represents a location where a cache can spawn in Insurgency.
/// </summary>
[CannotApplyEqualityOperator]
public class CacheLocation : IEquatable<CacheLocation>
{
    /// <summary>
    /// Display name of the cache. Purely for file readability.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// World position of the cache.
    /// </summary>
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    /// <summary>
    /// Euler rotation of the cache. Use <see cref="GetPlacementAngle"/> to place buildables.
    /// </summary>
    [JsonPropertyName("euler_angles")]
    public Vector3 Rotation { get; set; }

    /// <summary>
    /// Player who set up the cache.
    /// </summary>
    [JsonPropertyName("placer")]
    public ulong Placer { get; set; }

    /// <summary>
    /// If the cache was temporarily disabled.
    /// </summary>
    [JsonPropertyName("disabled")]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Optional override for which buildable to use.
    /// </summary>
    [JsonPropertyName("buildable")]
    public IAssetLink<ItemPlaceableAsset>? CacheBuildable { get; set; }

    /// <summary>
    /// Get the angle at which to spawn the buildable.
    /// </summary>
    public Quaternion GetPlacementAngle() => Quaternion.Euler(Rotation);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CacheLocation cacheLocation && Equals(cacheLocation);

    /// <inheritdoc />
    public bool Equals(CacheLocation? location)
    {
        if (location is null)
            return this is null;
        return location.Position.IsNearlyEqual(Position) && location.Rotation.IsNearlyEqual(Rotation);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Position, Rotation);

    // ReSharper restore NonReadonlyMemberInGetHashCode

    /// <inheritdoc />
    public override string ToString() => Name ?? ("[" + new GridLocation(Position) + "] " + LocationHelper.GetClosestLocationName(Position));
}