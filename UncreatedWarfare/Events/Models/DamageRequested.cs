using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models;

/// <summary>
/// Base class for <see cref="DamageBarricadeRequested"/> and <see cref="DamageStructureRequested"/>.
/// </summary>
public abstract class DamageRequested(object region) : CancellableEvent, IBuildableDestroyedEvent
{
    protected readonly object RegionObj = region;
    protected IBuildable? BuildableCache;
    protected BuildableSave? SaveCache;

    /// <summary>
    /// The player that tried to damage the barricade, if any.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// The steam ID player that tried to damage the barricade, if any.
    /// </summary>
    public required CSteamID InstigatorId { get; init; }

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the buildable.
    /// </summary>
    public abstract IBuildable Buildable { get; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// Instance Id of the buildable.
    /// </summary>
    public required uint InstanceId { get; init; }

    /// <summary>
    /// The Unity model of the buildable.
    /// </summary>
    public abstract Transform Transform { get; }

    /// <summary>
    /// The index of the buildable in it's region.
    /// </summary>
    public required ushort RegionIndex { get; init; }

    /// <summary>
    /// Total damage to do to the buildable.
    /// </summary>
    /// <remarks>Can be changed.</remarks>
    public required float Damage { get; set; }

    EDamageOrigin IBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.SecondaryAsset => null;
    object IBuildableDestroyedEvent.Region => RegionObj;
}