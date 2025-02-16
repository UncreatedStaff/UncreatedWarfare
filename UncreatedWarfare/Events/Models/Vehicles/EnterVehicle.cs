using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class EnterVehicle : PlayerEvent
{
    /// <summary>
    /// Vehicle that was entered.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's new seat.
    /// </summary>
    public required int PassengerIndex { get; init; }

    /// <summary>
    /// The seat object for the player's new seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.Vehicle.passengers.Length >= PassengerIndex ? null! : Vehicle.Vehicle.passengers[PassengerIndex];
}
