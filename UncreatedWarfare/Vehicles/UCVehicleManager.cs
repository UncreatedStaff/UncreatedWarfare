using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public static VehicleBarricadeRegion FindRegionFromVehicleWithIndex(this InteractableVehicle vehicle, out int index, int subvehicleIndex = 0)
        {
            if (vehicle == null)
            {
                index = -1;
                return null;
            }
            for (int i = 0; i < BarricadeManager.vehicleRegions.Count; i++)
            {
                VehicleBarricadeRegion vehicleRegion = BarricadeManager.vehicleRegions[i];
                if (vehicleRegion.vehicle == vehicle && vehicleRegion.subvehicleIndex == subvehicleIndex)
                {
                    index = i;
                    return vehicleRegion;
                }
            }
            index = -1;
            return null;
        }
    }
}
