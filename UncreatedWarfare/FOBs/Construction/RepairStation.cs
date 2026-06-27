using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.Construction;
public class RepairStation : RestockableBuildableFobEntity<ShovelableInfo>
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly FobManager _fobManager;
    private readonly VehicleService _vehicleService;
    private readonly ZoneStore? _zoneStore;
    private readonly IPlayerService? _playerService;
    private readonly TipService? _tipService;
    private readonly ILoopTicker _repairTicker;

    public RepairStation(
        ShovelableInfo? info,
        Team team,
        IBuildable buildable,
        FobManager fobManager,
        IServiceProvider serviceProvider)
        : base(buildable, serviceProvider, true, info, team)
    {
        _fobManager = fobManager;

        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _vehicleService = serviceProvider.GetRequiredService<VehicleService>();

        _zoneStore = serviceProvider.GetService<ZoneStore>();
        _playerService = serviceProvider.GetService<IPlayerService>();
        _tipService = serviceProvider.GetService<TipService>();

        ILoopTickerFactory loopTickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();

        _repairTicker = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(4), false, true, RepairTick);
    }

    private void RepairTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        NearbySupplyCrates supplyCrateGroup = NearbySupplyCrates.FindNearbyCrates(Buildable.Position, Team.GroupId, _fobManager);

        float maxRadius = Math.Max(
            _fobManager.Configuration.RepairStationAircraftRepairRadius,
            _fobManager.Configuration.RepairStationGroundVehicleRepairRadius
        );
        IEnumerable<WarfareVehicle> nearbyVehicles = _vehicleService.Vehicles.Where(v =>
            !v.Vehicle.isDead
            && Team.IsFriendly(v.Vehicle.lockedGroup)
            && v.Vehicle.ReplicatedSpeed < 3
            && !v.Info.Type.IsEmplacement()
            && MathUtility.WithinRange(Buildable.Position, v.Position, maxRadius));

        foreach (WarfareVehicle vehicle in nearbyVehicles)
        {
            // planes and helis get a larger repair radius
            if (vehicle.Info.Type.IsGroundVehicle() && !MathUtility.WithinRange(vehicle.Position, Buildable.Position, _fobManager.Configuration.RepairStationGroundVehicleRepairRadius))
                continue;

            if (vehicle.Info.Type.IsAircraft() && !MathUtility.WithinRange(vehicle.Position, Buildable.Position, _fobManager.Configuration.RepairStationAircraftRepairRadius))
                continue;
            
            if (_zoneStore != null && !_zoneStore.IsInMainBase(vehicle.Position))
            {
                if (supplyCrateGroup.BuildCount > 0)
                {
                    supplyCrateGroup.SubtractSupplies(_fobManager.Configuration.RepairStationBuildConsumedPerTick, SupplyType.Build, SupplyChangeReason.ConsumeRepairVehicle);
                }
                else
                {
                    TryTipDriverNoBuild(vehicle);
                    continue;
                }
            }

            Repair(vehicle);
            Refuel(vehicle);
        }
    }

    private void TryTipDriverNoBuild(WarfareVehicle vehicle)
    {
        if (_playerService == null || _tipService == null)
            return;

        SteamPlayer? steamPlayer = vehicle.Vehicle.GetDriverClient();
        if (steamPlayer == null)
        {
            return;
        }

        WarfarePlayer driver = _playerService.GetOnlinePlayer(steamPlayer);
        _tipService.TryGiveTip(driver, cooldown: 15, t => t.RepairStationVehicleNoBuild);
    }

    private void Refuel(WarfareVehicle vehicle)
    {
        if (vehicle.Vehicle.fuel >= vehicle.Vehicle.asset.fuel)
            return;
        
        vehicle.Vehicle.askFillFuel(_fobManager.Configuration.RepairStationFuelPerTick);

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
        ushort newHealth = (ushort)Math.Clamp(vehicle.Vehicle.health + _fobManager.Configuration.RepairStationHealthPerTick, 0, vehicle.Vehicle.asset.health);
        if (newHealth >= vehicle.Vehicle.asset.health)
        {
            if (vehicle.Vehicle.transform.TryGetComponent(out WarfareVehicleComponent c))
            {
                c.WarfareVehicle.DamageTracker.ClearDamage();
            }
            
            if (vehicle.Vehicle.health >= newHealth) // vehicle is already full health, so health does not need to be updated
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
