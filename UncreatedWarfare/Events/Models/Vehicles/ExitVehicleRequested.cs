namespace Uncreated.Warfare.Events.Vehicles;
public class ExitVehicleRequested : CancellablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private Vector3 pendingLocation;
    private float pendingYaw;
    public InteractableVehicle Vehicle => _vehicle;
    public Vector3 ExitLocation
    {
        get => pendingLocation;
        set => pendingLocation = value;
    }
    public float ExitLocationYaw
    {
        get => pendingYaw;
        set => pendingYaw = value;
    }
    public ExitVehicleRequested(Player player, InteractableVehicle vehicle, bool shouldAllow, Vector3 pendingLocation, float pendingYaw) : base(UCPlayer.FromPlayer(player)!)
    {
        _vehicle = vehicle;
        if (!shouldAllow)
            Break();
        this.pendingLocation = pendingLocation;
        this.pendingYaw = pendingYaw;
    }
}
