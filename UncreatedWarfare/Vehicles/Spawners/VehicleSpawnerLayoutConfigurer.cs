using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Vehicles.Spawners;
using static Uncreated.Warfare.Vehicles.VehicleSpawnerStore;

namespace Uncreated.Warfare.Vehicles;
public class VehicleSpawnerLayoutConfigurer : ILayoutHostedService
{
    private IConfiguration _configuration;
    public VehicleSpawnerLayoutConfigurer(IConfiguration configuration, ILogger<VehicleSpawnerLayoutConfigurer> logger)
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
