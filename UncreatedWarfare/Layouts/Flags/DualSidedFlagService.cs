using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Zones.Pathing;

namespace Uncreated.Warfare.Layouts.Flags;
public abstract class DualSidedFlagService : 
    ILayoutHostedService,
    IFlagRotationService,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>
{
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _loopTicker;
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ITeamManager<Team> TeamManager;
    protected readonly Layout Layout;
    protected IList<Zone>? PathingResult { get; private set; }
    protected ZoneStore ZoneStore { get; private set; }
    protected FlagPhaseSettings FlagSettings { get; private set; }

    /// <summary>
    /// Array of all zones in order *including the main bases* at the beginning and end of the list.
    /// </summary>
    private FlagObjective[]? _flags;

    public bool IsActive { get; private set; }

    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IReadOnlyList<FlagObjective> ActiveFlags { get; private set; } = Array.Empty<FlagObjective>();

    public FlagObjective StartingTeam { get; private set; }

    public FlagObjective EndingTeam { get; private set; }

    protected DualSidedFlagService(IServiceProvider serviceProvider, IConfiguration config)
    {
        _loopTickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        ServiceProvider = serviceProvider;
        Layout = serviceProvider.GetRequiredService<Layout>();
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

        TimeSpan tickSpeed = Configuration.GetValue("TickSpeed", TimeSpan.FromSeconds(4d));

        RecalculateObjectives();

        _loopTicker = _loopTickerFactory.CreateTicker(tickSpeed, tickSpeed, true, OnTick);
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

        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();

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
            flagList.Add(new FlagObjective(region, Team.NoTeam));
        }

        _flags = flagList.ToArrayFast();
        if (_flags.Length < 3)
        {
            throw new LayoutConfigurationException("Unable to create zone path longer than one zone (not including main bases).");
        }

        ActiveFlags = new ReadOnlyCollection<FlagObjective>(new ArraySegment<FlagObjective>(_flags, 1, _flags.Length - 2));
        StartingTeam = new FlagObjective(_flags[0].Region, TeamManager.AllTeams.First());
        EndingTeam = new FlagObjective(_flags[^1].Region, TeamManager.AllTeams.Last());
        IsActive = true;

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FlagsSetUp { ActiveFlags = ActiveFlags, FlagService = this });
    }

    protected void TriggerVictory(Team winner)
    {
        Layout.Data[KnownLayoutDataKeys.WinnerTeam] = winner;

        _ = Layout.MoveToNextPhase(token: default);
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        foreach (FlagObjective flag in ActiveFlags)
        {
            FlagContestResult contestResult = GetContestResult(flag, Layout.TeamManager.AllTeams);
            if (contestResult.State == FlagContestResult.ContestState.OneTeamIsLeading)
            {
                flag.MarkContested(false);
                flag.Contest.AwardPoints(contestResult.Leader!, 12);
            }
            else if (contestResult.State == FlagContestResult.ContestState.Contested)
                flag.MarkContested(true);
        }
    }

    void IEventListener<FlagNeutralized>.HandleEvent(FlagNeutralized e, IServiceProvider serviceProvider)
    {
        RecalculateObjectives();
    }

    void IEventListener<FlagCaptured>.HandleEvent(FlagCaptured e, IServiceProvider serviceProvider)
    {
        RecalculateObjectives();

        foreach (Team team in Layout.TeamManager.AllTeams)
        {
            if (ActiveFlags.All(f => f.Owner == team))
            {
                TriggerVictory(team);
                return;
            }
        }
    }
    protected abstract void RecalculateObjectives();
    public abstract FlagContestResult GetContestResult(FlagObjective flag, IEnumerable<Team> possibleContestingTeams);
    public abstract FlagObjective? GetObjective(Team team);
}