using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
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
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.Rallypoints;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs;

public partial class FobManager :
    IEventListener<IBuildablePlacedEvent>,
    IEventListener<PlaceBarricadeRequested>,
    IEventListener<IBuildableDestroyedEvent>,
    IEventListener<DropItemRequested>,
    IEventListener<ItemDropped>,
    IEventListener<VehicleSpawned>,
    IEventListener<VehicleDespawned>,
    IEventListener<TriggerTrapRequested>,
    IEventListener<IDamageBuildableRequestedEvent>,
    IEventListener<IBuildableDamagedEvent>
{
    void IEventListener<PlaceBarricadeRequested>.HandleEvent(PlaceBarricadeRequested e, IServiceProvider serviceProvider)
    {
        if (e.Asset is not ItemTrapAsset trap)
        {
            ShovelableInfo? shovelable = Configuration.Shovelables.FirstOrDefault(x => x.Foundation.MatchAsset(e.Asset));
            if (shovelable == null
                || !shovelable.CompletedStructure.TryGetAsset(out ItemPlaceableAsset? placeable)
                || placeable is not ItemTrapAsset t)
            {
                return;
            }

            trap = t;
        }

        // dont allow placing traps near FOBs
        bool tooNear = false;
        Vector3 pos = e.Position;
        float baseDistance = !trap.isExplosive ? 7 : 3;
        foreach (IBuildableFob fob in Fobs.OfType<IBuildableFob>())
        {
            bool isFriendly = fob.Team.IsFriendly(e.OriginalPlacer.Team);
            if (!MathUtility.WithinRange2D(in pos, fob.SpawnPosition, !isFriendly ? baseDistance + 5 : baseDistance))
                continue;

            tooNear = true;
            break;
        }

        if (!tooNear)
            return;

        _chatService.Send(e.OriginalPlacer, _translations.TrapNotAllowed);

        e.Cancel();
    }

    void IEventListener<TriggerTrapRequested>.HandleEvent(TriggerTrapRequested e, IServiceProvider serviceProvider)
    {
        ItemTrapAsset asset = (ItemTrapAsset)e.Barricade.asset;
        Vector3 pos = asset.isExplosive
            ? e.Barricade.GetServersideData().point
            : e.TriggerCollider.transform.position;

        bool tooNear = false;
        float baseDistance = asset.isExplosive ? 3 : 7;
        foreach (IBuildableFob fob in Fobs.OfType<IBuildableFob>())
        {
            bool isFriendly = fob.Team.IsFriendly(new CSteamID(e.Barricade.GetServersideData().group));
            if (!MathUtility.WithinRange2D(in pos, fob.SpawnPosition, isFriendly ? baseDistance + 5 : baseDistance))
                continue;

            tooNear = true;
            break;
        }

        if (tooNear)
        {
            e.Cancel();
        }
    }

    [EventListener(RequireNextFrame = true)]
    void IEventListener<IBuildablePlacedEvent>.HandleEvent(IBuildablePlacedEvent e, IServiceProvider serviceProvider)
    {
        Team team = _teamManager.GetTeam(e.Buildable.Group);
        if (!team.IsValid)
            return;

        ShovelableBuildable? shovelable;

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
            if (TryCreateShovelable(e.Buildable, team, out shovelable))
            {
                shovelable.IsIconVisible = !unbuiltFob.HasBeenRebuilt;
                shovelable.OnComplete += completedBuildable =>
                {
                    if (unbuiltFob == null)
                        return;
                    
                    TrackingList<SupplyCrate> findNearbyPlacedSupplyCrates = FindNearbyFobCreationCrates(shovelable.Position, shovelable.Team);
                    float totalBuildFromSupplyCrates = findNearbyPlacedSupplyCrates.Sum(s => s.SupplyCount);
                    // don't need to check if there are nearby crates here
                    unbuiltFob.ChangeSupplies(totalBuildFromSupplyCrates, totalBuildFromSupplyCrates, SupplyChangeReason.InitialSupplyFob);
                    foreach (SupplyCrate supplyCrate in findNearbyPlacedSupplyCrates)
                    {
                        supplyCrate.Buildable.Destroy();
                    }

                    unbuiltFob.MarkBuilt(completedBuildable!);
                    _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobBuilt { Fob = unbuiltFob, Shovelable = shovelable }, CancellationToken.None);
                };

                unbuiltFob.Shovelable = shovelable;
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
        TryRegisterEntity(e.Buildable, team, serviceProvider);
        TryCreateShoveableForFobEntity(e.Buildable, team, e.Owner, out shovelable);
    }

    private void TryRegisterEntity(IBuildable buildable, Team team, IServiceProvider serviceProvider)
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

        if (completedFortification.ConstuctionType == ShovelableType.FobAmmoVendor)
        {
            FobAmmoVendor fobAmmoVendor = new FobAmmoVendor(completedFortification, team, buildable, this, serviceProvider);
            RegisterFobEntity(fobAmmoVendor);
        }
        else if (completedFortification.ConstuctionType == ShovelableType.RepairStation)
        {
            RepairStation repairStation = new RepairStation(completedFortification, team, buildable, this, serviceProvider);
            RegisterFobEntity(repairStation);
        }
        else if (completedFortification.ConstuctionType == ShovelableType.Fortification)
        {
            RegisterFobEntity(new FortificationEntity(completedFortification, team, buildable, serviceProvider));
        }
    }

    private bool TryCreateShoveableForFobEntity(IBuildable buildable, Team team, WarfarePlayer? placer, [NotNullWhen(true)] out ShovelableBuildable? newShovelable)
    {
        if (!TryCreateShovelable(buildable, team, out newShovelable))
            return false;

        BunkerFob? nearestFriendlyFob = FindNearestBunkerFob(newShovelable.Buildable.Group, buildable.Position);
        if (nearestFriendlyFob != null)
        {
            nearestFriendlyFob.ChangeBuild(-newShovelable.Info.SupplyCost, SupplyChangeReason.ConsumeShovelablePlaced);
            
            placer?.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseBuild.Translate(newShovelable.Info.SupplyCost, placer)));
        }

        return true;
    }

    private bool TryCreateShovelable(IBuildable buildable, Team team, [NotNullWhen(true)] out ShovelableBuildable? shovelable)
    {
        shovelable = null;

        if (!team.IsValid)
            return false;

        ShovelableInfo? shovelableInfo = Configuration.Shovelables.FirstOrDefault(s => s.Foundation.MatchAsset(buildable.Asset));
        if (shovelableInfo == null)
            return false;

        ShovelableBuildable newShovelable = new ShovelableBuildable(team, shovelableInfo, buildable, _serviceProvider, _assetConfiguration.GetAssetLink<EffectAsset>("Effects:ShovelHit"));
        shovelable = newShovelable;

        RegisterFobEntity(newShovelable);
        return true;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<IBuildableDestroyedEvent>.HandleEvent(IBuildableDestroyedEvent e, IServiceProvider serviceProvider)
    {
        IBuildableFob? fob = FindBuildableFob<IBuildableFob>(e.Buildable);

        if (BuildableExtensions.TryGetBuildableBounds(e.Buildable.Asset, out Bounds buildableBounds))
        {
            float r = Math.Max(buildableBounds.size.x, buildableBounds.size.z) + 4f;
            Vector3 position = e.Buildable.Position;
            UniTask.Create(async () =>
            {
                // wait for buildable to be destroyed
                await UniTask.NextFrame();
                UpdateNearbySupplyCrateSupports(position, r);
            });
        }

        if (fob is BunkerFob buildableFob)
        {
            if (buildableFob.IsBuilt)
            {
                _logger.LogDebug("Replacing FOB foundation with unbuilt...");

                ItemPlaceableAsset unbuiltFob = _assetConfiguration.GetAssetLink<ItemPlaceableAsset>("Buildables:Gameplay:FobUnbuilt").GetAssetOrFail();

                IBuildable buildable = e.Buildable.ReplaceBuildable(unbuiltFob, destroyOld: false);
                buildableFob.MarkUnbuilt(buildable);

                _logger.LogDebug("FOB foundation successfully replaced with unbuilt version.");

                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = buildableFob, Event = e });
            }
            else
            {
                _logger.LogDebug("Buildable fob base was destroyed and will be deregistered.");
                DeregisterFob(fob, e);
            }
        }
        else if (fob != null)
        {
            _logger.LogDebug("Attempting to destroy other buildable fob.");
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDestroyed { Fob = fob, Event = e });
            DeregisterFob(fob, e);
        }

        IBuildableFobEntity? entity = GetBuildableFobEntity<IBuildableFobEntity>(e.Buildable);
        if (entity == null)
            return;

        if (entity is ShovelableBuildable shovelable && e.WasSalvaged)
        {
            BunkerFob? nearestFriendlyFob = FindNearestBunkerFob(shovelable.Buildable.Group, shovelable.Buildable.Position);

            if (nearestFriendlyFob != null)
            {
                // refund supplies back to nearby FOB
                nearestFriendlyFob.ChangeBuild(shovelable.Info.SupplyCost, SupplyChangeReason.ResupplyShoveableSalvaged);
                e.Instigator?.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastGainBuild.Translate(shovelable.Info.SupplyCost, e.Instigator)));
            }
            
            // otherwise, there's no FOB nearby so we don't need to do anything
        }
        
        DeregisterFobEntity(entity);
    }

    private void UpdateNearbySupplyCrateSupports(Vector3 buildablePosition, float radius)
    {
        radius *= radius;
        foreach (SupplyCrate crate in _entities.OfType<SupplyCrate>())
        {
            float sqrDst = (crate.Position - buildablePosition).sqrMagnitude;
            if (sqrDst > radius)
                continue;

            if (crate.Stack.Crates.Count > 1)
            {
                // don't bother with stacks
                continue;
            }

            crate.RecheckSupport();
        }
    }

    private bool CheckValidSupplyCrateDropLocation(SupplyCrateInfo supplyCrateInfo, WarfarePlayer player, Team team, Vector3 estimatedDropPosition)
    {
        Zone? mainBase = _zoneStore.FindClosestZone(estimatedDropPosition, ZoneType.MainBase, Configuration.MinFobDistanceFromMain);
        if (mainBase != null)
        {
            // is too near main base
            _chatService.SendHint(player, _translations.DropSupplyCrateTooNearMain, Configuration.MinFobDistanceFromMain, 8f);
            return false;
        }

        if (WaterUtility.isPointUnderwater(estimatedDropPosition))
        {
            // will likely drop underwater
            _chatService.SendHint(player, _translations.DropSupplyCrateUnderwater, 8f);
            return false;
        }

        if (supplyCrateInfo.Type != CrateType.FobCreation)
            return true;

        BunkerFob? fobInRange = FindNearestBunkerFob(team, estimatedDropPosition);
        if (fobInRange != null)
        {
            // is restocking existing FOB.
            return true;
        }

        BunkerFob? nearestBunkerFob = _fobs.OfType<BunkerFob>()
            .AggregateOrDefault((curr, next) => (curr.Position - estimatedDropPosition).sqrMagnitude > (next.Position - estimatedDropPosition).sqrMagnitude ? next : curr);

        if (nearestBunkerFob == null)
        {
            // is making new FOB
            return true;
        }

        if (MathUtility.WithinRange(nearestBunkerFob.Position, in estimatedDropPosition, Configuration.MinDistanceBetweenFobs))
        {
            // too close to an existing FOB to make a new FOB
            if (nearestBunkerFob.Team.IsFriendly(team))
                _chatService.SendHint(player, _translations.DropSupplyCrateTooNearFriendlyFob, nearestBunkerFob, Configuration.MinDistanceBetweenFobs, 8f);
            else
                _chatService.SendHint(player, _translations.DropSupplyCrateTooNearEnemyFob, Configuration.MinDistanceBetweenFobs, 8f);
            return false;
        }

        return true;
    }

    [EventListener]
    void IEventListener<DropItemRequested>.HandleEvent(DropItemRequested e, IServiceProvider serviceProvider)
    {
        InteractableVehicle vehicle = e.Player.UnturnedPlayer.movement.getVehicle();
        if (vehicle == null || !e.Player.Team.IsValid)
            return;

        if (vehicle.isDead || vehicle.isExploded)
        {
            e.Cancel();
            return;
        }

        SupplyCrateInfo? supplyCrateInfo = Configuration.SupplyCrates.FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(e.Asset));
        if (supplyCrateInfo == null)
            return;

        Transform? dropTransform = null;
        if (vehicle.asset.engine.IsFlyingEngine() && TerrainUtility.GetDistanceToGround(vehicle.transform.position) > 6)
        {
            dropTransform = vehicle.transform.Find("Drop_Flying");
        }

        dropTransform ??= vehicle.transform.Find("Drop");
        dropTransform ??= e.Player.Transform;

        /* VALIDATION */

        Vector3 dropPoint = dropTransform.transform.position;

        float groundPointY = TerrainUtility.GetHighestPoint(in dropPoint, float.NaN);

        Vector3 estDropPoint = dropPoint with { y = groundPointY };

        if (vehicle.asset.engine.IsFlyingEngine())
        {
            // limit of 150m above terrain to drop crates
            float limit = Configuration.SupplyCrateMaxDropHeight;

            if (dropPoint.y - groundPointY > limit)
            {
                _chatService.SendHint(e.Player, _translations.DropSupplyCrateTooHigh, limit);
                e.Cancel();
                return;
            }
        }

        if (!CheckValidSupplyCrateDropLocation(supplyCrateInfo, e.Player, e.Player.Team, estDropPoint))
        {
            e.Cancel();
            return;
        }

        FallingCrateArgs<BunkerFob> args = new FallingCrateArgs<BunkerFob>
        {
            Buildable = (ItemPlaceableAsset)e.Asset,
            OnConverted = (effect, _) =>
            {
                if (effect is IFobEntity entity)
                    RegisterFobEntity(entity);
            }
        };

        if (supplyCrateInfo.Type == CrateType.FobCreation)
        {
            args.OnDroppedNearFob = (fob, effect) =>
            {
                fob.ChangeSupplies(supplyCrateInfo.StartingSupplies, supplyCrateInfo.StartingSupplies, SupplyChangeReason.ResupplyFob, e.Player);
                effect.PlayPlacementEffect();
                return true;
            };
        }

        if (_fallingEffectManager.IsFallingEffectObstructed(e.Asset, dropTransform))
        {
            e.Cancel();
            _chatService.SendHint(e.Player, _translations.DropSupplyCrateObstructed);
            return;
        }

        _fallingEffectManager.CreateFallingEffect<FallingBunkerFobCrateEffect, FallingCrateArgs<BunkerFob>>(
            e.Asset,
            dropTransform,
            ref args,
            (ref effect) =>
            {
                effect.Owner = e.Player;
                effect.Team = e.Player.Team;
            });

        e.Cancel();
        if (e.Page != (Page)255)
        {
            PlayerInventory inv = e.Player.UnturnedPlayer.inventory;
            byte index = inv.getIndex((byte)e.Page, e.X, e.Y);

            if (index != byte.MaxValue)
                inv.removeItem((byte)e.Page, index);
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (e.Item == null || e.DroppedItem == null)
            return;

        ItemAsset? asset = e.Asset;
        if (asset == null)
            return;

        SupplyCrateInfo? supplyCrateInfo = Configuration.SupplyCrates.FirstOrDefault(s => s.SupplyItemAsset.MatchAsset(asset));

        if (supplyCrateInfo == null)
            return;

        Team team = e.Player.Team;
        if (!CheckValidSupplyCrateDropLocation(supplyCrateInfo, e.Player, team, e.LandingPoint))
        {
            return;
        }

        bool isInMain = serviceProvider.GetService<ZoneStore>()?.IsInMainBase(e.ServersidePoint) ?? false;
        if (isInMain)
            return;
        
        if (!team.IsValid)
            return;

        _ = new FallingCrate(
            e.Player,
            e.DroppedItem,
            e.LandingPoint,
            e.DropPoint,
            supplyCrateInfo,
            e.Player.Yaw,
            serviceProvider,
            shouldConvertToBuildable: fallingCrate =>
            {
                if (supplyCrateInfo.Type == CrateType.FobCreation)
                {
                    BunkerFob? nearestFob = FindNearestBunkerFob(team, e.LandingPoint, includeUnbuilt: false);
                    if (nearestFob != null)
                    {
                        nearestFob.ChangeSupplies(supplyCrateInfo.StartingSupplies, supplyCrateInfo.StartingSupplies, SupplyChangeReason.ResupplyFob, e.Player);
                        fallingCrate.PlayPlacementEffect();
                        return false;
                    }
                }

                return true;
            },
            onConvertedToBuildable: RegisterFobEntity
        );
    }

    void IEventListener<VehicleSpawned>.HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        ShovelableInfo? emplacementShoveable = Configuration.Shovelables.FirstOrDefault(s => s.Emplacement != null && s.Emplacement.Vehicle.MatchAsset(e.Vehicle.Vehicle.asset));

        if (emplacementShoveable == null)
            return;

        Team team = _teamManager.GetTeam(e.Vehicle.Vehicle.lockedGroup);
        if (!team.IsValid)
            return;

        RegisterFobEntity(new EmplacementEntity(e.Vehicle, team, emplacementShoveable.Foundation));

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
        // note this shouldn't run in IBuildableDamagedEvent because that doesn't run if the buildable is destroyed

        IDamageableFob? correspondingFob = FindBuildableFob<IDamageableFob>(e.Buildable);
        if (correspondingFob == null)
            return;

        _logger.LogTrace($"Recording damage: {e.PendingDamage} {e.DamageOrigin}");
        if (!correspondingFob.CanRecordDamage) // only record damage on built fobs
            return;

        if (e.InstigatorId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            correspondingFob.DamageTracker.RecordDamage(e.InstigatorId, e.PendingDamage, e.DamageOrigin, e.InstigatorTeam.IsFriendly(e.Buildable.Group));
        else
            correspondingFob.DamageTracker.RecordDamage(e.DamageOrigin);
    }

    void IEventListener<IBuildableDamagedEvent>.HandleEvent(IBuildableDamagedEvent e, IServiceProvider serviceProvider)
    {
        ResourceFob? correspondingFob = FindBuildableFob<ResourceFob>(e.Buildable);
        correspondingFob?.InvokeHealthUpdated();
    }

    private const float MaxBoxRadius = 1.5f;
    private static readonly Collider?[] ColliderBuffer = new Collider?[1];
    private static Vector3 FindDropPositionForSupplyCrate(InteractableVehicle vehicle, Vector3 playerSeatPosition)
    {
        if (vehicle.asset.engine.IsFlyingEngine() && TerrainUtility.GetDistanceToGround(in playerSeatPosition) > 2.5f)
        {
            // toss out side of vehicle, choose side closest to player (based on their relative position from the center of the vehicle)
            Vector3 relativeSeatPosition = vehicle.transform.InverseTransformPoint(playerSeatPosition);
            Vector3 tossVector = vehicle.transform.TransformVector(relativeSeatPosition.x >= 0 ? 0.5f : -0.5f, 0f, 0f);
            do
            {
                playerSeatPosition += tossVector;
            } while (Physics.OverlapSphereNonAlloc(playerSeatPosition, 2.5f, ColliderBuffer, RayMasks.VEHICLE) > 0);

            ColliderBuffer[0] = null;

            return playerSeatPosition;
        }

        const float distanceToBack = 7.75f + MaxBoxRadius;
        const float distanceToFront = 5.25f + MaxBoxRadius;

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
        int amt = Physics.RaycastNonAlloc(new Ray(origin, direction), HitArray, maxDistance, RayMasks.BLOCK_ITEM | RayMasks.LOGIC, QueryTriggerInteraction.Collide);
        foreach (RaycastHit raycastHit in new ArraySegment<RaycastHit>(HitArray, 0, amt))
        {
            Transform transform = raycastHit.transform;
            if (transform != null && !transform.IsChildOf(vehicle.transform) && transform != vehicle.transform)
            {
                if (transform.gameObject.layer == LayerMasks.LOGIC && transform.TryGetComponent(out SupplyStackComponent stack))
                {
                    didHit = false;
                    if (stack.Stack.TryGetNextCratePosition(out _, out _, out Vector3 position))
                    {
                        return position + Vector3.up;
                    }
                }

                hitDistance = Mathf.Min(hitDistance, raycastHit.distance);
                didHit = true;
            }
        }

        return origin + direction * (hitDistance - (MaxBoxRadius / 2 + 0.1f));
    }
}