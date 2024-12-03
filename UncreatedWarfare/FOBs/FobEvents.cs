using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands;
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
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Sessions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Fobs;
public partial class FobManager :
    IEventListener<PlaceBarricadeRequested>,
    IAsyncEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<ItemDropped>,
    IEventListener<MeleeHit>,
    IEventListener<ClaimBedRequested>
{
    public void HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid != e.Barricade.asset.GUID)
            return;

        if (e.OriginalPlacer == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();

        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(e.Position, e.OriginalPlacer.Team.GroupId, this);

        if (supplyCrates.BuildCount == 0)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBNoSupplyCrate);
            e.Cancel();
            return;
        }

        int maxNumberOfFobs = _configuration.GetValue("MaxNumberOfFobs", 10);
        bool fobLimitReached = _fobs.Count(f => f is BuildableFob bf && bf.Team == e.OriginalPlacer.Team) >= maxNumberOfFobs;
        if (fobLimitReached)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildMaxFOBsHit);
            e.Cancel();
            return;
        }

        float minDistanceBetweenFobs = _configuration.GetValue("MinDistanceBetweenFobs", 150f);

        BuildableFob? tooCloseFob = (BuildableFob?) _fobs.FirstOrDefault(f => 
            f is BuildableFob bf && 
            bf.Team ==  e.OriginalPlacer.Team &&
            MathUtility.WithinRange(e.Position, bf.Position, minDistanceBetweenFobs)
            );
        if (tooCloseFob != null)
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBTooClose, tooCloseFob, Vector3.Distance(tooCloseFob.Position, e.Position), minDistanceBetweenFobs);
            e.Cancel();
            return;
        }

        float minFobDistanceFromMain = _configuration.GetValue<float>("MinFobDistanceFromMain", 300);

        var zoneStore = serviceProvider.GetService<ZoneStore>();
        if (zoneStore != null)
        {
            Zone? mainBase = zoneStore.FindClosestZone(e.Position, ZoneType.MainBase);

            if (mainBase != null && MathUtility.WithinRange(mainBase.Center, e.Position, minFobDistanceFromMain))
            {
                chatService.Send(e.OriginalPlacer, _translations.BuildFOBTooCloseToMain);
                e.Cancel();
                return;
            }
        }

        if (WaterUtility.isPointUnderwater(e.Position))
        {
            chatService.Send(e.OriginalPlacer, _translations.BuildFOBUnderwater);
            e.Cancel();
            return;
        }
    }

    async UniTask IAsyncEventListener<BarricadePlaced>.HandleEventAsync(BarricadePlaced e, IServiceProvider serviceProvider, CancellationToken token = default)
    {
        await UniTask.NextFrame();

        BuildableContainer container = CreateBuildableContainer(e);

        // if barricade is Fob foundation, register a new Fob, or find the existing fob at this poisition
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").Guid == e.Barricade.asset.GUID)
        {
            // only register a new Fob with this foundation if it doesn't belong to an existing one.
            // this can happen after a built Fob is destroyed after which the foundation is replaced.
            BuildableFob unbuiltFob = (BuildableFob)_fobs.FirstOrDefault(f => f is BuildableFob bf && bf.Buildable.InstanceId == e.Buildable.InstanceId);
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
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = unbuiltFob });
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

        ShovelableInfo? shovelableInfo = (_configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.Guid == buildable.Asset.GUID);

        if (shovelableInfo == null)
            return false;

        ShovelableBuildable newShovelable = new ShovelableBuildable(shovelableInfo, buildable, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"), _serviceProvider);
        shovelable = newShovelable;

        container.AddComponent(shovelable);

        BuildableFob? nearestFriendlyFob = (BuildableFob)_fobs.FirstOrDefault(f =>
            f is BuildableFob bf &&
            bf.Team.GroupId == newShovelable.Buildable.Group &&
            bf.IsWithinRadius(buildable.Position)
        );

        if (nearestFriendlyFob != null && shouldConsumeSupplies)
        {
            NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(nearestFriendlyFob.Position, nearestFriendlyFob.Team.GroupId, this);
            supplyCrates.SubstractSupplies(shovelableInfo.SupplyCost, SupplyType.Build, SupplyChangeReason.ConsumeShovelablePlaced);
        }

        return true;
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        BasePlayableFob? fob = (BasePlayableFob?)_fobs.FirstOrDefault(f => f is BasePlayableFob bpf && e.Buildable.Equals(bpf.Buildable));
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
            BuildableFob? nearestFriendlyFob = (BuildableFob)_fobs.FirstOrDefault(f =>
                f is BuildableFob bf &&
                bf.Team.GroupId == shovelable.Buildable.Group &&
                bf.IsWithinRadius(shovelable.Buildable.Position)
            );

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
       
        SupplyCrate? supplyCrate = (SupplyCrate)_floatingItems.FirstOrDefault(i => i is SupplyCrate && i.Buildable.Equals(e.Buildable.InstanceId));
        if (supplyCrate != null)
        {
            NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, -supplyCrate.SupplyCount, SupplyChangeReason.ConsumeSuppliesDestroyed);
        }
        _floatingItems.RemoveAll(i => i.Buildable.InstanceId == e.Buildable.InstanceId);
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
                NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, supplyCrate.SupplyCount, SupplyChangeReason.ResupplyFob);
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

        shovelable!.Shovel(e.Player, raycast.point);
    }

    public void HandleEvent(ClaimBedRequested e, IServiceProvider serviceProvider)
    {
        SupplyCrate? ammoCrate = (SupplyCrate)_floatingItems.FirstOrDefault(i => 
            i is SupplyCrate s && 
            s.Type == SupplyType.Ammo && 
            s.Buildable.InstanceId == e.Buildable.InstanceId
        ); // todo: is this an efficient way to do this?

        if (ammoCrate == null)
            return;

        ChatService chatService = serviceProvider.GetRequiredService<ChatService>();
        AmmoCommandTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoCommandTranslations>>().Value;

        if (!e.Player.TryGetFromContainer(out KitPlayerComponent? kit) || kit?.CachedKit == null)
        {
            chatService.Send(e.Player, translations.AmmoNoKit);
            e.Cancel();
            return;
        }

        int rearmCost = GetKitRearmCost(kit.ActiveClass);

        NearbySupplyCrates supplyCrate = NearbySupplyCrates.FromSingleCrate(ammoCrate, this);
        if (rearmCost > supplyCrate.AmmoCount)
        {
            chatService.Send(e.Player, translations.AmmoOutOfStock, supplyCrate.AmmoCount, rearmCost);
            e.Cancel();
            return;
        }
        KitManager kitManager = serviceProvider.GetRequiredService<KitManager>();
        _ = kitManager.Requests.GiveKit(e.Player, kit.CachedKit, false, true);
        supplyCrate.SubstractSupplies(1, SupplyType.Ammo, SupplyChangeReason.ConsumeGeneral);

        chatService.Send(e.Player, translations.AmmoResuppliedKit, rearmCost, supplyCrate.AmmoCount);
        e.Cancel();
    }
    private int GetKitRearmCost(Class kitClass)
    {
        switch (kitClass)
        {
            case Class.HAT:
            case Class.CombatEngineer:
                return 3;
            case Class.LAT:
            case Class.MachineGunner:
            case Class.Sniper:
                return 2;
            default:
                return 1;
        }
    }
}
