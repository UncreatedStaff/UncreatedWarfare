using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Fobs;

public partial class FobManager :
    IEventListener<IBuildablePlacedEvent>,
    IEventListener<PlaceBarricadeRequested>,
    IEventListener<IBuildableDestroyedEvent>,
    IEventListener<DropItemRequested>,
    IEventListener<ItemDropped>,
    IEventListener<VehicleSpawned>,
    IEventListener<VehicleDespawned>,
    IEventListener<TriggerTrapRequested>,
    IEventListener<IDamageBuildableRequestedEvent>
{
    private bool IsTrapTooNearFobSpawn(in Vector3 pos)
    {
        const float maxDistance = 10;

        foreach (BunkerFob fob in Fobs.OfType<BunkerFob>())
        {
            if (MathUtility.WithinRange2D(in pos, fob.SpawnPosition, maxDistance))
            {
                return true;
            }
        }

        return false;
    }

    void IEventListener<PlaceBarricadeRequested>.HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        // dont allow placing traps near spawner
        if (e.Asset is not ItemTrapAsset || !IsTrapTooNearFobSpawn(e.Position))
            return;

        _chatService.Send(e.OriginalPlacer, _translations.BuildableNotAllowed);

        e.Cancel();
    }

    void IEventListener<TriggerTrapRequested>.HandleEvent(TriggerTrapRequested e, IServiceProvider serviceProvider)
    {
        Vector3 pos = e.Barricade.GetServersideData().point;
        if (IsTrapTooNearFobSpawn(in pos))
            e.Cancel();
    }

    [EventListener(RequireNextFrame = true)]
    void IEventListener<IBuildablePlacedEvent>.HandleEvent(IBuildablePlacedEvent e, IServiceProvider serviceProvider)
    {
        // if barricade is Fob foundation, register a new Fob, or find the existing fob at this poisition
        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Gameplay:FobUnbuilt").MatchAsset(e.Buildable.Asset))
        {
            // only register a new Fob with this foundation if it doesn't belong to an existing one.
            // this can happen after a built Fob is destroyed after which the foundation is replaced.
            BunkerFob? unbuiltFob = FindBuildableFob<BunkerFob>(e.Buildable);
            if (unbuiltFob == null)
            {
                unbuiltFob = RegisterBunkerFob(e.Buildable);
            }

            // fobs need their own special shoveable with a completed event
            if (TryCreateShoveable(e.Buildable, e.Owner, out ShovelableBuildable? shovelable, shouldConsumeSupplies: !unbuiltFob.HasBeenRebuilt))
            {
                shovelable!.OnComplete += completedBuildable =>
                {
                    if (unbuiltFob == null)
                        return;

                    unbuiltFob.MarkBuilt(completedBuildable!);
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = unbuiltFob, Shovelable = shovelable }, CancellationToken.None);
                };
            }
            return;
        }
        // if it's the player's faction's rally point, register a new rally point
        if (e.Owner != null && e.Owner.IsInSquad() && e.Owner.Team.Faction.RallyPoint.MatchAsset(e.Buildable.Asset))
        {
            RegisterFob(new RallyPoint(e.Buildable, e.Owner.GetSquad()!, serviceProvider));
            return;
        }

        // other entities and shovelables get registered here
        TryRegisterEntity(e.Buildable, serviceProvider);
        TryCreateShoveable(e.Buildable, e.Owner, out _);
    }

    private void TryRegisterEntity(IBuildable buildable, IServiceProvider serviceProvider)
    {
        if (_entities.Any(x => x is IBuildableFobEntity b && b.Buildable.Equals(buildable)))
        {
            _logger.LogDebug($"Buildable {buildable} already registered as an entity.");
            return;
        }

        ShovelableInfo? completedFortification = Configuration.Shovelables
            .FirstOrDefault(s => s.Emplacement == null && s.CompletedStructure.MatchAsset(buildable.Asset));

        if (completedFortification == null)
            return;

        if (completedFortification.ConstuctionType == ShovelableType.RepairStation)
        {
            RepairStation repairStation = new RepairStation(buildable, this, serviceProvider);
            
            RegisterFobEntity(repairStation);
        }
        else if (completedFortification.ConstuctionType == ShovelableType.Fortification)
        {
            RegisterFobEntity(new FortificationEntity(buildable));
        }
    }

    private bool TryCreateShoveable(IBuildable buildable, WarfarePlayer? placer, out ShovelableBuildable? shovelable, bool shouldConsumeSupplies = true)
    {
        shovelable = null;

        ShovelableInfo? shovelableInfo = Configuration.Shovelables.FirstOrDefault(s => s.Foundation != null && s.Foundation.MatchAsset(buildable.Asset));

        if (shovelableInfo == null)
            return false;

        ShovelableBuildable newShovelable = new ShovelableBuildable(shovelableInfo, buildable, _serviceProvider, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"));
        shovelable = newShovelable;

        RegisterFobEntity(newShovelable);

        BunkerFob? nearestFriendlyFob = FindNearestBunkerFob(newShovelable.Buildable.Group, buildable.Position);

        if (nearestFriendlyFob != null && shouldConsumeSupplies)
        {
            NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(nearestFriendlyFob.Position, nearestFriendlyFob.Team.GroupId, this);
            supplyCrates.SubstractSupplies(shovelableInfo.SupplyCost, SupplyType.Build, SupplyChangeReason.ConsumeShovelablePlaced);
            
            placer?.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseBuild.Translate(shovelableInfo.SupplyCost, placer)));
        }

        return true;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<IBuildableDestroyedEvent>.HandleEvent(IBuildableDestroyedEvent e, IServiceProvider serviceProvider)
    {
        IBuildableFob? fob = FindBuildableFob<IBuildableFob>(e.Buildable);
        if (fob is BunkerFob buildableFob)
        {
            if (buildableFob.IsBuilt)
            {
                _logger.LogInformation("Replacing FOB foundation with unbuilt...");

                ItemPlaceableAsset unbuiltFob = _assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").GetAssetOrFail();

                IBuildable buildable = e.Buildable.ReplaceBuildable(unbuiltFob, destroyOld: false);
                buildableFob.MarkUnbuilt(buildable);

                _logger.LogInformation("FOB foundation successfully replaced with unbuilt version.");

                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = buildableFob, Event = e });
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
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = fob, Event = e });
            DeregisterFob(fob);
        }

        IBuildableFobEntity? entity = GetBuildableFobEntity<IBuildableFobEntity>(e.Buildable);
        if (entity == null)
            return;

        if (entity is ShovelableBuildable shovelable && e.WasSalvaged)
        {
            BunkerFob? nearestFriendlyFob = FindNearestBunkerFob(shovelable.Buildable.Group, shovelable.Buildable.Position);

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
            e.Instigator?.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastGainBuild.Translate(shovelable.Info.SupplyCost, e.Instigator)));
        }
       
        if (entity is SupplyCrate supplyCrate)
            NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, -supplyCrate.SupplyCount, SupplyChangeReason.ConsumeSuppliesDestroyed);
            
        DeregisterFobEntity(entity);
    }

    [EventListener(MustRunLast = true)]
    void IEventListener<DropItemRequested>.HandleEvent(DropItemRequested e, IServiceProvider serviceProvider)
    {
        InteractableVehicle vehicle = e.Player.UnturnedPlayer.movement.getVehicle();
        if (vehicle is null || vehicle.asset.engine.IsFlyingEngine())
            return;

        SupplyCrateInfo? supplyCrateInfo = Configuration.SupplyCrates.FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(e.Asset));
        if (supplyCrateInfo == null)
            return;

        Vector3 dropPos = FindDropPositionForSupplyCrate(vehicle);

        e.Position = dropPos;
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        ItemAsset asset = e.Item.GetAsset();
        SupplyCrateInfo? supplyCrateInfo = Configuration.SupplyCrates.FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(asset));

        bool isInMain = serviceProvider.GetService<ZoneStore>()?.IsInMainBase(e.ServersidePoint) ?? false;
        if (isInMain)
            return;
        
        if (supplyCrateInfo == null)
            return;

        _ = new FallingBuildable(
            e.Player,
            e.DroppedItem,
            supplyCrateInfo.SupplyItemAsset.GetAssetOrFail(),
            supplyCrateInfo.PlacementEffect?.GetAsset(),
            e.Player.Position,
            e.Player.Yaw,
            buildable =>
            {
                SupplyCrate supplyCrate = new SupplyCrate(supplyCrateInfo, buildable, serviceProvider, !e.Player.IsOnDuty);
                RegisterFobEntity(supplyCrate);

                NearbySupplyCrates
                    .FromSingleCrate(supplyCrate, this)
                    .NotifyChanged(supplyCrate.Type, supplyCrate.SupplyCount, SupplyChangeReason.ResupplyFob, e.Player);
                    
                string tipMsg = supplyCrate.Type == SupplyType.Ammo
                    ? serviceProvider.GetRequiredService<TranslationInjection<AmmoTranslations>>().Value.ToastGainAmmo.Translate(supplyCrate.SupplyCount, e.Player)
                    : _translations.ToastGainBuild.Translate(supplyCrate.SupplyCount, e.Player);

                e.Player.SendToast(new ToastMessage(ToastMessageStyle.Tip, tipMsg));
            }
        );
    }

    void IEventListener<VehicleSpawned>.HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        ShovelableInfo? emplacementShoveable = Configuration.Shovelables.FirstOrDefault(s => s.Emplacement != null && s.Emplacement.Vehicle.MatchAsset(e.Vehicle.Vehicle.asset));

        if (emplacementShoveable == null)
            return;

        RegisterFobEntity(new EmplacementEntity(e.Vehicle, emplacementShoveable.Foundation));

    }

    void IEventListener<VehicleDespawned>.HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
    {
        EmplacementEntity? emplacement = GetEmplacementFobEntity(e.Vehicle.Vehicle);
        if (emplacement == null)
            return;

        DeregisterFobEntity(emplacement);
    }

    [EventListener(MustRunLast = true)]
    void IEventListener<IDamageBuildableRequestedEvent>.HandleEvent(IDamageBuildableRequestedEvent e, IServiceProvider serviceProvider)
    {
        BunkerFob? correspondingFob = FindBuildableFob<BunkerFob>(e.Buildable);
        if (correspondingFob == null)
            return;

        if (!correspondingFob.IsBuilt) // only record damage on built fobs
            return;

        if (e.InstigatorId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            correspondingFob.DamageTracker.RecordDamage(e.InstigatorId, e.PendingDamage, e.DamageOrigin, e.InstigatorTeam.IsFriendly(e.Buildable.Group));
        else
            correspondingFob.DamageTracker.RecordDamage(e.DamageOrigin);
    }

    private const float MaxBoxRadius = 1.5f;
    private static Vector3 FindDropPositionForSupplyCrate(InteractableVehicle vehicle)
    {
        const float distanceToBack = 7.75f + MaxBoxRadius;
        const float distanceToFront = 4.25f + MaxBoxRadius;

        Vector3 vehiclePosition = vehicle.GetSentryTargetingPoint();

        Vector3 behind = vehicle.transform.TransformVector(Vector3.back);
        Vector3 front = vehicle.transform.TransformVector(Vector3.forward);

        // from player exit position code
        Vector3 backPos = RaycastFindEmptySpot(vehicle, vehiclePosition, behind, distanceToBack, out bool didHit);
        if (didHit)
        {
            Vector3 frontPos = RaycastFindEmptySpot(vehicle, vehiclePosition, front, distanceToFront, out didHit);
            if (!didHit)
                return frontPos;
        }

        return backPos;
    }

    private static readonly RaycastHit[] HitArray = new RaycastHit[32];
    private static Vector3 RaycastFindEmptySpot(InteractableVehicle vehicle, Vector3 origin, Vector3 direction, float maxDistance, out bool didHit)
    {
        didHit = false;
        float hitDistance = maxDistance;
        int amt = Physics.RaycastNonAlloc(new Ray(origin, direction), HitArray, maxDistance, RayMasks.BLOCK_EXIT);
        foreach (RaycastHit raycastHit in new ArraySegment<RaycastHit>(HitArray, 0, amt))
        {
            Transform transform = raycastHit.transform;
            if (transform != null && !transform.IsChildOf(vehicle.transform) && transform != vehicle.transform)
            {
                hitDistance = Mathf.Min(hitDistance, raycastHit.distance);
                didHit = true;
            }
        }

        return origin + direction * (hitDistance - (MaxBoxRadius / 2 + 0.1f));
    }
}