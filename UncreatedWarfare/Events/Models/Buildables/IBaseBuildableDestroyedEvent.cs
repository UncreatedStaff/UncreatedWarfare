using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Invoked when a barricade or structure is destroyed (<see cref="BarricadeDestroyed"/> and <see cref="StructureDestroyed"/>)
/// </summary>
public interface IBuildableDestroyedEvent : IBaseBuildableDestroyedEvent, IActionLoggableEvent;

/// <summary>
/// Invoked when a barricade or structure is about to be damaged (<see cref="DamageBarricadeRequested"/> and <see cref="DamageStructureRequested"/>).
/// </summary>
public interface IDamageBuildableRequestedEvent : ICancellable, IBaseBuildableDestroyedEvent
{
    /// <summary>
    /// The amount of damage to be done to the buildable.
    /// </summary>
    ushort PendingDamage { get; }
}

/// <summary>
/// Invoked when a player is about to salvage a buildable (<see cref="SalvageBarricadeRequested"/> and <see cref="SalvageStructureRequested"/>).
/// </summary>
public interface ISalvageBuildableRequestedEvent : ICancellable, IBaseBuildableDestroyedEvent
{
    WarfarePlayer Player { get; }
}

/// <summary>
/// Represents all event args in which a barricade or structure was destroyed or a destruction is/was requested.
/// </summary>
public interface IBaseBuildableDestroyedEvent
{
    /// <summary>
    /// Player that destroyed the buildable.
    /// </summary>
    WarfarePlayer? Instigator { get; }

    /// <summary>
    /// Steam64 ID of the player that destroyed the buildable.
    /// </summary>
    CSteamID InstigatorId { get; }

    /// <summary>
    /// The Unity model of the buildable.
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Buildable that was destroyed.
    /// </summary>
    IBuildable Buildable { get; }

    /// <summary>
    /// Instance Id of the buildable that was destroyed.
    /// </summary>
    uint InstanceId { get; }

    /// <summary>
    /// Coordinate of the buildable region in it's corresponding region list.
    /// </summary>
    RegionCoord RegionPosition { get; }

    /// <summary>
    /// Origin of the damage that caused the buildable to be destroyed.
    /// </summary>
    EDamageOrigin DamageOrigin { get; }

    /// <summary>
    /// Primary item used to destroy the buildable.
    /// </summary>
    IAssetLink<ItemAsset>? PrimaryAsset { get; }

    /// <summary>
    /// Secondary item used to destroy the buildable.
    /// </summary>
    IAssetLink<ItemAsset>? SecondaryAsset { get; }

    /// <summary>
    /// If the buildable was salvaged.
    /// </summary>
    bool WasSalvaged { get; }

    /// <summary>
    /// The team that was responsible for the buildable being destroyed.
    /// </summary>
    Team InstigatorTeam { get; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the buildable is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    ushort VehicleRegionIndex { get; }

    /// <summary>
    /// If this buildable was placed on a vehicle.
    /// </summary>
    bool IsOnVehicle { get; }
}