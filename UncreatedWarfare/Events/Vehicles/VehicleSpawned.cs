using SDG.Unturned;

namespace Uncreated.Warfare.Events.Vehicles;
public class VehicleSpawned : EventState
{
    private readonly InteractableVehicle _vehicle;
    public InteractableVehicle Vehicle => _vehicle;
    public VehicleSpawned(InteractableVehicle vehicle)
    {
        _vehicle = vehicle;
    }
}
