using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Vehicles;
public class VehicleInfoStore : IHostedService, IDisposable, IUnlockRequirementProvider
{
    private readonly WarfareModule _warfare;
    private readonly ILogger<VehicleInfoStore> _logger;

    private readonly PhysicalFileProvider _fileProvider;

    private List<WarfareVehicleInfo> _vehicles = new List<WarfareVehicleInfo>(32);
    private readonly HashSet<string> _watchedFiles = new HashSet<string>(32);
    private List<IDisposable> _disposableConfigurationRoots = new List<IDisposable>(32);

    /// <summary>
    /// List of vehicles as configured based on the current map.
    /// </summary>
    public IReadOnlyList<WarfareVehicleInfo> Vehicles { get; private set; }

    public VehicleInfoStore(WarfareModule warfare, ILogger<VehicleInfoStore> logger)
    {
        _warfare = warfare;
        _logger = logger;
        _fileProvider = new PhysicalFileProvider(Path.Join(_warfare.HomeDirectory, "Vehicles"));
        Vehicles = new ReadOnlyCollection<WarfareVehicleInfo>(_vehicles);
    }

    /// <summary>
    /// Finds the vehicle info associated with the given <paramref name="guid"/>, if it exists.
    /// </summary>
    public WarfareVehicleInfo? GetVehicleInfo(Guid guid)
    {
        return guid == Guid.Empty ? null : _vehicles.Find(x => x.Vehicle.MatchGuid(guid));
    }

    /// <summary>
    /// Finds the vehicle info associated with the given <paramref name="asset"/>, if it exists.
    /// </summary>
    public WarfareVehicleInfo? GetVehicleInfo(VehicleAsset? asset)
    {
        return asset == null ? null : _vehicles.Find(x => x.Vehicle.MatchAsset(asset));
    }

    /// <summary>
    /// Finds the vehicle info associated with the given <paramref name="asset"/>, if it exists.
    /// </summary>
    public WarfareVehicleInfo? GetVehicleInfo(IAssetLink<VehicleAsset>? asset)
    {
        return asset == null ? null : _vehicles.Find(x => x.Vehicle.MatchAsset(asset));
    }

    /// <inheritdoc />
    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        string[] files = Directory.GetFiles(_fileProvider.Root, "*.yml", SearchOption.AllDirectories);

        foreach (string file in files)
        {
            if (!YamlUtility.CheckMatchesMapFilter(file))
                continue;

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddYamlFile(_fileProvider, file, false, true)
                .Build();

            WarfareVehicleInfo vehicle = config.Get<WarfareVehicleInfo>();
            if (vehicle.Vehicle == null)
            {
                _logger.LogWarning("Invalid file {0} missing 'Vehicle' property.", file);
                continue;
            }

            vehicle.Configuration = config;

            config.GetReloadToken().RegisterChangeCallback(ReloadVehicleInfoConfiguration, vehicle);

            lock (_watchedFiles)
            {
                _watchedFiles.Add(file);
            }

            if (config is IDisposable disposableConfig)
                _disposableConfigurationRoots.Add(disposableConfig);

            _vehicles.Add(vehicle);
            _logger.LogDebug("Found vehicle info in file: {0}.", file);
        }

        
        IDisposable changeTokenRegistration = ChangeToken.OnChange(() => _fileProvider.Watch("./*.yml"), ReloadUnwatchedFiles);

        _disposableConfigurationRoots.Add(changeTokenRegistration);

        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        Dispose();
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Handles any new files that are added later.
    /// </summary>
    private void ReloadUnwatchedFiles()
    {
        string[] files = Directory.GetFiles(_fileProvider.Root, "*.yml", SearchOption.AllDirectories);

        List<IDisposable> newDisposables = new List<IDisposable>();
        List<WarfareVehicleInfo> newVehicles = new List<WarfareVehicleInfo>();

        foreach (string file in files)
        {
            lock (_watchedFiles)
            {
                if (_watchedFiles.Contains(file))
                    continue;
            }

            try
            {
                if (!YamlUtility.CheckMatchesMapFilter(file))
                    continue;

                IConfigurationRoot config = new ConfigurationBuilder()
                    .AddYamlFile(_fileProvider, file, false, true)
                    .Build();

                WarfareVehicleInfo vehicle = config.Get<WarfareVehicleInfo>();
                if (vehicle.Vehicle == null)
                {
                    _logger.LogWarning("Invalid file {0} missing 'Vehicle' property.", file);
                    continue;
                }

                lock (_watchedFiles)
                {
                    if (!_watchedFiles.Add(file))
                    {
                        if (config is IDisposable disposableConfig)
                            disposableConfig.Dispose();
                        continue;
                    }
                }

                vehicle.Configuration = config;

                config.GetReloadToken().RegisterChangeCallback(ReloadVehicleInfoConfiguration, vehicle);

                if (config is IDisposable disposableConfig2)
                    newDisposables.Add(disposableConfig2);

                newVehicles.Add(vehicle);
                _logger.LogInformation("Found vehicle info in new file: {0}.", file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping file {0}, exception encountered.", file);
            }
        }

        if (newVehicles.Count == 0)
            return;

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            _disposableConfigurationRoots.AddRange(newDisposables);

            List<WarfareVehicleInfo> vehicles = new List<WarfareVehicleInfo>(_vehicles.Count + newVehicles.Count);
            vehicles.AddRange(_vehicles);
            foreach (WarfareVehicleInfo vehicle in newVehicles)
            {
                int existingIndex = vehicles.FindIndex(info => info.Equals(vehicle));
                if (existingIndex == -1)
                {
                    vehicles.Add(vehicle);
                }
                else
                {
                    vehicles[existingIndex] = vehicle;
                }
            }

            _vehicles = vehicles;
            Vehicles = new ReadOnlyCollection<WarfareVehicleInfo>(_vehicles);
        });
    }

    /// <summary>
    /// Invoked when a vehicle's file is changed.
    /// </summary>
    private void ReloadVehicleInfoConfiguration(object stateBox)
    {
        WarfareVehicleInfo state = (WarfareVehicleInfo)stateBox;

        // not binding for thread safety reasons
        WarfareVehicleInfo newVehicle = state.Configuration.Get<WarfareVehicleInfo>();

        if (newVehicle.Vehicle.Equals(state.Vehicle))
        {
            newVehicle.Configuration = state.Configuration;

            newVehicle.DependantInfo = new WeakReference<WarfareVehicleInfo>(state);
            state.UpdateFrom(newVehicle);
        }

        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread(_warfare.UnloadToken);

            int index = _vehicles.FindIndex(x => ReferenceEquals(x, state));
            if (index == -1)
            {
                _vehicles = new List<WarfareVehicleInfo>(_vehicles) { newVehicle };
                Vehicles = new ReadOnlyCollection<WarfareVehicleInfo>(_vehicles);
            }
            else
            {
                _vehicles[index] = newVehicle;
            }
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileProvider.Dispose();

        List<IDisposable> toDispose = Interlocked.Exchange(ref _disposableConfigurationRoots, [ ]);

        for (int i = 0; i < toDispose.Count; i++)
            toDispose[i].Dispose();
    }

    /// <inheritdoc />
    IEnumerable<UnlockRequirement> IUnlockRequirementProvider.UnlockRequirements => _vehicles.SelectMany(x => x.UnlockRequirements);
}