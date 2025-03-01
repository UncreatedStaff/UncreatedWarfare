using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;
using MathUtility = Uncreated.Warfare.Util.MathUtility;

namespace Uncreated.Warfare.FOBs.Construction;
public class RepairStation : IBuildableFobEntity, IDisposable
{
    private readonly VehicleService _vehicleService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ZoneStore _zoneStore;
    private readonly ILoopTicker _ticker;

    public float GroundRepairRadius => 15;

    public float AircraftRepairRadius => 70;

    public IBuildable Buildable { get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public RepairStation(IBuildable buildable, Team team, ILoopTickerFactory loopTickerFactory, VehicleService vehicleService, FobManager fobManager, AssetConfiguration assetConfiguration, ZoneStore zoneStore)
    {
        Buildable = buildable;
        _vehicleService = vehicleService;
        _assetConfiguration = assetConfiguration;
        _zoneStore = zoneStore;
        IdentifyingAsset = AssetLink.Create(Buildable.Asset);
        _ticker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(4), false, true);
        _ticker.OnTick += (_, _, _) =>
        {
            NearbySupplyCrates supplyCrateGroup = NearbySupplyCrates.FindNearbyCrates(buildable.Position, team.GroupId, fobManager);

            IEnumerable<WarfareVehicle> nearbyVehicles = _vehicleService.Vehicles.Where(v =>
                !v.Vehicle.isDead
                && v.Vehicle.lockedGroup == buildable.Group
                && v.Vehicle.ReplicatedSpeed > 3
                && !v.Info.Type.IsEmplacement()
                && MathUtility.WithinRange(buildable.Position, v.Position, AircraftRepairRadius));
            
            foreach (WarfareVehicle vehicle in nearbyVehicles)
            {
                // planes and helis get a larger repair radius
                bool isGroundVehicle = !(vehicle.Asset.engine == EEngine.PLANE || vehicle.Asset.engine == EEngine.HELICOPTER);
                if (isGroundVehicle && !MathUtility.WithinRange(vehicle.Position, buildable.Position, GroundRepairRadius))
                    continue;

                if (!_zoneStore.IsInMainBase(vehicle.Position))
                {
                    if (supplyCrateGroup.BuildCount > 0)
                        supplyCrateGroup.SubstractSupplies(1, SupplyType.Build, SupplyChangeReason.ConsumeRepairVehicle);
                    else
                        continue;
                }

                Repair(vehicle);
                Refuel(vehicle);
            }
        };
    }

    private void Refuel(WarfareVehicle vehicle)
    {
        if (vehicle.Vehicle.fuel >= vehicle.Vehicle.asset.fuel)
            return;

        const ushort amount = 260;

        vehicle.Vehicle.askFillFuel(amount);

        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:RepairStation:RefuelSound").GetAssetOrFail(),
            EffectManager.SMALL,
            vehicle.Position,
            true
        );

        vehicle.Vehicle.updateVehicle();
    }

    private void Repair(WarfareVehicle vehicle)
    {
        if (vehicle.Vehicle.health >= vehicle.Vehicle.asset.health)
            return;

        const ushort amount = 40;

        ushort newHealth = (ushort)Math.Min(vehicle.Vehicle.health + amount, ushort.MaxValue);
        if (vehicle.Vehicle.health + amount >= vehicle.Vehicle.asset.health)
        {
            newHealth = vehicle.Vehicle.asset.health;
            if (vehicle.Vehicle.transform.TryGetComponent(out WarfareVehicleComponent c))
            {
                c.WarfareVehicle.DamageTracker.ClearDamage();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle.Vehicle, newHealth);

        EffectUtility.TriggerEffect(
            _assetConfiguration.GetAssetLink<EffectAsset>("Effects:RepairStation:RepairSound").GetAssetOrFail(),
            EffectManager.SMALL,
            vehicle.Position,
            true
        );

        vehicle.Vehicle.updateVehicle();
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
