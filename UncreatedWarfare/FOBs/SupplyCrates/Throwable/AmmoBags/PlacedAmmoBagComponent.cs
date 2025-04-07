using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.AmmoBags;

public class PlacedAmmoBagComponent : MonoBehaviour, IAmmoStorage, IManualOnDestroy
{
    private IBuildable _buildable = null!;
    public CSteamID Owner { get; private set; }
    public float AmmoCount { get; private set; }
    public WorldIconInfo? Icon { get; private set; }
    public Team Team { get; private set; } = null!;
    public void Init(WarfarePlayer warfarePlayer, IBuildable buildable, float startingAmmo, Team team, IServiceProvider serviceProvider)
    {
        AmmoCount = startingAmmo;
        _buildable = buildable;
        Owner = warfarePlayer.Steam64;
        Team = team;

        WorldIconManager? worldIconManager = serviceProvider.GetService<WorldIconManager>();
        AssetConfiguration? assetConfig = serviceProvider.GetService<AssetConfiguration>();

        if (Icon != null)
        {
            Icon.Dispose();
            Icon = null;
        }

        if (worldIconManager == null || assetConfig == null)
            return;

        IAssetLink<EffectAsset> assetLink = assetConfig.GetAssetLink<EffectAsset>("Effects:Fobs:Ammo");
        if (!assetLink.TryGetAsset(out _))
            return;

        Icon = new WorldIconInfo(_buildable, assetLink, Team)
        {
            Offset = new Vector3(0f, 2f, 0f),
            RelevanceRegions = _buildable.IsStructure ? StructureManager.STRUCTURE_REGIONS : BarricadeManager.BARRICADE_REGIONS,
            TickSpeed = 10f
        };

        worldIconManager.CreateIcon(Icon);
    }
    public void SubtractAmmo(float ammoCount)
    {
        AmmoCount -= ammoCount;
        
        if (AmmoCount <= 0)
        {
            AmmoCount = 0;
            UniTask.Create(async () =>
            {
                await UniTask.NextFrame(); // destroy the ammo bag next frame
                Destroy(this);
                _buildable.Destroy();
            });
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return AssetLink.ToDisplayString(_buildable.Asset) + $" ({AmmoCount:F2} ammo)";
    }

    public void ManualOnDestroy()
    {
        Destroy(this);
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        Icon?.Dispose();
        Icon = null;
    }
}