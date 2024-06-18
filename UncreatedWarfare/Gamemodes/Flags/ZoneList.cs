//#define MIGRATE
using MySql.Data.MySqlClient;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

[SingletonDependency(typeof(Level))]
public sealed class ZoneList : ListSqlSingleton<Zone>, IUIListener
{
    public const int MaxNameLength = 48;
    public const int MaxShortNameLength = 24;
    public override MySqlDatabase Sql => Data.AdminSql;
    public override bool AwaitLoad => true;
    public ZoneList() : base("zones", Schemas)
    {
        OnItemDownloaded += OnItemDownloadedIntl;
        OnItemsRefreshed += OnItemsDownloadedIntl;
    }
#if MIGRATE
    public override async Task PostLoad(CancellationToken token)
    {
        await ImportFromJson(Path.Combine(Data.Paths.FlagStorage, "zones.json"), false, (a, b) => a.Id.CompareTo(b.Id), token);
    }
#endif
    public SqlItem<Zone>? FindInsizeZone(Vector3 pos, bool noOverlap)
    {
        WriteWait();
        try
        {
            SqlItem<Zone>? current = null;
            for (int i = 0; i < Items.Count; ++i)
            {
                if (Items[i] is { Item: { } zone } proxy && zone.IsInside(pos))
                {
                    if (current is null)
                        current = proxy;
                    else return noOverlap ? null : current;
                }
            }

            return current;
        }
        finally
        {
            WriteRelease();
        }
    }
    public SqlItem<Zone>? FindInsizeZone(Vector2 pos, bool noOverlap)
    {
        WriteWait();
        try
        {
            SqlItem<Zone>? current = null;
            for (int i = 0; i < Items.Count; ++i)
            {
                if (Items[i] is { Item: { } zone } proxy && zone.IsInside(pos))
                {
                    if (current is null)
                    {
                        if (!noOverlap) return proxy;
                        current = proxy;
                    }
                    else return noOverlap ? null : current;
                }
            }

            return current;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool IsNameTaken(string name, out string takenName)
    {
        WriteWait();
        try
        {
            int index = F.StringIndexOf(List, x => x.Item?.Name, name, true);
            if (index < 0)
            {
                takenName = name;
                return false;
            }

            takenName = Items[index].Item?.Name ?? name;
            return true;
        }
        finally
        {
            WriteRelease();
        }
    }
    public SqlItem<Zone>? SearchZone(string term)
    {
        WriteWait();
        try
        {
            int index = F.StringIndexOf(List, x => x.Item?.Name, term, false);
            if (index < 0)
            {
                index = F.StringIndexOf(List, x => x.Item?.ShortName, term, false);
                if (index < 0)
                    goto checkPredefs;
            }

            return Items[index];
        }
        finally
        {
            WriteRelease();
        }

        checkPredefs:
        if (term.Equals("lobby", StringComparison.OrdinalIgnoreCase) || term.Equals("spawn", StringComparison.OrdinalIgnoreCase))
            return FindProxyNoLock(TeamManager.LobbyZone.PrimaryKey);
        if (term.Equals("t1main", StringComparison.OrdinalIgnoreCase) || term.Equals("t1", StringComparison.OrdinalIgnoreCase))
            return FindProxyNoLock(TeamManager.Team1Main);
        if (term.Equals("t2main", StringComparison.OrdinalIgnoreCase) || term.Equals("t2", StringComparison.OrdinalIgnoreCase))
            return FindProxyNoLock(TeamManager.Team2Main);
        if (term.Equals("t1amc", StringComparison.OrdinalIgnoreCase))
            return FindProxyNoLock(TeamManager.Team1AMC);
        if (term.Equals("t2amc", StringComparison.OrdinalIgnoreCase))
            return FindProxyNoLock(TeamManager.Team2AMC);
        Flag? fl = null;
        if (term.Equals("obj1", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IFlagTeamObjectiveGamemode gm))
            {
                fl = gm.ObjectiveTeam1;
            }
        }
        else if (term.Equals("obj2", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IFlagTeamObjectiveGamemode gm))
            {
                fl = gm.ObjectiveTeam2;
            }
        }
        else if (term.Equals("obj", StringComparison.OrdinalIgnoreCase))
        {
            if (Data.Is(out IFlagTeamObjectiveGamemode rot))
            {
                if (Data.Is(out IAttackDefense atdef))
                {
                    ulong t = atdef.DefendingTeam;
                    fl = t == 1 ? rot.ObjectiveTeam1 : rot.ObjectiveTeam2;
                }
            }
            else if (Data.Is(out IFlagObjectiveGamemode obj))
            {
                fl = obj.Objective;
            }
        }
        if (fl != null)
            return fl.ZoneData;
        return null;
    }
    public void TickZoneFlags(UCPlayer player, bool forceFlags = false)
    {
        player.SafezoneZone = null;
        player.NoDropZone = null;
        player.NoPickZone = null;
        WriteWait();
        try
        {
            bool flagGm = !forceFlags && Data.Is(out FlagGamemode gm) && gm.ConsumeFlagUseCaseZones;
            Vector3 pos = player.Position;
            for (int i = 0; i < List.Count; ++i)
            {
                Zone? zone = List[i]?.Item;

                if (zone is null || zone.Data.Flags == ZoneFlags.None || flagGm && zone.Data.UseCase == ZoneUseCase.Flag || !zone.IsInside(pos))
                    continue;
                
                ApplyZoneFlags(player, zone);
            }
        }
        finally
        {
            WriteRelease();
        }
    }
    public static void ApplyZoneFlags(UCPlayer player, Zone zone)
    {
        if ((zone.Data.Flags & ZoneFlags.Safezone) != 0)
        {
            if (player.SafezoneZone is null || player.SafezoneZone.BoundsArea > zone.BoundsArea)
                player.SafezoneZone = zone;
        }

        if ((zone.Data.Flags & ZoneFlags.NoDropItems) != 0)
        {
            if (player.NoDropZone is null || player.NoDropZone.BoundsArea > zone.BoundsArea)
                player.NoDropZone = zone;
        }

        if ((zone.Data.Flags & ZoneFlags.NoPickItems) != 0)
        {
            if (player.NoPickZone is null || player.NoPickZone.BoundsArea > zone.BoundsArea)
                player.NoPickZone = zone;
        }
    }
    private static void OnItemDownloadedIntl(SqlItem<Zone> obj)
    {
        if (obj.Item is { Data.UseCase: ZoneUseCase.Team1Main or ZoneUseCase.Team2Main or ZoneUseCase.Team1MainCampZone or ZoneUseCase.Team2MainCampZone })
        {
            TeamManager.ResetLocations();
        }
    }
    private static void OnItemsDownloadedIntl()
    {
        TeamManager.ResetLocations();
    }
    #region Sql
    // ReSharper disable InconsistentNaming
    public const string TABLE_MAIN = "zone_data";
    public const string TABLE_CIRCLE_DATA = "zone_circle_data";
    public const string TABLE_RECTANGLE_DATA = "zone_rectangle_data";
    public const string TABLE_POLYGON_DATA = "zone_polygon_data";
    public const string TABLE_ADJACENCIES = "zone_adjacencies";
    public const string TABLE_GRID_OBJECTS = "zone_grid_objects";

