﻿#if false
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
public class VehicleSpawnerOld : ListSqlSingleton<VehicleSpawn>, ILevelStartListenerAsync, IGameStartListener, IStagingPhaseOverListener, ITimeSyncListener, IFlagCapturedListener, IFlagNeutralizedListener, ICacheDiscoveredListener, ICacheDestroyedListener
{
    private static readonly List<InteractableVehicle> NearbyTempOutput = new List<InteractableVehicle>(4);
    public const ushort MaxBatteryCharge = 10000;
    public const float VehicleHeightOffset = 5f;
    
    public override MySqlDatabase Sql => Data.AdminSql;
    public override bool AwaitLoad => true;
    public VehicleSpawner() : base("vehiclespawns", SCHEMAS) { }
    public static VehicleSpawner? GetSingletonQuick() => Data.Is(out IVehicles r) ? r.VehicleSpawner : null;
    public override Task PreLoad(CancellationToken token)
    {
        EventDispatcher.EnterVehicleRequested += OnVehicleEnterRequested;
        EventDispatcher.VehicleSwapSeatRequested += OnVehicleSwapSeatRequested;
        EventDispatcher.ExitVehicleRequested += OnVehicleExitRequested;
        EventDispatcher.ExitVehicle += OnVehicleExit;
        TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
        TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.StructureDestroyed += OnStructureDestroyed;
        EventDispatcher.VehicleDestroyed += OnVehicleDestroyed;
        UCPlayerKeys.SubscribeKeyDown(DropFlaresStart, Data.Keys.SpawnCountermeasures);
        UCPlayerKeys.SubscribeKeyUp(DropFlaresStop, Data.Keys.SpawnCountermeasures);
        return base.PreLoad(token);
    }
    public override Task PreUnload(CancellationToken token)
    {
        UCPlayerKeys.UnsubscribeKeyDown(DropFlaresStart, Data.Keys.SpawnCountermeasures);
        UCPlayerKeys.UnsubscribeKeyUp(DropFlaresStop, Data.Keys.SpawnCountermeasures);
        EventDispatcher.VehicleDestroyed -= OnVehicleDestroyed;
        EventDispatcher.StructureDestroyed -= OnStructureDestroyed;
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
        EventDispatcher.ExitVehicle -= OnVehicleExit;
        EventDispatcher.ExitVehicleRequested -= OnVehicleExitRequested;
        EventDispatcher.VehicleSwapSeatRequested -= OnVehicleSwapSeatRequested;
        EventDispatcher.EnterVehicleRequested -= OnVehicleEnterRequested;
        return base.PreUnload(token);
    }
    public static bool CanUseCountermeasures(InteractableVehicle vehicle) => true;
    private static readonly List<BarricadeDrop> WorkingToUpdate = new List<BarricadeDrop>(32);
    private void OnPlayerEnterMain(UCPlayer player, ulong team)
    {
        GameThread.AssertCurrent();
        try
        {
            WriteWait();
            try
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    SavedStructure? sign = Items[i].Item?.Sign?.Item;
                    if (sign?.Buildable?.Drop is not BarricadeDrop drop)
                        continue;
                    if (team == 1 && TeamManager.Team1Main.IsInside(sign.Position) || team == 2 && TeamManager.Team2Main.IsInside(sign.Position))
                        WorkingToUpdate.Add(drop);
                }
            }
            finally
            {
                WriteRelease();
            }
            for (int i = 0; i < WorkingToUpdate.Count; ++i)
                Signs.SendSignUpdate(WorkingToUpdate[i], player, false);
        }
        finally
        {
            WorkingToUpdate.Clear();
        }
        InteractableVehicle? vehicle = player.CurrentVehicle;
        if (vehicle == null)
            return;
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return;
        VehicleData? data = bay.GetDataSync(vehicle.asset.GUID);
        if ((data == null || !VehicleBay.CanSoloVehicle(data)) && IsOnlyPassenger(player, out byte seat))
        {
            ActionLog.Add(ActionLogType.SoloRTB, (seat == 0 ? "Driver of " : "Passenger of ") + ActionLog.AsAsset(vehicle.asset) +
                                                      "." + (seat == 0 ? string.Empty : " Seat: " + seat.ToString(CultureInfo.InvariantCulture) + "."), player);
        }
    }
    private static void OnPlayerLeftMain(UCPlayer player, ulong team)
    {
        EnsureVehicleLocked(player);
        InteractableVehicle? vehicle = player.CurrentVehicle;
        if (vehicle == null)
            return;
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return;
        VehicleData? data = bay.GetDataSync(vehicle.asset.GUID);
        if ((data == null || !VehicleBay.CanSoloVehicle(data)) && IsOnlyPassenger(player, out byte seat))
        {
            ActionLog.Add(ActionLogType.PossibleSolo, (seat == 0 ? "Driver of " : "Passenger of ") + ActionLog.AsAsset(vehicle.asset) +
                                                           "." + (seat == 0 ? string.Empty : " Seat: " + seat.ToString(CultureInfo.InvariantCulture) + "."), player);
        }
    }
    private void OnVehicleDestroyed(VehicleDestroyed e)
    {
        if (TryGetSpawn(e.Vehicle, out SqlItem<VehicleSpawn> spawn))
        {
            WriteWait();
            try
            {
                VehicleSpawn? sp = spawn.Item;
                if (sp == null)
                    return;
                sp.LinkedVehicle = null;
                sp.UpdateSign();
            }
            finally
            {
                WriteRelease();
            }
        }
    }
    void ICacheDiscoveredListener.OnCacheDiscovered(Components.Cache cache) => UpdateFlagSigns();
    void ICacheDestroyedListener.OnCacheDestroyed(Components.Cache cache) => UpdateFlagSigns();
    void IFlagNeutralizedListener.OnFlagNeutralized(Flag flag, ulong newOwner, ulong oldOwner) => UpdateFlagSigns();
    void IFlagCapturedListener.OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner) => UpdateFlagSigns();
    private void UpdateFlagSigns()
    {
        GameThread.AssertCurrent();
        Signs.UpdateVehicleBaySigns(null);
    }
    private void DropFlaresStart(UCPlayer player, /*float timeDown, */ref bool handled)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            player.Player.movement.getSeat() == 0 &&
            (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && CanUseCountermeasures(vehicle) &&
            vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.TryDropFlares();
        }
    }
    private void DropFlaresStop(UCPlayer player, float timeDown, ref bool handled)
    {
#if false
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            player.Player.movement.getSeat() == 0 &&
            (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && CanUseCountermeasures(vehicle) &&
            vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            // TODO: this method isn't really needed anymore
        }
#endif
    }

    public static bool IsOnlyPassenger(UCPlayer player, out byte playerSeat, byte inSeat = byte.MaxValue)
    {
        playerSeat = byte.MaxValue;
        if (!player.IsOnline)
        {
            return false;
        }

        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle == null || vehicle.isDead)
        {
            return false;
        }

        byte seat = player.Player.movement.getSeat();
        if (inSeat != byte.MaxValue && seat != inSeat)
        {
            return false;
        }

        for (int i = 0; i < vehicle.passengers.Length; ++i)
        {
            if (i != seat && vehicle.passengers[i].player != null)
                return false;
        }

        playerSeat = seat;
        return true;
    }
    async Task ILevelStartListenerAsync.OnLevelReady(CancellationToken token)
    {
        GameThread.AssertCurrent();
        WriteWait();
        try
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Item is not { } spawn) continue;
                SqlItem<VehicleData>? data = spawn.Vehicle;
                if (data?.Item is null)
                {
                    L.LogError("[VEH SPAWNER] Error initializing spawn " + spawn + ", vehicle data " + spawn.VehicleKey + " not found.");
                    continue;
                }
                if (spawn.Structure?.Item is not { } bayStr)
                {
                    L.LogError("[VEH SPAWNER] Error initializing spawn " + spawn + ", bay structure " + spawn.StructureKey + " not found.");
                    continue;
                }

                if (bayStr.Buildable is null)
                    L.LogError("[VEH SPAWNER] Error initializing spawn " + spawn + ", bay structure " + spawn.StructureKey + " is not yet initialized.");
                else
                {
                    if (!bayStr.Buildable.Model.TryGetComponent(out VehicleBayComponent comp))
                        comp = bayStr.Buildable.Model.gameObject.AddComponent<VehicleBayComponent>();
                    comp.Init(Items[i], data);
                }
                if (spawn.Sign?.Item is null && spawn.SignKey.IsValid)
                {
                    L.LogWarning("[VEH SPAWNER] Error initializing spawn " + spawn + ", sign " + spawn.SignKey + " not found.");
                }
            }
        }
        finally
        {
            WriteRelease();
        }

        await RespawnAllVehicles(token).ConfigureAwait(false);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        Signs.UpdateVehicleBaySigns(null);
    }
    /// <summary>Locks <see cref="VehicleSpawner"/> write semaphore.</summary>
    void IStagingPhaseOverListener.OnStagingPhaseOver()
    {
        Signs.UpdateVehicleBaySigns(null);
    }
    /// <summary>Locks <see cref="VehicleSpawner"/> write semaphore.</summary>
    private void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(e.Barricade.asset.GUID) && TryGetSpawn(e.Barricade, out SqlItem<VehicleSpawn> spawn))
        {
            Guid guid = spawn.Item is { Vehicle.Item: { } vehicle } ? vehicle.VehicleID : Guid.Empty;
            UCWarfare.RunTask(RemoveSpawn, spawn, Data.Gamemode.UnloadToken, "Spawn destroyed, removing from database.");
            L.LogDebug("Vehicle spawn {" + guid.ToString("N") + "} #" + spawn.LastPrimaryKey.ToString(CultureInfo.InvariantCulture) + " deregistered because the barricade was salvaged or destroyed.");
            if (Assets.find(guid) is VehicleAsset asset)
            {
                ActionLog.Add(ActionLogType.DeregisteredSpawn,
                    $"{ActionLog.AsAsset(asset)} - " +
                    $"DEREGISTERED SPAWN {spawn.LastPrimaryKey.ToString(CultureInfo.InvariantCulture)} (BARRICADE ID: {e.InstanceID
                        .ToString(CultureInfo.InvariantCulture)}).", e.Instigator);
            }
            else
            {
                ActionLog.Add(ActionLogType.DeregisteredSpawn, $"{guid:N} - DEREGISTERED SPAWN {spawn.LastPrimaryKey
                    .ToString(CultureInfo.InvariantCulture)} (BARRICADE ID: {e.InstanceID.ToString(CultureInfo.InvariantCulture)}).", e.Instigator);
            }
        }
    }
    /// <summary>Locks <see cref="VehicleSpawner"/> write semaphore.</summary>
    private void OnStructureDestroyed(StructureDestroyed e)
    {
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(e.Structure.asset.GUID) && TryGetSpawn(e.Structure, out SqlItem<VehicleSpawn> spawn))
        {
            Guid guid = spawn.Item is { Vehicle.Item: { } vehicle } ? vehicle.VehicleID : Guid.Empty;
            UCWarfare.RunTask(RemoveSpawn, spawn, Data.Gamemode.UnloadToken, "Spawn destroyed, removing from database.");
            L.LogDebug("Vehicle spawn {" + guid.ToString("N") + "} #" + spawn.LastPrimaryKey.ToString(CultureInfo.InvariantCulture) + " deregistered because the structure was salvaged or destroyed.");
            if (Assets.find(guid) is VehicleAsset asset)
            {
                ActionLog.Add(ActionLogType.DeregisteredSpawn,
                    $"{ActionLog.AsAsset(asset)} - " +
                    $"DEREGISTERED SPAWN {spawn.LastPrimaryKey.ToString(CultureInfo.InvariantCulture)} (STRUCTURE ID: {e.InstanceID
                        .ToString(CultureInfo.InvariantCulture)}).", e.Instigator);
            }
            else
            {
                ActionLog.Add(ActionLogType.DeregisteredSpawn, $"{guid:N} - DEREGISTERED SPAWN {spawn.LastPrimaryKey
                    .ToString(CultureInfo.InvariantCulture)} (STRUCTURE ID: {e.InstanceID.ToString(CultureInfo.InvariantCulture)}).", e.Instigator);
            }
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task RemoveSpawn(VehicleSpawn spawn, CancellationToken token = default)
    {
        if (spawn == null)
            throw new ArgumentNullException(nameof(spawn));
        SqlItem<VehicleSpawn>? proxy = await FindProxy(spawn.PrimaryKey, token).ConfigureAwait(false);
        if (proxy is not null)
            await RemoveSpawn(proxy, token).ConfigureAwait(false);
    }
    /// <remarks>Thread Safe</remarks>
    public async Task RemoveSpawn(SqlItem<VehicleSpawn> spawn, CancellationToken token = default)
    {
        if (spawn is null)
            throw new ArgumentNullException(nameof(spawn));
        VehicleSpawn? spawnItem = spawn.Item;
        VehicleData? data = spawnItem?.Vehicle?.Item;
        await spawn.Delete(token).ConfigureAwait(false);
        if (data is null && spawnItem is null) return;
        await UniTask.SwitchToMainThread(token);
        if (data != null)
        {
            Signs.UpdateVehicleBaySigns(null, spawnItem);
        }
        if (spawnItem != null)
        {
            Transform? t = spawnItem.Structure?.Item?.Buildable?.Model;
            if (t != null && t.TryGetComponent(out VehicleBayComponent comp))
                UnityEngine.Object.Destroy(comp);
        }
    }
    /// <remarks>Thread Safe</remarks>
    /// <remarks>Do not call in <see cref="VehicleSpawner"/> write wait.</remarks>
    public async Task RespawnAllVehicles(CancellationToken token = default)
    {
        L.Log("Respawning vehicles...", ConsoleColor.Magenta);
        await UniTask.SwitchToMainThread(token);
        DeleteAllVehiclesFromWorld();

        WriteWait();
        List<VehicleSpawn> spawns = new List<VehicleSpawn>(Items.Count);
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                VehicleSpawn? spawn = Items[i].Item;
                if (spawn is null) continue;
                spawns.Add(spawn);
            }
        }
        finally
        {
            WriteRelease();
        }

        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < spawns.Count; ++i)
            {
                await spawns[i].SpawnVehicle(token).ConfigureAwait(false);
            }
        }
        finally
        {
            Release();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<SqlItem<VehicleSpawn>> CreateSpawn(SqlItem<SavedStructure> structure, SqlItem<VehicleData> vehicle, SqlItem<SavedStructure>? sign, CancellationToken token = default)
    {
        await structure.Enter(token).ConfigureAwait(false);
        try
        {
            await vehicle.Enter(token).ConfigureAwait(false);
            try
            {
                if (!vehicle.PrimaryKey.IsValid)
                    throw new ArgumentException("Vehicle data does not have a valid primary key.", nameof(vehicle));
                if (!structure.PrimaryKey.IsValid)
                    throw new ArgumentException("Saved bay does not have a valid primary key.", nameof(structure));
                if (sign is not null && !sign.PrimaryKey.IsValid)
                    throw new ArgumentException("Saved sign does not have a valid primary key.", nameof(sign));
                VehicleSpawn spawn = new VehicleSpawn(vehicle.PrimaryKey, structure.PrimaryKey, sign is null ? PrimaryKey.NotAssigned : sign.PrimaryKey);
                SqlItem<VehicleSpawn> proxy = await AddOrUpdate(spawn, token).ConfigureAwait(false);
                Transform? model = structure.Item?.Buildable?.Model;
                if (model != null && !model.TryGetComponent<VehicleBayComponent>(out _))
                    model.gameObject.AddComponent<VehicleBayComponent>().Init(proxy, vehicle);
                return proxy;
            }
            finally
            {
                vehicle.Release();
            }
        }
        finally
        {
            structure.Release();
        }
    }

    public static async Task<InteractableVehicle?> SpawnVehicle(SqlItem<VehicleSpawn> spawn, CancellationToken token = default)
    {
        await spawn.Enter(token).ConfigureAwait(false);
        try
        {
            if (spawn.Item == null)
                return null;
            return await spawn.Item.SpawnVehicle(token);
        }
        finally
        {
            spawn.Release();
        }
    }
    public static bool IsVehicleFull(InteractableVehicle vehicle, bool excludeDriver = false)
    {
        GameThread.AssertCurrent();
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
        GameThread.AssertCurrent();
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
        GameThread.AssertCurrent();
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
        GameThread.AssertCurrent();
        if (vehicle.lockedOwner == CSteamID.Nil || owner == null) return false;

        foreach (Passenger passenger in vehicle.passengers)
        {
            if (passenger.player != null && owner.Steam64 == passenger.player.playerID.steamID.m_SteamID)
                return true;
        }
        return false;
    }
    public static int CountCrewmen(InteractableVehicle vehicle, VehicleData data)
    {
        GameThread.AssertCurrent();
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
    private static void EnsureVehicleLocked(UCPlayer player)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            !vehicle.isDead &&
            vehicle.checkDriver(player.CSteamID) &&
            vehicle.TryGetComponent(out VehicleComponent c) &&
            c.Data?.Item is { } &&
            (vehicle.lockedOwner.m_SteamID == 0 ||
             !vehicle.isLocked ||
             UCPlayer.FromID(vehicle.lockedOwner.m_SteamID) is not { IsOnline: true } ||
             vehicle.lockedGroup.m_SteamID is not 1ul and not 2ul
             ))
        {
            VehicleManager.ServerSetVehicleLock(vehicle, player.CSteamID, new CSteamID(TeamManager.GetGroupID(player.GetTeam())), true);
        }
    }
    private static void OnVehicleExit(ExitVehicle e)
    {
        GameThread.AssertCurrent();
        if (e.OldPassengerIndex == 0 && e.Vehicle.transform.TryGetComponent(out VehicleComponent comp))
            comp.LastDriverTime = Time.realtimeSinceStartup;
        if (KitDefaults<WarfareDbContext>.ShouldDequipOnExitVehicle(e.Player.KitClass))
            e.Player.Player.equipment.dequip();
    }
    private static void OnVehicleExitRequested(ExitVehicleRequested e)
    {
        GameThread.AssertCurrent();
        if (!e.Player.OnDuty() && e.ExitLocation.y - F.GetHeightAt2DPoint(e.ExitLocation.x, e.ExitLocation.z) > UCWarfare.Config.MaxVehicleHeightToLeave)
        {
            if (!FOBManager.Config.Buildables.Exists(v => v.Type == BuildableType.Emplacement && v.Emplacement is not null && v.Emplacement.EmplacementVehicle is not null && v.Emplacement.EmplacementVehicle.Guid == e.Vehicle.asset.GUID))
            {
                e.Player.SendChat(T.VehicleTooHigh);
                e.Cancel();
            }
        }
    }
    private void OnVehicleEnterRequested(EnterVehicleRequested e)
    {
        GameThread.AssertCurrent();
        if (!VehicleUtility.IgnoreSwapCooldown && CooldownManager.IsLoaded && CooldownManager.HasCooldown(e.Player, CooldownType.InteractVehicleSeats, out _, e.Vehicle))
        {
            e.Cancel();
            return;
        }
        if (Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging)
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Cancel();
            return;
        }
        if (!e.Vehicle.asset.canBeLocked) return;
        if (!e.Player.OnDuty() && Data.Gamemode.State == State.Staging && Data.Is<IStagingPhase>(out _) && (!Data.Is(out IAttackDefense? atk) || e.Player.GetTeam() == atk.AttackingTeam))
        {
            e.Player.SendChat(T.VehicleStaging, e.Vehicle.asset);
            e.Cancel();
            return;
        }
        if (Data.Is(out IRevives? r) && r.ReviveManager.IsInjured(e.Player.Steam64))
        {
            e.Cancel();
            return;
        }

        if (!e.Player.HasKit)
        {
            e.Player.SendChat(T.VehicleNoKit);
            e.Cancel();
        }
        if (e.Vehicle.transform.TryGetComponent(out VehicleComponent vc) && 
            (vc.Data?.Item?.IsDelayed(out Delay delay) ?? false) && 
            delay.Type == DelayType.Teammates
            && e.Player.Player.IsInMain())
        {
            e.Player.SendChat(T.RequestVehicleTeammatesDelay, Mathf.FloorToInt(delay.Value));
            e.Cancel();
        }
    }
    private void OnVehicleSwapSeatRequested(VehicleSwapSeatRequested e)
    {
        GameThread.AssertCurrent();
        if (!VehicleUtility.IgnoreSwapCooldown && CooldownManager.IsLoaded && CooldownManager.HasCooldown(e.Player, CooldownType.InteractVehicleSeats, out _, e.Vehicle))
        {
            e.Cancel();
            return;
        }
        if (!e.Vehicle.TryGetComponent(out VehicleComponent c))
            return;
        if (c.IsEmplacement && e.FinalSeat == 0)
        {
            e.Cancel();
        }
        else
        {
            if (!e.Player.HasKit)
            {
                e.Player.SendChat(T.VehicleNoKit);
                e.Cancel();
                return;
            }

            UCPlayer? owner = UCPlayer.FromCSteamID(e.Vehicle.lockedOwner);
            VehicleData? data = c.Data?.Item;
            if (data != null &&
                data.RequiredClass != Class.None) // vehicle requires crewman or pilot
            {
                if (!VehicleUtility.AllowEnterDriverSeat
                    && c.IsAircraft &&
                    e.InitialSeat == 0 &&
                    e.FinalSeat != 0 &&
                    e.Vehicle.transform.position.y - LevelGround.getHeight(e.Vehicle.transform.position) > 30 &&
                    !e.Player.OnDuty())
                {
                    e.Player.SendChat(T.VehicleAbandoningPilot);
                    e.Cancel();
                }
                else if (data.CrewSeats.ArrayContains(e.FinalSeat)) // seat is for crewman only
                {
                    if ((e.Player.KitClass == data.RequiredClass) || e.Player.OnDuty())
                    {
                        if (e.FinalSeat == 0) // if a crewman is trying to enter the driver's seat
                        {
                            FOBManager? manager = Data.Singletons.GetSingleton<FOBManager>();
                            bool canEnterDriverSeat = VehicleUtility.AllowEnterDriverSeat ||
                                                      owner == null ||
                                e.Player == owner ||
                                e.Player.OnDuty() ||
                                IsOwnerInVehicle(e.Vehicle, owner) ||
                                (owner != null && owner.Squad != null && owner.Squad.Members.Contains(e.Player) ||
                                (owner!.Position - e.Vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                                (data.Type == VehicleType.LogisticsGround && manager != null && manager.FindNearestFOB<FOB>(e.Vehicle.transform.position, e.Vehicle.lockedGroup.m_SteamID.GetTeam()) != null);

                            if (!canEnterDriverSeat)
                            {
                                if (owner?.Squad is null)
                                {
                                    OfflinePlayer pl = new OfflinePlayer(e.Vehicle.lockedOwner);
                                    if (owner != null || pl.TryCacheLocal())
                                    {
                                        e.Player.SendChat(T.VehicleWaitForOwner, owner ?? pl as IPlayer);
                                    }
                                    else
                                    {
                                        UCWarfare.RunTask(async token =>
                                        {
                                            OfflinePlayer pl2 = pl;
                                            await pl2.CacheUsernames(token).ConfigureAwait(false);
                                            await UniTask.SwitchToMainThread(token);
                                            e.Player.SendChat(T.VehicleWaitForOwner, pl);
                                        }, UCWarfare.UnloadCancel);
                                    }
                                }
                                else
                                    e.Player.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);
                                e.Cancel();
                            }
                        }
                        else // if the player is trying to switch to a gunner's seat
                        {
                            if (!(F.IsInMain(e.Vehicle.transform.position) || e.Player.OnDuty())) // if player is trying to switch to a gunner's seat outside of main
                            {
                                if (e.Vehicle.passengers.Length == 0 || e.Vehicle.passengers[0].player is null) // if they have no driver
                                {
                                    e.Player.SendChat(T.VehicleDriverNeeded);
                                    e.Cancel();
                                }
                                else if (e.Player.Steam64 == e.Vehicle.passengers[0].player.playerID.steamID.m_SteamID) // if they are the driver
                                {
                                    e.Player.SendChat(T.VehicleAbandoningDriver);
                                    e.Cancel();
                                }
                            }
                        }
                    }
                    else
                    {
                        e.Player.SendChat(T.VehicleMissingKit, data.RequiredClass);
                        e.Cancel();
                    }
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

                        e.Cancel();
                    }
                }
            }
        }
    }

    public async Task<bool> UnlinkSign(BarricadeDrop drop, CancellationToken token = default)
    {
        if (drop.interactable is not InteractableSign sign)
            return false;
        StructureSaver? saver = StructureSaver.GetSingletonQuick();
        if (saver != null)
        {
            SqlItem<SavedStructure>? save = await saver.GetSaveItem(drop, token).ConfigureAwait(false);
            if (save?.Item == null)
                return false;
            int key = save.PrimaryKey;
            VehicleSpawn? spawn = null;
            await WaitAsync(token).ConfigureAwait(false);
            try
            {
                await WriteWaitAsync(token).ConfigureAwait(false);
                try
                {
                    for (int i = 0; i < Items.Count; ++i)
                    {
                        spawn = Items[i].Item;
                        if (spawn != null && spawn.SignKey.Key == key)
                        {
                            spawn.SignKey = PrimaryKey.NotAssigned;
                            break;
                        }
                    }
                }
                finally
                {
                    WriteRelease();
                }
            }
            finally
            {
                Release();
            }
            if (spawn != null)
                await AddOrUpdate(spawn, token).ConfigureAwait(false);
            await save.Delete(token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            Signs.SetSignTextServerOnly(sign, string.Empty);
            Signs.CheckSign(drop);
            return true;
        }

        return false;
    }
    public async Task<bool> LinkSign(BarricadeDrop drop, SqlItem<VehicleSpawn> spawn, CancellationToken token = default)
    {
        if (drop.interactable is not InteractableSign sign || spawn is null)
            return false;
        StructureSaver? saver = StructureSaver.GetSingletonQuick();
        if (saver != null)
        {
            VehicleData? data = spawn.Item?.Vehicle?.Item;
            Guid vehId = data == null ? Guid.Empty : data.VehicleID;
            await UniTask.SwitchToMainThread(token);
            Signs.SetSignTextServerOnly(sign, Signs.Prefix + Signs.VBSPrefix + vehId.ToString("N"));
            (SqlItem<SavedStructure> save, _) = await saver.AddBarricade(drop, token).ConfigureAwait(false);
            await spawn.Enter(token).ConfigureAwait(false);
            try
            {
                VehicleSpawn? sp = spawn.Item;
                if (sp == null)
                    return false;
                sp.SignKey = save.PrimaryKey;
                await spawn.SaveItem(token).ConfigureAwait(false);
            }
            finally
            {
                spawn.Release();
            }
            await UniTask.SwitchToMainThread(token);
            Signs.CheckSign(drop);
            return true;
        }
        return false;
    }
    public void TimeSync(float time)
    {
        for (int i = 0; i < Items.Count; ++i)
        {
            VehicleSpawn? spawn = Items[i]?.Item;
            if (spawn?.Structure?.Item?.Buildable is { } buildable && buildable.Model.TryGetComponent(out VehicleBayComponent comp))
                comp.TimeSync();
        }
    }

    /// <summary>Locks <see cref="VehicleSpawner"/> write semaphore until it's disposed. Be careful of what async work is done within the loop.</summary>
    public IEnumerable<SqlItem<VehicleSpawn>> EnumerateSpawns(SqlItem<VehicleData> data) => new SpawnEnumerator(data.LastPrimaryKey, this);
    public IEnumerable<SqlItem<VehicleSpawn>> EnumerateSpawns(VehicleData data) => new SpawnEnumerator(data.PrimaryKey, this);
    private class SpawnEnumerator : IEnumerator<SqlItem<VehicleSpawn>>, IEnumerable<SqlItem<VehicleSpawn>>, ICloneable
    {
        private readonly VehicleSpawner _spawner;
        private readonly PrimaryKey _data;
        private bool _disposed;
        private bool _ran;
        private int _index;
        private int _uses;
        public SqlItem<VehicleSpawn> Current => _spawner.Items[_index];
        object IEnumerator.Current => Current;
        public SpawnEnumerator(PrimaryKey data, VehicleSpawner spawner)
        {
            _data = data;
            _index = -1;
            _spawner = spawner;
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _spawner.WriteRelease();
                --_uses;
            }
        }
        public bool MoveNext()
        {
            if (!_ran)
            {
                ++_uses;
                _spawner.WriteWait();
                _ran = true;
            }
            for (int i = _index + 1; i < _spawner.Items.Count; ++i)
            {
                if (_spawner.Items[i].Item is { } v && v.VehicleKey.Key == _data.Key)
                {
                    _index = i;
                    return true;
                }
            }
            
            Dispose();
            return false;
        }
        public void Reset()
        {
            _index = 0;
            _disposed = false;
            _ran = false;
        }

        public IEnumerator<SqlItem<VehicleSpawn>> GetEnumerator() => ++_uses == 1 ? this : (SpawnEnumerator)Clone();
        IEnumerator IEnumerable.GetEnumerator() => ++_uses == 1 ? this : (SpawnEnumerator)Clone();
        public object Clone() => new SpawnEnumerator(_data, _spawner);
    }

    #region Sql
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "vehicle_spawns";

    public const string COLUMN_PK = "pk";
    public const string COLUMN_VEHICLE = "Vehicle";
    public const string COLUMN_STRUCTURE = "Structure";
    public const string COLUMN_SIGN = "Sign";

    private static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_VEHICLE, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = VehicleBay.TABLE_MAIN,
                ForeignKeyColumn = VehicleBay.COLUMN_PK
            },
            new Schema.Column(COLUMN_STRUCTURE, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = StructureSaver.TABLE_MAIN,
                ForeignKeyColumn = StructureSaver.COLUMN_PK
            },
            new Schema.Column(COLUMN_SIGN, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = StructureSaver.TABLE_MAIN,
                ForeignKeyColumn = StructureSaver.COLUMN_PK,
                Nullable = true
            }
        }, true, typeof(VehicleSpawn))
    };
    // ReSharper restore InconsistentNaming
    [Obsolete]
    protected override async Task AddOrUpdateItem(VehicleSpawn? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item == null)
        {
            if (!pk.IsValid)
                throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0;", new object[] { pk.Key }, token).ConfigureAwait(false);
            return;
        }
        if (!item.VehicleKey.IsValid)
            throw new ArgumentException("Item must have a valid vehicle key.", nameof(item));
        if (!item.StructureKey.IsValid)
            throw new ArgumentException("Item must have a valid structure key.", nameof(item));

        bool hasPk = pk.IsValid;
        uint pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 4 : 3];
        objs[0] = item.VehicleKey.Key;
        objs[1] = item.StructureKey.Key;
        objs[2] = item.SignKey.IsValid ? item.SignKey.Key : DBNull.Value;
        if (hasPk)
            objs[3] = pk.Key;
        await Sql.QueryAsync(F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk, null, null, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN),
            objs, reader =>
            {
                pk2 = reader.GetUInt32(0);
            }, token).ConfigureAwait(false);
        pk = pk2;
        if (!pk.IsValid)
            throw new Exception("Unable to get a valid primary key for " + item + ".");
        item.PrimaryKey = pk;
    }
    [Obsolete]
    protected override async Task<VehicleSpawn?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        VehicleSpawn? obj = null;
        if (!pk.IsValid)
            throw new ArgumentException("Primary key is not valid.", nameof(pk));
        int pk2 = pk;
        await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_PK, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN) +
                             $" FROM `{TABLE_MAIN}` AS `a` WHERE `{COLUMN_PK}`=@0 AND " +
                             $"(SELECT `b`.`{StructureSaver.COLUMN_MAP}` " +
                                $"FROM `{StructureSaver.TABLE_MAIN}` AS `b` " +
                                $"WHERE `b`.`{StructureSaver.COLUMN_PK}`=`a`.`{COLUMN_STRUCTURE}` LIMIT 1) = @1 LIMIT 1;", new object[] { pk2, MapScheduler.Current },
            reader =>
            {
                obj = new VehicleSpawn(reader.GetUInt32(0), reader.GetUInt32(1), reader.GetUInt32(2), reader.IsDBNull(3) ? PrimaryKey.NotAssigned : reader.GetUInt32(3));
            }, token).ConfigureAwait(false);

        return obj;
    }
    [Obsolete]
    protected override async Task<VehicleSpawn[]> DownloadAllItems(CancellationToken token = default)
    {
        List<VehicleSpawn> spawns = new List<VehicleSpawn>(32);
        await Sql.QueryAsync("SELECT " + SqlTypes.ColumnList(COLUMN_PK, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN) +
                             $" FROM `{TABLE_MAIN}` AS `a` WHERE " +
                             $"(SELECT `b`.`{StructureSaver.COLUMN_MAP}` " +
                             $"FROM `{StructureSaver.TABLE_MAIN}` AS `b` " +
                             $"WHERE `b`.`{StructureSaver.COLUMN_PK}`=`a`.`{COLUMN_STRUCTURE}` LIMIT 1) = @0;", new object[] { MapScheduler.Current },
            reader =>
            {
                spawns.Add(new VehicleSpawn(reader.GetUInt32(0), reader.GetUInt32(1), reader.GetUInt32(2), reader.IsDBNull(3) ? PrimaryKey.NotAssigned : reader.GetUInt32(3)));
            }, token).ConfigureAwait(false);

        return spawns.ToArray();
    }
    #endregion
}

