using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(Whitelister))]
public class VehicleBay : ListSqlSingleton<VehicleData>, ILevelStartListenerAsync, IDeclareWinListenerAsync, IPlayerPostInitListenerAsync, IQuestCompletedHandler
{
    public const ushort MAX_BATTERY_CHARGE = 10000;
    private static VehicleBayConfig _config;
    private bool _hasWhitelisted;
    private static VehicleBay? _vb;

    public static VehicleBay? GetSingletonQuick()
    {
        if (_vb == null || !_vb.IsLoaded)
            return _vb = Data.Singletons.GetSingleton<VehicleBay>();
        return _vb;
    }
    public static VehicleBayData Config => _config == null ? throw new SingletonUnloadedException(typeof(VehicleBay)) : _config.Data;
    public VehicleBay() : base("vehiclebay", SCHEMAS)
    {
    }
    public override Task PreLoad()
    {
        if (_config == null)
            _config = new VehicleBayConfig();
        else _config.Reload();
        return Task.CompletedTask;
    }
    public override async Task PostLoad()
    {
        EventDispatcher.EnterVehicleRequested += OnVehicleEnterRequested;
        EventDispatcher.VehicleSwapSeatRequested += OnVehicleSwapSeatRequested;
        EventDispatcher.ExitVehicleRequested += OnVehicleExitRequested;
        EventDispatcher.ExitVehicle += OnVehicleExit;
        EventDispatcher.VehicleSpawned += OnVehicleSpawned;
        await WaitAsync();
        try
        {
            if (Whitelister.Loaded && !_hasWhitelisted) // whitelist all vehicle bay items
            {
                if (!UCWarfare.IsMainThread)
                    await UCWarfare.ToUpdate();
                WhitelistItems();
            }
        }
        finally
        {
            Release();
        }
    }
    public override Task PostUnload()
    {
        EventDispatcher.VehicleSpawned -= OnVehicleSpawned;
        EventDispatcher.ExitVehicle -= OnVehicleExit;
        EventDispatcher.ExitVehicleRequested -= OnVehicleExitRequested;
        EventDispatcher.VehicleSwapSeatRequested -= OnVehicleSwapSeatRequested;
        EventDispatcher.EnterVehicleRequested -= OnVehicleEnterRequested;
        _hasWhitelisted = false;
        _vb = null;
        return Task.CompletedTask;
    }
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    private void StartDualLock()
    {
        Monitor.Enter(this);
        Monitor.Enter(Items);
    }
    private void EndDualLock()
    {
        Monitor.Exit(this);
        Monitor.Exit(Items);
    }
    private void WhitelistItems()
    {
        lock (Items)
        {
            for (int i = 0; i < Count; i++)
            {
                SqlItem<VehicleData> data = Items[i];
                if (data.Item?.Items != null && data.Item.Items.Length > 0)
                {
                    VehicleData d = data.Item;
                    for (int j = 0; j < d.Items.Length; j++)
                    {
                        if (!Whitelister.IsWhitelisted(d.Items[j], out _))
                            Whitelister.AddItem(d.Items[j]);
                    }
                }
            }
        }
        _hasWhitelisted = true;
    }
    async Task ILevelStartListenerAsync.OnLevelReady()
    {
        await WaitAsync();
        try
        {
            if (Whitelister.Loaded && !_hasWhitelisted) // whitelist all vehicle bay items
            {
                if (!UCWarfare.IsMainThread)
                    await UCWarfare.ToUpdate();
                WhitelistItems();
            }
        }
        finally
        {
            Release();
        }
    }
    async Task IDeclareWinListenerAsync.OnWinnerDeclared(ulong winner)
    {
        ThreadUtil.assertIsGameThread();
        await WaitAsync().ConfigureAwait(false);
        try
        {
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();
            AbandonAllVehicles();
        }
        finally
        {
            Release();
        }
    }
    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player)
    {
        await SendQuests(player).ConfigureAwait(false);
    }
    private async Task SendQuests(UCPlayer player, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await WaitAsync(token).ConfigureAwait(false);
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate(token);
        try
        {
            for (int i = 0; i < Items.Count; i++)
            {
                SqlItem<VehicleData> item = Items[i];
                if (item.Item?.UnlockRequirements != null && item.Item.UnlockRequirements.Length > 0)
                {
                    VehicleData data = item.Item;
                    for (int j = 0; j < data.UnlockRequirements.Length; j++)
                    {
                        if (data.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets is { Length: > 0 } && !req.CanAccess(player))
                        {
                            if (Assets.find(req.QuestID) is QuestAsset quest)
                            {
                                player.Player.quests.sendAddQuest(quest.id);
                            }
                            else
                            {
                                L.LogWarning("Unknown quest id " + req.QuestID + " in vehicle requirement for " + data.VehicleID.ToString("N"));
                            }
                            for (int r = 0; r < req.UnlockPresets.Length; r++)
                            {
                                BaseQuestTracker? tracker = QuestManager.CreateTracker(player, req.UnlockPresets[r]);
                                if (tracker == null)
                                    L.LogWarning("Failed to create tracker for vehicle " + data.VehicleID.ToString("N") + ", player " + player.Name.PlayerName);
                                else
                                    L.LogDebug("Created tracker for vehicle unlock quest: " + tracker.QuestData.QuestType + ".");
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task AddRequestableVehicle(InteractableVehicle vehicle, CancellationToken token = default)
    {
        this.AssertLoadedIntl();
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleData data = new VehicleData(vehicle.asset.GUID)
        {
            PrimaryKey = PrimaryKey.NotAssigned
        };
        data.SaveMetaData(vehicle);
        ThreadUtil.assertIsGameThread();
        StartDualLock();
        await AddOrUpdate(data, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveRequestableVehicle(Guid vehicle, CancellationToken token = default)
    {
        this.AssertLoadedIntl();
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SqlItem<VehicleData>? data = await this.GetDataProxy(vehicle, token).ConfigureAwait(false);
        if (data is not null)
        {
            await data.Delete(token).ConfigureAwait(false);
            return true;
        }

        return false;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<VehicleData?> GetData(Guid guid, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < List.Count; ++i)
            {
                SqlItem<VehicleData> item = List[i];
                if (item.Item != null && item.Item.VehicleID == guid)
                    return item.Item;
            }
        }
        finally
        {
            Release();
        }

        return null;
    }
    public VehicleData? GetDataSync(Guid guid)
    {
        lock (Items)
        {
            int map = MapScheduler.Current;
            for (int i = 0; i < Items.Count; i++)
            {
                SqlItem<VehicleData> item = Items[i];
                if (item.Item != null && map == item.Item.Map && item.Item.VehicleID == guid)
                    return item.Item;
            }
            for (int i = 0; i < Items.Count; i++)
            {
                SqlItem<VehicleData> item = Items[i];
                if (item.Item != null && item.Item.Map < 0 && item.Item.VehicleID == guid)
                    return item.Item;
            }
        }

        return null;
    }
    public SqlItem<VehicleData>? GetDataProxySync(Guid guid)
    {
        lock (Items)
        {
            int map = MapScheduler.Current;
            for (int i = 0; i < Items.Count; i++)
            {
                SqlItem<VehicleData> item = Items[i];
                if (item.Item != null && map == item.Item.Map && item.Item.VehicleID == guid)
                    return item;
            }
            for (int i = 0; i < Items.Count; i++)
            {
                SqlItem<VehicleData> item = Items[i];
                if (item.Item != null && item.Item.Map < 0 && item.Item.VehicleID == guid)
                    return item;
            }
        }

        return null;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<SqlItem<VehicleData>?> GetDataProxy(Guid guid, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        int map = MapScheduler.Current;
        try
        {
            lock (Items)
            {
                for (int i = 0; i < List.Count; ++i)
                {
                    SqlItem<VehicleData> item = List[i];
                    if (item.Item != null && map == item.Item.Map && item.Item.VehicleID == guid)
                        return item;
                }
                for (int i = 0; i < List.Count; ++i)
                {
                    SqlItem<VehicleData> item = List[i];
                    if (item.Item != null && item.Item.Map < 0 && item.Item.VehicleID == guid)
                        return item;
                }
            }
        }
        finally
        {
            Release();
        }

        return null;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<(SetPropertyResult, MemberInfo?)> SetProperty(VehicleData data, string property, string value, CancellationToken token = default)
    {
        AssertLoadedIntl();
        await WaitAsync(token).ConfigureAwait(false);
        StartDualLock();
        try
        {
            SqlItem<VehicleData>? item = FindProxyNoLock(data);
            if (item is null || item.Item == null)
            {
                if (data.PrimaryKey.IsValid)
                    item = await DownloadNoLock(data.PrimaryKey, token).ConfigureAwait(false);
                if (item is null || item.Item == null)
                    return (SetPropertyResult.ObjectNotFound, null);
            }

            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();
            return await SetPropertyNoLock(item, property, value, true, token).ConfigureAwait(false);
        }
        finally
        {
            EndDualLock();
            Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<InteractableVehicle?> SpawnLockedVehicle(Guid vehicleID, Vector3 position, Quaternion rotation, ulong owner = 0ul, ulong groupOwner = 0ul, CancellationToken token = default)
    {
        await UCWarfare.ToLevelLoad();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SqlItem<VehicleData>? proxy = await GetDataProxy(vehicleID, token).ConfigureAwait(false);
        if (proxy?.Item == null)
        {
            L.LogError("Unable to spawn vehicle, not saved to vehicle bay.");
            return null;
        }

        await proxy.Enter(token).ConfigureAwait(false);
        try
        {
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();

            if (Assets.find(vehicleID) is not VehicleAsset asset)
            {
                L.LogError("Unable to find vehicle asset of " + vehicleID.ToString("N") + ".");
                return null;
            }
            ThreadUtil.assertIsGameThread();
            InteractableVehicle vehicle;
            try
            {
                vehicle = VehicleManager.SpawnVehicleV3(asset, 0, 0, 0f, position, rotation, false, false, false, false, asset.fuel,
                    asset.health, MAX_BATTERY_CHARGE, new CSteamID(owner), new CSteamID(groupOwner), true, null, byte.MaxValue);
                if (vehicle == null)
                {
                    L.LogError("Unknown internal error encountered spawning " + asset.vehicleName + ".");
                    return null;
                }
            }
            catch (Exception ex)
            {
                L.LogError("Internal error encountered spawning " + asset.vehicleName + ".");
                L.LogError(ex);
                return null;
            }

            VehicleData data = proxy.Item;
            if (data.Metadata != null)
            {
                if (data.Metadata.TrunkItems is { Count: > 0 })
                {
                    foreach (KitItem k in data.Metadata.TrunkItems)
                    {
                        if (Assets.find(k.Id) is ItemAsset iasset)
                        {
                            Item item = new Item(iasset.id, k.Amount, 100, Util.CloneBytes(k.Metadata));
                            if (vehicle.trunkItems.checkSpaceEmpty(k.X, k.Y, iasset.size_x, iasset.size_y, k.Rotation))
                                vehicle.trunkItems.addItem(k.X, k.Y, k.Rotation, item);
                            else if (!vehicle.trunkItems.tryAddItem(item))
                                ItemManager.dropItem(item, vehicle.transform.position, false, true, true);
                        }
                    }
                }
                if (data.Metadata.Barricades is { Count: > 0 })
                {
                    foreach (VBarricade vb in data.Metadata.Barricades)
                    {
                        if (Assets.find(vb.BarricadeID) is not ItemBarricadeAsset basset)
                        {
                            L.LogError("Unable to find barricade asset of " + vb.BarricadeID.ToString("N") + ".");
                            continue;
                        }
                        byte[] state = Util.CloneBytes(vb.Metadata);
                        F.EnsureCorrectGroupAndOwner(ref state, basset, 0ul, TeamManager.AdminID);
                        Barricade barricade = new Barricade(basset, asset.health, state);
                        Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                        Transform t = BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, owner, groupOwner);
                        if (basset is ItemStorageAsset && BarricadeManager.FindBarricadeByRootTransform(t)?.interactable is InteractableStorage s)
                            s.despawnWhenDestroyed = true;
                    }
                }
            }

            return vehicle;
        }
        finally
        {
            Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> AddCrewSeat(Guid guid, byte seat, CancellationToken token = default, bool save = true)
    {
        SqlItem<VehicleData>? proxy = await GetDataProxy(guid, token).ConfigureAwait(false);
        if (proxy is null)
            return false;
        return await AddCrewSeat(proxy, seat, token, save).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> AddCrewSeat(SqlItem<VehicleData> proxy, byte seat, CancellationToken token = default, bool save = true)
    {
        await proxy.Enter(token).ConfigureAwait(false);
        try
        {
            if (proxy.Item == null)
                return false;
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();
            if (proxy.Item.CrewSeats == null)
            {
                proxy.Item.CrewSeats = new byte[] { seat };
                return true;
            }
            for (int i = 0; i < proxy.Item.CrewSeats.Length; ++i)
                if (proxy.Item.CrewSeats[i] == seat)
                    return false;
            Util.AddToArray(ref proxy.Item.CrewSeats!, seat);
            if (save)
                await proxy.SaveItem(token).ConfigureAwait(false);
        }
        finally
        {
            proxy.Release();
        }
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveCrewSeat(Guid guid, byte seat, CancellationToken token = default, bool save = true)
    {
        SqlItem<VehicleData>? proxy = await GetDataProxy(guid, token).ConfigureAwait(false);
        if (proxy is null)
            return false;
        return await RemoveCrewSeat(proxy, seat, token, save).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> RemoveCrewSeat(SqlItem<VehicleData> proxy, byte seat, CancellationToken token = default, bool save = true)
    {
        await proxy.Enter(token).ConfigureAwait(false);
        try
        {
            if (proxy.Item == null)
                return false;
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();
            if (proxy.Item.CrewSeats == null || proxy.Item.CrewSeats.Length == 0)
                return false;
            int index = -1;
            for (int i = 0; i < proxy.Item.CrewSeats.Length; ++i)
            {
                if (proxy.Item.CrewSeats[i] == seat)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
                return false;
            Util.RemoveFromArray(ref proxy.Item.CrewSeats!, index);
            if (save)
                await proxy.SaveItem(token).ConfigureAwait(false);
        }
        finally
        {
            proxy.Release();
        }
        return true;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> SetItems(Guid guid, Guid[] items, CancellationToken token = default, bool save = true, bool clone = false)
    {
        SqlItem<VehicleData>? proxy = await GetDataProxy(guid, token).ConfigureAwait(false);
        if (proxy is null)
            return false;
        return await SetItems(proxy, items, token, save, clone).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<bool> SetItems(SqlItem<VehicleData> proxy, Guid[] items, CancellationToken token = default, bool save = true, bool clone = false)
    {
        await proxy.Enter(token).ConfigureAwait(false);
        try
        {
            if (proxy.Item == null)
                return false;
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate();
            if (clone)
            {
                Guid[] old = items;
                items = new Guid[old.Length];
                Array.Copy(old, items, old.Length);
            }
            proxy.Item.Items = items;
            if (save)
                await proxy.SaveItem(token).ConfigureAwait(false);
        }
        finally
        {
            proxy.Release();
        }
        return true;
    }
    
    // todo move all below to VehicleSpawner
    public void AbandonAllVehicles()
    {
        ThreadUtil.assertIsGameThread();
        for (int i = 0; i < VehicleSpawner.Singleton.Count; ++i)
        {
            VehicleSpawn v = VehicleSpawner.Singleton[i];
            if (v.HasLinkedVehicle(out InteractableVehicle veh))
            {
                ulong t = veh.lockedGroup.m_SteamID.GetTeam();
                if (t == 1 && TeamManager.Team1Main.IsInside(veh.transform.position) ||
                    t == 2 && TeamManager.Team2Main.IsInside(veh.transform.position))
                {
                    AbandonVehicle(veh, null, v, false);
                }
            }
        }
    }
    public void AbandonVehicle(InteractableVehicle vehicle, VehicleData? data, VehicleSpawn? spawn, bool respawn = true)
    {
        ThreadUtil.assertIsGameThread();
        if ((data ??= GetDataSync(vehicle.asset.GUID)) is null)
            return;
        if (spawn is null && !VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out spawn))
            return;

        UCPlayer? pl = UCPlayer.FromID(vehicle.lockedOwner.m_SteamID);
        if (pl != null)
        {
            int creditReward = 0;
            if (data!.CreditCost > 0 && spawn.Component != null && spawn.Component.RequestTime != 0)
                creditReward = data.CreditCost - Mathf.Min(data.CreditCost, Mathf.FloorToInt(data.AbandonValueLossSpeed * (Time.realtimeSinceStartup - spawn.Component.RequestTime)));

            Points.AwardCredits(pl, creditReward, T.AbandonCompensationToast.Translate(pl), false, false);
        }

        DeleteVehicle(vehicle);

        if (respawn)
            Task.Run(() => spawn.SpawnVehicle());
    }
    public static void DeleteVehicle(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        vehicle.forceRemoveAllPlayers();
        BarricadeRegion reg = BarricadeManager.getRegionFromVehicle(vehicle);
        if (reg != null)
        {
            for (int b = 0; b < reg.drops.Count; b++)
            {
                if (reg.drops[b].interactable is InteractableStorage storage)
                {
                    storage.despawnWhenDestroyed = true;
                }
            }
        }
        vehicle.trunkItems?.clear();
        VehicleManager.askVehicleDestroy(vehicle);
    }
    public static void DeleteAllVehiclesFromWorld()
    {
        for (int i = 0; i < VehicleManager.vehicles.Count; i++)
        {
            DeleteVehicle(VehicleManager.vehicles[i]);
        }
    }
    public static bool IsVehicleFull(InteractableVehicle vehicle, bool excludeDriver = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            if (seat == 0 && excludeDriver)
                continue;

            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player == null)
            {
                return true;
            }
        }
        return true;
    }
    public static bool TryGetFirstNonCrewSeat(InteractableVehicle vehicle, VehicleData data, out byte seat)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player == null && !data.CrewSeats.Contains(seat))
            {
                return true;
            }
        }
        seat = 0;
        return false;
    }
    public static bool TryGetFirstNonDriverSeat(InteractableVehicle vehicle, out byte seat)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        seat = 0;
        do
        {
            if (++seat >= vehicle.passengers.Length)
                return false;
        } while (vehicle.passengers[seat].player != null);
        return true;
    }
    public static bool IsOwnerInVehicle(InteractableVehicle vehicle, UCPlayer owner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.lockedOwner == CSteamID.Nil || owner == null) return false;

        foreach (Passenger passenger in vehicle.passengers)
        {
            if (passenger.player != null && owner.CSteamID == passenger.player.playerID.steamID)
            {
                return true;
            }
        }
        return false;
    }
    public static int CountCrewmen(InteractableVehicle vehicle, VehicleData data)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        int count = 0;
        for (byte seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (data.CrewSeats.Contains(seat) && passenger.player != null)
            {
                count++;
            }
        }
        return count;
    }
    private static void OnVehicleSpawned(VehicleSpawned e)
    {
        e.Vehicle.gameObject.AddComponent<VehicleComponent>().Initialize(e.Vehicle);
    }
    private static void OnVehicleExit(ExitVehicle e)
    {
        if (e.OldPassengerIndex == 0 && e.Vehicle.transform.TryGetComponent(out VehicleComponent comp))
            comp.LastDriverTime = Time.realtimeSinceStartup;
        if (KitManager.KitExists(e.Player.KitName, out Kit kit))
        {
            if (kit.Class is EClass.LAT or EClass.HAT)
            {
                e.Player.Player.equipment.dequip();
            }
        }
    }
    private static void OnVehicleExitRequested(ExitVehicleRequested e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!e.Player.OnDuty() && e.ExitLocation.y - F.GetHeightAt2DPoint(e.ExitLocation.x, e.ExitLocation.z) > UCWarfare.Config.MaxVehicleHeightToLeave)
        {
            if (!FOBManager.Config.Buildables.Exists(v => v.Type == EBuildableType.EMPLACEMENT && v.Emplacement is not null && v.Emplacement.EmplacementVehicle is not null && v.Emplacement.EmplacementVehicle.Guid == e.Vehicle.asset.GUID))
            {
                e.Player.SendChat(T.VehicleTooHigh);
                e.Break();
                return;
            }
        }
    }
    private void OnVehicleEnterRequested(EnterVehicleRequested e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Break();
            return;
        }
        if (!e.Vehicle.asset.canBeLocked) return;
        if (!e.Player.OnDuty() && Data.Gamemode.State == EState.STAGING && Data.Is<IStagingPhase>(out _) && (!Data.Is(out IAttackDefense atk) || e.Player.GetTeam() == atk.AttackingTeam))
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Break();
            return;
        }
        if (Data.Is(out IRevives r) && r.ReviveManager.IsInjured(e.Player.Steam64))
        {
            e.Break();
            return;
        }

        if (!KitManager.HasKit(e.Player, out Kit kit))
        {
            e.Player.SendChat(T.VehicleNoKit);
            e.Break();
            return;
        }
    }
    private void OnVehicleSwapSeatRequested(VehicleSwapSeatRequested e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!e.Vehicle.TryGetComponent(out VehicleComponent c))
            return;
        if (c.IsEmplacement && e.FinalSeat == 0)
        {
            e.Break();
        }
        else
        {
            if (!KitManager.HasKit(e.Player, out Kit kit))
            {
                e.Player.SendChat(T.VehicleNoKit);
                e.Break();
                return;
            }

            UCPlayer? owner = UCPlayer.FromCSteamID(e.Vehicle.lockedOwner);
            VehicleData? data = c.Data?.Item;
            if (data != null &&
                data.CrewSeats.ArrayContains(e.FinalSeat) &&
                data.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
            {
                if (e.Player.KitClass == data.RequiredClass || e.Player.OnDuty())
                {
                    if (e.FinalSeat == 0) // if a crewman is trying to enter the driver's seat
                    {
                        bool canEnterDriverSeat = owner == null ||
                            e.Player == owner ||
                            e.Player.OnDuty() ||
                            IsOwnerInVehicle(e.Vehicle, owner) ||
                            (owner != null && owner.Squad != null && owner.Squad.Members.Contains(e.Player) ||
                            (owner!.Position - e.Vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                            (data.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(e.Vehicle.transform.position, EfobRadius.FULL_WITH_BUNKER_CHECK, e.Vehicle.lockedGroup.m_SteamID) != null);

                        if (!canEnterDriverSeat)
                        {
                            if (owner is null || owner!.Squad is null)
                                e.Player.SendChat(T.VehicleWaitForOwner, owner ?? new OfflinePlayer(e.Vehicle.lockedOwner.m_SteamID) as IPlayer);
                            else
                                e.Player.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);
                            e.Break();
                        }
                    }
                    else // if the player is trying to switch to a gunner's seat
                    {
                        if (!(F.IsInMain(e.Vehicle.transform.position) || e.Player.OnDuty())) // if player is trying to switch to a gunner's seat outside of main
                        {
                            if (e.Vehicle.passengers.Length == 0 || e.Vehicle.passengers[0].player is null) // if they have no driver
                            {
                                e.Player.SendChat(T.VehicleDriverNeeded);
                                e.Break();
                            }
                            else if (e.Player.Steam64 == e.Vehicle.passengers[0].player.playerID.steamID.m_SteamID) // if they are the driver
                            {
                                e.Player.SendChat(T.VehicleAbandoningDriver);
                                e.Break();
                            }
                        }
                    }
                }
                else
                {
                    e.Player.SendChat(T.VehicleMissingKit, data.RequiredClass);
                    e.Break();
                }
            }
            else
            {
                if (e.FinalSeat == 0)
                {
                    bool canEnterDriverSeat = owner is null || e.Player.Steam64 == owner.Steam64 || e.Player.OnDuty() || IsOwnerInVehicle(e.Vehicle, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(e.Player));

                    if (!canEnterDriverSeat)
                    {
                        if (owner!.Squad == null)
                            e.Player.SendChat(T.VehicleWaitForOwner, owner);
                        else
                            e.Player.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                        e.Break();
                    }
                }
            }
        }
    }
    // TODO
    void IQuestCompletedHandler.OnQuestCompleted(QuestCompleted e) { }
    #region Sql
    private const string TABLE_MAIN = "vehicle_data";
    private const string TABLE_UNLOCK_REQUIREMENTS = "vehicle_data_unlock_requirements";
    private const string TABLE_DELAYS = "vehicle_data_delays";
    private const string TABLE_ITEMS = "vehicle_data_request_items";
    private const string TABLE_CREW_SEATS = "vehicle_data_crew_seats";
    private const string TABLE_BARRICADES = "vehicle_data_barricades";
    private const string TABLE_BARRICADE_ITEMS = "vehicle_data_barricade_items";
    private const string TABLE_BARRICADE_DISPLAY_DATA = "vehicle_data_barricade_display_data";
    private const string TABLE_TRUNK_ITEMS = "vehicle_data_trunk_items";
    private const string COLUMN_PK = "pk";
    private const string COLUMN_EXT_PK = "VehicleData";
    private const string COLUMN_MAP = "Map";
    private const string COLUMN_FACTION = "Faction";
    private const string COLUMN_VEHICLE_GUID = "VehicleId";
    private const string COLUMN_RESPAWN_TIME = "RespawnTime";
    private const string COLUMN_TICKET_COST = "TicketCost";
    private const string COLUMN_CREDIT_COST = "CreditCost";
    private const string COLUMN_REARM_COST = "RearmCost";
    private const string COLUMN_COOLDOWN = "Cooldown";
    private const string COLUMN_VEHICLE_TYPE = "VehicleType";
    private const string COLUMN_BRANCH = "Branch";
    private const string COLUMN_REQUIRED_CLASS = "RequiredClass";
    private const string COLUMN_REQUIRES_SQUADLEADER = "RequiresSquadleader";
    private const string COLUMN_ABANDON_BLACKLISTED = "AbandonBlacklisted";
    private const string COLUMN_ABANDON_VALUE_LOSS_SPEED = "AbandonValueLossSpeed";
    private const string COLUMN_ITEM_GUID = "Item";
    private const string COLUMN_CREW_SEATS_SEAT = "Index";
    private static readonly Schema[] SCHEMAS;
    static VehicleBay()
    {
        SCHEMAS = new Schema[9];
        SCHEMAS[0] = new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_MAP, SqlTypes.MAP_ID)
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_FACTION, SqlTypes.INCREMENT_KEY)
            {
                Nullable = true,
                ForeignKey = true,
                ForeignKeyColumn = FactionInfo.COLUMN_PK,
                ForeignKeyTable = FactionInfo.TABLE_MAIN
            },
            new Schema.Column(COLUMN_VEHICLE_GUID, SqlTypes.GUID_STRING),
            new Schema.Column(COLUMN_RESPAWN_TIME, SqlTypes.FLOAT)
            {
                Default = "600"
            },
            new Schema.Column(COLUMN_TICKET_COST, SqlTypes.INT)
            {
                Default = "0"
            },
            new Schema.Column(COLUMN_CREDIT_COST, SqlTypes.INT)
            {
                Default = "0"
            },
            new Schema.Column(COLUMN_REARM_COST, SqlTypes.INT)
            {
                Default = "3"
            },
            new Schema.Column(COLUMN_COOLDOWN, SqlTypes.FLOAT)
            {
                Default = "0"
            },
            new Schema.Column(COLUMN_VEHICLE_TYPE, "varchar(" + VehicleData.VEHICLE_TYPE_MAX_CHAR_LIMIT + ")")
            {
                Default = nameof(EVehicleType.NONE)
            },
            new Schema.Column(COLUMN_BRANCH, "varchar(" + KitEx.BRANCH_MAX_CHAR_LIMIT + ")")
            {
                Default = nameof(EBranch.DEFAULT)
            },
            new Schema.Column(COLUMN_REQUIRED_CLASS, "varchar(" + KitEx.CLASS_MAX_CHAR_LIMIT + ")")
            {
                Default = nameof(EClass.NONE)
            },
            new Schema.Column(COLUMN_REQUIRES_SQUADLEADER, SqlTypes.BOOLEAN),
            new Schema.Column(COLUMN_ABANDON_BLACKLISTED, SqlTypes.BOOLEAN),
            new Schema.Column(COLUMN_ABANDON_VALUE_LOSS_SPEED, SqlTypes.FLOAT)
            {
                Default = "0.125"
            },
        }, true, typeof(VehicleData));
        SCHEMAS[1] = BaseUnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[2] = Delay.GetDefaultSchema(TABLE_DELAYS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[3] = new Schema(TABLE_ITEMS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_EXT_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID_STRING)
        }, false, typeof(Guid));
        SCHEMAS[4] = F.GetListSchema<byte>(TABLE_CREW_SEATS, COLUMN_EXT_PK, COLUMN_CREW_SEATS_SEAT, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[5] = KitItem.GetDefaultSchema(TABLE_TRUNK_ITEMS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, includePage: false);
        Schema[] vbarrs = VBarricade.GetDefaultSchemas(TABLE_BARRICADES, TABLE_BARRICADE_ITEMS, TABLE_BARRICADE_DISPLAY_DATA, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, includeHealth: true);
        Array.Copy(vbarrs, 0, SCHEMAS, 6, vbarrs.Length);
    }

    [Obsolete]
    protected override async Task AddOrUpdateItem(VehicleData? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item == null)
        {
            if (!pk.IsValid)
                throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0;", new object[] { pk.Key }, token).ConfigureAwait(false);
            return;
        }
        bool hasPk = pk.IsValid;
        int pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 15 : 14];
        objs[0] = item.Map < 0 ? DBNull.Value : item.Map;
        objs[1] = item.Faction.IsValid ? item.Faction.Key : DBNull.Value;
        objs[2] = item.VehicleID.ToByteArray();
        objs[3] = item.RespawnTime;
        objs[4] = item.TicketCost;
        objs[5] = item.CreditCost;
        objs[6] = item.RearmCost;
        objs[7] = item.Cooldown;
        objs[8] = item.Branch.ToString();
        objs[9] = item.RequiredClass.ToString();
        objs[10] = item.Type.ToString();
        objs[11] = item.RequiresSL;
        objs[12] = item.DisallowAbandons;
        objs[13] = item.AbandonValueLossSpeed;
        if (hasPk)
            objs[13] = pk.Key;
        await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_MAP}`,`{COLUMN_FACTION}`,`{COLUMN_VEHICLE_GUID}`,`{COLUMN_RESPAWN_TIME}`," +
                             $"`{COLUMN_TICKET_COST}`,`{COLUMN_CREDIT_COST}`,`{COLUMN_REARM_COST}`,`{COLUMN_COOLDOWN}`," +
                             $"`{COLUMN_BRANCH}`,`{COLUMN_REQUIRED_CLASS}`,`{COLUMN_VEHICLE_TYPE}`,`{COLUMN_REQUIRES_SQUADLEADER}`," +
                             $"`{COLUMN_ABANDON_BLACKLISTED}`,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}`" +
                             (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
                             ") VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12,@13" +
                             (hasPk ? ",@14" : string.Empty) +
                             ") ON DUPLICATE KEY UPDATE " +
                             $"`{COLUMN_MAP}`=@0,`{COLUMN_FACTION}`=@1,`{COLUMN_RESPAWN_TIME}`=@3,`{COLUMN_TICKET_COST}`=@4," +
                             $"`{COLUMN_CREDIT_COST}`=@5,`{COLUMN_REARM_COST}`=@6,`{COLUMN_COOLDOWN}`=@7,`{COLUMN_BRANCH}`=@8,`{COLUMN_REQUIRED_CLASS}`=@9," +
                             $"`{COLUMN_VEHICLE_TYPE}`=@10,`{COLUMN_REQUIRES_SQUADLEADER}`=@11,`{COLUMN_ABANDON_BLACKLISTED}`=@12,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}`=@13," +
                             $"`{COLUMN_PK}`=LAST_INSERT_ID(`{COLUMN_PK}`);" +
                             "SET @pk := (SELECT LAST_INSERT_ID() as `pk`);" +
                             $"DELETE FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             $"DELETE FROM `{TABLE_DELAYS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             $"DELETE FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             $"DELETE FROM `{TABLE_CREW_SEATS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             $"DELETE FROM `{TABLE_BARRICADES}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             $"DELETE FROM `{TABLE_TRUNK_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@pk;" +
                             "SELECT @pk;",
            objs, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        pk = pk2;
        if (!pk.IsValid)
            throw new Exception("Unable to get a valid primary key for " + item + ".");
        item.PrimaryKey = pk;
        StringBuilder builder = new StringBuilder(128);
        if (item.Delays is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_DELAYS}` (`{Delay.COLUMN_TYPE}`,`{Delay.COLUMN_VALUE}`,`{Delay.COLUMN_GAMEMODE}`) VALUES ");
            objs = new object[item.Delays.Length * 3];
            for (int i = 0; i < item.Delays.Length; ++i)
            {
                Delay delay = item.Delays[i];
                int index = i * 3;
                objs[index] = delay.Type.ToString();
                objs[index + 1] = delay.Type switch
                {
                    EDelayType.OUT_OF_STAGING or EDelayType.NONE => DBNull.Value,
                    _ => delay.Value
                };
                objs[index + 2] = (object?)delay.Gamemode ?? DBNull.Value;
                if (i != 0)
                    builder.Append(',');
                builder.Append('(');
                for (int j = index; j < index + 3; ++j)
                {
                    if (j != index)
                        builder.Append(',');
                    builder.Append('@').Append(index);
                }
                builder.Append(')');
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token);
            builder.Clear();
        }

        if (item.Items is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_ITEMS}` (`{COLUMN_EXT_PK}`,`{COLUMN_ITEM_GUID}`) VALUES ");
            objs = new object[item.Items.Length * 2];
            for (int i = 0; i < item.Items.Length; ++i)
            {
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = item.Items[i];
                if (i != 0)
                    builder.Append(',');
                builder.Append('(');
                for (int j = index; j < index + 2; ++j)
                {
                    if (j != index)
                        builder.Append(',');
                    builder.Append('@').Append(index);
                }
                builder.Append(')');
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token);
            builder.Clear();
        }

        if (item.CrewSeats is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_CREW_SEATS}` (`{COLUMN_EXT_PK}`,`{COLUMN_CREW_SEATS_SEAT}`) VALUES ");
            objs = new object[item.CrewSeats.Length * 2];
            for (int i = 0; i < item.CrewSeats.Length; ++i)
            {
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = item.CrewSeats[i];
                if (i != 0)
                    builder.Append(',');
                builder.Append('(');
                for (int j = index; j < index + 2; ++j)
                {
                    if (j != index)
                        builder.Append(',');
                    builder.Append('@').Append(index);
                }
                builder.Append(')');
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token);
            builder.Clear();
        }

        if (item.UnlockRequirements is { Length: > 0 })
        {
            builder.Append($"INSERT INTO `{TABLE_UNLOCK_REQUIREMENTS}` (`{COLUMN_EXT_PK}`,`{BaseUnlockRequirement.COLUMN_JSON}`) VALUES ");
            objs = new object[item.UnlockRequirements.Length * 2];
            using MemoryStream str = new MemoryStream(48);
            for (int i = 0; i < item.UnlockRequirements.Length; ++i)
            {
                BaseUnlockRequirement req = item.UnlockRequirements[i];
                if (i != 0)
                    str.Seek(0L, SeekOrigin.Begin);
                Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
                BaseUnlockRequirement.Write(writer, req);
                writer.Dispose();
                string json = System.Text.Encoding.UTF8.GetString(str.GetBuffer(), 0, checked((int)str.Position));
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = json;
                if (i != 0)
                    builder.Append(',');
                builder.Append('(');
                for (int j = index; j < index + 2; ++j)
                {
                    if (j != index)
                        builder.Append(',');
                    builder.Append('@').Append(index);
                }
                builder.Append(')');
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token);
            builder.Clear();
        }
    }
    [Obsolete]
    protected override async Task<VehicleData[]> DownloadAllItems(CancellationToken token = default)
    {
        List<VehicleData> list = new List<VehicleData>(32);
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_MAP}`,`{COLUMN_FACTION}`,`{COLUMN_VEHICLE_GUID}`,`{COLUMN_RESPAWN_TIME}`," +
                             $"`{COLUMN_TICKET_COST}`,`{COLUMN_CREDIT_COST}`,`{COLUMN_REARM_COST}`,`{COLUMN_COOLDOWN}`," +
                             $"`{COLUMN_BRANCH}`,`{COLUMN_REQUIRED_CLASS}`,`{COLUMN_VEHICLE_TYPE}`,`{COLUMN_REQUIRES_SQUADLEADER}`," +
                             $"`{COLUMN_ABANDON_BLACKLISTED}`,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}` FROM `{TABLE_MAIN}` " +
                             $"WHERE (`{COLUMN_FACTION}` IS NULL OR `{COLUMN_FACTION}`=@0 OR `{COLUMN_FACTION}`=@1) AND " +
                             $"(`{COLUMN_MAP}` IS NULL OR `{COLUMN_MAP}`=@2);",
            new object[]
            {
                TeamManager.Team1Faction.PrimaryKey.Key, TeamManager.Team2Faction.PrimaryKey.Key, MapScheduler.Current
            },
            reader =>
            {
                Guid? guid = reader.ReadGuidString(3);
                if (!guid.HasValue)
                    throw new FormatException("Invalid GUID: " + reader.GetString(3));

                VehicleData data = new VehicleData
                {
                    PrimaryKey = reader.GetInt32(0),
                    VehicleID = guid.Value,
                    Map = reader.GetInt32(1),
                    Faction = reader.GetInt32(2),
                    RespawnTime = reader.GetFloat(4),
                    TicketCost = reader.GetInt32(5),
                    CreditCost = reader.GetInt32(6),
                    RearmCost = reader.GetInt32(7),
                    Cooldown = reader.GetFloat(8),
                    Branch = reader.ReadStringEnum(9, EBranch.DEFAULT),
                    RequiredClass = reader.ReadStringEnum(10, EClass.NONE),
                    Type = reader.ReadStringEnum(11, EVehicleType.NONE),
                    RequiresSL = reader.GetBoolean(12),
                    DisallowAbandons = reader.GetBoolean(13),
                    AbandonValueLossSpeed = reader.GetFloat(14)
                };
                data.Name = Assets.find(data.VehicleID)?.FriendlyName ?? data.VehicleID.ToString("N");
                list.Add(data);
            }, token).ConfigureAwait(false);
        if (list.Count == 0)
            return list.ToArray();
        StringBuilder sb = new StringBuilder("IN (", 6 + list.Count * 4);
        object[] pkeyObjs = new object[list.Count];
        for (int i = 0; i < list.Count; ++i)
        {
            if (i != 0)
                sb.Append(',');
            pkeyObjs[i] = list[i].PrimaryKey.Key;
            sb.Append('@').Append(i.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append(");");
        string pkeys = sb.ToString();
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{Delay.COLUMN_TYPE}`,`{Delay.COLUMN_VALUE}`,`{Delay.COLUMN_GAMEMODE}` " +
                             $"FROM `{TABLE_DELAYS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         EDelayType type = reader.ReadStringEnum(1, EDelayType.NONE);
                                         if (type == EDelayType.NONE)
                                             break;
                                         Util.AddToArray(ref list[i].Delays!, new Delay(type, reader.IsDBNull(2) ? 0f : reader.GetFloat(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{BaseUnlockRequirement.COLUMN_JSON}` " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reader.GetString(1));
                                         Utf8JsonReader reader2 = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                                         BaseUnlockRequirement? req = BaseUnlockRequirement.Read(ref reader2);
                                         if (req != null) return;
                                         Util.AddToArray(ref list[i].UnlockRequirements!, req);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{COLUMN_ITEM_GUID}` " +
                             $"FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         Guid? guid = reader.ReadGuidString(1);
                                         if (!guid.HasValue)
                                             throw new FormatException("Invalid GUID: " + reader.GetString(1));
                                         Util.AddToArray(ref list[i].Items!, guid.Value);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{COLUMN_CREW_SEATS_SEAT}` " +
                             $"FROM `{TABLE_CREW_SEATS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         byte seat = reader.GetByte(1);
                                         Util.AddToArray(ref list[i].CrewSeats!, seat);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{KitItem.COLUMN_GUID}`,`{KitItem.COLUMN_X}`," +
                             $"`{KitItem.COLUMN_Y}`,`{KitItem.COLUMN_ROTATION}`,`{KitItem.COLUMN_AMOUNT}`,`{KitItem.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_TRUNK_ITEMS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         VehicleData data = list[i];
                                         data.Metadata ??= new MetaSave();
                                         data.Metadata.TrunkItems ??= new List<KitItem>(8);
                                         KitItem item = new KitItem(reader.ReadGuid(1), reader.GetByte(2), reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(6), reader.GetByte(5), PlayerInventory.STORAGE);
                                         data.Metadata.TrunkItems.Add(item);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        List<object> objs2 = new List<object>(list.Count * 2);
        sb.Clear();
        sb.Append("IN (");
        bool f = false;
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_GUID}`,`{VBarricade.COLUMN_HEALTH}`," +
                             $"`{VBarricade.COLUMN_POS_X}`,`{VBarricade.COLUMN_POS_Y}`,`{VBarricade.COLUMN_POS_Z}`," +
                             $"`{VBarricade.COLUMN_ROT_X}`,`{VBarricade.COLUMN_ROT_Y}`,`{VBarricade.COLUMN_ROT_Z}`," +
                             $"`{VBarricade.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_BARRICADES}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 int bpk = reader.GetInt32(1);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         VehicleData data = list[i];
                                         data.Metadata ??= new MetaSave();
                                         data.Metadata.Barricades ??= new List<VBarricade>(4);
                                         VBarricade barricade = new VBarricade(reader.ReadGuid(2), reader.GetUInt16(3),
                                             reader.GetFloat(4), reader.GetFloat(5), reader.GetFloat(6),
                                             reader.GetFloat(7), reader.GetFloat(8), reader.GetFloat(9),
                                             reader.ReadByteArray(10))
                                         {
                                             PrimaryKey = bpk,
                                             LinkedKey = pk
                                         };
                                         data.Metadata.Barricades.Add(barricade);
                                         if (f)
                                             sb.Append(',');
                                         else f = true;
                                         sb.Append('@').Append(objs2.Count.ToString(Data.AdminLocale));
                                         objs2.Add(bpk);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        sb.Append(");");
        pkeys = sb.ToString();
        sb = null!;
        pkeyObjs = objs2.ToArray();
        List<ItemJarData> items = new List<ItemJarData>(32);
        List<ItemDisplayData> display = new List<ItemDisplayData>(16);
        await Sql.QueryAsync(
            $"SELECT `{VBarricade.COLUMN_ITEM_PK}`,`{VBarricade.COLUMN_ITEM_BARRICADE_PK}`,`{VBarricade.COLUMN_ITEM_GUID}`," +
            $"`{VBarricade.COLUMN_ITEM_POS_X}`,`{VBarricade.COLUMN_ITEM_POS_Y}`,`{VBarricade.COLUMN_ITEM_ROT}`,`{VBarricade.COLUMN_ITEM_AMOUNT}`," +
            $"`{VBarricade.COLUMN_ITEM_QUALITY}`,`{VBarricade.COLUMN_ITEM_METADATA}` " +
            $"FROM `{TABLE_BARRICADE_ITEMS}` WHERE `{VBarricade.COLUMN_ITEM_BARRICADE_PK}` IN " +
            pkeys, pkeyObjs,
            reader =>
            {
                items.Add(new ItemJarData(reader.GetInt32(0), reader.GetInt32(1), reader.ReadGuid(2), reader.GetByte(3),
                    reader.GetByte(4), reader.GetByte(5), reader.GetByte(6), reader.GetByte(7),
                    reader.ReadByteArray(8)));
            }, token);
        await Sql.QueryAsync(
            $"SELECT `{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_DISPLAY_SKIN}`,`{VBarricade.COLUMN_DISPLAY_MYTHIC}`," +
            $"`{VBarricade.COLUMN_DISPLAY_ROT}`,`{VBarricade.COLUMN_DISPLAY_TAGS}`,`{VBarricade.COLUMN_DISPLAY_DYNAMIC_PROPS}` " +
            $"FROM `{TABLE_BARRICADE_DISPLAY_DATA}` WHERE `{VBarricade.COLUMN_PK}` IN " +
            pkeys, pkeyObjs,
            reader =>
            {
                display.Add(new ItemDisplayData(reader.GetInt32(0), reader.GetUInt16(1), reader.GetUInt16(2), reader.GetByte(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5)));
            }, token);

        List<ItemJarData> current = new List<ItemJarData>(32);
        for (int i = 0; i < list.Count; ++i)
        {
            if (list[i].Metadata?.Barricades == null)
                continue;
            List<VBarricade> barricades = list[i].Metadata!.Barricades!;
            for (int k = 0; k < barricades.Count; ++k)
            {
                VBarricade b = barricades[k];
                int pk = b.PrimaryKey.Key;
                f = true;
                for (int j = 0; j < items.Count; ++j)
                {
                    if (items[j].Structure.Key == pk)
                    {
                        if (f)
                        {
                            current.Clear();
                            f = false;
                        }
                        current.Add(items[j]);
                        items.RemoveAtFast(j);
                        --j;
                    }
                }

                ItemDisplayData data = display.Find(x => x.Key.Key == pk);
                if (f && data.Key.Key != pk)
                    continue;
                if (f)
                    current.Clear();
                else if (current.Count > byte.MaxValue)
                    current.RemoveRange(byte.MaxValue - 1, current.Count - byte.MaxValue - 1);
                int ct = 17;
                for (int j = 0; j < current.Count; ++j)
                    ct += 8 + current[j].Metadata.Length;
                if (data.Key.Key == pk)
                {
                    ct += 7;
                    if (!string.IsNullOrEmpty(data.Tags))
                        ct += data.Tags!.Length;
                    if (!string.IsNullOrEmpty(data.DynamicProps))
                        ct += data.DynamicProps!.Length;
                }
                byte[] state = new byte[ct];
                int index = 16;
                state[++index] = (byte)current.Count;
                for (int j = 0; j < current.Count; ++j)
                {
                    ItemJarData jar = current[j];
                    state[++index] = jar.X;
                    state[++index] = jar.Y;
                    state[++index] = jar.Rotation;
                    if (Assets.find(jar.Item) is ItemAsset item)
                        Buffer.BlockCopy(BitConverter.GetBytes(item.id), 0, state, index + 1, sizeof(ushort));
                    else L.LogWarning("Unable to find item: " + jar.Item.ToString("N"));
                    index += sizeof(ushort);
                    state[++index] = jar.Amount;
                    state[++index] = jar.Quality;
                    if (jar.Metadata is { Length: > 0 })
                    {
                        state[++index] = (byte)Math.Min(jar.Metadata.Length, byte.MaxValue);
                        Buffer.BlockCopy(jar.Metadata, 0, state, index + 1, state[index]);
                        index += state[index];
                    }
                    else ++index;
                }
                if (data.Key.Key == pk)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(data.Skin), 0, state, index + 1, sizeof(ushort));
                    Buffer.BlockCopy(BitConverter.GetBytes(data.Mythic), 0, state, index + 3, sizeof(ushort));
                    index += sizeof(ushort) * 2;
                    if (!string.IsNullOrEmpty(data.Tags))
                    {
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.Tags!);
                        state[++index] = checked((byte)bytes.Length);
                        Buffer.BlockCopy(bytes, 0, state, index + 1, bytes.Length);
                        index += bytes.Length;
                    }
                    else ++index;
                    if (!string.IsNullOrEmpty(data.DynamicProps))
                    {
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.DynamicProps!);
                        state[++index] = checked((byte)bytes.Length);
                        Buffer.BlockCopy(bytes, 0, state, index + 1, bytes.Length);
                        index += bytes.Length;
                    }
                    else ++index;

                    state[index + 1] = data.Rotation;
                }

                b.Metadata = state;
            }
        }

        return list.ToArray();
    }
    [Obsolete]
    protected override async Task<VehicleData?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        VehicleData? obj = null;
        if (!pk.IsValid)
            throw new ArgumentException("Primary key is not valid.", nameof(pk));
        int pk2 = pk;
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_MAP}`,`{COLUMN_FACTION}`,`{COLUMN_VEHICLE_GUID}`,`{COLUMN_RESPAWN_TIME}`," +
                             $"`{COLUMN_TICKET_COST}`,`{COLUMN_CREDIT_COST}`,`{COLUMN_REARM_COST}`,`{COLUMN_COOLDOWN}`," +
                             $"`{COLUMN_BRANCH}`,`{COLUMN_REQUIRED_CLASS}`,`{COLUMN_VEHICLE_TYPE}`,`{COLUMN_REQUIRES_SQUADLEADER}`," +
                             $"`{COLUMN_ABANDON_BLACKLISTED}`,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}` FROM `{TABLE_MAIN}` " +
                             $"WHERE `{COLUMN_PK}`=@0 LIMIT 1;",
            new object[] { pk2 },
            reader =>
            {
                Guid? guid = reader.ReadGuidString(3);
                if (!guid.HasValue)
                    throw new FormatException("Invalid GUID: " + reader.GetString(3));

                obj = new VehicleData
                {
                    PrimaryKey = reader.GetInt32(0),
                    VehicleID = guid.Value,
                    Map = reader.GetInt32(1),
                    Faction = reader.GetInt32(2),
                    RespawnTime = reader.GetFloat(4),
                    TicketCost = reader.GetInt32(5),
                    CreditCost = reader.GetInt32(6),
                    RearmCost = reader.GetInt32(7),
                    Cooldown = reader.GetFloat(8),
                    Branch = reader.ReadStringEnum(9, EBranch.DEFAULT),
                    RequiredClass = reader.ReadStringEnum(10, EClass.NONE),
                    Type = reader.ReadStringEnum(11, EVehicleType.NONE),
                    RequiresSL = reader.GetBoolean(12),
                    DisallowAbandons = reader.GetBoolean(13),
                    AbandonValueLossSpeed = reader.GetFloat(14)
                };
                return true;
            }, token).ConfigureAwait(false);
        if (obj == null)
            return null;
        if (!obj.PrimaryKey.IsValid)
            return obj;
        object[] pkeyObj = { obj.PrimaryKey.Key };
        await Sql.QueryAsync($"SELECT `{Delay.COLUMN_TYPE}`,`{Delay.COLUMN_VALUE}`,`{Delay.COLUMN_GAMEMODE}` " +
                             $"FROM `{TABLE_DELAYS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 EDelayType type = reader.ReadStringEnum(0, EDelayType.NONE);
                                 if (type == EDelayType.NONE)
                                     return;
                                 Util.AddToArray(ref obj.Delays!, new Delay(type, reader.IsDBNull(1) ? 0f : reader.GetFloat(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{BaseUnlockRequirement.COLUMN_JSON}` " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reader.GetString(0));
                                 Utf8JsonReader reader2 = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                                 BaseUnlockRequirement? req = BaseUnlockRequirement.Read(ref reader2);
                                 if (req != null) return;
                                 Util.AddToArray(ref obj.UnlockRequirements!, req);
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_ITEM_GUID}` " +
                             $"FROM `{TABLE_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 Guid? guid = reader.ReadGuidString(0);
                                 if (!guid.HasValue)
                                     throw new FormatException("Invalid GUID: " + reader.GetString(0));
                                 Util.AddToArray(ref obj.Items!, guid.Value);
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_CREW_SEATS_SEAT}` " +
                             $"FROM `{TABLE_CREW_SEATS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 byte seat = reader.GetByte(0);
                                 Util.AddToArray(ref obj.CrewSeats!, seat);
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{KitItem.COLUMN_GUID}`,`{KitItem.COLUMN_X}`," +
                             $"`{KitItem.COLUMN_Y}`,`{KitItem.COLUMN_ROTATION}`,`{KitItem.COLUMN_AMOUNT}`,`{KitItem.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_TRUNK_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 obj.Metadata ??= new MetaSave();
                                 obj.Metadata.TrunkItems ??= new List<KitItem>(8);
                                 KitItem item = new KitItem(reader.ReadGuid(1), reader.GetByte(2), reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(6), reader.GetByte(5), PlayerInventory.STORAGE);
                                 obj.Metadata.TrunkItems.Add(item);
                             }, token).ConfigureAwait(false);
        List<object> objs2 = new List<object>(4);
        StringBuilder? sb = null;
        bool f = false;
        await Sql.QueryAsync($"SELECT `{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_GUID}`,`{VBarricade.COLUMN_HEALTH}`," +
                             $"`{VBarricade.COLUMN_POS_X}`,`{VBarricade.COLUMN_POS_Y}`,`{VBarricade.COLUMN_POS_Z}`," +
                             $"`{VBarricade.COLUMN_ROT_X}`,`{VBarricade.COLUMN_ROT_Y}`,`{VBarricade.COLUMN_ROT_Z}`," +
                             $"`{VBarricade.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_BARRICADES}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 int bpk = reader.GetInt32(0);
                                 obj.Metadata ??= new MetaSave();
                                 obj.Metadata.Barricades ??= new List<VBarricade>(4);
                                 VBarricade barricade = new VBarricade(reader.ReadGuid(1), reader.GetUInt16(2),
                                     reader.GetFloat(3), reader.GetFloat(4), reader.GetFloat(5),
                                     reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8),
                                     reader.ReadByteArray(9))
                                 {
                                     PrimaryKey = bpk,
                                     LinkedKey = pk
                                 };
                                 obj.Metadata.Barricades.Add(barricade);
                                 sb ??= new StringBuilder("IN (", 20);
                                 if (f)
                                     sb.Append(',');
                                 else f = true;
                                 sb.Append('@').Append(objs2.Count.ToString(Data.AdminLocale));
                                 objs2.Add(bpk);
                             }, token).ConfigureAwait(false);
        if (f)
            sb!.Append(");");
        else goto skipBarricades;
        string pkeys = sb.ToString();
        sb = null!;
        pkeyObj = objs2.ToArray();
        List<ItemJarData>? items = null;
        List<ItemDisplayData>? display = null;
        await Sql.QueryAsync(
            $"SELECT `{VBarricade.COLUMN_ITEM_PK}`,`{VBarricade.COLUMN_ITEM_BARRICADE_PK}`,`{VBarricade.COLUMN_ITEM_GUID}`," +
            $"`{VBarricade.COLUMN_ITEM_POS_X}`,`{VBarricade.COLUMN_ITEM_POS_Y}`,`{VBarricade.COLUMN_ITEM_ROT}`,`{VBarricade.COLUMN_ITEM_AMOUNT}`," +
            $"`{VBarricade.COLUMN_ITEM_QUALITY}`,`{VBarricade.COLUMN_ITEM_METADATA}` " +
            $"FROM `{TABLE_BARRICADE_ITEMS}` WHERE `{VBarricade.COLUMN_ITEM_BARRICADE_PK}` IN " +
            pkeys, pkeyObj,
            reader =>
            {
                (items ??= new List<ItemJarData>(16)).Add(new ItemJarData(reader.GetInt32(0), reader.GetInt32(1), reader.ReadGuid(2), reader.GetByte(3),
                    reader.GetByte(4), reader.GetByte(5), reader.GetByte(6), reader.GetByte(7),
                    reader.ReadByteArray(8)));
            }, token);
        await Sql.QueryAsync(
            $"SELECT `{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_DISPLAY_SKIN}`,`{VBarricade.COLUMN_DISPLAY_MYTHIC}`," +
            $"`{VBarricade.COLUMN_DISPLAY_ROT}`,`{VBarricade.COLUMN_DISPLAY_TAGS}`,`{VBarricade.COLUMN_DISPLAY_DYNAMIC_PROPS}` " +
            $"FROM `{TABLE_BARRICADE_DISPLAY_DATA}` WHERE `{VBarricade.COLUMN_PK}` IN " +
            pkeys, pkeyObj,
            reader =>
            {
                (display ??= new List<ItemDisplayData>(obj.Metadata!.Barricades!.Count)).Add(new ItemDisplayData(reader.GetInt32(0), reader.GetUInt16(1), reader.GetUInt16(2), reader.GetByte(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5)));
            }, token);

        List<VBarricade> barricades = obj.Metadata!.Barricades!;
        List<ItemJarData>? current = null;
        for (int k = 0; k < barricades.Count; ++k)
        {
            VBarricade b = barricades[k];
            int pk3 = b.PrimaryKey.Key;
            f = true;
            if (items != null)
            {
                for (int j = 0; j < items.Count; ++j)
                {
                    if (items[j].Structure.Key == pk3)
                    {
                        if (f)
                        {
                            current ??= new List<ItemJarData>(32);
                            current.Clear();
                            f = false;
                        }
                        current!.Add(items[j]);
                        items.RemoveAtFast(j);
                        --j;
                    }
                }
            }

            ItemDisplayData data = display == null ? default : display.Find(x => x.Key.Key == pk);
            if (f && data.Key.Key != pk)
                continue;
            if (f)
            {
                if (current != null)
                    current.Clear();
                else current = new List<ItemJarData>(8);
            }
            else if (current!.Count > byte.MaxValue)
                current.RemoveRange(byte.MaxValue - 1, current.Count - byte.MaxValue - 1);
            int ct = 17;
            for (int j = 0; j < current.Count; ++j)
                ct += 8 + current[j].Metadata.Length;
            if (data.Key.Key == pk)
            {
                ct += 7;
                if (!string.IsNullOrEmpty(data.Tags))
                    ct += data.Tags!.Length;
                if (!string.IsNullOrEmpty(data.DynamicProps))
                    ct += data.DynamicProps!.Length;
            }
            byte[] state = new byte[ct];
            int index = 16;
            state[++index] = (byte)current.Count;
            for (int j = 0; j < current.Count; ++j)
            {
                ItemJarData jar = current[j];
                state[++index] = jar.X;
                state[++index] = jar.Y;
                state[++index] = jar.Rotation;
                if (Assets.find(jar.Item) is ItemAsset item)
                    Buffer.BlockCopy(BitConverter.GetBytes(item.id), 0, state, index + 1, sizeof(ushort));
                else L.LogWarning("Unable to find item: " + jar.Item.ToString("N"));
                index += sizeof(ushort);
                state[++index] = jar.Amount;
                state[++index] = jar.Quality;
                if (jar.Metadata is { Length: > 0 })
                {
                    state[++index] = (byte)Math.Min(jar.Metadata.Length, byte.MaxValue);
                    Buffer.BlockCopy(jar.Metadata, 0, state, index + 1, state[index]);
                    index += state[index];
                }
                else ++index;
            }
            if (data.Key.Key == pk)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data.Skin), 0, state, index + 1, sizeof(ushort));
                Buffer.BlockCopy(BitConverter.GetBytes(data.Mythic), 0, state, index + 3, sizeof(ushort));
                index += sizeof(ushort) * 2;
                if (!string.IsNullOrEmpty(data.Tags))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.Tags!);
                    state[++index] = checked((byte)bytes.Length);
                    Buffer.BlockCopy(bytes, 0, state, index + 1, bytes.Length);
                    index += bytes.Length;
                }
                else ++index;
                if (!string.IsNullOrEmpty(data.DynamicProps))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data.DynamicProps!);
                    state[++index] = checked((byte)bytes.Length);
                    Buffer.BlockCopy(bytes, 0, state, index + 1, bytes.Length);
                    index += bytes.Length;
                }
                else ++index;

                state[index + 1] = data.Rotation;
            }

            b.Metadata = state;
        }
        skipBarricades:
        return obj;
    }
    #endregion
}