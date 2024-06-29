using SDG.Unturned;

namespace Uncreated.Warfare.Events.Vehicles;
public class VehicleLockChangeRequested : CancellablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    public InteractableVehicle Vehicle => _vehicle;
    public bool IsLocking => !Vehicle.isLocked;
    public VehicleLockChangeRequested(Player player, InteractableVehicle vehicle, bool shouldAllow) : base(UCPlayer.FromPlayer(player)!)
    {
        _vehicle = vehicle;
        if (!shouldAllow) Break();
    }
}
