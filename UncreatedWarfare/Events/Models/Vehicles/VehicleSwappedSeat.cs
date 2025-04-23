using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(EventSynchronizationContext.Pure)]
public class VehicleSwappedSeat : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The player's current vehicle.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's new seat.
    /// </summary>
    public required int NewPassengerIndex { get; init; }
    
    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's old seat.
    /// </summary>
    public required int OldPassengerIndex { get; init; }

    /// <summary>
    /// The seat object for the player's old seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.Vehicle.passengers.Length >= NewPassengerIndex ? null! : Vehicle.Vehicle.passengers[NewPassengerIndex];

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.EnterVehicle,
            $"Swapped seated in {AssetLink.ToDisplayString(Vehicle.Asset)} (owned by {Vehicle.Vehicle.lockedOwner} ({Vehicle.Vehicle.lockedGroup})) from seat {OldPassengerIndex} -> {NewPassengerIndex} (turret: {PassengerData?.turret?.itemID.ToString() ?? "F"})",
            Player.Steam64.m_SteamID
        );
    }
}
