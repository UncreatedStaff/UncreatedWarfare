namespace Uncreated.Warfare.Events.Models.Vehicles;
public class VehicleSwapSeatRequested : CancellablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte sourceSeat;
    private byte finalSeat;
    public InteractableVehicle Vehicle => _vehicle;
    public byte InitialSeat => sourceSeat;
    public byte FinalSeat
    {
        get => finalSeat;
        set => finalSeat = value;
    }
    public VehicleSwapSeatRequested(Player player, InteractableVehicle vehicle, bool shouldAllow, byte sourceSeat, byte resultSeat) : base(UCPlayer.FromPlayer(player)!)
    {
        _vehicle = vehicle;
        if (!shouldAllow) Break();
        this.sourceSeat = sourceSeat;
        finalSeat = resultSeat;
    }
}
