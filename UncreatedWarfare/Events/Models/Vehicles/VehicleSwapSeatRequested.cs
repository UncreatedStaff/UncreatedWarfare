using System;
using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Events.Models.Vehicles;
public class VehicleSwapSeatRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The player's current vehicle.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

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

            if (value < byte.MinValue || value >= Vehicle.passengers.Length || value > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} out of range [0, {Math.Min(byte.MaxValue + 1, Vehicle.passengers.Length)}).");

            if (Vehicle.passengers[value].player != null)
                throw new ArgumentOutOfRangeException(nameof(value), $"Seat {value} taken.");

            field = value;
        }
    }

    /// <summary>
    /// Passenger index in the vehicle's seat info for the player's old seat.
    /// </summary>
    public required int OldPassengerIndex { get; init; }

    /// <summary>
    /// The <see cref="VehicleComponent"/> data of the vehicle that was exited.
    /// </summary>
    public required VehicleComponent Component { get; init; }

    /// <summary>
    /// The seat object for the player's old seat.
    /// </summary>
    public Passenger PassengerData => Vehicle.passengers.Length >= NewPassengerIndex ? null! : Vehicle.passengers[NewPassengerIndex];
}