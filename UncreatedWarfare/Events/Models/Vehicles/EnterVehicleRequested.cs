using System;
using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_vehicle" ])]
public class EnterVehicleRequested : CancellablePlayerEvent
{
    /// <summary>
    /// Vehicle being entered.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

    /// <summary>
    /// The <see cref="VehicleComponent"/> data of the vehicle being entered.
    /// </summary>
    public required VehicleComponent Component { get; init; }

    /// <summary>
    /// The seat the player will enter.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Seat out of range or taken.</exception>
    public required int Seat
    {
        get;
        set
        {
            if (field == value)
                return;

            if (value < byte.MinValue || value >= Vehicle.passengers.Length || value > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} out of range [0, {Math.Min(byte.MaxValue + 1, Vehicle.passengers.Length)}).");
            
            if (Vehicle.passengers[value].player != null)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} taken.");

            field = value;
        }
    }
}
