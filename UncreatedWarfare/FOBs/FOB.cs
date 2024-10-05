using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
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

    public IBuildable Buildable { get; private set; }

    /// <inheritdoc />
    public int BuildCount { get; private set; }
    /// <inheritdoc />
    public int AmmoCount { get; private set; }

    /// <inheritdoc />
    public string Name { get; private set; }

    /// <inheritdoc />
    public Color32 Color { get; private set; }

    /// <inheritdoc />
    public Team Team { get; private set; }

    /// <inheritdoc />
    public Vector3 Position
    {
        get => Buildable.Position;
        set => throw new NotSupportedException();
    }
    public float EffectiveRadius => 50f;
    public ISphereProximity FriendlyProximity { get; private set; }
    public ISphereProximity EnemyProximity { get; private set; }
    public ProximityCollector<WarfarePlayer> NearbyFriendlies { get; private set; }
    public ProximityCollector<WarfarePlayer> NearbyEnemies { get; private set; }
    public ProximityCollector<IFobItem> Items { get; private set; }

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

    public event Action<IFobItem>? OnItemAdded;
    public event Action<IFobItem>? OnItemRemoved;

    public BasePlayableFob(IServiceProvider serviceProvider, string name, IBuildable buildable)
    {
        Name = name;
        Buildable = buildable;
        Team = serviceProvider.GetRequiredService<ITeamManager<Team>>().GetTeam(buildable.Group);
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType().Name + " | " + Name);
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _fobManager = serviceProvider.GetRequiredService<FobManager>();

        FriendlyProximity = new SphereProximity(Position, EffectiveRadius);

        _loopTicker = new UnityLoopTickerFactory().CreateTicker(TimeSpan.FromSeconds(0.5f), true, true);

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
        Items = new ProximityCollector<IFobItem>(
            new ProximityCollector<IFobItem>.ProximityCollectorOptions
            {
                Ticker = _loopTicker,
                Proximity = FriendlyProximity,
                ObjectsToCollect = () => _fobManager.FloatingItems,
                PositionFunction = i => i.Position,
                OnItemAdded = (i) =>
                {
                    if (i is SupplyCrate crate)
                        AddSupplies(crate.SupplyCount, crate.Type);

                    _logger.LogInformation("Added fob item. State: " + this);
                },
                OnItemRemoved = (i) =>
                {
                    if (i is SupplyCrate crate)
                        SubstractSupplies(crate.SupplyCount, crate.Type, false);

                    _logger.LogInformation("Removed fob item. State: " + this);
                }
            }
        );
    }
    public void AddSupplies(int amount, SupplyType type)
    {
        if (type == SupplyType.Ammo)
            AmmoCount = Mathf.Max(AmmoCount + amount, 0);
        if (type == SupplyType.Build)
            BuildCount = Mathf.Max(BuildCount + amount, 0);
    }
    public void SubstractSupplies(int amount, SupplyType type, bool subtractFromCrates = true)
    {
        AddSupplies(-amount, type);

        if (!subtractFromCrates)
            return;

        // substarct from crates
        foreach (SupplyCrate crate in Items.Collection.Where(i => i is SupplyCrate s && s.Type == type))
        {
            int remainder = crate.SupplyCount - amount;
            int toSubstract = Mathf.Clamp(crate.SupplyCount - amount, 0, crate.SupplyCount);
            crate.SupplyCount = toSubstract;

            if (remainder <= 0)
            {
                // todo: destroy this crate
                // move on and try to substract the remainder from the next crate
                amount = -remainder;
            }

            if (remainder >= 0) // no need to substract from any further crates
                break;
        }
    }
    public UniTask DestroyAsync(CancellationToken token = default)
    {
        return UniTask.CompletedTask;
    }

    public UniTask AddItemAsync(IFobItem fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public UniTask BuildItemAsync(IFobItem fobItem, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Vector3 SpawnPosition => Position; // todo
    public float Yaw => 0f;

    public TimeSpan GetDelay(WarfarePlayer player)
    {
        return TimeSpan.Zero;
    }
    public bool CheckDeployableTo(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        return NearbyEnemies.Collection.Count > 0;
    }

    public bool CheckDeployableFrom(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings, IDeployable deployingTo)
    {
        return true;
    }

    public bool CheckDeployableToTick(WarfarePlayer player, DeploymentTranslations translations, in DeploySettings settings)
    {
        return NearbyEnemies.Collection.Count > 0;
    }

    public override string ToString()
    {
        return $"Fob: {Name}, Team: {Team}, Position: {Position}, BuildCount: {BuildCount}, AmmoCount: {AmmoCount}, EffectiveRadius: {EffectiveRadius}\n" +
               $"NearbyFriendlies: {NearbyFriendlies.Collection.Count}\n" +
               $"NearbyEnemies: {NearbyEnemies.Collection.Count}\n" +
               $"Items: {Items.Collection.Count}\n"
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
