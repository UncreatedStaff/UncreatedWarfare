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
    private readonly FobConfiguration _fobConfiguration;
    private readonly VehicleService _vehicleService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ZoneStore? _zoneStore;
    private readonly ILoopTicker _repairTicker;
    private readonly ILoopTicker _refillTicker;
    private byte[]? _originalBarricadeState;

    public IBuildable Buildable { get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;
    public bool WipeStorageOnDestroy => true;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public RepairStation(IBuildable buildable, Team team, ILoopTickerFactory loopTickerFactory, VehicleService vehicleService, FobManager fobManager, AssetConfiguration assetConfiguration, ZoneStore? zoneStore)
    {
        Buildable = buildable;
        _fobConfiguration = fobManager.Configuration;
        _vehicleService = vehicleService;
        _assetConfiguration = assetConfiguration;
        _zoneStore = zoneStore;
        IdentifyingAsset = AssetLink.Create(Buildable.Asset);
        if (!buildable.IsStructure)
            _originalBarricadeState = buildable.GetItem<Barricade>().state;
        _repairTicker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(4), false, true);
        _repairTicker.OnTick += (_, _, _) =>
        {
            NearbySupplyCrates supplyCrateGroup = NearbySupplyCrates.FindNearbyCrates(buildable.Position, team.GroupId, fobManager);

            float maxRadius = Math.Max(
                _fobConfiguration.RepairStationAircraftRepairRadius,
                _fobConfiguration.RepairStationGroundVehicleRepairRadius
            );
            IEnumerable<WarfareVehicle> nearbyVehicles = _vehicleService.Vehicles.Where(v =>
                !v.Vehicle.isDead
                && v.Vehicle.lockedGroup == buildable.Group
                && v.Vehicle.ReplicatedSpeed < 3
                && !v.Info.Type.IsEmplacement()
                && MathUtility.WithinRange(buildable.Position, v.Position, maxRadius));
            
            foreach (WarfareVehicle vehicle in nearbyVehicles)
            {
                // planes and helis get a larger repair radius
                bool isGroundVehicle = !vehicle.Asset.engine.IsFlyingEngine();

                if (isGroundVehicle && !MathUtility.WithinRange(vehicle.Position, buildable.Position, _fobConfiguration.RepairStationGroundVehicleRepairRadius))
                    continue;

                if (!isGroundVehicle && !MathUtility.WithinRange(vehicle.Position, buildable.Position, _fobConfiguration.RepairStationAircraftRepairRadius))
                    continue;

                if (_zoneStore != null && !_zoneStore.IsInMainBase(vehicle.Position))
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
        _refillTicker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(60), false, true);
        _refillTicker.OnTick += (_, _, _) =>
        {
            if (Buildable.IsStructure || _originalBarricadeState == null)
                return;

            BarricadeDrop drop = Buildable.GetDrop<BarricadeDrop>();
            BarricadeUtility.WriteOwnerAndGroup(_originalBarricadeState, drop, Buildable.Owner.m_SteamID,
                Buildable.Group.m_SteamID);
            BarricadeUtility.SetState(drop, _originalBarricadeState);
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

    public void Dispose()
    {
        _repairTicker.Dispose();
        _refillTicker.Dispose();
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
