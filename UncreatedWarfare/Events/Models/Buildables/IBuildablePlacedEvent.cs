using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Base event for when a barricade or structure is placed.
/// </summary>
public interface IBuildablePlacedEvent
{
    /// <summary>
    /// The owner of the buildable, if they're online.
    /// </summary>
    WarfarePlayer? Owner { get; }

    /// <summary>
    /// Coordinate of the region the buildable was placed in.
    /// </summary>
    RegionCoord RegionPosition { get; }

    /// <summary>
    /// The Steam ID of the player that placed the structure.
    /// </summary>
    CSteamID OwnerId { get; }

    /// <summary>
    /// The Steam ID of the group that placed the structure.
    /// </summary>
    CSteamID GroupId { get; }

    /// <summary>
    /// The Unity model of the structure.
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the buildable.
    /// </summary>
    IBuildable Buildable { get; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the buildable is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    ushort VehicleRegionIndex { get; }

    /// <summary>
    /// If this buildable is being placed on a vehicle.
    /// </summary>
    bool IsOnVehicle { get; }
}
