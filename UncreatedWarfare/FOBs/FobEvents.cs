using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Fobs;
public partial class FobManager :
    IAsyncEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<ItemDropped>
{
    async UniTask IAsyncEventListener<BarricadePlaced>.HandleEventAsync(BarricadePlaced e, IServiceProvider serviceProvider, CancellationToken token)
    {
        await UniTask.NextFrame();

        BuildableContainer container = CreateBuildableContainer(e);

        // if barricade is Fob foundation, register a new Fob, or find the existing fob at this poisition
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid == e.Barricade.asset.GUID)
        {
            // only register a new Fob with this foundation if it doesn't belong to an existing one.
            // this can happen after a built Fob is destroyed after which the foundation is replaced.
            BuildableFob? unbuiltFob = FindFob<BuildableFob>(e.Buildable);
            if (unbuiltFob == null)
            {
                unbuiltFob = RegisterFob(new BuildableBarricade(e.Barricade));
            }

            if (TryCreateShoveable(e.Buildable, container, out ShovelableBuildable? shovelable, shouldConsumeSupplies: !unbuiltFob.HasBeenRebuilt))
            {
                shovelable!.OnComplete = completedBuildable =>
                {
                    if (unbuiltFob == null)
                        return;

                    unbuiltFob.MarkBuilt(completedBuildable!);
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = unbuiltFob }, CancellationToken.None);
                };
            }
            return;
        }
        
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:RepairStation").Guid == e.Barricade.asset.GUID)
        {
            Team team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(e.GroupId);
            container.AddComponent(new RepairStation(e.Buildable, team, serviceProvider.GetRequiredService<ILoopTickerFactory>(), this, _assetConfiguration));
        }
        else if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:RepairStation").Guid == e.Barricade.asset.GUID)
        {
            Team team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(e.GroupId);
            container.AddComponent(new RepairStation(e.Buildable, team, serviceProvider.GetRequiredService<ILoopTickerFactory>(), this, _assetConfiguration));
        }

        TryCreateShoveable(e.Buildable, container, out _);
    }
    private BuildableContainer CreateBuildableContainer(BarricadePlaced e)
    {
        return e.Buildable.Model.GetOrAddComponent<BuildableContainer>();
    }
    private bool TryCreateShoveable(IBuildable buildable, BuildableContainer container, out ShovelableBuildable? shovelable, bool shouldConsumeSupplies = true)
    {
        shovelable = null;

        ShovelableInfo? shovelableInfo = (Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.Guid == buildable.Asset.GUID);

        if (shovelableInfo == null)
            return false;

        ShovelableBuildable newShovelable = new ShovelableBuildable(shovelableInfo, buildable, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"), _serviceProvider);
        shovelable = newShovelable;

        container.AddComponent(shovelable);

        BuildableFob? nearestFriendlyFob = FindNearestBuildableFob(newShovelable.Buildable.Group, buildable.Position);

        if (nearestFriendlyFob != null && shouldConsumeSupplies)
        {
            NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(nearestFriendlyFob.Position, nearestFriendlyFob.Team.GroupId, this);
            supplyCrates.SubstractSupplies(shovelableInfo.SupplyCost, SupplyType.Build, SupplyChangeReason.ConsumeShovelablePlaced);
        }

        return true;
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        BasePlayableFob? fob = FindFob<BasePlayableFob>(e.Buildable);
        if (fob is BuildableFob buildableFob)
        {
            if (buildableFob.IsBuilt)
            {
                _logger.LogInformation("Replacing FOB foundation with unbuilt...");

                Transform transform = BarricadeManager.dropNonPlantedBarricade(
                    new Barricade(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").GetAssetOrFail()),
                    e.Buildable.Position,
                    e.Buildable.Rotation,
                    e.Buildable.Owner.m_SteamID,
                    e.Buildable.Group.m_SteamID
                );
                buildableFob.MarkUnbuilt(new BuildableBarricade(BarricadeManager.FindBarricadeByRootTransform(transform)));

                _logger.LogInformation("FOB foundation successfully replaced with unbuilt version.");

                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = buildableFob });
            }
            else
            {
                _logger.LogInformation("Buildable fob base was destroyed and will be deregistered.");
                DeregisterFob(fob);
            }
        }
        else if (fob != null)
        {
            _logger.LogInformation("Attempting to destroy other buildable fob.");
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = fob });
            DeregisterFob(fob);
        }

        ShovelableBuildable? shovelable = e.Transform.GetOrAddComponent<BuildableContainer>().ComponentOrNull<ShovelableBuildable>();
        if (shovelable != null)
        {
            BuildableFob? nearestFriendlyFob = FindNearestBuildableFob(shovelable.Buildable.Group, shovelable.Buildable.Position);

            if (nearestFriendlyFob != null)
            {
                NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(nearestFriendlyFob.Position, nearestFriendlyFob.Team.GroupId, this);
                supplyCrates.RefundSupplies(shovelable.Info.SupplyCost, SupplyType.Build);
            }
            else
            {
                NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(shovelable.Buildable.Position, shovelable.Buildable.Group, this);
                supplyCrates.RefundSupplies(shovelable.Info.SupplyCost, SupplyType.Build);
            }
        }
       
        SupplyCrate? supplyCrate = _floatingItems.OfType<SupplyCrate>().FirstOrDefault(i => i.Buildable.Equals(e.Buildable));
        if (supplyCrate != null)
        {
            NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, -supplyCrate.SupplyCount, SupplyChangeReason.ConsumeSuppliesDestroyed);
        }
        _floatingItems.RemoveAll(i => i.Buildable.Equals(e.Buildable));
    }
    public void HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        SupplyCrateInfo? supplyCrateInfo = Configuration.GetRequiredSection("SupplyCrates").Get<List<SupplyCrateInfo>>()?
            .FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(e.Item.GetAsset()));

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
                NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, supplyCrate.SupplyCount, SupplyChangeReason.ResupplyFob);
            }
        );
    }
}
