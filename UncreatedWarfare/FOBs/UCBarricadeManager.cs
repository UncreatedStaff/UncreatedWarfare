﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Physics;

namespace Uncreated.Warfare
{
    public static class UCBarricadeManager
    {

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

        public static BarricadeData GetBarricadeFromLook(UnturnedPlayer player)
        {
            PlayerLook look = player.Player.look;

            Transform barricadeTransform = GetBarricadeTransformFromLook(look);

            if (barricadeTransform == null || !BarricadeManager.tryGetInfo(barricadeTransform, out _, out _, out _, out var index,
                out var region))
                return null;
            return region.barricades[index];
        }

        public static Transform GetBarricadeTransformFromLook(PlayerLook look)
        {
            return Physics.Raycast(look.aim.position, look.aim.forward, out var collision, 4, RayMasks.BLOCK_COLLISION) &&
                   Physics.Raycast(look.aim.position, look.aim.forward, out var hit, 4, RayMasks.BARRICADE) &&
                   collision.transform == hit.transform
                ? hit.transform
                : null;
        }
        public static T GetInteractableFromLook<T>(PlayerLook look) where T : Interactable
        {
            Transform barricadeTransform = GetBarricadeTransformFromLook(look);
            if (barricadeTransform == null) return null;
            if (barricadeTransform.TryGetComponent(out T interactable))
                return interactable;
            else return null;
        }

        public static void RemoveSingleItemFromStorage(InteractableStorage storage, ushort item_id)
        {
            for (byte i = 0; i < storage.items.items.Count; i++)
            {
                if (storage.items.getItem(i).item.id == item_id)
                {
                    storage.items.removeItem(i);
                    return;
                }
            }
        }

        public static void RemoveNumberOfItemsFromStorage(InteractableStorage storage, ushort item_id, int amount)
        {
            int counter = 0;

            for (byte i = (byte)(storage.items.getItemCount() - 1); i >= 0; i--)
            {
                if (storage.items.getItem(i).item.id == item_id)
                {
                    counter++;
                    storage.items.removeItem(i);

                    if (counter == amount)
                        return;
                }
            }
        }

        public static InteractableVehicle GetVehicleFromLook(UnturnedPlayer player)
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
