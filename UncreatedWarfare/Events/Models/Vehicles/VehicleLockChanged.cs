using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles <see cref="VehicleManager.OnToggledVehicleLock"/>.
/// </summary>
public class VehicleLockChanged
{
    /// <summary>
    /// Player that locked the vehicle, if any.
    /// </summary>
    public WarfarePlayer? Player { get; init; }

    /// <summary>
    /// The vehicle on which the lock state is being changed.
    /// </summary>
    public required InteractableVehicle Vehicle { get; init; }

    /// <summary>
    /// Steam ID of the new owner of the vehicle.
    /// </summary>
    public CSteamID OwnerId => Vehicle.lockedOwner;

    /// <summary>
    /// Group ID of the new owner of the vehicle.
    /// </summary>
    public CSteamID GroupId => Vehicle.lockedGroup;

    /// <summary>
    /// The new lock state of the vehicle.
    /// </summary>
    public bool IsLocked => Vehicle.isLocked;
}
