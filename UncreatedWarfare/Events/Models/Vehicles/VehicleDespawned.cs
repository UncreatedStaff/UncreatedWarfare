using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which fire just before an InteractableVehicle is destroyed.
/// </summary>
public class VehicleDespawned : IActionLoggableEvent
{
    /// <summary>
    /// The vehicle that was despawned.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.VehicleDespawned,
            $"{AssetLink.ToDisplayString(Vehicle.Asset)} owned by {Vehicle.Vehicle.lockedOwner} ({Vehicle.Vehicle.lockedOwner})",
            0
        );
    }
}
