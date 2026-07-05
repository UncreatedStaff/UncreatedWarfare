using Microsoft.Extensions.DependencyInjection;
using System;
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
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.StrategyMaps.MapTacks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Util.Timing.Collectors;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;
using HealthUpdated = Uncreated.Warfare.StrategyMaps.MapTacks.HealthUpdated;
using VehicleUpdated = Uncreated.Warfare.StrategyMaps.MapTacks.VehicleUpdated;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Base class for standard FOBs, caches, and any other FOBs that support items.
/// </summary>
public class ResourceFob : IBuildableFob, IResourceFob, IDisposable, IMapTackUIHandler
{
    // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
    private readonly IPlayerService _playerService;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly WorldIconManager? _worldIconManager;
    private readonly TipService? _tipService;
    protected readonly FobManager FobManager;
    private readonly ILoopTicker _loopTicker;
    private readonly ZoneStore _zoneStore;
    private readonly VehicleInfoStore _vehicleInfoStore;
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

    public virtual float EffectiveRadius => 70f;
    public bool IsProxied { get; private set; }
    public ISphereProximity FriendlyProximity { get; }
    public ProximityCollector<WarfarePlayer> NearbyFriendlies { get; }
    public ProximityCollector<WarfarePlayer> NearbyEnemies { get; }
    public IEnumerable<IFobEntity> EnumerateEntities() => FobManager.Entities.Where(e => MathUtility.WithinRange(Position, e.Position, EffectiveRadius));

    public WorldIconInfo? Icon { get; private set; }

    /// <summary>
    /// The name of the closest location to this FOB.
    /// </summary>
    [field: MaybeNull]
    public string ClosestLocation
    {
        get
        {
            field ??= _zoneStore.GetClosestLocationName(Position, true);
            return field;
        }
    }

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
        _zoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _vehicleInfoStore = serviceProvider.GetRequiredService<VehicleInfoStore>();

        FriendlyProximity = new SphereProximity(Position, EffectiveRadius);

