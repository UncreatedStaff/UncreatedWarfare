using System;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_vehicle" ])]
public class VehicleSwapSeatRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The player's current vehicle.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's new seat.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Seat out of range or taken.</exception>
    public required int NewPassengerIndex
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

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's old seat.
    /// </summary>
    public required int OldPassengerIndex { get; init; }
    
    /// <summary>
    /// If the <see cref="KnownCooldowns.VehicleInteract"/> cooldown should be ignored.
    /// </summary>
    public bool IgnoreInteractCooldown { get; init; }

    /// <summary>
    /// The seat object for the player's old seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.Vehicle.passengers.Length >= NewPassengerIndex ? null! : Vehicle.Vehicle.passengers[NewPassengerIndex];
}