using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Water;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Zones;
using Microsoft.Extensions.Configuration;
using Uncreated.Warfare.Util;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Util.Containers;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Events.Models.Vehicles;
using SDG.Unturned;

namespace Uncreated.Warfare.Vehicles.Events.Vehicles;
internal class GuidedMissileLaunchTweaks :
    IEventListener<ProjectileSpawned>,
    IEventListener<VehicleSpawned>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly AssetConfiguration _assetConfiguration;

    public GuidedMissileLaunchTweaks(IServiceProvider serviceProvider, ILogger<GuidedMissileLaunchTweaks> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
    }
    public void HandleEvent(ProjectileSpawned e, IServiceProvider serviceProvider)
    {
        if (e.Player == null)
            return;

        _logger.LogDebug($"Projectile asset: {e.Asset} ground AA missiles: {string.Join(", ", _groundAAMissiles)}");
        if (_guidedMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            e.Object.GetOrAddComponent<GuidedMissileComponent>().Initialize(e.Object, e.Player.UnturnedPlayer, serviceProvider, 90, 0.33f, 800);
        }
        else if (_groundAAMissiles.Any(a => a.MatchAsset(e.Asset)))
        {
            _logger.LogDebug($"init ground AA missile");
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
        foreach (var passenger in e.Vehicle.Vehicle.turrets)
        {
            if (_groundAAMissiles.Any(a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset>? lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:GuidanceLock:GroundAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(700, 1500, lockOnEffect, 0.7f, 14.6f, _logger);
            }
            else if (_airAAMissiles.Any(a => a.Id == passenger.turret.itemID))
            {
                IAssetLink<EffectAsset> lockOnEffect = _assetConfiguration.GetAssetLink<EffectAsset>("Effects:GuidanceLock:AirAALock");
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(600, lockOnEffect, 1, 11, _logger);
            }
        }
    }

    private IEnumerable<IAssetLink<ItemGunAsset>> _guidedMissiles => _assetConfiguration.GetRequiredSection("Projectiles:GuidedMissiles")?
            .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> _groundAAMissiles => _assetConfiguration.GetRequiredSection("Projectiles:GroundAAMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> _airAAMissiles => _assetConfiguration.GetRequiredSection("Projectiles:AirAAMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
    private IEnumerable<IAssetLink<ItemGunAsset>> _laserGuidedMissiles => _assetConfiguration.GetRequiredSection("Projectiles:LaserGuidedMissiles")?
        .Get<IEnumerable<IAssetLink<ItemGunAsset>>>() ?? Array.Empty<IAssetLink<ItemGunAsset>>();
}
