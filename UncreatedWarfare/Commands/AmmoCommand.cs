using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
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

            BarricadeData barricade = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look, out BarricadeDrop drop);
            //InteractableStorage storage = UCBarricadeManager.GetInteractableFromLook<InteractableStorage>(player.Player.look);
            InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);

            
            if (vehicle != null)
            {
                if (!VehicleBay.VehicleExists(vehicle.id, out VehicleData vehicleData))
                {
                    player.SendChat("ammo_vehicle_cant_rearm"); 
                    return;
                }
                if (FOBManager.config.Data.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(player, ECooldownType.AMMO_VEHICLE, out Cooldown cooldown))
                {
                    player.SendChat("ammo_vehicle_cooldown", cooldown.Timeleft.TotalSeconds.ToString("N0"));
                    return;
                }
                if (vehicleData.Metadata != null && vehicleData.Metadata.Barricades.Count > 0)
                {
                    if (!player.Player.IsInMain())
                    {
                        player.SendChat("ammo_vehicle_out_of_main"); 
                        return;
                    }

                    VehicleBay.ResupplyVehicleBarricades(vehicle, vehicleData);

                    if (FOBManager.config.Data.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(player, ECooldownType.AMMO_VEHICLE, FOBManager.config.Data.AmmoCommandCooldown);
                    foreach (ushort item in vehicleData.Items)
                        ItemManager.dropItem(new Item(item, true), player.Position, true, true, true);

                    player.SendChat("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), vehicleData.RearmCost == 1 ? "" : "ES");
                    return;
                }

                IEnumerable<BarricadeDrop> NearbyAmmoStations = UCBarricadeManager.GetNearbyBarricades(FOBManager.config.Data.AmmoCrateID, 100, vehicle.transform.position, player.GetTeam(), true);

                if (NearbyAmmoStations.Count() == 0)
                {
                    player.SendChat("ammo_vehicle_not_near_ammo_crate"); 
                    return;
                }

                BarricadeDrop ammoStation = NearbyAmmoStations.FirstOrDefault();

                if (!(ammoStation.interactable is InteractableStorage storage))
                {
                    player.SendChat("ammo_crate_has_no_storage"); 
                    return;
                }

                int ammo_count = 0;
                ulong team = player.GetTeam();
                foreach (ItemJar jar in storage.items.items)
                {
                    if (team == 1 && jar.item.id == FOBManager.config.Data.Team1AmmoID || team == 2 && jar.item.id == FOBManager.config.Data.Team2AmmoID)
                        ammo_count++;
                }

                if (ammo_count < vehicleData.RearmCost)
                {
                    player.SendChat("ammo_not_enough_stock", ammo_count.ToString(Data.Locale), vehicleData.RearmCost.ToString(Data.Locale));
                    return;
                }
                if (vehicleData.Items.Count == 0)
                {
                    player.SendChat("ammo_vehicle_full_already");
                    return;
                }

                player.SendChat("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), vehicleData.RearmCost == 1 ? "" : "ES");

                EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

                if (FOBManager.config.Data.AmmoCommandCooldown > 0)
                    CooldownManager.StartCooldown(player, ECooldownType.AMMO_VEHICLE, FOBManager.config.Data.AmmoCommandCooldown);

                foreach (ushort item in vehicleData.Items)
                    ItemManager.dropItem(new Item(item, true), player.Position, true, true, true);

                if (player.IsTeam1())
                    UCBarricadeManager.RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Data.Team1AmmoID, vehicleData.RearmCost);
                else if (player.IsTeam2())
                    UCBarricadeManager.RemoveNumberOfItemsFromStorage(storage, FOBManager.config.Data.Team2AmmoID, vehicleData.RearmCost);

                return;
            }
            else if (barricade != null && drop != null && barricade.barricade != null)
            {
                if (!player.IsTeam1() && !player.IsTeam2())
                {
                    player.SendChat("ammo_not_in_team");
                    return;
                }
                if (!KitManager.HasKit(player.Steam64, out Kit kit))
                {
                    player.SendChat("ammo_no_kit");
                    return;
                }
                if (FOBManager.config.Data.AmmoCrateID == barricade.barricade.id)
                {
                    if (!(drop.interactable is InteractableStorage storage))
                    {
                        player.SendChat("ammo_crate_has_no_storage");
                        return;
                    }
                    if (FOBManager.config.Data.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(player, ECooldownType.AMMO, out Cooldown cooldown))
                    {
                        player.SendChat("ammo_cooldown", cooldown.Timeleft.TotalSeconds.ToString("N0"));
                        return;
                    }
                    if ((player.IsTeam1() && !storage.items.items.Exists(j => j.item.id == FOBManager.config.Data.Team1AmmoID)) ||
                    (player.IsTeam2() && !storage.items.items.Exists(j => j.item.id == FOBManager.config.Data.Team2AmmoID)))
                    {
                        player.SendChat("ammo_no_stock");
                        return;
                    }

                    WipeDroppedItems(player.Player.inventory);
                    KitManager.ResupplyKit(player, kit);

                    EffectManager.sendEffect(30, EffectManager.SMALL, player.Position);

                    player.SendChat("ammo_success");

                    if (FOBManager.config.Data.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(player, ECooldownType.AMMO, FOBManager.config.Data.AmmoCommandCooldown);
                    if (player.IsTeam1())
                        UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.Data.Team1AmmoID);
                    else if (player.IsTeam2())
                        UCBarricadeManager.RemoveSingleItemFromStorage(storage, FOBManager.config.Data.Team2AmmoID);
                }
                else if (FOBManager.config.Data.AmmoBagIDs.Contains(barricade.barricade.id))
                {
                    if (drop.model.TryGetComponent<AmmoBagComponent>(out var ammobag))
                    {
                        if (ammobag.ResuppliedPlayers.TryGetValue(player.Steam64, out var lifeIndex) && lifeIndex == player.LifeCounter)
                        {
                            player.Message("ammo_bag_already_resupplied");
                            return;
                        }

                        ammobag.ResupplyPlayer(player, kit);

                        EffectManager.sendEffect(30, EffectManager.SMALL, player.Position);

                        WipeDroppedItems(player.Player.inventory);
                    }
                    else
                    {
                        player.SendChat("ERROR: AmmoBagComponent was not found. Please report this to the admins.");
                        CommandWindow.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
                    }
                }
                else
                {
                    player.SendChat("ammo_error_nocrate");
                    return;
                }
            }
            else
            {
                player.SendChat("ammo_error_nocrate");
            }
        }
        internal static void WipeDroppedItems(PlayerInventory player)
        {
            if (!EventFunctions.droppeditems.TryGetValue(player.player.channel.owner.playerID.steamID.m_SteamID, out List<uint> instances))
                return;
            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    if (Regions.checkSafe(x, y))
                    {
                        ItemRegion region = ItemManager.regions[x, y];
                        for (int i = 0; i < instances.Count; i++)
                        {
                            int index = region.items.FindIndex(r => r.instanceID == instances[i]);
                            if (index != -1)
                            {
                                Data.SendTakeItem.Invoke(SDG.NetTransport.ENetReliability.Reliable, Regions.EnumerateClients(x, y, ItemManager.ITEM_REGIONS), x, y, instances[i]);
                                ItemManager.regions[x, y].items.RemoveAt(index);
                            }
                        }
                    }
                }
            }
            instances.Clear();
        }
    }
}
