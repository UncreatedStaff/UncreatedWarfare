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
        if (vehicle is null || vehicle.isExploded || vehicle.isDrowned || vehicle.isDead)
            return;

        WarfareVehicle? vehicleInfo = vehicleService.GetVehicle(vehicle);
        if (vehicleInfo?.Info == null)
            return;

        byte seat = e.Player.UnturnedPlayer.movement.getSeat();
        WarfareVehicleInfo.CrewInfo crewInfo = vehicleInfo.Info.Crew;

        bool isCrew = crewInfo.Seats.Contains(seat);
        bool isInvincible = isCrew ? crewInfo.Invincible : crewInfo.PassengersInvincible;
        if (seat != 0 && crewInfo.Except != null && crewInfo.Except.Contains(seat))
            isInvincible = !isInvincible;

        if (isInvincible && crewInfo.InvincibleForwardOnly)
        {
            // the dot product of two normalized v3's will be negative if one is facing the opposite direction from another,
            // but since 'direction' is the direction the bullet is traveling not the hit normal, it should be positive
            Vector3 dir = e.Parameters.direction;
            Vector3 vehicleFwd = vehicle.transform.forward;

            float dotPdt = Vector3.Dot(dir, vehicleFwd);

            if (dotPdt > 0)
            {
                isInvincible = false;
            }
        }
        
        if (isInvincible)
        {
            e.Cancel();
        }
    }
}