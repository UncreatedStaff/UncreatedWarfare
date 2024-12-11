using DanielWillett.ReflectionTools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;

/// <summary>
/// Saves vehicle spawns linked to structures or barricades in the level savedata.
/// </summary>
/// <remarks>Decided to go with raw binary instead of SQL since this kind of relies on other level savedata and the map, plus it was easier.</remarks>

[Priority(-1 /* load after BuildableSaver */)]
public class VehicleSpawnerStore : ILayoutHostedService
{
    public class VehicleSpawnRecord
    {
        public required string UniqueName { get; set; }
        public required uint BuildableInstanceId { get; set; }
        public required IAssetLink<VehicleAsset> VehicleAsset { get; set; }
        public required List<uint> SignInstanceIds { get; set; }
        public bool IsStructure { get; set; } = false;
    }

    private readonly YamlDataStore<List<VehicleSpawnRecord>> _dataStore;
    private readonly ILogger<VehicleSpawnerStore> _logger;
    private readonly List<VehicleSpawnInfo> _spawns;

    /// <summary>
    /// List of all spawns.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync"/> or <see cref="AddOrUpdateSpawnAsync"/> when making changes.</remarks>
    public IReadOnlyList<VehicleSpawnInfo> Spawns { get; }

    public VehicleSpawnerStore(ILogger<VehicleSpawnerStore> logger)
    {
        _logger = logger;
        _dataStore = new YamlDataStore<List<VehicleSpawnRecord>>(GetFolderPath(), logger, reloadOnFileChanged: false, () => new List<VehicleSpawnRecord>());
        _spawns = new List<VehicleSpawnInfo>(32);
        Spawns = new ReadOnlyCollection<VehicleSpawnInfo>(_spawns);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _dataStore.Reload();
        ReloadVehicleSpawns(_dataStore.Data);
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        _dataStore.Dispose();
        return UniTask.CompletedTask;
    }

    private static string GetFolderPath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Level.info.name,
            "VehicleSpawners.yml"
        );
    }

    /// <summary>
    /// Add a new spawn or just save the list if it's already in the list.
    /// </summary>
    public async UniTask AddOrUpdateSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);


        int existingSpawnerIndex = _spawns.FindIndex(s => s.Spawner.Equals(spawnInfo.Spawner));

        if (existingSpawnerIndex != -1)
        {
            _spawns[existingSpawnerIndex] = spawnInfo;
        }
        else
        {
            _spawns.Add(spawnInfo);
        }

        int existingRecordIndex = _dataStore.Data.FindIndex(s => s.BuildableInstanceId == spawnInfo.Spawner.InstanceId && s.IsStructure == spawnInfo.Spawner.IsStructure);

        VehicleSpawnRecord record = new VehicleSpawnRecord
        {
            UniqueName = spawnInfo.UniqueName,
            BuildableInstanceId = spawnInfo.Spawner.InstanceId,
            VehicleAsset = spawnInfo.Vehicle,
            SignInstanceIds = spawnInfo.Signs.Select(s => s.InstanceId).ToList(),
            IsStructure = spawnInfo.Spawner.IsStructure
        };

        if (existingRecordIndex != -1)
        {
            _dataStore.Data[existingRecordIndex] = record;
        }
        else
        {
            _dataStore.Data.Add(record);
        }

        _dataStore.Data.Sort((x, y) => string.Compare(x.UniqueName, y.UniqueName, CultureInfo.InvariantCulture, 0));
        _dataStore.Save();

        PrintSpawns();
    }

    /// <summary>
    /// Remove the given spawn if it's in the list.
    /// </summary>
    public async UniTask<bool> RemoveSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        int removed = _dataStore.Data.RemoveAll(s => s.BuildableInstanceId == spawnInfo.Spawner.InstanceId && s.IsStructure == spawnInfo.Spawner.IsStructure);

        _spawns.Remove(spawnInfo);

        _dataStore.Save();

        PrintSpawns();

        if (removed != 1)
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

    private void ReloadVehicleSpawns(List<VehicleSpawnRecord> records)
    {
        _spawns.Clear();
        _logger.LogDebug("Vehicle Spawner Store loading spawns...");
        foreach (VehicleSpawnRecord rec in records)
        {
            IBuildable? spawner = null;
            if (rec.IsStructure)
            {
                StructureInfo spawnerInfo = StructureUtility.FindStructure(rec.BuildableInstanceId);
                if (spawnerInfo.Drop == null)
                {
                    _logger.LogWarning("Missing spawner structure for vehicle spawner '{0}' (Instance ID: {1} Vehicle Asset: {2}. " +
                        "This spawner may be removed from config, or fixed by editing the Instance ID.",
                        rec.UniqueName, rec.BuildableInstanceId, rec.VehicleAsset);
                }
                else
                {
                    spawner = new BuildableStructure(spawnerInfo.Drop);
                }
            }
            else
            {
                BarricadeInfo spawnerInfo = BarricadeUtility.FindBarricade(rec.BuildableInstanceId);
                if (spawnerInfo.Drop == null)
                {
                    _logger.LogWarning("Missing spawner barricade for vehicle spawner '{0}' (Instance ID: {1} Vehicle Asset: {2}. " +
                        "This spawner may be removed from config, or fixed by editing the Instance ID.",
                        rec.UniqueName, rec.BuildableInstanceId, rec.VehicleAsset);
                }
                else
                {
                    spawner = new BuildableBarricade(spawnerInfo.Drop);
                }
            }

            if (spawner == null)
                continue;

            VehicleSpawnInfo spawnInfo = new VehicleSpawnInfo
            {
                UniqueName = rec.UniqueName,
                Vehicle = AssetLink.Create<VehicleAsset>(rec.VehicleAsset),
                Spawner = spawner
            };

            foreach (uint signInstanceId in rec.SignInstanceIds)
            {
                BarricadeInfo signInfo = BarricadeUtility.FindBarricade(signInstanceId);
                if (signInfo.Drop == null)
                {
                    _logger.LogWarning("Missing sign barricade for linked vehicle spawner '{0}' (Sign Instance ID: {1}). " +
                        "This sign may be removed from config, or fixed by editing the Instance ID.",
                        rec.UniqueName, signInstanceId, rec.VehicleAsset);
                }
                else
                {
                    spawnInfo.Signs.Add(new BuildableBarricade(signInfo.Drop));
                }
            }
            _spawns.Add(spawnInfo);
        }
        _logger.LogDebug($"Vehicle Spawner Store loaded {_dataStore.Data.Count} spawns.");
        PrintSpawns();
    }
    private void PrintSpawns()
    {
        foreach(var spawn in _spawns)
        {
            _logger.LogDebug($"     {spawn}");
        }
    }
}