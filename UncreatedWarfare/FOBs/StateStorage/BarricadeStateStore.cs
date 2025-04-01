using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.FOBs.StateStorage;

/// <summary>
/// A service for storing and fetching <see cref="BarricadeData"/> states (byte arrays that contain
/// information about its stored items) in yaml, so that a particular state can be loaded later.
/// </summary>
public class BarricadeStateStore : ILayoutHostedService, IDisposable
{
    private readonly YamlDataStore<List<BarricadeStateSave>> _dataStore;
    private readonly WarfareModule _warfareModule;

    /// <summary>
    /// List of all buildable save.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync(CancellationToken)"/> or <see cref="SaveAsync(ItemBarricadeAsset,byte[],FactionInfo?,CancellationToken)"/> when making changes.</remarks>
    public IReadOnlyList<BarricadeStateSave> Spawns => _dataStore.Data;
    public BarricadeStateStore(WarfareModule warfareModule, ILogger<BarricadeStateStore> logger)
    {
        _warfareModule = warfareModule;
        _dataStore = new YamlDataStore<List<BarricadeStateSave>>(GetFolderPath(), logger, reloadOnFileChanged: true, () => []);
        ReloadSaves();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        ReloadSaves();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
    private void ReloadSaves()
    {
        _dataStore.Reload();
    }
    private string GetFolderPath()
    {
        return Path.Combine(
            _warfareModule.HomeDirectory,
            "Maps",
            Provider.map,
            "BarricadeStateSaves.yml"
        );
    }

    /// <summary>
    /// Add a new barricade save or just save the list if it's already in the list.
    /// </summary>
    public async UniTask SaveAsync(ItemBarricadeAsset asset, byte[] state, FactionInfo? faction = null, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int existingRecordIndex = _dataStore.Data.FindIndex(s => s.BarricadeAsset.MatchAsset(asset) && (faction == null || string.Equals(s.FactionId, faction.FactionId)));

        BarricadeStateSave newSave = new BarricadeStateSave
        {
            BarricadeAsset = AssetLink.Create(asset),
            InertFriendlyName = asset.FriendlyName,
            FactionId = faction?.FactionId ?? null,
            Base64State = Convert.ToBase64String(state),
        };

        if (existingRecordIndex != -1)
        {
            _dataStore.Data[existingRecordIndex] = newSave;
        }
        else
        {
            _dataStore.Data.Add(newSave);
        }

        _dataStore.Data.Sort((x, y) => x.FactionId != null && y.FactionId != null ? x.FactionId.CompareTo(y.FactionId) : x.InertFriendlyName.CompareTo(y.InertFriendlyName));
        _dataStore.Save();
    }

    /// <summary>
    /// Delete the given barricade save if it exists.
    /// </summary>
    public async UniTask<bool> RemoveAsync(BarricadeStateSave save, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int removed = _dataStore.Data.RemoveAll(s => s.BarricadeAsset.Guid == save.BarricadeAsset.Guid);

        _dataStore.Save();

        if (removed <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Find a barricade save from an asset an an optional faction. A faction match is prioritized over an unfiltered match.
    /// </summary>
    /// <param name="matchingAsset"></param> The <see cref="ItemBarricadeAsset"/> to filter on.
    /// <param name="matchingfactionInfo"></param> An associated <see cref="FactionInfo"/> to filter on. If <see langword="null"/>, the filter will not be applied.
    /// <returns></returns>
    public BarricadeStateSave? FindBarricadeSave(ItemPlaceableAsset matchingAsset, FactionInfo? matchingfactionInfo = null)
    {
        if (matchingfactionInfo is { IsDefaultFaction: false })
        {
            BarricadeStateSave? save = _dataStore.Data.FirstOrDefault(s => s.BarricadeAsset.MatchAsset(matchingAsset)
                                                                           && string.Equals(s.FactionId, matchingfactionInfo.FactionId, StringComparison.Ordinal));
            if (save != null)
                return save;
        }

        return _dataStore.Data.FirstOrDefault(s => s.BarricadeAsset.MatchAsset(matchingAsset) && string.IsNullOrEmpty(s.FactionId));
    }

    /// <summary>
    /// Save all barricade saves to disk.
    /// </summary>
    public async UniTask SaveAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _dataStore.Save();
    }

    public void Dispose()
    {
        _dataStore.Dispose();
    }
}
