using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawnerLayoutConfigurer : ILayoutHostedService
{
    private readonly IConfiguration _configuration;
    public VehicleSpawnerLayoutConfigurer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public UniTask StartAsync(CancellationToken token) => UniTask.CompletedTask;

    public UniTask StopAsync(CancellationToken token) => UniTask.CompletedTask;

    public List<VehicleSpawnerLayoutConfiguration> GetEnabledSpawnerNames() => _configuration.GetRequiredSection("EnabledVehicleSpawners").Get<List<VehicleSpawnerLayoutConfiguration>>() ?? throw new Exception("Invalid EnabledVehicleSpawners config");

    public bool IsEnabledInLayout(VehicleSpawnInfo vehicleSpawnInfo) => GetEnabledSpawnerNames().Exists(s => vehicleSpawnInfo.UniqueName.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
    public bool TryGetSpawnerConfiguration(VehicleSpawnInfo vehicleSpawnInfo, [NotNullWhen(true)] out VehicleSpawnerLayoutConfiguration? configuration)
    {
        configuration = GetEnabledSpawnerNames().FirstOrDefault(s => vehicleSpawnInfo.UniqueName.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
        return configuration != null;
    }
}
