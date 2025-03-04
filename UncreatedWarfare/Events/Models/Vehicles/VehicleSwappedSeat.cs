using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class VehicleSwappedSeat : PlayerEvent
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
}
