using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Events.Models.Vehicles;

public class EnterVehicle : PlayerEvent
{
    /// <summary>
    /// Vehicle that was entered.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

    /// <summary>
    /// The <see cref="VehicleComponent"/> data of the vehicle that was entered.
    /// </summary>
    public required VehicleComponent Component { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's new seat.
    /// </summary>
    public required int PassengerIndex { get; init; }

    /// <summary>
    /// The seat object for the player's new seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.passengers.Length >= PassengerIndex ? null! : Vehicle.passengers[PassengerIndex];
}
