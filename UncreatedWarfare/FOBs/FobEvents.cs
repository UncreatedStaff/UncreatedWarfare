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
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;
using static SDG.Unturned.ItemCurrencyAsset;

namespace Uncreated.Warfare.Fobs;
public partial class FobManager :
    IAsyncEventListener<BarricadePlaced>,
    IEventListener<BarricadeDestroyed>,
    IEventListener<ItemDropped>,
    IEventListener<VehicleSpawned>,
    IEventListener<VehicleDespawned>,
    IEventListener<BarricadePreDamaged>
{
    async UniTask IAsyncEventListener<BarricadePlaced>.HandleEventAsync(BarricadePlaced e, IServiceProvider serviceProvider, CancellationToken token)
    {
        await UniTask.NextFrame();

        BuildableContainer container = CreateBuildableContainer(e);

        // if barricade is Fob foundation, register a new Fob, or find the existing fob at this poisition
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:FobUnbuilt").MatchAsset(e.Barricade.asset))
        {
            // only register a new Fob with this foundation if it doesn't belong to an existing one.
            // this can happen after a built Fob is destroyed after which the foundation is replaced.
            BunkerFob? unbuiltFob = FindBuildableFob<BunkerFob>(e.Buildable);
            if (unbuiltFob == null)
            {
                unbuiltFob = RegisterBunkerFob(new BuildableBarricade(e.Barricade));
            }

            // fobs need their own special shoveable with a completed event
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
        // if it's the player's faction's rally point, register a new rally point
        else if (e.Owner != null && e.Owner.IsInSquad() && e.Owner.Team.Faction.RallyPoint.MatchAsset(e.Barricade.asset))
        {
            RegisterFob(new RallyPoint(e.Buildable, e.Owner.GetSquad()!, serviceProvider));
            return;
        }

        // other entities and shovelables get registered here
        TryRegisterEntity(e.Buildable, serviceProvider);
        TryCreateShoveable(e.Buildable, container, out _);
    }
    private BuildableContainer CreateBuildableContainer(BarricadePlaced e)
    {
        return e.Buildable.Model.GetOrAddComponent<BuildableContainer>();
    }
    private void TryRegisterEntity(IBuildable buildable, IServiceProvider serviceProvider)
    {
        ShovelableInfo? completedFortification = (Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.CompletedStructure != null && s.Emplacement == null && s.CompletedStructure.MatchAsset(buildable.Asset));

        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Fobs:RepairStation").MatchAsset(buildable.Asset))
        {
            Team team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);
            RepairStation repairStation = new RepairStation(buildable, team, serviceProvider.GetRequiredService<ILoopTickerFactory>(), this, _assetConfiguration);
            RegisterFobEntity(repairStation);
        }
        else if (completedFortification != null)
        {
            RegisterFobEntity(new FortificationEntity(buildable));
        }
    }
    private bool TryCreateShoveable(IBuildable buildable, BuildableContainer container, out ShovelableBuildable? shovelable, bool shouldConsumeSupplies = true)
    {
        shovelable = null;

        ShovelableInfo? shovelableInfo = (Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Foundation != null && s.Foundation.MatchAsset(buildable.Asset));

        if (shovelableInfo == null)
            return false;

        ShovelableBuildable newShovelable = new ShovelableBuildable(shovelableInfo, buildable, _serviceProvider, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"));
        shovelable = newShovelable;

        RegisterFobEntity(newShovelable);

        BunkerFob? nearestFriendlyFob = FindNearestBuildableFob(newShovelable.Buildable.Group, buildable.Position);

        if (nearestFriendlyFob != null && shouldConsumeSupplies)
        {
            NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(nearestFriendlyFob.Position, nearestFriendlyFob.Team.GroupId, this);
            supplyCrates.SubstractSupplies(shovelableInfo.SupplyCost, SupplyType.Build, SupplyChangeReason.ConsumeShovelablePlaced);
        }

        return true;
    }

    public void HandleEvent(BarricadeDestroyed e, IServiceProvider serviceProvider)
    {
        IBuildableFob? fob = FindBuildableFob<IBuildableFob>(e.Buildable);
        if (fob is BunkerFob buildableFob)
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

        ShovelableBuildable? shovelable = GetBuildableFobEntity<ShovelableBuildable>(e.Buildable);
        if (shovelable != null)
        {
            BunkerFob? nearestFriendlyFob = FindNearestBuildableFob(shovelable.Buildable.Group, shovelable.Buildable.Position);

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
       
        SupplyCrate? supplyCrate = _entities.OfType<SupplyCrate>().FirstOrDefault(i => i.Buildable.Equals(e.Buildable));
        if (supplyCrate != null)
        {
            NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, -supplyCrate.SupplyCount, SupplyChangeReason.ConsumeSuppliesDestroyed);
        }

        _entities.RemoveAll(en => en is IBuildableFobEntity bfe && bfe.Buildable.Equals(e.Buildable));
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
                RegisterFobEntity(supplyCrate);
                NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, supplyCrate.SupplyCount, SupplyChangeReason.ResupplyFob, e.Player);
            }
        );
    }
    public void HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        ShovelableInfo? emplacementShoveable = (Configuration.GetRequiredSection("Shovelables").Get<IEnumerable<ShovelableInfo>>() ?? Array.Empty<ShovelableInfo>())
            .FirstOrDefault(s => s.Emplacement != null && s.Emplacement.Vehicle.MatchAsset(e.Vehicle.Vehicle.asset));

        if (emplacementShoveable == null)
            return;

        RegisterFobEntity(new EmplacementEntity(e.Vehicle, emplacementShoveable.Foundation));

    }
    public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
    {
        EmplacementEntity? emplacement = GetEmplacementFobEntity(e.Vehicle.Vehicle);
        if (emplacement == null)
            return;

        DeregisterFobEntity(emplacement);
    }

    public void HandleEvent(BarricadePreDamaged e, IServiceProvider serviceProvider)
    {
        BunkerFob? correspondingFob = FindBuildableFob<BunkerFob>(e.Buildable);
        if (correspondingFob == null)
            return;

        if (!correspondingFob.IsBuilt) // only record damage on built fobs
            return;

        if (e.Instigator != null)
            correspondingFob.DamageTracker.RecordDamage(e.Instigator.Value, e.PendingDamage, e.DamageOrigin);
        else
            correspondingFob.DamageTracker.RecordDamage(e.PendingDamage, e.DamageOrigin);
    }
}