public class VehicleSpawn : IListItem
{
    private SqlItem<VehicleData>? _vehicle;
    private SqlItem<SavedStructure>? _structure;
    private SqlItem<SavedStructure>? _sign;
    private PrimaryKey _signKey;
    private PrimaryKey _structureKey;
    private PrimaryKey _vehicleKey;
    private InteractableVehicle? _linkedVehicle;
    public PrimaryKey PrimaryKey { get; set; }
    public PrimaryKey VehicleKey
    {
        get => _vehicleKey;
        set { _vehicleKey = value; _vehicle = null; }
    }
    public PrimaryKey StructureKey
    {
        get => _structureKey;
        set { _structureKey = value; _structure = null; }
    }
    public PrimaryKey SignKey
    {
        get => _signKey;
        set { _signKey = value; _sign = null; }
    }

    /// <exception cref="NotSupportedException"/>
    public InteractableVehicle? LinkedVehicle
    {
        get => _linkedVehicle;
        set { GameThread.AssertCurrent(); _linkedVehicle = value; }
    }

    /// <remarks>Getter can lock <see cref="VehicleBay"/> write semaphore.</remarks>
    [JsonIgnore]
    public SqlItem<VehicleData>? Vehicle
    {
        get
        {
            if (_vehicle is not null && _vehicle.Manager != null && ((VehicleBay)_vehicle.Manager).IsLoaded)
            {
                return _vehicle;
            }
            
            return _vehicle = VehicleBay.GetSingletonQuick()?.FindProxyNoLock(VehicleKey);
        }
        set
        {
            if (value is null)
            {
                _vehicleKey = PrimaryKey.NotAssigned;
                _vehicle = null;
                return;
            }

            if (!value.PrimaryKey.IsValid)
                throw new ArgumentException("Value must be null or have a set primary key.", nameof(value));
            _vehicleKey = value.PrimaryKey;
            _vehicle = value;
        }
    }

