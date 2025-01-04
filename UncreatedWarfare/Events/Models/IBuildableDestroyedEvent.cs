using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models;

/// <summary>
/// Represents all event args in which a barricade or structure was destroyed.
/// </summary>
public interface IBuildableDestroyedEvent
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
    /// Region in which the buildable was.
    /// </summary>
    object Region { get; }

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
}