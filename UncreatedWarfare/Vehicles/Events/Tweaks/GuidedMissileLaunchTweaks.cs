using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Weapons;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks;

internal sealed class GuidedMissileLaunchTweaks :
    IEventListener<ProjectileSpawned>,
    IEventListener<VehicleSpawned>
{
    private readonly ILogger _logger;
    private readonly AssetConfiguration _assetConfiguration;

    public GuidedMissileLaunchTweaks(IServiceProvider serviceProvider, ILogger<GuidedMissileLaunchTweaks> logger)
    {
        _logger = logger;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
    }
    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        if (e.Player == null)
            return;

        _logger.LogDebug($"Projectile asset: {e.Asset} ground AA missiles: {string.Join(", ", GroundAAMissiles)}");
        if (GuidedMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<GuidedMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 90, 0.33f, 800);
        }
        else if (GroundAAMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            _logger.LogDebug("init ground AA missile");
            e.Object.GetOrAddComponent<HeatSeekingMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 190, 8f, 2);
        }
        else if (AirAAMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<HeatSeekingMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 190, 6f, 0.5f);
        }
        else if (LaserGuidedMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<LaserGuidedMissileComponent>().Initialize(e.Object, e.Player, serviceProvider, 150, 1.15f, 150, 15, 0.6f);
        }
    }

    public void HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        foreach (var passenger in e.Vehicle.Vehicle.turrets)
        {
            if (GroundAAMissiles.Any(a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset>? lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:GuidanceLock:GroundAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(700, 1500, lockOnEffect, 0.7f, 14.6f, _logger);
            }
            else if (AirAAMissiles.Any(a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset> lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:GuidanceLock:AirAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(600, lockOnEffect, 1, 11, _logger);
            }
        }
    }

    private IEnumerable<IAssetLink<ItemGunAsset>> GuidedMissiles => _assetConfiguration.GetRequiredSection("Projectiles:GuidedMissiles")?
            .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> GroundAAMissiles => _assetConfiguration.GetRequiredSection("Projectiles:GroundAAMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> AirAAMissiles => _assetConfiguration.GetRequiredSection("Projectiles:AirAAMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> LaserGuidedMissiles => _assetConfiguration.GetRequiredSection("Projectiles:LaserGuidedMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
}
