using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class ExitVehicle : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// Vehicle that was exited from.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's old seat.
    /// </summary>
    public required int PassengerIndex { get; init; }

    /// <summary>
    /// The seat object for the player's old seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.Vehicle.passengers.Length >= PassengerIndex ? null! : Vehicle.Vehicle.passengers[PassengerIndex];

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.EnterVehicle,
            $"Exited {AssetLink.ToDisplayString(Vehicle.Asset)} (owned by {Vehicle.Vehicle.lockedOwner} ({Vehicle.Vehicle.lockedGroup})) from seat {PassengerIndex} (turret: {PassengerData?.turret?.itemID.ToString() ?? "F"})",
            Player.Steam64.m_SteamID
        );
    }
}
