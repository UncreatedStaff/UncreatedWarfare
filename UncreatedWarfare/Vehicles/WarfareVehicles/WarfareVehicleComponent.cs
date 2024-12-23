using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles
{
    public class WarfareVehicleComponent : MonoBehaviour, IRequestable<VehicleSpawner>
    {
        public WarfareVehicle WarfareVehicle { get; private set; }
        public void Init(WarfareVehicle warfareVehicle)
        {
            WarfareVehicle = warfareVehicle;
        }
    }
}
