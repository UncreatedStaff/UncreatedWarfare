using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.FOBs.Entities;

public class BuildableFobEntity<TInfo> : IBuildableFobEntity, IDisposable where TInfo : IBuildableFobEntityInfo
{
    private readonly string? _iconOverride;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly WorldIconManager? _worldIconManager;

    /// <summary>
    /// Shovelable or supply crate info of this buildable if it's configured.
    /// </summary>
    public TInfo? Info { get; private set; }

    /// <summary>
    /// The floating icon above this buildable.
    /// </summary>
    public WorldIconInfo? Icon { get; private set; }

    /// <summary>
    /// Temporarily hides the icon.
    /// </summary>
    public bool IsIconVisible
    {
        get => Icon is { IsVisible: true };
        set
        {
            if (Icon != null)
                Icon.IsVisible = value;
        }
    }

    public Vector3 Position => Buildable.Position;
    
    public Quaternion Rotation => Buildable.Rotation;

    /// <inheritdoc />
    public virtual IAssetLink<Asset> IdentifyingAsset { get; }

    public IBuildable Buildable { get; }

    public Team Team { get; }

    public virtual bool PreventItemDrops => false;

    public BuildableFobEntity(TInfo? info, IBuildable buildable, Team team, IServiceProvider serviceProvider, string? iconOverride = null)
    {
        _iconOverride = iconOverride;
        Info = info;
        Buildable = buildable;
        IdentifyingAsset = Info?.IdentifyingAsset ?? AssetLink.Create(buildable.Asset);
        Team = team;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _worldIconManager = serviceProvider.GetService<WorldIconManager>();

        // world icon
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (Icon != null)
        {
            Icon.Dispose();
            Icon = null;
        }

        string? icon = _iconOverride;
        Vector3 offset = default;
        if (Info != null)
        {
            offset = Info.IconOffset;
            if (!string.IsNullOrEmpty(Info.Icon))
                icon ??= Info.Icon;
        }

        if (icon == null)
            return;

        IAssetLink<EffectAsset> iconAsset = _assetConfiguration.GetAssetLink<EffectAsset>(icon);

        if (!iconAsset.TryGetAsset(out _) || _worldIconManager == null)
            return;

        Icon = new WorldIconInfo(Buildable, iconAsset, Team)
        {
            Offset = offset,
            RelevanceRegions = Buildable.IsStructure ? StructureManager.STRUCTURE_REGIONS : BarricadeManager.BARRICADE_REGIONS,
            TickSpeed = 10f
        };

        _worldIconManager.CreateIcon(Icon);
    }

    public virtual void UpdateConfiguration(FobConfiguration configuration)
    {
        if (typeof(TInfo) == typeof(ShovelableInfo))
        {
            ShovelableInfo? newInfo = configuration.Shovelables.FirstOrDefault(x => x.Foundation.MatchAsset(IdentifyingAsset));
            if (newInfo != null)
                Info = (TInfo)(object)newInfo;
        }
        else if (typeof(TInfo) == typeof(SupplyCrateInfo))
        {
            SupplyCrateInfo? newInfo = configuration.SupplyCrates.FirstOrDefault(x => x.SupplyItemAsset.MatchAsset(IdentifyingAsset));
            if (newInfo != null)
                Info = (TInfo)(object)newInfo;
        }

        UpdateIcon();
    }

    protected void DestroyIcon()
    {
        Icon?.Dispose();
        Icon = null;
    }

    public virtual void Dispose()
    {
        DestroyIcon();
    }

    public override string ToString()
    {
        return IdentifyingAsset.ToDisplayString();
    }
}