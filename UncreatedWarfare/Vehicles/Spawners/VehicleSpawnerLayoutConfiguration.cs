using Uncreated.Warfare.Vehicles.Spawners.Delays;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawnerLayoutConfiguration
{
    public required string SpawnerName { get; set; }
    public TimerDelay? Delay { get; set; }
}
