using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
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
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.ammo" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            if (!Data.Is(out IKitRequests ctf))
            {
                player.Message("command_e_gamemode");
                return;
            }
            SDG.Unturned.BarricadeData barricade = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look, out BarricadeDrop drop);
            //InteractableStorage storage = UCBarricadeManager.GetInteractableFromLook<InteractableStorage>(player.Player.look);
            InteractableVehicle vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);


            if (vehicle != null)
            {
                if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                {
                    player.SendChat("ammo_vehicle_cant_rearm");
                    return;
                }
                if (FOBManager.config.data.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(player, ECooldownType.AMMO_VEHICLE, out Cooldown cooldown))
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

                    if (FOBManager.config.data.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(player, ECooldownType.AMMO_VEHICLE, FOBManager.config.data.AmmoCommandCooldown);
                    foreach (Guid item in vehicleData.Items)
                        if (Assets.find(item) is ItemAsset a)
                            ItemManager.dropItem(new Item(a.id, true), player.Position, true, true, true);

                    player.SendChat("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), vehicleData.RearmCost == 1 ? "" : "ES");
                    return;
                }

                var repairStation = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, 10, vehicle.transform.position, player.GetTeam(), false).FirstOrDefault();

                if (repairStation == null)
                {
                    player.SendChat("ammo_not_near_repair_station");
                    return;
                }

                var fob = FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL, vehicle.lockedGroup.m_SteamID);

                if (fob == null)
                {
                    player.SendChat("ammo_not_near_fob");
                    return;
                }

                if (fob.Ammo < vehicleData.RearmCost)
                {
                    player.SendChat("ammo_not_enough_stock", fob.Ammo.ToString(Data.Locale), vehicleData.RearmCost.ToString(Data.Locale));
                    return;
                }
                
                if (vehicleData.Items.Length == 0)
                {
                    player.SendChat("ammo_vehicle_full_already");
                    return;
                }

                player.SendChat("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), vehicleData.RearmCost == 1 ? "" : "ES");

                EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

                if (FOBManager.config.data.AmmoCommandCooldown > 0)
                    CooldownManager.StartCooldown(player, ECooldownType.AMMO_VEHICLE, FOBManager.config.data.AmmoCommandCooldown);

                foreach (Guid item in vehicleData.Items)
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), player.Position, true, true, true);

                fob.ReduceAmmo(vehicleData.RearmCost);

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
                if (barricade.barricade.asset.GUID == Gamemode.Config.Barricades.AmmoCrateGUID || (Data.Is<Insurgency>(out _) && barricade.barricade.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID))
                {
                    if (FOBManager.config.data.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(player, ECooldownType.AMMO, out Cooldown cooldown))
                    {
                        player.SendChat("ammo_cooldown", cooldown.Timeleft.TotalSeconds.ToString("N0"));
                        return;
                    }
                    if (!(Assets.find(Gamemode.Config.Items.T1Ammo) is ItemAsset t1ammo) || !(Assets.find(Gamemode.Config.Items.T2Ammo) is ItemAsset t2ammo))
                    {
                        L.LogError("Either t1ammo or t2ammo guid isn't a valid item");
                        return;
                    }

                    bool isInMain = false;

                    if (!player.IsOnFOB(out var fob))
                    {
                        if (F.IsInMain(barricade.point))
                        {
                            isInMain = true;
                        }
                        else
                        {
                            player.SendChat("ammo_not_near_fob");
                            return;
                        }   
                    }
                    if (!isInMain && fob.Ammo == 0)
                    {
                        player.SendChat("ammo_no_stock");
                        return;
                    }

                    WipeDroppedItems(player.Player.inventory);
                    KitManager.ResupplyKit(player, kit);

                    EffectManager.sendEffect(30, EffectManager.SMALL, player.Position);

                    player.SendChat("ammo_success");

                    if (isInMain)
                    {
                        if (FOBManager.config.data.AmmoCommandCooldown > 0)
                            CooldownManager.StartCooldown(player, ECooldownType.AMMO, FOBManager.config.data.AmmoCommandCooldown);
                    }
                    else
                        fob.ReduceAmmo(1);
                }
                else if (Gamemode.Config.Barricades.AmmoBagGUID == barricade.barricade.asset.GUID)
                {
                    if (drop.model.TryGetComponent(out AmmoBagComponent ammobag))
                    {
                        if (ammobag.ResuppliedPlayers.TryGetValue(player.Steam64, out int lifeIndex) && lifeIndex == player.LifeCounter)
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
            ushort build1 = Assets.find(Gamemode.Config.Items.T1Build)?.id ?? 0;
            ushort build2 = Assets.find(Gamemode.Config.Items.T2Build)?.id ?? 0;
            ushort ammo1 = Assets.find(Gamemode.Config.Items.T1Ammo)?.id ?? 0;
            ushort ammo2 = Assets.find(Gamemode.Config.Items.T2Ammo)?.id ?? 0;
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
                            SDG.Unturned.ItemData it = ItemManager.regions[x, y].items[index];
                            if (it.item.id == build1 || it.item.id == build2 || it.item.id == ammo1 || it.item.id == ammo2) continue;
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
