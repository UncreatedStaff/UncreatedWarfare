using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Buildables;

namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which invoked after structure damage is applied. It does not get invoked if the structure is destroyed by the damage.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public sealed class StructureDamaged : BaseBuildableDamaged, IBuildableDamagedEvent
{
    /// <summary>
    /// The structure's object and model data.
    /// </summary>
    public required StructureDrop Structure { get; init; }

    /// <summary>
    /// The structure's server-side data.
    /// </summary>
    public required StructureData ServersideData { get; init; }

    /// <summary>
    /// The origin of the damage to the structure.
    /// </summary>
    public required EDamageOrigin DamageOrigin { get; init; }

    /// <summary>
    /// The item or vehicle that cause the damage to the structure.
    /// </summary>
    public required IAssetLink<Asset>? PrimaryAsset { get; init; }

    /// <summary>
    /// The item or vehicle that cause the damage to the structure. This may be an alternate item like the grenade thrown at a landmine, etc.
    /// </summary>
    public required IAssetLink<Asset>? SecondaryAsset { get; init; }

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

    /// <summary>
    /// The direction the ragdoll was sent.
    /// </summary>
    public required Vector3 Direction { get; init; }

    bool IBaseBuildableDestroyedEvent.WasSalvaged => false;
    ushort IBaseBuildableDestroyedEvent.VehicleRegionIndex => ushort.MaxValue;
    bool IBaseBuildableDestroyedEvent.IsOnVehicle => false;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.PrimaryAsset => null;
    IAssetLink<ItemAsset>? IBaseBuildableDestroyedEvent.SecondaryAsset => null;
}