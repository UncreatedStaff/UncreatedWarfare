using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Vehicles;

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

        UCPlayer? player = UCPlayer.FromSteamPlayer(vehicle.passengers[0].player);
        if (player is null)
            return;

        ChangeVehicleLockRequested args = new ChangeVehicleLockRequested
        {
            Player = player,
            Vehicle = vehicle
        };

        UniTask<bool> task = DispatchEventAsync(args, CancellationToken.None);

        if (task.Status != UniTaskStatus.Pending)
        {
            if (args.IsActionCancelled)
            {
                shouldallow = false;
            }

            if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            {
                vehicleComponent.LastLocker = player.CSteamID;
            }

            return;
        }

        shouldallow = false;
        UniTask.Create(async () =>
        {
            if (!await task)
            {
                return;
            }

            await UniTask.SwitchToMainThread(_unloadToken);

            bool isLocking = args.IsLocking;

            if (args.Vehicle == null || args.Vehicle.isDead || args.Vehicle.isLocked == isLocking)
                return;

            if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
            {
                vehicleComponent.LastLocker = args.Player.CSteamID;
            }

            VehicleManager.ServerSetVehicleLock(args.Vehicle, args.Player.CSteamID, args.Player.Player.quests.groupID, args.IsLocking);

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
    }

    /// <summary>
    /// Invoked by <see cref="VehicleManager.OnToggledVehicleLock"/> when a vehicle is locked or unlocked.
    /// </summary>
    private void VehicleManagerOnToggledVehicleLock(InteractableVehicle vehicle)
    {
        UCPlayer? player = null;
        if (vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            player = UCPlayer.FromCSteamID(vehicleComponent.LastLocker);
        }

        if (vehicle.lockedOwner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            player ??= UCPlayer.FromCSteamID(vehicle.lockedOwner);
        }

        VehicleLockChanged args = new VehicleLockChanged
        {
            Player = player,
            Vehicle = vehicle
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }
}
