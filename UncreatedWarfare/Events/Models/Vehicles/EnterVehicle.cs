using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Vehicles;
public class EnterVehicle : PlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte _seatIndex;
    public InteractableVehicle Vehicle => _vehicle;
    public byte PassengerIndex => _seatIndex;
    public Passenger PassengerData => _vehicle.passengers.Length >= _seatIndex ? null! : _vehicle.passengers[_seatIndex];

    [SetsRequiredMembers]
    public EnterVehicle(WarfarePlayer player, InteractableVehicle vehicle, byte seatIndex)
    {
        Player = player;
        _vehicle = vehicle;
        _seatIndex = seatIndex;
    }
}
