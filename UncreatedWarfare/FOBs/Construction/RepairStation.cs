using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Construction;
public class RepairStation : RestockableBuildableFobEntity
{
    private readonly FobConfiguration _fobConfiguration;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ILoopTicker _repairTicker;

    public RepairStation(
        IBuildable buildable,
        FobManager fobManager,
        IServiceProvider serviceProvider)
        : base(buildable, serviceProvider, true)
    {
        _fobConfiguration = fobManager.Configuration;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _repairTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromSeconds(4), false, true);

        VehicleService vehicleService = serviceProvider.GetRequiredService<VehicleService>();
        ZoneStore? zoneStore = serviceProvider.GetService<ZoneStore>();
        _repairTicker.OnTick += (_, _, _) =>
        {
            NearbySupplyCrates supplyCrateGroup = NearbySupplyCrates.FindNearbyCrates(Buildable.Position, Team.GroupId, fobManager);

            float maxRadius = Math.Max(
                _fobConfiguration.RepairStationAircraftRepairRadius,
                _fobConfiguration.RepairStationGroundVehicleRepairRadius
            );
            IEnumerable<WarfareVehicle> nearbyVehicles = vehicleService.Vehicles.Where(v =>
                !v.Vehicle.isDead
                && v.Vehicle.lockedGroup == Buildable.Group
                && v.Vehicle.ReplicatedSpeed < 3
                && !v.Info.Type.IsEmplacement()
                && MathUtility.WithinRange(Buildable.Position, v.Position, maxRadius));
            
            foreach (WarfareVehicle vehicle in nearbyVehicles)
            {
                if (vehicle.Vehicle.lockedGroup == CSteamID.Nil)
                    continue;
                
                // planes and helis get a larger repair radius
                if (vehicle.Info.Type.IsGroundVehicle() && !MathUtility.WithinRange(vehicle.Position, Buildable.Position, _fobConfiguration.RepairStationGroundVehicleRepairRadius))
                    continue;
                
                if (vehicle.Info.Type.IsAircraft() && !MathUtility.WithinRange(vehicle.Position, Buildable.Position, _fobConfiguration.RepairStationAircraftRepairRadius))
                    continue;
                
                if (zoneStore != null && !zoneStore.IsInMainBase(vehicle.Position))
                {
                    if (supplyCrateGroup.BuildCount > 0)
                        supplyCrateGroup.SubstractSupplies(fobManager.Configuration.RepairStationBuildConsumedPerTick, SupplyType.Build, SupplyChangeReason.ConsumeRepairVehicle);
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
        
        vehicle.Vehicle.askFillFuel(_fobConfiguration.RepairStationHealthPerTick);

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
        ushort newHealth = (ushort)Math.Clamp(vehicle.Vehicle.health + _fobConfiguration.RepairStationFuelPerTick, 0, vehicle.Vehicle.asset.health);
        if (newHealth >= vehicle.Vehicle.asset.health)
        {
            if (vehicle.Vehicle.transform.TryGetComponent(out WarfareVehicleComponent c))
            {
                c.WarfareVehicle.DamageTracker.ClearDamage();
            }
            
            return;
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

    public override void Dispose()
    {
        _repairTicker.Dispose();
        base.Dispose();
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
