using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Structures;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Base class for <see cref="SalvageBarricadeRequested"/> and <see cref="SalvageStructureRequested"/>.
/// </summary>
public abstract class SalvageRequested(object region) : CancellablePlayerEvent, IBuildableDestroyedEvent
{
    protected readonly object RegionObj = region;
    protected IBuildable? BuildableCache;
    protected BuildableSave? SaveCache;

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

    EDamageOrigin IBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.SecondaryAsset => null;
    UCPlayer IBuildableDestroyedEvent.Instigator => Player;
    object IBuildableDestroyedEvent.Region => RegionObj;
    CSteamID IBuildableDestroyedEvent.InstigatorId => Player.CSteamID;
}
