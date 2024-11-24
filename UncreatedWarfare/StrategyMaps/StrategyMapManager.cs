using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;
using static Humanizer.On;

namespace Uncreated.Warfare.StrategyMaps;
public class StrategyMapManager : 
    ILayoutHostedService,
    IEventListenerProvider,
    IEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<FobRegistered>,
    IEventListener<FobDeregistered>, 
    IEventListener<FobBuilt>, 
    IEventListener<FobDestroyed>, 
    IEventListener<FobProxyChanged>,
    IEventListener<FlagsSetUp>,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>
{
    private readonly TrackingList<StrategyMap> _strategyMaps;
    private readonly ILogger<StrategyMapManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StrategyMapsConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;

    public StrategyMapManager(ILogger<StrategyMapManager> logger, IServiceProvider serviceProvider)
    {
        _strategyMaps = new TrackingList<StrategyMap>();
        _logger = logger;

        _serviceProvider = serviceProvider;
        _configuration = serviceProvider.GetRequiredService<StrategyMapsConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();

        var mapTables = _configuration.GetSection("MapTables").Get<List<MapTableInfo>>();

        _logger.LogInformation("Configured Strategy MapTables: " + mapTables?.Count ?? "config not found");
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
        tableInfo.BuildableAsset.AssertValid();

        StrategyMap map = new StrategyMap(buildable, tableInfo);
        _strategyMaps.AddIfNotExists(map);

        DualSidedFlagService? flagService = _serviceProvider.GetService<DualSidedFlagService>();
        FobManager? fobManager = _serviceProvider.GetService<FobManager>();

        if (flagService != null)
            RepopulateFlagTacks(map, flagService);
        if (fobManager != null)
            RepopulateBuildableFobTacks(map, fobManager);

        _logger.LogDebug($"Registered new StrategyMap: {map}");
    }
    public void DeregisterStrategyMap(IBuildable buildable)
    {
        StrategyMap? map = _strategyMaps.FindAndRemove(m => m.MapTable.Equals(buildable));
        map?.Dispose();
        _logger.LogDebug($"Deregistered StrategyMap: {map}");
    }
    public IEnumerable<StrategyMap> StrategyMapsOfTeam(Team team) => _strategyMaps.Where(s => s.MapTable.Group == team.GroupId);
    public StrategyMap? GetStrategyMap(uint buildableInstanceId) => _strategyMaps.FirstOrDefault(m => m.MapTable.InstanceId == buildableInstanceId);

    public void HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        MapTableInfo? mapTableInfo = _configuration.GetRequiredSection("MapTables").Get<List<MapTableInfo>>()
            .FirstOrDefault(m => m.BuildableAsset.Guid == e.Buildable.Asset.GUID);

        if (mapTableInfo == null)
            return;

        RegisterStrategyMap(e.Buildable, mapTableInfo);
    }
    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        bool isMapTableBuildable = _configuration.GetRequiredSection("MapTables").Get<List<MapTableInfo>>()
            .Any(m => m.BuildableAsset.Guid == e.Buildable.Asset.GUID);

        if (!isMapTableBuildable)
            return;

        DeregisterStrategyMap(e.Buildable);
    }

    public void HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        MapTack newTack;
        if (e.Fob is BuildableFob)
            newTack = new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobUnbuilt"), e.Fob);
        else
            return;

        foreach (StrategyMap map in StrategyMapsOfTeam(e.Fob.Team))
        {
            map.AddMapTack(newTack);
        }
    }

    public void HandleEvent(FobDeregistered e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in StrategyMapsOfTeam(e.Fob.Team))
        {
            map.RemoveMapTacks(m => m is DeployableMapTack fm && fm.Deployable == e.Fob);
        }
    }

    public void HandleEvent(FobBuilt e, IServiceProvider serviceProvider)
    {
        MapTack newTack;
        if (e.Fob is BuildableFob)
            newTack = new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:Fob"), e.Fob);
        else
            return;

        foreach (StrategyMap map in StrategyMapsOfTeam(e.Fob.Team))
        {
            map.RemoveMapTacks(m => m is DeployableMapTack fm && fm.Deployable == e.Fob);
            map.AddMapTack(newTack);
        }
    }

    public void HandleEvent(FobDestroyed e, IServiceProvider serviceProvider)
    {
        MapTack newTack;
        if (e.Fob is BuildableFob)
            newTack = new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobUnbuilt"), e.Fob);
        else
            return;

        foreach (StrategyMap map in StrategyMapsOfTeam(e.Fob.Team))
        {
            map.RemoveMapTacks(m => m is DeployableMapTack fm && fm.Deployable == e.Fob);
            map.AddMapTack(newTack);
        }
    }

    public void HandleEvent(FobProxyChanged e, IServiceProvider serviceProvider)
    {
        MapTack newTack;
        if (e.Fob is BuildableFob bf && bf.IsBuilt)
            newTack = new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:" + (e.IsProxied ? "FobProxied" : "Fob")), e.Fob);
        else
            return;

        foreach (StrategyMap map in StrategyMapsOfTeam(e.Fob.Team))
        {
            map.RemoveMapTacks(m => m is DeployableMapTack fm && fm.Deployable == e.Fob);
            map.AddMapTack(newTack);
        }
    }
    public void HandleEvent(FlagsSetUp e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            RepopulateFlagTacks(map, e.FlagService);
        }
    }
    public void HandleEvent(FlagCaptured e, IServiceProvider serviceProvider) // todo: move flag and fob stuff into a partial class maybe?
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            map.RemoveMapTacks(m => m is FlagMapTack fm && fm.Flag == e.Flag);
            map.AddMapTack(CreateFlagTack(e.Flag));
        }
    }
    public void HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            map.RemoveMapTacks(m => m is FlagMapTack fm && fm.Flag == e.Flag);
            map.AddMapTack(CreateFlagTack(e.Flag));
        }
    }
    private void RepopulateFlagTacks(StrategyMap map, DualSidedFlagService flagService)
    {
        map.RemoveMapTacks(m => m is FlagMapTack);

        foreach (var flag in flagService.ActiveFlags)
        {
            map.AddMapTack(CreateFlagTack(flag));
        }
        if (map.MapTable.Group == flagService.StartingTeam.Owner.GroupId)
            map.AddMapTack(CreateMainBaseTack(flagService.StartingTeam.Region.Primary.Zone));
        if (map.MapTable.Group == flagService.EndingTeam.Owner.GroupId)
            map.AddMapTack(CreateMainBaseTack(flagService.EndingTeam.Region.Primary.Zone));
    }
    private void RepopulateBuildableFobTacks(StrategyMap map, FobManager fobManager)
    {
        map.RemoveMapTacks(m => m is DeployableMapTack fm && fm.Deployable is BuildableFob);

        foreach (var fob in fobManager.Fobs)
        {
            if (fob.Team.GroupId != map.MapTable.Group)
                continue;

            if (fob is BuildableFob bf)
            {
                map.AddMapTack(CreateBuildableFobTack(bf));
            }
        }
    }

    private DeployableMapTack CreateBuildableFobTack(BuildableFob fob)
    {
        if (fob.IsBuilt)
            return new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:" + (fob.IsProxied ? "FobProxied" : "Fob")), fob);
        else
            return new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobUnbuilt"), fob);
    }
    private DeployableMapTack CreateMainBaseTack(IDeployable mainBase)
    {
        return new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:MainBase"), mainBase);
    }
    private FlagMapTack CreateFlagTack(FlagObjective flag)
    {
        if (flag.Owner == Team.NoTeam || flag.Owner.Faction.MapTackFlag == null)
            return new FlagMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:NeutralFlag"), flag);
        else
            return new FlagMapTack(flag.Owner.Faction.MapTackFlag, flag);
    }
}
