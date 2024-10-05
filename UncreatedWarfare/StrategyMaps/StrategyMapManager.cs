using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.StrategyMaps;
internal class StrategyMapManager : ILayoutHostedService, IEventListenerProvider, IEventListener<BarricadePlaced>, IEventListener<BarricadeDestroyed>
{
    private readonly TrackingList<StrategyMap> _strategyMaps;
    private readonly ILogger<StrategyMapManager> _logger;
    private readonly StrategyMapsConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;

    public StrategyMapManager(ILogger<StrategyMapManager> logger, IServiceProvider serviceProvider)
    {
        _strategyMaps = new TrackingList<StrategyMap>();
        _logger = logger;
        _configuration = serviceProvider.GetRequiredService<StrategyMapsConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();

        var mapTables = _configuration.GetSection("MapTables").Get<List<MapTableInfo>>();

        _logger.LogInformation("Configured MapTables: " + mapTables.Count);
    }

    public UniTask StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
    public IEnumerable<IAsyncEventListener<TEventArgs>> EnumerateAsyncListeners<TEventArgs>(TEventArgs args) => _strategyMaps.OfType<IAsyncEventListener<TEventArgs>>();
    public IEnumerable<IEventListener<TEventArgs>> EnumerateNormalListeners<TEventArgs>(TEventArgs args) => _strategyMaps.OfType<IEventListener<TEventArgs>>();

    public void RegisterStrategyMap(IBuildable buildable, MapTableInfo tableInfo)
    {
        StrategyMap map = new StrategyMap(buildable, tableInfo);
        _strategyMaps.AddIfNotExists(map);
        _logger.LogDebug($"Registered new StrategyMap: {map}");
    }
    public void DeregisterStrategyMap(IBuildable buildable)
    {
        StrategyMap? map = _strategyMaps.FindAndRemove(m => m.MapTable.Equals(buildable));
        _logger.LogDebug($"Deregistered StrategyMap: {map}");
    }
    public StrategyMap? GetStrategyMap(uint buildableInstanceId) => _strategyMaps.FirstOrDefault(m => m.MapTable.InstanceId == buildableInstanceId);

    public void HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        MapTableInfo? mapTableInfo = _configuration.GetSection("MapTables").Get<List<MapTableInfo>>()
            .FirstOrDefault(m => m.BuildableGuid == e.Buildable.Asset.GUID);

        if (mapTableInfo == null)
            return;

        RegisterStrategyMap(e.Buildable, mapTableInfo);
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        bool isMapTableBuildable = _configuration.GetSection("MapTables").Get<List<MapTableInfo>>()
            .Any(m => m.BuildableGuid == e.Buildable.Asset.GUID);

        if (!isMapTableBuildable)
            return;

        DeregisterStrategyMap(e.Buildable);
    }
}
