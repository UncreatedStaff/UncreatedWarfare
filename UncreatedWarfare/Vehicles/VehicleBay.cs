using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(Whitelister))]
public class VehicleBay : ListSqlSingleton<VehicleData>, ILevelStartListenerAsync, IDeclareWinListenerAsync
{
    public VehicleBay() : base("vehiclebay", SCHEMAS)
    {
    }

    public override bool AwaitLoad => true;

    public override MySqlDatabase Sql => Data.AdminSql;
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
        if (MapScheduler.Current == -1)
            throw new InvalidOperationException("MapScheduler not loaded.");
        bool hasPk = pk.IsValid;
        int pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 14 : 13];
        objs[0] = MapScheduler.Current;
        objs[1] = item.Faction;
        objs[2] = item.RespawnTime;
        objs[3] = item.TicketCost;
        objs[4] = item.CreditCost;
        objs[5] = item.RearmCost;
        objs[6] = item.Cooldown;
        objs[7] = item.Branch.ToString();
        objs[8] = item.RequiredClass.ToString();
        objs[9] = item.Type.ToString();
        objs[10] = item.RequiresSL;
        objs[11] = item.DisallowAbandons;
        objs[12] = item.AbandonValueLossSpeed;
        if (hasPk)
            objs[13] = pk.Key;
        await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_MAP}`,`{COLUMN_FACTION}`,`{COLUMN_RESPAWN_TIME}`," +
                             $"`{COLUMN_TICKET_COST}`,`{COLUMN_CREDIT_COST}`,`{COLUMN_REARM_COST}`,`{COLUMN_COOLDOWN}`," +
                             $"`{COLUMN_BRANCH}`,`{COLUMN_REQUIRED_CLASS}`,`{COLUMN_VEHICLE_TYPE}`,`{COLUMN_REQUIRES_SQUADLEADER}`," +
                             $"`{COLUMN_ABANDON_BLACKLISTED}`,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}`" +
                             (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
                             ") VALUES (@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12" +
                             (hasPk ? ",@13" : string.Empty) +
                             ") ON DUPLICATE KEY UPDATE " +
                             $"`{COLUMN_MAP}`=@0,`{COLUMN_FACTION}`=@1,`{COLUMN_RESPAWN_TIME}`=@2,`{COLUMN_TICKET_COST}`=@3," +
                             $"`{COLUMN_CREDIT_COST}`=@4,`{COLUMN_REARM_COST}`=@5,`{COLUMN_COOLDOWN}`=@6,`{COLUMN_BRANCH}`=@7,`{COLUMN_REQUIRED_CLASS}`=@8," +
                             $"`{COLUMN_VEHICLE_TYPE}`=@9,`{COLUMN_REQUIRES_SQUADLEADER}`=@10,`{COLUMN_ABANDON_BLACKLISTED}`=@11,`{COLUMN_ABANDON_VALUE_LOSS_SPEED}`=@12," +
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
                objs[index] = delay.type.ToString();
                objs[index + 1] = delay.type switch
                {
                    EDelayType.OUT_OF_STAGING or EDelayType.NONE => DBNull.Value,
                    _ => delay.value
                };
                objs[index + 2] = (object?)delay.gamemode ?? DBNull.Value;
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
    protected override Task<VehicleData[]> DownloadAllItems(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
    [Obsolete]
    protected override Task<VehicleData?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    Task ILevelStartListenerAsync.OnLevelReady()
    {
        throw new NotImplementedException();
    }

    Task IDeclareWinListenerAsync.OnWinnerDeclared(ulong winner)
    {
        throw new NotImplementedException();
    }

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
            new Schema.Column(COLUMN_REQUIRES_SQUADLEADER, SqlTypes.BOOLEAN)
            {
                Default = "0"
            },
            new Schema.Column(COLUMN_ABANDON_BLACKLISTED, SqlTypes.BOOLEAN)
            {
                Default = "0"
            },
            new Schema.Column(COLUMN_ABANDON_VALUE_LOSS_SPEED, SqlTypes.BOOLEAN)
            {
                Default = "0.125"
            },
        }, true, typeof(VehicleData));
        SCHEMAS[1] = BaseUnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[2] = Delay.GetDefaultSchema(TABLE_DELAYS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[3] = F.GetListSchema<Guid>(TABLE_ITEMS, COLUMN_EXT_PK, COLUMN_ITEM_GUID, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[4] = F.GetListSchema<byte>(TABLE_CREW_SEATS, COLUMN_EXT_PK, COLUMN_CREW_SEATS_SEAT, TABLE_MAIN, COLUMN_PK);
        SCHEMAS[5] = KitItem.GetDefaultSchema(TABLE_TRUNK_ITEMS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, includePage: false);
        Schema[] vbarrs = VBarricade.GetDefaultSchemas(TABLE_BARRICADES, TABLE_BARRICADE_ITEMS, TABLE_BARRICADE_DISPLAY_DATA, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, includeHealth: true);
        Array.Copy(vbarrs, 0, SCHEMAS, 6, vbarrs.Length);
    }
}

