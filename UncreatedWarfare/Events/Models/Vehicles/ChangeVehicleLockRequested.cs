namespace Uncreated.Warfare.Events.Vehicles;

/// <summary>
/// Event listener args which handles <see cref="VehicleManager.OnToggleVehicleLockRequested"/>.
/// </summary>
public class ChangeVehicleLockRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The vehicle on which the lock state is being changed.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }
    public bool IsLocking => Vehicle.asset.canBeLocked && !Vehicle.isLocked;
}
