using System;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Events.Tweaks;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles;

public class VehicleSeatRestrictionService :
    IEventListener<EnterVehicleRequested>,
    IEventListener<VehicleSwapSeatRequested>,
    IEventListener<ExitVehicleRequested>
{

    public const float MaxAllowedHeightToExitVehicle = 10;

    private readonly ChatService _chatService;
    private readonly VehicleTweaksTranslations _translations;
    private readonly ZoneStore? _zoneStore;
    private readonly IPlayerService? _playerService;
    private readonly WarfareModule? _module;
    private readonly CooldownManager? _cooldownManager;

    public VehicleSeatRestrictionService(
        ChatService chatService,
        TranslationInjection<VehicleTweaksTranslations> translations,
        ZoneStore? zoneStore = null,
        IPlayerService? playerService = null,
        WarfareModule? module = null,
        CooldownManager? cooldownManager = null)
    {
        _chatService = chatService;
        _translations = translations.Value;
        _zoneStore = zoneStore;
        _playerService = playerService;
        _module = module;
        _cooldownManager = cooldownManager;
    }

    /// <summary>
    /// Runs all the checks needed to enter a vehicle without any feedback.
    /// </summary>
    /// <remarks>Cooldowns are not considered.</remarks>
    public VehicleChangeSeatsResult TryEnterSeat(WarfareVehicle vehicle, WarfarePlayer player, byte seat)
    {
        // is grounded in prep phase
        if (_module != null
            && _module.IsLayoutActive()
            && _module.GetActiveLayout()?.ActivePhase is PreparationPhase pp
            && pp.Teams?.FirstOrDefault(t => t.TeamInfo == player.Team) is { Grounded: true })
        {
            return new VehicleChangeSeatsResult(null, ChangeSeatsResult.PrepPhase);
        }

        ChangeSeatsResult result = ChangeSeatsResult.Success;
        int? preferredSeat = null;

        // ensure that if the player is entering an emplacement, they always enter the next available seat after the driver's seat
        if (vehicle.Info.Type.IsEmplacement() && seat == 0)
        {
            int? nextAvailableGunnerSeat = FindAvailableNonDriverSeat(vehicle.Vehicle);
            // fail silently without chat message if null
            if (!nextAvailableGunnerSeat.HasValue)
                return new VehicleChangeSeatsResult(nextAvailableGunnerSeat, ChangeSeatsResult.Emplacement);

            result = ChangeSeatsResult.Emplacement;
            preferredSeat = nextAvailableGunnerSeat;
        }

        Class playerKitClass = player.Component<KitPlayerComponent>().ActiveClass;

        // if the player is trying to enter a crew seat
        if (vehicle.Info.IsCrewSeat(seat))
        {
            // prevent entering a crew seat without the required kit
            // if the player has the wrong kit, they will be assigned to the next available non-crewman seat if there is one
            if (playerKitClass != vehicle.Info.Class)
            {
                int? nextAvailablePassengerSeat = FindAvailablePassengerSeat(vehicle.Vehicle, vehicle.Info);
                if (!nextAvailablePassengerSeat.HasValue)
                    return new VehicleChangeSeatsResult(nextAvailablePassengerSeat, ChangeSeatsResult.NotInCrew);

                result = ChangeSeatsResult.NotInCrew;
                preferredSeat = nextAvailablePassengerSeat;
            }

            // prevent entering a crew seat if this vehicle is already being manned by the maximum number of crew.
            if (MaxAllowedCrewReached(vehicle.Vehicle, vehicle.Info))
            {
                return new VehicleChangeSeatsResult(null, ChangeSeatsResult.CrewFull);
            }
        }

        // prevent entering a vehicle if the owner is around and not yet inside
        if (ShouldCheckIfOwnerInVehicle(player, vehicle.Vehicle, vehicle.Info) && OnlineOwnerIsNotInVehicle(vehicle.Vehicle))
        {
            int? nextAvailablePassengerSeat = FindAvailablePassengerSeat(vehicle.Vehicle, vehicle.Info);
            if (!nextAvailablePassengerSeat.HasValue)
                return new VehicleChangeSeatsResult(nextAvailablePassengerSeat, ChangeSeatsResult.OwnerNearby);

            result = ChangeSeatsResult.OwnerNearby;
            preferredSeat = nextAvailablePassengerSeat;
        }

        return new VehicleChangeSeatsResult(preferredSeat, result);
    }

    /// <summary>
    /// Runs all the checks needed to swap seats in a vehicle without any feedback.
    /// </summary>
    /// <remarks>Cooldowns are not considered.</remarks>
    public VehicleChangeSeatsResult TrySwapSeat(WarfareVehicle vehicle, WarfarePlayer player, byte toSeat, byte fromSeat = byte.MaxValue)
    {
        if (fromSeat == byte.MaxValue)
            fromSeat = player.UnturnedPlayer.movement.getSeat();

        Class playerKitClass = player.Component<KitPlayerComponent>().ActiveClass;

        // prevent entering the driver's seat of an emplacement
        if (vehicle.Info.Type.IsEmplacement())
        {
            return new VehicleChangeSeatsResult(null, ChangeSeatsResult.Emplacement);
        }

        // if the player is trying to enter a crew seat
        if (vehicle.Info.IsCrewSeat(toSeat))
        {
            // prevent entering crew seat without the required kit
            if (playerKitClass != vehicle.Info.Class)
            {
                return new VehicleChangeSeatsResult(null, ChangeSeatsResult.NotInCrew);
            }
            // prevent entering a crew seat (from a non-crew seat) if this vehicle is already being manned by the maximum number of crew.
            if (!vehicle.Info.IsCrewSeat(fromSeat)
                && MaxAllowedCrewReached(vehicle.Vehicle, vehicle.Info))
            {
                return new VehicleChangeSeatsResult(null, ChangeSeatsResult.CrewFull);
            }
        }

        // prevent abandoning driver's seat midair
        if (fromSeat == 0 && !CanExitAircraftMidair(vehicle.Vehicle, vehicle.Info))
        {
            return new VehicleChangeSeatsResult(null, ChangeSeatsResult.AbandonMidAir);
        }

        // prevent abandoning driver's seat on the battlefield
        if (!CanAbandonDriverSeat(player, vehicle.Info, fromSeat, toSeat))
        {
            return new VehicleChangeSeatsResult(null, ChangeSeatsResult.AbandonDriver);
        }

        return new VehicleChangeSeatsResult(toSeat, ChangeSeatsResult.Success);
    }

    /// <summary>
    /// Runs all the checks needed to exit a seat without any feedback.
    /// </summary>
    /// <remarks>Cooldowns are not considered.</remarks>
    public ChangeSeatsResult TryExitSeat(WarfareVehicle vehicle, WarfarePlayer player, byte fromSeat = byte.MaxValue)
    {
        if (fromSeat == byte.MaxValue)
            fromSeat = player.UnturnedPlayer.movement.getSeat();

        // prevent exiting aircraft midair
        if (!CanExitAircraftMidair(vehicle.Vehicle, vehicle.Info))
        {
            return ChangeSeatsResult.AbandonMidAir;
        }

        // prevent abandoning driver's seat on the battlefield
        if (!CanAbandonDriverSeat(player, vehicle.Info, fromSeat))
        {
            return ChangeSeatsResult.AbandonDriver;
        }

        return ChangeSeatsResult.Success;
    }

    /// <summary>
    /// Check if a player is on cooldown to enter <paramref name="vehicle"/>.
    /// </summary>
    public bool IsOnInteractCooldown(WarfarePlayer player, InteractableVehicle vehicle)
    {
        return _cooldownManager != null && _cooldownManager.HasCooldown(player, KnownCooldowns.VehicleInteract, vehicle);
    }

    /// <summary>
    /// Add a cooldown for a player to enter <paramref name="vehicle"/>.
    /// </summary>
    public void AddInteractCooldown(WarfarePlayer player, InteractableVehicle vehicle)
    {
        _cooldownManager?.StartCooldown(player, KnownCooldowns.VehicleInteract, vehicle);
    }

    void IEventListener<EnterVehicleRequested>.HandleEvent(EnterVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (!e.IgnoreInteractCooldown && IsOnInteractCooldown(e.Player, e.Vehicle.Vehicle))
        {
            e.Cancel();
            return;
        }

        VehicleChangeSeatsResult result = TryEnterSeat(e.Vehicle, e.Player, (byte)e.Seat);

        if (result.PreferredSeat.HasValue)
        {
            e.Seat = result.PreferredSeat.Value;
            return;
        }

        switch (result.Result)
        {
            case ChangeSeatsResult.PrepPhase:
                _chatService.Send(e.Player, _translations.EnterVehicleGrounded,
                    (_module?.GetActiveLayout()?.ActivePhase as PreparationPhase)?.Name?.Translate(e.Player.Locale.LanguageInfo) ?? "Preparation Phase"
                );
                e.Cancel();
                break;

            case ChangeSeatsResult.NotInCrew:
                _chatService.Send(e.Player, _translations.EnterVehicleWrongKit, e.Vehicle.Info.Class);
                e.Cancel();
                break;

            case ChangeSeatsResult.CrewFull:
                _chatService.Send(e.Player, _translations.VehicleMaxAllowedCrewReached, e.Vehicle.Info.Crew.MaxAllowedCrew.GetValueOrDefault());
                e.Cancel();
                break;

            case ChangeSeatsResult.OwnerNearby:
                _chatService.Send(e.Player, _translations.EnterVehicleOwnerNotInside, _playerService?.GetOnlinePlayerOrNull(e.Vehicle.Vehicle.lockedOwner));
                e.Cancel();
                break;
        }
    }

    void IEventListener<VehicleSwapSeatRequested>.HandleEvent(VehicleSwapSeatRequested e, IServiceProvider serviceProvider)
    {
        if (!e.IgnoreInteractCooldown && IsOnInteractCooldown(e.Player, e.Vehicle.Vehicle))
        {
            e.Cancel();
            return;
        }

        VehicleChangeSeatsResult result = TrySwapSeat(e.Vehicle, e.Player, (byte)e.NewPassengerIndex, (byte)e.OldPassengerIndex);

        switch (result.Result)
        {
            case ChangeSeatsResult.NotInCrew:
                _chatService.Send(e.Player, _translations.SwapSeatWrongKit, e.Vehicle.Info.Class);
                e.Cancel();
                break;

            case ChangeSeatsResult.CrewFull:
                _chatService.Send(e.Player, _translations.VehicleMaxAllowedCrewReached, e.Vehicle.Info.Crew.MaxAllowedCrew.GetValueOrDefault());
                e.Cancel();
                break;

            case ChangeSeatsResult.AbandonMidAir:
                _chatService.Send(e.Player, _translations.ExitVehicleAircraftToHigh);
                e.Cancel();
                break;

            case ChangeSeatsResult.AbandonDriver:
                _chatService.Send(e.Player, _translations.SwapSeatCannotAbandonDriver);
                e.Cancel();
                break;
        }
    }

    void IEventListener<ExitVehicleRequested>.HandleEvent(ExitVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (!e.IgnoreInteractCooldown && IsOnInteractCooldown(e.Player, e.Vehicle.Vehicle))
        {
            e.Cancel();
            return;
        }

        ChangeSeatsResult result = TryExitSeat(e.Vehicle, e.Player, e.PassengerIndex);

        switch (result)
        {
            case ChangeSeatsResult.AbandonMidAir:
                _chatService.Send(e.Player, _translations.ExitVehicleAircraftToHigh);
                e.Cancel();
                break;

            case ChangeSeatsResult.AbandonDriver:
                _chatService.Send(e.Player, _translations.SwapSeatCannotAbandonDriver);
                e.Cancel();
                break;
        }
    }

    private static int? FindAvailableNonDriverSeat(InteractableVehicle vehicle)
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

    private static int? FindAvailablePassengerSeat(InteractableVehicle vehicle, WarfareVehicleInfo info)
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

    private static bool IsOwnerOfVehicle(SteamPlayer player, InteractableVehicle vehicle)
    {
        return player.playerID.steamID == vehicle.lockedOwner;
    }

    private static bool OnlineOwnerIsNotInVehicle(InteractableVehicle vehicle)
    {
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player != null && IsOwnerOfVehicle(passenger.player, vehicle))
                return true;
        }
        return false;
    }

    private bool ShouldCheckIfOwnerInVehicle(WarfarePlayer enteringPlayer, InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        if (vehicle.lockedOwner == CSteamID.Nil)
            return false;

        if (IsOwnerOfVehicle(enteringPlayer.SteamPlayer, vehicle))
            return false;

        if (info.Type.IsEmplacement())
            return false;

        if (_playerService == null)
            return false;

        WarfarePlayer? onlineOwner = _playerService.GetOnlinePlayerOrNull(vehicle.lockedOwner);

        if (onlineOwner == null)
            return false;

        if (onlineOwner.IsInSquadWith(enteringPlayer))
            return false;

        // if zones are present, this check is only applied when in main. Otherwise, it is always applied.
        if (_zoneStore != null && !_zoneStore.IsInMainBase(enteringPlayer))
            return false;

        // player can enter if the owner is not nearby
        if (!MathUtility.WithinRange(enteringPlayer.Position, vehicle.transform.position, 75f))
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

    private static bool CanExitAircraftMidair(InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        if (info.Type.IsAircraft() && TerrainUtility.GetDistanceToGround(vehicle.transform.position) > MaxAllowedHeightToExitVehicle)
            return false;

        return true;
    }

    private static bool MaxAllowedCrewReached(InteractableVehicle vehicle, WarfareVehicleInfo info)
    {
        if (!info.Crew.MaxAllowedCrew.HasValue)
            return false;

        int crewHeadCount = 0;
        for (int seat = 0; seat < vehicle.passengers.Length; seat++)
        {
            Passenger passenger = vehicle.passengers[seat];

            if (passenger.player != null && info.IsCrewSeat(seat))
                crewHeadCount++;
        }

        return crewHeadCount >= info.Crew.MaxAllowedCrew.Value;
    }
}

public readonly struct VehicleChangeSeatsResult
{
    public readonly int? PreferredSeat;
    public readonly ChangeSeatsResult Result;

    public VehicleChangeSeatsResult(int? preferredSeat, ChangeSeatsResult result)
    {
        PreferredSeat = preferredSeat;
        Result = result;
    }
}
public enum ChangeSeatsResult
{
    Success,
    PrepPhase,
    Emplacement,
    NotInCrew,
    OwnerNearby,
    CrewFull,
    AbandonMidAir,
    AbandonDriver,
    Cooldown
}