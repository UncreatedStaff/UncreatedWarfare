using Cysharp.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Buildables;
using Uncreated.Warfare.Services;
using UnityEngine;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Responsible for saving structures or barricades that are restored if they're destroyed.
/// </summary>
public class BuildableSaver : ISessionHostedService
{
    private readonly IBuildablesDbContext _dbContext;
    private readonly ILogger<BuildableSaver> _logger;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private List<BuildableSave>? _saves;
    public BuildableSaver(IBuildablesDbContext dbContext, ILogger<BuildableSaver> logger)
    {
        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        _dbContext = dbContext;
        _logger = logger;
    }

    async UniTask ISessionHostedService.StartAsync(CancellationToken token)
    {
        await UniTask.SwitchToThreadPool();

        byte region = UCWarfare.Config.Region;
        int mapId = MapScheduler.Current;

        await _semaphore.WaitAsync(token);
        try
        {
            List<BuildableSave> saves = await _dbContext.Saves
                .Include(save => save.DisplayData)
                .Include(save => save.InstanceIds)
                .Include(save => save.Items)
                .Where(save => save.MapId.HasValue && save.MapId.Value == mapId && save.InstanceIds!.Any(instanceId => instanceId.RegionId == region))
                .ToListAsync(token);

            foreach (BuildableSave save in saves)
            {
                save.InstanceId = save.InstanceIds!.First(instanceId => instanceId.RegionId == UCWarfare.Config.Region).InstanceId;
            }

            _saves = saves;

            await UniTask.SwitchToMainThread(token);

            List<BuildableSave> newSaves = _saves;

            for (int i = newSaves.Count - 1; i >= 0; i--)
            {
                BuildableSave save = newSaves[i];
                uint oldInstanceId = save.InstanceId;
                if (!RestoreSave(newSaves, i, save))
                    continue;
                
                _dbContext.Update(save);
                if (save.InstanceIds?.FirstOrDefault(instanceId => instanceId.RegionId == UCWarfare.Config.Region) is { } instId && instId.InstanceId != oldInstanceId)
                {
                    _dbContext.Update(instId);
                }
            }

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    UniTask ISessionHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Save a barricade, or update the save if it's already saved.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade was saved, <see langword="false"/> if it was just updated.</returns>
    public async UniTask<bool> SaveBarricadeAsync(BarricadeDrop barricade, CancellationToken token = default)
    {
        BarricadeData data = barricade.GetServersideData();
        await UniTask.SwitchToThreadPool();

        BuildableSave newSave;
        List<BuildableSave> saves;

        await _semaphore.WaitAsync(token);
        try
        {
            byte region = UCWarfare.Config.Region;
            int mapId = MapScheduler.Current;
            uint instId = barricade.instanceID;

            saves = await _dbContext.Saves
                .Include(save => save.InstanceIds)
                .Where(save => !save.IsStructure && save.MapId.HasValue && save.MapId.Value == mapId && save.InstanceIds!.Any(instanceId => instanceId.RegionId == region && instanceId.InstanceId == instId))
                .ToListAsync(token);

            _dbContext.RemoveRange(saves);

            newSave = new BuildableSave
            {
                Buildable = new BuildableBarricade(barricade),
                InstanceId = instId,
                InstanceIds = new List<BuildableInstanceId>(1),
                IsStructure = false,
                Group = data.group,
                Owner = data.owner,
                Item = new UnturnedAssetReference(barricade.asset.GUID),
                Position = data.point,
                Rotation = new Vector3(MeasurementTool.byteToAngle(data.angle_x), MeasurementTool.byteToAngle(data.angle_y), MeasurementTool.byteToAngle(data.angle_z)),
                MapId = MapScheduler.Current
            };

            if (barricade.interactable is InteractableStorage storage)
            {
                await UniTask.SwitchToMainThread(token);
                
                int ct = storage.items.getItemCount();

                newSave.Items = new List<BuildableStorageItem>(ct);
                newSave.State = Array.Empty<byte>();

                for (int i = 0; i < ct; ++i)
                {
                    ItemJar item = storage.items.getItem((byte)i);
                    byte[] state = new byte[item.item.state.Length];
                    Buffer.BlockCopy(item.item.state, 0, state, 0, state.Length);
                    BuildableStorageItem storageItem = new BuildableStorageItem
                    {
                        Save = newSave,
                        Amount = item.item.amount,
                        Quality = item.item.quality,
                        State = state,
                        PositionX = item.x,
                        PositionY = item.y,
                        Rotation = item.rot,
                        Item = item.GetAsset() is { } asset ? new UnturnedAssetReference(asset.GUID) : new UnturnedAssetReference()
                    };
                    
                    newSave.Items.Add(storageItem);
                    _dbContext.Add(storageItem);
                }

                if (storage.isDisplay)
                {
                    BuildableItemDisplayData displayData = new BuildableItemDisplayData
                    {
                        Mythic = Assets.find(EAssetType.MYTHIC, storage.displayMythic) is { } mythic ? new UnturnedAssetReference(mythic.GUID) : new UnturnedAssetReference(storage.displayMythic),
                        Skin = Assets.find(EAssetType.SKIN, storage.displaySkin) is { } skin ? new UnturnedAssetReference(skin.GUID) : new UnturnedAssetReference(storage.displayMythic),
                        DynamicProps = storage.displayDynamicProps,
                        Tags = storage.displayTags,
                        Rotation = storage.rot_comp,
                        Save = newSave
                    };

                    newSave.DisplayData = displayData;
                    _dbContext.Add(displayData);
                }

                await UniTask.SwitchToThreadPool();
            }
            else
            {
                newSave.Items = new List<BuildableStorageItem>(0);
                byte[] state = new byte[data.barricade.state.Length];
                Buffer.BlockCopy(data.barricade.state, 0, state, 0, state.Length);
                newSave.State = state;
            }

            BuildableInstanceId instanceId = new BuildableInstanceId
            {
                InstanceId = instId,
                RegionId = region,
                Save = newSave
            };

            _dbContext.Add(newSave);
            _dbContext.Add(instanceId);

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        bool removedAny = saves.Count == 0;

        if (!data.barricade.isDead)
            return removedAny;

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (!RestoreSave(null, 0, newSave))
            return removedAny;

        await _semaphore.WaitAsync(token);
        try
        {
            _dbContext.Update(newSave);
            if (newSave.InstanceIds.FirstOrDefault(x => x.RegionId == UCWarfare.Config.Region) is { } instanceId)
                _dbContext.Update(instanceId);

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        return removedAny;
    }

    /// <summary>
    /// Save a structure, or update the save if it's already saved.
    /// </summary>
    /// <returns><see langword="true"/> if the structure was saved, <see langword="false"/> if it was just updated.</returns>
    public async UniTask<bool> SaveStructureAsync(StructureDrop structure, CancellationToken token = default)
    {
        StructureData data = structure.GetServersideData();
        await UniTask.SwitchToThreadPool();

        BuildableSave newSave;
        List<BuildableSave> saves;

        await _semaphore.WaitAsync(token);
        try
        {
            byte region = UCWarfare.Config.Region;
            int mapId = MapScheduler.Current;
            uint instId = structure.instanceID;

            saves = await _dbContext.Saves
                .Include(save => save.InstanceIds)
                .Where(save => save.IsStructure && save.MapId.HasValue && save.MapId.Value == mapId && save.InstanceIds!.Any(instanceId => instanceId.RegionId == region && instanceId.InstanceId == instId))
                .ToListAsync(token);

            _dbContext.RemoveRange(saves);

            newSave = new BuildableSave
            {
                Buildable = new BuildableStructure(structure),
                InstanceId = instId,
                InstanceIds = new List<BuildableInstanceId>(1),
                IsStructure = true,
                Group = data.group,
                Owner = data.owner,
                Item = new UnturnedAssetReference(structure.asset.GUID),
                Position = data.point,
                Rotation = new Vector3(MeasurementTool.byteToAngle(data.angle_x), MeasurementTool.byteToAngle(data.angle_y), MeasurementTool.byteToAngle(data.angle_z)),
                MapId = MapScheduler.Current,
                Items = new List<BuildableStorageItem>(0)
            };

            BuildableInstanceId instanceId = new BuildableInstanceId
            {
                InstanceId = instId,
                RegionId = region,
                Save = newSave
            };

            _dbContext.Add(newSave);
            _dbContext.Add(instanceId);

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        bool removedAny = saves.Count == 0;

        if (!data.structure.isDead)
            return removedAny;
        
        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (!RestoreSave(null, 0, newSave))
            return removedAny;
        
        await _semaphore.WaitAsync(token);
        try
        {
            _dbContext.Update(newSave);
            if (newSave.InstanceIds.FirstOrDefault(x => x.RegionId == UCWarfare.Config.Region) is { } instanceId)
                _dbContext.Update(instanceId);

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        return removedAny;
    }

    /// <summary>
    /// Remove the save for a barricade.
    /// </summary>
    /// <returns><see langword="true"/> if the barricade's save was removed, otherwise <see langword="false"/>.</returns>
    public UniTask<bool> DiscardBarricadeAsync(uint instanceId, CancellationToken token = default)
    {
        return DiscardBuildableAsync(instanceId, false, token);
    }

    /// <summary>
    /// Remove the save for a structure.
    /// </summary>
    /// <returns><see langword="true"/> if the structure's save was removed, otherwise <see langword="false"/>.</returns>
    public UniTask<bool> DiscardStructureAsync(uint instanceId, CancellationToken token = default)
    {
        return DiscardBuildableAsync(instanceId, true, token);
    }

    /// <summary>
    /// Remove the save for a buildable.
    /// </summary>
    /// <returns><see langword="true"/> if the buildable's save was removed, otherwise <see langword="false"/>.</returns>
    public async UniTask<bool> DiscardBuildableAsync(uint instanceId, bool isStructure, CancellationToken token)
    {
        await UniTask.SwitchToThreadPool();

        await _semaphore.WaitAsync(token);
        try
        {
            byte region = UCWarfare.Config.Region;
            int mapId = MapScheduler.Current;

            List<BuildableSave> saves = await _dbContext.Saves
                .Where(save => save.IsStructure == isStructure && save.MapId.HasValue && save.MapId.Value == mapId && save.InstanceIds!.Any(instId => instId.RegionId == region && instId.InstanceId == instanceId))
                .ToListAsync(token);

            _dbContext.RemoveRange(saves);

            return await _dbContext.SaveChangesAsync(token) > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Sync an exitsing buildable with a buildable save, or replace the buildable if it's missing.
    /// </summary>
    private bool RestoreSave(List<BuildableSave>? saves, int index, BuildableSave save)
    {
        object? drop = save.IsStructure
            ? UCBarricadeManager.FindStructure(save.InstanceId, save.Position)
            : UCBarricadeManager.FindBarricade(save.InstanceId, save.Position);

        ItemAsset? asset = save.IsStructure
            ? save.Item.GetAsset<ItemStructureAsset>()
            : save.Item.GetAsset<ItemBarricadeAsset>();

        Vector3 rotation = save.Rotation;
        byte rotX = MeasurementTool.angleToByte(Mathf.RoundToInt(rotation.x / 2f) * 2);
        byte rotY = MeasurementTool.angleToByte(Mathf.RoundToInt(rotation.y / 2f) * 2);
        byte rotZ = MeasurementTool.angleToByte(Mathf.RoundToInt(rotation.z / 2f) * 2);

        Vector3 rot2 = new Vector3(
            MeasurementTool.byteToAngle(rotX),
            MeasurementTool.byteToAngle(rotY),
            MeasurementTool.byteToAngle(rotZ)
        );

        bool dirty = false;
        if (!rot2.AlmostEquals(rotation))
        {
            save.Rotation = rot2;
            dirty = true;
        }

        if (asset == null && drop == null)
        {
            _logger.LogWarning("Unable to find asset for save {0} with ID {1} ({2}).", save.Id, save.Item, save.IsStructure ? "structure" : "barriade");
            saves?.RemoveAt(index);
            return dirty;
        }

        if (asset == null)
        {
            asset = (ItemAsset?)(drop as BarricadeDrop)?.asset ?? ((StructureDrop)drop!).asset;
            _logger.LogWarning("Existing buildable {0} does not have the expected asset: {1}. It actually has {2} ({3}).", save.Id, save.Item, asset.GUID, asset.FriendlyName);
            saves?.RemoveAt(index);
            return dirty;
        }

        byte[] state, expectedState;

        if (drop == null)
        {
            Vector3 pos = save.Position, rot = save.Rotation;
            _logger.LogInformation("Replacing missing buildable {0} ({1} - {2}) at {3}.", save.Id, save.Item, asset.FriendlyName, pos);
            uint instanceId;
            BuildableInstanceId? instanceIdObj;
            if (save.IsStructure)
            {
                if (!StructureManager.dropReplicatedStructure(new Structure((ItemStructureAsset)asset, ((ItemStructureAsset)asset).health), pos, Quaternion.Euler(rot), save.Owner, save.Group))
                {
                    _logger.LogError("Failed to place structure buildable {0} ({1} - {2}) at {3}", save.Id, save.Item, asset.FriendlyName, pos);
                    saves?.RemoveAt(index);
                }

                Regions.tryGetCoordinate(pos, out byte x, out byte y);
                StructureManager.tryGetRegion(x, y, out _);
                drop = StructureManager.regions[x, y].drops.GetTail();
                instanceId = ((StructureDrop)drop).instanceID;
                save.InstanceId = instanceId;
                save.Buildable = new BuildableStructure((StructureDrop)drop);
                instanceIdObj = (save.InstanceIds ??= new List<BuildableInstanceId>()).FirstOrDefault(id => id.RegionId == UCWarfare.Config.Region);
                if (instanceIdObj == null)
                {
                    instanceIdObj = new BuildableInstanceId
                    {
                        RegionId = UCWarfare.Config.Region,
                        InstanceId = instanceId,
                        Save = save,
                        SaveId = save.Id
                    };
                    save.InstanceIds.Add(instanceIdObj);
                    _dbContext.Add(instanceIdObj);
                }
                else
                {
                    instanceIdObj.InstanceId = instanceId;
                    _dbContext.Update(instanceIdObj);
                }
                return dirty;
            }

            state = save.State ?? Array.Empty<byte>();
            if (state.Length > 0)
            {
                byte[] newState = new byte[state.Length];
                Buffer.BlockCopy(state, 0, newState, 0, state.Length);
                state = newState;
            }

            Transform? barricade = BarricadeManager.dropNonPlantedBarricade(
                new Barricade((ItemBarricadeAsset)asset, ((ItemBarricadeAsset)asset).health, state),
                pos, Quaternion.Euler(rot), save.Owner, save.Group
            );

            drop = BarricadeManager.FindBarricadeByRootTransform(barricade);

            if (drop == null)
            {
                _logger.LogError("Failed to place barricade buildable {0} ({1} - {2}) at {3}", save.Id, save.Item, asset.FriendlyName, pos);
                saves?.RemoveAt(index);
                return dirty;
            }

            save.Buildable = new BuildableBarricade((BarricadeDrop)drop);
            instanceId = ((BarricadeDrop)drop).instanceID;
            save.InstanceId = instanceId;
            instanceIdObj = (save.InstanceIds ??= new List<BuildableInstanceId>()).FirstOrDefault(id => id.RegionId == UCWarfare.Config.Region);
            if (instanceIdObj == null)
            {
                instanceIdObj = new BuildableInstanceId
                {
                    RegionId = UCWarfare.Config.Region,
                    InstanceId = instanceId,
                    Save = save,
                    SaveId = save.Id
                };
                save.InstanceIds.Add(instanceIdObj);
                _dbContext.Add(instanceIdObj);
            }
            else
            {
                instanceIdObj.InstanceId = instanceId;
                _dbContext.Update(instanceIdObj);
            }
        }

        if (save.IsStructure)
        {
            StructureDrop sDrop = (StructureDrop)drop;
            StructureData sData = sDrop.GetServersideData();
            if (sData.angle_x != rotX || sData.angle_y != rotY || sData.angle_z != rotZ || !sData.point.AlmostEquals(save.Position))
            {
                _logger.LogInformation("Moving misplaced structure {0} ({1} - {2}) at pos: {3} rot: {4}.", save.Id, save.Item, asset.FriendlyName, save.Position, save.Rotation);
                StructureManager.ServerSetStructureTransform(sDrop.model, save.Position, Quaternion.Euler(save.Rotation));
            }

            if (sDrop.asset.health > sData.structure.health)
            {
                StructureManager.repair(sDrop.model, sDrop.asset.health, 1f, Provider.server);
            }

            save.Buildable = new BuildableStructure((StructureDrop)drop);

            if (sData.owner != save.Owner || sData.group != save.Group)
            {
                F.SetOwnerOrGroup(sDrop, save.Owner, save.Group);
            }

            return dirty;
        }

        BarricadeDrop bDrop = (BarricadeDrop)drop;
        BarricadeData bData = bDrop.GetServersideData();

        if (bData.angle_x != rotX || bData.angle_y != rotY || bData.angle_z != rotZ || !bData.point.AlmostEquals(save.Position))
        {
            _logger.LogInformation("Moving misplaced structure {0} ({1} - {2}) at pos: {3} rot: {4}.", save.Id, save.Item, asset.FriendlyName, save.Position, save.Rotation);
            BarricadeManager.ServerSetBarricadeTransform(bDrop.model, save.Position, Quaternion.Euler(save.Rotation));
        }

        if (bDrop.asset.health > bData.barricade.health)
        {
            StructureManager.repair(bDrop.model, bDrop.asset.health, 1f, Provider.server);
        }

        save.Buildable = new BuildableBarricade((BarricadeDrop)drop);

        bool isDifferent = false;
        bool replicated = false;
        if (bDrop.interactable is InteractableStorage storage)
        {
            if (storage.isDisplay)
            {
                BuildableItemDisplayData? dispData = save.DisplayData;
                if (dispData == null)
                {
                    save.DisplayData = new BuildableItemDisplayData
                    {
                        Mythic = Assets.find(EAssetType.MYTHIC, storage.displayMythic) is { } mythic ? new UnturnedAssetReference(mythic.GUID) : new UnturnedAssetReference(storage.displayMythic),
                        Skin = Assets.find(EAssetType.SKIN, storage.displaySkin) is { } skin ? new UnturnedAssetReference(skin.GUID) : new UnturnedAssetReference(storage.displayMythic),
                        DynamicProps = storage.displayDynamicProps,
                        Tags = storage.displayTags,
                        Rotation = storage.rot_comp,
                        Save = save,
                        SaveId = save.Id
                    };
                    dirty = true;
                }
                else
                {
                    ushort myhticId = dispData.Mythic.GetAsset<MythicAsset>()?.id ?? 0;
                    ushort skinId = dispData.Skin.GetAsset<SkinAsset>()?.id ?? 0;
                    if (storage.displayMythic != myhticId
                        || storage.displaySkin != skinId
                        || storage.rot_comp != dispData.Rotation
                        || !string.Equals(storage.displayTags, dispData.Tags, StringComparison.Ordinal)
                        || !string.Equals(storage.displayDynamicProps, dispData.DynamicProps, StringComparison.Ordinal))
                    {
                        storage.applyRotation(dispData.Rotation);
                        storage.displayMythic = myhticId;
                        storage.displaySkin = skinId;
                        storage.displayTags = dispData.Tags;
                        storage.displayDynamicProps = dispData.DynamicProps;
                        isDifferent = true;
                        _logger.LogInformation("Fixing mismatched display info {0} ({1} - {2}).", save.Id, save.Item, asset.FriendlyName);
                    }
                }
            }

            IList<BuildableStorageItem> items = save.Items ?? Array.Empty<BuildableStorageItem>();
            BitArray hasChecked = new BitArray(items.Count);
            int itemCount = storage.items.getItemCount();
            for (int i = itemCount - 1; i >= 0; --i)
            {
                ItemJar item = storage.items.getItem((byte)i);
                int ind = -1;
                for (int j = 0; j < items.Count; ++j)
                {
                    BuildableStorageItem storageItem = items[i];
                    if (storageItem.PositionX != item.x || storageItem.PositionY != item.y)
                        continue;

                    ind = j;
                    break;
                }

                if (ind >= 0)
                {
                    BuildableStorageItem matchingItem = items[ind];
                    if (matchingItem.Quality != item.item.quality || matchingItem.Amount != item.item.amount || matchingItem.Rotation != item.rot)
                    {
                        _logger.LogInformation("Removing item with mismatched quality, amount, or rotation to storage item {0} ({1} - {2}) at ({3}, {4}).", save.Id, save.Item, asset.FriendlyName, item.x, item.y);
                        storage.items.removeItem((byte)i);
                        continue;
                    }

                    ItemAsset? expectedAsset = matchingItem.Item.GetAsset<ItemAsset>(),
                               actualAsset = item.item.GetAsset();
                    if (actualAsset == null || expectedAsset != null && expectedAsset.GUID != actualAsset.GUID)
                    {
                        _logger.LogInformation("Removing item with mismatched asset to storage item {0} ({1} - {2}) at ({3}, {4}).", save.Id, save.Item, asset.FriendlyName, item.x, item.y);
                        storage.items.removeItem((byte)i);
                        continue;
                    }

                    state = item.item.state ?? Array.Empty<byte>();
                    expectedState = matchingItem.State ?? Array.Empty<byte>();
                    bool isItemDifferent = state.Length != expectedState.Length;
                    if (!isItemDifferent)
                    {
                        for (int j = 0; j < expectedState.Length; ++j)
                        {
                            if (state[j] == expectedState[j])
                                continue;

                            isItemDifferent = true;
                            break;
                        }
                    }

                    if (isItemDifferent)
                    {
                        _logger.LogInformation("Removing item with mismatched state to storage item {0} ({1} - {2}) at ({3}, {4}).", save.Id, save.Item, asset.FriendlyName, item.x, item.y);
                        storage.items.removeItem((byte)i);
                        continue;
                    }

                    hasChecked[ind] = true;
                }
                else
                {
                    storage.items.removeItem((byte)i);
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                if (hasChecked[i])
                    continue;

                BuildableStorageItem storageItem = items[i];
                ItemAsset? expectedAsset = storageItem.Item.GetAsset<ItemAsset>();
                if (expectedAsset == null)
                {
                    _logger.LogInformation("Unknown asset in storage item for {0} ({1} - {2}) at ({3}, {4}).", save.Id, save.Item, asset.FriendlyName, storageItem.PositionX, storageItem.PositionY);
                    storage.items.removeItem((byte)i);
                    continue;
                }

                storage.items.addItem(storageItem.PositionX, storageItem.PositionY, storageItem.Rotation, new Item(expectedAsset, EItemOrigin.WORLD)
                {
                    state = storageItem.State,
                    amount = storageItem.Amount,
                    quality = storageItem.Quality
                });
            }

            if (bData.owner != save.Owner || bData.group != save.Group)
            {
                replicated = F.SetOwnerOrGroup(bDrop, save.Owner, save.Group);
            }

            if (save.State is not { Length: 0 })
            {
                save.State = Array.Empty<byte>();
                dirty = true;
            }

            if (!replicated && isDifferent)
                ReplicateBarricadeState(bDrop);

            return dirty;
        }

        state = bData.barricade.state ?? Array.Empty<byte>();
        expectedState = save.State ?? Array.Empty<byte>();

        isDifferent |= state.Length != expectedState.Length;
        if (!isDifferent)
        {
            for (int i = 0; i < expectedState.Length; ++i)
            {
                if (state[i] == expectedState[i])
                    continue;

                isDifferent = true;
                break;
            }
        }

        if (isDifferent)
        {
            BarricadeManager.updateState(bDrop.model, expectedState, expectedState.Length);
            bDrop.ReceiveUpdateState(expectedState);
        }

        if (bData.owner != save.Owner || bData.group != save.Group)
        {
            replicated = F.SetOwnerOrGroup(bDrop, save.Owner, save.Group);
            state = bData.barricade.state ?? Array.Empty<byte>();

            bool isDifferent2 = state.Length != expectedState.Length;
            if (!isDifferent2)
            {
                for (int i = 0; i < expectedState.Length; ++i)
                {
                    if (state[i] == expectedState[i])
                        continue;

                    isDifferent2 = true;
                    break;
                }
            }

            if (isDifferent2)
            {
                save.State = state;
                dirty = true;
            }
        }

        if (!replicated && isDifferent)
            ReplicateBarricadeState(bDrop);

        return dirty;
    }
    
    private static void ReplicateBarricadeState(BarricadeDrop bDrop)
    {
        if (Data.SendUpdateBarricadeState == null)
            return;

        BarricadeData bData = bDrop.GetServersideData();
        BarricadeManager.tryGetRegion(bDrop.model, out byte x, out byte y, out ushort plant, out _);
        
        switch (bDrop.interactable)
        {
            // client-side state is different for storages so players can't know what's in storages without opening them
            case InteractableStorage storage:
                byte[] state;
                if (storage.isDisplay)
                {
                    Block block = new Block();

                    if (storage.displayItem != null)
                        block.write(storage.displayItem.id, storage.displayItem.quality, storage.displayItem.state ?? Array.Empty<byte>());
                    else
                        block.step += 4;

                    block.write(storage.displaySkin, storage.displayMythic,
                        storage.displayTags ?? string.Empty,
                        storage.displayDynamicProps ?? string.Empty, storage.rot_comp);

                    byte[] b = block.getBytes(out int size);
                    state = new byte[size + sizeof(ulong) * 2];
                    Buffer.BlockCopy(b, 0, state, sizeof(ulong) * 2, size);
                }
                else
                {
                    state = new byte[sizeof(ulong) * 2];
                }

                Buffer.BlockCopy(bData.barricade.state, 0, state, 0, sizeof(ulong) * 2);
                Data.SendUpdateBarricadeState.Invoke(bDrop.GetNetId(), ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), state);
                break;

            // client-side signs requires separate translation
            case InteractableSign sign when sign.text.StartsWith(Signs.Prefix, StringComparison.Ordinal):
                NetId id = bDrop.GetNetId();
                state = null!;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (plant == ushort.MaxValue && !Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, BarricadeManager.BARRICADE_REGIONS))
                        continue;

                    byte[] text = Encoding.UTF8.GetBytes(Signs.GetClientText(bDrop, pl));
                    int txtLen = Math.Min(text.Length, byte.MaxValue - 17);
                    if (state == null || state.Length != txtLen + 17)
                    {
                        state = new byte[txtLen + 17];
                        Buffer.BlockCopy(bData.barricade.state, 0, state, 0, sizeof(ulong) * 2);
                        state[16] = (byte)txtLen;
                    }

                    Buffer.BlockCopy(text, 0, state, 17, txtLen);
                    Data.SendUpdateBarricadeState.Invoke(id, ENetReliability.Reliable, pl.Connection, state);
                }

                break;

            default:
                Data.SendUpdateBarricadeState.Invoke(bDrop.GetNetId(), ENetReliability.Reliable, BarricadeManager.GatherRemoteClientConnections(x, y, plant), bData.barricade.state);
                break;
        }
    }
}