using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Vehicles.Spawners;
using static Uncreated.Warfare.Vehicles.VehicleSpawnerStore;

namespace Uncreated.Warfare.Vehicles;
public class VehicleSpawnerSelector : ILayoutHostedService
{
    private IConfiguration _configuration;
    public VehicleSpawnerSelector(IConfiguration configuration, ILogger<VehicleSpawnerSelector> logger)
    {
        _configuration = configuration;
    }

    public UniTask StartAsync(CancellationToken token) => UniTask.CompletedTask;

    public UniTask StopAsync(CancellationToken token) => UniTask.CompletedTask;

    public List<string> GetEnabledSpawnerNames() => _configuration.GetRequiredSection("EnabledVehicleSpawners").Get<List<string>>() ?? throw new Exception("Invalid EnabledVehicleSpawners config");

    public bool IsEnabledInLayout(VehicleSpawnInfo vehicleSpawnInfo) => GetEnabledSpawnerNames().Exists(s => vehicleSpawnInfo.UniqueName.Equals(s, StringComparison.OrdinalIgnoreCase));
}
