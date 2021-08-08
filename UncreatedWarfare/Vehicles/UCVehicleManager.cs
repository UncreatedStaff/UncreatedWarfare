using Org.BouncyCastle.Crypto;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using UnityEngine;
using static UnityEngine.Physics;

namespace Uncreated.Warfare.Vehicles
{
    public static class UCVehicleManager
    {
        public static InteractableVehicle VehicleFromPlayerLook(UnturnedPlayer player)
        {
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
        public static IEnumerable<InteractableVehicle> GetNearbyVehicles(ushort id, float radius, Vector3 origin)
        {
            float sqrRadius = radius * radius;
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
            VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
            for (int v = 0; v < vehicles.Count; v++)
{
                if (vehicles[v].id == id)
                    newvehicles.Add(vehicles[v]);
            }
            vehicles.Clear();
            return newvehicles;
        }
        public static IEnumerable<InteractableVehicle> GetNearbyVehicles(IEnumerable<ushort> ids, float radius, Vector3 origin)
        {
            float sqrRadius = radius * radius;
            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
            List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
            VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
            for (int v = 0; v < vehicles.Count; v++)
            {
                if (ids.Contains(vehicles[v].id))
                    newvehicles.Add(vehicles[v]);
            }
            vehicles.Clear();
            return newvehicles;
        }
    }
}
