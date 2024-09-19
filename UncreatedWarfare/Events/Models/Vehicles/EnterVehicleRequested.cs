using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Vehicles;
public class EnterVehicleRequested : CancellablePlayerEvent
{
    private readonly InteractableVehicle _vehicle;
    public InteractableVehicle Vehicle => _vehicle;

    [SetsRequiredMembers]
    public EnterVehicleRequested(WarfarePlayer player, InteractableVehicle vehicle, bool shouldAllow)
    {
        Player = player;
        _vehicle = vehicle;
    }
}
