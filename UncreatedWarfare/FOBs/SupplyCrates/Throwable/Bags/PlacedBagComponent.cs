using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Bags;

public abstract class PlacedBagComponent : MonoBehaviour, IManualOnDestroy  
{
    protected IBuildable _Buildable = null!;
    public CSteamID Owner { get; private set; }
    public WorldIconInfo? Icon { get; private set; }
    public Team Team { get; private set; } = null!;

    public abstract IAssetLink<EffectAsset> GetIconEffect(AssetConfiguration assetConfig);
    
    protected void InitBase(WarfarePlayer warfarePlayer, IBuildable buildable, Team team, IServiceProvider serviceProvider)
    {
        _Buildable = buildable;
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
        
        IAssetLink<EffectAsset> iconEffect = GetIconEffect(assetConfig);
        if (!iconEffect.TryGetAsset(out _))
            return;

        Icon = new WorldIconInfo(_Buildable, iconEffect, Team)
        {
            Offset = new Vector3(0f, 2f, 0f),
            RelevanceRegions = _Buildable.IsStructure ? StructureManager.STRUCTURE_REGIONS : BarricadeManager.BARRICADE_REGIONS,
            TickSpeed = 10f
        };

        worldIconManager.CreateIcon(Icon);
    }

    protected void DestroyNextFrame()
    {
        UniTask.Create(async () =>
        {
            await UniTask.NextFrame(); // destroy the bag next frame
            Destroy(this);
            _Buildable.Destroy();
        });
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