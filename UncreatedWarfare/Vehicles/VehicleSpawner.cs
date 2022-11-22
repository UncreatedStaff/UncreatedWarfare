using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
[SingletonDependency(typeof(Level))]
public class VehicleSpawner : ListSingleton<VehicleSpawn>, ILevelStartListenerAsync, IGameStartListener, IStagingPhaseOverListener
{
    public static VehicleSpawner Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSpawner, VehicleSpawn>();
    public const float VEHICLE_HEIGHT_OFFSET = 5f;
    public VehicleSpawner() : base("vehiclespawns", Path.Combine(Data.Paths.VehicleStorage, "vehiclespawns.json")) { }
    protected override string LoadDefaults() => EMPTY_LIST;
    public static IEnumerable<VehicleSpawn> Spawners => Loaded ? Singleton : null!;
    public override void Load()
    {
        TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
        TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
        UCPlayerKeys.SubscribeKeyUp(SpawnCountermeasuresPressed, Data.Keys.SpawnCountermeasures);
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
        UCPlayerKeys.UnsubscribeKeyUp(SpawnCountermeasuresPressed, Data.Keys.SpawnCountermeasures);
        TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
    }
    async Task ILevelStartListenerAsync.OnLevelReady()
    {
        LoadSpawns();
        await RespawnAllVehicles().ConfigureAwait(false);
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        UpdateSigns();
    }
    private void SpawnCountermeasuresPressed(UCPlayer player, float timeDown, ref bool handled)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null && player.Player.movement.getSeat() == 0 && (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.TrySpawnCountermeasures();
        }
    }
    private void LoadSpawns()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (VehicleSpawn spawn in this)
        {
            spawn.Initialize();
        }
    }
    internal void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(data.barricade.asset.GUID) && IsRegistered(data.instanceID, out VehicleSpawn spawn, EStructType.BARRICADE))
        {
            if (Assets.find<VehicleAsset>(spawn.VehicleGuid) is VehicleAsset asset)
            {
                ActionLogger.Add(EActionLogType.DEREGISTERED_SPAWN,
                    $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                    $"DEREGISTERED SPAWN ID: {spawn.InstanceId}");
                    //e.Instigator == null ? 0ul : e.Instigator.Steam64);
            }
            else
            {
                ActionLogger.Add(EActionLogType.DEREGISTERED_SPAWN,
                    $"{spawn.VehicleGuid:N} - " +
                    $"DEREGISTERED SPAWN ID: {spawn.InstanceId}");
                    //e.Instigator == null ? 0ul : e.Instigator.Steam64);
            }
            L.LogDebug("Vehicle spawn was deregistered because the barricade was salvaged or destroyed.");
            DeleteSpawn(data.instanceID, EStructType.BARRICADE);
        }
    }
    internal void OnStructureDestroyed(StructureDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(e.Structure.asset.GUID) && IsRegistered(e.InstanceID, out VehicleSpawn spawn, EStructType.STRUCTURE))
        {
            if (Assets.find<VehicleAsset>(spawn.VehicleGuid) is VehicleAsset asset)
            {
                ActionLogger.Add(EActionLogType.DEREGISTERED_SPAWN,
                    $"{asset.vehicleName} / {asset.id} / {asset.GUID:N} - " +
                    $"DEREGISTERED SPAWN ID: {spawn.InstanceId}",
                    e.Instigator == null ? 0ul : e.Instigator.Steam64);
            }
            else
            {
                ActionLogger.Add(EActionLogType.DEREGISTERED_SPAWN, 
                    $"{spawn.VehicleGuid:N} - " +
                    $"DEREGISTERED SPAWN ID: {spawn.InstanceId}",
                    e.Instigator == null ? 0ul : e.Instigator.Steam64);
            }
            L.LogDebug("Vehicle spawn was deregistered because the structure was " + (e.WasPickedUp ? "salvaged" : "destroyed") + ".");
            DeleteSpawn(e.InstanceID, EStructType.STRUCTURE);
        }
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        Singleton.Save();
    }
    public static async Task RespawnAllVehicles(CancellationToken token = default)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        L.Log("Respawning vehicles...", ConsoleColor.Magenta);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = VehicleManager.vehicles.Count - 1; i >= 0; i--)
        {
            InteractableVehicle v = VehicleManager.vehicles[i];
            VehicleBay.DeleteVehicle(v);
        }
        foreach (VehicleSpawn spawn in Singleton)
        {
            await spawn.SpawnVehicle(token);
        }
    }
    public static void CreateSpawn(BarricadeDrop drop, BarricadeData data, Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.BARRICADE);
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        Data.Singletons.GetSingleton<StructureSaver>()?.BeginAddBarricade(drop);
        Task.Run(() => spawn.SpawnVehicle());
    }
    public static void CreateSpawn(StructureDrop drop, StructureData data, Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.STRUCTURE);
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        Data.Singletons.GetSingleton<StructureSaver>()?.BeginAddStructure(drop);
        Task.Run(() => spawn.SpawnVehicle());
    }
    public static void DeleteSpawn(uint barricadeInstanceID, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = Singleton.GetObject(s => s.InstanceId == barricadeInstanceID && s.StructureType == type);
        if (spawn != null)
        {
            if (spawn.StructureType == EStructType.STRUCTURE && spawn.StructureDrop != null && spawn.StructureDrop.model.TryGetComponent(out VehicleBayComponent vbc))
            {
                UnityEngine.Object.Destroy(vbc);
            }
            if (VehicleSigns.Loaded)
            {
                foreach (VehicleSign sign in VehicleSigns.GetLinkedSigns(spawn))
                {
                    if (sign.SignInteractable != null)
                        VehicleSigns.UnlinkSign(sign.SignInteractable);
                }
            }
        }
        Singleton.RemoveWhere(s => s.InstanceId == barricadeInstanceID && s.StructureType == type);
        StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
        if (saver != null && saver.TryGetSave(barricadeInstanceID, type, out SavedStructure structure))
            saver.BeginRemoveItem(structure);
    }
    public static bool IsRegistered(uint barricadeInstanceID, out VehicleSpawn spawn, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == EStructType.BARRICADE)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.InstanceId, out spawn);
        else if (type == EStructType.STRUCTURE)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.InstanceId, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool IsRegistered(SerializableTransform transform, out VehicleSpawn spawn, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == EStructType.BARRICADE)
            return Singleton.ObjectExists(s => s.StructureType == EStructType.BARRICADE && s.BarricadeDrop != null && transform == s.BarricadeDrop.model.transform, out spawn);
        else if (type == EStructType.STRUCTURE)
            return Singleton.ObjectExists(s => s.StructureType == EStructType.STRUCTURE && s.StructureDrop != null && transform == s.StructureDrop.model.transform, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool UnusedSpawnExists(Guid vehicleID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s =>
        {
            if (s.VehicleGuid == vehicleID && s.VehicleInstanceID != 0)
            {
                InteractableVehicle vehicle = VehicleManager.getVehicle(s.VehicleInstanceID);
                return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
            }
            return false;
        }, out spawn);
    }

    public static bool SpawnExists(uint bayInstanceID, EStructType type, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s => s.InstanceId == bayInstanceID && s.StructureType == type, out spawn);
    }

    public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);
    }
    private void OnPlayerEnterMain(SteamPlayer player, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.SendChat(T.EnteredMain, TeamManager.GetFaction(team));
        for (int i = 0; i < Count; i++)
        {
            VehicleSpawn spawn = this[i];
            if (spawn.LinkedSign != null && spawn.LinkedSign.SignDrop != null && (
                team == 1 && TeamManager.Team1Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position) ||
                team == 2 && TeamManager.Team2Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position)))
                spawn.UpdateSign(player);
        }
    }
    private void OnPlayerLeftMain(SteamPlayer player, ulong team)
    {
        player.SendChat(T.LeftMain, TeamManager.GetFaction(team));
    }
    public static void UpdateSigns(Guid vehicle)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (spawn.VehicleGuid == vehicle)
                spawn.UpdateSign();
        }
    }
    public static void UpdateSigns(UCPlayer player)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            Singleton[i].UpdateSign(player.Player.channel.owner);
        }
    }
    public static void UpdateSigns()
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
            Singleton[i].UpdateSign();
    }
    public static void UpdateSigns(uint vehicle)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (spawn.VehicleInstanceID == vehicle)
                spawn.UpdateSign();
        }
    }
    public static void UpdateSignsWhere(Predicate<VehicleSpawn> selector)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (selector(spawn))
                spawn.UpdateSign();
        }
    }

    internal static bool TryGetSpawnFromSign(InteractableSign sign, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        for (int i = 0; i < Singleton.Count; i++)
        {
            spawn = Singleton[i];
            if (spawn.LinkedSign is not null && spawn.LinkedSign.SignInteractable == sign)
                return true;
        }
        spawn = null!;
        return false;
    }

    void IStagingPhaseOverListener.OnStagingPhaseOver()
    {
        UpdateSignsWhere(spawn => spawn.Data?.Item != null && spawn.Data.Item.HasDelayType(DelayType.OutOfStaging));
    }
}
[JsonSerializable(typeof(VehicleSpawn))]
public class VehicleSpawn
{
    [JsonPropertyName("instance_id")]
    public uint InstanceId { get; set; }

