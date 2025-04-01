using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Base event for when a barricade or structure is placed.
/// </summary>
public interface IPlaceBuildableRequestedEvent : ICancellable
{
    /// <summary>
    /// The player that initially tried to place the barricade.
    /// </summary>
    WarfarePlayer OriginalPlacer { get; }

    /// <summary>
    /// Coordinate of the region the buildable was placed in.
    /// </summary>
    RegionCoord RegionPosition { get; }

    /// <summary>
    /// The player that owns the buildable's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    CSteamID Owner { get; set; }

    /// <summary>
    /// The group that owns the buildable's Steam ID, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    CSteamID GroupOwner { get; set; }

    /// <summary>
    /// The exact position of the buildable.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    /// <exception cref="ArgumentException">Position is not in a region when not planted.</exception>
    Vector3 Position { get; set; }

    /// <summary>
    /// The exact rotation of the buildable.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// Buildable place target (where the player was looking). This could be a vehicle in which case barricades will be planted.
    /// </summary>
    /// <remarks>If this is a vehicle, it will be stored in <see cref="TargetVehicle"/>.</remarks>
    Transform? HitTarget { get; }

    /// <summary>
    /// The asset of the buildable being placed.
    /// </summary>
    ItemPlaceableAsset Asset { get; }

    /// <summary>
    /// If the buildable being placed is a structure.
    /// </summary>
    bool IsStructure { get; }

    /// <summary>
    /// The vehicle the buildable will be placed on, if any.
    /// </summary>
    InteractableVehicle? TargetVehicle { get; }

    /// <summary>
    /// If this buildable is being placed on a vehicle.
    /// </summary>
    bool IsOnVehicle { get; }

    /// <summary>
    /// The <see cref="Barricade"/> or <see cref="Structure"/> of the buildable.
    /// </summary>
    object Item { get; }

    /// <summary>
    /// Get <see cref="Item"/> as either a <see cref="Barricade"/> or <see cref="Structure"/>.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    TData GetItem<TData>() where TData : class
    {
        return Item as TData ?? throw new InvalidCastException($"This buildable's item is not a {Accessor.ExceptionFormatter.Format<TData>()}.");
    }
}