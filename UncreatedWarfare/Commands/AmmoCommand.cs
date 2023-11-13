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
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Commands;

public class AmmoCommand : AsyncCommand
{
    public AmmoCommand() : base("ammo", EAdminType.MEMBER, sync: true)
    {
        Structure = new CommandStructure
        {
            Description = "Refill your kit while looking at an ammo crate or ammo bag or your vehicle's trunk while in main."
        };
    }
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
            if (!Util.IsValidSteam64Id(vehicle.lockedOwner.m_SteamID))
                throw ctx.Reply(T.AmmoVehicleCantRearm);

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

                FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(vehicle.transform.position, vehicle.lockedGroup.m_SteamID.GetTeam());

                if (fob == null && !isInMain)
                    throw ctx.Reply(T.AmmoNotNearFOB);

                if (!isInMain && fob!.AmmoSupply < vehicleData.RearmCost)
                    throw ctx.Reply(T.AmmoOutOfStock, fob.AmmoSupply, vehicleData.RearmCost);

                if (vehicle.lockedGroup.m_SteamID != 0 && vehicle.lockedGroup.m_SteamID != ctx.Caller.GetTeam())
                    throw ctx.Reply(T.AmmoVehicleCantRearm);

                vehicle.TryGetComponent(out VehicleComponent? vcomp);
                if (VehicleData.IsFlyingEngine(vehicle.asset.engine) && (vehicle.isDriven ||
                    vcomp == null || Time.realtimeSinceStartup - vcomp.LastDriverTime > 1.25f) &&
                    vehicle.transform.position.y - F.GetTerrainHeightAt2DPoint(vehicle.transform.position) > 1f)
                {
                    L.LogDebug("Detected in-air vehicle.");
                    UCPlayer? lastDriver = vcomp == null ? null : UCPlayer.FromID(vcomp.LastDriver);
                    if (lastDriver != null)
                    {
                        if (lastDriver.CurrentVehicle == null)
                        {
                            Vector3 pos = lastDriver.Position;
                            if (pos.y - F.GetTerrainHeightAt2DPoint(pos) > 2f)
                            {
                                CooldownManager.StartCooldown(lastDriver, CooldownType.InteractVehicleSeats, 0.75f,
                                    vehicle);
                                L.LogDebug("Started cooldown after /ammoing outside flying vehicle.");
                            }

                            L.LogDebug("Starting passsenger cooldowns.");
                            // prevent players from switching seats
                            foreach (Passenger passenger in vehicle.passengers)
                            {
                                if (passenger.player != null && UCPlayer.FromSteamPlayer(passenger.player) is { } player)
                                    CooldownManager.StartCooldown(player, CooldownType.InteractVehicleSeats, 5f, vehicle);
                            }
                        }
                        else if (lastDriver.CurrentVehicle == vehicle)
                            throw ctx.Reply(T.AmmoInVehicle);
                    }
                }

                if (vehicleData.Items.Length == 0)
                    throw ctx.Reply(T.AmmoVehicleFullAlready);
                if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

                foreach (Guid item in vehicleData.Items)
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), ctx.Caller.Position, true, true, true);

                if (vcomp != null)
                    vcomp.ReloadCountermeasures();

                if (!isInMain)
                {
                    fob!.ModifyAmmo(-vehicleData.RearmCost);

                    FOBManager.ShowResourceToast(new LanguageSet(ctx.Caller), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(ctx.Caller));

                    if (vehicle.TryGetComponent(out VehicleComponent comp) && UCPlayer.FromID(comp.LastDriver) is { } lastDriver && lastDriver.Steam64 != ctx.CallerID)
                        FOBManager.ShowResourceToast(new LanguageSet(lastDriver), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(lastDriver));

                    ctx.Reply(T.AmmoResuppliedVehicle, vehicleData, vehicleData.RearmCost, fob.AmmoSupply);
                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE");
                }
                else
                {
                    ctx.Reply(T.AmmoResuppliedVehicleMain, vehicleData, vehicleData.RearmCost);
                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE IN MAIN");
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

            Kit? kit = ctx.Caller.GetActiveKit();
            if (kit == null)
                throw ctx.Reply(T.AmmoNoKit);

            FOBManager? fobManager = Data.Singletons.GetSingleton<FOBManager>();

            int ammoCost = KitManager.GetAmmoCost(kit.Class);

            if (Data.Gamemode.CanRefillAmmoAt(barricade.asset))
            {
                bool isInMain = false;
                bool isCache = Gamemode.Config.BarricadeInsurgencyCache.MatchGuid(barricade.asset.GUID);
                if (fobManager?.FindFob(new UCBarricade(barricade)) is not FOB fob)
                {
                    fob = null!;
                    if (!isCache)
                    {
                        if (F.IsInMain(barricade.model.transform.position))
                        {
                            isInMain = true;
                        }
                        else
                            throw ctx.Reply(T.AmmoNotNearFOB);
                    }
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(ctx.Caller, CooldownType.Ammo, out Cooldown cooldown))
                    throw ctx.Reply(T.AmmoCooldown, cooldown);

                if (!isInMain && !isCache && fob.AmmoSupply < ammoCost)
                    throw ctx.Reply(T.AmmoOutOfStock, fob.AmmoSupply, ammoCost);

                if (barricade.GetServersideData().group != ctx.Caller.GetTeam())
                    throw ctx.Reply(T.AmmoWrongTeam);

                if (!isInMain && !isCache)
                {
                    ctx.AssertNotOnPortionCooldown();
                    ctx.PortionCommandCooldownTime = 15f;
                }

                WipeDroppedItems(ctx.CallerID);
                await req.KitManager.ResupplyKit(ctx.Caller, kit!, token: token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);

                if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);

                if (isInMain)
                {
                    ctx.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(ctx.Caller, CooldownType.Ammo, FOBManager.Config.AmmoCommandCooldown);
                }
                else if (isCache)
                {
                    ctx.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM CACHE");
                }
                else
                {
                    fob.ModifyAmmo(-ammoCost);
                    FOBManager.ShowResourceToast(new LanguageSet(ctx.Caller), ammo: -ammoCost, message: T.FOBResourceToastRearmPlayer.Translate(ctx.Caller));
                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BOX");
                    ctx.Reply(T.AmmoResuppliedKit, ammoCost, fob.AmmoSupply);
                }
            }
            else if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(barricade.asset.GUID))
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (barricade.GetServersideData().group != ctx.Caller.GetTeam())
                        throw ctx.Reply(T.AmmoWrongTeam);

                    if (ammobag.Ammo < ammoCost)
                        throw ctx.Reply(T.AmmoOutOfStock, ammobag.Ammo, ammoCost);

                    await ammobag.ResupplyPlayer(ctx.Caller, kit!, ammoCost, token).ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);

                    if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                        F.TriggerEffectReliable(effect, EffectManager.SMALL, ctx.Caller.Position);

                    ctx.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BAG");

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
        if (!EventFunctions.DroppedItems.TryGetValue(player, out List<uint> instances))
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

                            Data.SendDestroyItem.Invoke(SDG.NetTransport.ENetReliability.Reliable, Regions.GatherRemoteClientConnections(x, y, ItemManager.ITEM_REGIONS), x, y, instances[i], false);
                            ItemManager.regions[x, y].items.RemoveAt(index);
                            EventFunctions.OnItemRemoved(it);
                        }
                    }
                }
            }
        }
        instances.Clear();
    }
}
