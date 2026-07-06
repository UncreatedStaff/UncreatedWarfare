using DanielWillett.ReflectionTools;
using DanielWillett.SpeedBytes;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Vehicles.Spawners;

[Priority(-3 /* run after vehicle storage services (VehicleInfoStore) */)]
public class VehicleSpawnerService : ILayoutHostedService, IDisposable
{
    private ImmutableArray<VehicleSpawner> _spawners;
    private readonly IVehicleSpawnerDataStore _spawnerStore;
    private readonly WarfareModule _module;
    private readonly ILogger<VehicleSpawnerService> _logger;
    private readonly ILoopTicker _updateTicker;
    private readonly string _spawnerBuildableMapFile;

    internal readonly Dictionary<string, SpawnerBuildables> SpawnerBuildableMap;
    internal bool SpawnerBuildableMapIsDirty;

    private bool _disposed;
    private bool _loaded;

    /// <summary>
    /// List of all active vehicle spawners.
    /// </summary>
    public ImmutableArray<VehicleSpawner> Spawners => _spawners;

    public VehicleSpawnerService(IVehicleSpawnerDataStore spawnerStore,
        ILoopTickerFactory loopTickerFactory,
        ILogger<VehicleSpawnerService> logger,
        WarfareModule module)
    {
        _spawners = ImmutableArray<VehicleSpawner>.Empty;
        SpawnerBuildableMap = new Dictionary<string, SpawnerBuildables>(0);
        _spawnerBuildableMapFile = Path.Combine(
            UnturnedPaths.RootDirectory.FullName,
            ServerSavedata.directoryName,
            Provider.serverID,
            "Level",
            Provider.map,
            "Spawner Buildables.bin"
        );

        try
        {
            ReadSpawnerBuildableMap();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading spawner buildable map.");
        }

        _spawnerStore = spawnerStore;
        _spawnerStore.OnDataUpdated += OnReloadSpawnersNeeded;

        _logger = logger;
        _module = module;
        _updateTicker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(1), false, true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;

        _spawnerStore.OnDataUpdated -= OnReloadSpawnersNeeded;
        if (_spawnerStore is IDisposable disp)
            disp.Dispose();

        _updateTicker.Dispose();

        foreach (VehicleSpawner spawner in _spawners)
        {
            spawner.Dispose();
        }

        _spawners = ImmutableArray<VehicleSpawner>.Empty;

        if (SpawnerBuildableMapIsDirty)
        {
            WriteSpawnerBuildableMap();
        }
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);
        
        IReadOnlyList<VehicleSpawnerInfo> info = await _spawnerStore.ReadSpawnersAsync(token);
        
        await UniTask.SwitchToMainThread(token);

