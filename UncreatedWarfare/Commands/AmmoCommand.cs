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
using Uncreated.Warfare.Teams;
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
                throw ctx.Reply(T.AmmoVehicleCantRearm);

            if (vehicleData.Metadata != null && vehicleData.Metadata.TrunkItems != null && vehicleData.Items.Length == 0 && (vehicleData.Type == EVehicleType.LOGISTICS || vehicleData.Type == EVehicleType.HELI_TRANSPORT))
                throw ctx.Reply(T.AmmoAutoSupply);

            bool isInMain = F.IsInMain(vehicle.transform.position);
            if (VehicleData.IsEmplacement(vehicleData.Type) && !isInMain)
            {
                BarricadeDrop? repairStation = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.BarricadeRepairStation.Value.Guid,
                10,
                vehicle.transform.position,
                ctx.Caller!.GetTeam(),
                false).FirstOrDefault();

                if (repairStation == null)
                    throw ctx.Reply(T.AmmoNotNearRepairStation);
            }

            FOB? fob = FOB.GetNearestFOB(vehicle.transform.position, EFOBRadius.FULL, vehicle.lockedGroup.m_SteamID);

            if (fob == null && !isInMain)
                throw ctx.Reply(T.AmmoNotNearFOB);

            if (!isInMain && fob!.Ammo < vehicleData.RearmCost)
                throw ctx.Reply(T.AmmoOutOfStock, fob.Ammo, vehicleData.RearmCost);

            if (vehicleData.Items.Length == 0)
                throw ctx.Reply(T.AmmoVehicleFullAlready);

            EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);

            foreach (Guid item in vehicleData.Items)
                if (Assets.find(item) is ItemAsset a)
                    ItemManager.dropItem(new Item(a.id, true), ctx.Caller.Position, true, true, true);

            if (!isInMain)
            {
                fob!.ReduceAmmo(vehicleData.RearmCost);
                ctx.Reply(T.AmmoResuppliedVehicle, vehicleData, vehicleData.RearmCost, fob.Ammo);
                ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR VEHICLE");
            }
            else
            {
                ctx.Reply(T.AmmoResuppliedVehicleMain, vehicleData, vehicleData.RearmCost);
                ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR VEHICLE IN MAIN");
            }
        }
        else if (ctx.TryGetTarget(out BarricadeDrop barricade))
        {
            ctx.AssertGamemode<IKitRequests>();

            if (!ctx.Caller.IsTeam1() && !ctx.Caller.IsTeam2())
                throw ctx.Reply(T.NotOnCaptureTeam);

            if (!KitManager.HasKit(ctx.Caller.Steam64, out Kit kit))
                throw ctx.Reply(T.AmmoNoKit);

            int ammoCost = ctx.Caller.KitClass switch
            {
                EClass.HAT or EClass.MACHINE_GUNNER or EClass.COMBAT_ENGINEER => 3,
                EClass.LAT or EClass.AUTOMATIC_RIFLEMAN or EClass.GRENADIER => 2,
                _ => 1
            };

            if (Gamemode.Config.BarricadeAmmoCrate.MatchGuid(barricade.asset.GUID) ||
                (Data.Is<Insurgency>() && Gamemode.Config.BarricadeInsurgencyCache.MatchGuid(barricade.asset.GUID)))
            {
                if (TeamManager.Team1Faction.Ammo is null || !TeamManager.Team1Faction.Ammo.Exists || TeamManager.Team2Faction.Ammo is null || !TeamManager.Team2Faction.Ammo.Exists)
                {
                    L.LogError("Either t1ammo or t2ammo guid isn't a valid item");
                    return;
                }

                bool isInMain = false;

                if (!ctx.Caller.IsOnFOB(out FOB? fob))
                {
                    if (F.IsInMain(barricade.model.transform.position))
                        isInMain = true;
                    else
                        throw ctx.Reply(T.AmmoNotNearFOB);
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(ctx.Caller, ECooldownType.AMMO, out Cooldown cooldown))
                    throw ctx.Reply(T.AmmoCooldown, cooldown);

                if (!isInMain && fob.Ammo < ammoCost)
                    throw ctx.Reply(T.AmmoOutOfStock, fob.Ammo, ammoCost);

                WipeDroppedItems(ctx.CallerID);
                KitManager.ResupplyKit(ctx.Caller, kit);

                EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);

                if (isInMain)
                {
                    ctx.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(ctx.Caller, ECooldownType.AMMO, FOBManager.Config.AmmoCommandCooldown);
                }
                else
                {
                    fob.ReduceAmmo(ammoCost);
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BOX");
                    ctx.Reply(T.AmmoResuppliedKit, ammoCost, fob.Ammo);
                }
            }
            else if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(barricade.asset.GUID))
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (ammobag.Ammo < ammoCost)
                        throw ctx.Reply(T.AmmoOutOfStock, ammobag.Ammo, ammoCost);

                    ammobag.ResupplyPlayer(ctx.Caller, kit, ammoCost);

                    EffectManager.sendEffect(30, EffectManager.SMALL, ctx.Caller.Position);
                    ctx.LogAction(EActionLogType.REQUEST_AMMO, "FOR KIT FROM BAG");

                    WipeDroppedItems(ctx.CallerID);
                    ctx.Reply(T.AmmoResuppliedKit, ammoCost, ammobag.Ammo);
                }
                else
                    L.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
            }
            else throw ctx.Reply(T.AmmoNoTarget);
        }
        else throw ctx.Reply(T.AmmoNoTarget);
    }
    internal static void WipeDroppedItems(ulong player)
    {
        if (!EventFunctions.droppeditems.TryGetValue(player, out List<uint> instances))
            return;
        ushort build1 = TeamManager.Team1Faction.Build is null || !TeamManager.Team1Faction.Build.Exists ? (ushort)0 : TeamManager.Team1Faction.Build.Id;
        ushort build2 = TeamManager.Team2Faction.Build is null || !TeamManager.Team2Faction.Build.Exists ? (ushort)0 : TeamManager.Team2Faction.Build.Id;
        ushort ammo1 = TeamManager.Team1Faction.Ammo is null || !TeamManager.Team1Faction.Ammo.Exists ? (ushort)0 : TeamManager.Team1Faction.Ammo.Id;
        ushort ammo2 = TeamManager.Team2Faction.Ammo is null || !TeamManager.Team2Faction.Ammo.Exists ? (ushort)0 : TeamManager.Team2Faction.Ammo.Id;
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

                            Data.SendDestroyItem.Invoke(SDG.NetTransport.ENetReliability.Reliable, Regions.EnumerateClients(x, y, ItemManager.ITEM_REGIONS), x, y, instances[i], false);
                            ItemManager.regions[x, y].items.RemoveAt(index);
                        }
                    }
                }
            }
        }
        instances.Clear();
    }
}
