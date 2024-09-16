using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which handles a patch on <see cref="StructureManager.destroyStructure(StructureDrop, byte, byte, Vector3, bool)"/>.
/// </summary>
public class StructureDestroyed : IBuildableDestroyedEvent
{
    private IBuildable? _buildableCache;

    /// <summary>
    /// Player that destroyed the structure, if any.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// Steam ID of the player that destroyed the structure, or <see cref="CSteamID.Nil"/>.
    /// </summary>
    public required CSteamID InstigatorId { get; init; }

    /// <summary>
    /// The structure's object and model data.
    /// </summary>
    public required StructureDrop Structure { get; init; }

    /// <summary>
    /// The structure's server-side data.
    /// </summary>
    public required StructureData ServersideData { get; init; }

    /// <summary>
    /// Coordinate of the structure region in <see cref="StructureManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The region the structure was placed in.
    /// </summary>
    public required StructureRegion Region { get; init; }

    /// <summary>
    /// Origin of the damage that caused the structure to be destroyed.
    /// </summary>
    public required EDamageOrigin DamageOrigin { get; init; }

    /// <summary>
    /// Instance Id of the structure that was destroyed.
    /// </summary>
    public required uint InstanceId { get; init; }

    /// <summary>
    /// Primary item used to destroy the structure.
    /// </summary>
    public IAssetLink<ItemAsset>? PrimaryAsset { get; init; }

    /// <summary>
    /// Secondary item used to destroy the structure.
    /// </summary>
    public IAssetLink<ItemAsset>? SecondaryAsset { get; init; }

    /// <summary>
    /// The Unity model of the structure.
    /// </summary>
    public Transform Transform => Structure.model;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the structure.
    /// </summary>
    public IBuildable Buildable => _buildableCache ??= new BuildableStructure(Structure);
    object IBuildableDestroyedEvent.Region => Region;
}
