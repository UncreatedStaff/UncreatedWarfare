using Autofac.Features.OwnedInstances;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.Construction;
internal class RepairStation : IBuildableComponent, IFobItem
{
    private readonly FobManager _fobManager;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ILoopTicker _ticker;

    public IBuildable Buildable { get; }
    public float GroundRepairRadius { get; } = 15;
    public float AircraftRepairRadius { get; } = 70;

    public RepairStation(IBuildable buildable, Team team, ILoopTickerFactory loopTickerFactory, FobManager fobManager, AssetConfiguration assetConfiguration)
    {
        Buildable = buildable;
        _fobManager = fobManager;
        _assetConfiguration = assetConfiguration;
        _ticker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(4), false, true);
        _ticker.OnTick += (ticker, timeSinceStart, deltaTime) =>
        {
            var supplyCrateGroup = new SupplyCrateGroup(fobManager, buildable.Position, team);

            List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
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
            if (vehicle.transform.TryGetComponent(out VehicleComponent c))
            {
                c.DamageTable.Clear();
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
}
