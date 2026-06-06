using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Uncreated.Warfare.Vehicles.Spawners;

/// <summary>
/// Provider for vehicle spawners that reads them from the map's <c>Uncreated/vehicle_bays.json</c> file.
/// </summary>
public sealed class MapVehicleSpawnerDataStore : IVehicleSpawnerDataStore
{
    private readonly ILogger<MapVehicleSpawnerDataStore> _logger;

    public MapVehicleSpawnerDataStore(ILogger<MapVehicleSpawnerDataStore> logger)
    {
        _logger = logger;
    }

    event Action? IVehicleSpawnerDataStore.OnDataUpdated { add { } remove { } } // no-op

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<VehicleSpawnerInfo>> ReadSpawnersAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        return new ValueTask<IReadOnlyList<VehicleSpawnerInfo>>(ReadSpawners());
    }

    public IReadOnlyList<VehicleSpawnerInfo> ReadSpawners(string? levelPath = null)
    {
        string mapFolder = Path.Combine(levelPath ?? Level.info.path, "Uncreated");

        string vehicleBaysPath = Path.Combine(mapFolder, "vehicle_bays.json");
        string vehicleBaySignsPath = Path.Combine(mapFolder, "vehicle_bay_signs.json");

        if (!File.Exists(vehicleBaysPath))
        {
            _logger.LogError($"Vehicle spawners file {vehicleBaysPath} not found.");
            return Array.Empty<VehicleSpawnerInfo>();
        }

        List<VehicleSpawnerInfo>? spawners;
        try
        {
            using FileStream fs = new FileStream(vehicleBaysPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan);
            spawners = (List<VehicleSpawnerInfo>?)JsonSerializer.Deserialize(fs, typeof(List<VehicleSpawnerInfo>), VehicleSpawnerInfoSerializeContext.Default);
            if (spawners == null)
            {
                _logger.LogError($"Invalid vehicle spawners file: {vehicleBaysPath}.");
                return Array.Empty<VehicleSpawnerInfo>();
            }

            int removed = spawners.RemoveAll(x =>
            {
                if (x == null)
                    return true;

                return !Regions.checkSafe(x.Position);
            });

            if (removed > 0)
            {
                _logger.LogWarning($"Removed {removed} invalid vehicle spawner(s).");
            }

            if (spawners.Count == 0)
            {
                _logger.LogError($"No valid vehicle spawners available in file: {vehicleBaysPath}.");
            }
            else
            {
                _logger.LogInformation($"Discovered {spawners.Count} vehicle spawner(s).");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading vehicle spawner file: {vehicleBaysPath}.");
            return Array.Empty<VehicleSpawnerInfo>();
        }

        if (!File.Exists(vehicleBaySignsPath))
            return spawners;

        try
        {
            using FileStream fs = new FileStream(vehicleBaySignsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024, FileOptions.SequentialScan);
            List<VehicleSpawnerSignInfo>? signs = (List<VehicleSpawnerSignInfo>?)JsonSerializer.Deserialize(fs, typeof(List<VehicleSpawnerSignInfo>), VehicleSpawnerInfoSerializeContext.Default);
            if (signs == null)
            {
                _logger.LogError($"Invalid vehicle spawner signs file: {vehicleBaySignsPath}.");
                return Array.Empty<VehicleSpawnerInfo>();
            }

            int removed = signs.RemoveAll(x =>
            {
                if (x == null)
                    return true;

                return !Regions.checkSafe(x.Position);
            });

            if (removed > 0)
            {
                _logger.LogWarning($"Removed {removed} invalid vehicle spawner sign(s).");
            }

            List<VehicleSpawnerSignInfo> buffer = new List<VehicleSpawnerSignInfo>(2);
            foreach (IGrouping<string, VehicleSpawnerSignInfo> group in signs.GroupBy(x => x.VehicleSpawnerId, StringComparer.OrdinalIgnoreCase))
            {
                VehicleSpawnerInfo? spawner = spawners.Find(x => string.Equals(x.Id, group.Key, StringComparison.OrdinalIgnoreCase));

                if (spawner == null)
                {
                    _logger.LogWarning($"Unknown vehicle spawner \"{group.Key}\" in spawner signs file: {vehicleBaySignsPath}.");
                    continue;
                }

                buffer.AddRange(group);
                spawner.Signs = buffer.ToImmutableArray();
                buffer.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading vehicle spawner signs file: {vehicleBaySignsPath}.");
            return Array.Empty<VehicleSpawnerInfo>();
        }

        return spawners;
    }
}
