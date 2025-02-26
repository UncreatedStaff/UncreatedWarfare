using System;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_vehicle" ])]
public class EnterVehicleRequested : CancellablePlayerEvent
{
    /// <summary>
    /// Vehicle being entered.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

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

            if (value < byte.MinValue || value >= Vehicle.Vehicle.passengers.Length || value > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} out of range [0, {Math.Min(byte.MaxValue + 1, Vehicle.Vehicle.passengers.Length)}).");
            
            if (Vehicle.Vehicle.passengers[value].player != null)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} taken.");

            field = value;
        }
    }
}
