using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    private IAssetLink<EffectAsset>? _firemodeEffect;

    /// <summary>
    /// Invoked by <see cref="VehicleManager.OnToggleVehicleLockRequested"/> when a player attempts to lock or unlock their vehicle.
    /// </summary>
    private void VehicleManagerOnToggleVehicleLockRequested(InteractableVehicle vehicle, ref bool shouldallow)
    {
        if (vehicle == null || !vehicle.isDriven || !shouldallow) 
            return;

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(vehicle.passengers[0].player);
        if (player is null)
            return;

        ChangeVehicleLockRequested args = new ChangeVehicleLockRequested
        {
            Player = player,
            Vehicle = vehicle
        };

        EventContinuations.Dispatch(args, this, _unloadToken, out shouldallow, continuation: args =>
        {
            bool isLocking = args.IsLocking;

            if (args.Vehicle == null || args.Vehicle.isDead || args.Vehicle.isLocked == isLocking)
                return;

            if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            {
                vehicleComponent.LastLocker = args.Player.Steam64;
            }

            VehicleManager.ServerSetVehicleLock(args.Vehicle, args.Player.Steam64, args.Player.GroupId, args.IsLocking);

            _firemodeEffect ??= AssetLink.Create<EffectAsset>(new Guid("bc41e0feaebe4e788a3612811b8722d3"));

            if (_firemodeEffect.TryGetAsset(out EffectAsset? firemodeAsset))
            {
                EffectManager.triggerEffect(new TriggerEffectParameters(firemodeAsset)
                {
                    position = args.Vehicle.transform.position,
                    relevantDistance = EffectManager.SMALL
                });
            }

            VehicleLockChanged finalArgs = new VehicleLockChanged
            {
                Player = args.Player,
                Vehicle = args.Vehicle
            };

            _ = DispatchEventAsync(finalArgs, CancellationToken.None);
        });

        if (!shouldallow)
            return;
        
        if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            vehicleComponent.LastLocker = player.Steam64;
        }
    }

    /// <summary>
    /// Invoked by <see cref="VehicleManager.OnToggledVehicleLock"/> when a vehicle is locked or unlocked.
    /// </summary>
    private void VehicleManagerOnToggledVehicleLock(InteractableVehicle vehicle)
    {
        WarfarePlayer? player = null;
        if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            player = _playerService.GetOnlinePlayerOrNull(vehicleComponent.LastLocker);
        }

        if (vehicle.lockedOwner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            player ??= _playerService.GetOnlinePlayerOrNull(vehicle.lockedOwner);
        }

        VehicleLockChanged args = new VehicleLockChanged
        {
            Player = player,
            Vehicle = vehicle
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }
}
