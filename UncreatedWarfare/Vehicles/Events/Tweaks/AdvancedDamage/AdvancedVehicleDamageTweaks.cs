using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
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
    IEventListener<ProjectileExploding>,
    IEventListener<DamageVehicleRequested>,
    IDisposable
{
    private readonly AssetConfiguration _assetConfiguration;
    private readonly ILogger _logger;

#nullable disable
    private IAssetLink<ItemGunAsset>[] _fullDamageOnIndirectHitWeapons;
    private IAssetLink<ItemGunAsset>[] _groundAttackOnlyWeapons;
    private IAssetLink<ItemGunAsset>[] _antiAirOnlyWeapons;
#nullable restore

    public AdvancedVehicleDamageTweaks(AssetConfiguration assetConfiguration, ILogger<AdvancedVehicleDamageTweaks> logger)
    {
        _assetConfiguration = assetConfiguration;
        _logger = logger;
        ReinitConfig(assetConfiguration.UnderlyingConfiguration);
        assetConfiguration.OnChange += ReinitConfig;
    }
    
    private void ReinitConfig(IConfiguration assetConfiguration)
    {
        IConfigurationSection fullDmg = assetConfiguration.GetSection("Projectiles:AdvancedDamage:FullDamageOnIndirectHitWeapons");
        IConfigurationSection gndOnly = assetConfiguration.GetSection("Projectiles:AdvancedDamage:GroundAttackOnlyWeapons");
        IConfigurationSection airOnly = assetConfiguration.GetSection("Projectiles:AdvancedDamage:AntiAirOnlyWeapons");

        if (!fullDmg.Exists())
        {
            _logger.LogWarning("Projectiles:AdvancedDamage:FullDamageOnIndirectHitWeapons not configured.");
        }
        if (!gndOnly.Exists())
        {
            _logger.LogWarning("Projectiles:AdvancedDamage:GroundAttackOnlyWeapons not configured.");
        }
        if (!airOnly.Exists())
        {
            _logger.LogWarning("Projectiles:AdvancedDamage:AntiAirOnlyWeapons not configured.");
        }

        _fullDamageOnIndirectHitWeapons = fullDmg.Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
        _groundAttackOnlyWeapons = gndOnly.Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
        _antiAirOnlyWeapons = airOnly.Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        UseableGun.onBulletHit += UseableGunOnBulletHit;
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        UseableGun.onBulletHit -= UseableGunOnBulletHit;
        return UniTask.CompletedTask;
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(ProjectileExploding e, IServiceProvider serviceProvider)
    {
        if (e.Vehicle == null || !e.Vehicle.TryGetComponent(out WarfareVehicleComponent comp))
            return;

        float dmgMult = AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(e.HitCollider.transform);
        comp.WarfareVehicle.AdvancedDamageApplier.RegisterDirectHitDamageMultiplier(dmgMult);
    }

    private void UseableGunOnBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow)
    {
        if (hit.vehicle != null && hit.vehicle.TryGetComponent(out WarfareVehicleComponent comp))
            comp.WarfareVehicle.AdvancedDamageApplier.RegisterDirectHitDamageMultiplier(AdvancedVehicleDamageApplier.GetComponentDamageMultiplier(hit));
    }

    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        e.Object.gameObject.AddComponent<AdvancedVehicleDamageProjectile>().Init(e.RocketComponent, e.Asset);
    }

    public void HandleEvent(DamageVehicleRequested e, IServiceProvider serviceProvider)
    {
        float finalMultiplier = 1;
        
        Asset? latestInstigatorWeapon = e.Vehicle.DamageTracker.LatestInstigatorWeapon;
        
        AdvancedVehicleDamageApplier.AdvancedDamagePending? directHit = e.Vehicle.AdvancedDamageApplier.ApplyLatestPendingDirectHit();
        
        if (directHit.HasValue) // direct hit
            finalMultiplier = directHit.Value.Multiplier;
        else if (!_fullDamageOnIndirectHitWeapons.ContainsAsset(latestInstigatorWeapon))
            // weapons that do not participate in advanced damage do full damage on an indirect hit
            finalMultiplier = 0.1f;

        bool isAircraft = e.Vehicle.Info.Type.IsAircraft();
        if (isAircraft && _groundAttackOnlyWeapons.ContainsAsset(latestInstigatorWeapon))
            finalMultiplier *= 0.1f;
        else if (!isAircraft && _antiAirOnlyWeapons.ContainsAsset(latestInstigatorWeapon))
            finalMultiplier *= 0.1f;
        
        ushort newDamage = (ushort) Mathf.RoundToInt(e.PendingDamage * finalMultiplier);
        _logger.LogDebug($"Final damage multiplier of {finalMultiplier} (caused by weapon: {latestInstigatorWeapon?.FriendlyName ?? "unknown"}) will be applied to vehicle {e.Vehicle.Vehicle.asset.FriendlyName}. Original damage: {e.PendingDamage} - New damage: {newDamage}");
        e.PendingDamage = newDamage;
    }

    public void Dispose()
    {
        _assetConfiguration.OnChange -= ReinitConfig;
    }
}