    [JsonPropertyName("struct_type")]
    public EStructType StructureType { get; set; }

    [JsonPropertyName("vehicle_guid")]
    public Guid VehicleGuid { get; set; }

    [JsonIgnore]
    public uint VehicleInstanceID { get; internal set; }

    [JsonIgnore]
    public BarricadeDrop? BarricadeDrop { get; internal set; }

    [JsonIgnore]
    public StructureDrop? StructureDrop { get; internal set; }

    [JsonIgnore]
    public bool IsActive { get; private set; }

    [JsonIgnore]
    public bool Initialized { get; private set; }

    [JsonIgnore]
    public VehicleSign? LinkedSign { get; internal set; }

    [JsonIgnore]
    public SqlItem<VehicleData>? Data { get; internal set; }

    [JsonIgnore]
    public VehicleBayComponent? Component
    {
        get
        {
            if (StructureType == EStructType.STRUCTURE)
            {
                if (StructureDrop != null && StructureDrop.model.TryGetComponent(out VehicleBayComponent comp))
                    return comp;
            }
            else if (StructureType == EStructType.BARRICADE)
            {
                if (BarricadeDrop != null && BarricadeDrop.model.TryGetComponent(out VehicleBayComponent comp))
                    return comp;
            }

            return null;
        }
    }
    public VehicleSpawn(uint spawnPadInstanceId, Guid vehicleID, EStructType type)
    {
        InstanceId = spawnPadInstanceId;
        VehicleGuid = vehicleID;
        IsActive = true;
        StructureType = type;
        Initialized = false;
    }
    public VehicleSpawn()
    {
        IsActive = true;
        StructureType = EStructType.BARRICADE;
        Initialized = false;
    }
    public void Initialize()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            Data = Warfare.Data.Singletons.TryGetSingleton(out VehicleBay bay) ? bay.GetDataProxySync(VehicleGuid) : null;
            if (StructureType == EStructType.BARRICADE)
            {
                BarricadeDrop = UCBarricadeManager.FindBarricade(InstanceId);
                Initialized = BarricadeDrop != null;
                if (!Initialized)
                {
                    StructureSaver? saver = Warfare.Data.Singletons.GetSingleton<StructureSaver>();
                    L.LogWarning("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found, attempting to replace the barricade.");
                    if (saver == null || !saver.TryGetSave(InstanceId, EStructType.BARRICADE, out SavedStructure structure))
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: barricade save not found.");
                        Initialized = false;
                        IsActive = false;
                        return;
                    }
                    if (Assets.find(structure.ItemGuid) is not ItemBarricadeAsset asset)
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: barricade asset not found.");
                        Initialized = false;
                        IsActive = false;
                        return;
                    }
                    Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                        new Barricade(asset, asset.health, structure.Metadata),
                        structure.Position, Quaternion.Euler(structure.Rotation), structure.Owner, structure.Group);
                    if (newBarricade == null)
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: barricade could not be spawned.");
                        Initialized = false;
                        IsActive = false;
                        return;
                    }
                    BarricadeDrop newdrop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                    if (newdrop != null)
                    {
                        BarricadeDrop = newdrop;
                        structure.InstanceID = newdrop.instanceID;
                        Task.Run(() => Util.TryWrap(saver.AddOrUpdate(structure, F.DebugTimeout), "Error saving structure."));
                        InstanceId = newdrop.instanceID;
                        VehicleSpawner.SaveSingleton();
                        Initialized = true;
                    }
                    else
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: spawned barricade could not be found.");
                        Initialized = false;
                    }
                }
                if (BarricadeDrop != null)
                {
                    if (Data?.Item == null)
                        L.LogError("VEHICLE SPAWNER ERROR: Failed to find vehicle data for " + VehicleGuid.ToString("N"));
                    else if (!BarricadeDrop.model.TryGetComponent<VehicleBayComponent>(out _))
                        BarricadeDrop.model.transform.gameObject.AddComponent<VehicleBayComponent>().Init(this, Data);
                }
                else
                {
                    L.LogError("VEHICLE SPAWNER ERROR: Failed to find or create the barricade drop.");
                }
            }
            else if (StructureType == EStructType.STRUCTURE)
            {
                StructureDrop = UCBarricadeManager.FindStructure(InstanceId);
                Initialized = StructureDrop != null;
                if (!Initialized)
                {
                    StructureSaver? saver = Warfare.Data.Singletons.GetSingleton<StructureSaver>();
                    L.LogWarning("VEHICLE SPAWNER ERROR: corresponding StructureDrop could not be found, attempting to replace the structure.");
                    if (saver == null || !saver.TryGetSave(InstanceId, EStructType.STRUCTURE, out SavedStructure structure))
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: structure save not found.");
                        Initialized = false;
                        IsActive = false;
                        return;
                    }
                    if (Assets.find(structure.ItemGuid) is not ItemStructureAsset asset)
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: structure asset not found.");
                        Initialized = false;
                        IsActive = false;
                        return;
                    }
                    if (!StructureManager.dropReplicatedStructure(new Structure(asset, ushort.MaxValue),
                        structure.Position, Quaternion.Euler(structure.Rotation), structure.Owner, structure.Group))
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: Structure could not be replaced");
                        Initialized = false;
                    }
                    else if (Regions.tryGetCoordinate(structure.Position, out byte x, out byte y))
                    {
                        StructureDrop? newdrop = StructureManager.regions[x, y].drops.TailOrDefault();
                        if (newdrop == null)
                        {
                            L.LogWarning("VEHICLE SPAWNER ERROR: Spawned structure could not be found");
                            Initialized = false;
                        }
                        else
                        {
                            StructureDrop = newdrop;
                            structure.InstanceID = newdrop.instanceID;
                            InstanceId = newdrop.instanceID;
                            VehicleSpawner.SaveSingleton();
                            Task.Run(() => Util.TryWrap(saver.AddOrUpdate(structure, F.DebugTimeout), "Error saving structure."));
                            Initialized = true;
                        }
                    }
                    else
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: Unable to get region coordinates.");
                        Initialized = false;
                    }
                }
                if (StructureDrop != null)
                {
                    if (Data?.Item == null)
                        L.LogError("VEHICLE SPAWNER ERROR: Failed to find vehicle data for " + VehicleGuid.ToString("N"));
                    else if (!StructureDrop.model.TryGetComponent<VehicleBayComponent>(out _))
                        StructureDrop.model.transform.gameObject.AddComponent<VehicleBayComponent>().Init(this, Data);
                }
                else
                {
                    L.LogError("VEHICLE SPAWNER ERROR: Failed to find or create the structure drop.");
                }
            }
            IsActive = Initialized;
        }
        catch (Exception ex)
        {
            Initialized = false;
            L.LogError("Error initializing vehicle spawn: ");
            L.LogError(ex);
        }
    }

    internal void ReportError(LogSeverity severity, string messsage, Exception? ex = null)
    {
        if (severity == LogSeverity.Debug && !UCWarfare.Config.Debug)
            return;
        string msg = ToString() + ": " + messsage;
        switch (severity)
        {
            case LogSeverity.Debug:
                L.LogDebug(msg);
                break;
            case LogSeverity.Info:
                L.Log(msg);
                break;
            case LogSeverity.Warning:
                L.LogWarning(msg, method: "VEHICLE SPAWNER");
                break;
            case LogSeverity.Exception:
                throw new Exception(msg, ex);
            default:
                L.LogError(msg, method: "VEHICLE SPAWNER");
                break;
        }
        if (ex != null)
            L.LogError(ex, method: "VEHICLE SPAWNER");
    }
    public async Task<InteractableVehicle?> SpawnVehicle(CancellationToken token = default)
    {
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate(token);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasLinkedVehicle(out InteractableVehicle vehicle))
        {
            ReportError(LogSeverity.Debug, "Vehicle already linked: {#" + vehicle.instanceID + "}.");
            return null;
        }
        try
        {
            if (!Warfare.Data.Singletons.TryGetSingleton(out VehicleBay bay))
            {
                ReportError(LogSeverity.Error, "VehicleBay not loaded when spawning vehicle.");
                return null;
            }
            if (!Initialized)
            {
                ReportError(LogSeverity.Error, "Spawn not initialized when spawning vehicle.");
                return null;
            }
            if (StructureType is not EStructType.BARRICADE and not EStructType.STRUCTURE)
            {
                ReportError(LogSeverity.Error, "Spawn has incorrect structure type.");
                return null;
            }
            if ((StructureType == EStructType.BARRICADE && BarricadeDrop == null) || (StructureType == EStructType.STRUCTURE && StructureDrop == null))
            {
                ReportError(LogSeverity.Error, "Unable to find drop when spawning vehicle.");
                return null;
            }

            Vector3 euler = StructureType == EStructType.BARRICADE
                ? F.BytesToEulerForVehicle(BarricadeDrop!.GetServersideData().angle_x,
                    BarricadeDrop.GetServersideData().angle_y, BarricadeDrop.GetServersideData().angle_z)
                : F.BytesToEulerForVehicle(StructureDrop!.GetServersideData().angle_x,
                    StructureDrop.GetServersideData().angle_y, StructureDrop.GetServersideData().angle_z);

            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 offset = (StructureType == EStructType.BARRICADE
                ? BarricadeDrop!.model
                : StructureDrop!.model).position
                             + new Vector3(0f, VehicleSpawner.VEHICLE_HEIGHT_OFFSET, 0f);
            InteractableVehicle? veh = await bay.SpawnLockedVehicle(VehicleGuid, offset, rotation, token: token).ThenToUpdate(token);
            if (veh == null)
            {
                ReportError(LogSeverity.Error, "Unable to spawn vehicle, unknown reason.");
                return null;
            }
            LinkNewVehicle(veh);
            UpdateSign();
            ReportError(LogSeverity.Debug, "Spawned vehicle.");
            return veh;
        }
        catch (Exception ex)
        {
            L.LogError($"Error spawning vehicle {this.VehicleGuid} on vehicle bay: ");
            L.LogError(ex);
        }
        return null;
    }
    public bool HasLinkedVehicle(out InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleInstanceID == 0)
        {
            vehicle = null!;
            return false;
        }
        vehicle = VehicleManager.getVehicle(VehicleInstanceID);
        return vehicle != null && !vehicle.isDead && !vehicle.isDrowned && !vehicle.isExploded;
    }
    public void LinkNewVehicle(InteractableVehicle vehicle)
    {
        VehicleInstanceID = vehicle.instanceID;
        if (StructureType == EStructType.STRUCTURE)
        {
            if (StructureDrop != null && StructureDrop.model.TryGetComponent(out VehicleBayComponent comp))
                comp.OnSpawn(vehicle);
        }
        else if (StructureType == EStructType.BARRICADE && BarricadeDrop != null && BarricadeDrop.model.TryGetComponent(out VehicleBayComponent comp))
            comp.OnSpawn(vehicle);
    }
    public void Unlink()
    {
        VehicleInstanceID = 0;
    }

    public void UpdateSign(SteamPlayer player)
    {
        if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
        UpdateSignInternal(player, this);
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
        if (TeamManager.Team1Main.IsInside(LinkedSign.SignDrop.model.transform.position))
        {
            IEnumerator<SteamPlayer> t1Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 1);
            UpdateSignInternal(t1Main, this, 1ul);
        }
        else if (TeamManager.Team2Main.IsInside(LinkedSign.SignDrop.model.transform.position))
        {
            IEnumerator<SteamPlayer> t2Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 2);
            UpdateSignInternal(t2Main, this, 2ul);
        }
        else if (Regions.tryGetCoordinate(LinkedSign.SignDrop.model.transform.position, out byte x, out byte y))
        {
            IEnumerator<SteamPlayer> everyoneElse = F.EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            UpdateSignInternal(everyoneElse, this, 0);
        }
        else
        {
            L.LogWarning($"Vehicle sign not in main bases or any region!");
        }
    }
    private static void UpdateSignInternal(IEnumerator<SteamPlayer> players, VehicleSpawn spawn, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleSigns.Loaded && VehicleSpawner.Loaded && spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable != null)
        {
            if (spawn.Data?.Item == null)
                return;
            foreach (LanguageSet set in LanguageSet.All(players))
            {
                string val = Localization.TranslateVBS(spawn, spawn.Data.Item, set.Language, team == 0 ? spawn.Data.Item.Team : team);
                NetId id = spawn.LinkedSign.SignInteractable.GetNetId();
                while (set.MoveNext())
                {
                    try
                    {
                        string val2 = Signs.QuickFormat(val, spawn.Data.Item.GetCostLine(set.Next));
                        Warfare.Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val2);
                    }
                    catch (FormatException)
                    {
                        Warfare.Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val);
                        L.LogError("Formatting error in send vbs!");
                    }
                }
            }
        }
        players.Dispose();
    }
    private void UpdateSignInternal(SteamPlayer player, VehicleSpawn spawn)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable != null)
        {
            if (Data?.Item == null)
                return;
            if (!Warfare.Data.Languages.TryGetValue(player.playerID.steamID.m_SteamID, out string lang))
                lang = L.DEFAULT;
            string val = Localization.TranslateVBS(spawn, Data.Item, lang, player.GetTeam());
            try
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(player);
                if (pl == null) return;
                string val2 = string.Format(val, Data.Item.GetCostLine(pl));
                Warfare.Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable,
                    player.transportConnection, val2);
            }
            catch (FormatException)
            {
                Warfare.Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable,
                    player.transportConnection, val);
                L.LogError("Formatting error in send vbs!");
            }
        }
    }
    public override string ToString() => $"Bay Instance id: {InstanceId}, Guid: {Assets.find(VehicleGuid)?.FriendlyName ?? VehicleGuid.ToString("N")}" +
                                         $", Type: {StructureType}, {(Data is null ? "<unknown vehicle bay data>" : Data.ToString())}";
}
public enum EVehicleBayState : byte
{
    UNKNOWN = 0,
    READY = 1,
    IDLE = 2,
    DEAD = 3,
    TIME_DELAYED = 4,
    IN_USE = 5,
    DELAYED = 6,
    NOT_INITIALIZED = 255,
}

