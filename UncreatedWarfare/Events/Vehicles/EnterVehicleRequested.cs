using SDG.Unturned;

namespace Uncreated.Warfare.Events.Vehicles;
public class EnterVehicleRequested : BreakablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    public InteractableVehicle Vehicle => _vehicle;
    public EnterVehicleRequested(Player player, InteractableVehicle vehicle, bool shouldAllow) : base(UCPlayer.FromPlayer(player)!)
    {
        _vehicle = vehicle;
        if (!shouldAllow) Break();
    }
}
