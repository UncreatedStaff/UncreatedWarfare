using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Projectiles;

namespace Uncreated.Warfare.Events.Models.Projectiles;
public class ProjectileSpawned
{
    public required WarfarePlayer Player { get; init; }
    public required UseableGun Gun { get; init; }
    public required ItemGunAsset Asset { get; init; }
    public required ItemMagazineAsset? Ammo { get; init; }
    public required ItemBarrelAsset? Barrel { get; init; }
    public required Rocket RocketComponent { get; init; }
    public required GameObject Object { get; init; }
    public required InteractableVehicle? Vehicle { get; init; }
    public required WarfareProjectile Projectile { get; init; }

    /// <summary>
    /// Time at which the player fired the weapon (when the first projectile spawned).
    /// </summary>
    public required DateTime FiredTime { get; init; }

    /// <summary>
    /// Time at which this projectile spawned.
    /// </summary>
    public required DateTime LaunchedTime { get; init; }
}