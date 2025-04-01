using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles;

[Priority(-3 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
public class VehicleService : 
    ILayoutHostedService,
    IEventListener<VehicleDespawned>
{
    private readonly List<WarfareVehicle> _vehicles;

    private static readonly ClientStaticMethod<uint, byte, byte>? SendSwapVehicleSeats
        = ReflectionUtility.FindRpc<VehicleManager, ClientStaticMethod<uint, byte, byte>>("SendSwapVehicleSeats");

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VehicleService> _logger;
    private readonly VehicleInfoStore _vehicleInfoStore;
    private readonly VehicleSpawnerStore _vehicleSpawnerStore;
    private readonly VehicleSeatRestrictionService _seatRestrictions;
    private EventDispatcher? _eventDispatcher;

    public const float VehicleSpawnOffset = 5f;
    public const ushort MaxBatteryCharge = 10000;

    public IReadOnlyList<WarfareVehicle> Vehicles { get; }

    public VehicleService(IServiceProvider serviceProvider,
        ILogger<VehicleService> logger,
        VehicleInfoStore vehicleInfoStore,
        VehicleSpawnerStore vehicleSpawnerStore,
        VehicleSeatRestrictionService vehicleSeatRestrictionService)
    {
        _vehicles = new List<WarfareVehicle>(64);
        Vehicles = _vehicles.AsReadOnly();

        _logger = logger;
        _serviceProvider = serviceProvider;
        _vehicleInfoStore = vehicleInfoStore;
        _vehicleSpawnerStore = vehicleSpawnerStore;
        _seatRestrictions = vehicleSeatRestrictionService;
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        await DeleteAllVehiclesAsync(token);

        await UniTask.SwitchToMainThread(token);

        _vehicleSpawnerStore.ReloadSpawners();
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public async UniTask<bool> TryMovePlayerToEmptySeat(WarfarePlayer player)
    {
        if (SendSwapVehicleSeats == null)
        {
            return false;
        }

        await UniTask.SwitchToMainThread();

        InteractableVehicle vehicle = player.UnturnedPlayer.movement.getVehicle();
        WarfareVehicle warfareVehicle = GetVehicle(vehicle);

        if (vehicle == null)
        {
            return false;
        }

        // keep running in case a player happens to move while the event is dispatching or something
        do
        {
            byte fromSeat = player.UnturnedPlayer.movement.getSeat();

            int freeSeat = -1;
            VehicleChangeSeatsResult swapResult;

            for (int i = 0; i < vehicle.passengers.Length; ++i)
            {
                if (vehicle.passengers[i].player != null)
                    continue;

                swapResult = _seatRestrictions.TrySwapSeat(warfareVehicle, player, (byte)i, fromSeat);
                if (swapResult.Result != ChangeSeatsResult.Success)
                    continue;

                freeSeat = i;
                break;
            }

            if (freeSeat < 0 || freeSeat >= vehicle.passengers.Length)
            {
                return false;
            }

            VehicleSwapSeatRequested swapSeatRequest = new VehicleSwapSeatRequested
            {
                Player = player,
                Vehicle = warfareVehicle,
                NewPassengerIndex = freeSeat,
                OldPassengerIndex = fromSeat,
                IgnoreInteractCooldown = true
            };


            // event dispatcher circular reference
            _eventDispatcher ??= _serviceProvider.GetRequiredService<EventDispatcher>();
            bool canContinue = await _eventDispatcher.DispatchEventAsync(swapSeatRequest, player.DisconnectToken);
            if (!canContinue)
                return false;

            await UniTask.SwitchToMainThread(player.DisconnectToken);

            if (player.UnturnedPlayer.movement.getSeat() != fromSeat)
                continue;

            // recheck swap to see if it's still valid in case a player moved
            swapResult = _seatRestrictions.TrySwapSeat(warfareVehicle, player, (byte)freeSeat, fromSeat);
            if (swapResult.Result != ChangeSeatsResult.Success)
                continue;

            SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, fromSeat, (byte)freeSeat);

            return player.Equals(vehicle.passengers[freeSeat].player);
        }
        while (vehicle == player.UnturnedPlayer.movement.getVehicle());

        throw new OperationCanceledException("Vehicle changed");
    }

    public WarfareVehicle RegisterWarfareVehicle(InteractableVehicle vehicle)
    {
        WarfareVehicle warfareVehicle;
        lock (_vehicles)
        {
            uint instId = vehicle.instanceID;
            for (int i = 0; i < _vehicles.Count; ++i)
            {
                if (_vehicles[i].InstanceId == instId)
                    return _vehicles[i];
            }

            WarfareVehicleInfo info = _vehicleInfoStore.GetVehicleInfo(vehicle.asset) ?? WarfareVehicleInfo.CreateDefault(vehicle.asset);

            warfareVehicle = new WarfareVehicle(vehicle, info, _serviceProvider);
            _vehicles.Add(warfareVehicle);
        }

        _logger.LogDebug($"Registered vehicle: {warfareVehicle.Info.VehicleAsset.ToDisplayString()}");
        return warfareVehicle;
    }

    public WarfareVehicle? DeregisterWarfareVehicle(InteractableVehicle vehicle)
    {
        lock (_vehicles)
        {
            uint instId = vehicle.instanceID;
            for (int i = 0; i < _vehicles.Count; ++i)
            {
                WarfareVehicle info = _vehicles[i];
                if (info.InstanceId != instId)
                    continue;

                _vehicles.RemoveAt(i);
                info.Dispose();
                return info;
            }
        }

        return null;
    }

    public WarfareVehicle GetVehicle(InteractableVehicle vehicle)
    {
        if (GameThread.IsCurrent)
            return GetVehicleLocked(vehicle);

        lock (_vehicles)
        {
            // auto register if it wasn't for some reason
            return GetVehicleLocked(vehicle);
        }
    }

    private WarfareVehicle GetVehicleLocked(InteractableVehicle vehicle)
    {
        uint instId = vehicle.instanceID;
        // ReSharper disable InconsistentlySynchronizedField
        for (int i = 0; i < _vehicles.Count; ++i)
        {
            if (_vehicles[i].InstanceId == instId)
                return _vehicles[i];
        }

        // ReSharper restore InconsistentlySynchronizedField
        return RegisterWarfareVehicle(vehicle);
    }

    /// <summary>
    /// Spawn a vehicle at a given vehicle spawner.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AssetNotFoundException">Unable to find the vehicle asset.</exception>
    /// <exception cref="NotSupportedException">There's already a linked vehicle to the spawner.</exception>
    /// <exception cref="RecordsNotFoundException">Unable to find any <see cref="WarfareVehicleInfo"/> for the vehicle.</exception>
    /// <exception cref="InvalidOperationException">The spawner buildable doesn't exist. -OR- Failed to unlink or link the vehicle to it's spawn.</exception>
    /// <exception cref="Exception">Game failed to spawn the vehicle.</exception>
    public async UniTask<WarfareVehicle> SpawnVehicleAsync(VehicleSpawner spawner, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (spawner.LinkedVehicle != null && spawner.LinkedVehicle.isDead && !spawner.LinkedVehicle.isExploded)
        {
            throw new NotSupportedException($"There can only be one vehicle per spawn, and this spawn already has a vehicle: {spawner.ToDisplayString()}.");
        }

        spawner.UnlinkVehicle();

        if (spawner.Buildable == null || spawner.Buildable.IsDead)
        {
            throw new InvalidOperationException("Spawner buildable no longer exists.");
        }

        Quaternion spawnRotation = spawner.Buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

        Vector3 spawnPosition = spawner.Buildable.Position + Vector3.up * VehicleSpawnOffset;

        WarfareVehicle warfareVehicle = await SpawnVehicleAsync(spawner.VehicleInfo.VehicleAsset, spawnPosition, spawnRotation, paintColor: spawner.VehicleInfo.PaintColor, token: token);
        await UniTask.SwitchToMainThread(token);

        spawner.LinkVehicle(warfareVehicle.Vehicle);

        _logger.LogDebug("Spawned new {0} at {1}.", spawner.VehicleInfo.VehicleAsset.ToDisplayString(), spawnPosition);
        return warfareVehicle;
    }

    /// <summary>
    /// Spawn a vehicle with the given information.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AssetNotFoundException">Unable to find the vehicle asset.</exception>
    /// <exception cref="Exception">Game failed to spawn the vehicle.</exception>
    public async UniTask<WarfareVehicle> SpawnVehicleAsync(
        IAssetLink<VehicleAsset> vehicle,
        Vector3 position,
        Quaternion rotation,
        CSteamID owner = default,
        CSteamID group = default,
        Color32 paintColor = default,
        bool locked = true,
        CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        VehicleAsset asset = vehicle.GetAssetOrFail(nameof(vehicle));

        byte[][] turrets = new byte[asset.turrets.Length][];

        for (int i = 0; i < asset.turrets.Length; ++i)
        {
            // todo april fools should add dootpressor but i couldn't get it to work on s3
            if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemGunAsset turret)
            {
                turrets[i] = turret.getState(EItemOrigin.ADMIN);
            }
        }

        InteractableVehicle? veh = VehicleManager.SpawnVehicleV3(asset, 0, 0, 0f, position, rotation, false, false, false, false,
            asset.fuel, asset.health, MaxBatteryCharge, owner, group, locked, turrets,
            byte.MaxValue, paintColor: paintColor);
        if (veh == null)
            throw new Exception($"Failed to spawn vehicle {vehicle.ToDisplayString()} due to vanilla code, possible a misconfigured vehicle.");

        WarfareVehicle? warfareVehicle = GetVehicle(veh);
        if (warfareVehicle == null)
            throw new Exception($"Vehicle {vehicle.ToDisplayString()} was spawned successfully, but is not registered in the VehicleService.");

        if (warfareVehicle.Info is not { Trunk.Count: > 0 })
            return warfareVehicle;

        RefillTrunkItems(warfareVehicle, warfareVehicle.Info.Trunk);

        return warfareVehicle;
    }

    public void RefillTrunkItems(WarfareVehicle warfareVehicle, IReadOnlyCollection<WarfareVehicleInfo.TrunkItem> trunkItems, bool dropItemsOnGroundIfNoSpace = true)
    {
        Items? items = warfareVehicle.Vehicle.trunkItems;
        if (items == null || trunkItems.Count == 0)
            return;

        for (int i = items.getItemCount(); i > 0; --i)
        {
            items.removeItem(0);
        }

        foreach (WarfareVehicleInfo.TrunkItem item in trunkItems)
        {
            if (!item.Item.TryGetAsset(out ItemAsset? itemAsset))
            {
                _logger.LogWarning("Failed to find item asset for the trunk of {0}: {1}.", warfareVehicle.Vehicle.asset.FriendlyName, item.Item.ToDisplayString());
                continue;
            }

            Item info = new Item(itemAsset.id, itemAsset.amount, 100, item.State ?? itemAsset.getState(EItemOrigin.ADMIN));
            if (items.checkSpaceEmpty(item.X, item.Y, itemAsset.size_x, itemAsset.size_y, item.Rotation))
            {
                items.addItem(item.X, item.Y, item.Rotation, info);
            }
            else if (!items.tryAddItem(info) && dropItemsOnGroundIfNoSpace)
            {
                ItemManager.dropItem(info, warfareVehicle.Position, false, true, true);
            }
        }
    }
    /// <summary>
    /// Remove one vehicle and clean up spawn information and items.
    /// </summary>
    public async UniTask DeleteVehicleAsync(InteractableVehicle vehicle, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        PrepareToDeleteVehicle(vehicle);
        VehicleManager.askVehicleDestroy(vehicle);
    }
    /// <summary>
    /// Remove all vehicles and clean up spawn information and items.
    /// </summary>
    public async UniTask<int> DeleteAllVehiclesAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int count = VehicleManager.vehicles.Count;
        for (int i = 0; i < count; i++)
        {
            PrepareToDeleteVehicle(VehicleManager.vehicles[i]);
        }

        VehicleManager.askVehicleDestroyAll();

        return count;
    }

    private void PrepareToDeleteVehicle(InteractableVehicle vehicle)
    {
        // keep storage items from dropping on destroy
        BarricadeRegion region = BarricadeManager.getRegionFromVehicle(vehicle);
        if (region != null)
        {
            for (int b = 0; b < region.drops.Count; b++)
            {
                BarricadeUtility.PreventItemDrops(region.drops[b]);
            }
        }

        // empty trunk so items don't drop
        if (vehicle.trunkItems != null)
        {
            for (int i = vehicle.trunkItems.getItemCount() - 1; i >= 0; --i)
                vehicle.trunkItems.removeItem((byte)i);
        }

        // remove all players and teleport them to the ground so they don't take fall damage
        for (int i = 0; i < vehicle.passengers.Length; ++i)
        {
            SteamPlayer? pl = vehicle.passengers[i].player;
            if (pl == null)
                continue;

            VehicleManager.forceRemovePlayer(vehicle, pl.playerID.steamID);

            if (vehicle.asset.engine is not EEngine.BLIMP and not EEngine.HELICOPTER and not EEngine.PLANE)
                continue;

            Vector3 p = pl.player.transform.position;
            float y = TerrainUtility.GetHighestPoint(in p, 1f);
            if (Mathf.Abs(p.y - y) > 5f)
            {
                p.y = y;
                pl.player.teleportToLocationUnsafe(p, pl.player.look.aim.transform.rotation.eulerAngles.y);
            }
        }

        // unlink vehicle from it's spawner if it had one
        try
        {
            GetVehicle(vehicle).Spawn?.UnlinkVehicle();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to unlink vehicle spawn for vehicle {0} ({1}).", vehicle.asset.FriendlyName, vehicle.asset.GUID);
        }
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
    {
        PrepareToDeleteVehicle(e.Vehicle.Vehicle);
        DeregisterWarfareVehicle(e.Vehicle.Vehicle);
    }
}