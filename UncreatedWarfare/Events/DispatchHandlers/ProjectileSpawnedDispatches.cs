using System.Collections.Generic;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Projectiles;

namespace Uncreated.Warfare.Events;

partial class EventDispatcher
{
    private static readonly List<Rocket> WorkingRocketList = new List<Rocket>(8);

    /// <summary>
    /// Invoked when a projectile is spawned.
    /// </summary>
    private void OnProjectileSpawned(UseableGun sender, GameObject projectile)
    {
        ItemGunAsset? gun = sender.equippedGunAsset;
        if (gun == null)
        {
            GetLogger(typeof(UseableGun), nameof(UseableGun.onProjectileSpawned))
                .LogWarning($"Failed to find gun.");
            return;
        }

        ItemMagazineAsset? magazine = null;
        ItemBarrelAsset? barrel = null;
        bool canSolve = false;
        Vector3 origin = default;
        Vector3 direction = default;
        
        if (ProjectileSolvingDataCapturePatch.LastGun == sender)
        {
            magazine = ProjectileSolvingDataCapturePatch.LastMagazineAsset;
            barrel = ProjectileSolvingDataCapturePatch.LastBarrelAsset;
            origin = ProjectileSolvingDataCapturePatch.LastOrigin;
            direction = ProjectileSolvingDataCapturePatch.LastDirection;
            canSolve = true;
        }

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(sender.player);

        /*
         *  Multi-projectile turrets add extra Rocket components to the original projectile, usually with delays
         *  This sets the killer for all those and creates ProjectileComponents for them as well.
         */
        projectile.GetComponentsInChildren(true, WorkingRocketList);
        try
        {
            InteractableVehicle? firerVehicle = warfarePlayer.UnturnedPlayer.movement.getVehicle();
            
            if (firerVehicle == null) firerVehicle = null; // check unity dead

            foreach (Rocket rocket in WorkingRocketList)
            {
                rocket.killer = warfarePlayer.Steam64;
                if (firerVehicle is not null)
                    rocket.ignoreTransform = firerVehicle.transform;

                WarfareProjectile projComponent = rocket.gameObject.AddComponent<WarfareProjectile>();
                projComponent.Initialize(rocket, warfarePlayer, sender, gun, magazine, barrel, origin, direction, canSolve, firerVehicle, this, _projectileSolver);
            }
        }
        finally
        {
            WorkingRocketList.Clear();
        }
    }
}
