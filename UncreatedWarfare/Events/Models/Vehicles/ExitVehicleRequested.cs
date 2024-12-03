using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_vehicle" ])]
public class ExitVehicleRequested : CancellablePlayerEvent
{
    /// <summary>
    /// Vehicle being exited from.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

    /// <summary>
    /// The <see cref="VehicleComponent"/> data of the vehicle being exited.
    /// </summary>
    public required VehicleComponent Component { get; init; }

    /// <summary>
    /// Location where the player will exit at.
    /// </summary>
    public required Vector3 ExitLocation { get; set; }

    /// <summary>
    /// Yaw rotation where the player will exit at.
    /// </summary>
    public required float ExitLocationYaw { get; set; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's current seat.
    /// </summary>
    public required byte PassengerIndex { get; init; }

    /// <summary>
    /// Client-side velocity if the exiting player is the driver.
    /// </summary>
    public required Vector3 JumpVelocity { get; init; }

    /// <summary>
    /// The seat object for the player's current seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.passengers.Length >= PassengerIndex ? null! : Vehicle.passengers[PassengerIndex];
}
