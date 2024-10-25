using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Fobs;
public partial class FobManager :
    IEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<ItemDropped>,
    IEventListener<MeleeHit>
{
    void IEventListener<BarricadePlaced>.HandleEvent(BarricadePlaced e, IServiceProvider serviceProvider)
    {
        BuildableContainer container = CreateBuildableContainer(e);

        // if barricade is Fob foundation, register a new Fob
        BuildableFob? newFob = null;
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid == e.Barricade.asset.GUID)
        {
            newFob = RegisterFob(new BuildableBarricade(e.Barricade));
        }
        else if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:RepairStation").Guid == e.Barricade.asset.GUID)
        {
            Team team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(e.GroupId);
            container.AddComponent(new RepairStation(e.Buildable, team, serviceProvider.GetRequiredService<ILoopTickerFactory>(), this, _assetConfiguration));
        }
        CreateShoveable(e, container, newFob);
    }
    private BuildableContainer CreateBuildableContainer(BarricadePlaced e)
    {
        return e.Buildable.Model.GetOrAddComponent<BuildableContainer>();
    }
    private void CreateShoveable(BarricadePlaced e, BuildableContainer container, BuildableFob? newFob)
    {
        ShovelableInfo? shovelableInfo = _configuration.GetRequiredSection("Shovelables").Get<List<ShovelableInfo>>()
        .FirstOrDefault(s => s.Foundation.Guid == e.Buildable.Asset.GUID);

        if (shovelableInfo != null)
        {
            ShovelableBuildable shovelable = new ShovelableBuildable(shovelableInfo, e.Buildable, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"), _serviceProvider);

            switch (shovelableInfo.ConstuctionType)
            {
                case ShovelableType.Fob:
                    shovelable.OnComplete = completedBuildable =>
                    {
                        if (newFob == null)
                            return;

                        newFob.MarkBuilt(completedBuildable!);
                        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = newFob });
                    };
                    break;
            }
            container.AddComponent(shovelable);
        }
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        BasePlayableFob? fob = (BasePlayableFob?)_fobs.FirstOrDefault(f => f is BasePlayableFob bpf && e.Buildable.InstanceId == bpf.Buildable.InstanceId);
        if (fob != null)
        {
            if (fob is BuildableFob buildableFob)
            {
                // only deregister buildable fobs if the unbuilt foundation is being destroyed
                if (!buildableFob.IsBuilt)
                    DeregisterFob(buildableFob);
                else // otherwise, spawn back the unbuilt foundation
                {
                    Transform transform = BarricadeManager.dropNonPlantedBarricade(
                        new Barricade(_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").GetAssetOrFail()),
                        e.Buildable.Position,
                        e.Buildable.Rotation,
                        e.Buildable.Owner.m_SteamID,
                        e.Buildable.Group.m_SteamID
                    );

                    buildableFob.MarkUnbuilt(new BuildableBarricade(BarricadeManager.FindBarricadeByRootTransform(transform)));
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = buildableFob });
                }
            }
            else
            {
                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = fob });
                DeregisterFob(fob);
            }
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
            }
        );
    }
    public void HandleEvent(MeleeHit e, IServiceProvider serviceProvider)
    {
        if (e.Equipment?.asset?.GUID == null)
            return;

        IAssetLink<ItemAsset>? entrenchingTool = _assetConfiguration.GetAssetLink<ItemAsset>("Items:EntrenchingTool");
        if (entrenchingTool.GetAssetOrFail().GUID != e.Equipment.asset.GUID)
            return;

        RaycastInfo raycast = DamageTool.raycast(new Ray(e.Look.aim.position, e.Look.aim.forward), 2, RayMasks.BARRICADE, e.Player.UnturnedPlayer);
        if (raycast.transform == null)
            return;

        if (!raycast.transform.TryGetComponent(out BuildableContainer container))
            return;

        if (container.Buildable.Group != e.Player.Team.GroupId)
            return;

        if (!container.TryGetFromContainer(out IShovelable? shovelable))
            return;

        SupplyCrateGroup supplyCrates = new SupplyCrateGroup(this, raycast.point, e.Player.Team);

        if (supplyCrates.BuildCount == 0)
        {
            // todo: maybe send chat message?
            return;
        }

        bool success = shovelable!.Shovel(e.Player, raycast.point);

        if (success)
            supplyCrates.SubstractSupplies(1, SupplyType.Build, SupplyChangeReason.ConsumeShovelHit);
    }
}
