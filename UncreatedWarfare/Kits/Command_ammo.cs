using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static UnityEngine.Physics;

namespace Uncreated.Warfare.Kits
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

            BarricadeData barricadeData = UCBarricadeManager.GetBarricadeDataFromLook(player);
            InteractableStorage storage = UCBarricadeManager.GetInteractableFromLook<InteractableStorage>(player.Player.look);

            if (barricadeData != null)
            {
                if (barricadeData.barricade.id != FOBManager.config.AmmoCrateID)
                {
                    player.Message("ammo_error_nocrate");
                    return;
                }
                if (!TeamManager.IsTeam1(player) && !TeamManager.IsTeam2(player))
                {
                    player.Message("Join a team first and get a kit.");
                    return;
                }
                if ((TeamManager.IsTeam1(player) && !storage.items.items.Exists(j => j.item.id == FOBManager.config.Team1AmmoID)) || 
                    (TeamManager.IsTeam2(player) && !storage.items.items.Exists(j => j.item.id == FOBManager.config.Team2AmmoID)))
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
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.Team1AmmoID);
                else if (TeamManager.IsTeam2(player))
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.Team2AmmoID);
            }
            else
            {
                InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player);

                if (vehicle != null)
                {
                    if (VehicleBay.VehicleExists(vehicle.id, out var vehicleData))
                    {
                        if (vehicleData.Metadata.Barricades.Count != 0)
                        {
                            if (!F.IsInMain(player.Player))
                            {
                                player.Message($"You must be in main to restock this {vehicle.asset.vehicleName}.");
                                return;
                            }

                            VehicleBarricadeRegion vehicleRegion = BarricadeManager.findRegionFromVehicle(vehicle);

                            ushort plant = (ushort)BarricadeManager.vehicleRegions.ToList().IndexOf(vehicleRegion);
                            for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
                            {
                                if (i >= 0)
                                {
                                    if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                                        store.despawnWhenDestroyed = true;

                                    BarricadeManager.destroyBarricade(vehicleRegion, 0, 0, plant, (ushort)i);
                                }
                            }

                            foreach (var vb in vehicleData.Metadata.Barricades)
                            {
                                Barricade barricade = new Barricade(vb.BarricadeID);
                                barricade.state = Convert.FromBase64String(vb.State);

                                Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);

                                BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                            }

                            EffectManager.sendEffect((ushort)30, EffectManager.SMALL, vehicle.transform.position);

                            player.Message($"Vehicle has been restocked and resupplied.");
                            return;
                        }

                        List<RegionCoordinate> regions = new List<RegionCoordinate>();
                        Regions.getRegionsInRadius(player.Position, 20, regions);

                        List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
                        List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

                        List<BarricadeData> NearbyAmmoStations = barricadeDatas.Where(
                            b => (b.point - vehicle.transform.position).sqrMagnitude <= Math.Pow(100, 2) &&
                            b.barricade.id == FOBManager.config.AmmoCrateID)
                            .OrderBy(b => (b.point - vehicle.transform.position).sqrMagnitude)
                            .ToList();

                        if (NearbyAmmoStations.Count == 0)
                        {
                            player.Message($"Your vehicle must be next to an Ammo Station in order to rearm.");
                            return;
                        }

                        BarricadeData ammostation = NearbyAmmoStations.FirstOrDefault();
                        BarricadeDrop ammostation_drop = BarricadeManager.regions.Cast<BarricadeRegion>().Concat(BarricadeManager.vehicleRegions).SelectMany(brd => brd.drops).FirstOrDefault(k => k.instanceID == ammostation.instanceID);

                        if (ammostation_drop.interactable is InteractableStorage)
                            storage = ammostation_drop.interactable as InteractableStorage;

                        int ammoCount = 0;

                        foreach (ItemJar jar in storage.items.items)
                        {
                            if (TeamManager.IsTeam1(player) && jar.item.id == FOBManager.config.Team1AmmoID)
                                ammoCount++;
                            else if (TeamManager.IsTeam2(player) && jar.item.id == FOBManager.config.Team2AmmoID)
                                ammoCount++;
                        }

                        //if (vehicleData.RearmCost.refill_list.items.Count == 0)
                        //{
                        //    player.Message($"This vehicle does not need to be refilled.", new HexColor("#FA8072")); return;
                        //}
                        if (ammoCount < vehicleData.RearmCost)
                        {
                            player.Message($"<color=#FAE69C>This Ammo Station is missing ammo! <color=#d1c597>Ammo crates: </color><color=#d1c597>{ammoCount}/{vehicleData.RearmCost}</color></color>"); return;
                        }

                        foreach (var itemID in vehicleData.Items)
                        {
                            ItemManager.dropItem(new Item(itemID, true), player.Position, true, true, true);
                        }

                        player.Message($"Your vehicle has been resupplied. <color=#d1c597>-{vehicleData.RearmCost} Ammo crates</color>");

                        EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

                        //VehicleBay.SpawnVehicleRefill(player.Position, vehicle.id);

                        if (TeamManager.IsTeam1(player))
                            UCBarricadeManager.RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Team1AmmoID, vehicleData.RearmCost);
                        else if (TeamManager.IsTeam2(player))
                            UCBarricadeManager.RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Team2AmmoID, vehicleData.RearmCost);
                    }
                    else
                    {
                        player.Message("vehiclebay_notrequestable");
                    }
                }
                else
                    player.Message("Look at an ammo box or vehicle to resupply.");
            }
        }
    }
}
