using Uncreated.Warfare.Components;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class ExitVehicle : PlayerEvent
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
}
