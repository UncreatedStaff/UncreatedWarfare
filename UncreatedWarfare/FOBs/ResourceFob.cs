using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Icons;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Util.Timing.Collectors;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Base class for standard FOBs, caches, and any other FOBs that support items.
/// </summary>
public class ResourceFob : IBuildableFob, IResourceFob, IDisposable
{
    // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
    private readonly IPlayerService _playerService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly WorldIconManager? _worldIconManager;
    private readonly TipService? _tipService;
    protected readonly FobManager FobManager;
    private readonly ILoopTicker _loopTicker;
    private readonly Func<WarfarePlayer, float> _getProxyScore;
    // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

    public IBuildable Buildable { get; protected set; }

    /// <inheritdoc />
    public float BuildCount { get; private set; }
    /// <inheritdoc />
    public float AmmoCount { get; private set; }

    /// <inheritdoc />
    public string Name { get; private set; }

    /// <inheritdoc />
    public Team Team { get; private set; }

    /// <inheritdoc />
    public Vector3 Position
    {
        get => Buildable.Position;
        set
        {
            Buildable.Position = value;
            if (Icon != null)
                _worldIconManager?.UpdateIcon(Icon.Effect);
        }
    }

    /// <inheritdoc />
    public Quaternion Rotation
    {
        get => Buildable.Rotation;
        set
        {
            Buildable.Rotation = value;
            if (Icon != null)
                _worldIconManager?.UpdateIcon(Icon.Effect);
        }
    }

    public float EffectiveRadius => 70f;
    public bool IsProxied { get; private set; }
    public ISphereProximity FriendlyProximity { get; }
    public ProximityCollector<WarfarePlayer> NearbyFriendlies { get; }
    public ProximityCollector<WarfarePlayer> NearbyEnemies { get; }
    public IEnumerable<IFobEntity> EnumerateEntities() => FobManager.Entities.Where(e => MathUtility.WithinRange(Position, e.Position, EffectiveRadius));

    public WorldIconInfo? Icon { get; private set; }

    public ResourceFob(IServiceProvider serviceProvider, string name, IBuildable buildable)
    {
        Name = name;
        Buildable = buildable;
        Team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        FobManager = serviceProvider.GetRequiredService<FobManager>();
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _worldIconManager = serviceProvider.GetService<WorldIconManager>();
        _tipService = serviceProvider.GetService<TipService>();

        FriendlyProximity = new SphereProximity(Position, EffectiveRadius);

        _loopTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromSeconds(0.25f), true, true);

