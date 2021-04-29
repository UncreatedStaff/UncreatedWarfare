using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Physics;

namespace UncreatedWarfare.FOBs
{
    public static class UCBarricadeManager
    {
        public static Barricade GetBarricadeFromLook(UnturnedPlayer player)
        {
            PlayerLook look = player.Player.look;

            Transform barricadeTransform = Raycast(look.aim.position, look.aim.forward, out var collision, Mathf.Infinity, RayMasks.BLOCK_COLLISION) &&
                   Raycast(look.aim.position, look.aim.forward, out var hit, Mathf.Infinity, RayMasks.BARRICADE) &&
                   collision.transform == hit.transform
                ? hit.transform
                : null;

            if (barricadeTransform == null || !BarricadeManager.tryGetInfo(barricadeTransform, out var x, out var y, out var plant, out var index,
                out var region))
                return null;
            return region.barricades[index].barricade;
        }

        public static InteractableSign GetSignFromLook(UnturnedPlayer player)
        {
            Transform look = player.Player.look.aim;
            Ray ray = new Ray
            {
                direction = look.forward,
                origin = look.position
            };
            RaycastHit hit;
            //4 units for normal reach
            if (Raycast(ray, out hit, 4, RayMasks.BARRICADE))
            {
                return hit.transform.GetComponent<InteractableSign>();
            }
            else
            {
                return null;
            }
        }

        public static BarricadeData GetBarricadeByInstanceID(uint InstanceID)
        {
            var barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();

            var barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

            return barricadeDatas.Find(brd => brd.instanceID == InstanceID);
        }
    }
}
