using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

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

    [SetsRequiredMembers]
    public VehicleSwapSeatRequested(WarfarePlayer player, InteractableVehicle vehicle, bool shouldAllow, byte sourceSeat, byte resultSeat)
    {
        Player = player;
        _vehicle = vehicle;
        this.sourceSeat = sourceSeat;
        finalSeat = resultSeat;
    }
}
