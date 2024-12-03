using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class ExitVehicle : PlayerEvent
{
    /// <summary>
    /// Vehicle that was exited from.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's old seat.
    /// </summary>
    public required int PassengerIndex { get; init; }

    /// <summary>
    /// The <see cref="VehicleComponent"/> data of the vehicle that was exited.
    /// </summary>
    public required VehicleComponent Component { get; init; }

    /// <summary>
    /// The seat object for the player's old seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.passengers.Length >= PassengerIndex ? null! : Vehicle.passengers[PassengerIndex];
}
