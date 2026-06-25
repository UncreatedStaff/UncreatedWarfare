using Uncreated.Warfare.Interaction.Requests;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

public class WarfareVehicleComponent : MonoBehaviour, IRequestable<VehicleSpawner>
{
#nullable disable

    public WarfareVehicle WarfareVehicle { get; private set; }

#nullable restore

    public WarfareVehicleComponent Init(WarfareVehicle warfareVehicle)
    {
        WarfareVehicle = warfareVehicle;
        return this;
    }
}