public sealed class VehicleBayComponent : MonoBehaviour
{
    private VehicleSpawn spawnData;
    private SqlItem<VehicleData> vehicleData;
    public VehicleSpawn Spawn => spawnData;
    private EVehicleBayState state = EVehicleBayState.NOT_INITIALIZED;
    private InteractableVehicle? vehicle;
    public EVehicleBayState State => state;
    public Vector3 lastLoc = Vector3.zero;
    public float RequestTime;
    public void Init(VehicleSpawn spawn, SqlItem<VehicleData> data)
    {
        spawnData = spawn;
        vehicleData = data;
        state = EVehicleBayState.UNKNOWN;
    }
    private int lastLocIndex = -1;
    private float idleStartTime = -1f;
    public string CurrentLocation = string.Empty;
    public float IdleTime;
    public float DeadTime;
    private float deadStartTime = -1f;
    private float lastIdleCheck = 0f;
    private float lastSignUpdate = 0f;
    private float lastLocCheck = 0f;
    private float lastDelayCheck = 0f;
    public void OnSpawn(InteractableVehicle vehicle)
    {
        RequestTime = 0f;
        this.vehicle = vehicle;
        IdleTime = 0f;
        DeadTime = 0f;
        if (vehicleData.Item != null && vehicleData.Item.IsDelayed(out Delay delay))
            this.state = delay.Type == DelayType.Time ? EVehicleBayState.TIME_DELAYED : EVehicleBayState.DELAYED;
        else this.state = EVehicleBayState.READY;
    }
    public void OnRequest()
    {
        RequestTime = Time.realtimeSinceStartup;
        state = EVehicleBayState.IN_USE;
    }
    private bool checkTime = false;
    public void UpdateTimeDelay()
    {
        checkTime = true;
    }
    [UsedImplicitly]
    void FixedUpdate()
    {
        if (state == EVehicleBayState.NOT_INITIALIZED) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float time = Time.realtimeSinceStartup;
        if (checkTime || (time - lastDelayCheck > 1f && (state == EVehicleBayState.UNKNOWN || state == EVehicleBayState.DELAYED || state == EVehicleBayState.TIME_DELAYED)))
        {
            lastDelayCheck = time;
            checkTime = false;
            if (vehicleData.Item != null && vehicleData.Item.IsDelayed(out Delay delay))
            {
                if (delay.Type == DelayType.Time)
                {
                    state = EVehicleBayState.TIME_DELAYED;
                    lastSignUpdate = time;
                    UpdateSign();
                    return;
                }
                else if (state != EVehicleBayState.DELAYED)
                {
                    state = EVehicleBayState.DELAYED;
                    lastSignUpdate = time;
                    UpdateSign();
                }
            }
            else if (vehicle != null && spawnData.HasLinkedVehicle(out InteractableVehicle veh) && !veh.lockedOwner.IsValid())
            {
                state = EVehicleBayState.READY;
                lastSignUpdate = time;
                UpdateSign();
            }
            else
            {
                state = EVehicleBayState.IN_USE;
                lastSignUpdate = time;
                UpdateSign();
            }
        }
        if ((state == EVehicleBayState.IDLE || state == EVehicleBayState.IN_USE) && time - lastIdleCheck >= 4f)
        {
            lastIdleCheck = time;
            if (vehicle != null && (vehicle.anySeatsOccupied || PlayerManager.IsPlayerNearby(vehicle.lockedOwner.m_SteamID, 150f, vehicle.transform.position)))
            {
                if (state != EVehicleBayState.IN_USE)
                {
                    state = EVehicleBayState.IN_USE;
                    lastSignUpdate = time;
                    UpdateSign();
                }
            }
            else if (state != EVehicleBayState.IDLE)
            {
                idleStartTime = time;
                IdleTime = 0f;
                state = EVehicleBayState.IDLE;
                lastSignUpdate = time;
                UpdateSign();
            }
        }
        if (state != EVehicleBayState.DEAD && state >= EVehicleBayState.READY && state <= EVehicleBayState.IN_USE)
        {
            if (vehicle is null || vehicle.isDead || vehicle.isExploded)
            {
                // carry over idle time to dead timer
                if (state == EVehicleBayState.IDLE)
                {
                    deadStartTime = idleStartTime;
                    DeadTime = deadStartTime < 0 ? 0 : time - deadStartTime;
                }
                else
                {
                    deadStartTime = time;
                    DeadTime = 0f;
                }
                state = EVehicleBayState.DEAD;
                lastSignUpdate = time;
                UpdateSign();
            }
        }
        if (state == EVehicleBayState.IN_USE && time - lastLocCheck > 4f && vehicle != null && !vehicle.isDead)
        {
            lastLocCheck = time;
            if (lastLoc != vehicle.transform.position)
            {
                lastLoc = vehicle.transform.position;
                int ind = F.GetClosestLocationIndex(lastLoc);
                if (ind != lastLocIndex)
                {
                    lastLocIndex = ind;
                    CurrentLocation = ((LocationNode)LevelNodes.nodes[ind]).name;
                    lastSignUpdate = time;
                    UpdateSign();
                }
            }
        }
        if (state == EVehicleBayState.IDLE)
            IdleTime = idleStartTime < 0 ? 0 : time - idleStartTime;
        else if (state == EVehicleBayState.DEAD)
            DeadTime = deadStartTime < 0 ? 0 : time - deadStartTime;
        if (vehicleData.Item != null && ((state == EVehicleBayState.IDLE && IdleTime > vehicleData.Item.RespawnTime) || (state == EVehicleBayState.DEAD && DeadTime > vehicleData.Item.RespawnTime)))
        {
            if (vehicle != null)
                VehicleBay.DeleteVehicle(vehicle);
            vehicle = null;
            Task.Run(() => spawnData.SpawnVehicle());
        }
        if (state is EVehicleBayState.IDLE or EVehicleBayState.DEAD or EVehicleBayState.TIME_DELAYED && time - lastSignUpdate >= 1f)
        {
            lastSignUpdate = time;
            UpdateSign();
        }
    }
    [UsedImplicitly]
    void OnDestroy()
    {
        state = EVehicleBayState.UNKNOWN;
        UpdateSign();
    }
    private void UpdateSign() => spawnData.UpdateSign();

    internal void TimeSync()
    {
        checkTime = true;
        lastIdleCheck = 0f;
        lastSignUpdate = 0f;
        lastLocCheck = 0f;
        lastDelayCheck = 0f;
    }
}