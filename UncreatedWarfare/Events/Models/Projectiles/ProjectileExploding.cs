using System;
using Uncreated.Warfare.Projectiles;

namespace Uncreated.Warfare.Events.Models.Projectiles;

public class ProjectileExploding : CancellablePlayerEvent
{
    private readonly ExplosionParameters _parameters;

    public required UseableGun Gun { get; init; }
    public required ItemGunAsset Asset { get; init; }
    public required ItemMagazineAsset? Ammo { get; init; }
    public required ItemBarrelAsset? Barrel { get; init; }
    public required Rocket RocketComponent { get; init; }
    public required GameObject Object { get; init; }

    public required DateTime ImpactTime { get; init; }
    public required DateTime? PredictedImpactTime { get; init; }

    public required Vector3 HitPosition { get; init; }
    public required Vector3? PredictedHitPosition { get; init; }
    public required Transform HitObject { get; init; }
    public required Collider HitCollider { get; init; }

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

    /// <summary>
    /// Parameters that will be used to create the explosion.
    /// </summary>
    public ref readonly ExplosionParameters Explosion => ref _parameters;

    public ProjectileExploding(ExplosionParameters parameters)
    {
        _parameters = parameters;
    }
}
