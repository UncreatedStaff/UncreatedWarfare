using DanielWillett.ReflectionTools;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles;

[Priority(-2 /* run after VehicleSpawnerStore */)]
public class VehicleService : ILayoutHostedService,
    IRequestHandler<VehicleSpawnerComponent, VehicleSpawnInfo>,
    IRequestHandler<VehicleBaySignInstanceProvider, VehicleSpawnInfo>,
    IRequestHandler<VehicleSpawnInfo, VehicleSpawnInfo>
{
    private const float VehicleSpawnOffset = 5f;
    public const ushort MaxBatteryCharge = 10000;
    private readonly RequestVehicleTranslations _reqTranslations;
    private readonly VehicleInfoStore _vehicleInfoStore;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly ILogger<VehicleService> _logger;
    private readonly WarfareModule _module;
    private readonly ZoneStore _globalZoneStore;
    private readonly DatabaseInterface _moderationSql;

    private const float MaxVehicleAbandonmentDistance = 300;

    public VehicleService(ILogger<VehicleService> logger, VehicleInfoStore vehicleInfoStore, VehicleSpawnerStore spawnerStore, WarfareModule module, TranslationInjection<RequestVehicleTranslations> reqTranslations, ZoneStore globalZoneStore, DatabaseInterface moderationSql)
    {
        _logger = logger;
        _vehicleInfoStore = vehicleInfoStore;
        _spawnerStore = spawnerStore;
        _module = module;
        _globalZoneStore = globalZoneStore;
        _moderationSql = moderationSql;
        _reqTranslations = reqTranslations.Value;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        IServiceProvider serviceProvider = _module.ScopedProvider.Resolve<IServiceProvider>();
        foreach (VehicleSpawnInfo spawn in _spawnerStore.Spawns)
        {
            WarfareVehicleInfo? info = _vehicleInfoStore.Vehicles.FirstOrDefault(x => x.Vehicle.MatchAsset(spawn.Vehicle));

            if (info == null)
                continue;
            
            spawn.Spawner.Model.GetOrAddComponent<VehicleSpawnerComponent>().Init(spawn, info, serviceProvider);
        }

        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        foreach (VehicleSpawnInfo spawn in _spawnerStore.Spawns)
        {
            if (spawn.Spawner.Model.TryGetComponent(out VehicleSpawnerComponent comp))
            {
                Object.Destroy(comp);
            }
        }

        return UniTask.CompletedTask;
    }

    Task<bool> IRequestHandler<VehicleBaySignInstanceProvider, VehicleSpawnInfo>.RequestAsync(WarfarePlayer player, VehicleBaySignInstanceProvider? sign, IRequestResultHandler resultHandler, CancellationToken token)
    {
        if (sign?.Spawn == null || !sign.Spawn.Spawner.Model.TryGetComponent(out VehicleSpawnerComponent component) || component.SpawnInfo == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return Task.FromResult(false);
        }

        return RequestAsync(player, component.SpawnInfo, resultHandler, token);
    }

    Task<bool> IRequestHandler<VehicleSpawnerComponent, VehicleSpawnInfo>.RequestAsync(WarfarePlayer player, VehicleSpawnerComponent? spawner, IRequestResultHandler resultHandler, CancellationToken token)
    {
        if (spawner?.SpawnInfo == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return Task.FromResult(false);
        }

        return RequestAsync(player, spawner.SpawnInfo, resultHandler, token);
    }

    /// <summary>
    /// Request unlocking a vehicle from a spawn.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public async Task<bool> RequestAsync(WarfarePlayer player, VehicleSpawnInfo? spawn, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!player.IsOnline)
        {
            return false;
        }

        if (spawn == null || spawn.Spawner.IsDead || !spawn.Vehicle.TryGetAsset(out VehicleAsset? vehicleAsset))
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        WarfareVehicleInfo? vehicleInfo = _vehicleInfoStore.Vehicles.FirstOrDefault(x => x.Vehicle.MatchAsset(vehicleAsset));
        if (vehicleInfo == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        AssetBan? existingAssetBan = await _moderationSql.GetActiveAssetBan(player.Steam64, vehicleInfo.Type, token: token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        if (!player.IsOnline)
        {
            return false;
        }

        // asset ban
        if (existingAssetBan != null && existingAssetBan.IsAssetBanned(vehicleInfo.Type, true, true))
        {
            if (existingAssetBan.VehicleTypeFilter.Length == 0)
            {
                resultHandler.MissingRequirement(player, spawn,
                    existingAssetBan.IsPermanent
                        ? _reqTranslations.AssetBannedGlobalPermanent.Translate(player)
                        : _reqTranslations.AssetBannedGlobal.Translate(existingAssetBan.GetTimeUntilExpiry(false), player)
                );
                return false;
            }

            string commaList = existingAssetBan.GetCommaList(false, player.Locale.LanguageInfo);
            resultHandler.MissingRequirement(player, spawn,
                existingAssetBan.IsPermanent
                    ? _reqTranslations.AssetBannedPermanent.Translate(commaList, player)
                    : _reqTranslations.AssetBanned.Translate(commaList, existingAssetBan.GetTimeUntilExpiry(false), player)
            );
        }

        InteractableVehicle? vehicle = spawn.LinkedVehicle;
        if (vehicle == null || vehicle.isDead || vehicle.isExploded || vehicle.isDrowned || !vehicle.asset.canBeLocked)
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.NotAvailable.Translate(player));
            return false;
        }

        if (vehicle.lockedGroup.m_SteamID != 0 || vehicle.lockedOwner.m_SteamID != 0)
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.AlreadyRequested.Translate(player));
            return false;
        }

        Team team = player.Team;
        Zone? mainZone = _globalZoneStore.EnumerateInsideZones(spawn.Spawner.Position, ZoneType.MainBase).FirstOrDefault();
        if (mainZone?.Faction != null && !mainZone.Faction.Equals(team.Faction.FactionId, StringComparison.OrdinalIgnoreCase))
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.IncorrectTeam.Translate(player));
            return false;
        }

        KitPlayerComponent comp = player.Component<KitPlayerComponent>();

        if (vehicleInfo.Class > Class.Unarmed && comp.ActiveClass != vehicleInfo.Class || vehicleInfo.Class == Class.Squadleader && !player.IsSquadLeader())
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.IncorrectKitClass.Translate(vehicleInfo.Class, player));
            return false;
        }

        Vector3 pos = player.Position;
        foreach (VehicleSpawnInfo otherSpawn in _spawnerStore.Spawns)
        {
            if (otherSpawn == spawn || otherSpawn.LinkedVehicle == null || otherSpawn.LinkedVehicle.isDead || otherSpawn.LinkedVehicle.isExploded || otherSpawn.LinkedVehicle.isDrowned)
                continue;

            InteractableVehicle v = otherSpawn.LinkedVehicle;
            if (v.lockedOwner.m_SteamID != player.Steam64.m_SteamID || MathUtility.SquaredDistance(v.transform.position, in pos) > MaxVehicleAbandonmentDistance * MaxVehicleAbandonmentDistance)
                continue;

            resultHandler.MissingRequirement(player, spawn, _reqTranslations.AnotherVehicleAlreadyOwned.Translate(v.asset, player));
            return false;
        }

        if (vehicleInfo.UnlockRequirements != null)
        {
            foreach (UnlockRequirement requirement in vehicleInfo.UnlockRequirements)
            {
                bool canAccess = await requirement.CanAccessAsync(player, token);
                await UniTask.SwitchToMainThread(token);
                if (!player.IsOnline)
                    return false;

                if (canAccess)
                    continue;

                resultHandler.MissingUnlockRequirement(player, spawn, requirement);
                return false;
            }
        }

        if (!await RequestHelper.TryApplyCosts(vehicleInfo.UnlockCosts, spawn, resultHandler, player, team, token))
        {
            return false;
        }

        await UniTask.SwitchToMainThread(token);

        if (spawn.LinkedVehicle == null || spawn.LinkedVehicle.isDead || spawn.LinkedVehicle.isDrowned || spawn.LinkedVehicle.isExploded)
        {
            spawn.UnlinkVehicle();
            await SpawnVehicleAsync(spawn, token);
            await UniTask.SwitchToMainThread(token);
        }

        vehicle = spawn.LinkedVehicle;

        VehicleManager.ServerSetVehicleLock(vehicle, player.Steam64, player.Team.GroupId, true);
        resultHandler.Success(player, spawn);
        return true;
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
            if (!spawn.LinkedVehicle.isDead && !spawn.LinkedVehicle.isExploded)
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

        InteractableVehicle vehicle = await SpawnVehicleAsync(spawn.Vehicle, spawnPosition, spawnRotation, paintColor: vehicleInfo.PaintColor, token: token);
        await UniTask.SwitchToMainThread(token);

        spawn.LinkVehicle(vehicle);

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
