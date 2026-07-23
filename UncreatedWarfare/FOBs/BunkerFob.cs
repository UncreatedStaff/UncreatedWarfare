using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.DamageTracking;

namespace Uncreated.Warfare.FOBs;

public class BunkerFob : ResourceFob, IFobStrategyMapTackHandler, IDamageableFob
{
    private ShovelableBuildable? _shovelable;

    public bool IsBuilt { get; private set; }
    public bool HasBeenRebuilt { get; private set; }
    public DamageTracker DamageTracker { get; }
    public CSteamID Creator => Buildable.Owner;

    bool IDamageableFob.CanRecordDamage => IsBuilt;

    public ShovelableBuildable? Shovelable
    {
        get => _shovelable;
        internal set
        {
            ShovelableBuildable? old = Interlocked.Exchange(ref _shovelable, value);
            if (ReferenceEquals(old, value))
                return;

            if (old != null)
                old.OnProgressUpdated -= OnShovelableProgressUpdated;
            if (value != null)
                value.OnProgressUpdated += OnShovelableProgressUpdated;

            InvokeHealthUpdated();
        }
    }

    public BunkerFob(IServiceProvider serviceProvider, string name, IBuildable buildable) : base(serviceProvider, name, buildable)
    {
        IsBuilt = false;
        HasBeenRebuilt = false;
        DamageTracker = new DamageTracker(name);

        // show shovelable icon instead
        Icon?.IsVisible = false;
    }

    private void OnShovelableProgressUpdated()
    {
        InvokeHealthUpdated();
    }

    public override Color32 GetColor(Team viewer)
    {
        return !IsBuilt ? Color.gray : base.GetColor(viewer);
    }

    public void MarkBuilt(IBuildable newBuildable)
    {
        IsBuilt = true;
        HasBeenRebuilt = true;
        Buildable = newBuildable;
        UpdateIcon();
        InvokeHealthUpdated();
        InvokeAttributesUpdated();
    }
    public void MarkUnbuilt(IBuildable newBuildable)
    {
        IsBuilt = false;
        Buildable = newBuildable;
        UpdateIcon();
        InvokeHealthUpdated();
        InvokeAttributesUpdated();
    }

    #region Deployment

    /// <inheritdoc />
    protected override void OnDeployTo(WarfarePlayer player, in DeploySettings settings)
    {
        player.Component<FobDeploymentInvulnerabilityCooldown>().StartCooldown();
        base.OnDeployTo(player, in settings);
    }

    public override bool CheckDeployableTo(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (!base.CheckDeployableTo(player, chatService, translations, in settings))
            return false;

        if (!IsBuilt)
        {
            chatService.Send(player, HasBeenRebuilt ? translations.DeployDestroyed : translations.DeployNotBuilt, this);
            return false;
        }

        return true;
    }

    public override bool CheckDeployableFrom(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        if (!base.CheckDeployableFrom(player, chatService, translations, in settings, deployingTo))
            return false;

        if (!IsBuilt)
        {
            chatService.Send(player, HasBeenRebuilt ? translations.DeployDestroyed : translations.DeployNotBuilt, this);
            return false;
        }

        return true;
    }

    public override bool CheckDeployableFromTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        if (!base.CheckDeployableFromTick(player, chatService, translations, in settings, deployingTo))
            return false;

        if (!IsBuilt)
        {
            chatService.Send(player, translations.DeployDestroyedTick, this);
            return false;
        }

        return true;
    }

    public override bool CheckDeployableToTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (!base.CheckDeployableToTick(player, chatService, translations, in settings))
            return false;

        if (!IsBuilt)
        {
            chatService.Send(player, translations.DeployDestroyedTick, this);
            return false;
        }

        return true;
    }
    public override TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.FromSeconds(FobManager.Configuration.GetValue("FobDeployDelay", 10));
    }

    #endregion



    #region Map Tacks

    protected virtual MapTack? CreateMapTack(StrategyMapManager manager, StrategyMap map, AssetConfiguration assetConfig)
    {
        if (!Buildable.IsAlive || !Team.IsFriendly(map.MapTable.Group))
            return null;

        IAssetLink<ItemBarricadeAsset> barricade;
        if (!IsBuilt)
            barricade = assetConfig.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobUnbuilt");
        else if (IsProxied)
            barricade = assetConfig.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:FobProxied");
        else
            barricade = assetConfig.GetAssetLink<ItemBarricadeAsset>("Buildables:MapTacks:Fob");

        return new DeployableMapTack(manager, map, barricade, this);
    }

    /// <inheritdoc />
    MapTack? IFobStrategyMapTackHandler.CreateMapTack(StrategyMapManager manager, StrategyMap map, AssetConfiguration assetConfig)
    {
        return CreateMapTack(manager, map, assetConfig);
    }

    public override double? GetHealth()
    {
        if (IsBuilt)
        {
            return base.GetHealth();
        }

        if (_shovelable == null)
            return null;
        
        return 1d - (double)_shovelable.HitsRemaining / _shovelable.Info.RequiredShovelHits;
    }

    /// <inheritdoc />
    public override MapTackAttributes GetAttributes()
    {
        if (!IsBuilt && !HasBeenRebuilt)
        {
            return MapTackAttributes.NotBuilt;
        }

        MapTackAttributes attributes = base.GetAttributes();
        
        if (!IsBuilt)
            attributes |= MapTackAttributes.Destroyed;

        return attributes;
    }

    #endregion

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        Shovelable = null;
    }
}