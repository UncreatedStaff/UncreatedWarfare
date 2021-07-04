using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands
{
    public class Command_ammo : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "ammo";
        public string Help => "resupplies your kit";
        public string Syntax => "/ammo";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.ammo" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (player is null)
                F.Log("PLAYER WAS NULL", ConsoleColor.DarkYellow);

            var barricade = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look);
            var storage = UCBarricadeManager.GetInteractableFromLook<InteractableStorage>(player.Player.look);
            var vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);

            if (barricade != null && FOBManager.config.data.AmmoBagIDs.Contains(barricade.barricade.id))
            {
                if (storage is null)
                {
                    player.Message("This is an ammo crate according to the server's config, but it has no storage. The admins may have messed something up."); return;
                }
                if (!(player.IsTeam1() || player.IsTeam2()))
                {
                    player.Message("Please join a team first."); return;
                }
                if ((player.IsTeam1() && !storage.items.items.Exists(j => j.item.id == FOBManager.config.data.Team1AmmoID)) || (player.IsTeam2() && !storage.items.items.Exists(j => j.item.id == FOBManager.config.data.Team2AmmoID)))
                {
                    player.Message("This ammo crate has no ammo. Fill it up with AMMO BOXES in order to resupply."); return;
                }
                if (!KitManager.HasKit(player.Steam64, out var kit))
                {
                    player.Message("You don't have a kit."); return;
                }

                KitManager.ResupplyKit(player, kit);

                player.Message("Your kit has been resupplied using <color=#d1c597>1x</color> <color=#d1c597>AMMO BOX</color>.");

                if (player.IsTeam1())
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.data.Team1AmmoID);
                else if (player.IsTeam2())
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.data.Team2AmmoID);
            }
            else if (vehicle != null)
            {
                if (!VehicleBay.VehicleExists(vehicle.id, out var vehicleData) || vehicleData.Items?.Count == 0)
                {
                    player.Message("This vehicle cannot be rearmed."); return;
                }
                if (vehicleData.Metadata != null && vehicleData.Metadata.Barricades.Count > 0)
                {
                    if (!player.Player.IsInMain())
                    {
                        player.Message("This vehicle can only be rearmed in main."); return;
                    }

                    VehicleBay.ResupplyVehicleBarricades(vehicle, vehicleData);

                    player.Message($"This vehicle has been restocked.");
                    return;
                }

                List<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>().ToList();
                List<BarricadeData> barricadeDatas = barricadeRegions.SelectMany(brd => brd.barricades).ToList();

                List<BarricadeData> NearbyAmmoStations = barricadeDatas.Where(
                    b => (b.point - vehicle.transform.position).sqrMagnitude <= Math.Pow(100, 2) &&
                    b.barricade.id == FOBManager.config.data.AmmoCrateID)
                    .OrderBy(b => (b.point - vehicle.transform.position).sqrMagnitude)
                    .ToList();

                if (NearbyAmmoStations.Count == 0)
                {
                    player.Message($"Your vehicle must be next to an AMMO CRATE in order to rearm."); return;
                }

                BarricadeData ammoStation = NearbyAmmoStations.FirstOrDefault();
                BarricadeDrop ammoStationDrop = BarricadeManager.regions.Cast<BarricadeRegion>().Concat(BarricadeManager.vehicleRegions).SelectMany(brd => brd.drops).FirstOrDefault(k => k.instanceID == ammoStation.instanceID);

                storage = ammoStationDrop.interactable as InteractableStorage;

                if (storage == null)
                {
                    player.Message($"The nearest AMMO CRATE is not a barricade with storage. This is probably because the admins have messed something up."); return;
                }

                int ammo_count = 0;

                foreach (ItemJar jar in storage.items.items)
                {
                    if (player.IsTeam1() && jar.item.id == FOBManager.config.data.Team1AmmoID)
                        ammo_count++;
                    else if (player.IsTeam2() && jar.item.id == FOBManager.config.data.Team2AmmoID)
                        ammo_count++;
                }

                if (ammo_count < vehicleData.RearmCost)
                {
                    player.Message($"<color=#FAE69C>This Ammo Crate is missing AMMO BOXES. </color><color=#d1c597>{ammo_count}/{vehicleData.RearmCost}</color>"); return;
                }
                if (vehicleData.Items.Count == 0)
                {
                    player.Message($"This vehicle does not need to be refilled."); return;
                }

                player.Message($"Your vehicle has been resupplied using <color=#d1c597>{vehicleData.RearmCost}x</color> <color=#d1c597>AMMO BOXES</color>");

                EffectManager.sendEffect((ushort)30, EffectManager.SMALL, vehicle.transform.position);

                foreach (var item in vehicleData.Items)
                {
                    ItemManager.dropItem(new Item(item, true), player.Position, true, true, true);
                }

                if (player.IsTeam1())
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.data.Team1AmmoID);
                else if (player.IsTeam2())
                    UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.data.Team2AmmoID);
            }
            else
            {
                int ammoBagsCount = 0;
                foreach (var itemID in FOBManager.config.data.AmmoBagIDs)
                {
                    ammoBagsCount += UCInventoryManager.CountItems(player.Player, itemID);
                }

                if (ammoBagsCount == 0)
                {
                    player.Message("Look at an Ammo Crate or vehicle to resupply. Or, place a rifleman's AMMO BAG in your inventory."); return;
                }
                if (!KitManager.HasKit(player.Steam64, out var kit))
                {
                    player.Message("You don't have a kit."); return;
                }

                KitManager.ResupplyKit(player, kit);

                player.Message("Your kit has been resupplied using <color=#d1c597>1x</color> <color=#d1c597>AMMO BAG</color>.");

                for (byte page = 0; page < PlayerInventory.PAGES - 1; page++)
                {
                    var pageCount = player.Player.inventory.getItemCount(page);

                    for (byte index = 0; index < pageCount; index++)
                    {
                        if (FOBManager.config.data.AmmoBagIDs.Contains(player.Player.inventory.getItem(page, index).item.id))
                        {
                            player.Player.inventory.removeItem(page, index);
                            return;
                        }
                    }
                }
            }
        }
    }
}
