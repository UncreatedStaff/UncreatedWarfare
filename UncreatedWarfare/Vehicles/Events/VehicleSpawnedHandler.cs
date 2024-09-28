using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Vehicles.Events;

internal class VehicleSpawnedHandler : IEventListener<VehicleSpawned>, ILayoutStartingListener
{
    [EventListener(RequireActiveLayout = true)]
    void IEventListener<VehicleSpawned>.HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        e.Vehicle.gameObject.AddComponent<VehicleComponent>().Initialize(e.Vehicle, serviceProvider);
    }

    UniTask ILayoutStartingListener.HandleLayoutStartingAsync(Layout layout, CancellationToken token)
    {
        IServiceProvider serviceProvider = layout.ServiceProvider.Resolve<IServiceProvider>();
        foreach (InteractableVehicle vehicle in VehicleManager.vehicles)
        {
            if (!vehicle.TryGetComponent(out VehicleComponent vehicleComp))
                vehicleComp = vehicle.gameObject.AddComponent<VehicleComponent>();

            vehicleComp.Initialize(vehicle, serviceProvider);
        }

        return UniTask.CompletedTask;
    }
}