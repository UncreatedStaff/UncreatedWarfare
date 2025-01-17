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
    private readonly ILogger _logger;

    public AdvancedVehicleDamageTweaks(AssetConfiguration assetConfiguration, ILogger<AdvancedVehicleDamageTweaks> logger)
    {
        _assetConfiguration = assetConfiguration;
        _logger = logger;
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
            comp.WarfareVehicle.AdvancedDamageApplier.RegisterDirectHitDamageMultiplier(AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(hit));
    }
    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        e.Object.AddComponent<AdvancedVehicleDamageProjectile>().Init(e.RocketComponent, e.Asset);
    }
    public void HandleEvent(DamageVehicleRequested e, IServiceProvider serviceProvider)
    {
        float finalMultiplier = 1;
        
        Asset? latestInstigatorWeapon = e.Vehicle.DamageTracker.LatestInstigatorWeapon;
        
        AdvancedVehicleDamageApplier.AdvancedDamagePending? directHit = e.Vehicle.AdvancedDamageApplier.ApplyLatestPendingDirectHit();
        
        if (directHit.HasValue) // direct hit
            finalMultiplier = directHit.Value.Multiplier;
        else if (!FullDamageOnIndirectHitWeapons.ContainsAsset(latestInstigatorWeapon))
            // weapons that do not participate in advanced damage do full damage on an indirect hit
            finalMultiplier = 0.1f;

        bool isAircraft = e.Vehicle.Info.Type.IsAircraft();
        if (isAircraft && GroundAttackOnlyWeapons.ContainsAsset(latestInstigatorWeapon))
            finalMultiplier *= 0.1f;
        else if (!isAircraft && AntiAirOnlyWeapons.ContainsAsset(latestInstigatorWeapon))
            finalMultiplier *= 0.1f;
        
        ushort newDamage = (ushort) Mathf.RoundToInt(e.PendingDamage * finalMultiplier);
        _logger.LogDebug($"Final damage multiplier of {finalMultiplier} (caused by weapon: {latestInstigatorWeapon?.FriendlyName ?? "unknown"}) will be applied to vehicle {e.Vehicle.Vehicle.asset.FriendlyName}. Original damage: {e.PendingDamage} - New damage: {newDamage}");
        e.PendingDamage = newDamage;
    }
    
    private IEnumerable<IAssetLink<ItemGunAsset>> FullDamageOnIndirectHitWeapons => _assetConfiguration.GetRequiredSection("Projectiles:AdvancedDamage:FullDamageOnIndirectHitWeapons")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    
    private IEnumerable<IAssetLink<ItemGunAsset>> GroundAttackOnlyWeapons => _assetConfiguration.GetRequiredSection("Projectiles:AdvancedDamage:GroundAttackOnlyWeapons")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    
    private IEnumerable<IAssetLink<ItemGunAsset>> AntiAirOnlyWeapons => _assetConfiguration.GetRequiredSection("Projectiles:AdvancedDamage:AntiAirOnlyWeapons")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
}