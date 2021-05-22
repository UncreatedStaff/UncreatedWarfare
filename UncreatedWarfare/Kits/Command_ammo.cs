using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.FOBs;
using UncreatedWarfare.Teams;
using UnityEngine;
using static UnityEngine.Physics;

namespace UncreatedWarfare.Kits
{
    class Command_refill : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "ammo";

        public string Help => "refills your current loadout or vehicle";

        public string Syntax => "/ammo";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>() { "uc.ammo" };
        //private BuildManager BuildManager => UCWarfare.I.BuildManager;

        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            var barricadeData = GetBarricadeFromLook(player);
            var storage = GetStorageFromLook(player);

            if (barricadeData != null)
            {
                if (barricadeData.barricade.id != Data.FOBManager.config.AmmoCrateID)
                {
                    player.Message("ammo_error_nocrate");
                    return;
                }
                if (!TeamManager.IsTeam1(player) && !TeamManager.IsTeam2(player))
                {
                    player.Message("Join a team first and get a kit.");
                    return;
                }
                if ((TeamManager.IsTeam1(player) && !storage.items.items.Exists(j => j.item.id == Data.FOBManager.config.Team1AmmoID)) || 
                    (TeamManager.IsTeam2(player) && !storage.items.items.Exists(j => j.item.id == Data.FOBManager.config.Team2AmmoID)))
                {
                    player.Message("This ammo box has no ammo. If you have logistics truck, go and fetch some more crates from main.");
                    return;
                }
                if (!KitManager.HasKit(player.CSteamID, out var kit))
                {
                    player.Message("ammo_error_nokit");
                    return;
                }

                KitManager.ResupplyKit(player, kit);

                player.Message("ammo_success");

                if (TeamManager.IsTeam1(player))
                    RemoveSingleItemFromStorage(storage, Data.FOBManager.config.Team1AmmoID);
                else if (TeamManager.IsTeam2(player))
                    RemoveSingleItemFromStorage(storage, Data.FOBManager.config.Team2AmmoID);
            }
            else
            {
                InteractableVehicle vehicle = GetVehicleFromLook(player);

                if (vehicle != null)
                {
                    //    var VehicleData = UCBarricadeManager.GetCost(vehicle.id);

                    //    VehicleBarricadeSave metadata_save = VehicleSpawner.GetBarricadeSave(vehicle.id);
                    //    if (metadata_save == null)
                    //    {
                    //        player.Message("This vehicle cannot be rearmed.", new HexColor("#FA8072")); return;
                    //    }
                    //    if (cost == null)
                    //    {
                    //        player.Message("This vehicle cannot be rearmed, but we think this is unintentional (admins might have stuffed something up).", new HexColor("#FA8072")); return;
                    //    }
                    //    if (cost.is_logi)
                    //    {
                    //        if (!(Command_join.IsInAdvancedZoneGroup(player, "usmain") || Command_join.IsInAdvancedZoneGroup(player, "rumain")))
                    //        {
                    //            player.Message($"You must be in main to restock this {vehicle.asset.vehicleName}", new HexColor("#FA8072")); return;
                    //        }

                    //        VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);

                    //        ushort plant = (ushort)BarricadeManager.vehicleRegions.ToList().IndexOf(vehicleRegion);
                    //        for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
                    //        {
                    //            if (i >= 0)
                    //            {
                    //                if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                    //                    store.despawnWhenDestroyed = true;

                    //                BarricadeManager.destroyBarricade(vehicleRegion, 0, 0, plant, (ushort)i);
                    //            }
                    //        }

                    //        foreach (VehicleBarricade vb in metadata_save.barricades)
                    //        {
                    //            Barricade barricade = new Barricade(vb.BarricadeID);
                    //            barricade.state = Convert.FromBase64String(vb.State);

                    //            Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);

                    //            BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                    //        }

                    //        EffectManager.sendEffect((ushort)30, EffectManager.SMALL, vehicle.transform.position);

                    //        player.Message($"Your vehicle has been restocked and resupplied.", new HexColor("#d6c17c"));
                    //        return;
                    //    }

                    //    List<RegionCoordinate> regions = new List<RegionCoordinate>();
                    //    Regions.getRegionsInRadius(player.Position, 20, regions);

                    //    List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
                    //    List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

                    //    List<BarricadeData> NearbyAmmoStations = barricadeDatas.Where(
                    //        b => (b.point - vehicle.transform.position).sqrMagnitude <= Math.Pow(100, 2) &&
                    //        b.barricade.id == FOBManager.config.AmmoStationID)
                    //        .OrderBy(b => (b.point - vehicle.transform.position).sqrMagnitude)
                    //        .ToList();

                    //    if (NearbyAmmoStations.Count == 0)
                    //    {
                    //        player.Message($"Your vehicle must be next to an Ammo Station in order to rearm.", new HexColor("#FA8072")); return;
                    //    }

                    //    BarricadeData ammostation = NearbyAmmoStations.FirstOrDefault();
                    //    BarricadeDrop ammostation_drop = BarricadeManager.regions.Cast<BarricadeRegion>().Concat(BarricadeManager.vehicleRegions).SelectMany(brd => brd.drops).FirstOrDefault(k => k.instanceID == ammostation.instanceID);

                    //    storage = ammostation_drop.interactable as InteractableStorage;

                    //    if (storage == null)
                    //    {
                    //        player.Message($"Something is wrong with the nearest ammo box.", new HexColor("#FA8072")); return;
                    //    }

                    //    int ammo_count = 0;

                    //    foreach (ItemJar jar in storage.items.items)
                    //    {
                    //        if (teams.IsTeam(player, ETeam.TEAM1) && jar.item.id == FOBManager.config.Team1AmmoID)
                    //            ammo_count++;
                    //        else if (teams.IsTeam(player, ETeam.TEAM2) && jar.item.id == FOBManager.config.Team2AmmoID)
                    //            ammo_count++;
                    //    }

                    //    if (ammo_count < cost.rearm_cost)
                    //    {
                    //        player.Message($"<color=#FAE69C>This Ammo Station is missing ammo! <color=#d1c597>Ammo crates: </color><color=#d1c597>{ammo_count}/{cost.rearm_cost}</color></color>"); return;
                    //    }
                    //    if (metadata_save.refill_list.items.Count == 0)
                    //    {
                    //        player.Message($"This vehicle does not need to be refilled.", new HexColor("#FA8072")); return;
                    //    }

                    //    player.Message($"Your vehicle has been resupplied. <color=#d1c597>-{cost.rearm_cost} Ammo crates</color>", new HexColor("#d6c17c"));

                    //    EffectManager.sendEffect((ushort)30, EffectManager.SMALL, vehicle.transform.position);

                    //    VehicleSpawner.SpawnVehicleRefill(player.Position, vehicle.id);

                    //    if (teams.IsTeam(player, ETeam.TEAM1))
                    //        RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Team1AmmoID, cost.rearm_cost);
                    //    else if (teams.IsTeam(player, ETeam.TEAM2))
                    //        RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Team2AmmoID, cost.rearm_cost);
                    //}
                    //else
                    //    player.Message("You must be looking at an Ammo Box or vehicle in order to refill your ammo.", new HexColor("#FA8072"));
                }
            }
        }

        public BarricadeData GetBarricadeFromLook(UnturnedPlayer player)
        {
            PlayerLook look = player.Player.look;

            Transform barricadeTransform = GetBarricadeTransformFromLook(look);

            if (barricadeTransform == null || !BarricadeManager.tryGetInfo(barricadeTransform, out _, out _, out _, out var index,
                out var region))
                return null;
            return region.barricades[index];
        }

        public Transform GetBarricadeTransformFromLook(PlayerLook look)
        {
            return Physics.Raycast(look.aim.position, look.aim.forward, out var collision, Mathf.Infinity, RayMasks.BLOCK_COLLISION) &&
                   Physics.Raycast(look.aim.position, look.aim.forward, out var hit, Mathf.Infinity, RayMasks.BARRICADE) &&
                   collision.transform == hit.transform
                ? hit.transform
                : null;
        }

        public static InteractableStorage GetStorageFromLook(UnturnedPlayer player)
        {
            Transform look = player.Player.look.aim;
            Ray ray = new Ray
            {
                direction = look.forward,
                origin = look.position
            };
            //4 units for normal reach
            if (Raycast(ray, out RaycastHit hit, 4, RayMasks.BARRICADE))
            {
                return hit.transform.GetComponent<InteractableStorage>();
            }
            else
            {
                return null;
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
    }

}
