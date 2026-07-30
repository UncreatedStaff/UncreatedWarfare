using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags.AmmoBags;

public class PlacedAmmoBagComponent : PlacedBagComponent, IAmmoStorage
{
    public override IAssetLink<EffectAsset> GetIconEffect(AssetConfiguration assetConfig) => assetConfig.GetAssetLink<EffectAsset>("Effects:Fobs:Ammo");
    public float AmmoCount { get; private set; }

    public void Init(WarfarePlayer warfarePlayer, IBuildable buildable, Team team, IServiceProvider serviceProvider,
        float startingAmmo)
    {
        InitBase(warfarePlayer, buildable, team, serviceProvider);
        AmmoCount = startingAmmo;
    }
    
    public void SubtractAmmo(float ammoCount)
    {
        AmmoCount -= ammoCount;
        
        if (AmmoCount <= 0)
        {
            AmmoCount = 0;
            DestroyNextFrame();
        }
    }
    /// <inheritdoc />
    public override string ToString()
    {
        return AssetLink.ToDisplayString(_Buildable.Asset) + $" ({AmmoCount:F2} ammo)";
    }
}