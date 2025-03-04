using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Fobs;
public partial class FobManager :
    IEventListener<IBuildablePlacedEvent>,
    IEventListener<PlaceBarricadeRequested>,
    IEventListener<IBuildableDestroyedEvent>,
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

        if (e.OriginalPlacer != null)
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
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = unbuiltFob }, CancellationToken.None);
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
        ShovelableInfo? completedFortification = Configuration.Shovelables
            .FirstOrDefault(s => s.Emplacement == null && s.CompletedStructure.MatchAsset(buildable.Asset));

        if (_assetConfiguration.GetAssetLink<ItemBarricadeAsset>("Buildables:Gameplay:RepairStation").MatchAsset(buildable.Asset))
        {
            Team team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);
            RepairStation repairStation = new RepairStation(buildable, team, serviceProvider.GetRequiredService<ILoopTickerFactory>(), serviceProvider.GetRequiredService<VehicleService>(), this, _assetConfiguration, serviceProvider.GetService<ZoneStore>());
            RegisterFobEntity(repairStation);
        }
        else if (completedFortification != null)
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

        ShovelableBuildable? shovelable = GetBuildableFobEntity<ShovelableBuildable>(e.Buildable);
        if (shovelable != null && e.WasSalvaged)
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
       
        SupplyCrate? supplyCrate = _entities.OfType<SupplyCrate>().FirstOrDefault(i => i.Buildable.Equals(e.Buildable));
        if (supplyCrate != null)
        {
            NearbySupplyCrates.FromSingleCrate(supplyCrate, this).NotifyChanged(supplyCrate.Type, -supplyCrate.SupplyCount, SupplyChangeReason.ConsumeSuppliesDestroyed);
            
            // clear barricade state to prevent items from dropping out of the crate after it is destroyed
            if (!supplyCrate.Buildable.IsStructure && supplyCrate.Buildable.GetDrop<BarricadeDrop>() is { interactable: InteractableStorage { items: { } } storage })
            {
                for (int i = storage.items.getItemCount(); i > 0; --i)
                {
                    storage.items.removeItem(0);
                }
                storage.rebuildState();
            }
        }

        _entities.RemoveAll(en => en is IBuildableFobEntity bfe && bfe.Buildable.Equals(e.Buildable));
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        ItemAsset asset = e.Item.GetAsset();
        SupplyCrateInfo? supplyCrateInfo = Configuration.SupplyCrates.FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(asset));

        if (supplyCrateInfo != null)
        {
            _ = new FallingBuildable(
                e.Player,
                e.DroppedItem,
                supplyCrateInfo.SupplyItemAsset.GetAssetOrFail(),
                supplyCrateInfo.PlacementEffect?.GetAsset(),
                e.Player.Position,
                e.Player.Yaw,
                buildable =>
                {
                    SupplyCrate supplyCrate = new SupplyCrate(supplyCrateInfo, buildable, serviceProvider.GetRequiredService<ILoopTickerFactory>(), !e.Player.IsOnDuty);
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
            return;
        }
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
            correspondingFob.DamageTracker.RecordDamage(e.InstigatorId, e.PendingDamage, e.DamageOrigin);
        else
            correspondingFob.DamageTracker.RecordDamage(e.DamageOrigin);
    }
}