using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Vehicles.Spawners;

/// <summary>
/// Saves vehicle spawns linked to structures or barricades in the level savedata.
/// </summary>
/// <remarks>Decided to go with raw binary instead of SQL since this kind of relies on other level savedata and the map, plus it was easier.</remarks>

[Priority(-1 /* load after BuildableSaver and VehicleInfoStore */)]
public class VehicleSpawnerStore : ILayoutHostedService, IDisposable
{
    private readonly YamlDataStore<List<VehicleSpawnInfo>> _dataStore;

    /// <summary>
    /// List of all spawns.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync"/> or <see cref="AddOrUpdateSpawnAsync"/> when making changes.</remarks>
    public IReadOnlyList<VehicleSpawnInfo> Spawns => _dataStore.Data;

    public event Action? OnSpawnsReloaded;

    public VehicleSpawnerStore(ILogger<VehicleSpawnerStore> logger)
    {
        _dataStore = new YamlDataStore<List<VehicleSpawnInfo>>(GetFolderPath(), logger, reloadOnFileChanged: true, () => new List<VehicleSpawnInfo>());
        _dataStore.OnFileReload = _ =>
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

        _dataStore.Data.Sort((x, y) => string.Compare(x.UniqueName, y.UniqueName, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase));
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

        _dataStore.Save();
    }

    public void Dispose()
    {
        _dataStore.Dispose();
    }
}