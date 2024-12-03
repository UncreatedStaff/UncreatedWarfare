using SDG.NetTransport;
using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher2
{
    private static readonly ClientStaticMethod<uint, byte, byte>? SendSwapVehicleSeats = ReflectionUtility.FindRpc<VehicleManager, ClientStaticMethod<uint, byte, byte>>("SendSwapVehicleSeats");

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

    /// <summary>
    /// Invoked by <see cref="VehicleManager.onExitVehicleRequested"/> when a player tries to leave a vehicle.
    /// </summary>
    private void VehicleManagerOnPassengerExitRequested(Player unturnedPlayer, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        if (!vehicle.TryGetComponent(out VehicleComponent vehComp))
        {
            vehComp = vehicle.gameObject.AddComponent<VehicleComponent>();
            vehComp.Initialize(vehicle, (_warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider).Resolve<IServiceProvider>());
        }

        byte seat = player.UnturnedPlayer.movement.getSeat();

        ExitVehicleRequested args = new ExitVehicleRequested
        {
            Player = player,
            Component = vehComp,
            ExitLocation = pendingLocation,
            ExitLocationYaw = pendingYaw,
            PassengerIndex = seat,
            Vehicle = vehicle,
            JumpVelocity = InteractableVehicleRequestExit.LastVelocity
        };

        EventContinuations.Dispatch(args, this, player.DisconnectToken, out shouldAllow, args =>
        {
            VehicleManager.sendExitVehicle(args.Vehicle, args.PassengerIndex, args.ExitLocation, MeasurementTool.angleToByte(args.ExitLocationYaw), false);
            if (args.PassengerIndex == 0)
                args.Vehicle.GetComponent<Rigidbody>().velocity = args.JumpVelocity;
        });

        if (!shouldAllow)
            return;

        pendingLocation = args.ExitLocation;
        pendingYaw = args.ExitLocationYaw;
    }

    /// <summary>
    /// Invoked by <see cref="VehicleManager.onSwapSeatRequested"/> when a player tries to switch seats.
    /// </summary>
    private void VehicleManagerOnSwapSeatRequested(Player unturnedPlayer, InteractableVehicle vehicle, ref bool shouldAllow, byte fromSeatIndex, ref byte toSeatIndex)
    {
        if (SendSwapVehicleSeats == null)
            return;

        WarfarePlayer player = _playerService.GetOnlinePlayer(unturnedPlayer);

        if (!vehicle.TryGetComponent(out VehicleComponent vehComp))
        {
            vehComp = vehicle.gameObject.AddComponent<VehicleComponent>();
            vehComp.Initialize(vehicle, (_warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider).Resolve<IServiceProvider>());
        }

        VehicleSwapSeatRequested args = new VehicleSwapSeatRequested
        {
            Vehicle = vehicle,
            Player = player,
            Component = vehComp,
            NewPassengerIndex = toSeatIndex,
            OldPassengerIndex = fromSeatIndex
        };

        EventContinuations.Dispatch(args, this, player.DisconnectToken, out shouldAllow, args =>
        {
            args.Vehicle.lastSeat = Time.realtimeSinceStartup;
            SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), args.Vehicle.instanceID, (byte)args.OldPassengerIndex, (byte)args.NewPassengerIndex);
        });

        if (!shouldAllow)
            return;

        toSeatIndex = (byte)args.NewPassengerIndex;
    }
}