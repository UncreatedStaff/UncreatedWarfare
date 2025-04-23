using Uncreated.Warfare.Players;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

/// <summary>
/// Event listener args which handles <see cref="VehicleManager.OnToggledVehicleLock"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class VehicleLockChanged
{
    /// <summary>
    /// Player that locked the vehicle, if any.
    /// </summary>
    public WarfarePlayer? Player { get; init; }

    /// <summary>
    /// The vehicle on which the lock state is being changed.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }

    /// <summary>
    /// Steam ID of the new owner of the vehicle.
    /// </summary>
    public CSteamID OwnerId => Vehicle.Vehicle.lockedOwner;

    /// <summary>
    /// Group ID of the new owner of the vehicle.
    /// </summary>
    public CSteamID GroupId => Vehicle.Vehicle.lockedGroup;

    /// <summary>
    /// The new lock state of the vehicle.
    /// </summary>
    public bool IsLocked => Vehicle.Vehicle.isLocked;
}
