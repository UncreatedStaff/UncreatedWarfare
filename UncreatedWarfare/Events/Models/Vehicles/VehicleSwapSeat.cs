namespace Uncreated.Warfare.Events.Vehicles;
public class VehicleSwapSeat : PlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte _fromSeatIndex;
    private readonly byte _toSeatIndex;
    public InteractableVehicle Vehicle => _vehicle;
    public byte OldSeat => _fromSeatIndex;
    public byte NewSeat => _toSeatIndex;
    public Passenger OldPassengerData => _vehicle.passengers.Length >= _fromSeatIndex ? null! : _vehicle.passengers[_fromSeatIndex];
    public Passenger PassengerData => _vehicle.passengers.Length >= _toSeatIndex ? null! : _vehicle.passengers[_toSeatIndex];
    public VehicleSwapSeat(UCPlayer player, InteractableVehicle vehicle, byte fromSeatIndex, byte toSeatIndex) : base(player)
    {
        _vehicle = vehicle;
        _fromSeatIndex = fromSeatIndex;
        _toSeatIndex = toSeatIndex;
    }
}
