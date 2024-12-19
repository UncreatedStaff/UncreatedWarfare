using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Vehicles.Spawners.Delays;

namespace Uncreated.Warfare.Vehicles.Spawners;
public class VehicleSpawnerLayoutConfiguration
{
    public string SpawnerName { get; set; }
    public TimerDelay? Delay { get; set; }
}
