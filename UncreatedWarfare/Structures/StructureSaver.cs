using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Sync;
using UnityEngine;
using Action = System.Action;

namespace Uncreated.Warfare.Structures;

[Sync((int)WarfareSyncTypes.StructureSaver)]
[SingletonDependency(typeof(Level))]
[SingletonDependency(typeof(MapScheduler))]
public sealed class StructureSaver : ListSqlSingleton<SavedStructure>, ILevelStartListenerAsync
{
    private static readonly MethodInfo? _onStateUpdatedInteractableStorageMethod =
        typeof(InteractableStorage).GetMethod("onStateUpdated", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo? _updateDisplayMethod =
        typeof(InteractableStorage).GetMethod("updateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public StructureSaver() : base("structures", SCHEMAS) { }
    public override Task PostReload()
    {
        return Level.isLoaded ? (this as ILevelStartListenerAsync).OnLevelReady() : Task.CompletedTask;
    }
    [Obsolete]
    protected override async Task AddOrUpdateItem(SavedStructure? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item != null)
        {
            if (MapScheduler.Current == -1)
                throw new InvalidOperationException("MapScheduler not loaded.");
            ItemAsset? asset = Assets.find(item.ItemGuid) as ItemAsset;
            ItemStorageAsset? storage = asset as ItemStorageAsset;
            bool hasPk = pk.IsValid;
            object[] p = new object[hasPk ? 12 : 11];
            p[0] = MapScheduler.Current;
            p[1] = item.ItemGuid.ToSqlParameter();
            p[2] = item.Position.x;
            p[3] = item.Position.y;
            p[4] = item.Position.z;
            p[5] = item.Rotation.x;
            p[6] = item.Rotation.y;
            p[7] = item.Rotation.z;
            p[8] = item.Owner;
            p[9] = item.Group;
            if (storage != null && item.Metadata != null)
            {
                byte[] newState = new byte[sizeof(ulong) * 2];
                Buffer.BlockCopy(item.Metadata, 0, newState, 0, Math.Min(item.Metadata.Length, newState.Length));
                p[10] = newState;
            }
            else p[10] = item.Metadata ?? Array.Empty<byte>();

            if (hasPk)
                p[11] = item.PrimaryKey.Key;

            int pk2 = PrimaryKey.NotAssigned;
            await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_MAP}`,`{COLUMN_GUID}`," +
                           $"`{COLUMN_POS_X}`,`{COLUMN_POS_Y}`,`{COLUMN_POS_Z}`," +
                           $"`{COLUMN_ROT_X}`,`{COLUMN_ROT_Y}`,`{COLUMN_ROT_Z}`," +
                           $"`{COLUMN_OWNER}`,`{COLUMN_GROUP}`,`{COLUMN_METADATA}`"
                           + (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
                           ") VALUES (" +
                           "@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10" + (hasPk ? ",@11" : string.Empty) +
                           ") ON DUPLICATE KEY UPDATE " +
                           $"`{COLUMN_GUID}`=@1," +
                           $"`{COLUMN_POS_X}`=@2,`{COLUMN_POS_Y}`=@3,`{COLUMN_POS_Z}`=@4," +
                           $"`{COLUMN_ROT_X}`=@5,`{COLUMN_ROT_Y}`=@6,`{COLUMN_ROT_Z}`=@7," +
                           $"`{COLUMN_OWNER}`=@8,`{COLUMN_GROUP}`=@9,`{COLUMN_METADATA}`=@10," +
                           $"`{COLUMN_PK}`=LAST_INSERT_ID(`{COLUMN_PK}`); " +
                           "SET @pk := (SELECT LAST_INSERT_ID() as `pk`); " +
                           $"DELETE FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@pk; " +
                           $"DELETE FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@pk; " +
                           $"DELETE FROM `{TABLE_INSTANCES}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@pk; " +
                           "SELECT @pk;",
            p, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
            PrimaryKey structKey = pk2;
            item.PrimaryKey = structKey;
            if (item.InstanceID > 0 && ServerRegion.Key != byte.MaxValue)
            {
                await Sql.NonQueryAsync($"INSERT INTO `{TABLE_INSTANCES}` " +
                                        $"(`{COLUMN_INSTANCES_REGION_KEY}`,`{COLUMN_ITEM_STRUCTURE_PK}`,`{COLUMN_INSTANCES_INSTANCE_ID}`)" +
                                        " VALUES (@0,@1,@2);",
                        new object[] { ServerRegion.Key, pk2, item.InstanceID }, token)
                    .ConfigureAwait(false);
            }
            if (storage != null && item.Metadata != null)
            {
                ItemJarData[] data = item.Items ?? Array.Empty<ItemJarData>();
                if (data.Length > 0)
                {
                    StringBuilder builder = new StringBuilder($"INSERT INTO `{TABLE_STRUCTURE_ITEMS}` (" +
                                                              $"`{COLUMN_ITEM_STRUCTURE_PK}`,`{COLUMN_ITEM_GUID}`," +
                                                              $"`{COLUMN_ITEM_AMOUNT}`,`{COLUMN_ITEM_QUALITY}`," +
                                                              $"`{COLUMN_ITEM_POS_X}`,`{COLUMN_ITEM_POS_Y}`," +
                                                              $"`{COLUMN_ITEM_ROT}`,`{COLUMN_ITEM_METADATA}`) VALUES ", 256);
                    object[] objs = new object[data.Length * 8];
                    for (int i = 0; i < data.Length; ++i)
                    {
                        ItemJarData dataPt = data[i];
                        int ind = i * 8;
                        if (i != 0)
                            builder.Append(',');
                        builder.Append('(');
                        for (int j = 0; j < 8; ++j)
                        {
                            if (j != 0)
                                builder.Append(',');
                            builder.Append('@').Append(ind + j);
                        }

                        builder.Append(')');

                        objs[ind] = structKey.Key;
                        objs[++ind] = dataPt.Item.ToSqlParameter();
                        objs[++ind] = dataPt.Amount;
                        objs[++ind] = dataPt.Quality;
                        objs[++ind] = dataPt.X;
                        objs[++ind] = dataPt.Y;
                        objs[++ind] = dataPt.Rotation;
                        objs[++ind] = dataPt.Metadata ?? Array.Empty<byte>();
                    }
                    builder.Append(';');
                    await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                }
                if (storage.isDisplay && item.DisplayData.HasValue)
                {
                    ItemDisplayData disp = item.DisplayData.Value;
                    await Sql.NonQueryAsync($"INSERT INTO `{TABLE_DISPLAY_DATA}` (" +
                                            $"`{COLUMN_PK}`," +
                                            $"`{COLUMN_DISPLAY_SKIN}`,`{COLUMN_DISPLAY_MYTHIC}`,`{COLUMN_DISPLAY_ROT}`," +
                                            $"`{COLUMN_DISPLAY_TAGS}`,`{COLUMN_DISPLAY_DYNAMIC_PROPS}`) VALUES (@0, @1, @2, @3, @4, @5);",
                        new object[]
                        {
                            pk2, disp.Skin, disp.Mythic, disp.Rotation,
                            string.IsNullOrEmpty(disp.Tags) ? DBNull.Value : disp.Tags!,
                            string.IsNullOrEmpty(disp.DynamicProps) ? DBNull.Value : disp.DynamicProps!
                        }, token).ConfigureAwait(false);
                }
            }
        }
        else if (pk.IsValid)
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0; " +
                                    $"DELETE FROM `{TABLE_INSTANCES}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@0;" +
                                    $"DELETE FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@0;" +
                                    $"DELETE FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@0;",
                new object[] { pk.Key }, token).ConfigureAwait(false);
        }
        else throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
    }
    [Obsolete]
    protected override async Task<SavedStructure[]> DownloadAllItems(CancellationToken token = default)
    {
        if (MapScheduler.Current == -1)
            throw new InvalidOperationException("MapScheduler not loaded.");
        List<SavedStructure> str = new List<SavedStructure>(32);
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_GUID}`," +
                             $"`{COLUMN_POS_X}`,`{COLUMN_POS_Y}`,`{COLUMN_POS_Z}`," +
                             $"`{COLUMN_ROT_X}`,`{COLUMN_ROT_Y}`,`{COLUMN_ROT_Z}`," +
                             $"`{COLUMN_OWNER}`,`{COLUMN_GROUP}`,`{COLUMN_METADATA}` FROM `{TABLE_MAIN}` " +
                             $"WHERE `{COLUMN_MAP}`=@0;", new object[] { MapScheduler.Current },
            reader =>
            {
                str.Add(new SavedStructure()
                {
                    PrimaryKey = reader.GetInt32(0),
                    ItemGuid = reader.ReadGuid(1),
                    Position = new Vector3(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4)),
                    Rotation = new Vector3(reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7)),
                    Owner = reader.GetUInt64(8),
                    Group = reader.GetUInt64(9),
                    Metadata = reader.ReadByteArray(10)
                });
            }, token).ConfigureAwait(false);
        List<PrimaryKey>? del = null;
        if (ServerRegion.Key != byte.MaxValue)
        {
            await Sql.QueryAsync($"SELECT `{COLUMN_ITEM_STRUCTURE_PK}`,`{COLUMN_INSTANCES_INSTANCE_ID}` FROM " +
                                 $"`{TABLE_INSTANCES}` WHERE `{COLUMN_INSTANCES_REGION_KEY}`=@0;",
                new object[] { ServerRegion.Key }, reader =>
                {
                    PrimaryKey pk = reader.GetInt32(0);
                    for (int i = 0; i < str.Count; ++i)
                    {
                        if (str[i].PrimaryKey.Key == pk.Key)
                        {
                            str[i].InstanceID = reader.GetUInt32(1);
                            return;
                        }
                    }
                }, token).ConfigureAwait(false);
        }
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_DISPLAY_SKIN}`,`{COLUMN_DISPLAY_MYTHIC}`," +
                             $"`{COLUMN_DISPLAY_ROT}`,`{COLUMN_DISPLAY_DYNAMIC_PROPS}`,`{COLUMN_DISPLAY_TAGS}`" +
                             $" FROM `{TABLE_DISPLAY_DATA}`;", null, reader =>
        {
            PrimaryKey pk = reader.GetInt32(0);
            for (int i = 0; i < str.Count; ++i)
            {
                if (str[i].PrimaryKey.Key == pk.Key)
                {
                    str[i].DisplayData = new ItemDisplayData(
                        pk, reader.GetUInt16(1), reader.GetUInt16(2),
                        reader.GetByte(3), reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.IsDBNull(4) ? null : reader.GetString(4));
                    return;
                }
            }

            (del ??= new List<PrimaryKey>(2)).Add(pk);
        }, token).ConfigureAwait(false);
        if (del != null)
        {
            StringBuilder sb = new StringBuilder($"DELETE FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}` IN (", 48);
            for (int i = 0; i < del.Count; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(del[i]);
            }

            sb.Append(");");

            await Sql.NonQueryAsync(sb.ToString(), null, token).ConfigureAwait(false);
        }
        List<ItemJarData>? itemData = null;
        await Sql.QueryAsync($"SELECT `{COLUMN_ITEM_PK}`,`{COLUMN_ITEM_STRUCTURE_PK}`,`{COLUMN_ITEM_GUID}`," +
                             $"`{COLUMN_ITEM_AMOUNT}`,`{COLUMN_ITEM_QUALITY}`,`{COLUMN_ITEM_POS_X}`," +
                             $"`{COLUMN_ITEM_POS_Y}`,`{COLUMN_ITEM_ROT}`,`{COLUMN_ITEM_METADATA}` FROM `{TABLE_STRUCTURE_ITEMS}`;",
            null, reader =>
            {
                (itemData ??= new List<ItemJarData>(8)).Add(new ItemJarData(reader.GetInt32(0), reader.GetInt32(1),
                    reader.ReadGuid(2), reader.GetByte(5), reader.GetByte(6), reader.GetByte(7),
                    reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(8)));
            }, token).ConfigureAwait(false);
        if (itemData != null)
        {
            for (int i = 0; i < str.Count; ++i)
            {
                int pk = str[i].PrimaryKey.Key;
                ItemJarData[] d = itemData.Where(x => x.Structure.Key == pk).ToArray();
                if (d.Length > 0)
                    str[i].Items = d;
            }
        }

        return str.ToArray();
    }
    [Obsolete]
    protected override async Task<SavedStructure?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        object[] arr = new object[] { pk.Key };
        SavedStructure? str = null;
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_GUID}`," +
                             $"`{COLUMN_POS_X}`,`{COLUMN_POS_Y}`,`{COLUMN_POS_Z}`," +
                             $"`{COLUMN_ROT_X}`,`{COLUMN_ROT_Y}`,`{COLUMN_ROT_Z}`," +
                             $"`{COLUMN_OWNER}`,`{COLUMN_GROUP}`,`{COLUMN_METADATA}`,`{COLUMN_MAP}` " +
                             $"FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0 LIMIT 1;", arr,
            reader =>
            {
                str = new SavedStructure()
                {
                    PrimaryKey = reader.GetInt32(0),
                    ItemGuid = reader.ReadGuid(1),
                    Position = new Vector3(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4)),
                    Rotation = new Vector3(reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7)),
                    Owner = reader.GetUInt64(8),
                    Group = reader.GetUInt64(9),
                    Metadata = reader.ReadByteArray(10)
                };
                return true;
            }, token).ConfigureAwait(false);
        if (str == null)
            return null;
        if (ServerRegion.Key != byte.MaxValue)
        {
            await Sql.QueryAsync($"SELECT `{COLUMN_INSTANCES_INSTANCE_ID}` FROM " +
                                 $"`{TABLE_INSTANCES}` WHERE `{COLUMN_INSTANCES_REGION_KEY}`=@0 " +
                                 $"AND `{COLUMN_ITEM_STRUCTURE_PK}=@1 LIMIT 1;",
                new object[] { ServerRegion.Key, arr[0] }, reader =>
                {
                    uint id = reader.GetUInt32(0);
                    str.InstanceID = id;
                }, token).ConfigureAwait(false);
        }
        await Sql.QueryAsync($"SELECT `{COLUMN_PK}`,`{COLUMN_DISPLAY_SKIN}`,`{COLUMN_DISPLAY_MYTHIC}`," +
                             $"`{COLUMN_DISPLAY_ROT}`,`{COLUMN_DISPLAY_DYNAMIC_PROPS}`,`{COLUMN_DISPLAY_TAGS}`" +
                             $" FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@0 LIMIT 1;", arr, reader =>
                             {
                                 str.DisplayData = new ItemDisplayData(
                                     reader.GetInt32(0), reader.GetUInt16(1), reader.GetUInt16(2),
                                     reader.GetByte(3), reader.IsDBNull(5) ? null : reader.GetString(5),
                                     reader.IsDBNull(4) ? null : reader.GetString(4));
                                 return true;
                             }, token).ConfigureAwait(false);
        List<ItemJarData>? itemData = null;
        await Sql.QueryAsync($"SELECT `{COLUMN_ITEM_PK}`,`{COLUMN_ITEM_STRUCTURE_PK}`,`{COLUMN_ITEM_GUID}`," +
                             $"`{COLUMN_ITEM_AMOUNT}`,`{COLUMN_ITEM_QUALITY}`,`{COLUMN_ITEM_POS_X}`," +
                             $"`{COLUMN_ITEM_POS_Y}`,`{COLUMN_ITEM_ROT}`,`{COLUMN_ITEM_METADATA}` " +
                             $"FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_PK}`=@0;",
            arr, reader =>
            {
                (itemData ??= new List<ItemJarData>(8)).Add(new ItemJarData(reader.GetInt32(0), reader.GetInt32(1),
                    reader.ReadGuid(2), reader.GetByte(5), reader.GetByte(6), reader.GetByte(7),
                    reader.GetByte(3), reader.GetByte(4), reader.ReadByteArray(8)));
            }, token).ConfigureAwait(false);
        if (itemData != null)
        {
            ItemJarData[] d = itemData.ToArray();
            if (d.Length > 0)
                str.Items = d;
        }

        return str;
    }
    async Task ILevelStartListenerAsync.OnLevelReady()
    {
        // await ImportFromJson(Path.Combine(Data.Paths.StructureStorage, "structures.json"), true, (x, y) => x.InstanceID.CompareTo(y.InstanceID)).ConfigureAwait(false);
        L.Log("Checking structures: " + Items.Count + " item(s) pending...", ConsoleColor.Magenta);
        await WaitAsync(F.DebugTimeout).ConfigureAwait(false);
        try
        {
            List<SavedStructure>? toSave = null;
            await UCWarfare.ToUpdate();
            for (int i = 0; i < Items.Count; ++i)
            {
                SqlItem<SavedStructure> item = Items[i];
                if (item.Item != null)
                {
                    int err = CheckExisting(item.Item);
                    switch (err)
                    {
                        case 2:
                            L.LogWarning("Error, GUID not found: " + item);
                            break;
                        case 3:
                            L.LogWarning("Error, GUID not barricade or barricade: " + item);
                            break;
                        case 12:
                        case 6:
                            L.LogWarning("Error, Failed to replace: " + item);
                            break;
                        case 5:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                            (toSave ??= new List<SavedStructure>(4)).Add(item.Item);
                            goto case 0;
                        case 0:
                            L.LogDebug("Confirmed validity of barricade {" + err.ToString("X2") + "}: " + item);
                            continue;
                        default:
                            L.LogWarning("Error, {" + err.ToString("X2") + "}: " + item);
                            break;
                    }
                }
            }
            if (toSave != null)
            {
                L.LogDebug("Resaving " + toSave.Count + " item(s)...");
                await AddOrUpdateNoLock(toSave).ConfigureAwait(false);
            }
        }
        finally
        {
            Release();
        }
    }
    private int CheckExisting(SavedStructure structure)
    {
        if (Assets.find(structure.ItemGuid) is not ItemAsset item)
            return 2;
        if (item is ItemStructureAsset)
            goto structure;
        else if (item is not ItemBarricadeAsset)
             return 3;

        ItemBarricadeAsset brAsset = (ItemBarricadeAsset)item;
        BarricadeDrop? bdrop = structure.InstanceID > 0 ? UCBarricadeManager.FindBarricade(structure.InstanceID, structure.Position) : null;
        if (bdrop == null)
        {
            bdrop = UCBarricadeManager.GetBarricadeFromPosition(structure.Position);
            if (bdrop == null || bdrop.asset.GUID != structure.ItemGuid)
            {
                return Replace(structure, brAsset) ? 5 : 6;
            }
            structure.InstanceID = bdrop.instanceID;
            structure.Position = bdrop.model.position;
            structure.Rotation = bdrop.model.rotation.eulerAngles;
            structure.Buildable = new UCBarricade(bdrop);
            FillHp(bdrop);
            BarricadeManager.changeOwnerAndGroup(bdrop.model, structure.Owner, structure.Group);
            return 7;
        }
        if (!structure.Position.AlmostEquals(bdrop.model.position) || !structure.Rotation.AlmostEquals(bdrop.model.rotation.eulerAngles))
        {
            structure.Position = bdrop.model.position;
            structure.Rotation = bdrop.model.rotation.eulerAngles;
            structure.Buildable = new UCBarricade(bdrop);
            FillHp(bdrop);
            BarricadeManager.changeOwnerAndGroup(bdrop.model, structure.Owner, structure.Group);
            return 8;
        }
        FillHp(bdrop);
        BarricadeManager.changeOwnerAndGroup(bdrop.model, structure.Owner, structure.Group);
        structure.Buildable = new UCBarricade(bdrop);

        return 0;
        structure:
        ItemStructureAsset stAsset = (ItemStructureAsset)item;
        StructureDrop? sdrop = UCBarricadeManager.FindStructure(structure.InstanceID, structure.Position);
        if (sdrop == null)
        {
            sdrop = UCBarricadeManager.GetStructureFromPosition(structure.Position);
            if (sdrop == null || sdrop.asset.GUID != structure.ItemGuid)
            {
                return Replace(structure, stAsset) ? 11 : 12;
            }
            structure.InstanceID = sdrop.instanceID;
            structure.Position = sdrop.model.position;
            structure.Rotation = sdrop.model.rotation.eulerAngles;
            structure.Buildable = new UCStructure(sdrop);
            FillHp(sdrop);
            StructureManager.changeOwnerAndGroup(sdrop.model, structure.Owner, structure.Group);
            return 9;
        }
        if (!structure.Position.AlmostEquals(sdrop.model.position) || !structure.Rotation.AlmostEquals(sdrop.model.rotation.eulerAngles))
        {
            structure.Position = sdrop.model.position;
            structure.Rotation = sdrop.model.rotation.eulerAngles;
            structure.Buildable = new UCStructure(sdrop);
            FillHp(sdrop);
            StructureManager.changeOwnerAndGroup(sdrop.model, structure.Owner, structure.Group);
            return 10;
        }
        FillHp(sdrop);
        StructureManager.changeOwnerAndGroup(sdrop.model, structure.Owner, structure.Group);
        structure.Buildable = new UCStructure(sdrop);

        return 0;
    }
    private static void FillHp(BarricadeDrop drop)
    {
        ushort hp = drop.GetServersideData().barricade.health;
        if (drop.asset.health > hp)
        {
            BarricadeManager.repair(drop.model, drop.asset.health, 1f, Provider.server);
        }
    }
    private static void FillHp(StructureDrop drop)
    {
        ushort hp = drop.GetServersideData().structure.health;
        if (drop.asset.health > hp)
        {
            StructureManager.repair(drop.model, drop.asset.health, 1f, Provider.server);
        }
    }
    private static bool Replace(SavedStructure structure, ItemBarricadeAsset asset)
    {
        byte[] state = GetBarricadeState(null, asset, structure);
        Transform t = BarricadeManager.dropNonPlantedBarricade(new Barricade(asset, asset.health, state),
            structure.Position, Quaternion.Euler(structure.Rotation), structure.Owner, structure.Group);
        BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(t);
        if (drop == null)
            return false;
        structure.Buildable = new UCBarricade(drop);
        if (asset is ItemStorageAsset storage && drop.interactable is InteractableStorage intx)
        {
            bool a = false;
            if (structure.Items != null && Data.SetStorageInventory != null)
            {
                Items inv = new Items(PlayerInventory.STORAGE);
                inv.resize(storage.storage_x, storage.storage_y);
                for (int i = 0; i < structure.Items.Length; ++i)
                {
                    ref ItemJarData data = ref structure.Items[i];
                    if (Assets.find(data.Item) is ItemAsset item)
                        inv.loadItem(data.X, data.Y, data.Rotation,
                            new Item(item.id, data.Amount, data.Quality, data.Metadata));
                }

                if (_onStateUpdatedInteractableStorageMethod != null)
                {
                    inv.onStateUpdated = (StateUpdated)Delegate.Combine(inv.onStateUpdated,
                        _onStateUpdatedInteractableStorageMethod.CreateDelegate(typeof(StateUpdated), intx));
                }
                else L.LogWarning("Unknown method: void onStateUpdated() in InteractableStorage.");
                Data.SetStorageInventory(intx, inv);
                a = true;
            }

            if (storage.isDisplay && structure.DisplayData.HasValue)
            {
                ItemDisplayData data = structure.DisplayData.Value;
                intx.displaySkin = data.Skin;
                intx.displayMythic = data.Mythic;
                intx.displayTags = data.Tags ?? string.Empty;
                intx.displayDynamicProps = data.DynamicProps ?? string.Empty;
                intx.applyRotation(data.Rotation);
                _updateDisplayMethod?.Invoke(intx, Array.Empty<object>());
                intx.refreshDisplay();
                a = true;
            }

            if (a)
            {
                intx.rebuildState();
                structure.Metadata = drop.GetServersideData().barricade.state;
            }
        }

        return true;
    }
    private static bool Replace(SavedStructure structure, ItemStructureAsset asset)
    {
        if (Regions.tryGetCoordinate(structure.Position, out byte x, out byte y))
        {
            Quaternion rotation = Quaternion.Euler(-90f, structure.Rotation.y, 0f);
            StructureRegion region = StructureManager.regions[x, y];
            Structure str = new Structure(asset, asset.health);
            bool success = StructureManager.dropReplicatedStructure(str,
                structure.Position, rotation, structure.Owner, structure.Group);
            if (!success) return false;
            StructureDrop? drop = region.drops.TailOrDefault();
            if (drop == null || drop.GetServersideData().structure != str)
            {
                for (int i = 0; i < region.drops.Count; ++i)
                {
                    if (region.drops[i].GetServersideData().structure == str)
                    {
                        drop = region.drops[i];
                        break;
                    }
                }
                if (drop == null)
                {
                    L.LogWarning("Structure spawn reported successful but couldn't be found in region {"
                                 + x + ", " + y + "}.");
                    return false;
                }
            }
            structure.Buildable = new UCStructure(drop);
            structure.Position = drop.model.position;
            structure.Rotation = drop.model.rotation.eulerAngles;
            return true;
        }
        return false;
    }
    private static byte[] GetBarricadeState(BarricadeDrop? drop, ItemBarricadeAsset asset, SavedStructure structure)
    {
        byte[] st2 = drop != null ? drop.GetServersideData().barricade.state : (structure.Metadata is null || structure.Metadata.Length == 0 ? Array.Empty<byte>() : structure.Metadata);
        byte[] owner = BitConverter.GetBytes(structure.Owner);
        byte[] group = BitConverter.GetBytes(structure.Group);
        byte[] state;
        switch (asset.build)
        {
            case EBuild.DOOR:
            case EBuild.GATE:
            case EBuild.SHUTTER:
            case EBuild.HATCH:
                state = new byte[17];
                Buffer.BlockCopy(owner, 0, state, 0, sizeof(ulong));
                Buffer.BlockCopy(group, 0, state, sizeof(ulong), sizeof(ulong));
                state[16] = (byte)(st2[16] > 0 ? 1 : 0);
                break;
            case EBuild.BED:
                state = owner;
                break;
            case EBuild.STORAGE:
            case EBuild.SENTRY:
            case EBuild.SENTRY_FREEFORM:
            case EBuild.SIGN:
            case EBuild.SIGN_WALL:
            case EBuild.NOTE:
            case EBuild.LIBRARY:
            case EBuild.MANNEQUIN:
                state = Util.CloneBytes(st2);
                if (state.Length > 15)
                {
                    Buffer.BlockCopy(owner, 0, state, 0, sizeof(ulong));
                    Buffer.BlockCopy(group, 0, state, sizeof(ulong), sizeof(ulong));
                }
                break;
            case EBuild.SPIKE:
            case EBuild.WIRE:
            case EBuild.CHARGE:
            case EBuild.BEACON:
            case EBuild.CLAIM:
                state = Array.Empty<byte>();
                if (drop != null)
                {
                    if (drop.interactable is InteractableCharge charge)
                    {
                        charge.owner = structure.Owner;
                        charge.group = structure.Group;
                    }
                    else if (drop.interactable is InteractableClaim claim)
                    {
                        claim.owner = structure.Owner;
                        claim.group = structure.Group;
                    }
                }
                break;
            default:
                state = Util.CloneBytes(st2);
                break;
        }

        return state;
    }
    public async Task<(SqlItem<SavedStructure>, bool)> AddStructure(StructureDrop drop, CancellationToken token = default)
    {
        if (drop == null || drop.model == null)
            throw new ArgumentNullException(nameof(drop));
        this.AssertLoadedIntl();

        uint id = drop.instanceID;
        for (int i = 0; i < this.Items.Count; ++i)
        {
            SqlItem<SavedStructure> str = Items[i];
            await str.Enter(token).ConfigureAwait(false);
            try
            {
                if (str.Item != null && str.Item.InstanceID == id && str.Item.ItemGuid == drop.asset.GUID)
                    return (str, false);
            }
            finally
            {
                str.Release();
            }
        }

        StructureData data = drop.GetServersideData();
        SavedStructure structure = new SavedStructure()
        {
            InstanceID = drop.instanceID,
            Group = data.group,
            Owner = data.owner,
            ItemGuid = drop.asset.GUID,
            Position = drop.model.position,
            Rotation = drop.model.rotation.eulerAngles with { x = 0f, z = 0f },
            PrimaryKey = PrimaryKey.NotAssigned,
            Metadata = Array.Empty<byte>(),
            Buildable = new UCStructure(drop),
            DisplayData = null,
            Items = null
        };
        return (await AddOrUpdate(structure, token).ConfigureAwait(false), true);
    }
    public async Task<(SqlItem<SavedStructure>, bool)> AddBarricade(BarricadeDrop drop, CancellationToken token = default)
    {
        if (drop == null || drop.model == null)
            throw new ArgumentNullException(nameof(drop));
        this.AssertLoadedIntl();

        uint id = drop.instanceID;
        for (int i = 0; i < this.Items.Count; ++i)
        {
            SqlItem<SavedStructure> str = Items[i];
            await str.Enter(token).ConfigureAwait(false);
            try
            {
                if (str.Item != null && str.Item.InstanceID == id && str.Item.ItemGuid == drop.asset.GUID)
                    return (str, false);
            }
            finally
            {
                str.Release();
            }
        }

        BarricadeData data = drop.GetServersideData();
        SavedStructure structure = new SavedStructure()
        {
            InstanceID = drop.instanceID,
            Group = data.group,
            Owner = data.owner,
            ItemGuid = drop.asset.GUID,
            Position = drop.model.position,
            Rotation = drop.model.rotation.eulerAngles,
            PrimaryKey = PrimaryKey.NotAssigned,
            Buildable = new UCBarricade(drop),
            DisplayData = null,
            Items = null
        };
        if (drop.asset is ItemStorageAsset asset && drop.interactable is InteractableStorage storage)
        {
            Items inv = storage.items;
            int ic = inv.getItemCount();
            structure.Items = new ItemJarData[ic];
            for (int i = 0; i < ic; ++i)
            {
                ItemJar jar = inv.items[i];
                Guid guid;
                if (Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset iasset)
                    guid = iasset.GUID;
                else
                {
                    L.LogWarning("Unknown item #" + jar.item.id + " in storage " + drop.asset.itemName + ".");
                    guid = new Guid(jar.item.id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                }

                structure.Items[i] = new ItemJarData(PrimaryKey.NotAssigned, PrimaryKey.NotAssigned,
                    guid, jar.x, jar.y, jar.rot, jar.item.amount, jar.item.quality, Util.CloneBytes(jar.item.state));
            }

            if (asset.isDisplay)
            {
                structure.DisplayData = new ItemDisplayData(PrimaryKey.NotAssigned, storage.displaySkin,
                    storage.displayMythic, storage.rot_comp,
                    string.IsNullOrEmpty(storage.displayTags) ? null : storage.displayTags,
                    string.IsNullOrEmpty(storage.displayDynamicProps) ? null : storage.displayDynamicProps);
            }

            structure.Metadata = new byte[sizeof(ulong) * 2];
            byte[] owner = BitConverter.GetBytes(structure.Owner);
            byte[] group = BitConverter.GetBytes(structure.Group);
            Buffer.BlockCopy(owner, 0, structure.Metadata, 0, sizeof(ulong));
            Buffer.BlockCopy(group, 0, structure.Metadata, sizeof(ulong), sizeof(ulong));
        }
        else
            structure.Metadata = Util.CloneBytes(data.barricade.state);
        return (await AddOrUpdate(structure, token).ConfigureAwait(false), true);
    }
    public Task RemoveItem(SavedStructure structure, CancellationToken token = default)
    {
        return Delete(structure, token);
    }
    public bool TryGetSave(StructureDrop structure, out SavedStructure value)
    {
        Wait();
        try
        {
            uint id = structure.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCStructure)
                {
                    value = item;
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public bool TryGetSave(BarricadeDrop barricade, out SavedStructure value)
    {
        Wait();
        try
        {
            uint id = barricade.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCBarricade)
                {
                    value = item;
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public bool TryGetSave(uint id, EStructType type, out SavedStructure value)
    {
        Wait();
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable != null && item.Buildable.Type == type)
                {
                    value = item;
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public bool TryGetSave(StructureDrop structure, out SqlItem<SavedStructure> value)
    {
        Wait();
        try
        {
            uint id = structure.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCStructure)
                {
                    value = Items[i];
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public bool TryGetSave(BarricadeDrop barricade, out SqlItem<SavedStructure> value)
    {
        Wait();
        try
        {
            uint id = barricade.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCBarricade)
                {
                    value = Items[i];
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public bool TryGetSave(uint id, EStructType type, out SqlItem<SavedStructure> value)
    {
        Wait();
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable != null && item.Buildable.Type == type)
                {
                    value = Items[i];
                    return true;
                }
            }
        }
        finally
        {
            Release();
        }

        value = null!;
        return false;
    }
    public async Task<SavedStructure?> GetSave(StructureDrop structure, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            uint id = structure.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCStructure)
                {
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
    public async Task<SavedStructure?> GetSave(BarricadeDrop barricade, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            uint id = barricade.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCBarricade)
                {
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
    public async Task<SavedStructure?> GetSave(uint id, EStructType type, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable != null && item.Buildable.Type == type)
                {
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
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(StructureDrop structure, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            uint id = structure.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCStructure)
                {
                    return Items[i];
                }
            }
        }
        finally
        {
            Release();
        }

        return null;
    }
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(BarricadeDrop barricade, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            uint id = barricade.instanceID;
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable is UCBarricade)
                {
                    return Items[i];
                }
            }
        }
        finally
        {
            Release();
        }

        return null;
    }
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(uint id, EStructType type, CancellationToken token = default)
    {
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                SavedStructure? item = Items[i].Item;
                if (item != null && item.InstanceID == id && item.Buildable != null && item.Buildable.Type == type)
                {
                    return Items[i];
                }
            }
        }
        finally
        {
            Release();
        }

        return null;
    }
    public SqlItem<SavedStructure>? GetSaveItemSync(uint instanceId, EStructType type)
    {
        Wait();
        try
        {
            for (int i = 0; i < List.Count; ++i)
            {
                SavedStructure? item = List[i].Item;
                if (item != null && item.InstanceID == instanceId && item.Buildable != null && item.Buildable.Type == type)
                {
                    return List[i];
                }
            }
        }
        finally
        {
            Release();
        }
        return null;
    }
    public void BeginAddStructure(StructureDrop drop)
    {
        Task.Run(async () =>
        {
            try
            {
                if (drop.GetServersideData().structure.isDead)
                    return;
                await AddStructure(drop, F.DebugTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                L.LogError("Error adding structure to structure saver.");
                L.LogError(ex);
            }
        });
    }
    public void BeginAddBarricade(BarricadeDrop drop)
    {
        Task.Run(async () =>
        {
            try
            {
                if (drop.GetServersideData().barricade.isDead)
                    return;
                await AddBarricade(drop, F.DebugTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                L.LogError("Error adding barricade to structure saver.");
                L.LogError(ex);
            }
        });
    }
    public void BeginRemoveItem(SavedStructure structure)
    {
        Task.Run(async () =>
        {
            try
            {
                await RemoveItem(structure, F.DebugTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                L.LogError("Error removing save from structure saver.");
                L.LogError(ex);
            }
        });
    }
    #region Schemas
    public const string TABLE_MAIN = "structures";
    public const string TABLE_DISPLAY_DATA = "structures_display_data";
    public const string TABLE_STRUCTURE_ITEMS = "structures_storage_items";
    public const string TABLE_INSTANCES = "structures_instance_ids";
    public const string COLUMN_PK = "pk";
    public const string COLUMN_MAP = "Map";
    public const string COLUMN_GUID = "Guid";
    public const string COLUMN_POS_X = "Pos_X";
    public const string COLUMN_POS_Y = "Pos_Y";
    public const string COLUMN_POS_Z = "Pos_Z";
    public const string COLUMN_ROT_X = "Rot_X";
    public const string COLUMN_ROT_Y = "Rot_Y";
    public const string COLUMN_ROT_Z = "Rot_Z";
    public const string COLUMN_OWNER = "Owner";
    public const string COLUMN_GROUP = "Group";
    public const string COLUMN_METADATA = "Metadata";
    public const string COLUMN_ITEM_PK = "pk";
    public const string COLUMN_ITEM_STRUCTURE_PK = "Structure";
    public const string COLUMN_ITEM_GUID = "Guid";
    public const string COLUMN_ITEM_POS_X = "Pos_X";
    public const string COLUMN_ITEM_POS_Y = "Pos_Y";
    public const string COLUMN_ITEM_ROT = "Rotation";
    public const string COLUMN_ITEM_AMOUNT = "Amount";
    public const string COLUMN_ITEM_QUALITY = "Quality";
    public const string COLUMN_ITEM_METADATA = "Metadata";
    public const string COLUMN_DISPLAY_SKIN = "Skin";
    public const string COLUMN_DISPLAY_MYTHIC = "Mythic";
    public const string COLUMN_DISPLAY_TAGS = "Tags";
    public const string COLUMN_DISPLAY_DYNAMIC_PROPS = "DynamicProps";
    public const string COLUMN_DISPLAY_ROT = "Rotation";
    public const string COLUMN_INSTANCES_INSTANCE_ID = "InstanceId";
    public const string COLUMN_INSTANCES_REGION_KEY = "Region";
    private static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK,       SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true
            },
            new Schema.Column(COLUMN_MAP,      SqlTypes.MAP_ID),
            new Schema.Column(COLUMN_GUID,     SqlTypes.GUID),
            new Schema.Column(COLUMN_POS_X,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_POS_Y,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_POS_Z,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_X,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_Y,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_Z,    SqlTypes.FLOAT),
            new Schema.Column(COLUMN_OWNER,    SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_GROUP,    SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_METADATA, SqlTypes.BYTES_255)
        }, true, typeof(SavedStructure)),
        new Schema(TABLE_DISPLAY_DATA, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true,
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_DISPLAY_SKIN,   SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_MYTHIC, SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_ROT,    SqlTypes.BYTE),
            new Schema.Column(COLUMN_DISPLAY_TAGS,   SqlTypes.STRING_255)
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_DISPLAY_DYNAMIC_PROPS, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(ItemDisplayData)),
        new Schema(TABLE_STRUCTURE_ITEMS, new Schema.Column[]
        {
            new Schema.Column(COLUMN_ITEM_PK, SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true,
            },
            new Schema.Column(COLUMN_ITEM_STRUCTURE_PK, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_ITEM_GUID,     SqlTypes.GUID),
            new Schema.Column(COLUMN_ITEM_AMOUNT,   SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_QUALITY,  SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_X,    SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_Y,    SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_ROT,      SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.BYTES_255),
        }, false, typeof(ItemJarData)),
        new Schema(TABLE_INSTANCES, new Schema.Column[]
        {
            new Schema.Column(COLUMN_INSTANCES_REGION_KEY, SqlTypes.REGION_KEY),
            new Schema.Column(COLUMN_ITEM_STRUCTURE_PK,    SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = COLUMN_PK,
                ForeignKeyTable = TABLE_MAIN
            },
            new Schema.Column(COLUMN_INSTANCES_INSTANCE_ID, SqlTypes.INSTANCE_ID)
        }, false, null)
    };
    #endregion
}
public record struct ItemJarData(PrimaryKey Key, PrimaryKey Structure, Guid Item, byte X, byte Y, byte Rotation, byte Amount, byte Quality,
    byte[] Metadata) : IListSubItem
{
    PrimaryKey IListSubItem.LinkedKey { get => Structure; set => Structure = value; }
    PrimaryKey IListItem.PrimaryKey { get => Key; set => Key = value; }
}
public record struct ItemDisplayData(PrimaryKey Key, ushort Skin, ushort Mythic, byte Rotation, string? Tags, string? DynamicProps) : IListSubItem
{
    PrimaryKey IListSubItem.LinkedKey { get => Key; set => Key = value; }
    PrimaryKey IListItem.PrimaryKey { get => Key; set => Key = value; }
}
public class UCBarricade : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public EStructType Type => EStructType.BARRICADE;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model;
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public BarricadeDrop Drop { get; internal set; }
    public BarricadeData Data { get; internal set; }
    public UCBarricade(BarricadeDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
}
public class UCStructure : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public EStructType Type => EStructType.BARRICADE;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model;
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public StructureDrop Drop { get; }
    public StructureData Data { get; }
    public UCStructure(StructureDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
}
public interface IBuildable
{
    uint InstanceId { get; }
    EStructType Type { get; }
    ItemAsset Asset { get; }
    Transform Model { get; }
    ulong Owner { get; }
    ulong Group { get; }
    object Drop { get; }
    object Data { get; }
    NetId NetId { get; }
}
public enum EStructType : byte
{
    UNKNOWN = 0,
    STRUCTURE = 1,
    BARRICADE = 2
}