using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Zones;
using MathUtility = Uncreated.Warfare.Util.MathUtility;

namespace Uncreated.Warfare.StrategyMaps;

[Priority(-1) /* after FlagService */]
public class StrategyMapManager :
    ILayoutHostedService,
    IEventListenerProvider,
    IEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<FlagsSetUp>,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>,
    IEventListener<FlagDiscovered>,
    IEventListener<PlayerExitedZone>
{
    private readonly TrackingList<StrategyMap> _strategyMaps;
    private readonly ILogger<StrategyMapManager> _logger;
    private readonly ZoneStore? _zoneStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly StrategyMapsConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly BuildableAttributesDataStore _attributeStore;

    private readonly MapTackInfoUI? _tackInfoUi;

    public ReadOnlyTrackingList<StrategyMap> StrategyMaps { get; }

    public StrategyMapManager(ILogger<StrategyMapManager> logger, IServiceProvider serviceProvider, ZoneStore? zoneStore = null)
    {
        _strategyMaps = new TrackingList<StrategyMap>();
        _logger = logger;
        _zoneStore = zoneStore;

        _serviceProvider = serviceProvider;
        _configuration = serviceProvider.GetRequiredService<StrategyMapsConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _attributeStore = serviceProvider.GetRequiredService<BuildableAttributesDataStore>();
        _tackInfoUi = serviceProvider.GetService<MapTackInfoUI>();

        StrategyMaps = _strategyMaps.AsReadOnly();

        _logger.LogInformation("Configured Strategy MapTables: " + (_configuration.MapTables?.Count.ToString() ?? "config not found"));
    }

    public UniTask StartAsync(CancellationToken token)
    {
        RegisterExistingStrategyMaps();

        if (_zoneStore is { IsGlobal: true } && _tackInfoUi != null)
        {
            TimeUtility.updated += OnUpdated;
        }
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        if (_zoneStore is { IsGlobal: true } && _tackInfoUi != null)
        {
            TimeUtility.updated -= OnUpdated;
        }
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

        if (_zoneStore is { IsGlobal: true })
        {
            Vector3 position = buildable.Position;
            foreach (ZoneProximity proximity in _zoneStore.ProximityZones)
            {
                if (!proximity.Proximity.TestPoint(in position))
                    continue;

                map.Zones.Add(proximity.Zone);
            }
        }

        _strategyMaps.Add(map);

        IFlagRotationService? flagService = _serviceProvider.GetService<IFlagRotationService>();

        ITeamManager<Team>? teamManager = _serviceProvider.GetService<ITeamManager<Team>>();
        ZoneStore? globalZoneStore = _serviceProvider.GetService<ZoneStore>();

        foreach (IStrategyMapProvider stratMapProvider in _serviceProvider.GetServices<IStrategyMapProvider>())
        {
            stratMapProvider.RepopulateStrategyMap(map, _assetConfiguration, _serviceProvider);
        }

        if (flagService != null)
            RepopulateFlagTacks(map, flagService);
        if (teamManager != null && globalZoneStore != null)
            RepopulateMainBaseFobTacks(map, teamManager, globalZoneStore);

        _logger.LogDebug($"Registered new StrategyMap: {map.MapTable.Asset}");
    }

    public void DeregisterStrategyMap(IBuildable buildable)
    {
        StrategyMap? map = _strategyMaps.FindAndRemove(m => m.MapTable.Equals(buildable));
        if (map == null)
            return;

        map.Dispose();
        _logger.LogDebug($"Deregistered StrategyMap: {map.MapTable.Asset}");
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

    public void HandleEvent(FlagsSetUp e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            RepopulateFlagTacks(map, e.FlagService);
        }
    }

    public void HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            map.RemoveMapTacks((_, owner) => owner == e.Flag);
            map.AddMapTack(CreateFlagTack(e.Flag), e.Flag);
        }
    }

    void IEventListener<FlagDiscovered>.HandleEvent(FlagDiscovered e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in StrategyMapsOfTeam(e.Team))
        {
            map.AddMapTack(CreateFlagTack(e.Flag), e.Flag);
        }
    }

    public void HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMaps)
        {
            map.RemoveMapTacks((_, owner) => owner == e.Flag);
            map.AddMapTack(CreateFlagTack(e.Flag), e.Flag);
        }
    }

    private void RepopulateFlagTacks(StrategyMap map, IFlagRotationService flagService)
    {
        map.RemoveMapTacks(m => m is FlagMapTack or DeployableMapTack { Deployable: Zone { Type: ZoneType.MainBase } });

        foreach (FlagObjective flag in flagService.ActiveFlags)
        {
            if (flag.IsDiscovered(map.MapTable.Group))
            {
                map.AddMapTack(CreateFlagTack(flag), flag);
            }
        }
    }

    private void RepopulateMainBaseFobTacks(StrategyMap map, ITeamManager<Team> teamManager, ZoneStore globalZoneStore)
    {
        map.RemoveMapTacks((_, owner) => owner is Team);

        foreach (Team team in teamManager.AllTeams)
        {
            Zone? teamMain = globalZoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (teamMain == null)
                continue;

            map.AddMapTack(CreateMainBaseTack(team, map, teamMain), team);
        }
    }

    private MapTack CreateMainBaseTack(Team team, StrategyMap map, Zone mainBase)
    {
        return team.IsFriendly(map.MapTable.Group)
            ? new DeployableMapTack(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:MainBase"), mainBase)
            : new MapTack(_assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:MapTacks:EnemyMainBase"), mainBase.Spawn);
    }

    private FlagMapTack CreateFlagTack(FlagObjective flag)
    {
        if (flag.Owner == Team.NoTeam || flag.Owner.Faction.MapTackFlag == null)
            return new FlagMapTack(_assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:MapTacks:NeutralFlag"), flag);
        else
            return new FlagMapTack(flag.Owner.Faction.MapTackFlag, flag);
    }

    #region UI Stuff

    private void OnUpdated()
    {
        foreach (ZoneProximity x in _zoneStore!.ProximityZones!)
        {
            if (x.Zone.Type != ZoneType.WarRoom || x.Proximity is not ITrackingProximity<WarfarePlayer> trackingProximity)
                continue;

            foreach (WarfarePlayer player in trackingProximity.ActiveObjects)
            {
                MapTack? tack = null;
                Vector3 playerPos = player.Position;
                foreach (StrategyMap map in StrategyMaps)
                {
                    if (!map.Zones.Contains(x.Zone))
                        continue;

                    Vector3 pos = map.Position;

                    // check if nearby strategy map before checking each tack
                    // radius of the smallest circle that can fit a square of width map.Size
                    float circleRadius = map.Size * 1.41421356237f /* sqrt(2) */;
                    if (!MathUtility.WithinRange(in pos, in playerPos, circleRadius + 4f))
                    {
                        continue;
                    }

                    tack = map.TickMapTackLook(player, in pos);
                    if (tack != null)
                        break;
                }

                if (tack == null)
                {
                    _tackInfoUi!.TryClose(player);
                }
                else
                {
                    MapTackInfoUI.Data data = _tackInfoUi!.GetOrAddData(player.Steam64);
                    if (!data.HasUI)
                    {
                        _tackInfoUi.Open(player, tack.UIHandler!);
                    }
                }
            }
        }
    }

    void IEventListener<PlayerExitedZone>.HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        if (_zoneStore == null || e.Zone.Type != ZoneType.WarRoom || _zoneStore.IsInWarRoom(e.Player))
        {
            return;
        }


    }

    #endregion
}