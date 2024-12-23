using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.DamageTracking;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Transport;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles
{
    public class WarfareVehicle : IDisposable
    {
        public InteractableVehicle Vehicle { get; }
        public WarfareVehicleComponent Component { get; }
        public WarfareVehicleInfo Info { get; }
        public VehicleSpawner? Spawn { get; private set; }
        public VehicleDamageTracker DamageTracker { get; }
        public TranportTracker TranportTracker { get; }
        public Vector3 Position => Vehicle.transform.position;
        public Quaternion Rotation => Vehicle.transform.rotation;
        public WarfareVehicle(InteractableVehicle interactableVehicle, WarfareVehicleInfo info)
        {
            Vehicle = interactableVehicle;
            Info = info;
            Component = interactableVehicle.transform.GetOrAddComponent<WarfareVehicleComponent>();
            Component.Init(this);
            DamageTracker = new VehicleDamageTracker();
            TranportTracker = new TranportTracker();
        }

        public void Dispose()
        {
            Object.Destroy(Component);
        }

        internal void UnlinkFromSpawn(VehicleSpawner spawn)
        {
            GameThread.AssertCurrent();

            if (spawn == null)
                throw new ArgumentNullException(nameof(spawn));

            if (!Equals(Spawn, spawn))
            {
                throw new InvalidOperationException("The given spawn is not linked to this vehicle.");
            }

            if (Spawn?.LinkedVehicle == Vehicle)
            {
                throw new InvalidOperationException("The old linked spawn is still linked to this vehicle.");
            }

            Spawn = null;
        }

        internal void LinkToSpawn(VehicleSpawner spawn)
        {
            GameThread.AssertCurrent();

            if (spawn == null)
                throw new ArgumentNullException(nameof(spawn));

            if (spawn.LinkedVehicle != Vehicle)
            {
                throw new InvalidOperationException("The given spawn is not linked to this vehicle.");
            }

            Spawn = spawn;
        }

        public override bool Equals(object? obj)
        {
            return obj is WarfareVehicle vehicle && Vehicle.GetNetId().Equals(vehicle.Vehicle.GetNetId());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Vehicle.GetNetId());
        }
    }
}
