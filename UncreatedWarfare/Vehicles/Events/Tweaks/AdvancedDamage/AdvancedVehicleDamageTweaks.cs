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
    IEventListener<VehiclePreDamaged>,
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
        if (e.HitVehicle == null || !e.HitVehicle.TryGetComponent(out WarfareVehicleComponent comp))
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

    public void HandleEvent(VehiclePreDamaged e, IServiceProvider serviceProvider)
    {
        float finalMultiplier = 1;
        
        Asset? latestInstigatorWeapon = e.Vehicle.DamageTracker.LatestInstigatorWeapon;
        
        AdvancedVehicleDamageApplier.AdvancedDamagePending? directHit = e.Vehicle.AdvancedDamageApplier.ApplyLatestPendingDirectHit();
        
        bool misusedDirectHitWeapon = false;
        
        if (directHit.HasValue) // direct hit
            finalMultiplier = directHit.Value.Multiplier;
        else if (!_fullDamageOnIndirectHitWeapons.ContainsAsset(latestInstigatorWeapon)) // indirect hit
            // only specified weapons do full damage on an indirect hit 
            misusedDirectHitWeapon = true;

        bool misusedGroundAttackWeapon = e.Vehicle.Info.Type.IsAircraft() &&
                                       _groundAttackOnlyWeapons.ContainsAsset(latestInstigatorWeapon);
        bool misusedAntiAirWeapon = !e.Vehicle.Info.Type.IsAircraft() &&
                                    _antiAirOnlyWeapons.ContainsAsset(latestInstigatorWeapon);
        
        if (misusedGroundAttackWeapon || misusedAntiAirWeapon || misusedDirectHitWeapon)
            finalMultiplier = 0.1f;
        
        ushort newDamage = (ushort) Mathf.RoundToInt(e.PendingDamage * finalMultiplier);
        //_logger.LogDebug(
        //    $"Final damage multiplier of {finalMultiplier} " +
        //    $"(caused by weapon: {latestInstigatorWeapon?.FriendlyName ?? "unknown"}) " +
        //    $"will be applied to vehicle {e.Vehicle.Vehicle.asset.FriendlyName}.\n" +
        //    $"Original damage: {e.PendingDamage} - " +
        //    $"New damage: {newDamage} - " +
        //    $"Direct Hit: {directHit.HasValue} - " +
        //    $"Direct Hit Multiplier: {directHit?.Multiplier} - " +
        //    $"Misused Direct Hit Weapon: {misusedDirectHitWeapon} - " +
        //    $"Misused Ground Attack: {misusedGroundAttackWeapon} - " +
        //    $"Misused Anti Air: {misusedAntiAirWeapon}");
        
        e.PendingDamage = newDamage;
    }

    public void Dispose()
    {
        _assetConfiguration.OnChange -= ReinitConfig;
    }
}