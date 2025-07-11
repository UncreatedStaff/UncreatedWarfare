using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.StrategyMaps.MapTacks;

namespace Uncreated.Warfare.FOBs;

internal class FobStrategyMapHandler :
    IStrategyMapProvider,
    IEventListener<FobRegistered>,
    IEventListener<FobDeregistered>,
    IEventListener<FobBuilt>,
    IEventListener<FobDestroyed>,
    IEventListener<FobProxyChanged>
{
    private readonly FobManager _fobManager;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly StrategyMapManager _strategyMapManager;

    public FobStrategyMapHandler(FobManager fobManager, AssetConfiguration assetConfiguration, StrategyMapManager strategyMapManager)
    {
        _fobManager = fobManager;
        _assetConfiguration = assetConfiguration;
        _strategyMapManager = strategyMapManager;
    }

    public void RepopulateStrategyMap(StrategyMap strategyMap, AssetConfiguration assetConfiguration, IServiceProvider serviceProvider)
    {
        strategyMap.RemoveMapTacks((_, owner) => owner is IFob);

        foreach (IFob fob in _fobManager.Fobs)
        {
            UpdateFobTacks(fob, true);
        }
    }

    internal void UpdateFobTacks(IFob fob, bool replace)
    {
        if (fob is not IFobStrategyMapTackHandler handler)
            return;

        foreach (StrategyMap map in _strategyMapManager.StrategyMaps)
        {
            if (replace)
            {
                map.RemoveMapTacks((_, owner) => owner == fob);
            }

            MapTack? tack = handler.CreateMapTack(map, _assetConfiguration);
            if (tack == null)
                continue;

            map.AddMapTack(tack, fob);
        }
    }

    public void HandleEvent(FobRegistered e, IServiceProvider serviceProvider)
    {
        UpdateFobTacks(e.Fob, replace: false);
    }

    public void HandleEvent(FobDeregistered e, IServiceProvider serviceProvider)
    {
        foreach (StrategyMap map in _strategyMapManager.StrategyMaps)
        {
            map.RemoveMapTacks((_, owner) => owner == e.Fob);
        }
    }

    public void HandleEvent(FobBuilt e, IServiceProvider serviceProvider)
    {
        UpdateFobTacks(e.Fob, replace: true);
    }

    public void HandleEvent(FobDestroyed e, IServiceProvider serviceProvider)
    {
        UpdateFobTacks(e.Fob, replace: true);
    }

    public void HandleEvent(FobProxyChanged e, IServiceProvider serviceProvider)
    {
        UpdateFobTacks(e.Fob, replace: true);
    }
}