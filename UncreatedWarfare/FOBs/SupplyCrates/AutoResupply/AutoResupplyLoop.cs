using System;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.SupplyCrates.AutoResupply;

public class AutoResupplyLoop : ILayoutHostedService, ILayoutStartingListener
{
    private readonly VehicleService _vehicleService;
    private readonly ZoneStore _zoneStore;
    private readonly IPlayerService _playerService;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _resupplyLoop;
    private readonly AmmoTranslations _translations;

    public AutoResupplyLoop(VehicleService vehicleService, ZoneStore zoneStore, IPlayerService playerService, ILoopTickerFactory loopTickerFactory, TranslationInjection<AmmoTranslations> ammoTranslations)
    {
        _vehicleService = vehicleService;
        _zoneStore = zoneStore;
        _playerService = playerService;
        _loopTickerFactory = loopTickerFactory;
        _translations = ammoTranslations.Value;
    }
    
    public UniTask StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
    public UniTask StopAsync(CancellationToken token)
    {
        _resupplyLoop?.Dispose();
        return UniTask.CompletedTask;
    }
    public UniTask HandleLayoutStartingAsync(Layout layout, CancellationToken token = default)
    {
        _resupplyLoop?.Dispose();
        _resupplyLoop = _loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(7), false, true, OnLoopTick);
        return UniTask.CompletedTask;
    }
    private void OnLoopTick(ILoopTicker ticker, TimeSpan timesincestart, TimeSpan deltatime)
    {
        foreach (WarfareVehicle vehicle in _vehicleService.Vehicles)
        {
            if (vehicle.Vehicle.ReplicatedSpeed > 0.5f)
                continue;

            bool isInMain = _zoneStore.IsInMainBase(vehicle.Position);

            if (!isInMain)
            {
                if (!vehicle.NeedsAutoResupply)
                    vehicle.NeedsAutoResupply = true;
                continue;
            }
            
            if (!vehicle.NeedsAutoResupply)
                continue;
            
            if (vehicle.Info.Trunk.Count == 0)
                continue;
            
            _vehicleService.RefillTrunkItems(vehicle, vehicle.Info.Trunk);
            vehicle.FlareEmitter?.ReloadCountermeasures();
            vehicle.NeedsAutoResupply = false;

            WarfarePlayer? owner = _playerService.GetOnlinePlayerOrNull(vehicle.Vehicle.lockedOwner);
            owner?.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.VehicleAutoSupply.Translate(owner)));
        }
    }
}