using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles a patch on <see cref="InteractableSign.updateText"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class SignTextChanged : IActionLoggableEvent
{
    private IBuildable? _cachedBuildable;

    /// <summary>
    /// Player that changed the sign text, if they exist.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The region the barricade was placed in. This could be of type <see cref="VehicleBarricadeRegion"/>.
    /// </summary>
    public required BarricadeRegion Region { get; init; }

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// The barricade's sign component.
    /// </summary>
    public required InteractableSign Sign { get; init; }

    /// <summary>
    /// The Unity model of the barricade.
    /// </summary>
    public Transform Transform => Barricade.model;

    /// <summary>
    /// If the barricade is planted on a vehicle.
    /// </summary>
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public IBuildable Buildable => _cachedBuildable ??= new BuildableBarricade(Barricade);

    /// <summary>
    /// New text on the sign.
    /// </summary>
    public required string Text { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        BarricadeData serversideData = Barricade.GetServersideData();
        return new ActionLogEntry(ActionLogTypes.BuildableSignChanged,
            $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID}, " +
            $"New text: \"{Text}\"",
            Instigator
        );
    }
}
