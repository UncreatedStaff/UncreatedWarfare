namespace Uncreated.Warfare.Events.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureManager.onStructureSpawned"/>.
/// </summary>
public class StructurePlaced
{
    /// <summary>
    /// The owner of the structure, if they're online.
    /// </summary>
    public required UCPlayer? Owner { get; init; }

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
    /// The Steam ID of the player that placed the structure.
    /// </summary>
    public CSteamID OwnerId => new CSteamID(ServersideData.owner);

    /// <summary>
    /// The Steam ID of the group that placed the structure.
    /// </summary>
    public CSteamID GroupId => new CSteamID(ServersideData.group);

    /// <summary>
    /// The Unity model of the structure.
    /// </summary>
    public Transform Transform => Structure.model;
}