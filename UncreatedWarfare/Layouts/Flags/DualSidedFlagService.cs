using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Zones;
using Uncreated.Warfare.Zones.Pathing;

namespace Uncreated.Warfare.Layouts.Flags;
public abstract class DualSidedFlagService : 
    ILayoutHostedService,
    IFlagRotationService,
    IEventListener<FlagCaptured>,
    IEventListener<FlagNeutralized>,
    IEventListener<PlayerEnteredFlagRegion>,
    IEventListener<PlayerExitedFlagRegion>
{
    private const double TickInternalSeconds = 4;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private ILoopTicker? _evaluationTicker;
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ITeamManager<Team> TeamManager;
    protected readonly Layout Layout;
    protected IList<Zone>? PathingResult { get; private set; }

#nullable disable

    protected ZoneStore ZoneStore { get; private set; }

    protected FlagPhaseSettings FlagSettings { get; private set; }

    public ZoneRegion StartingTeamRegion { get; private set; }

    public Team StartingTeam { get; private set; }

    public ZoneRegion EndingTeamRegion { get; private set; }

    public Team EndingTeam { get; private set; }

#nullable restore

    /// <summary>
    /// Array of all zones in order *including the main bases* at the beginning and end of the list.
    /// </summary>
    private FlagObjective[]? _flags;

    public bool IsActive { get; private set; }

    public IConfiguration Configuration { get; }

    /// <inheritdoc />
    public IReadOnlyList<FlagObjective> ActiveFlags { get; private set; } = Array.Empty<FlagObjective>();

    /// <inheritdoc />
    public virtual ElectricalGridBehaivor GridBehaivor => ElectricalGridBehaivor.EnabledWhenInRotation;

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

        TimeSpan tickSpeed = Configuration.GetValue("TickSpeed", TimeSpan.FromSeconds(TickInternalSeconds));

        RecalculateObjectives();
        // invoke objectivesChanged

        _evaluationTicker = _loopTickerFactory.CreateTicker(tickSpeed, tickSpeed, true, OnTick);
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

        Interlocked.Exchange(ref _evaluationTicker, null)?.Dispose();

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
        for (int i = 1; i < PathingResult.Count - 1; i++)
        {
            Zone zone = PathingResult[i];
            ZoneProximity[] zones = ZoneStore.Zones
                .Where(z => z.Name.Equals(zone.Name, StringComparison.Ordinal))
                .Select(z => new ZoneProximity(ZoneStore.CreateColliderForZone(z), z))
                .ToArray();

            ZoneRegion region = new ZoneRegion(zones, TeamManager);
            flagList.Add(new FlagObjective(region, Team.NoTeam));
        }

        _flags = flagList.ToArrayFast();
        if (_flags.Length < 1)
        {
            throw new LayoutConfigurationException("Unable to create zone path longer than one zone (not including main bases).");
        }

        ZoneProximity[] t1Zones = ZoneStore.Zones
            .Where(z => z.Name.Equals(PathingResult[0].Name, StringComparison.Ordinal))
            .Select(z => new ZoneProximity(ZoneStore.CreateColliderForZone(z), z))
            .ToArray();
        ZoneProximity[] t2Zones = ZoneStore.Zones
            .Where(z => z.Name.Equals(PathingResult[^1].Name, StringComparison.Ordinal))
            .Select(z => new ZoneProximity(ZoneStore.CreateColliderForZone(z), z))
            .ToArray();

        ActiveFlags = new ReadOnlyCollection<FlagObjective>(_flags);
        
        StartingTeamRegion = new ZoneRegion(t1Zones, TeamManager);
        StartingTeam = TeamManager.AllTeams.First(x => x.Faction.FactionId.Equals(StartingTeamRegion.Primary.Zone.Faction, StringComparison.Ordinal));
        
        EndingTeamRegion = new ZoneRegion(t2Zones, TeamManager);
        EndingTeam = TeamManager.AllTeams.First(x => x.Faction.FactionId.Equals(EndingTeamRegion.Primary.Zone.Faction, StringComparison.Ordinal));
        
        IsActive = true;

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FlagsSetUp { ActiveFlags = ActiveFlags, FlagService = this });
    }

    protected void TriggerVictory(Team winner)
    {
        Layout.Data[KnownLayoutDataKeys.WinnerTeam] = winner;

        _ = Layout.MoveToNextPhase(token: CancellationToken.None);
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        foreach (FlagObjective flag in ActiveFlags)
        {
            FlagContestState contestState = GetContestResult(flag, Layout.TeamManager.AllTeams);
            FlagContestState oldState = flag.CurrentContestState;
            flag.SetCurrentContestState(in contestState, invokeEvent: false);
            if (contestState.State == FlagContestState.ContestState.OneTeamIsLeading)
            {
                flag.MarkContested(false);
                flag.Contest.AwardPoints(contestState.Winner!, 12);
            }
            else if (contestState.State == FlagContestState.ContestState.Contested)
                flag.MarkContested(true);

            flag.InvokeContestStateChangedEvent(in oldState, in contestState);

            if (timeSinceStart.Seconds % (TickInternalSeconds * 4) == 0 && 
                !(contestState.State == FlagContestState.ContestState.NotObjective || contestState.State == FlagContestState.ContestState.NoPlayers))
            {
                SlowTickObjective(flag, contestState);
            }
        }
    }
    
    private void SlowTickObjective(FlagObjective flag, FlagContestState contestState)
    {
        ObjectiveSlowTick args = new ObjectiveSlowTick
        {
            Flag = flag,
            ContestState = contestState
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
    
    [EventListener(Priority = int.MaxValue)]
    public void HandleEvent(PlayerEnteredFlagRegion e, IServiceProvider serviceProvider)
    {
        e.Flag.SetCurrentContestState(GetContestResult(e.Flag, TeamManager.AllTeams));
    }
    
    [EventListener(Priority = int.MaxValue)]
    public void HandleEvent(PlayerExitedFlagRegion e, IServiceProvider serviceProvider)
    {
        e.Flag.SetCurrentContestState(GetContestResult(e.Flag, TeamManager.AllTeams));
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

    /// <summary>
    /// Must return a <see cref="FlagContestState"/> describing the resultant state of the contest caused by players trying to capture it, if any.
    /// <remarks>
    /// <para>
    /// The logic of this method should account for all the result cases featured in <see cref="FlagContestState.ContestState"/>.
    /// </para>
    /// <para>
    /// This method is called every flag tick (usually a few seconds), as well as on some other occasions, on every active flag in the rotation.
    /// </para>
    /// </remarks>
    /// 
    /// </summary>
    /// <param name="flag">The flag that is being evaluated.</param>
    /// <param name="possibleContestingTeams">A selection of possible teams who may be contesting the flag.</param>
    public abstract FlagContestState GetContestResult(FlagObjective flag, IEnumerable<Team> possibleContestingTeams);
    public abstract FlagObjective? GetObjective(Team team);

    public virtual IEnumerable<FlagObjective> EnumerateObjectives()
    {
        foreach (Team team in TeamManager.AllTeams)
        {
            FlagObjective? obj = GetObjective(team);
            if (obj != null)
                yield return obj;
        }
    }

    public IEnumerable<FlagListUIEntry> EnumerateFlagListEntries(LanguageSet set)
    {
        FlagObjective? objective = GetObjective(set.Team);

        // reverse list for ending team
        if (set.Team == EndingTeam)
        {
            for (int i = ActiveFlags.Count - 1; i >= 0; --i)
            {
                FlagObjective flag = ActiveFlags[i];
                yield return GetEntry(flag, ReferenceEquals(flag, objective));
            }
        }
        else
        {
            foreach (FlagObjective flag in ActiveFlags)
            {
                yield return GetEntry(flag, ReferenceEquals(flag, objective));
            }
        }

        yield break;

        static FlagListUIEntry GetEntry(FlagObjective flag, bool isObjective)
        {
            const string objectiveIcon = "<#ff8963>µ</color>";
            return new FlagListUIEntry(TranslationFormattingUtility.Colorize(flag.Name, flag.Owner.Faction.Color), isObjective ? objectiveIcon : string.Empty);
        }
    }
}