using Uncreated.Warfare.Interaction.Requests;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

public class WarfareVehicleComponent : MonoBehaviour, IRequestable<VehicleSpawner>
{
    public WarfareVehicle WarfareVehicle { get; private set; }
    public WarfareVehicleComponent Init(WarfareVehicle warfareVehicle)
    {
        WarfareVehicle = warfareVehicle;
        return this;
    }
}