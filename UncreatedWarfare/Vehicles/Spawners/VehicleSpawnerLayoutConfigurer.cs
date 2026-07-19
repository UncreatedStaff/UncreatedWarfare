using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Vehicles.Spawners;

public class VehicleSpawnerLayoutConfigurer : ILayoutHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly Layout _layout;
    private readonly WarfareModule _warfareModule;
    private IDisposable? _reloadToken;
    private IDisposable? _importedConfigReloadToken;

    private IConfigurationRoot? _importedConfig;
    private string? _importedConfigPath;

    /// <summary>
    /// List of all vehicle spawners that are enabled along with their configuration.
    /// </summary>
    public List<VehicleSpawnerLayoutConfiguration> EnabledSpawnerLayouts { get; private set; }

    public VehicleSpawnerLayoutConfigurer(IConfiguration configuration, Layout layout, WarfareModule warfareModule)
    {
        _configuration = configuration;
        _layout = layout;
        _warfareModule = warfareModule;
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
        Interlocked.Exchange(ref _importedConfigReloadToken, null)?.Dispose();
        (Interlocked.Exchange(ref _importedConfig, null) as IDisposable)?.Dispose();
        return UniTask.CompletedTask;
    }

    private void OnReload()
    {
        EnabledSpawnerLayouts = GetEnabledSpawnerNames();
    }

    public List<VehicleSpawnerLayoutConfiguration> GetEnabledSpawnerNames()
    {
        IConfiguration config = _configuration;
        List<VehicleSpawnerLayoutConfiguration>? enabled = null;
        List<string>? disabled = null;
        if (_configuration.GetValue<string?>("Import", null) is { Length: > 0 } filePath)
        {
            enabled = config.GetSection("EnabledVehicleSpawners").Get<List<VehicleSpawnerLayoutConfiguration>>();
            disabled = config.GetSection("DisabledVehicleSpawners").Get<List<string>>();

            string relativePath = _layout.LayoutInfo.ResolveRelativePath(filePath);
            if (_importedConfig == null || !string.Equals(_importedConfigPath, relativePath, StringComparison.Ordinal))
            {
                _importedConfigPath = relativePath;
                IConfigurationBuilder builder = new ConfigurationBuilder();
                ConfigurationHelper.AddJsonOrYamlFile(builder, _warfareModule.FileProvider, relativePath, reloadOnChange: true);

                IConfigurationRoot importedConfig = builder.Build();
                (Interlocked.Exchange(ref _importedConfig, importedConfig) as IDisposable)?.Dispose();
                Interlocked.Exchange(ref _importedConfigReloadToken, ChangeToken.OnChange(importedConfig.GetReloadToken, OnReload))?.Dispose();
            }

            config = _importedConfig!;
        }

        List<VehicleSpawnerLayoutConfiguration> spawners = config.GetRequiredSection("EnabledVehicleSpawners").Get<List<VehicleSpawnerLayoutConfiguration>>()
                                                           ?? throw new Exception("Invalid EnabledVehicleSpawners config");

        if (enabled is { Count: > 0 })
        {
            foreach (VehicleSpawnerLayoutConfiguration vehicle in enabled)
            {
                int index = spawners.FindIndex(x => string.Equals(x.SpawnerName, vehicle.SpawnerName, StringComparison.Ordinal));
                if (index < 0)
                    spawners.Add(vehicle);
                else
                    spawners[index] = vehicle;
            }
        }

        if (disabled is { Count: > 0 })
        {
            foreach (string spawnerName in disabled)
            {
                int index = spawners.FindIndex(x => string.Equals(x.SpawnerName, spawnerName, StringComparison.Ordinal));
                if (index < 0)
                    continue;
                
                spawners[index] = spawners[^1];
                spawners.RemoveAt(spawners.Count - 1);
            }
        }

        return spawners;
    }

    public bool IsEnabledInLayout(VehicleSpawnerInfo vehicleSpawnInfo)
    {
        return EnabledSpawnerLayouts.Exists(s =>
            vehicleSpawnInfo.Id.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGetSpawnerConfiguration(VehicleSpawnerInfo vehicleSpawnInfo, [NotNullWhen(true)] out VehicleSpawnerLayoutConfiguration? configuration)
    {
        configuration = EnabledSpawnerLayouts.FirstOrDefault(s => vehicleSpawnInfo.Id.Equals(s.SpawnerName, StringComparison.OrdinalIgnoreCase));
        return configuration != null;
    }

    public void Dispose()
    {
        _reloadToken?.Dispose();
        _reloadToken = null;
    }
}
