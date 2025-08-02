using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        if (_configuration.GetValue<string?>("Import", null) is { Length: > 0 } filePath)
        {
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

        return config.GetRequiredSection("EnabledVehicleSpawners")
                     .Get<List<VehicleSpawnerLayoutConfiguration>>() ?? throw new Exception("Invalid EnabledVehicleSpawners config");
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
