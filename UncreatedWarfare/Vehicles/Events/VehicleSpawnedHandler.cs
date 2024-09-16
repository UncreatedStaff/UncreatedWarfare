using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;

namespace Uncreated.Warfare.Vehicles.Events;

internal class VehicleSpawnedHandler : IEventListener<VehicleSpawned>
{
    void IEventListener<VehicleSpawned>.HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        e.Vehicle.gameObject.AddComponent<VehicleComponent>().Initialize(e.Vehicle, serviceProvider);
    }
}