        NearbyFriendlies = new ProximityCollector<WarfarePlayer>(
            new ProximityCollector<WarfarePlayer>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _playerService.OnlinePlayers.Where(p => p.Team.IsFriendly(Team)),
                PositionFunction = p => p.Position
            }
        );
        NearbyEnemies = new ProximityCollector<WarfarePlayer>(
            new ProximityCollector<WarfarePlayer>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _playerService.OnlinePlayers.Where(p => p.Team.IsOpponent(Team)),
                PositionFunction = p => p.Position
            }
        );

        _getProxyScore = GetProxyScore;
        _loopTicker.OnTick += (ticker, timeSinceStart, deltaTime) =>
        {
            bool newProxyState = NearbyEnemies.Collection.Sum(_getProxyScore) >= 1;
            if (newProxyState != IsProxied)
            {
                IsProxied = newProxyState;
                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobProxyChanged { Fob = this, IsProxied = newProxyState });
            }
        };

        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(Position, Team.GroupId, FobManager);
        ChangeSupplies(SupplyType.Build, supplyCrates.BuildCount);
        ChangeSupplies(SupplyType.Ammo, supplyCrates.AmmoCount);

        UpdateIcon();
    }

    /// <inheritdoc />
    public virtual Color32 GetColor(Team viewingTeam)
    {
        return IsProxied ? Color.red : Color.cyan;
    }

    public virtual void UpdateConfiguration(FobConfiguration configuration)
    {
        UpdateIcon();
    }

    protected void UpdateIcon()
    {
        if (Icon != null)
        {
            Icon.Dispose();
            Icon = null;
        }

        ShovelableInfo? shovelable = FobManager.Configuration.Shovelables.FirstOrDefault(x =>
            x.Foundation.MatchAsset(Buildable.Asset) || x.CompletedStructure.MatchAsset(Buildable.Asset)
        );

        if (shovelable == null)
        {
            return;
        }

        string? icon = null;
        Vector3 offset = shovelable.IconOffset;

        if (shovelable.Foundation.MatchAsset(Buildable.Asset) && !string.IsNullOrEmpty(shovelable.FoundationIcon))
        {
            icon = shovelable.FoundationIcon;
        }

        if (!string.IsNullOrEmpty(shovelable.Icon))
            icon ??= shovelable.Icon;

        if (icon == null)
            return;

        IAssetLink<EffectAsset> iconAsset = _assetConfiguration.GetAssetLink<EffectAsset>(icon);

        if (!iconAsset.TryGetAsset(out _) || _worldIconManager == null)
            return;

        Icon = new WorldIconInfo(Buildable, iconAsset, Team)
        {
            Offset = offset,
            RelevanceDistance = 512f,
            TickSpeed = 10f
        };

        _worldIconManager.CreateIcon(Icon);
    }

    void IDeployable.OnDeployTo(WarfarePlayer player, in DeploySettings settings)
    {
        // send a reminder to rearm if on low ammo
        if (!player.Save.WasKitLowAmmo || _tipService == null)
            return;

        if (FobManager.Entities
            .OfType<SupplyCrate>()
            .Any(x => x.Type == SupplyType.Ammo && x.IsWithinRadius(Buildable.Position)))
        {
            _tipService?.TryGiveTip(player, 0, _tipService.Translations.KitGiveLowAmmo);
        }
    }

    public virtual bool IsVisibleToPlayer(WarfarePlayer player) => player.IsOnline && player.Team == Team;

    private float GetProxyScore(WarfarePlayer enemy)
    {
        if (!enemy.IsOnline || enemy.UnturnedPlayer.life.isDead || enemy.Team.IsFriendly(Team))
            return 0;

        float distanceFromFob = (enemy.Position - Position).magnitude;

        if (distanceFromFob > EffectiveRadius)
            return 0;

        return 0.15f * EffectiveRadius / distanceFromFob;
    }
    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Position, point, EffectiveRadius);
    public void ChangeSupplies(SupplyType supplyType, float amount)
    {
        if (supplyType == SupplyType.Ammo)
        {
            amount = Math.Max(-AmmoCount, amount);
            AmmoCount += amount;
        }
        else if (supplyType == SupplyType.Build)
        {
            amount = Math.Max(-BuildCount, amount);
            BuildCount += amount;
        }

        NotifySuppliesChanged(supplyType, amount);
    }

    public Vector3 SpawnPosition => Position; // todo
    public float Yaw => 0f;

    protected virtual void NotifySuppliesChanged(SupplyType supplyType, float change)
    {

    }

    public virtual TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.Zero;
    }
    public virtual bool CheckDeployableTo(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployEnemiesNearby, this);
            return false;
        }

        return true;
    }

    public virtual bool CheckDeployableFrom(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        return true;
    }

    public virtual bool CheckDeployableToTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployEnemiesNearbyTick, this);
            return false;
        }
        return true;
    }

    public override string ToString()
    {
        // Deploy log uses this
        return $"\"{Name}\" | Team: {Team}";
    }

    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return formatter.Colorize(Name, GetColor(parameters.Team ?? Team.NoTeam), parameters.Options);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        NearbyFriendlies.Dispose();
        NearbyEnemies.Dispose();

        _loopTicker.Dispose();
        Icon?.Dispose();
        Icon = null;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    bool IDeployable.IsSafeZone => false;

    /// <inheritdoc />
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    void ITransformObject.SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        Buildable.SetPositionAndRotation(position, rotation);
        if (Icon != null)
            _worldIconManager?.UpdateIcon(Icon.Effect);
    }

    /// <inheritdoc />
    bool ITransformObject.Alive => Buildable.Alive;
}
