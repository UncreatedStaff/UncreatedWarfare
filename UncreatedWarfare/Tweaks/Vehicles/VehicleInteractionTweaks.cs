using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Info;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks.Vehicles;
internal class VehicleInteractionTweaks :
    IEventListener<EnterVehicleRequested>,
    IEventListener<VehicleSwapSeatRequested>,
    IEventListener<ExitVehicleRequested>
{
    public const float MaxAllowedHeightToExitVehicle = 20f;

    private readonly ILogger<VehicleInteractionTweaks> _logger;
    private readonly ChatService _chatService;
    private readonly VehicleInfoStore? _vehicleInfoStore;
    private readonly VehicleTweaksTranslations _translations;
    private readonly ZoneStore? _zoneStore;
    private readonly PlayerService? _playerService;

    public VehicleInteractionTweaks(ILogger<VehicleInteractionTweaks> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _vehicleInfoStore = serviceProvider.GetService<VehicleInfoStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<VehicleTweaksTranslations>>().Value;
        _zoneStore = serviceProvider.GetService<ZoneStore>();
        _playerService = serviceProvider.GetService<PlayerService>();
    }
    public void HandleEvent(EnterVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (_vehicleInfoStore == null)
            return;

        WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(e.Vehicle.asset.GUID);

        if (info == null)
            return;

        Class playerKitClass = e.Player.Component<KitPlayerComponent>().ActiveClass;

        // ensure that if the player is entering an emplacement, they always enter the next available seat after the driver's seat
        if (info.Type.IsEmplacement() && e.Seat == 0)
        {
            int? nextAvailableGunnerSeat = FindAvailableNonDriverSeat(e.Vehicle);
            if (nextAvailableGunnerSeat != null)
                e.Seat = nextAvailableGunnerSeat.Value;
            else
                e.Cancel(); // fail silently without chat message

            return;
        }

        // if the player is trying to enter a crew seat
        if (info.Class != Class.None && info.IsCrewSeat(e.Seat))
        {
            // prevent entering a crew seat without the required kit
            // if the player has the wrong kit, they will be assigned to the next available non-crewman seat if there is one
            if (playerKitClass != info.Class)
            {
                int? nextAvailablePassengerSeat = FindAvailablePassengerSeat(e.Vehicle, info);
                if (nextAvailablePassengerSeat == null)
                {
                    _chatService.Send(e.Player, _translations.EnterVehicleWrongKit, info.Class);
                    e.Cancel();
                    return;
                }
                else
                    e.Seat = nextAvailablePassengerSeat.Value;
            }
            // prevent entering a crew seat if this vehicle is already being manned by the maximum number of crew.
            else if (MaxAllowedCrewReached(e.Vehicle, info))
            {
                _chatService.Send(e.Player, _translations.VehicleMaxAllowedCrewReached, info.Crew.MaxAllowedCrew ?? -1);
                e.Cancel();
                return;
            }
        }

        // prevent entering a vehicle if the owner is around and not yet inside
        if (ShouldCheckIfOwnerInVehicle(e.Player, e.Vehicle, info, out WarfarePlayer? onlineOwner) && OnlineOwnerIsNotInVehicle(e.Vehicle))
        {
            int? nextAvailablePassengerSeat = FindAvailablePassengerSeat(e.Vehicle, info);
            if (nextAvailablePassengerSeat == null)
            {
                _chatService.Send(e.Player, _translations.EnterVehicleOwnerNotInside, onlineOwner);
                e.Cancel();
                return;
            }
            else
                e.Seat = nextAvailablePassengerSeat.Value;
        }
    }

    public void HandleEvent(VehicleSwapSeatRequested e, IServiceProvider serviceProvider)
    {
        if (_vehicleInfoStore == null)
            return;

        WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(e.Vehicle.asset.GUID);

        if (info == null)
            return;

        Class playerKitClass = e.Player.Component<KitPlayerComponent>().ActiveClass;

        // prevent entering the driver's seat of an emplacement
        if (info.Type.IsEmplacement())
        {
            e.Cancel();
            return;
        }

        // if the player is trying to enter a crew seat
        if (info.Class != Class.None && info.IsCrewSeat(e.NewPassengerIndex))
        {
            // prevent entering crew seat without the required kit
            if (playerKitClass != info.Class)
            {
                _chatService.Send(e.Player, _translations.SwapSeatWrongKit, info.Class);
                e.Cancel();
                return;
            }
            // prevent entering a crew seat (from a non-crew seat) if this vehicle is already being manned by the maximum number of crew.
            else if (
                !info.IsCrewSeat(e.OldPassengerIndex) && 
                MaxAllowedCrewReached(e.Vehicle, info))
            {
                _chatService.Send(e.Player, _translations.VehicleMaxAllowedCrewReached, info.Crew.MaxAllowedCrew ?? -1);
                e.Cancel();
                return;
            }
        }

        // prevent abandoning driver's seat midair
        if (e.OldPassengerIndex == 0 && !CanExitAircraftMidair(e.Vehicle, info))
        {
            _chatService.Send(e.Player, _translations.ExitVehicleAircraftToHigh);
            e.Cancel();
            return;
        }

        // prevent abandoning driver's seat on the battlefield
        if (!CanAbandonDriverSeat(e.Player, info, e.OldPassengerIndex, e.NewPassengerIndex))
        {
            _chatService.Send(e.Player, _translations.SwapSeatCannotAbandonDriver);
            e.Cancel();
            return;
        }
    }
    public void HandleEvent(ExitVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (_vehicleInfoStore == null)
            return;

        WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(e.Vehicle.asset.GUID);

        if (info == null)
            return;

        Class playerKitClass = e.Player.Component<KitPlayerComponent>().ActiveClass;

        // prevent exiting aircraft midair
        if (!CanExitAircraftMidair(e.Vehicle, info))
        {
            _chatService.Send(e.Player, _translations.ExitVehicleAircraftToHigh);
            e.Cancel();
            return;
        }

        // prevent abandoning driver's seat on the battlefield
        if (!CanAbandonDriverSeat(e.Player, info, e.PassengerIndex))
        {
            _chatService.Send(e.Player, _translations.SwapSeatCannotAbandonDriver);
            e.Cancel();
            return;
        }
    }

    private int? FindAvailableNonDriverSeat(InteractableVehicle vehicle)
    {
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (seat == 0)
                continue;

            if (passenger.player != null)
                continue;

            return seat;
        }
        return null;
    }
    private int? FindAvailablePassengerSeat(InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (info.IsCrewSeat(seat))
                continue;

            if (passenger.player != null)
                continue;

            return seat;
        }
        return null;
    }
    private bool IsOwnerOfVehicle(SteamPlayer player, InteractableVehicle vehicle) => player.playerID.steamID == vehicle.lockedOwner;
    private bool OnlineOwnerIsNotInVehicle(InteractableVehicle vehicle)
    {
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player != null && IsOwnerOfVehicle(passenger.player, vehicle))
                return true;
        }
        return false;
    }
    private bool ShouldCheckIfOwnerInVehicle(WarfarePlayer enteringPlayer, InteractableVehicle vehicle, WarfareVehicleInfo info, [NotNullWhen(true)] out WarfarePlayer? onlineOwner)
    {
        onlineOwner = null;

        if (vehicle.lockedOwner == CSteamID.Nil)
            return false;

        if (IsOwnerOfVehicle(enteringPlayer.SteamPlayer, vehicle))
            return false;

        if (info.Type.IsEmplacement())
            return false;

        if (_playerService == null)
            return false;

        onlineOwner = _playerService.GetOnlinePlayerOrNull(vehicle.lockedOwner);

        if (onlineOwner == null)
            return false;

        if (onlineOwner.IsInSquadWith(enteringPlayer))
            return false;

        // if zones are present, this check is only applied when in main. Otherwise, it is always applied.
        if (_zoneStore != null && !_zoneStore.IsInMainBase(enteringPlayer))
            return false;

        // player can enter if the owner is not nearby
        if (!MathUtility.WithinRange(enteringPlayer.Position, vehicle.transform.position, 200f))
            return false;

        return true;
    }
    private bool CanAbandonDriverSeat(WarfarePlayer exitingPlayer, WarfareVehicleInfo info, int currentSeatIndex, int? newSeatIndex = null)
    {
        if (_zoneStore == null)
            return true;

        if (info.Class == Class.None)
            return true;

        if (currentSeatIndex == 0 && 
            info.IsCrewSeat(currentSeatIndex) && 
            info.IsCrewSeat(newSeatIndex ?? -1) && 
            !_zoneStore.IsInMainBase(exitingPlayer))
            return false;

        return true;
    }
    private bool CanExitAircraftMidair(InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        if (info.Type.IsAircraft() && TerrainUtility.GetDistanceToGround(vehicle.transform.position) > MaxAllowedHeightToExitVehicle)
            return false;

        return true;
    }
    private bool MaxAllowedCrewReached(InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        if (info.Crew.MaxAllowedCrew == null)
            return false;

        int crewHeadCount = 0;
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player != null && info.IsCrewSeat(seat))
                crewHeadCount++;
        }
        return crewHeadCount < info.Crew.MaxAllowedCrew;
    }
}