    /// <remarks>Getter can lock <see cref="StructureSaver"/> write semaphore.</remarks>
    [JsonIgnore]
    public SqlItem<SavedStructure>? Structure
    {
        get
        {
            if (_structure is not null && _structure.Manager != null && ((StructureSaver)_structure.Manager).IsLoaded)
            {
                return _structure;
            }

            return _structure = StructureSaver.GetSingletonQuick()?.FindProxyNoLock(StructureKey);
        }
        set
        {
            if (value is null)
            {
                _structureKey = PrimaryKey.NotAssigned;
                _structure = null;
                return;
            }

            if (!value.PrimaryKey.IsValid)
                throw new ArgumentException("Value must be null or have a set primary key.", nameof(value));
            _structureKey = value.PrimaryKey;
            _structure = value;
        }
    }

    /// <remarks>Getter can lock <see cref="StructureSaver"/> write semaphore.</remarks>
    [JsonIgnore]
    public SqlItem<SavedStructure>? Sign
    {
        get
        {
            if (_sign is not null && _sign.Manager != null && ((StructureSaver)_sign.Manager).IsLoaded)
            {
                return _sign;
            }

            return _sign = StructureSaver.GetSingletonQuick()?.FindProxyNoLock(SignKey);
        }
        set
        {
            if (value is null)
            {
                _signKey = PrimaryKey.NotAssigned;
                _sign = null;
                return;
            }

            if (!value.PrimaryKey.IsValid)
                throw new ArgumentException("Value must be null or have a set primary key.", nameof(value));
            _signKey = value.PrimaryKey;
            _sign = value;
        }
    }
    public VehicleSpawn() { }

