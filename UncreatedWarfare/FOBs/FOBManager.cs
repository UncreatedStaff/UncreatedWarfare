using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Fobs;

public class FobManager : ILayoutHostedService, IEventListener<BarricadePlaced>, IEventListener<BarricadeDestroyed>, IEventListener<ItemDropped>
{
    private readonly FobConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly EventDispatcher2? _eventDispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FobManager> _logger;
    private readonly TrackingList<IFobItem> _floatingItems;
    private readonly TrackingList<IFob> _fobs;

    /// <summary>
    /// Items placed by players that aren't linked to a specific FOB.
    /// </summary>
    public IReadOnlyList<IFobItem> FloatingItems { get; }

    /// <summary>
    /// List of all FOBs in the world.
    /// </summary>
    public IReadOnlyList<IFob> Fobs { get; }

    public FobManager(IServiceProvider serviceProvider, ILogger<FobManager> logger)
    {
        _configuration = serviceProvider.GetRequiredService<FobConfiguration>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _eventDispatcher = serviceProvider.GetService<EventDispatcher2>();
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fobs = new TrackingList<IFob>(24);
        _floatingItems = new TrackingList<IFobItem>(32);

        Fobs = new ReadOnlyTrackingList<IFob>(_fobs);
        FloatingItems = new ReadOnlyTrackingList<IFobItem>(_floatingItems);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public void RegisterFob(IBuildable fobBuildable)
    {
        string fobName = "FOB" + _fobs.Count + 1;
        BasePlayableFob fob = new BasePlayableFob(_serviceProvider, fobName, fobBuildable);
        _fobs.Add(fob);
        _logger.LogDebug("Registered new FOB: " + fob);
        _eventDispatcher?.FobRegistered(fob);
    }
    public IFob? DeregisterFob(BasePlayableFob fob)
    {
        IFob? existing = _fobs.FindAndRemove(f => f == fob);
        _logger.LogDebug("Deregistered FOB: " + fob);
        _eventDispatcher?.FobDeregistered(fob);
        fob.DestroyAsync();
        return existing;
    }

    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        IAssetLink<ItemBarricadeAsset> fobAsset = _assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:Fob");
        if (fobAsset.Guid == e.Barricade.asset.GUID)
        {
            RegisterFob(new BuildableBarricade(e.Barricade));
            return;
        }
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        if (e.Barricade.model.TryGetComponent<BasePlayableFob>(out var fob))
        {
            DeregisterFob(fob);
        }
    }

    public void HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        SupplyCrateInfo? supplyCrateInfo = _configuration.GetRequiredSection("SupplyCrates").Get<List<SupplyCrateInfo>>()
            .FirstOrDefault(s => s.SupplyItemAsset.Guid == e.Item.GetAsset().GUID);

        if (supplyCrateInfo == null)
            return;

        new FallingBuildable(
            e.DroppedItem,
            supplyCrateInfo.SupplyItemAsset.GetAssetOrFail(),
            supplyCrateInfo.PlacementEffect.GetAssetOrFail(),
            e.Player.Position,
            e.Player.Yaw,
            (buildable) =>
            {
                SupplyCrate supplyCrate = new SupplyCrate(supplyCrateInfo, buildable);
                _floatingItems.Add(supplyCrate);
                _logger.LogInformation("Floating fob items: " + _floatingItems.Count);
            }
        );

    }
}