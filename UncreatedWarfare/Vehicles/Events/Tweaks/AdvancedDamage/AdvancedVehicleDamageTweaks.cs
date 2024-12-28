using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks.AdvancedDamage;

public class AdvancedVehicleDamageTweaks : 
    ILayoutHostedService,
    IEventListener<ProjectileSpawned>,
    IEventListener<DamageVehicleRequested>
{
    public UniTask StartAsync(CancellationToken token)
    {
        UseableGun.onBulletHit += UseableGunOnBulletHit;
        return UniTask.CompletedTask;
    }

    public UniTask StopAsync(CancellationToken token)
    {
        UseableGun.onBulletHit -= UseableGunOnBulletHit;
        return UniTask.CompletedTask;
    }
    
    private void UseableGunOnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow)
    {
        if (hit.vehicle != null && hit.vehicle.TryGetComponent(out WarfareVehicleComponent comp))
            comp.WarfareVehicle.AdvancedDamageApplier.RegisterPendingDamageForNextEvent(AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(hit));
    }

    public void HandleEvent(DamageVehicleRequested e, IServiceProvider serviceProvider)
    {
        float multiplier = e.Vehicle.AdvancedDamageApplier.ApplyLatestRelevantDamageMultiplier();
        ushort newDamage = (ushort) Mathf.RoundToInt(e.PendingDamage * multiplier);
        e.PendingDamage = newDamage;
    }

    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        e.Object.AddComponent<AdvancedVehicleDamageProjectile>();
    }
}