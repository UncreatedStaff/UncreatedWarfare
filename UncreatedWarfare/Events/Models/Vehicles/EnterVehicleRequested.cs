namespace Uncreated.Warfare.Events.Models.Vehicles;
public class EnterVehicleRequested : CancellablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    public InteractableVehicle Vehicle => _vehicle;
    public EnterVehicleRequested(Player player, InteractableVehicle vehicle, bool shouldAllow) : base(UCPlayer.FromPlayer(player)!)
    {
        _vehicle = vehicle;
        if (!shouldAllow) Break();
    }
}
