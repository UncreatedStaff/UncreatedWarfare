using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Physics;

namespace UncreatedWarfare.Vehicles
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
    }
}
