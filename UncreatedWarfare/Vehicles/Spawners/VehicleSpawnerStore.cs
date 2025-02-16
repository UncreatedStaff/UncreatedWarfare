using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Vehicles;

/// <summary>
/// Saves vehicle spawns linked to structures or barricades in the level savedata.
/// </summary>
/// <remarks>Decided to go with raw binary instead of SQL since this kind of relies on other level savedata and the map, plus it was easier.</remarks>

[Priority(-1 /* load after BuildableSaver and VehicleInfoStore */)]
public class VehicleSpawnerStore : ILayoutHostedService, IDisposable
{
    private YamlDataStore<List<VehicleSpawnInfo>> _dataStore;
    private readonly WarfareModule _warfare;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VehicleSpawnerStore> _logger;

    /// <summary>
    /// List of all spawns.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync"/> or <see cref="AddOrUpdateSpawnAsync"/> when making changes.</remarks>
    public IReadOnlyList<VehicleSpawnInfo> Spawns => _dataStore.Data;
    public Action OnSpawnsReloaded { get; set; }

    public VehicleSpawnerStore(WarfareModule warfare, IConfiguration configuration, ILogger<VehicleSpawnerStore> logger)
    {
        _warfare = warfare;
        _configuration = configuration;
        _logger = logger;
        _dataStore = new YamlDataStore<List<VehicleSpawnInfo>>(GetFolderPath(), logger, reloadOnFileChanged: true, () => new List<VehicleSpawnInfo>());
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
    private static string GetFolderPath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Provider.map,
            "VehicleSpawners.yml"
        );
    }

    /// <summary>
    /// Add a new spawn or just save the list if it's already in the list.
    /// </summary>
    public async UniTask AddOrUpdateSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int existingRecordIndex = _dataStore.Data.FindIndex(s => s.BuildableInstanceId == spawnInfo.BuildableInstanceId && s.IsStructure == spawnInfo.IsStructure);

        if (existingRecordIndex != -1)
        {
            _dataStore.Data[existingRecordIndex] = spawnInfo;
        }
        else
        {
            _dataStore.Data.Add(spawnInfo);
        }

        _dataStore.Data.Sort((x, y) => x.UniqueName.CompareTo(y.UniqueName));
        _dataStore.Save();
    }

    /// <summary>
    /// Remove the given spawn if it's in the list.
    /// </summary>
    public async UniTask<bool> RemoveSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int removed = _dataStore.Data.RemoveAll(s => s.BuildableInstanceId == spawnInfo.BuildableInstanceId && s.IsStructure == spawnInfo.IsStructure);

        _dataStore.Save();

        if (removed <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Save all vehicle spawns to disk.
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