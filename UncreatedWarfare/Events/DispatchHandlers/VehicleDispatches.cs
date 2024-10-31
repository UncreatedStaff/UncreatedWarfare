using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

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

            if (args.Vehicle.TryGetComponent(out VehicleComponent vehicleComponent))
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

    /// <summary>
    /// Invoked by <see cref="VehicleManager.OnVehicleExploded"/> when a vehicle is destroyed.
    /// </summary>
    private void VehicleManagerOnVehicleExploded(InteractableVehicle vehicle)
    {
        VehicleComponent component = vehicle.gameObject.GetOrAddComponent<VehicleComponent>();

        ITeamManager<Team>? teamManager = _warfare.IsLayoutActive() ? _warfare.ScopedProvider.Resolve<ITeamManager<Team>>() : null;

        WarfarePlayer? instigator = null, lastDriver = null;
        CSteamID instigatorId = CSteamID.Nil, lastDriverId = CSteamID.Nil;

        Team? instigatorTeam = null;

        WarfarePlayer? owner = _playerService.GetOnlinePlayerOrNull(vehicle.lockedOwner);

        if (component.LastInstigator != 0)
        {
            instigatorId = new CSteamID(component.LastInstigator);
            instigator = _playerService.GetOnlinePlayerOrNull(instigatorId);

            instigatorTeam = instigator == null ? null : teamManager?.GetTeam(instigator.UnturnedPlayer.quests.groupID);
        }

        if (component.LastDriver != 0)
        {
            lastDriverId = new CSteamID(component.LastDriver);
            lastDriver = _playerService.GetOnlinePlayerOrNull(instigatorId);
        }

        EDamageOrigin origin = component.LastDamageOrigin;
        InteractableVehicle? activeVehicle = component.LastDamagedFromVehicle;

        VehicleExploded args = new VehicleExploded
        {
            Vehicle = vehicle,
            Component = component,
            DamageOrigin = origin,
            InstigatorVehicle = activeVehicle,
            Team = teamManager?.GetTeam(vehicle.lockedGroup) ?? Team.NoTeam,
            Instigator = instigator,
            InstigatorId = instigatorId,
            InstigatorTeam = instigatorTeam,
            LastDriver = lastDriver,
            LastDriverId = lastDriverId,
            Owner = owner,
            OwnerId = vehicle.lockedOwner
        };

        _ = DispatchEventAsync(args, CancellationToken.None);
    }
}