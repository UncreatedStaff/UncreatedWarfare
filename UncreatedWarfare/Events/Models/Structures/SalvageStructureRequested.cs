using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureDrop.OnSalvageRequested_Global"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public sealed class SalvageStructureRequested : SalvageRequested, ISalvageBuildableRequestedEvent
{
    /// <inheritdoc />
    public override bool IsCancelled => base.IsCancelled || ServersideData.structure.isDead;

    /// <summary>
    /// The structure's object and model data.
    /// </summary>
    public required StructureDrop Structure { get; init; }

    /// <summary>
    /// The structure's server-side data.
    /// </summary>
    public required StructureData ServersideData { get; init; }

    /// <summary>
    /// The region the structure was placed in.
    /// </summary>
    public required StructureRegion Region { get; init; }

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the structure.
    /// </summary>
    public override IBuildable Buildable => BuildableCache ??= new BuildableStructure(Structure);

    /// <summary>
    /// The Unity model of the structure.
    /// </summary>
    public override Transform Transform => Structure.model;
    bool IBaseBuildableDestroyedEvent.WasSalvaged => true;
    ushort IBaseBuildableDestroyedEvent.VehicleRegionIndex => ushort.MaxValue;
    bool IBaseBuildableDestroyedEvent.IsOnVehicle => false;
    EDamageOrigin IBaseBuildableDestroyedEvent.DamageOrigin => EDamageOrigin.Unknown;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.SecondaryAsset => null;
    WarfarePlayer IBaseBuildableDestroyedEvent.Instigator => Player;
    CSteamID IBaseBuildableDestroyedEvent.InstigatorId => Player.Steam64;
}