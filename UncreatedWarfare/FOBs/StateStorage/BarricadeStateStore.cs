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

/// <summary>
/// A service for storing and fetching <see cref="BarricadeData"/> states (byte arrays that contain
/// information about its stored items) in yaml, so that a particular state can be loaded later.
/// </summary>
public class BarricadeStateStore : ILayoutHostedService, IDisposable
{
    private readonly YamlDataStore<List<BarricadeStateSave>> _dataStore;
    private readonly WarfareModule _warfareModule;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// List of all buildable save.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync(System.Threading.CancellationToken)"/> or <see cref="SaveAsync(SDG.Unturned.ItemBarricadeAsset,byte[],Uncreated.Warfare.Teams.FactionInfo?,System.Threading.CancellationToken)"/> when making changes.</remarks>
    public IReadOnlyList<BarricadeStateSave> Spawns => _dataStore.Data;
    public BarricadeStateStore(WarfareModule warfareModule, IConfiguration configuration, ILogger<BarricadeStateStore> logger)
    {
        _warfareModule = warfareModule;
        _configuration = configuration;
        _logger = logger;
        _dataStore = new YamlDataStore<List<BarricadeStateSave>>(GetFolderPath(), logger, reloadOnFileChanged: true, () => []);
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
    }
    private string GetFolderPath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            _warfareModule.HomeDirectory,
            "Barricades",
            "BarricadeStateSaves.yml"
        );
    }

    /// <summary>
    /// Add a new barricade save or just save the list if it's already in the list.
    /// </summary>
    public async UniTask SaveAsync(ItemBarricadeAsset asset, byte[] state, FactionInfo? faction = null, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int existingRecordIndex = _dataStore.Data.FindIndex(s => s.BarricadeAsset.MatchAsset(asset));

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
    /// 
    /// </summary>
    /// <param name="matchingAsset"></param> The <see cref="ItemBarricadeAsset"/> to filter on.
    /// <param name="matchingfactionInfo"></param> An associated <see cref="FactionInfo"/> to filter on. If <see langword="null"/>, the filter will not be applied.
    /// <returns></returns>
    public BarricadeStateSave? FindBarricadeSave(ItemPlaceableAsset matchingAsset, FactionInfo? matchingfactionInfo = null)
    {
        if (matchingfactionInfo != null)
            return _dataStore.Data.FirstOrDefault(s => s.BarricadeAsset.MatchAsset(matchingAsset) && s.FactionId != null && s.FactionId == matchingfactionInfo.FactionId);

        return _dataStore.Data.FirstOrDefault(s => s.BarricadeAsset.MatchAsset(matchingAsset));
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
