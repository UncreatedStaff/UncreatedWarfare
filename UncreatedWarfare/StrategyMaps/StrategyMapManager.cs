using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.StrategyMaps;

[Priority(-1) /* after FlagService */]
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
    IEventListener<FlagNeutralized>,
    IDisposable
{
    private readonly TrackingList<StrategyMap> _strategyMaps;
    private readonly ILogger<StrategyMapManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StrategyMapsConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly BuildableAttributesDataStore _attributeStore;

    private Dictionary<int, IAssetLink<ItemBarricadeAsset>> _rallyPoints = null!;
    private IAssetLink<ItemBarricadeAsset> _fobUnbuilt = null!;
    private IAssetLink<ItemBarricadeAsset> _fobBuilt = null!;
    private IAssetLink<ItemBarricadeAsset> _fobProxied = null!;
    private IAssetLink<ItemBarricadeAsset> _mainBase = null!;
    private IAssetLink<ItemBarricadeAsset> _neutralFlag = null!;

    public StrategyMapManager(ILogger<StrategyMapManager> logger, IServiceProvider serviceProvider)
    {
        _strategyMaps = new TrackingList<StrategyMap>();
        _logger = logger;

        _serviceProvider = serviceProvider;
        _configuration = serviceProvider.GetRequiredService<StrategyMapsConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _attributeStore = serviceProvider.GetRequiredService<BuildableAttributesDataStore>();

        OnAssetConfigurationChange(_assetConfiguration);

        _logger.LogInformation("Configured Strategy MapTables: " + (_configuration.MapTables?.Count.ToString() ?? "config not found"));
    }

    private void OnAssetConfigurationChange(IConfiguration obj)
    {
        _rallyPoints = _assetConfiguration.GetSection("Buildables:MapTacks:Rallypoints")
            .Get<Dictionary<int, IAssetLink<ItemBarricadeAsset>>>() ?? new Dictionary<int, IAssetLink<ItemBarricadeAsset>>();

        _fobUnbuilt = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobUnbuilt");
        _fobBuilt = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:Fob");
        _fobProxied = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobProxied");
        _mainBase = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:MainBase");
        _neutralFlag = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:NeutralFlag");
    }

    public UniTask StartAsync(CancellationToken token)
    {
        RegisterExistingStrategyMaps();
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    void IEventListenerProvider.AppendListeners<TEventArgs>(TEventArgs args, List<object> listeners)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            if (map is IEventListener<TEventArgs> el)
                listeners.Add(el);
            if (map is IAsyncEventListener<TEventArgs> ael)
                listeners.Add(ael);
        }
    }

    private void RegisterExistingStrategyMaps()
    {
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateNonPlantedBarricades())
        {
            MapTableInfo? info = _configuration.MapTables.FirstOrDefault(m => m.BuildableAsset.MatchAsset(barricade.Drop.asset));

            if (info == null)
                continue;

            RegisterStrategyMap(new BuildableBarricade(barricade.Drop), info);
        }
    }

    public void RegisterStrategyMap(IBuildable buildable, MapTableInfo tableInfo)
    {
        tableInfo.BuildableAsset.AssertValid();

        if (_strategyMaps.Any(m => m.MapTable.Equals(buildable)))
        {
            return;
        }

        StrategyMap map = new StrategyMap(buildable, tableInfo, _attributeStore);
        _strategyMaps.AddIfNotExists(map);

        DualSidedFlagService? flagService = _serviceProvider.GetService<DualSidedFlagService>();
        FobManager? fobManager = _serviceProvider.GetService<FobManager>();

        if (flagService != null)
            RepopulateFlagTacks(map, flagService);
        if (fobManager != null)
            RepopulateDeployableFobTacks(map, fobManager);

        _logger.LogDebug($"Registered new StrategyMap: {map}");
    }

    public void DeregisterStrategyMap(IBuildable buildable)
    {
        StrategyMap? map = _strategyMaps.FindAndRemove(m => m.MapTable.Equals(buildable));
        if (map == null)
            return;

        map.Dispose();
        _logger.LogDebug($"Deregistered StrategyMap: {map}");
    }
    public IEnumerable<StrategyMap> StrategyMapsOfTeam(Team team) => _strategyMaps.Where(s => s.MapTable.Group == team.GroupId);
    public StrategyMap? GetStrategyMap(uint buildableInstanceId) => _strategyMaps.FirstOrDefault(m => m.MapTable.InstanceId == buildableInstanceId);

    public void HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        MapTableInfo? mapTableInfo = _configuration.MapTables
            .FirstOrDefault(m => m.BuildableAsset.Guid == e.Buildable.Asset.GUID);

        if (mapTableInfo == null)
            return;

        RegisterStrategyMap(e.Buildable, mapTableInfo);
    }
    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        bool isMapTableBuildable = _configuration.MapTables
            .Any(m => m.BuildableAsset.Guid == e.Buildable.Asset.GUID);

        if (!isMapTableBuildable)
            return;

        DeregisterStrategyMap(e.Buildable);
    }

    public void HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        MapTack? newTack = null;
        if (e.Fob is BunkerFob)
            newTack = new DeployableMapTack(_fobUnbuilt, e.Fob);
        else if (e.Fob is RallyPoint rallypoint)
        {
            if (_rallyPoints.TryGetValue(rallypoint.Squad.TeamIdentificationNumber, out IAssetLink<ItemBarricadeAsset> rallyPointMapTack))
                newTack = new DeployableMapTack(rallyPointMapTack, rallypoint);
        }
        
        if (newTack == null)
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
        if (e.Fob != null)
            newTack = new DeployableMapTack(_fobBuilt, e.Fob);
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
        if (e.Fob is BunkerFob)
            newTack = new DeployableMapTack(_fobUnbuilt, e.Fob);
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
        if (e.Fob is BunkerFob bf && bf.IsBuilt)
            newTack = new DeployableMapTack(e.IsProxied ? _fobProxied : _fobBuilt, e.Fob);
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
        map.RemoveMapTacks(m => m is FlagMapTack or DeployableMapTack { Deployable: Zone { Type: ZoneType.MainBase } });

        foreach (var flag in flagService.ActiveFlags)
        {
            map.AddMapTack(CreateFlagTack(flag));
        }
        if (map.MapTable.Group.m_SteamID == flagService.StartingTeam.GroupId.m_SteamID)
            map.AddMapTack(CreateMainBaseTack(flagService.StartingTeamRegion.Primary.Zone));
        if (map.MapTable.Group.m_SteamID == flagService.EndingTeam.GroupId.m_SteamID)
            map.AddMapTack(CreateMainBaseTack(flagService.EndingTeamRegion.Primary.Zone));
    }
    private void RepopulateDeployableFobTacks(StrategyMap map, FobManager fobManager)
    {
        map.RemoveMapTacks(m => m is DeployableMapTack { Deployable: BunkerFob or RallyPoint });

        foreach (var fob in fobManager.Fobs)
        {
            if (fob.Team.GroupId != map.MapTable.Group)
                continue;

            if (fob is BunkerFob bf)
            {
                map.AddMapTack(CreateBunkerFobTack(bf));
            }
            else if (fob is RallyPoint rp)
            {
                if (_rallyPoints.TryGetValue(rp.Squad.TeamIdentificationNumber, out IAssetLink<ItemBarricadeAsset> tack))
                    map.AddMapTack(new DeployableMapTack(tack, fob));
            }
        }
    }

    private DeployableMapTack CreateBunkerFobTack(BunkerFob fob)
    {
        if (fob.IsBuilt)
            return new DeployableMapTack(fob.IsProxied ? _fobProxied : _fobBuilt, fob);
        else
            return new DeployableMapTack(_fobUnbuilt, fob);
    }

    private DeployableMapTack CreateMainBaseTack(IDeployable mainBase)
    {
        return new DeployableMapTack(_mainBase, mainBase);
    }

    private FlagMapTack CreateFlagTack(FlagObjective flag)
    {
        if (flag.Owner == Team.NoTeam || flag.Owner.Faction.MapTackFlag == null)
            return new FlagMapTack(_neutralFlag, flag);
        else
            return new FlagMapTack(flag.Owner.Faction.MapTackFlag, flag);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _assetConfiguration.OnChange -= OnAssetConfigurationChange;
    }
}