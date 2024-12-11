namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which fire just before an InteractableVehicle is destroyed.
/// </summary>
public class VehicleDespawned
{
    /// <summary>
    /// The vehicle that was despawned.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }
}
