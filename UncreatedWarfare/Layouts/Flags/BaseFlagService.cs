using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones.Pathing;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Layouts.Flags;
public abstract class BaseFlagService : ILayoutHostedService, IFlagRotationService
{
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ITeamManager<Team> TeamManager;
    protected IList<Zone>? PathingResult { get; private set; }
    protected ZoneStore ZoneStore { get; private set; }
    protected FlagPhaseSettings FlagSettings { get; private set; }

    /// <summary>
    /// Array of all zones in order *including the main bases* at the beginning and end of the list.
    /// </summary>
    private FlagObjective[]? _flags;

    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IReadOnlyList<FlagObjective> ActiveFlags { get; private set; } = Array.Empty<FlagObjective>();

    /// <inheritdoc />
    public ZoneRegion StartingTeam { get; private set; }

    /// <inheritdoc />
    public ZoneRegion EndingTeam { get; private set; }

    public BaseFlagService(IServiceProvider serviceProvider, IConfiguration config)
    {
        Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        ServiceProvider = serviceProvider;
        TeamManager = serviceProvider.GetRequiredService<ITeamManager<Team>>();
        FlagSettings = new FlagPhaseSettings();
        Configuration = config;
    }

    public virtual async UniTask StartAsync(CancellationToken token)
    {
        Configuration.Bind(FlagSettings);

        await CreateZonePaths(token);
        await UniTask.SwitchToMainThread(token);
        SetupFlags();
    }

    public virtual UniTask StopAsync(CancellationToken token)
    {
        foreach (FlagObjective flag in ActiveFlags)
        {
            flag.Dispose();
        }

        _flags = null;
        ActiveFlags = Array.Empty<FlagObjective>();
        IsActive = false;

        return UniTask.CompletedTask;
    }
    private async UniTask CreateZonePaths(CancellationToken token)
    {
        if (!ContextualTypeResolver.TryResolveType(FlagSettings.Pathing, out Type? pathingProviderType, typeof(IZonePathingProvider)))
        {
            Logger.LogError("Unknown or invalid pathing provider type: {0}.", FlagSettings.Pathing);

            throw new LayoutConfigurationException("Invalid pathing provider type.");
        }

        // create zone providers from config
        IReadOnlyList<Type> zoneProviderTypes = FlagSettings.GetFlagPoolTypes(Logger);

        List<IZoneProvider> zoneProviders = new List<IZoneProvider>(zoneProviderTypes.Count);
        foreach (Type zoneProviderType in zoneProviderTypes)
        {
            zoneProviders.Add((IZoneProvider)ReflectionUtility.CreateInstanceFixed(ServiceProvider, zoneProviderType, [this]));
        }

        ZoneStore = ActivatorUtilities.CreateInstance<ZoneStore>(ServiceProvider, [zoneProviders, false]);

        await ZoneStore.Initialize(token);

        // load pathing provider
        IConfigurationSection config = Configuration.GetSection("PathingData");
        IZonePathingProvider pathingProvider = (IZonePathingProvider)ReflectionUtility.CreateInstanceFixed(ServiceProvider, pathingProviderType, [ZoneStore, this, config]);

        config.Bind(pathingProvider);

        // create zone path
        PathingResult = await pathingProvider.CreateZonePathAsync(token);

        Logger.LogInformation("Zone path: {{{0}}}.", string.Join(" -> ", PathingResult.Skip(1).SkipLast(1).Select(zone => zone.Name)));
    }
    private void SetupFlags()
    {
        if (PathingResult == null || ZoneStore == null)
        {
            throw new LayoutConfigurationException("Unable to create zone path.");
        }

        // create zones as objects with colliders

        List<FlagObjective> flagList = new List<FlagObjective>(PathingResult.Count);
        foreach (Zone zone in PathingResult)
        {
            ZoneProximity[] zones = ZoneStore.Zones
                .Where(z => z.Name.Equals(zone.Name, StringComparison.Ordinal))
                .Select(z => new ZoneProximity(ZoneStore.CreateColliderForZone(z), z))
                .ToArray();

            ZoneRegion region = new ZoneRegion(zones, TeamManager);
            flagList.Add(new FlagObjective(region, TeamManager, Team.NoTeam));
        }

        _flags = flagList.ToArrayFast();
        if (_flags.Length < 3)
        {
            throw new LayoutConfigurationException("Unable to create zone path longer than one zone (not including main bases).");
        }

        ActiveFlags = new ReadOnlyCollection<FlagObjective>(new ArraySegment<FlagObjective>(_flags, 1, _flags.Length - 2));
        StartingTeam = _flags[0].Region;
        EndingTeam = _flags[^1].Region;
        IsActive = true;
    }

    public abstract FlagObjective? GetObjective(Team team);
}
