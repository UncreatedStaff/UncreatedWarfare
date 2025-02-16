using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using MathUtility = Uncreated.Warfare.Util.MathUtility;

namespace Uncreated.Warfare.FOBs.Construction;
public class RepairStation : IBuildableFobEntity, IDisposable
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ILoopTicker _ticker;

    public float GroundRepairRadius => 15;

    public float AircraftRepairRadius => 70;

    public IBuildable Buildable { get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public RepairStation(IBuildable buildable, Team team, ILoopTickerFactory loopTickerFactory, FobManager fobManager, AssetConfiguration assetConfiguration)
    {
        Buildable = buildable;
        _assetConfiguration = assetConfiguration;
        IdentifyingAsset = AssetLink.Create(Buildable.Asset);
        _ticker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(4), false, true);
        _ticker.OnTick += (_, _, _) =>
        {
            var supplyCrateGroup = NearbySupplyCrates.FindNearbyCrates(buildable.Position, team.GroupId, fobManager);

            List<InteractableVehicle> vehicles = ListPool<InteractableVehicle>.claim();

            VehicleManager.getVehiclesInRadius(buildable.Position, Mathf.Pow(AircraftRepairRadius, 2), vehicles);

            foreach (var vehicle in vehicles)
            {
                if (vehicle.isDead || vehicle.lockedGroup != buildable.Group || vehicle.ReplicatedSpeed > 3)
                    continue;

                // planes and helis get a larger repair radius
                bool isGroundVehicle = !(vehicle.asset.engine == EEngine.PLANE || vehicle.asset.engine == EEngine.HELICOPTER);
                if (isGroundVehicle && !MathUtility.WithinRange(vehicle.transform.position, buildable.Position, GroundRepairRadius))
                    continue;

                if (supplyCrateGroup.BuildCount <= 0)
                    break;

                supplyCrateGroup.SubstractSupplies(1, SupplyType.Build, SupplyChangeReason.ConsumeRepairVehicle);

                Repair(vehicle);
                Refuel(vehicle);
            }

            ListPool<InteractableVehicle>.release(vehicles);
        };
    }

    private void Refuel(InteractableVehicle vehicle)
    {
        if (vehicle.fuel >= vehicle.asset.fuel)
            return;

        const ushort amount = 260;

        vehicle.askFillFuel(amount);

        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:RepairStation:RefuelSound").GetAssetOrFail(),
            EffectManager.SMALL,
            vehicle.transform.position,
            true
        );

        vehicle.updateVehicle();
    }

    private void Repair(InteractableVehicle vehicle)
    {
        if (vehicle.health >= vehicle.asset.health)
            return;

        const ushort amount = 40;

        ushort newHealth = (ushort)Math.Min(vehicle.health + amount, ushort.MaxValue);
        if (vehicle.health + amount >= vehicle.asset.health)
        {
            newHealth = vehicle.asset.health;
            if (vehicle.transform.TryGetComponent(out WarfareVehicleComponent c))
            {
                c.WarfareVehicle.DamageTracker.ClearDamage();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle, newHealth);

        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:RepairStation:RepairSound").GetAssetOrFail(),
            EffectManager.SMALL,
            vehicle.transform.position,
            true
        );

        vehicle.updateVehicle();
    }

    public void Dispose()
    {
        _ticker.Dispose();
    }

    public override bool Equals(object? obj)
    {
        return obj is RepairStation repairStation && Buildable.Equals(repairStation.Buildable);
    }

    public override int GetHashCode()
    {
        return Buildable.GetHashCode();
    }
}
