using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Weapons;

namespace Uncreated.Warfare.Vehicles.Events.Tweaks;

internal sealed class GuidedMissileLaunchTweaks :
    IEventListener<ProjectileSpawned>,
    IEventListener<VehicleSpawned>,
    IDisposable
{
    private readonly ILogger _logger;
    private readonly AssetConfiguration _assetConfiguration;

    private IAssetLink<ItemGunAsset>[] _guidedMissiles = null!;
    private IAssetLink<ItemGunAsset>[] _groundAAMissiles = null!;
    private IAssetLink<ItemGunAsset>[] _airAAMissiles = null!;
    private IAssetLink<ItemGunAsset>[] _laserGuidedMissiles = null!;

    public GuidedMissileLaunchTweaks(IServiceProvider serviceProvider, ILogger<GuidedMissileLaunchTweaks> logger)
    {
        _logger = logger;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        OnAssetConfigUpdated(_assetConfiguration);
    }

    private void OnAssetConfigUpdated(IConfiguration obj)
    {
        _guidedMissiles = _assetConfiguration.GetRequiredSection("Projectiles:GuidedMissiles")?
            .Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
        _groundAAMissiles = _assetConfiguration.GetRequiredSection("Projectiles:GroundAAMissiles")?
            .Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
        _airAAMissiles = _assetConfiguration.GetRequiredSection("Projectiles:AirAAMissiles")?
            .Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
        _laserGuidedMissiles = _assetConfiguration.GetRequiredSection("Projectiles:LaserGuidedMissiles")?
            .Get<IAssetLink<ItemGunAsset>[]>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    }
    
    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        if (e.Player == null)
            return;

        if (_guidedMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<GuidedMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 90, 0.33f, 800);
        }
        else if (_groundAAMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<HeatSeekingMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 190, 8f, 2);
        }
        else if (_airAAMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<HeatSeekingMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 190, 6f, 0.5f);
        }
        else if (_laserGuidedMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<LaserGuidedMissileComponent>().Initialize(e.Object, e.Player, serviceProvider, 150, 1.15f, 150, 15, 0.6f);
        }
    }

    public void HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
    {
        foreach (Passenger? passenger in e.Vehicle.Vehicle.turrets)
        {
            if (Array.Exists(_groundAAMissiles, a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset> lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:Projectiles:GroundAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(700, 1500, lockOnEffect, 0.7f, 14.6f, _logger);
            }
            else if (Array.Exists(_airAAMissiles, a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset> lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:Projectiles:AirAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(600, lockOnEffect, 1, 11, _logger);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _assetConfiguration.OnChange -= OnAssetConfigUpdated;
    }
}