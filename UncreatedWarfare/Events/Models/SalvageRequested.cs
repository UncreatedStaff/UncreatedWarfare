using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models;

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

    /// <summary>
    /// The team that was responsible for the buildable being destroyed.
    /// </summary>
    public required Team InstigatorTeam { get; init; }

    bool IBuildableDestroyedEvent.WasSalvaged => true;
    EDamageOrigin IBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBuildableDestroyedEvent.SecondaryAsset => null;
    WarfarePlayer IBuildableDestroyedEvent.Instigator => Player;
    object IBuildableDestroyedEvent.Region => RegionObj;
    CSteamID IBuildableDestroyedEvent.InstigatorId => Player.Steam64;
}