    public const string COLUMN_PK = "pk";
    public const string COLUMN_MAP = "Map";
    public const string COLUMN_ZONE_TYPE = "Type";
    public const string COLUMN_NAME = "FullName";
    public const string COLUMN_SHORT_NAME = "ShortName";
    public const string COLUMN_SPAWN_X = "X";
    public const string COLUMN_SPAWN_Z = "Z";
    public const string COLUMN_USE_CASE = "UseCase";
    public const string COLUMN_USE_MAP_COORDS = "UseRelativeMapCoords";
    public const string COLUMN_MIN_HEIGHT = "MinHeight";
    public const string COLUMN_MAX_HEIGHT = "MaxHeight";
    public const string COLUMN_FLAGS = "Flags";

    public const string COLUMN_PK_EXT = "Zone";

    public const string COLUMN_CENTER_POS_X = "CenterX";
    public const string COLUMN_CENTER_POS_Z = "CenterZ";
    public const string COLUMN_POINT_INDEX = "Index";
    public const string COLUMN_POINT_POS_X = "PointX";
    public const string COLUMN_POINT_POS_Z = "PointZ";
    public const string COLUMN_SIZE_X = "Length";
    public const string COLUMN_SIZE_Z = "Width";
    public const string COLUMN_RADIUS = "Radius";

