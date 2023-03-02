//#define IMPORT
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if IMPORT
using System.IO;
using System.Text.Json;
using Uncreated.Json;
#endif
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Sync;
using UnityEngine;
using UnityEngine.Assertions;

namespace Uncreated.Warfare.Structures;

[Sync((int)WarfareSyncTypes.StructureSaver)]
[SingletonDependency(typeof(Level))]
[SingletonDependency(typeof(MapScheduler))]
public sealed class StructureSaver : ListSqlSingleton<SavedStructure>, ILevelStartListenerAsync
{
    private static readonly MethodInfo? OnStateUpdatedInteractableStorageMethod =
        typeof(InteractableStorage).GetMethod("onStateUpdated", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo? UpdateDisplayMethod =
        typeof(InteractableStorage).GetMethod("updateDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
#if IMPORT
    private bool _hasImported;
#endif

    public static StructureSaver? GetSingletonQuick() => Data.Is(out IStructureSaving r) ? r.StructureSaver : null;

    [OperationTest("Display State Methods Check")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestDisplayStateMethods()
    {
        Assert.IsNotNull(OnStateUpdatedInteractableStorageMethod);
        Assert.IsNotNull(UpdateDisplayMethod);
    }

    public override bool AwaitLoad => true;
    public override MySqlDatabase Sql => Data.AdminSql;
    public StructureSaver() : base("structures", SCHEMAS) { }
    async Task ILevelStartListenerAsync.OnLevelReady(CancellationToken token)
    {
#if IMPORT
        if (!_hasImported)
        {
            await ImportFromJson(Path.Combine(Data.Paths.StructureStorage, "structures.json"), true, (x, y) => x.ItemGuid == y.ItemGuid ? x.InstanceID.CompareTo(y.InstanceID) : x.ItemGuid.CompareTo(y.ItemGuid), token, deserializer:
                (ref Utf8JsonReader reader) =>
                {
                    SavedStructure save = new SavedStructure();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string val = reader.GetString()!;
                            if (reader.Read())
                            {
                                switch (val)
                                {
                                    case "position":
                                        Vector3 pos = JsonSerializer.Deserialize<Vector3>(ref reader, JsonEx.serializerSettings);
                                        save.Position = pos;
                                        break;
                                    case "rotation":
                                        Vector3 rot = JsonSerializer.Deserialize<Vector3>(ref reader, JsonEx.serializerSettings);
                                        save.Rotation = rot;
                                        break;
                                    case "state":
                                        save.StateString = reader.GetString()!;
                                        break;
                                    case "instance_id":
                                        save.InstanceID = reader.GetUInt32();
                                        break;
                                    case "owner":
                                        save.Owner = reader.GetUInt64();
                                        break;
                                    case "group":
                                        save.Group = reader.GetUInt64();
                                        break;
                                    case "guid":
                                        save.ItemGuid = reader.GetGuid();
                                        break;
                                }
                            }
                        }
                        else if (reader.TokenType == JsonTokenType.EndObject)
                            break;
                    }
                    if (Assets.find(save.ItemGuid) is ItemStorageAsset storage)
                    {
                        save.Items = F.GetItemsFromStorageState(storage, save.Metadata, out ItemDisplayData? displayData, save.PrimaryKey);
                        save.DisplayData = displayData;
                    }

                    return save;
                }).ConfigureAwait(false);
            Guid kitSign = new Guid("275dd81d60ae443e91f0655b8b7aa920");
            await ImportFromJson(Path.Combine(Data.Paths.StructureStorage, "request_signs.json"), true, (x, y) => x.ItemGuid == y.ItemGuid ? x.InstanceID.CompareTo(y.InstanceID) : x.ItemGuid.CompareTo(y.ItemGuid), token, deserializer:
                (ref Utf8JsonReader reader) =>
                {
                    SavedStructure save = new SavedStructure
                    {
                        Group = 3,
                        Owner = 76561198267927009ul,
                        ItemGuid = kitSign
                    };
                    string? kitname = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string val = reader.GetString()!;
                            if (reader.Read())
                            {
                                switch (val)
                                {
                                    case "position":
                                        Vector3 pos =
                                            JsonSerializer.Deserialize<Vector3>(ref reader, JsonEx.serializerSettings);
                                        save.Position = pos;
                                        break;
                                    case "rotation":
                                        Vector3 rot =
                                            JsonSerializer.Deserialize<Vector3>(ref reader, JsonEx.serializerSettings);
                                        save.Rotation = rot;
                                        break;
                                    case "kit_name":
                                        kitname = reader.GetString();
                                        break;
                                    case "instance_id":
                                        save.InstanceID = reader.GetUInt32();
                                        break;
                                }
                            }
                        }
                        else if (reader.TokenType == JsonTokenType.EndObject)
                            break;
                    }

                    if (kitname != null)
                    {
                        byte[] st = System.Text.Encoding.UTF8.GetBytes("sign_kit_" + kitname);
                        save.Metadata = new byte[st.Length + 17];
                        Buffer.BlockCopy(st, 0, save.Metadata, 17, st.Length);
                        save.Metadata[16] = (byte)st.Length;
                        Buffer.BlockCopy(BitConverter.GetBytes(76561198267927009ul), 0, save.Metadata, 0, sizeof(ulong));
                        Buffer.BlockCopy(BitConverter.GetBytes(3ul), 0, save.Metadata, sizeof(ulong), sizeof(ulong));
                    }


                    return save;
                }).ConfigureAwait(false);
        }
        _hasImported = true;
#endif
        L.Log("Checking structures: " + Items.Count + " item(s) pending...", ConsoleColor.Magenta);
        await WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<SavedStructure>? toSave = null;
            await UCWarfare.ToUpdate(token);
            WriteWait();
            try
            {
                List<uint> bmatched = new List<uint>(Items.Count);
                List<uint> smatched = new List<uint>(Items.Count / 10);
                for (int i = 0; i < Items.Count; ++i)
                {
                    SqlItem<SavedStructure> item = Items[i];
                    SavedStructure? structure = item.Item;
                    if (structure != null)
                    {
                        if (Assets.find(structure.ItemGuid) is not ItemAsset itemAsset)
                        {
                            ReportError(structure, "Item not found: " + structure.ItemGuid);
                            continue;
                        }
                        if (itemAsset is ItemStructureAsset)
                            goto structure;
                        if (itemAsset is not ItemBarricadeAsset brAsset)
                        {
                            ReportError(structure, "Item " + itemAsset.itemName + " (" + structure.ItemGuid + ") is not a barricade or structure.");
                            continue;
                        }

                        if (bmatched.Contains(structure.InstanceID))
                        {
                            ReportInfo(structure, "Barricade's instance ID already used: " + structure.InstanceID + ".");
                            structure.InstanceID = 0;
                        }
                        bredo:
                        BarricadeDrop? bdrop = structure.InstanceID > 0 ? UCBarricadeManager.FindBarricade(structure.InstanceID, structure.Position) : null;
                        BarricadeData bdata;
                        if (bdrop == null || bdrop.asset.GUID != structure.ItemGuid) // barricade of that instance id not found
                        {
                            bdrop = UCBarricadeManager.GetBarricadeFromPosition(structure.ItemGuid, structure.Position, 0.1f);
                            if (bdrop == null) // barricade at that position not found
                            {
                                if (Replace(structure, brAsset))
                                {
                                    bmatched.Add(structure.InstanceID);
                                    ReportInfo(structure, "Barricade not found via instance ID or position, replaced successfully.");
                                    goto save;
                                }
                                ReportError(structure, "Barricade not found via instance ID or position, unable to replace.");
                                continue;
                            }
                            structure.InstanceID = bdrop.instanceID;
                            ReportInfo(structure, "Barricade not found via instance ID, found with position, instance ID replaced successfully.");
                            bmatched.Add(structure.InstanceID);
                            structure.Position = bdrop.model.position;
                            structure.Rotation = bdrop.model.rotation.eulerAngles;
                            structure.Buildable = new UCBarricade(bdrop);
                            structure.Metadata = GetBarricadeState(null, brAsset, structure);
                            FillHp(bdrop);
                            bdata = bdrop.GetServersideData();
                            if (bdata.barricade.asset is not ItemStorageAsset && !structure.Metadata.CompareBytes(bdata.barricade.state))
                            {
                                bdata.barricade.state = Util.CloneBytes(structure.Metadata);
                                F.SetOwnerOrGroup(bdrop, structure.Owner, structure.Group);
                                ReportInfo(structure, "Found barricade metadata was incorrect, successfully corrected to saved values.");
                            }
                            else if (bdata.owner != structure.Owner || bdata.group != structure.Group)
                            {
                                F.SetOwnerOrGroup(bdrop, structure.Owner, structure.Group);
                                ReportInfo(structure, "Found barricade owner or group was incorrect, successfully corrected to saved values.");
                            }
                            goto save;
                        }
                        // barricade found from instance id, update positions if needed.
                        if (!structure.Position.AlmostEquals(bdrop.model.position, 0.1f) || !structure.Rotation.AlmostEquals(bdrop.model.rotation.eulerAngles, 3f))
                        {
                            for (int j = i + 1; j < Items.Count; ++j)
                            {
                                SavedStructure? st = Items[j].Item;
                                if (st != null &&
                                    st.ItemGuid == structure.ItemGuid &&
                                    st.Position.AlmostEquals(bdrop.model.position, 0.1f) &&
                                    st.Rotation.AlmostEquals(bdrop.model.rotation.eulerAngles, 3f))
                                {
                                    structure.InstanceID = 0;
                                    ReportInfo(structure, "Found another matching barricade instance later on.");
                                    goto bredo;
                                }
                            }
                            BarricadeManager.ServerSetBarricadeTransform(bdrop.model, structure.Position, Quaternion.Euler(structure.Rotation));
                            ReportInfo(structure, "Barricade position or rotation was incorrect, successfully corrected to saved values.");
                        }
                        bmatched.Add(structure.InstanceID);
                        FillHp(bdrop);
                        bdata = bdrop.GetServersideData();
                        if (!structure.Metadata.CompareBytes(bdata.barricade.state))
                        {
                            ReportInfo(structure, "Barricade metadata was incorrect, successfully corrected to saved values.");
                            bdata.barricade.state = Util.CloneBytes(structure.Metadata);
                            F.SetOwnerOrGroup(bdrop, structure.Owner, structure.Group);
                        }
                        else if (bdata.owner != structure.Owner || bdata.group != structure.Group)
                        {
                            F.SetOwnerOrGroup(bdrop, structure.Owner, structure.Group);
                            ReportInfo(structure, "Barricade owner or group was incorrect, successfully corrected to saved values.");
                        }
                        structure.Buildable = new UCBarricade(bdrop);
                        continue;
                        structure:
                        ItemStructureAsset stAsset = (ItemStructureAsset)itemAsset;
                        if (smatched.Contains(structure.InstanceID))
                        {
                            ReportInfo(structure, "Structure's instance ID already used: " + structure.InstanceID + ".");
                            structure.InstanceID = 0;
                        }
                        sredo:
                        StructureDrop? sdrop = structure.InstanceID != 0 ? UCBarricadeManager.FindStructure(structure.InstanceID, structure.Position) : null;
                        StructureData sdata;
                        if (sdrop == null || sdrop.asset.GUID != structure.ItemGuid)  // structure of that instance id not found
                        {
                            sdrop = UCBarricadeManager.GetStructureFromPosition(structure.ItemGuid, structure.Position, 0.1f);
                            if (sdrop == null) // structure at that position not found
                            {
                                if (Replace(structure, stAsset))
                                {
                                    smatched.Add(structure.InstanceID);
                                    ReportInfo(structure, "Structure not found via instance ID or position, replaced successfully.");
                                    goto save;
                                }
                                ReportError(structure, "Structure not found via instance ID or position, unable to replace.");
                                continue;
                            }
                            structure.InstanceID = sdrop.instanceID;
                            smatched.Add(structure.InstanceID);
                            structure.Position = sdrop.model.position;
                            structure.Rotation = sdrop.model.rotation.eulerAngles;
                            structure.Buildable = new UCStructure(sdrop);
                            FillHp(sdrop);
                            sdata = sdrop.GetServersideData();
                            if (sdata.owner != structure.Owner || sdata.group != structure.Group)
                                StructureManager.changeOwnerAndGroup(sdrop.model, structure.Owner, structure.Group);
                            goto save;
                        }
                        smatched.Add(structure.InstanceID);
                        // structure found from instance id, update positions if needed.
                        if (!structure.Position.AlmostEquals(sdrop.model.position, 0.1f) || !structure.Rotation.AlmostEquals(sdrop.model.rotation.eulerAngles, 3f))
                        {
                            for (int j = i + 1; j < Items.Count; ++j)
                            {
                                SavedStructure? st = Items[j].Item;
                                if (st != null &&
                                    st.ItemGuid == structure.ItemGuid &&
                                    st.Position.AlmostEquals(sdrop.model.position, 0.1f) &&
                                    st.Rotation.AlmostEquals(sdrop.model.rotation.eulerAngles, 3f))
                                {
                                    structure.InstanceID = 0;
                                    ReportInfo(structure, "Found another matching structure instance later on.");
                                    goto sredo;
                                }
                            }
                            StructureManager.ServerSetStructureTransform(sdrop.model, structure.Position, Quaternion.Euler(structure.Rotation));
                            ReportInfo(structure, "Structure position or rotation was incorrect, successfully corrected to saved values.");
                        }
                        FillHp(sdrop);
                        sdata = sdrop.GetServersideData();
                        if (sdata.owner != structure.Owner || sdata.group != structure.Group)
                        {
                            StructureManager.changeOwnerAndGroup(sdrop.model, structure.Owner, structure.Group);
                            ReportInfo(structure, "Barricade owner or group was incorrect, successfully corrected to saved values.");
                        }
                        structure.Buildable = new UCStructure(sdrop);
                        continue;

                        save:
                        (toSave ??= new List<SavedStructure>(4)).Add(structure);
                    }
                }
                BarricadeManager.save();
                StructureManager.save();
            }
            finally
            {
                WriteRelease();
            }
            if (toSave != null)
            {
                L.LogDebug("Resaving " + toSave.Count + " item(s)...");
                await AddOrUpdateNoLock(toSave, token).ConfigureAwait(false);
            }
        }
        finally
        {
            Release();
        }
    }
    private static void ReportError(SavedStructure structure, string message)
    {
        L.LogError("[STR SAVER] Error initializing " + structure + ": \"" + message + "\".");
    }
    private static void ReportInfo(SavedStructure structure, string message)
    {
        L.Log("[STR SAVER]  Info initializing " + structure + ": \"" + message + "\".");
    }
    public override Task PostReload(CancellationToken token)
    {
        return Level.isLoaded ? (this as ILevelStartListenerAsync).OnLevelReady(token) : Task.CompletedTask;
    }
    private static void FillHp(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.asset.health > drop.GetServersideData().barricade.health)
        {
            BarricadeManager.repair(drop.model, drop.asset.health, 1f, Provider.server);
        }
    }
    private static void FillHp(StructureDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        if (drop.asset.health > drop.GetServersideData().structure.health)
        {
            StructureManager.repair(drop.model, drop.asset.health, 1f, Provider.server);
        }
    }
    private static bool Replace(SavedStructure structure, ItemBarricadeAsset asset)
    {
        ThreadUtil.assertIsGameThread();
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

                if (OnStateUpdatedInteractableStorageMethod != null)
                {
                    inv.onStateUpdated = (StateUpdated)Delegate.Combine(inv.onStateUpdated,
                        OnStateUpdatedInteractableStorageMethod.CreateDelegate(typeof(StateUpdated), intx));
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
                UpdateDisplayMethod?.Invoke(intx, Array.Empty<object>());
                intx.refreshDisplay();
                a = true;
            }

            if (a)
            {
                intx.rebuildState();
            }
        }
        structure.Metadata = drop.GetServersideData().barricade.state;
        structure.Position = drop.model.position;
        structure.Rotation = drop.model.rotation.eulerAngles;
        structure.InstanceID = drop.instanceID;

        return true;
    }
    private static bool Replace(SavedStructure structure, ItemStructureAsset asset)
    {
        ThreadUtil.assertIsGameThread();
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
            structure.Position = drop.model.position;
            structure.Rotation = drop.model.rotation.eulerAngles;
            structure.Metadata = Array.Empty<byte>();
            structure.InstanceID = drop.instanceID;
            structure.Buildable = new UCStructure(drop);
            return true;
        }
        return false;
    }
    private static byte[] GetBarricadeState(BarricadeDrop? drop, ItemBarricadeAsset asset, SavedStructure structure)
    {
        ThreadUtil.assertIsGameThread();
        byte[] st2 = drop != null ? drop.GetServersideData().barricade.state : (structure.Metadata is null || structure.Metadata.Length == 0 ? asset.getState(EItemOrigin.ADMIN) : structure.Metadata);
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
        bool status = true;
        SavedStructure structure;
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < this.Items.Count; ++i)
            {
                SqlItem<SavedStructure> str = Items[i];
                if (str.Item != null && str.Item.InstanceID == id && str.Item.ItemGuid == drop.asset.GUID)
                {
                    structure = str.Item;
                    status = false;
                    goto save;
                }
            }
        }
        finally
        {
            WriteRelease();
        }

        await UCWarfare.ToUpdate(token);

        StructureData data = drop.GetServersideData();
        structure = new SavedStructure()
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
        save:
        return (await AddOrUpdate(structure, token).ConfigureAwait(false), status);
    }
    public async Task<(SqlItem<SavedStructure>, bool)> AddBarricade(BarricadeDrop drop, CancellationToken token = default)
    {
        if (drop == null || drop.model == null)
            throw new ArgumentNullException(nameof(drop));
        this.AssertLoadedIntl();

        uint id = drop.instanceID;
        bool status = true;
        SavedStructure structure;
        await WriteWaitAsync(token).ConfigureAwait(false);
        try
        {
            for (int i = 0; i < Items.Count; ++i)
            {
                SqlItem<SavedStructure> str = Items[i];
                if (str.Item != null && str.Item.InstanceID == id && str.Item.ItemGuid == drop.asset.GUID)
                {
                    structure = str.Item;
                    status = false;
                    goto save;
                }
            }
        }
        finally
        {
            WriteRelease();
        }

        await UCWarfare.ToUpdate(token);

        BarricadeData data = drop.GetServersideData();
        structure = new SavedStructure()
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
        {
            structure.Metadata = Util.CloneBytes(data.barricade.state);
        }
        save:
        if (drop.interactable is InteractableSign sign && sign.text.StartsWith(Signs.Prefix + Signs.KitPrefix, StringComparison.OrdinalIgnoreCase))
        {
            KitManager? manager = KitManager.GetSingletonQuick();
            if (manager != null)
            {
                SqlItem<Kit>? proxy = await manager.FindKit(sign.text.Substring(Signs.Prefix.Length + Signs.KitPrefix.Length), token).ConfigureAwait(false);
                if (proxy is { Item: { } kit })
                {
                    await proxy.Enter(token);
                    try
                    {
                        PrimaryKey[] keys = kit.RequestSigns;
                        for (int i = 0; i < keys.Length; ++i)
                        {
                            if (keys[i].Key == structure.PrimaryKey.Key)
                                goto save2;
                        }

                        Util.AddToArray(ref keys!, structure.PrimaryKey);
                        kit.RequestSigns = keys;
                    }
                    finally
                    {
                        proxy.Release();
                    }
                }
            }
        }
        save2:
        return (await AddOrUpdate(structure, token).ConfigureAwait(false), status);
    }
    public Task RemoveItem(SavedStructure structure, CancellationToken token = default)
    {
        return Delete(structure, token);
    }
    public bool TryGetSaveNoLock(StructureDrop structure, out SavedStructure value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool TryGetSaveNoLock(BarricadeDrop barricade, out SavedStructure value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool TryGetSaveNoLock(uint id, StructType type, out SavedStructure value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool TryGetSaveNoLock(StructureDrop structure, out SqlItem<SavedStructure> value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool TryGetSaveNoLock(BarricadeDrop barricade, out SqlItem<SavedStructure> value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public bool TryGetSaveNoLock(uint id, StructType type, out SqlItem<SavedStructure> value)
    {
        WriteWait();
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

            value = null!;
            return false;
        }
        finally
        {
            WriteRelease();
        }
    }
    public async Task<SavedStructure?> GetSave(StructureDrop structure, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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

            return null;
        }
        finally
        {
            WriteRelease();
        }
    }
    public async Task<SavedStructure?> GetSave(BarricadeDrop barricade, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public async Task<SavedStructure?> GetSave(uint id, StructType type, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(StructureDrop structure, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(BarricadeDrop barricade, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public async Task<SqlItem<SavedStructure>?> GetSaveItem(uint id, StructType type, CancellationToken token = default)
    {
        await WriteWaitAsync(token).ConfigureAwait(false);
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
            WriteRelease();
        }

        return null;
    }
    public SqlItem<SavedStructure>? GetSaveItemSync(uint instanceId, StructType type)
    {
        WriteWait();
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
            return null;
        }
        finally
        {
            WriteRelease();
        }
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

    // ReSharper disable InconsistentNaming
    #region Sql
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
            new Schema.Column(COLUMN_GUID,     SqlTypes.GUID_STRING),
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
            new Schema.Column(COLUMN_ITEM_GUID,     SqlTypes.GUID_STRING),
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
    // ReSharper restore InconsistentNaming
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
            object[] p = new object[hasPk ? 13 : 12];
            p[0] = ServerRegion.Key;
            p[1] = MapScheduler.Current;
            p[2] = item.ItemGuid.ToString("N");
            p[3] = item.Position.x;
            p[4] = item.Position.y;
            p[5] = item.Position.z;
            p[6] = item.Rotation.x;
            p[7] = item.Rotation.y;
            p[8] = item.Rotation.z;
            p[9] = item.Owner;
            p[10] = item.Group;
            if (storage != null && item.Metadata != null)
            {
                byte[] newState = new byte[sizeof(ulong) * 2];
                Buffer.BlockCopy(item.Metadata, 0, newState, 0, Math.Min(item.Metadata.Length, newState.Length));
                p[11] = newState;
            }
            else p[11] = item.Metadata ?? Array.Empty<byte>();

            if (hasPk)
                p[12] = item.PrimaryKey.Key;

            int pk2 = PrimaryKey.NotAssigned;
            await Sql.QueryAsync($"INSERT INTO `{TABLE_MAIN}` ({SqlTypes.ColumnList(COLUMN_MAP, COLUMN_GUID,
                           COLUMN_POS_X, COLUMN_POS_Y, COLUMN_POS_Z,
                           COLUMN_ROT_X, COLUMN_ROT_Y, COLUMN_ROT_Z,
                           COLUMN_OWNER, COLUMN_GROUP, COLUMN_METADATA)}"
                           + (hasPk ? $",`{COLUMN_PK}`" : string.Empty) +
                           ") VALUES (" +
                           "@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11" + (hasPk ? ",LAST_INSERT_ID(@12)" : string.Empty) +
                           ") ON DUPLICATE KEY UPDATE " +
                           $"{SqlTypes.ColumnUpdateList(1, COLUMN_MAP, COLUMN_GUID, COLUMN_POS_X, COLUMN_POS_Y, COLUMN_POS_Z,
                               COLUMN_ROT_X, COLUMN_ROT_Y, COLUMN_ROT_Z, COLUMN_OWNER, COLUMN_GROUP, COLUMN_METADATA)}," +
                           $"`{COLUMN_PK}`=LAST_INSERT_ID(`{COLUMN_PK}`); " +
                           "SET @pk := (SELECT LAST_INSERT_ID() as `pk`); " +
                           $"DELETE FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@pk; " +
                           $"DELETE FROM `{TABLE_INSTANCES}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@pk AND `{COLUMN_INSTANCES_REGION_KEY}`=@0; " +
                           "SELECT @pk;",
            p, reader =>
            {
                pk2 = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
            PrimaryKey structKey = pk2;
            item.PrimaryKey = structKey;
            if (item.InstanceID > 0 && item.InstanceID != uint.MaxValue)
            {
                await Sql.NonQueryAsync($"INSERT INTO `{TABLE_INSTANCES}` " +
                                        $"({SqlTypes.ColumnList(COLUMN_INSTANCES_REGION_KEY, COLUMN_ITEM_STRUCTURE_PK, COLUMN_INSTANCES_INSTANCE_ID)})" +
                                         " VALUES (@0,@1,@2);",
                        new object[] { ServerRegion.Key, pk2, item.InstanceID }, token)
                    .ConfigureAwait(false);
            }
            if (storage != null && item.Metadata != null)
            {
                ItemJarData[] data = item.Items ?? Array.Empty<ItemJarData>();
                if (data.Length > 0)
                {
                    StringBuilder builder = new StringBuilder($"INSERT INTO `{TABLE_STRUCTURE_ITEMS}` ({SqlTypes.ColumnList(
                                                              COLUMN_ITEM_STRUCTURE_PK, COLUMN_ITEM_GUID,
                                                              COLUMN_ITEM_AMOUNT, COLUMN_ITEM_QUALITY, COLUMN_ITEM_POS_X, COLUMN_ITEM_POS_Y,
                                                              COLUMN_ITEM_ROT, COLUMN_ITEM_METADATA)}) VALUES ", 256);
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

                        objs[ind] = pk2;
                        objs[ind + 1] = dataPt.Item.ToString("N");
                        objs[ind + 2] = dataPt.Amount;
                        objs[ind + 3] = dataPt.Quality;
                        objs[ind + 4] = dataPt.X;
                        objs[ind + 5] = dataPt.Y;
                        objs[ind + 6] = dataPt.Rotation;
                        objs[ind + 7] = dataPt.Metadata ?? Array.Empty<byte>();
                    }
                    builder.Append(';');
                    await Sql.NonQueryAsync(builder.ToString(), objs, token).ConfigureAwait(false);
                }
                if (storage.isDisplay && item.DisplayData.HasValue)
                {
                    ItemDisplayData disp = item.DisplayData.Value;
                    await Sql.NonQueryAsync($"INSERT INTO `{TABLE_DISPLAY_DATA}` ({SqlTypes.ColumnList(COLUMN_PK, COLUMN_DISPLAY_SKIN, COLUMN_DISPLAY_MYTHIC, COLUMN_DISPLAY_ROT,
                                            COLUMN_DISPLAY_TAGS, COLUMN_DISPLAY_DYNAMIC_PROPS)}) VALUES (@0, @1, @2, @3, @4, @5) ON DUPLICATE KEY UPDATE " +
                                            $"{SqlTypes.ColumnUpdateList(1, COLUMN_DISPLAY_SKIN, COLUMN_DISPLAY_MYTHIC, COLUMN_DISPLAY_ROT,
                                            COLUMN_DISPLAY_TAGS, COLUMN_DISPLAY_DYNAMIC_PROPS)};",
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
            await Sql.NonQueryAsync($"DELETE FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0;",
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
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_PK, COLUMN_GUID,
                             COLUMN_POS_X, COLUMN_POS_Y, COLUMN_POS_Z,
                             COLUMN_ROT_X, COLUMN_ROT_Y, COLUMN_ROT_Z,
                             COLUMN_OWNER, COLUMN_GROUP, COLUMN_METADATA)} FROM `{TABLE_MAIN}` " +
                             $"WHERE `{COLUMN_MAP}`=@0;", new object[] { MapScheduler.Current },
            reader =>
            {
                int pk = reader.GetInt32(0);
                Guid? guid = reader.ReadGuidString(1);
                if (!guid.HasValue)
                    L.LogWarning("Invalid item GUID at structure " + pk + ": \"" + reader.GetString(1) + "\".");
                else str.Add(new SavedStructure
                    {
                        PrimaryKey = pk,
                        ItemGuid = guid.Value,
                        Position = new Vector3(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4)),
                        Rotation = new Vector3(reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7)),
                        Owner = reader.GetUInt64(8),
                        Group = reader.GetUInt64(9),
                        Metadata = reader.ReadByteArray(10),
                        InstanceID = uint.MaxValue
                    });
            }, token).ConfigureAwait(false);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_ITEM_STRUCTURE_PK, COLUMN_INSTANCES_INSTANCE_ID)} FROM " +
                             $"`{TABLE_INSTANCES}` WHERE `{COLUMN_INSTANCES_REGION_KEY}`=@0;",
            new object[] { ServerRegion.Key }, reader =>
            {
                int pk = reader.GetInt32(0);
                for (int i = 0; i < str.Count; ++i)
                {
                    if (str[i].PrimaryKey.Key == pk)
                    {
                        str[i].InstanceID = reader.GetUInt32(1);
                        return;
                    }
                }
            }, token).ConfigureAwait(false);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_PK, COLUMN_DISPLAY_SKIN, COLUMN_DISPLAY_MYTHIC,
                             COLUMN_DISPLAY_ROT, COLUMN_DISPLAY_DYNAMIC_PROPS, COLUMN_DISPLAY_TAGS)} FROM `{TABLE_DISPLAY_DATA}`;", null, reader =>
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
         }, token).ConfigureAwait(false);
        List<ItemJarData>? itemData = null;
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_ITEM_STRUCTURE_PK, COLUMN_ITEM_GUID,
                             COLUMN_ITEM_AMOUNT, COLUMN_ITEM_QUALITY, COLUMN_ITEM_POS_X,
                             COLUMN_ITEM_POS_Y, COLUMN_ITEM_ROT, COLUMN_ITEM_METADATA)} FROM `{TABLE_STRUCTURE_ITEMS}`;",
            null, reader =>
            {
                Guid? guid = reader.ReadGuidString(1);
                if (!guid.HasValue)
                    L.LogWarning("Invalid item GUID at structure " + reader.GetInt32(0) + ": \"" + reader.GetString(1) + "\".");
                else
                    (itemData ??= new List<ItemJarData>(64)).Add(new ItemJarData(PrimaryKey.NotAssigned, reader.GetInt32(0),
                        guid.Value, reader.GetByte(4), reader.GetByte(5), reader.GetByte(6),
                        reader.GetByte(2), reader.GetByte(3), reader.ReadByteArray(7)));
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
        object[] arr = { pk.Key };
        SavedStructure? str = null;
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_PK, COLUMN_GUID,
                             COLUMN_POS_X, COLUMN_POS_Y, COLUMN_POS_Z,
                             COLUMN_ROT_X, COLUMN_ROT_Y, COLUMN_ROT_Z,
                             COLUMN_OWNER, COLUMN_GROUP, COLUMN_METADATA)} " +
                             $"FROM `{TABLE_MAIN}` WHERE `{COLUMN_PK}`=@0 LIMIT 1;", arr,
            reader =>
            {
                int lpk = reader.GetInt32(0);
                Guid? guid = reader.ReadGuidString(1);
                if (!guid.HasValue)
                    L.LogWarning("Invalid item GUID at structure " + lpk + ": \"" + reader.GetString(1) + "\".");
                else
                {
                    str = new SavedStructure
                    {
                        PrimaryKey = lpk,
                        ItemGuid = guid.Value,
                        Position = new Vector3(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4)),
                        Rotation = new Vector3(reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7)),
                        Owner = reader.GetUInt64(8),
                        Group = reader.GetUInt64(9),
                        Metadata = reader.ReadByteArray(10)
                    };
                }
                return true;
            }, token).ConfigureAwait(false);
        if (str == null)
            return null;
        if (ServerRegion.Key != byte.MaxValue)
        {
            await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_INSTANCES_INSTANCE_ID)} FROM " +
                                 $"`{TABLE_INSTANCES}` WHERE `{COLUMN_INSTANCES_REGION_KEY}`=@0 " +
                                 $"AND `{COLUMN_ITEM_STRUCTURE_PK}=@1 LIMIT 1;",
                new object[] { ServerRegion.Key, arr[0] }, reader =>
                {
                    uint id = reader.GetUInt32(0);
                    str.InstanceID = id;
                }, token).ConfigureAwait(false);
        }
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_DISPLAY_SKIN, COLUMN_DISPLAY_MYTHIC,
                             COLUMN_DISPLAY_ROT, COLUMN_DISPLAY_DYNAMIC_PROPS, COLUMN_DISPLAY_TAGS)}" +
                             $" FROM `{TABLE_DISPLAY_DATA}` WHERE `{COLUMN_PK}`=@0 LIMIT 1;", arr, reader =>
                             {
                                 str.DisplayData = new ItemDisplayData(
                                     pk, reader.GetUInt16(0), reader.GetUInt16(1),
                                     reader.GetByte(2), reader.IsDBNull(4) ? null : reader.GetString(4),
                                     reader.IsDBNull(3) ? null : reader.GetString(3));
                                 return true;
                             }, token).ConfigureAwait(false);
        List<ItemJarData>? itemData = null;
        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(COLUMN_ITEM_PK, COLUMN_ITEM_GUID,
                             COLUMN_ITEM_AMOUNT, COLUMN_ITEM_QUALITY, COLUMN_ITEM_POS_X,
                             COLUMN_ITEM_POS_Y, COLUMN_ITEM_ROT, COLUMN_ITEM_METADATA)} " +
                             $"FROM `{TABLE_STRUCTURE_ITEMS}` WHERE `{COLUMN_ITEM_STRUCTURE_PK}`=@0;",
            arr, reader =>
            {
                int lpk = reader.GetInt32(0);
                Guid? guid = reader.ReadGuidString(1);
                if (!guid.HasValue)
                    L.LogWarning("Invalid item GUID at structure " + lpk + ": \"" + pk + "\".");
                else
                    (itemData ??= new List<ItemJarData>(8)).Add(new ItemJarData(lpk, pk,
                        guid.Value, reader.GetByte(4), reader.GetByte(5), reader.GetByte(6),
                        reader.GetByte(2), reader.GetByte(3), reader.ReadByteArray(7)));
            }, token).ConfigureAwait(false);
        if (itemData != null)
        {
            ItemJarData[] d = itemData.ToArray();
            if (d.Length > 0)
                str.Items = d;
        }

        return str;
    }
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
    public StructType Type => StructType.Barricade;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null ? null! : Drop.model; // so you can use ? on it
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
    public StructType Type => StructType.Structure;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null ? null! : Drop.model; // so you can use ? on it
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
    StructType Type { get; }
    ItemAsset Asset { get; }
    Transform Model { get; }
    ulong Owner { get; }
    ulong Group { get; }
    object Drop { get; }
    object Data { get; }
    NetId NetId { get; }
}
public enum StructType : byte
{
    Unknown = 0,
    Structure = 1,
    Barricade = 2
}