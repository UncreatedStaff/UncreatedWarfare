using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Models.Vehicles;

[EventModel(EventSynchronizationContext.Pure)]
public class VehicleExploded : IActionLoggableEvent
{
    /// <summary>
    /// The actual vehicle that was destroyed.
    /// </summary>
    public required WarfareVehicle Vehicle { get; init; }
    
    /// <summary>
    /// The owner (requester usually) of this vehicle.
    /// </summary>
    public required WarfarePlayer? Owner { get; init; }

    /// <summary>
    /// The owner (requester usually) of this vehicle's Steam ID.
    /// </summary>
    public required CSteamID OwnerId { get; init; }

    /// <summary>
    /// The player who most likely caused the explosion if they're online.
    /// </summary>
    public required WarfarePlayer? Instigator { get; init; }

    /// <summary>
    /// The player who most likely caused the explosion's Steam ID.
    /// </summary>
    public required CSteamID InstigatorId { get; init; }

    /// <summary>
    /// The team of the player who most likely caused the explosion when it happened. May be <see cref="Team.NoTeam"/> if the instigator is known but not in a team.
    /// </summary>
    public required Team? InstigatorTeam { get; init; }

    /// <summary>
    /// The vehicle the instigator was in when this vehicle was destroyed.
    /// </summary>
    public required WarfareVehicle? InstigatorVehicle { get; init; }

    /// <summary>
    /// The last player to drive this vehicle if they're online.
    /// </summary>
    public required WarfarePlayer? LastDriver { get; init; }

    /// <summary>
    /// The last player to drive this vehicle's Steam ID.
    /// </summary>
    public required CSteamID LastDriverId { get; init; }

    /// <summary>
    /// The team this vehicle belongs to.
    /// </summary>
    public required Team Team { get; init; }
    
    /// <summary>
    /// The origin of the damage done to this vehicle.
    /// </summary>
    public required EDamageOrigin DamageOrigin { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.VehicleExploded,
            $"{AssetLink.ToDisplayString(Vehicle.Asset)} owned by {Vehicle.Vehicle.lockedOwner} ({Team}), Damage origin: {EnumUtility.GetName(DamageOrigin)}, Last driver: {LastDriverId}",
            InstigatorId
        );
    }
}
