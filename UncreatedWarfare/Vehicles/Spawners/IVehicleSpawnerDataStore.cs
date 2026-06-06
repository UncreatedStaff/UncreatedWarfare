namespace Uncreated.Warfare.Vehicles.Spawners;

/// <summary>
/// Reads the vehicle spawners for a map.
/// </summary>
public interface IVehicleSpawnerDataStore
{
    /// <summary>
    /// Invoked when the data should be reloaded (if supported).
    /// </summary>
    event Action OnDataUpdated;

    /// <summary>
    /// Read a list of all available spawners.
    /// </summary>
    ValueTask<IReadOnlyList<VehicleSpawnerInfo>> ReadSpawnersAsync(CancellationToken token = default);
}