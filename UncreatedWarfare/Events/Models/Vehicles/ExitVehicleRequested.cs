using System.Runtime.CompilerServices;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Vehicles;
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

    [SetsRequiredMembers]
    public ExitVehicleRequested(WarfarePlayer player, InteractableVehicle vehicle, Vector3 pendingLocation, float pendingYaw)
    {
        Player = player;
        _vehicle = vehicle;
        this.pendingLocation = pendingLocation;
        this.pendingYaw = pendingYaw;
    }
}
