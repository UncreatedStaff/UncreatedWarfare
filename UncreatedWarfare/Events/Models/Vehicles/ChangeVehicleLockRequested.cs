using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles <see cref="VehicleManager.OnToggleVehicleLockRequested"/>.
/// </summary>
public class ChangeVehicleLockRequested : CancellablePlayerEvent
{
    /// <summary>
    /// The vehicle on which the lock state is being changed.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }
    public bool IsLocking => Vehicle.Vehicle.asset.canBeLocked && !Vehicle.Vehicle.isLocked;
}