    public const string COLUMN_ADJACENT_ZONE = "AdjacentZone";
    public const string COLUMN_WEIGHT = "Weight";

    public const string COLUMN_GRID_OBJ_INSTANCE_ID = "InstanceId";
    public const string COLUMN_GRID_OBJ_GUID = "Guid";
    public const string COLUMN_GRID_OBJ_POS_X = "X";
    public const string COLUMN_GRID_OBJ_POS_Y = "Y";
    public const string COLUMN_GRID_OBJ_POS_Z = "Z";

    private static readonly Schema[] Schemas =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(COLUMN_MAP, SqlTypes.INCREMENT_KEY),
            new Schema.Column(COLUMN_ZONE_TYPE, SqlTypes.Enum(ZoneType.Invalid)),
            new Schema.Column(COLUMN_NAME, SqlTypes.String(MaxNameLength)),
            new Schema.Column(COLUMN_SHORT_NAME, SqlTypes.String(MaxShortNameLength)) { Nullable = true },
            new Schema.Column(COLUMN_SPAWN_X, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_SPAWN_Z, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_USE_CASE, SqlTypes.Enum<ZoneUseCase>()),
            new Schema.Column(COLUMN_USE_MAP_COORDS, SqlTypes.BOOLEAN),
            new Schema.Column(COLUMN_MIN_HEIGHT, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_MAX_HEIGHT, SqlTypes.FLOAT) { Nullable = true },
            new Schema.Column(COLUMN_FLAGS, SqlTypes.ULONG) { Default = "0" }

        }, true, typeof(ZoneModel)),
        new Schema(TABLE_CIRCLE_DATA, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK_EXT, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_CENTER_POS_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_CENTER_POS_Z, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_RADIUS, SqlTypes.FLOAT)
        }, false, typeof(CircleZone)),
        new Schema(TABLE_RECTANGLE_DATA, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK_EXT, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_CENTER_POS_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_CENTER_POS_Z, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_SIZE_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_SIZE_Z, SqlTypes.FLOAT)
        }, false, typeof(RectZone)),
        new Schema(TABLE_POLYGON_DATA, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK_EXT, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_POINT_INDEX, SqlTypes.INT) { Default = "'-1'" },
            new Schema.Column(COLUMN_POINT_POS_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_POINT_POS_Z, SqlTypes.FLOAT)
        }, false, typeof(PolygonZone)),
        new Schema(TABLE_ADJACENCIES, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK_EXT, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_ADJACENT_ZONE, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_WEIGHT, SqlTypes.FLOAT)
        }, false, typeof(AdjacentFlagData)),
        new Schema(TABLE_GRID_OBJECTS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK_EXT, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_GRID_OBJ_INSTANCE_ID, SqlTypes.INSTANCE_ID),
            new Schema.Column(COLUMN_GRID_OBJ_GUID, SqlTypes.GUID_STRING),
            new Schema.Column(COLUMN_GRID_OBJ_POS_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_GRID_OBJ_POS_Y, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_GRID_OBJ_POS_Z, SqlTypes.FLOAT)
        }, false, typeof(GridObject))
    };
    // ReSharper restore InconsistentNaming

    /// <inheritdoc/>
    [Obsolete]
    protected override async Task AddOrUpdateItem(Zone? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item == null)
        {
            await F.DeleteItem(Sql, pk, TABLE_MAIN, COLUMN_PK, token).ConfigureAwait(false);
            return;
        }

        bool hasPk = pk.IsValid;
        uint pk2 = PrimaryKey.NotAssigned;
        object[] objs = new object[hasPk ? 12 : 11];
        ZoneModel mdl = item.Data;
        objs[0] = mdl.Map;
        objs[1] = mdl.ZoneType.ToString();
        objs[2] = mdl.Name;
        objs[3] = string.IsNullOrEmpty(mdl.ShortName) ? DBNull.Value : mdl.ShortName!;
        objs[4] = float.IsNaN(mdl.SpawnX) || mdl.ZoneType != ZoneType.Polygon && mdl.SpawnX.AlmostEquals(mdl.ZoneData.X) ? DBNull.Value : mdl.SpawnX;
        objs[5] = float.IsNaN(mdl.SpawnZ) || mdl.ZoneType != ZoneType.Polygon && mdl.SpawnZ.AlmostEquals(mdl.ZoneData.Z) ? DBNull.Value : mdl.SpawnZ;
        objs[6] = mdl.UseCase.ToString();
        objs[7] = mdl.UseMapCoordinates;
        objs[8] = !float.IsNaN(mdl.MinimumHeight) ? mdl.MinimumHeight : DBNull.Value;
        objs[9] = !float.IsNaN(mdl.MaximumHeight) ? mdl.MaximumHeight : DBNull.Value;
        objs[10] = (ulong)mdl.Flags;
        if (hasPk)
            objs[11] = pk.Key;
        await Sql.QueryAsync(F.BuildInitialInsertQuery(TABLE_MAIN, COLUMN_PK, hasPk, COLUMN_PK_EXT,
            new string[] { TABLE_POLYGON_DATA, TABLE_ADJACENCIES, TABLE_GRID_OBJECTS },
            COLUMN_MAP, COLUMN_ZONE_TYPE, COLUMN_NAME, COLUMN_SHORT_NAME, COLUMN_SPAWN_X,
            COLUMN_SPAWN_Z, COLUMN_USE_CASE, COLUMN_USE_MAP_COORDS, COLUMN_MIN_HEIGHT,
            COLUMN_MAX_HEIGHT, COLUMN_FLAGS),
            objs, reader =>
            {
                pk2 = reader.GetUInt32(0);
            }, token).ConfigureAwait(false);
        pk = pk2;
        if (!pk.IsValid)
            throw new Exception("Unable to get a valid primary key for " + item + ".");
        item.PrimaryKey = pk;
        StringBuilder sb = new StringBuilder(256);
        objs = new object[1 + mdl.ZoneType switch
        {
            ZoneType.Polygon => mdl.ZoneData.Points.Length * 3,
            ZoneType.Circle => 3,
            ZoneType.Rectangle => 4,
            _ => 0
        } + mdl.Adjacencies.Length * 2 + mdl.GridObjects.Length * 5];
        int index = 0;
        objs[0] = pk2;
        switch (mdl.ZoneType)
        {
            case ZoneType.Polygon:
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_POLYGON_DATA, COLUMN_PK_EXT, COLUMN_POINT_INDEX, COLUMN_POINT_POS_X, COLUMN_POINT_POS_Z));
                for (int i = 0; i < mdl.ZoneData.Points.Length; ++i)
                {
                    if (i > 0)
                        sb.Append(',');
                    Vector2 v2 = mdl.ZoneData.Points[i];
                    objs[++index] = i;
                    sb.Append("(@0,@").Append(index.ToString(Data.AdminLocale));
                    objs[++index] = v2.x;
                    sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                    objs[++index] = v2.y;
                    sb.Append(",@").Append(index.ToString(Data.AdminLocale)).Append(')');
                }

                sb.Append(';');
                break;

            case ZoneType.Circle:
                string[] col = { COLUMN_PK_EXT, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_RADIUS };
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_CIRCLE_DATA, col));
                objs[++index] = mdl.ZoneData.X;
                sb.Append("(@0,@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = mdl.ZoneData.Z;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = mdl.ZoneData.Radius;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale)).Append(')');
                sb.Append(" ON DUPLICATE KEY UPDATE ").Append(SqlTypes.ColumnUpdateList(index - 2, 1, col));
                sb.Append(';');
                break;

            case ZoneType.Rectangle:
                col = new string[] { COLUMN_PK_EXT, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_SIZE_X, COLUMN_SIZE_Z };
                sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_RECTANGLE_DATA, COLUMN_PK_EXT, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_SIZE_X, COLUMN_SIZE_Z));
                objs[++index] = mdl.ZoneData.X;
                sb.Append("(@0,@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = mdl.ZoneData.Z;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = mdl.ZoneData.SizeX;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = mdl.ZoneData.SizeZ;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale)).Append(')');
                sb.Append(" ON DUPLICATE KEY UPDATE ").Append(SqlTypes.ColumnUpdateList(index - 3, 1, col));
                sb.Append(';');
                break;
        }

        if (mdl.Adjacencies is { Length: > 0 })
        {
            sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_ADJACENCIES, COLUMN_PK_EXT, COLUMN_ADJACENT_ZONE, COLUMN_WEIGHT));
            for (int i = 0; i < mdl.Adjacencies.Length; ++i)
            {
                if (i > 0)
                    sb.Append(',');
                AdjacentFlagData a = mdl.Adjacencies[i];
                objs[++index] = a.PrimaryKey.Key;
                sb.Append("(@0,@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = a.Weight;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale)).Append(')');
            }

            sb.Append(';');
        }
        if (mdl.GridObjects is { Length: > 0 })
        {
            sb.Append(F.StartBuildOtherInsertQueryNoUpdate(TABLE_GRID_OBJECTS, COLUMN_PK_EXT, COLUMN_GRID_OBJ_INSTANCE_ID,
                    COLUMN_GRID_OBJ_GUID, COLUMN_GRID_OBJ_POS_X, COLUMN_GRID_OBJ_POS_Y, COLUMN_GRID_OBJ_POS_Z));
            for (int i = 0; i < mdl.GridObjects.Length; ++i)
            {
                if (i > 0)
                    sb.Append(',');
                GridObject a = mdl.GridObjects[i];
                objs[++index] = a.ObjectInstanceId;
                sb.Append("(@0,@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = a.Guid.ToString("N");
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = a.X;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = a.Y;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale));
                objs[++index] = a.Z;
                sb.Append(",@").Append(index.ToString(Data.AdminLocale)).Append(')');
            }

            sb.Append(';');
        }

        await Sql.NonQueryAsync(sb.ToString(), objs, token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    [Obsolete]
    protected override async Task<Zone?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        object[] objPks = { pk.Key };
        ZoneModel? mdlN = null;
        await Sql.QueryAsync(F.BuildSelectWhereLimit1(TABLE_MAIN, COLUMN_PK, 0, COLUMN_MAP, COLUMN_ZONE_TYPE, COLUMN_NAME,
            COLUMN_SHORT_NAME, COLUMN_SPAWN_X, COLUMN_SPAWN_Z, COLUMN_USE_CASE, COLUMN_USE_MAP_COORDS,
            COLUMN_MIN_HEIGHT, COLUMN_MAX_HEIGHT, COLUMN_FLAGS),
            objPks, reader =>
            {
                mdlN = ReadZoneModel(reader, 0);
            }, token).ConfigureAwait(false);
        if (!mdlN.HasValue)
            return null;
        ZoneModel model = mdlN.Value;
        model.Id = pk.Key;
        switch (model.ZoneType)
        {
            case ZoneType.Polygon:
                List<KeyValuePair<int, Vector2>> v2 = new List<KeyValuePair<int, Vector2>>(12);
                await Sql.QueryAsync(
                    F.BuildSelectWhere(TABLE_POLYGON_DATA, COLUMN_PK_EXT, 0, COLUMN_POINT_INDEX, COLUMN_POINT_POS_X, COLUMN_POINT_POS_Z),
                    objPks,
                    reader =>
                    {
                        v2.Add(new KeyValuePair<int, Vector2>(reader.GetInt32(0), new Vector2(reader.GetFloat(1), reader.GetFloat(2))));
                    }, token).ConfigureAwait(false);
                model.ZoneData.Points = v2.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
                break;
            case ZoneType.Circle:
                float x = model.SpawnX, z = model.SpawnZ, sx = float.NaN;
                await Sql.QueryAsync(
                    F.BuildSelectWhereLimit1(TABLE_POLYGON_DATA, COLUMN_PK_EXT, 0, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_RADIUS),
                    objPks,
                    reader =>
                    {
                        x = reader.GetFloat(0);
                        z = reader.GetFloat(1);
                        sx = reader.GetFloat(2);
                    }, token).ConfigureAwait(false);
                model.ZoneData.X = x;
                model.ZoneData.Z = z;
                model.ZoneData.Radius = sx;
                break;
            case ZoneType.Rectangle:
                x = model.SpawnX;
                z = model.SpawnZ;
                sx = float.NaN;
                float sz = float.NaN;
                await Sql.QueryAsync(
                    F.BuildSelectWhereLimit1(TABLE_POLYGON_DATA, COLUMN_PK_EXT, 0, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_SIZE_X, COLUMN_SIZE_Z),
                    objPks,
                    reader =>
                    {
                        x = reader.GetFloat(0);
                        z = reader.GetFloat(1);
                        sx = reader.GetFloat(2);
                        sz = reader.GetFloat(3);
                    }, token).ConfigureAwait(false);
                model.ZoneData.X = x;
                model.ZoneData.Z = z;
                model.ZoneData.SizeX = sx;
                model.ZoneData.SizeZ = sz;
                break;
        }

        List<AdjacentFlagData>? adj = null;
        await Sql.QueryAsync(
            F.BuildSelectWhere(TABLE_ADJACENCIES, COLUMN_PK_EXT, 0, COLUMN_ADJACENT_ZONE, COLUMN_WEIGHT),
            objPks,
            reader =>
            {
                (adj ??= new List<AdjacentFlagData>(5)).Add(new AdjacentFlagData(reader.GetUInt32(0), reader.IsDBNull(1) ? 1f : reader.GetFloat(1)));
            }, token).ConfigureAwait(false);

        if (adj is not null)
            model.Adjacencies = adj.ToArray();

        List<GridObject>? gobj = null;
        await Sql.QueryAsync(
            F.BuildSelectWhere(TABLE_GRID_OBJECTS, COLUMN_PK_EXT, 0, COLUMN_GRID_OBJ_INSTANCE_ID,
                COLUMN_GRID_OBJ_GUID, COLUMN_GRID_OBJ_POS_X, COLUMN_GRID_OBJ_POS_Y, COLUMN_GRID_OBJ_POS_Z),
            objPks,
            reader =>
            {
                (gobj ??= new List<GridObject>(16)).Add(new GridObject(pk, reader.IsDBNull(0) ? uint.MaxValue : reader.GetUInt32(0), reader.IsDBNull(1) ? Guid.Empty : reader.ReadGuidString(1) ?? Guid.Empty,
                    reader.IsDBNull(2) ? float.NaN : reader.GetFloat(2), reader.IsDBNull(3) ? float.NaN : reader.GetFloat(3), reader.IsDBNull(4) ? float.NaN : reader.GetFloat(4)));
            }, token).ConfigureAwait(false);

        if (gobj is not null)
            model.GridObjects = gobj.ToArray();
        model.ValidateRead();

        return model.GetZone();
    }

    /// <inheritdoc/>
    [Obsolete]
    protected override async Task<Zone[]> DownloadAllItems(CancellationToken token = default)
    {
        List<ZoneBuilder> list = new List<ZoneBuilder>(64);
        await Sql.QueryAsync(F.BuildSelectWhere(TABLE_MAIN, COLUMN_MAP, 0, COLUMN_PK, COLUMN_MAP, COLUMN_ZONE_TYPE, COLUMN_NAME,
                COLUMN_SHORT_NAME, COLUMN_SPAWN_X, COLUMN_SPAWN_Z, COLUMN_USE_CASE, COLUMN_USE_MAP_COORDS,
                COLUMN_MIN_HEIGHT, COLUMN_MAX_HEIGHT, COLUMN_FLAGS),
            new object[] { MapScheduler.Current }, reader =>
            {
                list.Add(new ZoneBuilder(ReadZoneModel(reader, 1)));
            }, token).ConfigureAwait(false);
        if (list.Count == 0)
            return Array.Empty<Zone>();
        object[] pkeys = new object[list.Count];
        List<KeyValuePair<int, Vector2>>?[] v2 = new List<KeyValuePair<int, Vector2>>?[list.Count];
        List<AdjacentFlagData>?[] adj = new List<AdjacentFlagData>?[list.Count];
        for (int i = 0; i < list.Count; ++i)
            pkeys[i] = list[i].Id;
        string pin = " IN (" + SqlTypes.ParameterList(0, pkeys.Length) + ");";
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_PK_EXT, COLUMN_POINT_INDEX, COLUMN_POINT_POS_X, COLUMN_POINT_POS_Z)} FROM `{TABLE_POLYGON_DATA}` WHERE `{COLUMN_PK_EXT}` " +
            pin, pkeys, reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (pk == list[i].Id)
                    {
                        ref List<KeyValuePair<int, Vector2>>? l = ref v2[i];
                        (l ??= new List<KeyValuePair<int, Vector2>>(12)).Add(new KeyValuePair<int, Vector2>(reader.GetInt32(1), new Vector2(reader.GetFloat(2), reader.GetFloat(3))));
                        return;
                    }
                }
            }, token).ConfigureAwait(false);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_PK_EXT, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_RADIUS)} FROM `{TABLE_CIRCLE_DATA}` WHERE `{COLUMN_PK_EXT}` " +
            pin, pkeys, reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (pk == list[i].Id)
                    {
                        list[i].WithRadius(reader.GetFloat(3)).WithPosition(reader.GetFloat(1), reader.GetFloat(2));
                        return;
                    }
                }
            }, token).ConfigureAwait(false);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_PK_EXT, COLUMN_CENTER_POS_X, COLUMN_CENTER_POS_Z, COLUMN_SIZE_X, COLUMN_SIZE_Z)} FROM `{TABLE_RECTANGLE_DATA}` WHERE `{COLUMN_PK_EXT}` " +
            pin, pkeys, reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (pk == list[i].Id)
                    {
                        list[i].WithRectSize(reader.GetFloat(3), reader.GetFloat(4)).WithPosition(reader.GetFloat(1), reader.GetFloat(2));
                        return;
                    }
                }
            }, token).ConfigureAwait(false);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_PK_EXT, COLUMN_ADJACENT_ZONE, COLUMN_WEIGHT)} FROM `{TABLE_ADJACENCIES}` WHERE `{COLUMN_PK_EXT}` " +
            pin, pkeys, reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < list.Count; ++i)
                {
                    if (pk == list[i].Id)
                    {
                        ref List<AdjacentFlagData>? l = ref adj[i];
                        (l ??= new List<AdjacentFlagData>(5)).Add(new AdjacentFlagData(reader.GetUInt32(1), reader.GetFloat(2)));
                        return;
                    }
                }
            }, token).ConfigureAwait(false);
        List<GridObject> gobjs = new List<GridObject>(128);
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnList(COLUMN_PK_EXT, COLUMN_GRID_OBJ_INSTANCE_ID, COLUMN_GRID_OBJ_GUID, COLUMN_GRID_OBJ_POS_X, COLUMN_GRID_OBJ_POS_Y, COLUMN_GRID_OBJ_POS_Z)} FROM `{TABLE_GRID_OBJECTS}` WHERE `{COLUMN_PK_EXT}` " +
            pin, pkeys, reader =>
            {
                gobjs.Add(new GridObject(reader.GetUInt32(0), reader.IsDBNull(1) ? uint.MaxValue : reader.GetUInt32(1), reader.IsDBNull(2) ? Guid.Empty : reader.ReadGuidString(2) ?? Guid.Empty,
                    reader.IsDBNull(3) ? float.NaN : reader.GetFloat(3), reader.IsDBNull(4) ? float.NaN : reader.GetFloat(4), reader.IsDBNull(5) ? float.NaN : reader.GetFloat(5)));
            }, token).ConfigureAwait(false);

        Zone[] zones = new Zone[list.Count];
        for (int i = 0; i < list.Count; ++i)
        {
            ZoneBuilder builder = list[i];
            if (builder.ZoneType == ZoneType.Polygon && v2[i] is { Count: > 0 } points)
                builder.WithPoints(points.OrderBy(x => x.Key).Select(x => x.Value).ToArray());
            
            if (adj[i] is { Count: > 0 } adjacents)
                builder.WithAdjacencies(adjacents.ToArray());
            uint id = builder.Id;
            builder.WithGridObjects(gobjs.Where(x => x.PrimaryKey.Key == id).ToArray());
            zones[i] = builder.Build().GetZone();
        }

        return zones;
    }

    /// <inheritdoc/>
    public override async Task<int> ImportFromJson(string path, bool resetPrimaryKeys = true, Comparison<Zone>? equalityMatch = null,
        CancellationToken token = new CancellationToken(), CustomJsonDeserializer<Zone>? deserializer = null)
    {
        token.ThrowIfCancellationRequested();
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists) return -2;
            JsonZoneProvider provider = new JsonZoneProvider(info);
            provider.Reload();
            if (resetPrimaryKeys)
            {
                for (int i = 0; i < provider.Zones.Count; ++i)
                {
                    provider.Zones[i].PrimaryKey = PrimaryKey.NotAssigned;
                }
            }

            if (equalityMatch != null)
            {
                WriteWait();
                try
                {
                    for (int i = 0; i < Items.Count; ++i)
                    {
                        Zone? item = Items[i].Item;
                        if (item == null)
                            continue;
                        for (int j = 0; j < provider.Zones.Count; ++j)
                        {
                            if (equalityMatch(provider.Zones[j], item) == 0)
                            {
                                provider.Zones[j].PrimaryKey = Items[i].PrimaryKey;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    WriteRelease();
                }
            }
            await Sql.NonQueryAsync("SET FOREIGN_KEY_CHECKS=0;", null, token).ConfigureAwait(false);
            await AddOrUpdateNoLock(provider.Zones, token).ConfigureAwait(false);
            await Sql.NonQueryAsync("SET FOREIGN_KEY_CHECKS=1;", null, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await DownloadAllNoLock(token, true).ConfigureAwait(false);
            return provider.Zones.Count;
        }
        finally
        {
            Release();
        }
    }

    /// <inheritdoc/>
    public override async Task ExportToJson(string path, CustomJsonSerializer<Zone> serializer, CancellationToken token = new CancellationToken())
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            WriteWait();
            try
            {
                FileInfo info = new FileInfo(path);
                JsonZoneProvider provider = new JsonZoneProvider(info, Items.Select(x => x.Item).Where(x => x is not null).ToList()!);
                provider.Save();
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

    private static ZoneModel ReadZoneModel(MySqlDataReader reader, int offset)
    {
        ZoneModel mdl = new ZoneModel
        {
            Map = reader.IsDBNull(offset) ? -1 : reader.GetInt32(offset),
            ZoneType = reader.IsDBNull(offset + 1) ? ZoneType.Invalid : reader.ReadStringEnum(offset + 1, ZoneType.Invalid),
            Name = reader.IsDBNull(offset + 2) ? null! : reader.GetString(offset + 2),
            ShortName = reader.IsDBNull(offset + 3) ? null! : reader.GetString(offset + 3),
            SpawnX = reader.IsDBNull(offset + 4) ? float.NaN : reader.GetFloat(offset + 4),
            SpawnZ = reader.IsDBNull(offset + 5) ? float.NaN : reader.GetFloat(offset + 5),
            UseCase = reader.IsDBNull(offset + 6) ? ZoneUseCase.Other : reader.ReadStringEnum(offset + 6, ZoneUseCase.Other),
            UseMapCoordinates = !reader.IsDBNull(offset + 7) && reader.GetBoolean(offset + 7),
            MinimumHeight = reader.IsDBNull(offset + 8) ? float.NaN : reader.GetFloat(offset + 8),
            MaximumHeight = reader.IsDBNull(offset + 9) ? float.NaN : reader.GetFloat(offset + 9),
            Flags = reader.IsDBNull(offset + 10) ? ZoneFlags.None : (ZoneFlags)reader.GetUInt64(offset + 10)
        };
        if (offset > 0)
            mdl.Id = reader.GetUInt32(offset - 1);
        if (mdl.Name == null && mdl.ShortName != null)
            mdl.Name = mdl.ShortName;
        return mdl;
    }
    #endregion

    void IUIListener.ShowUI(UCPlayer player) { }
    void IUIListener.HideUI(UCPlayer player) { }
    void IUIListener.UpdateUI(UCPlayer player)
    {
        if (player.Player.TryGetComponent(out ZonePlayerComponent comp))
            comp.ReloadLang();
    }
}