    public VehicleSpawn(PrimaryKey vehicle, PrimaryKey structure, PrimaryKey sign) : this(PrimaryKey.NotAssigned, vehicle, structure, sign) { }

    public VehicleSpawn(PrimaryKey key, PrimaryKey vehicle, PrimaryKey structure, PrimaryKey sign)
    {
        PrimaryKey = key;
        VehicleKey = vehicle;
        StructureKey = structure;
        SignKey = sign;
    }

    private void ReportError(string message) => L.LogError("[VEH SPAWNER] " + this + " - " + message);
    [Conditional("DEBUG")]
    private void ReportInfo(string message) => L.LogDebug("[VEH SPAWNER] " + this + " - " + message);

    public bool HasLinkedVehicle(out InteractableVehicle vehicle)
    {
        if (LinkedVehicle == null)
        {
            vehicle = null!;
            return false;
        }
        vehicle = LinkedVehicle;
        return !vehicle.isDead && !vehicle.isExploded;
    }
    public void LinkNewVehicle(InteractableVehicle vehicle)
    {
        GameThread.AssertCurrent();
        LinkedVehicle = vehicle;
        Transform? model = Structure?.Item?.Buildable?.Model;
        if (model != null && model.TryGetComponent(out VehicleBayComponent comp))
        {
            comp.OnSpawn(vehicle);
        }
        ReportInfo("Linked new vehicle: " + vehicle.asset.vehicleName + ", " + vehicle.instanceID + ".");
    }
    public void Unlink()
    {
        GameThread.AssertCurrent();
        ReportInfo("Unlinked vehicle: " + (LinkedVehicle == null ? "<no prev linked>" : (LinkedVehicle.asset.vehicleName + ", " + LinkedVehicle.instanceID)) + ".");
        LinkedVehicle = null;
        Transform? model = Structure?.Item?.Buildable?.Model;
        if (model != null && model.TryGetComponent(out VehicleBayComponent comp))
        {
            comp.OnSpawn(null);
            comp.UpdateTimeDelay();
        }
    }
    public void UpdateSign(SteamPlayer player)
    {
        IBuildable? sign = Sign?.Item?.Buildable;
        if (sign is null || sign.Model == null || sign.Drop is not BarricadeDrop drop) return;
        UpdateSignInternal(player, this, drop);
    }
    private static IEnumerator<SteamPlayer> BasesToPlayer(IEnumerator<KeyValuePair<ulong, byte>> b, byte team)
    {
        while (b.MoveNext())
        {
            if (b.Current.Value == team)
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    if (Provider.clients[i].playerID.steamID.m_SteamID == b.Current.Key)
                        yield return Provider.clients[i];
                }
            }
        }
        b.Dispose();
    }
    public void UpdateSign()
    {
        GameThread.AssertCurrent();
        IBuildable? sign = Sign?.Item?.Buildable;
        if (sign is null || sign.Model == null || sign.Drop is not BarricadeDrop drop) return;
        if (TeamManager.PlayerBaseStatus != null && TeamManager.Team1Main.IsInside(sign.Model.transform.position))
        {
            IEnumerator<SteamPlayer> t1Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 1);
            UpdateSignInternal(t1Main, this, drop, 1ul);
        }
        else if (TeamManager.PlayerBaseStatus != null && TeamManager.Team2Main.IsInside(sign.Model.transform.position))
        {
            IEnumerator<SteamPlayer> t2Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 2);
            UpdateSignInternal(t2Main, this, drop, 2ul);
        }
        else if (Regions.tryGetCoordinate(sign.Model.transform.position, out byte x, out byte y))
        {
            IEnumerator<SteamPlayer> everyoneElse = F.EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            UpdateSignInternal(everyoneElse, this, drop, 0);
        }
        else
        {
            L.LogWarning("Vehicle sign for spawn " + PrimaryKey + " not in main bases or any region!");
        }
    }
    private static void UpdateSignInternal(IEnumerator<SteamPlayer> players, VehicleSpawn spawn, BarricadeDrop drop, ulong team)
    {
        VehicleData? data = spawn.Vehicle?.Item;
        if (data == null)
            return;
        FactionInfo? faction = TeamManager.GetFactionSafe(team) ?? TeamManager.GetFactionInfo(data.Faction);
        foreach (LanguageSet set in LanguageSet.All(players))
        {
            string val = Localization.TranslateVBS(spawn, data, set.Language, set.CultureInfo, faction);
            NetId id = drop.interactable.GetNetId();
            while (set.MoveNext())
            {
                string val2 = Util.QuickFormat(val, data.GetCostLine(set.Next));
                Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val2);
            }
        }
        players.Dispose();
    }
    private static void UpdateSignInternal(SteamPlayer player, VehicleSpawn spawn, BarricadeDrop drop)
    {
        VehicleData? data = spawn.Vehicle?.Item;
        if (data == null || UCPlayer.FromSteamPlayer(player) is not { } pl)
            return;
        ulong team = pl.GetTeam();
        string val = Localization.TranslateVBS(spawn, data, pl.Locale.LanguageInfo, pl.Locale.CultureInfo, TeamManager.GetFactionSafe(team) ?? TeamManager.GetFactionInfo(data.Faction));
        string val2 = Util.QuickFormat(val, pl == null ? string.Empty : data.GetCostLine(pl));
        Data.SendChangeText.Invoke(drop.interactable.GetNetId(), ENetReliability.Unreliable, player.transportConnection, val2);
    }
    public override string ToString() => $"#{PrimaryKey}, Bay key: {StructureKey}, Vehicle key: {VehicleKey}, " +
                                         $"Sign key: {SignKey}, Linked vehicle: " +
                                         (LinkedVehicle == null ? "<none>" : (LinkedVehicle.asset.FriendlyName + " #" + LinkedVehicle.instanceID)) + ".";
}
public enum VehicleBayState : byte
{
    Unknown = 0,
    Ready = 1,
    Idle = 2,
    Dead = 3,
    TimeDelayed = 4,
    InUse = 5,
    Delayed = 6,
    NotInitialized = 255,
}
public sealed class VehicleBayComponent : MonoBehaviour
{
    private SqlItem<VehicleSpawn> _spawnData;
    private SqlItem<VehicleData> _vehicleData;
    public SqlItem<VehicleSpawn> Spawn => _spawnData;
    private VehicleBayState _state = VehicleBayState.NotInitialized;
    private InteractableVehicle? _vehicle;
    public VehicleBayState State => _state;
    public Vector3 LastLocation = Vector3.zero;
    public float RequestTime;
    public void Init(SqlItem<VehicleSpawn> spawn, SqlItem<VehicleData> data)
    {
        _spawnData = spawn;
        _vehicleData = data;
        _state = VehicleBayState.Unknown;
    }
    private LocationDevkitNode? _lastLoc;
    private float _idleStartTime = -1f;
    public string CurrentLocation = string.Empty;
    public float IdleTime;
    public float DeadTime;
    private float _deadStartTime = -1f;
    private float _lastIdleCheck;
    private float _lastSignUpdate;
    private float _lastLocCheck;
    private float _lastDelayCheck;
    public void OnSpawn(InteractableVehicle? vehicle)
    {
        _vehicle = vehicle == null || vehicle.isDead || vehicle.isExploded ? null : vehicle;
        if (_vehicle is null)
        {
            _state = VehicleBayState.Dead;
            _deadStartTime = _idleStartTime;
            DeadTime = IdleTime;
            _lastSignUpdate = 0f;
            return;
        }
        RequestTime = 0f;
        IdleTime = 0f;
        _deadStartTime = -1f;
        _idleStartTime = -1f;
        DeadTime = 0f;
        _lastSignUpdate = 0f;
        if (_vehicleData.Item != null && _vehicleData.Item.IsDelayed(out Delay delay))
            _state = delay.Type == DelayType.Time ? VehicleBayState.TimeDelayed : VehicleBayState.Delayed;
        else _state = VehicleBayState.Ready;
        _lastDelayCheck = Time.realtimeSinceStartup;
    }
    public void OnRequest()
    {
        RequestTime = Time.realtimeSinceStartup;
        _state = VehicleBayState.InUse;
    }
    private bool _checkTime;
    private bool CheckVehicleDead(float time, ref bool vcheck)
    {
        if (vcheck) return false;
        vcheck = true;

        if (_vehicle == null || _vehicle.isDead || _vehicle.isExploded)
        {
            if (_state == VehicleBayState.Idle)
            {
                _deadStartTime = _idleStartTime;
                DeadTime = IdleTime;
            }
            else
            {
                _deadStartTime = time;
                DeadTime = 0;
            }
            _state = VehicleBayState.Dead;
            return true;
        }
        return false;
    }
    public void UpdateTimeDelay()
    {
        _checkTime = true;
    }
    [UsedImplicitly]
    void FixedUpdate()
    {
        if (_state == VehicleBayState.NotInitialized) return;
        float time = Time.realtimeSinceStartup;
        bool vcheck = false;
        if (_checkTime || (time - _lastDelayCheck > 1f && _state is VehicleBayState.Unknown or VehicleBayState.Delayed or VehicleBayState.TimeDelayed))
        {
            _lastDelayCheck = time;
            _checkTime = false;
            if (CheckVehicleDead(time, ref vcheck))
                goto timers;
            if (_vehicleData.Item != null && _vehicleData.Item.IsDelayed(out Delay delay))
            {
                if (delay.Type == DelayType.Time)
                {
                    _state = VehicleBayState.TimeDelayed;
                    goto timers;
                }
                if (_state != VehicleBayState.Delayed)
                {
                    _state = VehicleBayState.Delayed;
                    UpdateSign(time);
                    return;
                }
            }
            else if (OffenseManager.IsValidSteam64Id(_vehicle!.lockedOwner))
            {
                if (_state != VehicleBayState.InUse)
                {
                    _lastLocCheck = 0f;
                    _lastLoc = null;
                    _state = VehicleBayState.InUse;
                }
                goto locationUpdate;
            }
            else
            {
                _state = VehicleBayState.Ready;
                UpdateSign(time);
                return;
            }
        }

        if (_state is VehicleBayState.Idle or VehicleBayState.InUse && time - _lastIdleCheck >= 4f)
        {
            _lastIdleCheck = time;
            if (CheckVehicleDead(time, ref vcheck))
                goto timers;
            if (_vehicle!.anySeatsOccupied || PlayerManager.IsPlayerNearby(_vehicle.lockedOwner.m_SteamID, 150f, _vehicle.transform.position))
            {
                if (_state != VehicleBayState.InUse)
                {
                    _lastLocCheck = 0f;
                    _lastLoc = null;
                    _state = VehicleBayState.InUse;
                }
                goto locationUpdate;
            }
            if (_state != VehicleBayState.Idle)
            {
                _idleStartTime = time;
                IdleTime = 0f;
                _state = VehicleBayState.Idle;
            }

            goto timers;
        }

        locationUpdate:
        if (_state == VehicleBayState.InUse && (_lastLoc is null || time - _lastLocCheck > 4f))
        {
            if (CheckVehicleDead(time, ref vcheck))
                goto timers;
            _lastLocCheck = time;
            if (!LastLocation.AlmostEquals(_vehicle!.transform.position) || _lastLoc is null)
            {
                LastLocation = _vehicle.transform.position;
                LocationDevkitNode? ind = F.GetClosestLocation(LastLocation);
                if (ind is not null && ind != _lastLoc)
                {
                    _lastLoc = ind;
                    CurrentLocation = ind.locationName;
                    UpdateSign(time);
                }
            }
            return;
        }
        timers:
        bool respawn = false;
        if (_state == VehicleBayState.Idle)
        {
            IdleTime = _idleStartTime < 0f ? 0f : time - _idleStartTime;
            respawn = _vehicleData.Item != null && IdleTime >= _vehicleData.Item.RespawnTime;
        }
        else if (_state == VehicleBayState.Dead)
        {
            DeadTime = _deadStartTime < 0f ? 0f : time - _deadStartTime;
            respawn = _vehicleData.Item != null && DeadTime >= _vehicleData.Item.RespawnTime;
        }
        if (respawn)
        {
            if (_vehicle != null)
                VehicleSpawner.DeleteVehicle(_vehicle);
            _vehicle = null;
            if (_spawnData.Item != null)
            {
                UCWarfare.RunTask(VehicleSpawner.SpawnVehicle, Spawn, ctx: "Respawning vehicle " + _spawnData.LastPrimaryKey + " after idle or dead timer is up.");
                return;
            }
        }
        if (_state is VehicleBayState.Idle or VehicleBayState.Dead or VehicleBayState.TimeDelayed && time - _lastSignUpdate >= 1f)
        {
            UpdateSign(time);
        }
    }
    [UsedImplicitly]
    void OnDestroy()
    {
        _state = VehicleBayState.Unknown;
        UpdateSign(Time.realtimeSinceStartup);
    }
    private void UpdateSign(float time)
    {
        _lastSignUpdate = time;
        _spawnData.Item?.UpdateSign();
    }

    internal void TimeSync()
    {
        _checkTime = true;
        _lastIdleCheck = 0f;
        _lastSignUpdate = 0f;
        _lastLocCheck = 0f;
        _lastDelayCheck = 0f;
    }
}
#endif