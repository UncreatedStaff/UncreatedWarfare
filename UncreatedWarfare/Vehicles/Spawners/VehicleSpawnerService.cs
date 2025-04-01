using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles.Spawners;

[Priority(-2 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
public class VehicleSpawnerService : ILayoutHostedService, IDisposable
{
    private readonly TrackingList<VehicleSpawner> _spawns;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleInfoStore _vehicleStore;
    private readonly WarfareModule _module;
    private readonly ILogger<VehicleSpawnerService> _logger;
    private readonly ILoopTicker _updateTicker;

    public VehicleSpawnerStore SpawnerStore => _spawnerStore;
    public ReadOnlyTrackingList<VehicleSpawner> Spawners { get; }

    public VehicleSpawnerService(VehicleSpawnerStore spawnerStore,
        VehicleInfoStore vehicleStore,
        ILoopTickerFactory loopTickerFactory,
        ILogger<VehicleSpawnerService> logger,
        WarfareModule module)
    {
        _spawns = new TrackingList<VehicleSpawner>();
        _spawnerStore = spawnerStore;
        _spawnerStore.OnSpawnsReloaded += () => ReloadSpawners(_spawnerStore.Spawns);
        _vehicleStore = vehicleStore;
        _logger = logger;
        _module = module;
        _updateTicker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(1), false, true);

        Spawners = new ReadOnlyTrackingList<VehicleSpawner>(_spawns);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _updateTicker.Dispose();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public bool TryGetSpawner(uint signInstanceId, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawns.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == signInstanceId));
        return spawner != null;
    }

    public bool TryGetSpawner(IBuildable spawnerBuildable, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawns.FirstOrDefault(x => x.Buildable != null && x.Buildable.Equals(spawnerBuildable));
        return spawner != null;
    }

    public bool TryGetSpawner(string uniqueName, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawns.FirstOrDefault(x => x.SpawnInfo.UniqueName.Equals(uniqueName, StringComparison.OrdinalIgnoreCase));
        return spawner != null;
    }

    public VehicleSpawner? GetSpawner(uint signInstanceId)
    {
        return _spawns.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == signInstanceId));
    }

    public async UniTask<VehicleSpawner?> RegisterNewSpawner(IBuildable spawnerBuildable, WarfareVehicleInfo vehicleInfo, string uniqueName, CancellationToken token = default)
    {
        if (!_module.IsLayoutActive())
            return null;

        IServiceProvider layoutServiceProvider = _module.ScopedProvider.Resolve<IServiceProvider>();

        VehicleSpawnInfo spawnerInfo = new VehicleSpawnInfo
        {
            UniqueName = uniqueName,
            BuildableInstanceId = spawnerBuildable.InstanceId,
            VehicleAsset = vehicleInfo.VehicleAsset,
            SignInstanceIds = new List<uint>(),
            IsStructure = spawnerBuildable.IsStructure
        };

        await _spawnerStore.AddOrUpdateSpawnAsync(spawnerInfo, token); // todo: clean up async stuff
        VehicleSpawner spawner = new VehicleSpawner(spawnerInfo, vehicleInfo, _updateTicker, layoutServiceProvider);
        _spawns.Add(spawner);
        return spawner;
    }

    public async UniTask DeregisterSpawner(VehicleSpawner existingSpawner, CancellationToken token = default)
    {
        existingSpawner.Dispose();
        await _spawnerStore.RemoveSpawnAsync(existingSpawner.SpawnInfo, token); // todo: clean up async stuff
        _spawns.RemoveAll(s => s.SpawnInfo.BuildableInstanceId == existingSpawner.SpawnInfo.BuildableInstanceId);
    }

    public void ReloadSpawners(IReadOnlyList<VehicleSpawnInfo> records)
    {
        if (!_module.IsLayoutActive())
        {
            _logger.LogWarning("Could not get current Layout. Vehicle spawners will not be reloaded.");
            return;
        }

        Layout currentLayout = _module.GetActiveLayout();

        IServiceProvider layoutServiceProvider = currentLayout.ServiceProvider.Resolve<IServiceProvider>();

        _logger.LogDebug($"Reloading spawns from {records.Count} records...");
        // remove all spawners which are no longer saved
        var toRemove = _spawns.Where(s => !records.Any(r => r.BuildableInstanceId == s.Buildable?.InstanceId && r.IsStructure == s.Buildable.IsStructure)).ToList();
        foreach (VehicleSpawner spawner in toRemove)
        {
            spawner.Dispose();
            _spawns.Remove(spawner);
        }
        _logger.LogDebug($"Removed {toRemove.Count} spawns");

        // if each record has a corresponding spawner, reload it, otherwise add a new spawner
        foreach (var record in records)
        {
            WarfareVehicleInfo? vehicleInfo = _vehicleStore.GetVehicleInfo(record.VehicleAsset.GUID);
            if (vehicleInfo == null)
            {
                _logger.LogWarning($"Spawner '{record.UniqueName}' was saved with Vehicle asset {record.VehicleAsset.GUID} which no longer has registered info. This spawner will not be registered");
                return;
            }

            VehicleSpawner? existing = _spawns.FirstOrDefault(s => s.Buildable?.InstanceId == record.BuildableInstanceId && s.Buildable.IsStructure == record.IsStructure);
            if (existing != null)
            {
                if (existing.Layout == currentLayout)
                {
                    existing.SoftReload(record, vehicleInfo);
                    _logger.LogDebug($"Soft reloaded existing spawn: {vehicleInfo.VehicleAsset}");
                    continue;
                }

                existing.Dispose();
                _spawns.Remove(existing);
                _logger.LogDebug($"Replacing existing spawn: {vehicleInfo.VehicleAsset}");
            }
            else
            {
                _logger.LogDebug($"Adding new spawn: {vehicleInfo.VehicleAsset}");
            }

            VehicleSpawner newSpawner = new VehicleSpawner(record, vehicleInfo, _updateTicker, layoutServiceProvider);
            _spawns.Add(newSpawner);
        }

        _logger.LogDebug($"Successfully loaded {_spawns.Count} spawns");
    }
}