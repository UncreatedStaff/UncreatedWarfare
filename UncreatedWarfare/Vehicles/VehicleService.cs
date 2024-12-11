using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles.Info;
using Uncreated.Warfare.Vehicles.Vehicle;

namespace Uncreated.Warfare.Vehicles
{
    [Priority(-3 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
    public class VehicleService : 
        ILayoutHostedService,
        IEventListener<VehicleSpawned>,
        IEventListener<VehicleDespawned>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly VehicleInfoStore _vehicleInfoStore;
        private readonly VehicleSpawnerStore _vehicleSpawnerStore;

        public TrackingList<WarfareVehicle> Vehicles { get; }

        public VehicleService(IServiceProvider serviceProvider)
        {
            Vehicles = new TrackingList<WarfareVehicle>();
            _serviceProvider = serviceProvider;
            _vehicleInfoStore = serviceProvider.GetRequiredService<VehicleInfoStore>();
            _vehicleSpawnerStore = serviceProvider.GetRequiredService<VehicleSpawnerStore>();
        }

        public UniTask StartAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public UniTask StopAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
        public WarfareVehicle? RegisterWarfareVehicle(InteractableVehicle vehicle)
        {
            WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(vehicle.asset);

            if (info == null)
                info = new WarfareVehicleInfo(); // todo: make a default WarfareVehicleInfo to avoid nullptr exceptions

            WarfareVehicle warfareVehicle = new WarfareVehicle(vehicle, info);
            Vehicles.AddIfNotExists(warfareVehicle);
            return warfareVehicle;
        }
        public WarfareVehicle? DeregisterWarfareVehicle(InteractableVehicle vehicle)
        {
            WarfareVehicle? existing = Vehicles.FindAndRemove(f => f.Vehicle == vehicle);
            existing?.Dispose();
            return existing;
        }
        public void HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
        {
            RegisterWarfareVehicle(e.Vehicle);
        }
        public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
        {
            DeregisterWarfareVehicle(e.Vehicle);
        }
    }
}
