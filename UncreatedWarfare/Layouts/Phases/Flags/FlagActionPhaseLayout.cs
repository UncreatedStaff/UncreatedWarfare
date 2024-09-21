using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Zones.Pathing;

namespace Uncreated.Warfare.Layouts.Phases.Flags;
public class FlagActionPhaseLayout : IFlagRotationPhase
{
    private readonly ILogger<FlagActionPhaseLayout> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IList<Zone>? _pathingResult;
    private ZoneStore _zoneStore;

    /// <summary>
    /// Array of all zones in order *including the main bases* at the beginning and end of the list.
    /// </summary>
    private ActiveZoneCluster[]? _zones;

    public bool IsActive { get; private set; }
    public FlagPhaseSettings Flags { get; set; } = new FlagPhaseSettings();

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IReadOnlyList<ActiveZoneCluster> ActiveZones { get; private set; } = Array.Empty<ActiveZoneCluster>();

    /// <inheritdoc />
    public ActiveZoneCluster StartingTeam { get; private set; }

    /// <inheritdoc />
    public ActiveZoneCluster EndingTeam { get; private set; }

    public FlagActionPhaseLayout(ILogger<FlagActionPhaseLayout> logger, IServiceProvider serviceProvider, IConfiguration config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        Configuration = config;
    }

    public async UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        if (!ContextualTypeResolver.TryResolveType(Flags.Pathing, out Type? pathingProviderType, typeof(IZonePathingProvider)))
        {
            _logger.LogError("Unknown or invalid pathing provider type: {0}.", Flags.Pathing);

            throw new LayoutConfigurationException(this, "Invalid pathing provider type.");
        }

        // create zone providers from config
        IReadOnlyList<Type> zoneProviderTypes = Flags.GetFlagPoolTypes(_logger);

        List<IZoneProvider> zoneProviders = new List<IZoneProvider>(zoneProviderTypes.Count);
        foreach (Type zoneProviderType in zoneProviderTypes)
        {
            zoneProviders.Add((IZoneProvider)ReflectionUtility.CreateInstanceFixed(_serviceProvider, zoneProviderType, [ this ]));
        }

        _zoneStore = new ZoneStore(zoneProviders, _serviceProvider.GetRequiredService<ILogger<ZoneStore>>(), isGlobal: false);

        // load pathing provider
        IConfigurationSection config = Configuration.GetSection("PathingData");
        IZonePathingProvider pathingProvider = (IZonePathingProvider)ReflectionUtility.CreateInstanceFixed(_serviceProvider, pathingProviderType, [ _zoneStore, this, config ]);

        config.Bind(pathingProvider);

        // create zone path
        _pathingResult = await pathingProvider.CreateZonePathAsync(this, token);

        _logger.LogInformation("Zone path: {{{0}}}.", string.Join(" -> ", _pathingResult.Skip(1).SkipLast(1).Select(zone => zone.Name)));
    }

    public virtual async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        IsActive = true;

        if (_pathingResult == null || _zoneStore == null)
        {
            throw new LayoutConfigurationException(this, "Unable to create zone path.");
        }

        await UniTask.SwitchToMainThread(token);

        // create zones as objects with colliders

        List<ActiveZoneCluster> zoneList = new List<ActiveZoneCluster>(_pathingResult.Count);
        foreach (Zone zone in _pathingResult)
        {
            ZoneProximity[] zones = _zoneStore.Zones
                .Where(z => z.Name.Equals(zone.Name, StringComparison.Ordinal))
                .Select(z => new ZoneProximity(_zoneStore.CreateColliderForZone(z), z))
                .ToArray();

            zoneList.Add(new ActiveZoneCluster(zones, _serviceProvider));
        }

        _zones = zoneList.ToArrayFast();
        if (_zones.Length < 3)
        {
            throw new LayoutConfigurationException(this, "Unable to create zone path longer than one zone (not including main bases).");
        }

        ActiveZones = new ReadOnlyCollection<ActiveZoneCluster>(new ArraySegment<ActiveZoneCluster>(_zones, 1, _zones.Length - 2));
        StartingTeam = _zones[0];
        EndingTeam = _zones[^1];
    }

    public virtual async UniTask EndPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        // destroy collider objects
        foreach (ActiveZoneCluster cluster in ActiveZones)
        {
            cluster.Dispose();
        }

        _zones = null;
        ActiveZones = Array.Empty<ActiveZoneCluster>();
        IsActive = false;
    }
}