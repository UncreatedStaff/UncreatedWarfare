using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Vehicles;
public class ExitVehicle : PlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    private readonly byte _previousSeat;
    public InteractableVehicle Vehicle => _vehicle;
    public byte OldPassengerIndex => _previousSeat;
    public Passenger OldPassengerData => _vehicle.passengers.Length >= _previousSeat ? null! : _vehicle.passengers[_previousSeat];


    [SetsRequiredMembers]
    public ExitVehicle(WarfarePlayer player, InteractableVehicle vehicle, byte previousSeat)
    {
        Player = player;
        _vehicle = vehicle;
        this._previousSeat = previousSeat;
    }
}
