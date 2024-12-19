using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Util.Timing.Collectors;

namespace Uncreated.Warfare.Fobs;

/// <summary>
/// Base class for standard FOBs, caches, and any other FOBs that support items.
/// </summary>
public class BasePlayableFob : IResourceFob, IDisposable
{
    
    private readonly IPlayerService _playerService;
    private readonly FobManager _fobManager;
    private readonly ILogger _logger;
    private readonly ILoopTicker _loopTicker;

    public IBuildable Buildable { get; protected set; }

    /// <inheritdoc />
    public int BuildCount { get; private set; }
    /// <inheritdoc />
    public int AmmoCount { get; private set; }

    /// <inheritdoc />
    public string Name { get; private set; }

    /// <inheritdoc />
    public virtual Color32 Color
    {
        get
        {
            if (IsProxied)
                return UnityEngine.Color.red;

            return UnityEngine.Color.cyan;
        }
    }

    /// <inheritdoc />
    public Team Team { get; private set; }

    public Vector3 Position => Buildable.Position;
    public float EffectiveRadius => 70f;
    public bool IsProxied { get; private set; }
    public ISphereProximity FriendlyProximity { get; private set; }
    public ISphereProximity EnemyProximity { get; private set; }
    public ProximityCollector<WarfarePlayer> NearbyFriendlies { get; private set; }
    public ProximityCollector<WarfarePlayer> NearbyEnemies { get; private set; }
    public ReadOnlyTrackingList<IFobEntity> GetEntities() => _fobManager.Entities.Where(e => MathUtility.WithinRange(Position, e.Position, EffectiveRadius)).ToTrackingList().AsReadOnly();

    /// <summary>
    /// Invoked when a player enters the radius of the FOB.
    /// </summary>
    public event Action<WarfarePlayer>? OnPlayerEntered;

    /// <summary>
    /// Invoked when a player exits the radius of the FOB. Always invoked before the player enters the next FOB if they teleport to another.
    /// </summary>
    public event Action<WarfarePlayer>? OnPlayerExited;

    public event Action<InteractableVehicle>? OnVehicleEntered;
    public event Action<InteractableVehicle>? OnVehicleExited;

    public event Action<IFobEntity>? OnItemAdded;
    public event Action<IFobEntity>? OnItemRemoved;

    public BasePlayableFob(IServiceProvider serviceProvider, string name, IBuildable buildable)
    {
        Name = name;
        Buildable = buildable;
        Team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType().Name + " | " + Name);
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _fobManager = serviceProvider.GetRequiredService<FobManager>();

        FriendlyProximity = new SphereProximity(Position, EffectiveRadius);

        _loopTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>().CreateTicker(TimeSpan.FromSeconds(0.5f), true, true);

        NearbyFriendlies = new ProximityCollector<WarfarePlayer>(
            new ProximityCollector<WarfarePlayer>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _playerService.OnlinePlayers.Where(p => p.Team == Team),
                PositionFunction = p => p.Position
            }
        );
        NearbyEnemies = new ProximityCollector<WarfarePlayer>(
            new ProximityCollector<WarfarePlayer>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _playerService.OnlinePlayers.Where(p => p.Team != Team),
                PositionFunction = p => p.Position
            }
        );
        _loopTicker.OnTick += (ticker, timeSinceStart, deltaTime) =>
        {
            bool newProxyState = NearbyEnemies.Collection.Sum(GetProxyScore) >= 1;
            if (newProxyState != IsProxied)
            {
                IsProxied = newProxyState;
                _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobProxyChanged { Fob = this, IsProxied = newProxyState });
            }
        };
        NearbySupplyCrates supplyCrates = NearbySupplyCrates.FindNearbyCrates(Position, Team.GroupId, _fobManager);
        ChangeSupplies(SupplyType.Build, supplyCrates.BuildCount);
        ChangeSupplies(SupplyType.Ammo, supplyCrates.AmmoCount);
    }
    private float GetProxyScore(WarfarePlayer enemy)
    {
        if (enemy.UnturnedPlayer.life.isDead || enemy.UnturnedPlayer.movement.getVehicle() != null)
            return 0;

        float distanceFromFob = (enemy.Position - Position).magnitude;

        if (distanceFromFob > EffectiveRadius)
            return 0;

        return 0.15f * EffectiveRadius / distanceFromFob;
    }
    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Position, point, EffectiveRadius);
    public void ChangeSupplies(SupplyType supplyType, int amount)
    {
        if (supplyType == SupplyType.Build)
            BuildCount += amount;
        else if (supplyType == SupplyType.Ammo)
            AmmoCount += amount;
    }
    public UniTask DestroyAsync(CancellationToken token = default)
    {
        return UniTask.CompletedTask;
    }

    public UniTask AddItemAsync(IFobEntity fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public UniTask BuildItemAsync(IFobEntity fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Vector3 SpawnPosition => Position; // todo
    public float Yaw => 0f;

    public TimeSpan GetDelay(WarfarePlayer player)
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
        return $"Fob: {Name}, Team: {Team}, Position: {Position}, BuildCount: {BuildCount}, AmmoCount: {AmmoCount}, EffectiveRadius: {EffectiveRadius}\n" +
               $"NearbyFriendlies: {NearbyFriendlies.Collection.Count}\n" +
               $"NearbyEnemies: {NearbyEnemies.Collection.Count}\n" +
               $"Items: {GetEntities().Count}\n"
               ;
    }
    public string Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        return formatter.Colorize(Name, Color, parameters.Options);
    }

    public void Dispose()
    {
        _loopTicker.Dispose();
    }

    public int CompareTo(IFob other)
    {
        throw new NotImplementedException();
    }
}
