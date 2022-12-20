using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(Whitelister))]
public class VehicleBay : ListSqlSingleton<VehicleData>, ILevelStartListenerAsync, IDeclareWinListenerAsync, IPlayerPostInitListenerAsync, IQuestCompletedHandler
{
    private static VehicleBayConfig _config;
    private bool _hasWhitelisted;

    public static VehicleBay? GetSingletonQuick() => Data.Is(out IVehicles r) ? r.VehicleBay : null;
    public static VehicleBayData Config => _config == null ? throw new SingletonUnloadedException(typeof(VehicleBay)) : _config.Data;
    public VehicleBay() : base("vehiclebay", SCHEMAS)
    {
    }
    public override Task PreLoad(CancellationToken token)
    {
        if (_config == null)
            _config = new VehicleBayConfig();
        else _config.Reload();
        return base.PreLoad(token);
    }
    //public override async Task PostLoad(CancellationToken token)
    //{
    //    await ImportFromJson(Path.Combine(Data.Paths.VehicleStorage, "vehiclebay.json"), token: token).ConfigureAwait(false);
    //    await base.PostLoad(token);
    //}
    public override Task PostUnload(CancellationToken token)
    {
        _hasWhitelisted = false;
        return Task.CompletedTask;
    }
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public static bool CanSoloVehicle(VehicleData data) => data.Branch == Branch.Infantry;
    private void WhitelistItems()
    {
        WriteWait();
        try
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
            _hasWhitelisted = true;
        }
        finally
        {
            WriteRelease();
        }
    }
    async Task ILevelStartListenerAsync.OnLevelReady(CancellationToken token)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (Whitelister.Loaded && !_hasWhitelisted) // whitelist all vehicle bay items
            {
                if (!UCWarfare.IsMainThread)
                    await UCWarfare.ToUpdate(token);
                WhitelistItems();
            }
        }
        finally
        {
            Release();
        }
    }
    async Task IDeclareWinListenerAsync.OnWinnerDeclared(ulong winner, CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!UCWarfare.IsMainThread)
                await UCWarfare.ToUpdate(token);
            VehicleSpawner.GetSingletonQuick()?.AbandonAllVehicles(true);
        }
        finally
        {
            Release();
        }
    }
    Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player, CancellationToken token)
    {
        return SendQuests(player, token);
    }
    private async Task SendQuests(UCPlayer player, CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UCWarfare.ToUpdate(token);
            WriteWait();
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
                                    player.Player.quests.ServerAddQuest(quest);
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
                WriteRelease();
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
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public VehicleData? GetDataSync(Guid guid)
    {
        WriteWait();
        try
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

            return null;
        }
        finally
        {
            WriteRelease();
        }
    }
    public SqlItem<VehicleData>? GetDataProxySync(Guid guid)
    {
        WriteWait();
        try
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

            return null;
        }
        finally
        {
            WriteRelease();
        }
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<SqlItem<VehicleData>?> GetDataProxy(Guid guid, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            int map = MapScheduler.Current;
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
        finally
        {
            WriteRelease();
        }

        return null;
    }
    /// <remarks>Thread Safe</remarks>
    public async Task<(SetPropertyResult, MemberInfo?)> SetProperty(VehicleData data, string property, string value, CancellationToken token = default)
    {
        AssertLoadedIntl();
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
            Util.RemoveFromArray(ref proxy.Item.CrewSeats, index);
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
    
    // TODO
    void IQuestCompletedHandler.OnQuestCompleted(QuestCompleted e) { }
    #region Sql
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "vehicle_data";
    public const string TABLE_UNLOCK_REQUIREMENTS = "vehicle_data_unlock_requirements";
    public const string TABLE_DELAYS = "vehicle_data_delays";
    public const string TABLE_ITEMS = "vehicle_data_request_items";
    public const string TABLE_CREW_SEATS = "vehicle_data_crew_seats";
    public const string TABLE_BARRICADES = "vehicle_data_barricades";
    public const string TABLE_BARRICADE_ITEMS = "vehicle_data_barricade_items";
    public const string TABLE_BARRICADE_DISPLAY_DATA = "vehicle_data_barricade_display_data";
    public const string TABLE_TRUNK_ITEMS = "vehicle_data_trunk_items";

    public const string COLUMN_PK = "pk";
    public const string COLUMN_EXT_PK = "VehicleData";
    public const string COLUMN_MAP = "Map";
    public const string COLUMN_FACTION = "Faction";
    public const string COLUMN_VEHICLE_GUID = "VehicleId";
    public const string COLUMN_RESPAWN_TIME = "RespawnTime";
    public const string COLUMN_TICKET_COST = "TicketCost";
    public const string COLUMN_CREDIT_COST = "CreditCost";
    public const string COLUMN_REARM_COST = "RearmCost";
    public const string COLUMN_COOLDOWN = "Cooldown";
    public const string COLUMN_VEHICLE_TYPE = "VehicleType";
    public const string COLUMN_BRANCH = "Branch";
    public const string COLUMN_REQUIRED_CLASS = "RequiredClass";
    public const string COLUMN_REQUIRES_SQUADLEADER = "RequiresSquadleader";
    public const string COLUMN_ABANDON_BLACKLISTED = "AbandonBlacklisted";
    public const string COLUMN_ABANDON_VALUE_LOSS_SPEED = "AbandonValueLossSpeed";
    public const string COLUMN_ITEM_GUID = "Item";
    public const string COLUMN_CREW_SEATS_SEAT = "Index";
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
                ForeignKeyTable = FactionInfo.TABLE_MAIN,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull
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
            new Schema.Column(COLUMN_VEHICLE_TYPE, SqlTypes.Enum<VehicleType>())
            {
                Default = nameof(VehicleType.None)
            },
            new Schema.Column(COLUMN_BRANCH, SqlTypes.Enum<Branch>())
            {
                Default = nameof(Branch.Default)
            },
            new Schema.Column(COLUMN_REQUIRED_CLASS, SqlTypes.Enum<Class>())
            {
                Default = nameof(Class.None)
            },
            new Schema.Column(COLUMN_REQUIRES_SQUADLEADER, SqlTypes.BOOLEAN),
            new Schema.Column(COLUMN_ABANDON_BLACKLISTED, SqlTypes.BOOLEAN),
            new Schema.Column(COLUMN_ABANDON_VALUE_LOSS_SPEED, SqlTypes.FLOAT)
            {
                Default = "0.125"
            },
        }, true, typeof(VehicleData));
        SCHEMAS[1] = UnlockRequirement.GetDefaultSchema(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK);
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
        SCHEMAS[5] = PageItem.GetDefaultSchema(TABLE_TRUNK_ITEMS, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, true, includePage: false);
        Schema[] vbarrs = VBarricade.GetDefaultSchemas(TABLE_BARRICADES, TABLE_BARRICADE_ITEMS, TABLE_BARRICADE_DISPLAY_DATA, COLUMN_EXT_PK, TABLE_MAIN, COLUMN_PK, includeHealth: false);
        Array.Copy(vbarrs, 0, SCHEMAS, 6, vbarrs.Length);
    }
    // ReSharper restore InconsistentNaming

    [Obsolete]
    protected override async Task AddOrUpdateItem(VehicleData? item, PrimaryKey pk, CancellationToken token = default)
    {
        await UCWarfare.ToLevelLoad(token);
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
        objs[1] = item.Faction.IsValid && item.Faction.Key != 0 ? item.Faction.Key : DBNull.Value;
        objs[2] = item.VehicleID.ToString("N");
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
            objs[14] = pk.Key;
        await Sql.QueryAsync(
            F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk,
                COLUMN_MAP, COLUMN_FACTION, COLUMN_VEHICLE_GUID, COLUMN_RESPAWN_TIME,
                COLUMN_TICKET_COST, COLUMN_CREDIT_COST, COLUMN_REARM_COST, COLUMN_COOLDOWN,
                COLUMN_BRANCH, COLUMN_REQUIRED_CLASS, COLUMN_VEHICLE_TYPE, COLUMN_REQUIRES_SQUADLEADER,
                COLUMN_ABANDON_BLACKLISTED, COLUMN_ABANDON_VALUE_LOSS_SPEED),
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
            builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_DELAYS, COLUMN_EXT_PK, Delay.COLUMN_TYPE, Delay.COLUMN_VALUE, Delay.COLUMN_GAMEMODE));
            objs = new object[item.Delays.Length * 4];
            for (int i = 0; i < item.Delays.Length; ++i)
            {
                Delay delay = item.Delays[i];
                int index = i * 4;
                objs[index] = pk2;
                objs[index + 1] = delay.Type.ToString();
                objs[index + 2] = Delay.UsesValue(delay.Type) ? delay.Value : DBNull.Value;
                objs[index + 3] = (object?)delay.Gamemode ?? DBNull.Value;
                F.AppendPropertyList(builder, index, 4);
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        if (item.Items is { Length: > 0 })
        {
            builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_ITEMS, COLUMN_EXT_PK, COLUMN_ITEM_GUID));
            objs = new object[item.Items.Length * 2];
            for (int i = 0; i < item.Items.Length; ++i)
            {
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = item.Items[i].ToString("N");
                F.AppendPropertyList(builder, index, 2);
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        if (item.CrewSeats is { Length: > 0 })
        {
            builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_CREW_SEATS, COLUMN_EXT_PK, COLUMN_CREW_SEATS_SEAT));
            objs = new object[item.CrewSeats.Length * 2];
            for (int i = 0; i < item.CrewSeats.Length; ++i)
            {
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = item.CrewSeats[i];
                F.AppendPropertyList(builder, index, 2);
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        if (item.UnlockRequirements is { Length: > 0 })
        {
            builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_UNLOCK_REQUIREMENTS, COLUMN_EXT_PK, UnlockRequirement.COLUMN_JSON));
            objs = new object[item.UnlockRequirements.Length * 2];
            using MemoryStream str = new MemoryStream(48);
            for (int i = 0; i < item.UnlockRequirements.Length; ++i)
            {
                UnlockRequirement req = item.UnlockRequirements[i];
                if (i != 0)
                    str.Seek(0L, SeekOrigin.Begin);
                Utf8JsonWriter writer = new Utf8JsonWriter(str, JsonEx.condensedWriterOptions);
                UnlockRequirement.Write(writer, req);
                writer.Dispose();
                string json = System.Text.Encoding.UTF8.GetString(str.GetBuffer(), 0, checked((int)str.Position));
                int index = i * 2;
                objs[index] = pk2;
                objs[index + 1] = json;
                F.AppendPropertyList(builder, index, 2);
            }
            builder.Append(';');
            await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            builder.Clear();
        }

        if (item.Metadata != null)
        {
            if (item.Metadata.Barricades != null && item.Metadata.Barricades.Count > 0)
            {
                builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_BARRICADES, COLUMN_EXT_PK, VBarricade.COLUMN_GUID,
                    VBarricade.COLUMN_POS_X, VBarricade.COLUMN_POS_Y, VBarricade.COLUMN_POS_Z,
                    VBarricade.COLUMN_ROT_X, VBarricade.COLUMN_ROT_Y, VBarricade.COLUMN_ROT_Z, VBarricade.COLUMN_METADATA));
                objs = new object[item.Metadata.Barricades.Count * 9 + 1];
                List<VBarricade>? storages = null;
                for (int i = 0; i < item.Metadata.Barricades.Count; ++i)
                {
                    VBarricade barricade = item.Metadata.Barricades[i];
                    barricade.LinkedKey = pk2;
                    int index = i * 9;
                    objs[index] = pk2;
                    objs[index + 1] = barricade.BarricadeID.ToString("N");
                    objs[index + 2] = barricade.PosX;
                    objs[index + 3] = barricade.PosY;
                    objs[index + 4] = barricade.PosZ;
                    objs[index + 5] = barricade.AngleX;
                    objs[index + 6] = barricade.AngleY;
                    objs[index + 7] = barricade.AngleZ;
                    byte[] state = barricade.Metadata;
                    if (Assets.find<ItemBarricadeAsset>(barricade.BarricadeID) is ItemStorageAsset)
                    {
                        (storages ??= new List<VBarricade>(4)).Add(barricade);
                        if (state == null)
                            state = new byte[16];
                        else if (state.Length > 15)
                        {
                            if (state.Length != 16)
                            {
                                state = new byte[16];
                                Buffer.BlockCopy(barricade.Metadata, 0, state, 0, 16);
                            }
                        }
                        else state = Array.Empty<byte>();
                    }
                    objs[index + 8] = state ?? Array.Empty<byte>();
                    F.AppendPropertyList(builder, index, 9);
                }

                objs[objs.Length - 1] = pk2;
                builder.Append(';');
                int ind2 = -1;
                builder.Append(F.BuildSelectWhere(TABLE_BARRICADES, COLUMN_EXT_PK, objs.Length - 1,
                    VBarricade.COLUMN_PK, VBarricade.COLUMN_GUID,
                    VBarricade.COLUMN_POS_X, VBarricade.COLUMN_POS_Y, VBarricade.COLUMN_POS_Z));
                await Sql.QueryAsync(builder.ToString(), objs, reader =>
                {
                    ++ind2;
                    int pk3 = reader.GetInt32(0);
                    Guid? guid = reader.ReadGuidString(1);
                    if (!guid.HasValue)
                        return;
                    float posx = reader.GetFloat(2);
                    float posy = reader.GetFloat(3);
                    float posz = reader.GetFloat(4);
                    for (int i = ind2; i < item.Metadata.Barricades.Count; ++i)
                    {
                        VBarricade b = item.Metadata.Barricades[i];
                        if (b.BarricadeID == guid && Mathf.Abs(b.PosX - posx) < 0.05f
                                                  && Mathf.Abs(b.PosY - posy) < 0.05f
                                                  && Mathf.Abs(b.PosZ - posz) < 0.05f)
                        {
                            b.PrimaryKey = pk3;
                            return;
                        }
                    }
                    for (int i = 0; i < ind2; ++i)
                    {
                        VBarricade b = item.Metadata.Barricades[i];
                        if (b.BarricadeID == guid && Mathf.Abs(b.PosX - posx) < 0.05f
                                                  && Mathf.Abs(b.PosY - posy) < 0.05f
                                                  && Mathf.Abs(b.PosZ - posz) < 0.05f)
                        {
                            b.PrimaryKey = pk3;
                            return;
                        }
                    }
                }, token).ConfigureAwait(false);
                builder.Clear();
                await UCWarfare.ToUpdate(token);
                if (storages != null)
                {
                    List<ItemJarData> jars = new List<ItemJarData>(storages.Count * 8);
                    List<ItemDisplayData>? disp = null;
                    for (int i = 0; i < storages.Count; ++i)
                    {
                        ItemStorageAsset storage = Assets.find<ItemStorageAsset>(storages[i].BarricadeID);
                        VBarricade barricade = storages[i];
                        jars.AddRange(F.GetItemsFromStorageState(storage, barricade.Metadata, out ItemDisplayData? display, barricade.PrimaryKey));
                        if (display.HasValue)
                            (disp ??= new List<ItemDisplayData>(4)).Add(display.Value);
                    }
                    if (jars.Count > 0)
                    {
                        builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_BARRICADE_ITEMS,
                            VBarricade.COLUMN_ITEM_BARRICADE_PK,
                            VBarricade.COLUMN_ITEM_GUID, VBarricade.COLUMN_ITEM_AMOUNT, VBarricade.COLUMN_ITEM_QUALITY,
                            VBarricade.COLUMN_ITEM_POS_X, VBarricade.COLUMN_ITEM_POS_Y, VBarricade.COLUMN_ITEM_ROT,
                            VBarricade.COLUMN_ITEM_METADATA
                        ));
                        objs = new object[jars.Count * 8];
                        for (int i = 0; i < jars.Count; ++i)
                        {
                            ItemJarData data = jars[i];
                            int index = i * 8;
                            objs[index] = data.Structure.Key;
                            objs[index + 1] = data.Item.ToString("N");
                            objs[index + 2] = data.Amount;
                            objs[index + 3] = data.Quality;
                            objs[index + 4] = data.X;
                            objs[index + 5] = data.Y;
                            objs[index + 6] = data.Rotation;
                            objs[index + 7] = data.Metadata ?? Array.Empty<byte>();
                            F.AppendPropertyList(builder, index, 8);
                        }

                        builder.Append(';');
                        await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                        builder.Clear();
                    }

                    if (disp is { Count: > 0 })
                    {
                        string[] columns =
                        {
                            VBarricade.COLUMN_ITEM_BARRICADE_PK,
                            VBarricade.COLUMN_DISPLAY_SKIN, VBarricade.COLUMN_DISPLAY_MYTHIC, VBarricade.COLUMN_DISPLAY_ROT,
                            VBarricade.COLUMN_DISPLAY_TAGS, VBarricade.COLUMN_DISPLAY_DYNAMIC_PROPS
                        };
                        builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_BARRICADE_DISPLAY_DATA, columns));
                        objs = new object[disp.Count * 6];
                        for (int i = 0; i < disp.Count; ++i)
                        {
                            ItemDisplayData data = disp[i];
                            int index = i * 6;
                            objs[index] = data.Key.Key;
                            objs[index + 1] = data.Skin;
                            objs[index + 2] = data.Mythic;
                            objs[index + 3] = data.Rotation;
                            objs[index + 4] = (object?)data.Tags ?? DBNull.Value;
                            objs[index + 5] = (object?)data.DynamicProps ?? DBNull.Value;
                            F.AppendPropertyList(builder, index, 6);
                        }
                        builder.Append(F.EndBuildOtherInsertQueryUpdate(columns));
                        await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                    }
                }
            }
            if (item.Metadata.TrunkItems != null && item.Metadata.TrunkItems.Count > 0)
            {
                builder.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_TRUNK_ITEMS, COLUMN_EXT_PK,
                    PageItem.COLUMN_GUID, PageItem.COLUMN_X, PageItem.COLUMN_Y, PageItem.COLUMN_ROTATION, PageItem.COLUMN_AMOUNT, PageItem.COLUMN_METADATA));
                objs = new object[item.Metadata.TrunkItems.Count * 7];
                for (int i = 0; i < item.Metadata.TrunkItems.Count; ++i)
                {
                    PageItem item2 = item.Metadata.TrunkItems[i];
                    int index = i * 7;
                    objs[index] = pk2;
                    objs[index + 1] = item2.Item.ToString("N");
                    objs[index + 2] = item2.X;
                    objs[index + 3] = item2.Y;
                    objs[index + 4] = item2.Rotation;
                    objs[index + 5] = item2.Amount;
                    objs[index + 6] = item2.State;
                    F.AppendPropertyList(builder, index, 7);
                }

                builder.Append(';');
                await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
            }
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
                    Map = reader.IsDBNull(1) ? -1 : reader.GetInt32(1),
                    Faction = reader.IsDBNull(2) ? -1 : reader.GetInt32(2),
                    RespawnTime = reader.GetFloat(4),
                    TicketCost = reader.GetInt32(5),
                    CreditCost = reader.GetInt32(6),
                    RearmCost = reader.GetInt32(7),
                    Cooldown = reader.GetFloat(8),
                    Branch = reader.ReadStringEnum(9, Branch.Default),
                    RequiredClass = reader.ReadStringEnum(10, Class.None),
                    Type = reader.ReadStringEnum(11, VehicleType.None),
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
                                         DelayType type = reader.ReadStringEnum(1, DelayType.None);
                                         if (type == DelayType.None)
                                             break;
                                         Util.AddToArray(ref list[i].Delays!, new Delay(type, reader.IsDBNull(2) ? 0f : reader.GetFloat(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{UnlockRequirement.COLUMN_JSON}` " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         UnlockRequirement? req = UnlockRequirement.Read(reader);
                                         if (req != null)
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
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{PageItem.COLUMN_GUID}`,`{PageItem.COLUMN_X}`," +
                             $"`{PageItem.COLUMN_Y}`,`{PageItem.COLUMN_ROTATION}`,`{PageItem.COLUMN_AMOUNT}`,`{PageItem.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_TRUNK_ITEMS}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 Guid? guid = reader.ReadGuidString(1);
                                 if (!guid.HasValue)
                                 {
                                     L.LogWarning("Invalid GUID in vbay trunk item " + pk + ": " + reader.GetString(1) + ".");
                                     return;
                                 }
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         VehicleData data = list[i];
                                         data.Metadata ??= new MetaSave();
                                         data.Metadata.TrunkItems ??= new List<PageItem>(8);
                                         PageItem item = new PageItem(guid.Value, reader.GetByte(2), reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(6), reader.GetByte(5), (Page)PlayerInventory.STORAGE);
                                         data.Metadata.TrunkItems.Add(item);
                                         break;
                                     }
                                 }
                             }, token).ConfigureAwait(false);
        List<object> objs2 = new List<object>(list.Count * 2);
        sb.Clear();
        sb.Append("IN (");
        bool f = false;
        await Sql.QueryAsync($"SELECT `{COLUMN_EXT_PK}`,`{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_GUID}`," +
                             $"`{VBarricade.COLUMN_POS_X}`,`{VBarricade.COLUMN_POS_Y}`,`{VBarricade.COLUMN_POS_Z}`," +
                             $"`{VBarricade.COLUMN_ROT_X}`,`{VBarricade.COLUMN_ROT_Y}`,`{VBarricade.COLUMN_ROT_Z}`," +
                             $"`{VBarricade.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_BARRICADES}` WHERE `{COLUMN_EXT_PK}` " + pkeys, pkeyObjs, reader =>
                             {
                                 int pk = reader.GetInt32(0);
                                 int bpk = reader.GetInt32(1);
                                 Guid? guid = reader.ReadGuidString(2);
                                 if (!guid.HasValue)
                                 {
                                     L.LogWarning("Invalid GUID in vbarricade " + pk + "." + bpk + ": " + reader.GetString(2) + ".");
                                     return;
                                 }
                                 for (int i = 0; i < list.Count; ++i)
                                 {
                                     if (list[i].PrimaryKey.Key == pk)
                                     {
                                         VehicleData data = list[i];
                                         data.Metadata ??= new MetaSave();
                                         data.Metadata.Barricades ??= new List<VBarricade>(4);
                                         VBarricade barricade = new VBarricade(guid.Value, ushort.MaxValue,
                                             reader.GetFloat(3), reader.GetFloat(4), reader.GetFloat(5),
                                             reader.GetFloat(6), reader.GetFloat(7), reader.GetFloat(8),
                                             reader.ReadByteArray(9))
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
        if (pkeyObjs.Length > 0)
        {
            List<ItemJarData> items = new List<ItemJarData>(32);
            List<ItemDisplayData> display = new List<ItemDisplayData>(16);
            await Sql.QueryAsync(
                $"SELECT `{VBarricade.COLUMN_ITEM_PK}`,`{VBarricade.COLUMN_ITEM_BARRICADE_PK}`,`{VBarricade.COLUMN_ITEM_GUID}`," +
                $"`{VBarricade.COLUMN_ITEM_POS_X}`,`{VBarricade.COLUMN_ITEM_POS_Y}`,`{VBarricade.COLUMN_ITEM_ROT}`,`{VBarricade.COLUMN_ITEM_AMOUNT}`," +
                $"`{VBarricade.COLUMN_ITEM_QUALITY}`,`{VBarricade.COLUMN_ITEM_METADATA}` " +
                $"FROM `{TABLE_BARRICADE_ITEMS}` WHERE `{VBarricade.COLUMN_ITEM_BARRICADE_PK}` " +
                pkeys, pkeyObjs,
                reader =>
                {
                    int pk = reader.GetInt32(0);
                    int bpk = reader.GetInt32(1);
                    Guid? guid = reader.ReadGuidString(2);
                    if (!guid.HasValue)
                    {
                        L.LogWarning("Invalid GUID in vbarricade item " + pk + "." + bpk + ": " + reader.GetString(2) + ".");
                        return;
                    }
                    items.Add(new ItemJarData(pk, bpk, guid.Value,
                        reader.GetByte(3),
                        reader.GetByte(4), reader.GetByte(5), reader.GetByte(6), reader.GetByte(7),
                        reader.ReadByteArray(8)));
                }, token);
            await Sql.QueryAsync(
                $"SELECT `{VBarricade.COLUMN_PK}`,`{VBarricade.COLUMN_DISPLAY_SKIN}`,`{VBarricade.COLUMN_DISPLAY_MYTHIC}`," +
                $"`{VBarricade.COLUMN_DISPLAY_ROT}`,`{VBarricade.COLUMN_DISPLAY_TAGS}`,`{VBarricade.COLUMN_DISPLAY_DYNAMIC_PROPS}` " +
                $"FROM `{TABLE_BARRICADE_DISPLAY_DATA}` WHERE `{VBarricade.COLUMN_PK}` " +
                pkeys, pkeyObjs,
                reader =>
                {
                    display.Add(new ItemDisplayData(reader.GetInt32(0), reader.GetUInt16(1), reader.GetUInt16(2),
                        reader.GetByte(3),
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
                    int index = 15;
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
                    Branch = reader.ReadStringEnum(9, Branch.Default),
                    RequiredClass = reader.ReadStringEnum(10, Class.None),
                    Type = reader.ReadStringEnum(11, VehicleType.None),
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
                                 DelayType type = reader.ReadStringEnum(0, DelayType.None);
                                 if (type == DelayType.None)
                                     return;
                                 Util.AddToArray(ref obj.Delays!, new Delay(type, reader.IsDBNull(1) ? 0f : reader.GetFloat(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
                             }, token).ConfigureAwait(false);
        await Sql.QueryAsync($"SELECT `{UnlockRequirement.COLUMN_JSON}` " +
                             $"FROM `{TABLE_UNLOCK_REQUIREMENTS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reader.GetString(0));
                                 Utf8JsonReader reader2 = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                                 UnlockRequirement? req = UnlockRequirement.Read(ref reader2);
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
        await Sql.QueryAsync($"SELECT `{PageItem.COLUMN_GUID}`,`{PageItem.COLUMN_X}`," +
                             $"`{PageItem.COLUMN_Y}`,`{PageItem.COLUMN_ROTATION}`,`{PageItem.COLUMN_AMOUNT}`,`{PageItem.COLUMN_METADATA}` " +
                             $"FROM `{TABLE_TRUNK_ITEMS}` WHERE `{COLUMN_EXT_PK}`=@0;", pkeyObj, reader =>
                             {
                                 obj.Metadata ??= new MetaSave();
                                 obj.Metadata.TrunkItems ??= new List<PageItem>(8);
                                 Guid? guid = reader.ReadGuidString(1);
                                 if (!guid.HasValue)
                                 {
                                     L.LogWarning("Invalid GUID in vbay trunk item " + pk + ": " + reader.GetString(1) + ".");
                                     return;
                                 }
                                 PageItem item = new PageItem(guid.Value, reader.GetByte(2), reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(6), reader.GetByte(5), (Page)PlayerInventory.STORAGE);
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
                                 Guid? guid = reader.ReadGuidString(2);
                                 if (!guid.HasValue)
                                 {
                                     L.LogWarning("Invalid GUID in vbarricade item " + pk.Key + "." + bpk + ": " + reader.GetString(1) + ".");
                                     return;
                                 }
                                 obj.Metadata ??= new MetaSave();
                                 obj.Metadata.Barricades ??= new List<VBarricade>(4);
                                 VBarricade barricade = new VBarricade(guid.Value, reader.GetUInt16(2),
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
                int ipk = reader.GetInt32(0);
                int bpk = reader.GetInt32(1);
                Guid? guid = reader.ReadGuidString(2);
                if (!guid.HasValue)
                {
                    L.LogWarning("Invalid GUID in vbarricade item " + pk.Key + "." + bpk + "." + ipk + ": " + reader.GetString(1) + ".");
                    return;
                }
                (items ??= new List<ItemJarData>(16)).Add(new ItemJarData(ipk, bpk, guid.Value, reader.GetByte(3),
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