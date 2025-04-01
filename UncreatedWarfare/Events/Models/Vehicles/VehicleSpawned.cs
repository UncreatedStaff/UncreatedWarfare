using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening for a vehicle to be added.
/// </summary>
public class VehicleSpawned
{
    /// <summary>
    /// The vehicle that was spawned.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }
}