        _loopTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromSeconds(0.25f), true, true);

        NearbyFriendlies = new ProximityCollector<WarfarePlayer>(
            new ProximityCollector<WarfarePlayer>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _playerService.OnlinePlayers.Where(p => p.Team.IsFriendly(Team)),
                PositionFunction = p => p.Position,
                OnItemAdded = p =>
                {
                    InvokeVehicleUpdated(MapTackVehicleType.Infantry, NearbyFriendlies!.Collection.Count);
                }
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
                InvokeAttributesUpdated();
                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobProxyChanged { Fob = this, IsProxied = newProxyState });
            }

            UpdateVehicleCounts(null);
        };

        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(Position, Team.GroupId, FobManager);
        ChangeSupplies(SupplyType.Build, supplyCrates.BuildCount, SupplyChangeReason.InitialSupplyFob);
        ChangeSupplies(SupplyType.Ammo, supplyCrates.AmmoCount, SupplyChangeReason.InitialSupplyFob);

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

    protected virtual void OnDeployTo(WarfarePlayer player, in DeploySettings settings)
    {
        // send a reminder to rearm if on low ammo
        KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();

        if (_tipService == null || kitComp.GetActiveEffectiveKit() is { IsLowAmmo: false })
            return;

        if (FobManager.Entities
            .OfType<SupplyCrate>()
            .Any(x => x.Type == SupplyType.Ammo && x.IsWithinRadius(Buildable.Position)))
        {
            _tipService.TryGiveTip(player, 0, _tipService.Translations.KitGiveLowAmmo);
        }

        InvokeVehicleUpdated(MapTackVehicleType.Infantry, NearbyFriendlies.Collection.Count + (!NearbyFriendlies.Collection.Contains(player) ? 1 : 0));
    }

    void IDeployable.OnDeployTo(WarfarePlayer player, in DeploySettings settings)
    {
        OnDeployTo(player, in settings);
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
    public void ChangeSupplies(SupplyType supplyType, float amount, SupplyChangeReason reason, WarfarePlayer? instigator = null)
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

        FobSuppliesChanged args = new FobSuppliesChanged
        {
            Fob = this,
            AmountDelta = amount,
            SupplyType = supplyType,
            ChangeReason = reason,
            Instigator = instigator
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }

    public Vector3 SpawnPosition => Position;
    public float Yaw => 0f;

    protected virtual void NotifySuppliesChanged(SupplyType supplyType, float change)
    {
        InvokeSuppliesUpdated(
            supplyType,
            (int)(supplyType == SupplyType.Build ? Math.Round(BuildCount) : Math.Round(AmmoCount))
        );
        InvokeAttributesUpdated();
    }

    #region Deployment

    
    public virtual bool CheckDeployableTo(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployProxied, this);
            return false;
        }

        return true;
    }

    public virtual bool CheckDeployableFrom(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployProxied, this);
            return false;
        }
        return true;
    }

    public virtual bool CheckDeployableFromTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployProxiedTick, this);
            return false;
        }
        return true;
    }

    public virtual bool CheckDeployableToTick(WarfarePlayer player, ChatService chatService, DeploymentTranslations translations, in DeploySettings settings)
    {
        if (IsProxied)
        {
            chatService.Send(player, translations.DeployProxiedTick, this);
            return false;
        }
        return true;
    }
    public virtual TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.Zero;
    }


    #endregion

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

    #region Map Tacks

    public event SuppliesUpdated? OnSuppliesUpdated;
    public event VehicleUpdated? OnVehicleUpdated;
    public event HealthUpdated? OnHealthUpdated;
    public event AttributesUpdated? OnAttributesUpdated;

    protected internal void InvokeSuppliesUpdated(SupplyType type, int amount) => OnSuppliesUpdated?.Invoke(type, amount);

    protected internal void InvokeVehicleUpdated(MapTackVehicleType type, int amount) => OnVehicleUpdated?.Invoke(type, amount);

    protected internal void InvokeHealthUpdated()
    {
        double? health = GetHealth();
        OnHealthUpdated?.Invoke(health);
    }

    protected internal void InvokeAttributesUpdated() => OnAttributesUpdated?.Invoke(GetAttributes());

    public virtual string GetTitle(in LanguageSet languageSet)
    {
        return Name;
    }

    public virtual string GetLocation(in LanguageSet languageSet)
    {
        return ClosestLocation;
    }

    public virtual int? GetSupplyCount(SupplyType type)
    {
        if (type == SupplyType.Build)
            return (int)Math.Round(BuildCount);
    
        if (type == SupplyType.Ammo)
            return (int)Math.Round(AmmoCount);

        return null;
    }

    public virtual double? GetHealth()
    {
        return (double)Buildable.Health / Buildable.MaxHealth;
    }

    public virtual MapTackAttributes GetAttributes()
    {
        MapTackAttributes attributes = 0;
        if (IsProxied)
            attributes |= MapTackAttributes.Proxied;

        NearbySupplyCrates.HasNearbySupplyCrates(Position, Team.GroupId, FobManager, out bool hasAmmo, out bool hasBuild);

        if (!hasAmmo || AmmoCount < 5)
            attributes |= MapTackAttributes.LowAmmo;

        if (!hasBuild || BuildCount < 7)
            attributes |= MapTackAttributes.LowBuild;

        return attributes;
    }

    private static readonly List<InteractableVehicle> WorkingNearbyVehicles = new List<InteractableVehicle>(16);
    private static readonly int[] VehicleCountsBuffer = new int[MapTackVehicleType.Count - 1];

    private readonly int[] _vehicleCounts = new int[MapTackVehicleType.Count - 1];

    protected virtual void UpdateVehicleCounts(IList<KeyValuePair<MapTackVehicleType, int>>? vehicleCounts)
    {
        Buffer.BlockCopy(_vehicleCounts, 0, VehicleCountsBuffer, 0, sizeof(int) * _vehicleCounts.Length);
        Array.Clear(_vehicleCounts, 0, _vehicleCounts.Length);

        int friendlyCount = NearbyFriendlies.Collection.Count;

        _vehicleCounts[(int)MapTackVehicleType.Infantry - 1] = friendlyCount;

        vehicleCounts?.Add(new KeyValuePair<MapTackVehicleType, int>(MapTackVehicleType.Infantry, friendlyCount));

        try
        {
            float r = EffectiveRadius;
            VehicleManager.getVehiclesInRadius(Position, r * r, WorkingNearbyVehicles);

            foreach (InteractableVehicle vehicle in WorkingNearbyVehicles)
            {
                if (vehicle.isDrowned || vehicle.isExploded || !Team.IsFriendly(vehicle.lockedGroup))
                    continue;

                WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(vehicle.asset);
                if (info == null)
                    continue;

                MapTackVehicleType type = MapTackVehicleType.FromVehicleType(info.Type);
                if (type == MapTackVehicleType.Other)
                    continue;

                ++_vehicleCounts[(int)type - 1];

                if (vehicleCounts == null)
                    continue;

                bool found = false;
                for (int i = 0; i < vehicleCounts.Count; ++i)
                {
                    KeyValuePair<MapTackVehicleType, int> kvp = vehicleCounts[i];
                    if (kvp.Key == type)
                    {
                        vehicleCounts[i] = new KeyValuePair<MapTackVehicleType, int>(type, kvp.Value + 1);
                        found = true;
                    }
                }

                if (!found)
                {
                    vehicleCounts.Add(new KeyValuePair<MapTackVehicleType, int>(type, 1));
                }
            }
        }
        finally
        {
            WorkingNearbyVehicles.Clear();
        }

        if (vehicleCounts != null)
            return;

        for (MapTackVehicleType type = (MapTackVehicleType)MapTackVehicleType.Count - 1; type >= MapTackVehicleType.Infantry; --type)
        {
            int old = VehicleCountsBuffer[(int)type - 1];
            int now = _vehicleCounts[(int)type - 1];
            if (old != now)
            {
                InvokeVehicleUpdated(type, now);
            }
        }
    }

    void IMapTackUIHandler.CountVehicles(IList<KeyValuePair<MapTackVehicleType, int>> vehicleCounts)
    {
        UpdateVehicleCounts(vehicleCounts);
    }


    #endregion
}