[SingletonDependency(typeof(Whitelister))]
public class VehicleBayOld : ListSingleton<VehicleData>, ILevelStartListener, IDeclareWinListener
{
    private static VehicleBayConfig _config;
    internal static VehicleBay Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleBay, VehicleData>();
    public static VehicleBayData Config => _config.Data;

    public VehicleBayOld() : base("vehiclebay", Path.Combine(Data.Paths.VehicleStorage, "vehiclebay.json"))
    {
    }
    private bool hasWhitelisted = false;
    public override void Load()
    {
        _config = new VehicleBayConfig();
        EventDispatcher.OnEnterVehicleRequested += OnVehicleEnterRequested;
        EventDispatcher.OnVehicleSwapSeatRequested += OnVehicleSwapSeatRequested;
        EventDispatcher.OnExitVehicleRequested += OnVehicleExitRequested;
        EventDispatcher.OnExitVehicle += OnVehicleExit;
        EventDispatcher.OnVehicleSpawned += OnVehicleSpawned;
        if (Whitelister.Loaded) // whitelist all vehicle bay items
            WhitelistItems();
        Singleton = this;
    }
    public void OnLevelReady()
    {
        if (!hasWhitelisted && Whitelister.Loaded)
            WhitelistItems();
    }
    public override void Unload()
    {
        Singleton = null!;
        EventDispatcher.OnVehicleSpawned -= OnVehicleSpawned;
        EventDispatcher.OnExitVehicle -= OnVehicleExit;
        EventDispatcher.OnExitVehicleRequested -= OnVehicleExitRequested;
        EventDispatcher.OnVehicleSwapSeatRequested -= OnVehicleSwapSeatRequested;
        EventDispatcher.OnEnterVehicleRequested -= OnVehicleEnterRequested;
        _config = null!;
    }
    public void OnWinnerDeclared(ulong winner)
    {
        VehicleBay.AbandonAllVehicles();
    }
    private void OnVehicleSpawned(VehicleSpawned e)
    {
        e.Vehicle.gameObject.AddComponent<VehicleComponent>().Initialize(e.Vehicle);
    }
    private void WhitelistItems()
    {
        for (int i = 0; i < Count; i++)
        {
            VehicleData data = this[i];
            if (data.Items is not null)
            {
                for (int j = 0; j < data.Items.Length; j++)
                {
                    if (!Whitelister.IsWhitelisted(data.Items[j], out _))
                        Whitelister.AddItem(data.Items[j]);
                }
            }
        }
        hasWhitelisted = true;
    }
    private void OnVehicleExit(ExitVehicle e)
    {
        if (e.OldPassengerIndex == 0 && e.Vehicle.transform.TryGetComponent(out VehicleComponent comp))
            comp.LastDriverTime = Time.realtimeSinceStartup;
        if (KitManager.KitExists(e.Player.KitName, out Kit kit))
        {
            if (kit.Class == EClass.LAT || kit.Class == EClass.HAT)
            {
                e.Player.Player.equipment.dequip();
            }
        }
    }
    public static void OnPlayerJoinedQuestHandling(UCPlayer player)
    {
        if (!Singleton.IsLoaded<VehicleBay, VehicleData>()) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            if (Singleton[i].UnlockRequirements is not null)
            {
                VehicleData data = Singleton[i];
                for (int j = 0; j < data.UnlockRequirements.Length; j++)
                {
                    if (data.UnlockRequirements[j] is QuestUnlockRequirement req && req.UnlockPresets != null && req.UnlockPresets.Length > 0 && !req.CanAccess(player))
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
                            {
                                L.LogWarning("Failed to create tracker for vehicle " + data.VehicleID.ToString("N") + ", player " + player.Name.PlayerName);
                            }
                        }
                    }
                }
            }
        }
    }
    protected override string LoadDefaults() => EMPTY_LIST;
    public static void AddRequestableVehicle(InteractableVehicle vehicle)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleData data = new VehicleData(vehicle.asset.GUID);
        data.SaveMetaData(vehicle);
        Singleton.AddObjectToSave(data);
    }
    public new static ESetFieldResult SetProperty(VehicleData data, ref string property, string value)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        return (Singleton as JSONSaver<VehicleData>).SetProperty(data, ref property, value);
    }
    public static ESetFieldResult SetProperty(Guid vehicleGuid, ref string property, string value)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        return Singleton.SetProperty(x => x.VehicleID == vehicleGuid, ref property, value);
    }
    public static void RemoveRequestableVehicle(Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        Singleton.RemoveWhere(vd => vd.VehicleID == vehicleID);
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        Singleton.Save();
    }

    public static bool VehicleExists(Guid vehicleID, out VehicleData vehicleData)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            if (Singleton[i].VehicleID == vehicleID)
            {
                vehicleData = Singleton[i];
                return true;
            }
        }
        vehicleData = null!;
        return false;
    }
    public static void SetItems(Guid vehicleID, Guid[] newItems)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        Singleton.UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.Items = newItems);
    }

    public static void AddCrewmanSeat(Guid vehicleID, byte newSeatIndex)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        Singleton.UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Add(newSeatIndex));
    }

    public static void RemoveCrewmanSeat(Guid vehicleID, byte seatIndex)
    {
        Singleton.AssertLoaded<VehicleBay, VehicleData>();
        Singleton.UpdateObjectsWhere(vd => vd.VehicleID == vehicleID, vd => vd.CrewSeats.Remove(seatIndex));
    }

    /// <exception cref="InvalidOperationException">Thrown if the level is not loaded.</exception>
    public static InteractableVehicle? SpawnLockedVehicle(Guid vehicleID, Vector3 position, Quaternion rotation, out uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            instanceID = 0;
            if (VehicleExists(vehicleID, out VehicleData vehicleData))
            {
                if (Assets.find(vehicleID) is not VehicleAsset asset)
                {
                    L.LogError("SpawnLockedVehicle: Unable to find vehicle asset of " + vehicleID.ToString());
                    return null;
                }
                InteractableVehicle vehicle = VehicleManager.spawnVehicleV2(asset.id, position, rotation);
                if (vehicle == null) return null;
                instanceID = vehicle.instanceID;

                if (vehicleData.Metadata != null)
                {
                    if (vehicleData.Metadata.TrunkItems != null)
                    {
                        foreach (KitItem k in vehicleData.Metadata.TrunkItems)
                        {
                            if (Assets.find(k.Id) is ItemAsset iasset)
                            {
                                Item item = new Item(iasset.id, k.Amount, 100, Util.CloneBytes(k.Metadata));
                                if (!vehicle.trunkItems.tryAddItem(item))
                                    ItemManager.dropItem(item, vehicle.transform.position, false, true, true);
                            }
                        }
                    }

                    if (vehicleData.Metadata.Barricades != null)
                    {
                        foreach (VBarricade vb in vehicleData.Metadata.Barricades)
                        {
                            if (Assets.find(vb.BarricadeID) is not ItemBarricadeAsset basset)
                            {
                                L.LogError("SpawnLockedVehicle: Unable to find barricade asset of " + vb.BarricadeID.ToString());
                                continue;
                            }
                            Barricade barricade = new Barricade(basset, asset.health, Convert.FromBase64String(vb.State));
                            Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                            BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                        }
                    }
                }

                if (vehicle.asset.canBeLocked)
                {
                    vehicle.tellLocked(CSteamID.Nil, CSteamID.Nil, true);

                    VehicleManager.ServerSetVehicleLock(vehicle, CSteamID.Nil, CSteamID.Nil, true);

                    vehicle.updateVehicle();
                    vehicle.updatePhysics();
                }
                return vehicle;
            }
            else
            {
                L.Log($"VEHICLE SPAWN ERROR: {(Assets.find(vehicleID) is VehicleAsset va ? va.vehicleName : vehicleID.ToString("N"))} has not been registered in the VehicleBay.");
                return null;
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error spawning vehicle: ");
            L.LogError(ex);
            instanceID = 0;
            return null;
        }
    }
    // TODO
    internal static bool OnQuestCompleted(UCPlayer player, Guid presetKey) => false;
    public static void AbandonAllVehicles()
    {
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
    public static void AbandonVehicle(InteractableVehicle vehicle, VehicleData? data, VehicleSpawn? spawn, bool respawn = true)
    {
        if (data is null && !VehicleExists(vehicle.asset.GUID, out data))
            return;
        if (spawn is null && !VehicleSpawner.HasLinkedSpawn(vehicle.instanceID, out spawn))
            return;

        UCPlayer? pl = UCPlayer.FromID(vehicle.lockedOwner.m_SteamID);
        if (pl != null)
        {
            int creditReward = 0;
            if (data.CreditCost > 0 && spawn.Component != null && spawn.Component.RequestTime != 0)
                creditReward = data.CreditCost - Mathf.Min(data.CreditCost, Mathf.FloorToInt(data.AbandonValueLossSpeed * (Time.realtimeSinceStartup - spawn.Component.RequestTime)));

            Points.AwardCredits(pl, creditReward, T.AbandonCompensationToast.Translate(pl), false, false);
        }

        VehicleBay.DeleteVehicle(vehicle);

        if (respawn)
            spawn.SpawnVehicle();
    }
    public static void ResupplyVehicleBarricades(InteractableVehicle vehicle, VehicleData vehicleData)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleBarricadeRegion? vehicleRegion = vehicle.FindRegionFromVehicleWithIndex(out ushort plant);
        if (vehicleRegion != null)
        {
            if (plant < ushort.MaxValue)
            {
                for (int i = vehicleRegion.drops.Count - 1; i >= 0; i--)
                {
                    if (i >= 0)
                    {
                        if (vehicleRegion.drops[i].interactable is InteractableStorage store)
                            store.despawnWhenDestroyed = true;

                        BarricadeManager.destroyBarricade(vehicleRegion.drops[i], 0, 0, plant);
                    }
                }
            }
            if (vehicleData.Metadata != null && vehicleData.Metadata.Barricades != null)
            {
                foreach (VBarricade vb in vehicleData.Metadata.Barricades)
                {
                    Barricade barricade;
                    if (Assets.find(vb.BarricadeID) is ItemBarricadeAsset asset)
                    {
                        barricade = new Barricade(asset, asset.health, Convert.FromBase64String(vb.State));
                    }
                    else
                    {
                        L.LogError("ResupplyVehicleBarricades: Unable to find barricade asset of " + vb.BarricadeID.ToString());
                        continue;
                    }
                    Quaternion quarternion = Quaternion.Euler(vb.AngleX * 2, vb.AngleY * 2, vb.AngleZ * 2);
                    BarricadeManager.dropPlantedBarricade(vehicle.transform, barricade, new Vector3(vb.PosX, vb.PosY, vb.PosZ), quarternion, vb.OwnerID, vb.GroupID);
                }
                EffectManager.sendEffect(30, EffectManager.SMALL, vehicle.transform.position);
            }
        }
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
    private void OnVehicleExitRequested(ExitVehicleRequested e)
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
        if (Data.Is(out IRevives r) && r.ReviveManager.DownedPlayers.ContainsKey(e.Player.Steam64))
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

            if (c.Data.CrewSeats.Contains(e.FinalSeat) && c.Data.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
            {
                if (e.Player.KitClass == c.Data.RequiredClass || e.Player.OnDuty())
                {
                    if (e.FinalSeat == 0) // if a crewman is trying to enter the driver's seat
                    {
                        bool canEnterDriverSeat = owner == null ||
                            e.Player == owner ||
                            e.Player.OnDuty() ||
                            IsOwnerInVehicle(e.Vehicle, owner) ||
                            (owner != null && owner.Squad != null && owner.Squad.Members.Contains(e.Player) ||
                            (owner!.Position - e.Vehicle.transform.position).sqrMagnitude > Math.Pow(200, 2)) ||
                            (c.Data.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(e.Vehicle.transform.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, e.Vehicle.lockedGroup.m_SteamID) != null);

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
                            if (e.Vehicle.passengers[0].player is null) // if they have no driver
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
                    e.Player.SendChat(T.VehicleMissingKit, c.Data.RequiredClass);
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
}