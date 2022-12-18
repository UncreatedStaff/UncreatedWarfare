using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
public class VehicleSpawner : ListSqlSingleton<VehicleSpawn>, ILevelStartListener, IGameStartListener, IStagingPhaseOverListener
{
    public override MySqlDatabase Sql => Data.AdminSql;
    public override bool AwaitLoad => true;
    public VehicleSpawner() : base("vehiclespawns", SCHEMAS) { }
    public override Task PreLoad(CancellationToken token)
    {
        TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
        TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
        UCPlayerKeys.SubscribeKeyUp(SpawnCountermeasuresPressed, Data.Keys.SpawnCountermeasures);
        return base.PreLoad(token);
    }

    public override Task PreUnload(CancellationToken token)
    {
        UCPlayerKeys.UnsubscribeKeyUp(SpawnCountermeasuresPressed, Data.Keys.SpawnCountermeasures);
        TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
        return base.PreUnload(token);
    }

    public static bool CanUseCountermeasures(InteractableVehicle vehicle) => true;
    private void OnPlayerEnterMain(UCPlayer player, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        WriteWait();
        try
        {
            for (int i = 0; i < Items.Count; i++)
            {
                SavedStructure? sign = Items[i].Item?.Sign?.Item;
                if (sign?.Buildable?.Drop is not BarricadeDrop drop)
                    continue;
                if (team == 1 && TeamManager.Team1Main.IsInside(sign.Position) || team == 2 && TeamManager.Team2Main.IsInside(sign.Position))
                    Signs.SendSignUpdate(drop, player, false);
            }
        }
        finally
        {
            WriteRelease();
        }
    }
    private void OnPlayerLeftMain(UCPlayer player, ulong team)
    {
        // todo solo warning
    }
    private void SpawnCountermeasuresPressed(UCPlayer player, float timeDown, ref bool handled)
    {
        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle != null &&
            player.Player.movement.getSeat() == 0 &&
            (vehicle.asset.engine == EEngine.HELICOPTER || vehicle.asset.engine == EEngine.PLANE) && CanUseCountermeasures(vehicle) &&
            vehicle.transform.TryGetComponent(out VehicleComponent component))
        {
            component.TrySpawnCountermeasures();
        }
    }

    void ILevelStartListener.OnLevelReady()
    {
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
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        Signs.UpdateVehicleBaySigns(null, null);
    }
    void IStagingPhaseOverListener.OnStagingPhaseOver()
    {
        throw new NotImplementedException();
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
        int pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 4 : 3];
        objs[0] = item.VehicleKey.Key;
        objs[1] = item.StructureKey.Key;
        objs[2] = item.SignKey.IsValid ? item.SignKey.Key : DBNull.Value;
        if (hasPk)
            objs[3] = pk.Key;
        await Sql.QueryAsync(F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN),
            objs, reader =>
            {
                pk2 = reader.GetInt32(0);
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
        await Sql.QueryAsync(F.BuildSelectWhereLimit1(TABLE_MAIN, COLUMN_PK, 0, COLUMN_PK, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN), new object[] { pk2 },
            reader =>
            {
                obj = new VehicleSpawn(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? PrimaryKey.NotAssigned : reader.GetInt32(3));
            }, token).ConfigureAwait(false);

        return obj;
    }
    [Obsolete]
    protected override async Task<VehicleSpawn[]> DownloadAllItems(CancellationToken token = default)
    {
        List<VehicleSpawn> spawns = new List<VehicleSpawn>(32);
        await Sql.QueryAsync(F.BuildSelect(TABLE_MAIN, COLUMN_PK, COLUMN_VEHICLE, COLUMN_STRUCTURE, COLUMN_SIGN), null,
            reader =>
            {
                spawns.Add(new VehicleSpawn(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? PrimaryKey.NotAssigned : reader.GetInt32(3)));
            }, token).ConfigureAwait(false);

        return spawns.ToArray();
    }
    #endregion
}
[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
[SingletonDependency(typeof(Level))]
public class VehicleSpawnerOld : ListSingleton<VehicleSpawn>, ILevelStartListenerAsync, IGameStartListener, IStagingPhaseOverListener
{
    public static VehicleSpawnerOld Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSpawnerOld, VehicleSpawn>();
    public const float VEHICLE_HEIGHT_OFFSET = 5f;
    public VehicleSpawnerOld() : base("vehiclespawns", Path.Combine(Data.Paths.VehicleStorage, "vehiclespawns.json")) { }
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
    async Task ILevelStartListenerAsync.OnLevelReady(CancellationToken token)
    {
        LoadSpawns();
        await RespawnAllVehicles(token).ConfigureAwait(false);
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
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(data.barricade.asset.GUID) && IsRegistered(data.instanceID, out VehicleSpawn spawn, StructType.Barricade))
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
            DeleteSpawn(data.instanceID, StructType.Barricade);
        }
    }
    internal void OnStructureDestroyed(StructureDestroyed e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Gamemode.Config.StructureVehicleBay.MatchGuid(e.Structure.asset.GUID) && IsRegistered(e.InstanceID, out VehicleSpawn spawn, StructType.Structure))
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
            DeleteSpawn(e.InstanceID, StructType.Structure);
        }
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
        Singleton.Save();
    }
    public static async Task RespawnAllVehicles(CancellationToken token = default)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, StructType.Barricade);
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        Data.Singletons.GetSingleton<StructureSaver>()?.BeginAddBarricade(drop);
        Task.Run(() => spawn.SpawnVehicle());
    }
    public static void CreateSpawn(StructureDrop drop, StructureData data, Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, StructType.Structure);
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        Data.Singletons.GetSingleton<StructureSaver>()?.BeginAddStructure(drop);
        Task.Run(() => spawn.SpawnVehicle());
    }
    public static void DeleteSpawn(uint barricadeInstanceID, StructType type)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = Singleton.GetObject(s => s.InstanceId == barricadeInstanceID && s.StructureType == type);
        if (spawn != null)
        {
            if (spawn.StructureType == StructType.Structure && spawn.StructureDrop != null && spawn.StructureDrop.model.TryGetComponent(out VehicleBayComponent vbc))
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
        if (saver != null && saver.TryGetSaveNoLock(barricadeInstanceID, type, out SavedStructure structure))
            saver.BeginRemoveItem(structure);
    }
    public static bool IsRegistered(uint barricadeInstanceID, out VehicleSpawn spawn, StructType type)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == StructType.Barricade)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.InstanceId, out spawn);
        else if (type == StructType.Structure)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.InstanceId, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool IsRegistered(SerializableTransform transform, out VehicleSpawn spawn, StructType type)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == StructType.Barricade)
            return Singleton.ObjectExists(s => s.StructureType == StructType.Barricade && s.BarricadeDrop != null && transform == s.BarricadeDrop.model.transform, out spawn);
        else if (type == StructType.Structure)
            return Singleton.ObjectExists(s => s.StructureType == StructType.Structure && s.StructureDrop != null && transform == s.StructureDrop.model.transform, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool UnusedSpawnExists(Guid vehicleID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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

    public static bool SpawnExists(uint bayInstanceID, StructType type, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
        return Singleton.ObjectExists(s => s.InstanceId == bayInstanceID && s.StructureType == type, out spawn);
    }

    public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
            Singleton[i].UpdateSign();
    }
    public static void UpdateSigns(uint vehicle)
    {
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
        Singleton.AssertLoaded<VehicleSpawnerOld, VehicleSpawn>();
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
public class VehicleSpawn : IListItem
{
    private SqlItem<VehicleData>? _vehicle;
    private SqlItem<SavedStructure>? _structure;
    private SqlItem<SavedStructure>? _sign;
    private PrimaryKey _signKey;
    private PrimaryKey _structureKey;
    private PrimaryKey _vehicleKey;
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
    public VehicleSpawn(PrimaryKey key, PrimaryKey vehicle, PrimaryKey structure, PrimaryKey sign)
    {
        PrimaryKey = key;
        VehicleKey = vehicle;
        StructureKey = structure;
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
            if (StructureType is not StructType.Barricade and not StructType.Structure)
            {
                ReportError(LogSeverity.Error, "Spawn has incorrect structure type.");
                return null;
            }
            if ((StructureType == StructType.Barricade && BarricadeDrop == null) || (StructureType == StructType.Structure && StructureDrop == null))
            {
                ReportError(LogSeverity.Error, "Unable to find drop when spawning vehicle.");
                return null;
            }

            Vector3 euler = StructureType == StructType.Barricade
                ? F.BytesToEulerForVehicle(BarricadeDrop!.GetServersideData().angle_x,
                    BarricadeDrop.GetServersideData().angle_y, BarricadeDrop.GetServersideData().angle_z)
                : F.BytesToEulerForVehicle(StructureDrop!.GetServersideData().angle_x,
                    StructureDrop.GetServersideData().angle_y, StructureDrop.GetServersideData().angle_z);

            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 offset = (StructureType == StructType.Barricade
                ? BarricadeDrop!.model
                : StructureDrop!.model).position
                             + new Vector3(0f, VehicleSpawnerOld.VEHICLE_HEIGHT_OFFSET, 0f);
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
        if (StructureType == StructType.Structure)
        {
            if (StructureDrop != null && StructureDrop.model.TryGetComponent(out VehicleBayComponent comp))
                comp.OnSpawn(vehicle);
        }
        else if (StructureType == StructType.Barricade && BarricadeDrop != null && BarricadeDrop.model.TryGetComponent(out VehicleBayComponent comp))
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
        if (VehicleSigns.Loaded && VehicleSpawnerOld.Loaded && spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable != null)
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
                lang = L.Default;
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
    private int _lastLocIndex = -1;
    private float _idleStartTime = -1f;
    public string CurrentLocation = string.Empty;
    public float IdleTime;
    public float DeadTime;
    private float _deadStartTime = -1f;
    private float _lastIdleCheck;
    private float _lastSignUpdate;
    private float _lastLocCheck;
    private float _lastDelayCheck;
    public void OnSpawn(InteractableVehicle vehicle)
    {
        RequestTime = 0f;
        this._vehicle = vehicle;
        IdleTime = 0f;
        DeadTime = 0f;
        if (_vehicleData.Item != null && _vehicleData.Item.IsDelayed(out Delay delay))
            this._state = delay.Type == DelayType.Time ? VehicleBayState.TimeDelayed : VehicleBayState.Delayed;
        else this._state = VehicleBayState.Ready;
    }
    public void OnRequest()
    {
        RequestTime = Time.realtimeSinceStartup;
        _state = VehicleBayState.InUse;
    }
    private bool _checkTime = false;
    public void UpdateTimeDelay()
    {
        _checkTime = true;
    }
    [UsedImplicitly]
    void FixedUpdate()
    {
        if (_state == VehicleBayState.NotInitialized) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float time = Time.realtimeSinceStartup;
        if (_checkTime || (time - _lastDelayCheck > 1f && (_state == VehicleBayState.Unknown || _state == VehicleBayState.Delayed || _state == VehicleBayState.TimeDelayed)))
        {
            _lastDelayCheck = time;
            _checkTime = false;
            if (_vehicleData.Item != null && _vehicleData.Item.IsDelayed(out Delay delay))
            {
                if (delay.Type == DelayType.Time)
                {
                    _state = VehicleBayState.TimeDelayed;
                    _lastSignUpdate = time;
                    UpdateSign();
                    return;
                }
                else if (_state != VehicleBayState.Delayed)
                {
                    _state = VehicleBayState.Delayed;
                    _lastSignUpdate = time;
                    UpdateSign();
                }
            }
            else if (_vehicle != null && _spawnData.Item != null && _spawnData.Item.HasLinkedVehicle(out InteractableVehicle veh) && !veh.lockedOwner.IsValid())
            {
                _state = VehicleBayState.Ready;
                _lastSignUpdate = time;
                UpdateSign();
            }
            else
            {
                _state = VehicleBayState.InUse;
                _lastSignUpdate = time;
                UpdateSign();
            }
        }
        if ((_state == VehicleBayState.Idle || _state == VehicleBayState.InUse) && time - _lastIdleCheck >= 4f)
        {
            _lastIdleCheck = time;
            if (_vehicle != null && (_vehicle.anySeatsOccupied || PlayerManager.IsPlayerNearby(_vehicle.lockedOwner.m_SteamID, 150f, _vehicle.transform.position)))
            {
                if (_state != VehicleBayState.InUse)
                {
                    _state = VehicleBayState.InUse;
                    _lastSignUpdate = time;
                    UpdateSign();
                }
            }
            else if (_state != VehicleBayState.Idle)
            {
                _idleStartTime = time;
                IdleTime = 0f;
                _state = VehicleBayState.Idle;
                _lastSignUpdate = time;
                UpdateSign();
            }
        }
        if (_state != VehicleBayState.Dead && _state >= VehicleBayState.Ready && _state <= VehicleBayState.InUse)
        {
            if (_vehicle is null || _vehicle.isDead || _vehicle.isExploded)
            {
                // carry over idle time to dead timer
                if (_state == VehicleBayState.Idle)
                {
                    _deadStartTime = _idleStartTime;
                    DeadTime = _deadStartTime < 0 ? 0 : time - _deadStartTime;
                }
                else
                {
                    _deadStartTime = time;
                    DeadTime = 0f;
                }
                _state = VehicleBayState.Dead;
                _lastSignUpdate = time;
                UpdateSign();
            }
        }
        if (_state == VehicleBayState.InUse && time - _lastLocCheck > 4f && _vehicle != null && !_vehicle.isDead)
        {
            _lastLocCheck = time;
            if (LastLocation != _vehicle.transform.position)
            {
                LastLocation = _vehicle.transform.position;
                int ind = F.GetClosestLocationIndex(LastLocation);
                if (ind != _lastLocIndex)
                {
                    _lastLocIndex = ind;
                    CurrentLocation = ((LocationNode)LevelNodes.nodes[ind]).name;
                    _lastSignUpdate = time;
                    UpdateSign();
                }
            }
        }
        if (_state == VehicleBayState.Idle)
            IdleTime = _idleStartTime < 0 ? 0 : time - _idleStartTime;
        else if (_state == VehicleBayState.Dead)
            DeadTime = _deadStartTime < 0 ? 0 : time - _deadStartTime;
        if (_vehicleData.Item != null && ((_state == VehicleBayState.Idle && IdleTime > _vehicleData.Item.RespawnTime) || (_state == VehicleBayState.Dead && DeadTime > _vehicleData.Item.RespawnTime)))
        {
            if (_vehicle != null)
                VehicleBay.DeleteVehicle(_vehicle);
            _vehicle = null;
            if (_spawnData.Item != null)
                UCWarfare.RunTask(_spawnData.Item.SpawnVehicle());
        }
        if (_state is VehicleBayState.Idle or VehicleBayState.Dead or VehicleBayState.TimeDelayed && time - _lastSignUpdate >= 1f)
        {
            _lastSignUpdate = time;
            UpdateSign();
        }
    }
    [UsedImplicitly]
    void OnDestroy()
    {
        _state = VehicleBayState.Unknown;
        UpdateSign();
    }
    private void UpdateSign() => _spawnData.Item?.UpdateSign();

    internal void TimeSync()
    {
        _checkTime = true;
        _lastIdleCheck = 0f;
        _lastSignUpdate = 0f;
        _lastLocCheck = 0f;
        _lastDelayCheck = 0f;
    }
}