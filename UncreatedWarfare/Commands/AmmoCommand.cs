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

namespace Uncreated.Warfare.Commands;

public class AmmoCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "ammo";
    public string Help => "resupplies your kit";
    public string Syntax => "/ammo";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.ammo" };
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CommandContext ctx = new CommandContext(caller, command);

        if (ctx.IsConsole || ctx.Caller is null)
        {
            ctx.SendPlayerOnlyError();
            return;
        }

        if (!Data.Is(out IKitRequests ctf))
        {
            ctx.SendGamemodeError();
            return;
        }
        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData vehicleData))
            {
                ctx.Reply("ammo_vehicle_cant_rearm");
                return;
            }
            if (vehicleData.Metadata != null && vehicleData.Metadata.TrunkItems != null && (vehicleData.Type == EVehicleType.LOGISTICS || vehicleData.Type == EVehicleType.HELI_TRANSPORT))
            {
                ctx.Reply("ammo_auto_resupply");
                return;
            }
            bool isInMain = F.IsInMain(vehicle.transform.position);
            if (vehicleData.Type != EVehicleType.EMPLACEMENT && !isInMain)
            {
                BarricadeDrop? repairStation = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID,
                10,
                vehicle.transform.position,
                ctx.Caller!.GetTeam(),
                false).FirstOrDefault();

                if (repairStation == null)
                {
                    ctx.Reply("ammo_not_near_repair_station");
                    return;
                }
            }
            FOB? fob = FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL, vehicle.lockedGroup.m_SteamID);


            if (fob == null)
            {
                if (!isInMain)
                {
                    ctx.Reply("ammo_not_near_fob");
                    return;
                }
            }

            if (!isInMain && fob!.Ammo < vehicleData.RearmCost)
            {
                ctx.Reply("ammo_not_enough_stock", fob.Ammo.ToString(Data.Locale), vehicleData.RearmCost.ToString(Data.Locale));
                return;
            }

            if (vehicleData.Items.Length == 0)
            {
                ctx.Reply("ammo_vehicle_full_already");
                return;
            }

            EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

            foreach (Guid item in vehicleData.Items)
                if (Assets.find(item) is ItemAsset a)
                    ItemManager.dropItem(new Item(a.id, true), ctx.Caller.Position, true, true, true);

            if (!isInMain)
            {
                fob!.ReduceAmmo(vehicleData.RearmCost);
                ctx.Reply("ammo_success_vehicle", vehicleData.RearmCost.ToString(Data.Locale), fob.Ammo.ToString());
                ActionLog.Add(EActionLogType.REQUEST_AMMO, "FOR VEHICLE", ctx.Caller.Steam64);
            }
            else
            {
                ctx.Reply("ammo_success_vehicle_main", vehicleData.RearmCost.ToString(Data.Locale));
                ActionLog.Add(EActionLogType.REQUEST_AMMO, "FOR VEHICLE IN MAIN", ctx.Caller.Steam64);
            }
        }
        else if (ctx.TryGetTarget(out BarricadeDrop barricade))
        {
            if (!ctx.Caller.IsTeam1() && !ctx.Caller.IsTeam2())
            {
                ctx.Reply("ammo_not_in_team");
                return;
            }
            if (!KitManager.HasKit(ctx.Caller.Steam64, out Kit kit))
            {
                ctx.Reply("ammo_no_kit");
                return;
            }

            int ammoCost = 1;
            if (ctx.Caller.KitClass == EClass.LAT || ctx.Caller.KitClass == EClass.AUTOMATIC_RIFLEMAN || ctx.Caller.KitClass == EClass.GRENADIER)
                ammoCost = 2;
            else if (ctx.Caller.KitClass == EClass.HAT || ctx.Caller.KitClass == EClass.MACHINE_GUNNER || ctx.Caller.KitClass == EClass.COMBAT_ENGINEER)
                ammoCost = 3;

            if (barricade.asset.GUID == Gamemode.Config.Barricades.AmmoCrateGUID || (Data.Is<Insurgency>(out _) && barricade.asset.GUID == Gamemode.Config.Barricades.InsurgencyCacheGUID))
            {
                if (!(Assets.find(Gamemode.Config.Items.T1Ammo) is ItemAsset t1ammo) || !(Assets.find(Gamemode.Config.Items.T2Ammo) is ItemAsset t2ammo))
                {
                    L.LogError("Either t1ammo or t2ammo guid isn't a valid item");
                    return;
                }

                bool isInMain = false;

                if (!ctx.Caller.IsOnFOB(out var fob))
                {
                    if (F.IsInMain(barricade.model.transform.position))
                    {
                        isInMain = true;
                    }
                    else
                    {
                        ctx.Reply("ammo_not_near_fob");
                        return;
                    }
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(ctx.Caller, ECooldownType.AMMO, out Cooldown cooldown))
                {
                    ctx.Reply("ammo_cooldown", cooldown.ToString());
                    return;
                }

                if (!isInMain && fob.Ammo < ammoCost)
                {
                    ctx.Reply("ammo_not_enough_stock", fob.Ammo.ToString(), ammoCost.ToString());
                    return;
                }

                WipeDroppedItems(ctx.CallerID);
                KitManager.ResupplyKit(ctx.Caller, kit);

                EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);

                if (isInMain)
                {
                    ctx.Reply("ammo_success_main", ammoCost.ToString());
                    ActionLog.Add(EActionLogType.REQUEST_AMMO, "FOR KIT IN MAIN", ctx.Caller.Steam64);

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(ctx.Caller, ECooldownType.AMMO, FOBManager.Config.AmmoCommandCooldown);
                }
                else
                {
                    fob.ReduceAmmo(1);
                    ActionLog.Add(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BOX", ctx.Caller.Steam64);
                    ctx.Reply("ammo_success", ammoCost.ToString(), fob.Ammo.ToString());
                }

            }
            else if (Gamemode.Config.Barricades.AmmoBagGUID == barricade.asset.GUID)
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (ammobag.Ammo < ammoCost)
                    {
                        ctx.Reply("ammo_not_enough_stock", ammobag.Ammo.ToString(), ammoCost.ToString());
                        return;
                    }

                    ammobag.ResupplyPlayer(ctx.Caller, kit, ammoCost);

                    EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);
                    ActionLog.Add(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BAG", ctx.Caller.Steam64);

                    WipeDroppedItems(ctx.CallerID);
                }
                else
                {
                    ctx.Reply("ERROR: AmmoBagComponent was not found. Please report this to the admins.");
                    L.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
                }
            }
            else
            {
                ctx.Reply("ammo_error_nocrate");
                return;
            }
        }
        else
            ctx.Reply("ammo_error_nocrate");
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
