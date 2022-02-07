using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Physics;

namespace Uncreated.Warfare.Vehicles
{
    public static class UCVehicleManager
    {
        public static InteractableVehicle VehicleFromPlayerLook(UnturnedPlayer player)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Transform look = player.Player.look.aim;
            Ray ray = new Ray
            {
                direction = look.forward,
                origin = look.position
            };
            //4 units for normal reach
            if (Raycast(ray, out RaycastHit hit, 4, RayMasks.VEHICLE))
            {
                return hit.transform.GetComponent<InteractableVehicle>();
            }
            else
            {
                return null;
            }
        }
        public static VehicleBarricadeRegion FindRegionFromVehicleWithIndex(this InteractableVehicle vehicle, out ushort index, int subvehicleIndex = 0)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (vehicle == null)
            {
                index = ushort.MaxValue;
                return null;
            }
            for (ushort i = 0; i < BarricadeManager.vehicleRegions.Count; i++)
            {
                VehicleBarricadeRegion vehicleRegion = BarricadeManager.vehicleRegions[i];
                if (vehicleRegion.vehicle == vehicle && vehicleRegion.subvehicleIndex == subvehicleIndex)
                {
                    index = i;
                    return vehicleRegion;
                }
            }
            index = ushort.MaxValue;
            return null;
        }
        public static IEnumerable<InteractableVehicle> GetNearbyVehicles(Guid id, float radius, Vector3 origin)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            float sqrRadius = radius * radius;
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
            VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
            for (int v = 0; v < vehicles.Count; v++)
            {
                if (vehicles[v].asset.GUID == id)
                    newvehicles.Add(vehicles[v]);
            }
            vehicles.Clear();
            return newvehicles;
        }
        public static IEnumerable<InteractableVehicle> GetNearbyVehicles(IEnumerable<Guid> ids, float radius, Vector3 origin)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            float sqrRadius = radius * radius;
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
            VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
            for (int v = 0; v < vehicles.Count; v++)
            {
                if (ids.Contains(vehicles[v].asset.GUID))
                    newvehicles.Add(vehicles[v]);
            }
            vehicles.Clear();
            return newvehicles;
        }
        public static InteractableVehicle GetNearestLogi(Vector3 point, float radius, ulong team = 0)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            VehicleManager.getVehiclesInRadius(point, Mathf.Pow(radius, 2), vehicles);
            return vehicles.FirstOrDefault(v => v.lockedGroup.m_SteamID == team &&
            VehicleBay.VehicleExists(v.asset.GUID, out VehicleData vehicleData) &&
            (vehicleData.Type == EVehicleType.LOGISTICS || vehicleData.Type == EVehicleType.HELI_TRANSPORT));
        }
}
}
