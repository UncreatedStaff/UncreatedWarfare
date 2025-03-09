using System;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Tweaks;

/// <summary>
/// Disables damaging players that are supposed to be undamageable (like the driver of an enclosed vehicle).
/// </summary>
internal sealed class InvinciblePassengersTweak(VehicleService vehicleService) : IEventListener<DamagePlayerRequested>
{
    void IEventListener<DamagePlayerRequested>.HandleEvent(DamagePlayerRequested e, IServiceProvider serviceProvider)
    {
        if (e.Parameters.cause != EDeathCause.GUN)
            return;

        InteractableVehicle? vehicle = e.Player.UnturnedPlayer.movement.getVehicle();
        if (vehicle is null)
            return;

        WarfareVehicle? vehicleInfo = vehicleService.GetVehicle(vehicle);
        if (vehicleInfo?.Info == null)
            return;

        byte seat = e.Player.UnturnedPlayer.movement.getSeat();
        WarfareVehicleInfo.CrewInfo crewInfo = vehicleInfo.Info.Crew;

        bool isCrew = crewInfo.Seats.Contains(seat);
        if (isCrew ? crewInfo.Invincible : crewInfo.PassengersInvincible)
        {
            e.Cancel();
        }
    }
}