        ReloadSpawners(info);
        _loaded = true;
    }

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await UniTask.SwitchToMainThread(token);

        VehicleManager.askVehicleDestroyAll();

        foreach (VehicleSpawner spawner in _spawners)
        {
            spawner.Dispose();
        }

        _spawners = ImmutableArray<VehicleSpawner>.Empty;
    }

    public bool TryGetSpawner(uint signInstanceId, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawners.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == signInstanceId));
        return spawner != null;
    }

    public bool TryGetSpawner(IBuildable spawnerBuildable, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawners.FirstOrDefault(x => x.Buildable != null && x.Buildable.Equals(spawnerBuildable));
        return spawner != null;
    }

    public bool TryGetSpawner(string uniqueName, [NotNullWhen(true)] out VehicleSpawner? spawner)
    {
        spawner = _spawners.FirstOrDefault(x => string.Equals(x.SpawnInfo.Id, uniqueName, StringComparison.OrdinalIgnoreCase));
        return spawner != null;
    }

    public VehicleSpawner? GetSpawner(uint signInstanceId)
    {
        return _spawners.FirstOrDefault(x => x.Signs.Any(x => x.InstanceId == signInstanceId));
    }

    private void OnReloadSpawnersNeeded()
    {
        if (!_loaded || _disposed)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            if (_disposed)
                return;
            try
            {
                IReadOnlyList<VehicleSpawnerInfo> spawners = await _spawnerStore.ReadSpawnersAsync();
                await UniTask.SwitchToMainThread();
                ReloadSpawners(spawners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading vehicle spawners.");
            }
        });
    }

    public void ReloadSpawners(IReadOnlyList<VehicleSpawnerInfo> records)
    {
        GameThread.AssertCurrent();

        if (!_module.IsLayoutActive())
        {
            _logger.LogWarning("Could not get current Layout. Vehicle spawners will not be reloaded.");
            return;
        }

        Layout currentLayout = _module.GetActiveLayout();

        IServiceProvider layoutServiceProvider = currentLayout.ServiceProvider.Resolve<IServiceProvider>();

        _logger.LogDebug($"Reloading spawns from {records.Count} records...");

        List<VehicleSpawner> newSpawners = [ with(_spawners.Length) ];
        List<VehicleSpawner> oldSpawners = [ with(_spawners.Length) ];

        BitArray useMask = new BitArray(records.Count);
        foreach (VehicleSpawner existingSpawner in _spawners)
        {
            VehicleSpawnerInfo? info = records.FirstOrDefault(
                out int index,
                r => string.Equals(r.Id, existingSpawner.SpawnInfo.Id)
            );

            if (info == null || existingSpawner.Layout != currentLayout)
            {
                oldSpawners.Add(existingSpawner);
                continue;
            }

            useMask[index] = true;
            try
            {
                existingSpawner.UpdateSpawnInfo(info);
            }
            catch (AssetNotFoundException)
            {
                _logger.LogWarning($"Vehicle asset not found for vehicle spawner with GUID {info.VehicleId}.");
                oldSpawners.Add(existingSpawner);
                continue;
            }

            newSpawners.Add(existingSpawner);
        }

        // remove all spawners which are no longer saved
        foreach (VehicleSpawner spawner in oldSpawners)
        {
            spawner.Dispose();
        }

        _logger.LogDebug($"Removed {oldSpawners.Count} spawns");

        // if each record has a corresponding spawner, reload it, otherwise add a new spawner
        for (int i = 0; i < records.Count; i++)
        {
            if (useMask[i])
            {
                // already reloaded
                continue;
            }

            VehicleSpawnerInfo? record = records[i];

            VehicleSpawner newSpawner;
            try
            {
                newSpawner = new VehicleSpawner(record, _updateTicker, layoutServiceProvider);
            }
            catch (AssetNotFoundException)
            {
                _logger.LogWarning($"Vehicle asset not found for vehicle spawner with GUID {record.VehicleId}.");
                continue;
            }

            newSpawners.Add(newSpawner);
        }

        _spawners = newSpawners.ToImmutableArray();

        if (SpawnerBuildableMapIsDirty)
        {
            WriteSpawnerBuildableMap();
        }

        _logger.LogDebug($"Successfully loaded {newSpawners.Count} vehicle spawn(s).");
    }

    private void ReadSpawnerBuildableMap()
    {
        lock (SpawnerBuildableMap)
        {
            SpawnerBuildableMap.Clear();

            try
            {
                using FileStream fs = new FileStream(_spawnerBuildableMapFile, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan);
                ByteReader reader = new ByteReader
                {
                    ThrowOnError = true,
                    LogOnError = false
                };

                reader.LoadNew(fs);

                reader.ReadUInt8();

                Span<byte> buffer = stackalloc byte[8];

                int count = checked ( (int)reader.ReadUInt32() );

                SpawnerBuildableMap.EnsureCapacity(count);

                for (int i = 0; i < count; ++i)
                {
                    string key = reader.ReadString();

                    SpawnerBuildables buildables;
                    reader.ReadBlockTo(buffer);
                    buildables.Bay = new BuildableDescriptor(buffer);

                    byte ctHdr = reader.ReadUInt8();
                    uint ct = (ctHdr & 128) != 0 ? (uint)(ctHdr & 127) : reader.ReadUInt32();

                    BuildableDescriptor[] signs = new BuildableDescriptor[ct];
                    for (uint j = 0; j < ct; ++j)
                    {
                        reader.ReadBlockTo(buffer);
                        signs[j] = new BuildableDescriptor(buffer);
                    }

                    buildables.Signs = signs;

                    SpawnerBuildableMap[key] = buildables;
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (ByteBufferOverflowException)
            {
                _logger.LogWarning("Invalid file format of vehicle spawner buildable map file.");
            }
            catch (OverflowException)
            {
                _logger.LogWarning("Invalid data in vehicle spawner buildable map file.");
            }
        }
    }

    private void WriteSpawnerBuildableMap()
    {
        lock (SpawnerBuildableMap)
        {
            string? dir = Path.GetDirectoryName(_spawnerBuildableMapFile);
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            Thread.BeginCriticalRegion();
            try
            {
                using FileStream fs = new FileStream(_spawnerBuildableMapFile, FileMode.Create, FileAccess.Write, FileShare.Read, 512, FileOptions.SequentialScan);
                ByteWriter writer = new ByteWriter
                {
                    Stream = fs
                };

                writer.Write((byte)0);
                Span<byte> buffer = stackalloc byte[8];
                writer.Write((uint)SpawnerBuildableMap.Count);
                foreach ((string key, SpawnerBuildables buildables) in SpawnerBuildableMap)
                {
                    writer.Write(key);

                    buildables.Bay.TryWriteBytes(buffer, out _);
                    writer.WriteBlock(buffer);
                    if (buildables.Signs == null)
                    {
                        writer.Write((byte)128);
                        continue;
                    }
                    
                    if (buildables.Signs.Length < 128)
                    {
                        writer.Write((byte)(buildables.Signs.Length | 128));
                    }
                    else
                    {
                        writer.Write((byte)0);
                        writer.Write((uint)buildables.Signs.Length);
                    }

                    for (int i = 0; i < buildables.Signs.Length; ++i)
                    {
                        buildables.Signs[i].TryWriteBytes(buffer, out _);
                        writer.WriteBlock(buffer);
                    }
                }

                writer.Flush();
                writer.Stream = null;
                SpawnerBuildableMapIsDirty = false;
            }
            finally
            {
                Thread.EndCriticalRegion();
            }
        }
    }

    internal struct SpawnerBuildables
    {
        public BuildableDescriptor Bay;
        public BuildableDescriptor[] Signs;
    }
}