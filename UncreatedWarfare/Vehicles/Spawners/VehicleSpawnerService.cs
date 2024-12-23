using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Vehicles.Spawners
{
    [Priority(-2 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
    public class VehicleSpawnerService : ILayoutHostedService
    {
        private readonly TrackingList<VehicleSpawner> _spawns;
        private readonly VehicleSpawnerStore _spawnerStore;
        private readonly VehicleInfoStore _vehicleStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleSpawnerService> _logger;
        private readonly ILoopTicker _updateTicker;

        public VehicleSpawnerStore SpawnerStore => _spawnerStore;
        public readonly ReadOnlyTrackingList<VehicleSpawner> Spawners;

        public VehicleSpawnerService(IServiceProvider serviceProvider, ILogger<VehicleSpawnerService> logger)
        {
            _spawns = new TrackingList<VehicleSpawner>();
            _spawnerStore = serviceProvider.GetRequiredService<VehicleSpawnerStore>();
            _spawnerStore.OnSpawnsReloaded = () => ReloadSpawners(_spawnerStore.Spawns);
            _vehicleStore = serviceProvider.GetRequiredService<VehicleInfoStore>();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _updateTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>()
                .CreateTicker(TimeSpan.FromSeconds(1), false, true);

            Spawners = new ReadOnlyTrackingList<VehicleSpawner>(_spawns);
        }

        public UniTask StartAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public UniTask StopAsync(CancellationToken token)
        {
            _updateTicker.Dispose();
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
        public VehicleSpawner? GetSpawner(uint signInstanceId) => _spawns.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == signInstanceId));
        public async UniTask<VehicleSpawner> RegisterNewSpawner(IBuildable spawnerBuildable, WarfareVehicleInfo vehicleInfo, string uniqueName, CancellationToken token = default)
        {
            IServiceProvider layoutServiceProvider = _serviceProvider.GetRequiredService<Layout>().ServiceProvider.Resolve<IServiceProvider>();

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
            Layout? currentLayout = _serviceProvider.GetService<Layout>();

            if (currentLayout == null)
            {
                _logger.LogWarning($"Could not get current Layout. Vehicle spawners will not be reloaded.");
                return;
            }

            IServiceProvider layoutServiceProvider = currentLayout.ServiceProvider.Resolve<IServiceProvider>();

            _logger.LogDebug($"Reloading spawns from {records.Count} records...");
            // remove all spawners which are no longer saved
            var toRemove = _spawns.Where(s => !records.Any(r => r.BuildableInstanceId == s.Buildable?.InstanceId && r.IsStructure == s.Buildable.IsStructure)).ToList();
            foreach (VehicleSpawner spawner in toRemove)
            {
                spawner.Dispose();
                _spawns.Remove(spawner);
            }
            _logger.LogDebug($"Removed {toRemove.Count()} spawns");

            // if each record has a corresponding spawner, reload it, otherwise add a new spawner
            foreach (var record in records)
            {
                WarfareVehicleInfo? vehicleInfo = _vehicleStore.GetVehicleInfo(record.VehicleAsset.GUID);
                if (vehicleInfo == null)
                {
                    _logger.LogWarning($"Spawner '{record.UniqueName}' was saved with Vehicle asset {record.VehicleAsset.GUID} which no longer has registered info. This spawner will not be registered");
                    return;
                }

                VehicleSpawner existing = _spawns.FirstOrDefault(s => s.Buildable?.InstanceId == record.BuildableInstanceId && s.Buildable.IsStructure ==  record.IsStructure); 
                if (existing != null)
                {
                    existing.SoftReload(record, vehicleInfo);
                    _logger.LogDebug($"Soft reloaded existing spawn: {existing}");
                }
                else
                {
                    VehicleSpawner newSpawner = new VehicleSpawner(record, vehicleInfo, _updateTicker, layoutServiceProvider);
                    _spawns.Add(newSpawner);
                    _logger.LogDebug($"Added new spawn: {newSpawner}");
                }
            }

            _logger.LogDebug($"Successfully loaded {_spawns.Count} spawns");
        }
    }
}
