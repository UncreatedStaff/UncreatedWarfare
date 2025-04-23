using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Structures;

/// <summary>
/// Event listener args which handles <see cref="StructureManager.onStructureSpawned"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class StructurePlaced : IBuildablePlacedEvent
{
    protected IBuildable? BuildableCache;

    /// <summary>
    /// The owner of the structure, if they're online.
    /// </summary>
    public required WarfarePlayer? Owner { get; init; }

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

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public IBuildable Buildable => BuildableCache ??= new BuildableStructure(Structure);

    ushort IBuildablePlacedEvent.VehicleRegionIndex => ushort.MaxValue;

    bool IBuildablePlacedEvent.IsOnVehicle => false;

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.BuildableDestroyed,
            $"Structure {AssetLink.ToDisplayString(Structure.asset)} owned by {ServersideData.owner} ({ServersideData.group}) # {Structure.instanceID} " +
            $"@ {ServersideData.point:F2}, {ServersideData.rotation:F2}",
            Owner?.Steam64.m_SteamID ?? 0
        );
    }
}