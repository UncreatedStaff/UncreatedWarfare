using Uncreated.Warfare.Buildables;

namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureDrop.OnSalvageRequested_Global"/>.
/// </summary>
public sealed class SalvageStructureRequested(StructureRegion region) : SalvageRequested(region)
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