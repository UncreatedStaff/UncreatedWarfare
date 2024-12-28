using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
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
    private readonly AssetConfiguration _assetConfiguration;

    public AdvancedVehicleDamageTweaks(AssetConfiguration assetConfiguration)
    {
        _assetConfiguration = assetConfiguration;
    }

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
        if (_ignoreAdvancedDamage.ContainsAsset(e.Asset))
            return;
        
        e.Object.AddComponent<AdvancedVehicleDamageProjectile>();
    }
    
    private IEnumerable<IAssetLink<ItemGunAsset>> _ignoreAdvancedDamage => _assetConfiguration.GetRequiredSection("Projectiles:GunsThatIgnoreAdvancedDamage")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
}