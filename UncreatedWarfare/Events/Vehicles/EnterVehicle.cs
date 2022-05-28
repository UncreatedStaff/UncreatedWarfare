using SDG.Unturned;

namespace Uncreated.Warfare.Events.Vehicles;
public class EnterVehicle : PlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte _seatIndex;
    public InteractableVehicle Vehicle => _vehicle;
    public byte PassengerIndex => _seatIndex;
    public Passenger PassengerData => _vehicle.passengers.Length >= _seatIndex ? null! : _vehicle.passengers[_seatIndex];
    public EnterVehicle(UCPlayer player, InteractableVehicle vehicle, byte seatIndex) : base(player)
    {
        _vehicle = vehicle;
        _seatIndex = seatIndex;
    }
}
