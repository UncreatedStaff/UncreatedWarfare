using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using DanielWillett.SpeedBytes.Unity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
public class VehicleSpawnerStoreBinary : ILayoutHostedService
{
    private readonly ILogger<VehicleSpawnerStoreBinary> _logger;
    private readonly List<VehicleSpawnInfo> _spawns;

    /// <summary>
    /// List of all spawns.
    /// </summary>
    /// <remarks>Use <see cref="SaveAsync"/> or <see cref="AddOrUpdateSpawnAsync"/> when making changes.</remarks>
    public IReadOnlyList<VehicleSpawnInfo> Spawns { get; }

    public VehicleSpawnerStoreBinary(ILogger<VehicleSpawnerStoreBinary> logger)
    {
        _logger = logger;
        _spawns = new List<VehicleSpawnInfo>(32);
        Spawns = new ReadOnlyCollection<VehicleSpawnInfo>(_spawns);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        if (Level.isLoaded)
        {
            OnLevelLoaded(Level.BUILD_INDEX_GAME);
        }
        else
        {
            Level.onLevelLoaded += OnLevelLoaded;
        }

        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        Level.onLevelLoaded -= OnLevelLoaded;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Add a new spawn or just save the list if it's already in the list.
    /// </summary>
    public async UniTask AddOrUpdateSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!_spawns.Contains(spawnInfo))
        {
            _spawns.Add(spawnInfo);
        }

        Save(GetFilePath());
    }

    /// <summary>
    /// Remove the given spawn if it's in the list.
    /// </summary>
    public async UniTask<bool> RemoveSpawnAsync(VehicleSpawnInfo spawnInfo, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!_spawns.Remove(spawnInfo))
            return false;

        Save(GetFilePath());
        return true;
    }

    /// <summary>
    /// Save all vehicle spawns to disk.
    /// </summary>
    public async UniTask SaveAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        Save(GetFilePath());
    }

    private static string GetFilePath()
    {
        return Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Level.info.name,
            "VehicleSpawners.dat"
        );
    }

    private void OnLevelLoaded(int level)
    {
        if (level != Level.BUILD_INDEX_GAME)
            return;

        Level.onLevelLoaded -= OnLevelLoaded;

        string filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            Save(filePath);
            return;
        }

        try
        {
            LoadIntl(filePath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to load data file.");
        }
    }

    private void LoadIntl(string filePath)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        ByteReader reader = new ByteReader();
        reader.LoadNew(fs);

        // version
        _ = reader.ReadUInt8();

        int spawnCount = reader.ReadInt32();

        _spawns.Clear();

        for (int i = 0; i < spawnCount; ++i)
        {
            string uniqueName = reader.ReadString();
            Guid vehicleGuid = reader.ReadGuid();
            Vector3 spawnPosition = reader.ReadVector3();
            uint spawnInstanceId = reader.ReadUInt32();
            Guid spawnAsset = reader.ReadGuid();
            bool isSpawnStructure = reader.ReadBool();

            IBuildable? spawner = null;
            if (isSpawnStructure)
            {
                StructureInfo spawnerInfo = StructureUtility.FindStructure(spawnInstanceId, AssetLink.Create<ItemStructureAsset>(spawnAsset), spawnPosition);
                if (spawnerInfo.Drop == null)
                {
                    _logger.LogWarning("Missing spawner structure {0}, instance ID {1} at {2} for vehicle spawner for {3}.", spawnAsset, spawnInstanceId, spawnPosition, vehicleGuid);
                }
                else
                {
                    spawner = new BuildableStructure(spawnerInfo.Drop);
                }
            }
            else
            {
                BarricadeInfo spawnerInfo = BarricadeUtility.FindBarricade(spawnInstanceId, AssetLink.Create<ItemBarricadeAsset>(spawnAsset), spawnPosition);
                if (spawnerInfo.Drop == null)
                {
                    _logger.LogWarning("Missing spawner barricade {0}, instance ID {1} at {2} for vehicle spawner for {3}.", spawnAsset, spawnInstanceId, spawnPosition, vehicleGuid);
                }
                else
                {
                    spawner = new BuildableBarricade(spawnerInfo.Drop);
                }
            }

            bool willSkip = spawner == null;

            VehicleSpawnInfo spawnInfo = new VehicleSpawnInfo
            {
                UniqueName = uniqueName,
                Vehicle = AssetLink.Create<VehicleAsset>(vehicleGuid),
                Spawner = spawner!
            };

            int signCount = reader.ReadInt32();
            if (!willSkip && spawnInfo.Signs is List<IBuildable> list)
            {
                list.Capacity = signCount;
            }

            for (int j = 0; j < signCount; ++j)
            {
                Vector3 position = reader.ReadVector3();
                uint instanceId = reader.ReadUInt32();
                Guid asset = reader.ReadGuid();
                if (willSkip)
                    continue;

                BarricadeInfo signInfo = BarricadeUtility.FindBarricade(instanceId, AssetLink.Create<ItemBarricadeAsset>(asset), position);
                if (signInfo.Drop == null)
                {
                    _logger.LogInformation("Missing sign {0}, instance ID {1} at {2} for vehicle spawner for {3}.", asset, instanceId, position, vehicleGuid);
                }
                else
                {
                    spawnInfo.Signs.Add(new BuildableBarricade(signInfo.Drop));
                }
            }

            if (!willSkip)
            {
                _spawns.Add(spawnInfo);
            }
        }
    }

    private void Save(string filePath)
    {
        Thread.BeginCriticalRegion();
        try
        {
            SaveIntl(filePath);
        }
        finally
        {
            Thread.EndCriticalRegion();
        }
    }

    private void SaveIntl(string filePath)
    {
        using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        ByteWriter writer = new ByteWriter
        {
            Stream = fs
        };

        // version
        writer.Write((byte)0);

        int spawnCount = _spawns.Count;
        writer.Write(spawnCount);
        for (int i = 0; i < spawnCount; ++i)
        {
            VehicleSpawnInfo spawnInfo = _spawns[i];

            writer.Write(spawnInfo.UniqueName);
            writer.Write(spawnInfo.Vehicle.Guid);

            writer.Write(spawnInfo.Spawner.Position);
            writer.Write(spawnInfo.Spawner.InstanceId);
            writer.Write(spawnInfo.Spawner.Asset.GUID);
            writer.Write(spawnInfo.Spawner.IsStructure);

            IList<IBuildable> signs = spawnInfo.Signs;
            int signCount = signs.Count;

            writer.Write(signCount);
            for (int s = 0; s < signCount; ++s)
            {
                IBuildable sign = signs[s];
                writer.Write(sign.Position);
                writer.Write(sign.InstanceId);
                writer.Write(sign.Asset.GUID);
            }
        }

        writer.Flush();
    }
}