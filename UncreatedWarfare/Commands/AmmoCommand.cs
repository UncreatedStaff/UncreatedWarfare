using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

public class AmmoCommand : AsyncCommand
{
    public AmmoCommand() : base("ammo", EAdminType.MEMBER) { }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay == null || !bay.IsLoaded)
            throw ctx.SendGamemodeError();

        if (ctx.TryGetTarget(out InteractableVehicle vehicle))
        {
            ctx.AssertGamemode<IVehicles>();

            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (data?.Item == null)
                throw ctx.Reply(T.AmmoVehicleCantRearm);
            await data.Enter(token).ConfigureAwait(false);
            try
            {
                await UCWarfare.ToUpdate();
                VehicleData vehicleData = data.Item;
                if (vehicleData.Metadata != null && vehicleData.Metadata.TrunkItems != null && vehicleData.Items.Length == 0 && (vehicleData.Type == VehicleType.LogisticsGround || vehicleData.Type == VehicleType.TransportAir))
                    throw ctx.Reply(T.AmmoAutoSupply);

                bool isInMain = F.IsInMain(vehicle.transform.position);
                if (!VehicleData.IsEmplacement(vehicleData.Type) && !isInMain)
                {
                    BarricadeDrop? repairStation = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.BarricadeRepairStation.Value.Guid,
                    10,
                    vehicle.transform.position,
                    ctx.Caller.GetTeam(),
                    false).FirstOrDefault();

                    if (repairStation == null)
                        throw ctx.Reply(T.AmmoNotNearRepairStation);
                }

                FOB? fob = FOB.GetNearestFOB(vehicle.transform.position, EfobRadius.FULL, vehicle.lockedGroup.m_SteamID);

                if (fob == null && !isInMain)
                    throw ctx.Reply(T.AmmoNotNearFOB);

                if (!isInMain && fob!.Ammo < vehicleData.RearmCost)
                    throw ctx.Reply(T.AmmoOutOfStock, fob.Ammo, vehicleData.RearmCost);

                if (vehicleData.Items.Length == 0)
                    throw ctx.Reply(T.AmmoVehicleFullAlready);
                if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

                foreach (Guid item in vehicleData.Items)
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), ctx.Caller.Position, true, true, true);

                if (!isInMain)
                {
                    fob!.ReduceAmmo(vehicleData.RearmCost);
                    ctx.Reply(T.AmmoResuppliedVehicle, vehicleData, vehicleData.RearmCost, fob.Ammo);
                    ctx.LogAction(ActionLogType.REQUEST_AMMO, "FOR VEHICLE");
                }
                else
                {
                    ctx.Reply(T.AmmoResuppliedVehicleMain, vehicleData, vehicleData.RearmCost);
                    ctx.LogAction(ActionLogType.REQUEST_AMMO, "FOR VEHICLE IN MAIN");
                }
            }
            finally
            {
                data.Release();
            }
        }
        else if (ctx.TryGetTarget(out BarricadeDrop barricade))
        {
            ctx.AssertGamemode(out IKitRequests req);

            if (!ctx.Caller.IsOnTeam)
                throw ctx.Reply(T.NotOnCaptureTeam);

            SqlItem<Kit>? kit = ctx.Caller.ActiveKit;
            Kit? kit2 = kit?.Item;
            if (kit2 == null)
                throw ctx.Reply(T.AmmoNoKit);

            int ammoCost = KitManager.GetAmmoCost(kit2.Class);

            if (Data.Gamemode.CanRefillAmmoAt(barricade.asset))
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
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(ctx.Caller, CooldownType.Ammo, out Cooldown cooldown))
                    throw ctx.Reply(T.AmmoCooldown, cooldown);

                if (!isInMain && fob.Ammo < ammoCost)
                    throw ctx.Reply(T.AmmoOutOfStock, fob.Ammo, ammoCost);

                WipeDroppedItems(ctx.CallerID);
                await req.KitManager.ResupplyKit(ctx.Caller, kit!, token: token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);

                if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);

                if (isInMain)
                {
                    ctx.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    ctx.LogAction(ActionLogType.REQUEST_AMMO, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(ctx.Caller, CooldownType.Ammo, FOBManager.Config.AmmoCommandCooldown);
                }
                else
                {
                    fob.ReduceAmmo(ammoCost);
                    ctx.LogAction(ActionLogType.REQUEST_AMMO, "FOR KIT FROM BOX");
                    ctx.Reply(T.AmmoResuppliedKit, ammoCost, fob.Ammo);
                }
            }
            else if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(barricade.asset.GUID))
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (ammobag.Ammo < ammoCost)
                        throw ctx.Reply(T.AmmoOutOfStock, ammobag.Ammo, ammoCost);

                    await ammobag.ResupplyPlayer(ctx.Caller, kit!, ammoCost, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);

                    if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                        F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);

                    ctx.LogAction(ActionLogType.REQUEST_AMMO, "FOR KIT FROM BAG");

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
        ThreadUtil.assertIsGameThread();
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
