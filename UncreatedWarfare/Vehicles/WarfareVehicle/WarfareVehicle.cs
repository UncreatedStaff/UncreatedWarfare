using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Info;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Vehicles.Vehicle
{
    public class WarfareVehicle : IDisposable
    {
        public InteractableVehicle Vehicle { get; }
        public WarfareVehicleComponent Component { get; }
        public WarfareVehicleInfo Info { get; }
        public VehicleSpawner? Spawn { get; private set; }
        public Vector3 Position => Vehicle.transform.position;
        public Quaternion Rotation => Vehicle.transform.rotation;
        public WarfareVehicle(InteractableVehicle interactableVehicle, WarfareVehicleInfo info)
        {
            Vehicle = interactableVehicle;
            Info = info;
            Component = interactableVehicle.transform.GetOrAddComponent<WarfareVehicleComponent>();
            Component.Init(this);
        }

        public void Dispose()
        {
            GameObject.Destroy(Component);
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
    }
}
