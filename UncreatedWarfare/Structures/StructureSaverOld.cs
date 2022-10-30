using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Structures;

public class StructureSaver : ListSqlSingleton<SavedStructure>, ILevelStartListenerAsync
{
    private const string TABLE_MAIN = "structures";
    private const string TABLE_DISPLAY_DATA = "structures_display_data";
    private const string TABLE_STRUCTURE_ITEMS = "structures_storage_items";
    private const string COLUMN_PK = "pk";
    private const string COLUMN_GUID = "Guid";
    private const string COLUMN_INSTANCE_ID = "InstanceId";
    private const string COLUMN_POS_X = "Pos_X";
    private const string COLUMN_POS_Y = "Pos_Y";
    private const string COLUMN_POS_Z = "Pos_Z";
    private const string COLUMN_ROT_X = "Rot_X";
    private const string COLUMN_ROT_Y = "Rot_Y";
    private const string COLUMN_ROT_Z = "Rot_Z";
    private const string COLUMN_OWNER = "Owner";
    private const string COLUMN_GROUP = "Group";
    private const string COLUMN_METADATA = "Metadata";
    private const string COLUMN_ITEM_PK = "pk";
    private const string COLUMN_ITEM_STRUCTURE_PK = "Structure";
    private const string COLUMN_ITEM_GUID = "Guid";
    private const string COLUMN_ITEM_POS_X = "Pos_X";
    private const string COLUMN_ITEM_POS_Y = "Pos_Y";
    private const string COLUMN_ITEM_ROT = "Rotation";
    private const string COLUMN_ITEM_AMOUNT = "Amount";
    private const string COLUMN_ITEM_QUALITY = "Quality";
    private const string COLUMN_ITEM_METADATA = "Metadata";
    private const string COLUMN_DISPLAY_SKIN = "Skin";
    private const string COLUMN_DISPLAY_MYTHIC = "Mythic";
    private const string COLUMN_DISPLAY_TAGS = "Tags";
    private const string COLUMN_DISPLAY_DYNAMIC_PROPS = "DynamicProps";
    private const string COLUMN_DISPLAY_ROT = "Rotation";
    private static readonly Schema[] SCHEMAS =
    {
        new Schema(TABLE_MAIN, new Schema.Column[]
        {
            new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                AutoIncrement = true,
                PrimaryKey = true
            },
            new Schema.Column(COLUMN_GUID, SqlTypes.GUID),
            new Schema.Column(COLUMN_INSTANCE_ID, SqlTypes.INSTANCE_ID),
            new Schema.Column(COLUMN_POS_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_POS_Y, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_POS_Z, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_X, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_Y, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_ROT_Z, SqlTypes.FLOAT),
            new Schema.Column(COLUMN_OWNER, SqlTypes.STEAM_64),
            new Schema.Column(COLUMN_GROUP, SqlTypes.STEAM_64),
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
            new Schema.Column(COLUMN_DISPLAY_SKIN, SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_MYTHIC, SqlTypes.USHORT),
            new Schema.Column(COLUMN_DISPLAY_ROT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_DISPLAY_TAGS, SqlTypes.STRING_255)
            {
                Nullable = true
            },
            new Schema.Column(COLUMN_DISPLAY_DYNAMIC_PROPS, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(InteractableStorage)),
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
            new Schema.Column(COLUMN_ITEM_GUID, SqlTypes.GUID),
            new Schema.Column(COLUMN_ITEM_AMOUNT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_QUALITY, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_X, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_POS_Y, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_ROT, SqlTypes.BYTE),
            new Schema.Column(COLUMN_ITEM_METADATA, SqlTypes.BYTES_255),
        }, false, typeof(ItemJarData))
    };
    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public StructureSaver() : base("structures", SCHEMAS) { }

    protected override async Task AddOrUpdateItem(SavedStructure? item, PrimaryKey pk, CancellationToken token = default)
    {
        if (item != null)
        {
            ItemAsset? asset = Assets.find(item.ItemGuid) as ItemAsset;
            ItemStorageAsset? storage = asset as ItemStorageAsset;
            bool hasPk = !pk.IsValid;
            object[] p = new object[hasPk ? 12 : 11];
            p[0] = item.ItemGuid.ToSqlParameter();
            p[1] = item.InstanceID;
            p[2] = item.Position.x;
            p[3] = item.Position.y;
            p[4] = item.Position.z;
            p[5] = item.Rotation.x;
            p[6] = item.Rotation.y;
            p[7] = item.Rotation.z;
            p[8] = item.Owner;
            p[9] = item.Group;
            if (storage != null)
            {
                byte[] newState = new byte[sizeof(ulong) * 2];
                Buffer.BlockCopy(item.Metadata, 0, newState, 0, Math.Min(item.Metadata.Length, newState.Length));
                p[10] = newState;
            }
            else p[10] = item.Metadata;

            if (hasPk)
                p[11] = item.PrimaryKey.Key;

            int pk2 = PrimaryKey.NotAssigned;
            await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` (`{COLUMN_GUID}`,`{COLUMN_INSTANCE_ID}`," +
                           $"`{COLUMN_POS_X}`,`{COLUMN_POS_Y}`,`{COLUMN_POS_Z}`," +
                           $"`{COLUMN_ROT_X}`,`{COLUMN_ROT_Y}`,`{COLUMN_ROT_Z}`," +
                           $"`{COLUMN_OWNER}`,`{COLUMN_GROUP}`,`{COLUMN_METADATA}`"
                           + (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
                           ") VALUES (" +
                           "@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10" + (hasPk ? ",@11" : string.Empty) +
                           ") ON DUPLICATE KEY UPDATE " +
                           $"`{COLUMN_GUID}`=@0,`{COLUMN_INSTANCE_ID}`=@1," +
                           $"`{COLUMN_POS_X}`=@2,`{COLUMN_POS_Y}`=@3,`{COLUMN_POS_Z}`=@4," +
                           $"`{COLUMN_ROT_X}`=@5,`{COLUMN_ROT_Y}`=@6,`{COLUMN_ROT_Z}`=@7," +
                           $"`{COLUMN_OWNER}`=@8,`{COLUMN_GROUP}`=@9,`{COLUMN_METADATA}`=@10," +
                           $"`{COLUMN_PK}`=LAST_INSERT_ID(`{COLUMN_PK}`); " +
                           "SET @pk := (SELECT LAST_INSERT_ID() as `pk`); " +
                           $"DELETE FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@pk; " +
                           $"DELETE FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@pk; " +
                           "SELECT @pk;",
            p, (reader) =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
            PrimaryKey structKey = pk2;
            if (storage != null)
            {
                _reader.LoadNew(item.Metadata);
                _reader.Skip(sizeof(ulong) * 2);
                byte itemCt = _reader.ReadUInt8();
                ItemJarData[] data = new ItemJarData[itemCt];
                for (int i = 0; i < itemCt; ++i)
                {
                    byte x = _reader.ReadUInt8();
                    byte y = _reader.ReadUInt8();
                    byte r = _reader.ReadUInt8();
                    ushort id = _reader.ReadUInt16();
                    byte amt = _reader.ReadUInt8();
                    byte quality = _reader.ReadUInt8();
                    byte[] state = _reader.ReadBlock(_reader.ReadUInt8());
                    Guid guid;
                    if (Assets.find(EAssetType.ITEM, id) is ItemAsset itemAsset)
                        guid = itemAsset.GUID;
                    else
                    {
                        guid = new Guid(id, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                        L.LogWarning("Unknown item in storage on save: (UINT16 ID #" + id + ").");
                    }
                    data[i] = new ItemJarData(PrimaryKey.NotAssigned, structKey, guid, x, y, r, amt, quality, state);
                }

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
                        objs[++ind] = dataPt.Metadata;
                    }
                    builder.Append(';');
                    await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                }
                if (storage.isDisplay)
                {
                    ushort skin = _reader.ReadUInt16();
                    ushort mythic = _reader.ReadUInt16();
                    string? tags = _reader.ReadShortString();
                    if (tags.Length < 1)
                        tags = null;
                    string? dynProps = _reader.ReadShortString();
                    if (dynProps.Length < 1)
                        dynProps = null;
                    byte rot = _reader.ReadUInt8();
                    await Sql.NonQueryAsync($"INSERT INTO `{TABLE_DISPLAY_DATA}` (" +
                                            $"`{COLUMN_DISPLAY_SKIN}`,`{COLUMN_DISPLAY_MYTHIC}`,`{COLUMN_DISPLAY_ROT}`," +
                                            $"`{COLUMN_DISPLAY_TAGS}`,`{COLUMN_DISPLAY_DYNAMIC_PROPS}`) VALUES (@0, @1, @2, @3, @4);",
                        new object[]
                        {
                            skin, mythic, rot,
                            string.IsNullOrEmpty(tags)     ? DBNull.Value : tags!,
                            string.IsNullOrEmpty(dynProps) ? DBNull.Value : dynProps!
                        }, token).ConfigureAwait(false);
                }
            }
        }
        else if (pk.IsValid)
        {
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0; " +
                                    $"DELETE FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@0;" +
                                    $"DELETE FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@0;",
                new object[] { pk.Key }, token).ConfigureAwait(false);
        }
        else throw new ArgumentException("If item is null, pk must have a value to delete the item.", nameof(pk));
    }

    private record struct ItemJarData(PrimaryKey Key, PrimaryKey Structure, Guid Item, byte X, byte Y, byte Rotation, byte Amount, byte Quality,
        byte[] Metadata) : IListSubItem
    {
        PrimaryKey IListSubItem.LinkedKey { get => Structure; set => Structure = value; }
        PrimaryKey IListItem.PrimaryKey { get => Key; set => Key = value; }
    }
    private static readonly ByteReader _reader = new ByteReader();
    protected override async Task AddOrUpdateRange(SavedStructure[] items, CancellationToken token = default)
    {
        for (int i = 0; i < items.Length; ++i)
        {
            SavedStructure item = items[i];
            await AddOrUpdateItem(item, item.PrimaryKey, token).ConfigureAwait(false);
        }
    }

    protected override Task<SavedStructure[]> DownloadAllItems(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    protected override Task<SavedStructure?> DownloadItem(PrimaryKey pk, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    Task ILevelStartListenerAsync.OnLevelReady()
    {
        throw new NotImplementedException();
    }
}
public class StructureSaverOld : ListSingleton<SavedStructure>, ILevelStartListener
{
    public static bool Loaded => Singleton.IsLoaded<StructureSaverOld, SavedStructure>();
    internal static StructureSaverOld Singleton;
    public StructureSaverOld() : base("structures", Path.Combine(Data.Paths.StructureStorage, "structures.json")) { }
    public override void Load()
    {
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
    }
    protected override string LoadDefaults() => EMPTY_LIST;
    void ILevelStartListener.OnLevelReady()
    {
        CheckAll();
    }
    public static bool AddStructure(StructureDrop structure, out SavedStructure newStructure)
    {
        Singleton.AssertLoaded<StructureSaverOld, SavedStructure>();
        uint id = structure.instanceID;

        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == structure.asset.GUID)
            {
                newStructure = Singleton[i];
                return false;
            }
        }

        StructureData data = structure.GetServersideData();
        newStructure = new SavedStructure()
        {
            Group = data.group,
            Owner = data.owner,
            InstanceID = data.instanceID,
            ItemGuid = structure.asset.GUID,
            Position = structure.model.position,
            Rotation = structure.model.rotation.eulerAngles
        };
        Singleton.Add(newStructure);
        Singleton.Save();
        return true;
    }
    public static bool AddBarricade(BarricadeDrop barricade, out SavedStructure newStructure)
    {
        Singleton.AssertLoaded<StructureSaverOld, SavedStructure>();
        uint id = barricade.instanceID;

        for (int i = 0; i < Singleton.Count; ++i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == barricade.asset.GUID)
            {
                newStructure = Singleton[i];
                return false;
            }
        }

        BarricadeData data = barricade.GetServersideData();
        newStructure = new SavedStructure()
        {
            Group = data.group,
            Owner = data.owner,
            InstanceID = data.instanceID,
            ItemGuid = barricade.asset.GUID,
            Position = barricade.model.position,
            Rotation = barricade.model.rotation.eulerAngles
        };
        newStructure.Metadata = GetBarricadeState(barricade, barricade.asset, newStructure);
        Singleton.Add(newStructure);
        Singleton.Save();
        return true;
    }
    public static bool RemoveSave(SavedStructure save)
    {
        Singleton.AssertLoaded<StructureSaverOld, SavedStructure>();
        uint id = save.InstanceID;
        bool rem = false;
        for (int i = Singleton.Count - 1; i >= 0; --i)
        {
            if (Singleton[i].InstanceID == id && Singleton[i].ItemGuid == save.ItemGuid)
            {
                Singleton.RemoveAt(i);
                rem = true;
            }
        }

        if (rem) Singleton.Save();
        return rem;
    }
    public static bool SaveExists(StructureDrop structure, out SavedStructure found) => SaveExists(structure.instanceID, EStructType.STRUCTURE, out found);
    public static bool SaveExists(BarricadeDrop barricade, out SavedStructure found) => SaveExists(barricade.instanceID, EStructType.BARRICADE, out found);
    public static bool SaveExists(uint instanceId, EStructType type, out SavedStructure found)
    {
        Singleton.AssertLoaded<StructureSaverOld, SavedStructure>();
        if (type == EStructType.STRUCTURE || type == EStructType.BARRICADE)
        {
            for (int i = 0; i < Singleton.Count; ++i)
            {
                if (Singleton[i].InstanceID == instanceId)
                {
                    if (Assets.find(Singleton[i].ItemGuid) is ItemAsset a1 && (
                        type == EStructType.STRUCTURE && a1 is ItemStructureAsset ||
                        type == EStructType.BARRICADE && a1 is ItemBarricadeAsset))
                    {
                        found = Singleton[i];
                        return true;
                    }
                }
            }
        }

        found = null!;
        return false;
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<StructureSaverOld, SavedStructure>();
        Singleton.Save();
    }
    internal void CheckAll()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = Count - 1; i >= 0; --i)
        {
            if (!Check(this[i]))
                RemoveAt(i);
        }

        Save();
    }
    internal bool Check(SavedStructure structure)
    {
        try
        {
            if (Assets.find(structure.ItemGuid) is not ItemAsset asset)
            {
                ReportError(structure, "GUID is not a valid asset.");
                return false;
            }

            uint id = structure.InstanceID;
            Vector3 pos = structure.Position;
            Vector3 rot = structure.Rotation;
            if (asset is ItemStructureAsset structureAsset)
            {
                StructureDrop? drop = GetStructureDrop(id, pos);
                if (drop is not null)
                {
                    Vector3 ea = drop.model.rotation.eulerAngles;
                    if (drop.model.position != pos || rot != ea)
                    {
                        structure.Position = drop.model.position;
                        structure.Rotation = ea;
                    }
                }
                else
                {
                    drop = GetStructureDrop(pos, rot);
                    if (drop is null)
                    {
                        if (Regions.tryGetCoordinate(pos, out byte x, out byte y) &&
                            StructureManager.dropReplicatedStructure(
                                new Structure(structureAsset, structureAsset.health), pos,
                                Quaternion.Euler(rot), structure.Owner, structure.Group))
                        {
                            L.LogDebug("[STRUCTURE SAVER] Spawned new structure: " + structureAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                            List<StructureDrop> d = StructureManager.regions[x, y].drops;
                            drop = d[d.Count - 1];
                            if (drop.asset.GUID != structureAsset.GUID)
                            {
                                ReportError(structure, "Unable to spawn structure.");
                                return false;
                            }
                            structure.Position = drop.model.position;
                            structure.Rotation = drop.model.rotation.eulerAngles;
                            structure.InstanceID = drop.instanceID;
                            return true;
                        }
                        else
                        {
                            ReportError(structure, "Unable to spawn structure.");
                            return false;
                        }
                    }
                    L.LogDebug("[STRUCTURE SAVER] Identified alternate structure: " + structureAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                    structure.Position = drop.model.position;
                    structure.Rotation = drop.model.rotation.eulerAngles;
                    structure.InstanceID = drop.instanceID;
                }
                if (drop.model.TryGetComponent(out Interactable2HP hp))
                {
                    hp.hp = 100;
                }

                StructureManager.changeOwnerAndGroup(drop.model, structure.Owner, structure.Group);
                return true;
            }
            else if (asset is ItemBarricadeAsset barricadeAsset)
            {
                BarricadeDrop? drop = GetBarricadeDrop(id, pos);
                if (drop is not null)
                {
                    if (drop.model.parent != null && drop.model.parent.CompareTag("Vehicle"))
                    {
                        ReportError(structure, "Unable to load a planted barricade.");
                        return false;
                    }
                    Vector3 ea = drop.model.rotation.eulerAngles;
                    if (drop.model.position != pos || rot != ea)
                    {
                        structure.Position = drop.model.position;
                        structure.Rotation = ea;
                    }
                }
                else
                {
                    drop = GetBarricadeDrop(pos, rot);
                    if (drop is null)
                    {
                        Transform t = BarricadeManager.dropNonPlantedBarricade(
                            new Barricade(barricadeAsset, barricadeAsset.health, GetBarricadeState(null, barricadeAsset, structure)), pos,
                            Quaternion.Euler(rot), structure.Owner, structure.Group);
                        drop = BarricadeManager.FindBarricadeByRootTransform(t);
                        if (drop is null)
                        {
                            ReportError(structure, "Unable to spawn barricade.");
                            return false;
                        }
                        L.LogDebug("[STRUCTURE SAVER] Spawned new barricade: " + barricadeAsset.itemName + " at " + pos.ToString("F0", Data.Locale));
                        structure.Position = drop.model.position;
                        structure.Rotation = drop.model.rotation.eulerAngles;
                        structure.InstanceID = drop.instanceID;
                        return true;
                    }
                    L.LogDebug("[STRUCTURE SAVER] Identified alternate barricade: " + barricadeAsset.itemName + " at " + pos.ToString("F0", Data.Locale));

                    structure.Position = drop.model.position;
                    structure.Rotation = drop.model.rotation.eulerAngles;
                    structure.InstanceID = drop.instanceID;
                }

                BarricadeData bd = drop.GetServersideData();
                bd.group = structure.Group;
                bd.owner = structure.Owner;

                if (drop.interactable != null)
                {
                    byte[] dropSt = GetBarricadeState(drop, barricadeAsset, structure);
                    byte[] oldSt = drop.GetServersideData().barricade.state;
                    if (dropSt.Length == oldSt.Length)
                    {
                        for (int i = 0; i < dropSt.Length; ++i)
                            if (dropSt[i] != oldSt[i])
                                goto next;
                    }
                    else goto next;
                    structure.Metadata = oldSt;
                    goto setHp;
                next:
                    L.LogDebug("[STRUCTURE SAVER] " + structure.InstanceID + " State: " + string.Join(" ", oldSt.Select(x => x.ToString("X2"))) +
                           " to " + string.Join(" ", dropSt.Select(x => x.ToString("X2"))) + " at " + pos.ToString("F0", Data.Locale));
                    if (drop.interactable is InteractableSign sign)
                    {
                        BarricadeManager.updateState(drop.model, dropSt, dropSt.Length);
                        sign.updateState(barricadeAsset, dropSt);
                        Signs.BroadcastSignUpdate(sign);
                    }
                    else
                        BarricadeManager.updateReplicatedState(drop.model, dropSt, dropSt.Length);
                    structure.Metadata = dropSt;
                }
            setHp:
                if (drop.model.TryGetComponent(out Interactable2HP hp))
                {
                    hp.hp = 100;
                }
                BarricadeManager.changeOwnerAndGroup(drop.model, structure.Owner, structure.Group);
                return true;
            }
            else return false;
        }
        catch (Exception ex)
        {
            L.LogError("Error checking structure save: ");
            L.LogError(ex);
            return false;
        }
    }
    private static void ReportError(SavedStructure structure, string reason)
    {
        L.LogWarning("Error loading " + structure.ToString() + ":", method: "STRUCTURE SAVER");
        L.LogWarning(reason, method: "STRUCTURE SAVER");
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
    private static StructureDrop? GetStructureDrop(uint instanceId, Vector3 expectedPosition)
    {
        if (Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
        {
            List<StructureDrop> d = StructureManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].instanceID == instanceId)
                    return d[i];
            }
        }
        for (x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                List<StructureDrop> d = StructureManager.regions[x, y].drops;
                for (int i = 0; i < d.Count; ++i)
                {
                    if (d[i].instanceID == instanceId)
                        return d[i];
                }
            }
        }

        return null;
    }
    private static StructureDrop? GetStructureDrop(Vector3 position, Vector3 euler)
    {
        if (Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            List<StructureDrop> d = StructureManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].model.position == position && d[i].model.rotation.eulerAngles == euler)
                    return d[i];
            }
        }

        return null;
    }
    private static BarricadeDrop? GetBarricadeDrop(uint instanceId, Vector3 expectedPosition, bool vehicle = false)
    {
        if (!vehicle)
        {
            if (Regions.tryGetCoordinate(expectedPosition, out byte x, out byte y))
            {
                List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
                for (int i = 0; i < d.Count; ++i)
                {
                    if (d[i].instanceID == instanceId)
                        return d[i];
                }
            }
            for (x = 0; x < Regions.WORLD_SIZE; ++x)
            {
                for (y = 0; y < Regions.WORLD_SIZE; ++y)
                {
                    List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
                    for (int i = 0; i < d.Count; ++i)
                    {
                        if (d[i].instanceID == instanceId)
                            return d[i];
                    }
                }
            }
        }

        for (int v = 0; v < BarricadeManager.vehicleRegions.Count; ++v)
        {
            List<BarricadeDrop> d = BarricadeManager.vehicleRegions[v].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].instanceID == instanceId)
                    return d[i];
            }
        }

        return null;
    }
    private static BarricadeDrop? GetBarricadeDrop(Vector3 position, Vector3 euler)
    {
        if (Regions.tryGetCoordinate(position, out byte x, out byte y))
        {
            List<BarricadeDrop> d = BarricadeManager.regions[x, y].drops;
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i].model.position == position && d[i].model.rotation.eulerAngles == euler)
                    return d[i];
            }
        }

        return null;
    }
}

public enum EStructType : byte
{
    UNKNOWN = 0,
    STRUCTURE = 1,
    BARRICADE = 2
}