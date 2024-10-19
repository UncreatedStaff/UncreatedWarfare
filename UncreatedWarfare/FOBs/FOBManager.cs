using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Fobs;

public class FobManager : 
    ILayoutHostedService,
    IEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<ItemDropped>,
    IEventListener<MeleeHit>
{
    private readonly FobConfiguration _configuration;
    private readonly AssetConfiguration _assetConfiguration;
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
        string fobName = "FOB" + (_fobs.Count + 1);
        BasePlayableFob fob = new BasePlayableFob(_serviceProvider, fobName, fobBuildable);
        _fobs.Add(fob);
        _logger.LogDebug("Registered new FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobRegistered { Fob = fob });
    }
    public IFob? DeregisterFob(BasePlayableFob fob)
    {
        IFob? existing = _fobs.FindAndRemove(f => f == fob);
        _logger.LogDebug("Deregistered FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDeregistered { Fob = fob });
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

        BuildableContainer container = e.Buildable.Model.GetOrAddComponent<BuildableContainer>();

        ShovelableInfo? shovelableInfo = _configuration.GetRequiredSection("Shovelables").Get<List<ShovelableInfo>>()?
            .FirstOrDefault(s => s.FoundationBuildable.Guid == e.Buildable.Asset.GUID);

        if (shovelableInfo != null)
        {
            container.AddComponent(new ShovelableBuildable(shovelableInfo, e.Buildable, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit")));
        }
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        BasePlayableFob? fob = (BasePlayableFob?) _fobs.FirstOrDefault(i => i is BasePlayableFob f && f.Buildable.InstanceId == e.InstanceId);
        if (fob != null)
        {
            DeregisterFob(fob);
        }
    }

    public void HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        SupplyCrateInfo? supplyCrateInfo = _configuration.GetRequiredSection("SupplyCrates").Get<List<SupplyCrateInfo>>()?
            .FirstOrDefault(s => s.SupplyItemAsset.Guid == e.Item.GetAsset().GUID);

        if (supplyCrateInfo == null)
            return;

        new FallingBuildable(
            e.DroppedItem,
            supplyCrateInfo.SupplyItemAsset.GetAssetOrFail(),
            supplyCrateInfo.PlacementEffect.GetAssetOrFail(),
            e.Player.Position,
            e.Player.Yaw,
            serviceProvider.GetRequiredService<ILoopTickerFactory>(),
            (buildable) =>
            {
                SupplyCrate supplyCrate = new SupplyCrate(supplyCrateInfo, buildable);
                _floatingItems.Add(supplyCrate);
                _logger.LogInformation("Floating fob items: " + _floatingItems.Count);
            }
        );

    }

    public void HandleEvent(MeleeHit e, IServiceProvider serviceProvider)
    {
        if (e.Equipment?.asset == null)
            return;
        
        IAssetLink<ItemAsset> entrenchingTool = _assetConfiguration.GetAssetLink<ItemAsset>("Items:EntrenchingTool");
        if (entrenchingTool.MatchAsset(e.Equipment.asset))
            return;

        RaycastInfo info = DamageTool.raycast(new Ray(e.Look.aim.position, e.Look.aim.forward), 2, RayMasks.BARRICADE, e.Player.UnturnedPlayer);
        if (info.transform == null)
            return;

        if (!info.transform.TryGetComponent(out BuildableContainer container))
            return;

        if (!container.TryGetFromContainer(out IShovelable? shovelable))
            return;

        shovelable!.Shovel(e.Player, info.point);
    }
}