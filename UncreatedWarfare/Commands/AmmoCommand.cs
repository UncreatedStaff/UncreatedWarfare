using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class AmmoCommand : Command
{
    public AmmoCommand() : base("ammo", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            ctx.AssertGamemode<IVehicles>();

            if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
                throw ctx.Reply("ammo_vehicle_cant_rearm");

            if (vehicleData.Metadata != null && vehicleData.Metadata.TrunkItems != null && vehicleData.Items.Length == 0 && (vehicleData.Type == EVehicleType.LOGISTICS || vehicleData.Type == EVehicleType.HELI_TRANSPORT))
                throw ctx.Reply("ammo_auto_resupply");

            bool isInMain = F.IsInMain(vehicle.transform.position);
            if (vehicleData.Type != EVehicleType.EMPLACEMENT && !isInMain)
            {
                BarricadeDrop? repairStation = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID,
                10,
                vehicle.transform.position,
                ctx.Caller!.GetTeam(),
                false).FirstOrDefault();

                if (repairStation == null)
                    throw ctx.Reply("ammo_not_near_repair_station");
            }

            FOB? fob = FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL, vehicle.lockedGroup.m_SteamID);

            if (fob == null && !isInMain)
                throw ctx.Reply("ammo_not_near_fob");

            if (!isInMain && fob!.Ammo < vehicleData.RearmCost)
                throw ctx.Reply("ammo_not_enough_stock", fob.Ammo.ToString(Data.Locale), vehicleData.RearmCost.ToString(Data.Locale));

            if (vehicleData.Items.Length == 0)
                throw ctx.Reply("ammo_vehicle_full_already");

            EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

            foreach (Guid item in vehicleData.Items)
                if (Assets.find(item) is ItemAsset a)
                    ItemManager.dropItem(new Item(a.id, true), ctx.Caller.Position, true, true, true);

            if (!isInMain)
            {
                fob!.ReduceAmmo(vehicleData.RearmCost);
                ctx.Reply("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), fob.Ammo.ToString());
                ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR VEHICLE");
            }
            else
            {
                ctx.Reply("ammo_success_vehicle_main", vehicleData.RearmCost.ToString(Data.Locale));
                ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR VEHICLE IN MAIN");
            }
        }
        else if (ctx.TryGetTarget(out BarricadeDrop barricade))
        {
            ctx.AssertGamemode<IKitRequests>();

            if (!ctx.Caller.IsTeam1() && !ctx.Caller.IsTeam2())
                throw ctx.Reply("ammo_not_in_team");

            if (!KitManager.HasKit(ctx.Caller.Steam64, out Kit kit))
                throw ctx.Reply("ammo_no_kit");

            int ammoCost = ctx.Caller.KitClass switch
            {
                EClass.HAT or EClass.MACHINE_GUNNER or EClass.COMBAT_ENGINEER => 3,
                EClass.LAT or EClass.AUTOMATIC_RIFLEMAN or EClass.GRENADIER => 2,
                _ => 1
            };

            if (barricade.asset.GUID == Gamemode.Config.Barricades.AmmoCrateGUID || (Data.Is<Insurgency>(out _) && barricade.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID))
            {
                if (Assets.find(Gamemode.Config.Items.T1Ammo) is not ItemAsset t1ammo || Assets.find(Gamemode.Config.Items.T2Ammo) is not ItemAsset t2ammo)
                {
                    L.LogError("Either t1ammo or t2ammo guid isn't a valid item");
                    return;
                }

                bool isInMain = false;

                if (!ctx.Caller.IsOnFOB(out var fob))
                {
                    if (F.IsInMain(barricade.model.transform.position))
                        isInMain = true;
                    else
                        throw ctx.Reply("ammo_not_near_fob");
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(ctx.Caller, ECooldownType.AMMO, out Cooldown cooldown))
                    throw ctx.Reply("ammo_cooldown", cooldown.ToString());

                if (!isInMain && fob.Ammo < ammoCost)
                    throw ctx.Reply("ammo_not_enough_stock", fob.Ammo.ToString(), ammoCost.ToString());

                WipeDroppedItems(ctx.CallerID);
                KitManager.ResupplyKit(ctx.Caller, kit);

                EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);

                if (isInMain)
                {
                    ctx.Reply("ammo_success_main", ammoCost.ToString());
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(ctx.Caller, ECooldownType.AMMO, FOBManager.Config.AmmoCommandCooldown);
                }
                else
                {
                    fob.ReduceAmmo(1);
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BOX");
                    ctx.Reply("ammo_success", ammoCost.ToString(), fob.Ammo.ToString());
                }

            }
            else if (Gamemode.Config.Barricades.AmmoBagGUID == barricade.asset.GUID)
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (ammobag.Ammo < ammoCost)
                        throw ctx.Reply("ammo_not_enough_stock", ammobag.Ammo.ToString(), ammoCost.ToString());

                    ammobag.ResupplyPlayer(ctx.Caller, kit, ammoCost);

                    EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BAG");

                    WipeDroppedItems(ctx.CallerID);
                    ctx.Reply("ammo_success", ammoCost.ToString(), ammobag.Ammo.ToString());
                }
                else
                    L.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
            }
            else throw ctx.Reply("ammo_error_nocrate");
        }
        else throw ctx.Reply("ammo_error_nocrate");
    }
    internal static void WipeDroppedItems(ulong player)
    {
        if (!EventFunctions.droppeditems.TryGetValue(player, out List<uint> instances))
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
                        if (index != -1)
                        {
                            ItemData it = ItemManager.regions[x, y].items[index];
                            if (it.item.id == build1 || it.item.id == build2 || it.item.id == ammo1 || it.item.id == ammo2) continue;

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
