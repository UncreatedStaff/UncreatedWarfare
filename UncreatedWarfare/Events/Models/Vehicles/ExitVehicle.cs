namespace Uncreated.Warfare.Events.Models.Vehicles;
public class ExitVehicle : PlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte previousSeat;
    public InteractableVehicle Vehicle => _vehicle;
    public byte OldPassengerIndex => previousSeat;
    public Passenger OldPassengerData => _vehicle.passengers.Length >= previousSeat ? null! : _vehicle.passengers[previousSeat];
    public ExitVehicle(UCPlayer player, InteractableVehicle vehicle, byte previousSeat) : base(player)
    {
        _vehicle = vehicle;
        this.previousSeat = previousSeat;
    }
}
