using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;
public class VehicleService
{
    private const float VehicleSpawnOffset = 5f;
    public const ushort MaxBatteryCharge = 10000;
    private readonly ILogger<VehicleService> _logger;
    private readonly VehicleInfoStore _vehicleInfoStore;

    public VehicleService(ILogger<VehicleService> logger, VehicleInfoStore vehicleInfoStore)
    {
        _logger = logger;
        _vehicleInfoStore = vehicleInfoStore;
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
    public async UniTask<InteractableVehicle> SpawnVehicleAsync(VehicleSpawnInfo spawn, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (spawn.LinkedVehicle != null)
        {
            if (!spawn.LinkedVehicle.isDead)
            {
                throw new NotSupportedException($"There can only be one vehicle per spawn, and this spawn already has a vehicle: {spawn.Vehicle.ToDisplayString()}.");
            }

            spawn.UnlinkVehicle();
        }

        WarfareVehicleInfo? vehicleInfo = _vehicleInfoStore.GetVehicleInfo(spawn.Vehicle);
        if (vehicleInfo == null)
        {
            throw new RecordsNotFoundException($"WarfareVehicleInfo not available for {spawn.Vehicle.ToDisplayString()}");
        }

        IBuildable? spawner = spawn.Spawner;
        if (spawner == null || spawner.IsDead)
        {
            throw new InvalidOperationException("Spawner buildable no longer exists.");
        }

        Quaternion spawnRotation = spawner.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

        Vector3 spawnPosition = spawner.Position + Vector3.up * VehicleSpawnOffset;

        InteractableVehicle vehicle = await SpawnVehicleAsync(spawn.Vehicle, spawnPosition, spawnRotation, group: new CSteamID(spawner.Group), token: token);
        await UniTask.SwitchToMainThread(token);

        spawn.LinkVehicle(vehicle);
        // todo update sign
        _logger.LogDebug("Spawned new {0} at {1}.", spawn.Vehicle.ToDisplayString(), spawnPosition);
        return vehicle;
    }

    /// <summary>
    /// Spawn a vehicle with the given information.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AssetNotFoundException">Unable to find the vehicle asset.</exception>
    /// <exception cref="Exception">Game failed to spawn the vehicle.</exception>
    public async UniTask<InteractableVehicle> SpawnVehicleAsync(
        IAssetLink<VehicleAsset> vehicle,
        Vector3 position,
        Quaternion rotation,
        CSteamID owner = default,
        CSteamID group = default,
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
                                                                 byte.MaxValue, default
                                                                );
        if (veh == null)
            throw new Exception($"Failed to spawn vehicle {vehicle.ToDisplayString()} due to vanilla code, possible a misconfigured vehicle.");

        WarfareVehicleInfo? vehicleInfo = _vehicleInfoStore.GetVehicleInfo(vehicle);

        if (vehicleInfo is not { Trunk.Count: > 0 })
            return veh;

        // add items to trunk
        foreach (WarfareVehicleInfo.TrunkItem item in vehicleInfo.Trunk)
        {
            if (!item.Item.TryGetAsset(out ItemAsset? itemAsset))
            {
                _logger.LogWarning("Failed to find item asset for the trunk of {0}: {1}.", vehicle.ToDisplayString(), item.Item.ToDisplayString());
                continue;
            }

            Item info = new Item(itemAsset.id, itemAsset.amount, 100, item.State ?? itemAsset.getState(EItemOrigin.ADMIN));
            if (veh.trunkItems.checkSpaceEmpty(item.X, item.Y, itemAsset.size_x, itemAsset.size_y, item.Rotation))
            {
                veh.trunkItems.addItem(item.X, item.Y, item.Rotation, info);
            }
            else if (!veh.trunkItems.tryAddItem(info))
            {
                ItemManager.dropItem(info, position, false, true, true);
            }
        }

        return veh;
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
                if (region.drops[b].interactable is InteractableStorage storage)
                {
                    storage.despawnWhenDestroyed = true;
                }
            }
        }

        // empty trunk so items don't drop
        if (vehicle.trunkItems != null)
        {
            int ct = vehicle.trunkItems.getItemCount();
            for (int i = ct - 1; i >= 0; --i)
                vehicle.trunkItems.removeItem((byte)i);
        }

        // remove all players and teleport them to the ground so they don't take fall damage
        for (int i = 0; i < vehicle.passengers.Length; ++i)
        {
            SteamPlayer? pl = vehicle.passengers[i].player;
            if (pl == null)
                continue;

            VehicleManager.forceRemovePlayer(vehicle, pl.playerID.steamID);
            Vector3 p = pl.player.transform.position;
            float y = TerrainUtility.GetHighestPoint(in p, 1f);
            if (Mathf.Abs(p.y - y) > 5f)
            {
                p.y = y;
                pl.player.teleportToLocationUnsafe(p, pl.player.look.aim.transform.rotation.eulerAngles.y);
            }
        }

        if (!vehicle.TryGetComponent(out VehicleComponent component) || component.Spawn == null)
            return;

        // unlink vehicle from it's spawner if it had one
        try
        {
            component.Spawn.UnlinkVehicle();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to unlink vehicle spawn for vehicle {0} ({1}).", vehicle.asset.FriendlyName, vehicle.asset.GUID);
        }
    }
}
