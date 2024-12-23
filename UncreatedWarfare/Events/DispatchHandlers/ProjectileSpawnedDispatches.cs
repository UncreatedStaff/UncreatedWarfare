using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;
partial class EventDispatcher
{
    /// <summary>
    /// Invoked when a projectile is spawned.
    /// </summary>
    private void OnProjectileSpawned(UseableGun sender, GameObject projectile)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(sender.player);
        Rocket rocket = projectile.GetComponent<Rocket>();

        if (warfarePlayer.UnturnedPlayer.movement.getVehicle())
            rocket.ignoreTransform = warfarePlayer.UnturnedPlayer.movement.getVehicle().transform;

        ProjectileSpawned args = new ProjectileSpawned
        {
            Player = warfarePlayer,
            Asset = sender.equippedGunAsset,
            Object = projectile,
            RocketComponent = rocket
        };

        _ = DispatchEventAsync(args, _unloadToken);
    }
}
