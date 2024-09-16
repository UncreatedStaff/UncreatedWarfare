using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;


namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureManager.onDamageStructureRequested"/>.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public sealed class DamageStructureRequested(StructureRegion region) : DamageRequested(region)
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
    /// The direction the ragdoll is sent.
    /// </summary>
    public required Vector3 Direction { get; init; }

    /// <summary>
    /// The region the structure was placed in.
    /// </summary>
    public StructureRegion Region => (StructureRegion)RegionObj;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the structure.
    /// </summary>
    public override IBuildable Buildable => BuildableCache ??= new BuildableStructure(Structure);

    /// <summary>
    /// The Unity model of the structure.
    /// </summary>
    public override Transform Transform => Structure.model;
}