using SDG.Unturned;

namespace Uncreated.Warfare.Events.Vehicles;

/// <summary>
/// Event listener args which handles a patch listening for a vehicle to be added.
/// </summary>
public class VehicleSpawned
{
    /// <summary>
    /// The vehicle that was spawned.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }
}
