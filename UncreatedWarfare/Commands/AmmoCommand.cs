using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("ammo")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class AmmoCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Refill your kit while looking at an ammo crate or ammo bag or your vehicle's trunk while in main."
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay == null || !bay.IsLoaded)
            throw Context.SendGamemodeError();

        if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            if (vehicle.lockedOwner.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                throw Context.Reply(T.AmmoVehicleCantRearm);

            Context.AssertGamemode<IVehicles>();

            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (data?.Item == null)
                throw Context.Reply(T.AmmoVehicleCantRearm);
            await data.Enter(token).ConfigureAwait(false);
            try
            {
                await UniTask.SwitchToMainThread(token);
                VehicleData vehicleData = data.Item;
                if (vehicleData.Metadata is { TrunkItems: not null } && vehicleData.Items.Length == 0 && vehicleData.Type is VehicleType.LogisticsGround or VehicleType.TransportAir)
                {
                    throw Context.Reply(T.AmmoAutoSupply);
                }

                bool isInMain = F.IsInMain(vehicle.transform.position);

                if (!Gamemode.Config.BarricadeRepairStation.TryGetGuid(out Guid repairGuid))
                {
                    Context.ReplyString("No asset for repair station. Contact admin.");
                    throw Context.Reply(T.AmmoNotNearRepairStation);
                }

                if (!VehicleData.IsEmplacement(vehicleData.Type) && !isInMain)
                {
                    if (!BarricadeUtility.IsBarricadeInRange(
                            vehicle.transform.position,
                            10f,
                            Context.Player.UnturnedPlayer.quests.groupID.m_SteamID,
                            Gamemode.Config.BarricadeRepairStation,
                            horizontalDistanceOnly: false
                        ))
                    {
                        throw Context.Reply(T.AmmoNotNearRepairStation);
                    }
                }

                FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(vehicle.transform.position, vehicle.lockedGroup.m_SteamID.GetTeam());

                if (fob == null && !isInMain)
                    throw Context.Reply(T.AmmoNotNearFOB);

                if (!isInMain && fob!.AmmoSupply < vehicleData.RearmCost)
                    throw Context.Reply(T.AmmoOutOfStock, fob.AmmoSupply, vehicleData.RearmCost);

                if (vehicle.lockedGroup.m_SteamID != 0 && vehicle.lockedGroup.m_SteamID != Context.Player.GetTeam())
                    throw Context.Reply(T.AmmoVehicleCantRearm);

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
                            throw Context.Reply(T.AmmoInVehicle);
                    }
                }

                if (vehicleData.Items.Length == 0)
                    throw Context.Reply(T.AmmoVehicleFullAlready);
                if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);

                foreach (Guid item in vehicleData.Items)
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), Context.Player.Position, true, true, true);

                if (vcomp != null)
                    vcomp.ReloadCountermeasures();

                if (!isInMain)
                {
                    fob!.ModifyAmmo(-vehicleData.RearmCost);

                    FOBManager.ShowResourceToast(new LanguageSet(Context.Player), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(Context.Player));

                    if (vehicle.TryGetComponent(out VehicleComponent comp) && UCPlayer.FromID(comp.LastDriver) is { } lastDriver && lastDriver.Steam64 != Context.CallerId.m_SteamID)
                        FOBManager.ShowResourceToast(new LanguageSet(lastDriver), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(lastDriver));

                    Context.Reply(T.AmmoResuppliedVehicle, vehicleData, vehicleData.RearmCost, fob.AmmoSupply);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE");
                }
                else
                {
                    Context.Reply(T.AmmoResuppliedVehicleMain, vehicleData, vehicleData.RearmCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE IN MAIN");
                }
            }
            finally
            {
                data.Release();
            }
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            Context.AssertGamemode(out IKitRequests req);

            if (!Context.Player.IsOnTeam)
                throw Context.Reply(T.NotOnCaptureTeam);

            Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);
            if (kit == null)
                throw Context.Reply(T.AmmoNoKit);

            await UniTask.SwitchToMainThread(token);

            FOBManager? fobManager = Data.Singletons.GetSingleton<FOBManager>();

            int ammoCost = KitDefaults<WarfareDbContext>.GetAmmoCost(kit.Class);

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
                            throw Context.Reply(T.AmmoNotNearFOB);
                    }
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(Context.Player, CooldownType.Ammo, out Cooldown cooldown))
                    throw Context.Reply(T.AmmoCooldown, cooldown);

                if (!isInMain && !isCache && fob.AmmoSupply < ammoCost)
                    throw Context.Reply(T.AmmoOutOfStock, fob.AmmoSupply, ammoCost);

                if (barricade.GetServersideData().group != Context.Player.GetTeam())
                    throw Context.Reply(T.AmmoWrongTeam);

                if (!isInMain && !isCache)
                {
                    Context.AssertCommandNotOnIsolatedCooldown();
                    Context.IsolatedCommandCooldownTime = 15f;
                }

                WipeDroppedItems(Context.CallerId.m_SteamID);
                await req.KitManager.Requests.ResupplyKit(Context.Player, kit, token: token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                    F.TriggerEffectReliable(effect, EffectManager.SMALL, Context.Player.Position);

                if (isInMain)
                {
                    Context.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(Context.Player, CooldownType.Ammo, FOBManager.Config.AmmoCommandCooldown);
                }
                else if (isCache)
                {
                    Context.Reply(T.AmmoResuppliedKitMain, ammoCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM CACHE");
                }
                else
                {
                    fob.ModifyAmmo(-ammoCost);
                    FOBManager.ShowResourceToast(new LanguageSet(Context.Player), ammo: -ammoCost, message: T.FOBResourceToastRearmPlayer.Translate(Context.Player));
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BOX");
                    Context.Reply(T.AmmoResuppliedKit, ammoCost, fob.AmmoSupply);
                }
            }
            else if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(barricade.asset.GUID))
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (barricade.GetServersideData().group != Context.Player.GetTeam())
                        throw Context.Reply(T.AmmoWrongTeam);

                    if (ammobag.Ammo < ammoCost)
                        throw Context.Reply(T.AmmoOutOfStock, ammobag.Ammo, ammoCost);

                    await ammobag.ResupplyPlayer(Context.Player, kit, ammoCost, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);

                    if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                        F.TriggerEffectReliable(effect, EffectManager.SMALL, Context.Player.Position);

                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BAG");

                    WipeDroppedItems(Context.CallerId.m_SteamID);
                    Context.Reply(T.AmmoResuppliedKit, ammoCost, ammobag.Ammo);
                }
                else
                    L.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
            }
            else throw Context.Reply(T.AmmoNoTarget);
        }
        else throw Context.Reply(T.AmmoNoTarget);
    }
    internal static void WipeDroppedItems(ulong player)
    {
        ThreadUtil.assertIsGameThread();
        if (!EventFunctions.DroppedItems.TryGetValue(player, out List<uint> instances))
            return;
        TeamManager.Team1Faction.Build.TryGetId(out ushort build1);
        TeamManager.Team2Faction.Build.TryGetId(out ushort build2);
        TeamManager.Team1Faction.Ammo.TryGetId(out ushort ammo1);
        TeamManager.Team2Faction.Ammo.TryGetId(out ushort ammo2);
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
