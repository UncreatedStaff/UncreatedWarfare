using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Teams;
using System.Linq;

namespace Uncreated.Warfare.FOBs.StateStorage;
public class BarricadeStateStore : ILayoutHostedService, IDisposable
{
    private YamlDataStore<List<BuildableStateSave>> _dataStore;
    private readonly WarfareModule _warfareModule;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// List of all buildable save.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync"/> or <see cref="AddOrUpdateAsync"/> when making changes.</remarks>
    public IReadOnlyList<BuildableStateSave> Spawns => _dataStore.Data;
    public Action OnSpawnsReloaded { get; set; }

    public BarricadeStateStore(WarfareModule warfareModule, IConfiguration configuration, ILogger<BarricadeStateStore> logger)
    {
        _warfareModule = warfareModule;
        _configuration = configuration;
        _logger = logger;
        _dataStore = new YamlDataStore<List<BuildableStateSave>>(GetFolderPath(), logger, reloadOnFileChanged: true, () => new List<BuildableStateSave>());
        _dataStore.OnFileReload = (dataStore) =>
        {
            OnSpawnsReloaded?.Invoke();
        };
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        ReloadSpawners();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
    private void ReloadSpawners()
    {
        _dataStore.Reload();
        OnSpawnsReloaded?.Invoke();
    }
    private string GetFolderPath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            _warfareModule.HomeDirectory,
            "Buildables",
            "BuildableStateSaves.yml"
        );
    }

    /// <summary>
    /// Add a new buildable save or just save the list if it's already in the list.
    /// </summary>
    public async UniTask AddOrUpdateAsync(ItemPlaceableAsset asset, byte[] state, FactionInfo? faction = null, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int existingRecordIndex = _dataStore.Data.FindIndex(s => s.BuildableAsset.MatchAsset(asset));

        BuildableStateSave newSave = new BuildableStateSave
        {
            BuildableAsset = AssetLink.Create(asset),
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
    /// Remove the given buildable save if it's in the list.
    /// </summary>
    public async UniTask<bool> RemoveAsync(BuildableStateSave spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int removed = _dataStore.Data.RemoveAll(s => s.BuildableAsset == spawnInfo.BuildableAsset);

        _dataStore.Save();

        if (removed <= 0)
            return false;

        return true;
    }
    public BuildableStateSave FindBuildableSave(IAssetLink<ItemPlaceableAsset> matchingAsset, FactionInfo? factionInfo = null)
    {
        if (factionInfo != null)
            return _dataStore.Data.FirstOrDefault(s => s.BuildableAsset.MatchAsset(matchingAsset) && s.FactionId != null && s.FactionId == factionInfo.FactionId);

        return _dataStore.Data.FirstOrDefault(s => s.BuildableAsset.MatchAsset(matchingAsset));
    }

    /// <summary>
    /// Save all buildable saves to disk.
    /// </summary>
    public async UniTask SaveAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _dataStore.Save(); // todo: make an async Save() function
    }

    public void Dispose()
    {
        _dataStore.Dispose();
    }
}
