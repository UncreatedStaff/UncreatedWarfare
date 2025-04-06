using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawnerLayoutConfigurer : ILayoutHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private IDisposable? _reloadToken;

    /// <summary>
    /// List of all vehicle spawners that are enabled along with their configuration.
    /// </summary>
    public List<VehicleSpawnerLayoutConfiguration> EnabledSpawnerLayouts { get; private set; }

    public VehicleSpawnerLayoutConfigurer(IConfiguration configuration)
    {
        _configuration = configuration;
        EnabledSpawnerLayouts = GetEnabledSpawnerNames();
    }

    public UniTask StartAsync(CancellationToken token)
    {
        _reloadToken ??= ChangeToken.OnChange(
            _configuration.GetReloadToken,
            OnReload
        );
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        _reloadToken?.Dispose();
        _reloadToken = null;
        return UniTask.CompletedTask;
    }

    private void OnReload()
    {
        EnabledSpawnerLayouts = GetEnabledSpawnerNames();
    }

    public List<VehicleSpawnerLayoutConfiguration> GetEnabledSpawnerNames()
    {
        return _configuration.GetRequiredSection("EnabledVehicleSpawners").Get<List<VehicleSpawnerLayoutConfiguration>>()
               ?? throw new Exception("Invalid EnabledVehicleSpawners config");
    }

    public bool IsEnabledInLayout(VehicleSpawnInfo vehicleSpawnInfo)
    {
        return EnabledSpawnerLayouts.Exists(s =>
            vehicleSpawnInfo.UniqueName.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetSpawnerConfiguration(VehicleSpawnInfo vehicleSpawnInfo, [NotNullWhen(true)] out VehicleSpawnerLayoutConfiguration? configuration)
    {
        configuration = EnabledSpawnerLayouts.FirstOrDefault(s => vehicleSpawnInfo.UniqueName.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
        return configuration != null;
    }

    public void Dispose()
    {
        _reloadToken?.Dispose();
        _reloadToken = null;
    }
}
