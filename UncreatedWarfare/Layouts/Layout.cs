using Autofac.Core;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Stripe;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// Lasts for one game. Responsible for loading layouts.
/// </summary>
public class Layout : IDisposable
{
    private int _activePhase = -1;
    private readonly IDisposable _configListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly WarfareLifetimeComponent _appLifetime;
    internal bool UnloadedHostedServices;
    private readonly IList<IDisposable> _disposableVariationConfigurationRoots;

    protected ILogger<Layout> Logger;

    private readonly LayoutFactory _factory;
    private BitArray? _phaseDisposedMask;

    /// <summary>
    /// Data that can be accessed and updated over the lifetime of the layout.
    /// </summary>
    /// <remarks>Known keys are stored in <see cref="KnownLayoutDataKeys"/>.</remarks>
    public IDictionary<string, object> Data { get; } = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// A unique ID to this layout.
    /// </summary>
    public ulong LayoutId { get; private set; }

    /// <summary>
    /// Whether or not the layout was able to fully start.
    /// </summary>
    public bool WasStarted { get; private set; }

#nullable disable

    /// <summary>
    /// Reference to a database entry for this layout.
    /// </summary>
    public GameRecord LayoutStats { get; private set; }

    /// <summary>
    /// Keeps track of all teams.
    /// </summary>
    public ITeamManager<Team> TeamManager { get; protected set; }

#nullable restore

    /// <summary>
    /// If the layout is currently running.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Exposed to allow sub-classes to manually add phases if needed.
    /// </summary>
    protected IList<ILayoutPhase> PhaseList { get; }

    /// <summary>
    /// Configuration that defines how the layout of the game is loaded.
    /// </summary>
    public IConfigurationRoot LayoutConfiguration => LayoutInfo.Layout;

    /// <summary>
    /// Scoped service provider for this layout.
    /// </summary>
    public ILifetimeScope ServiceProvider { get; }

    /// <summary>
    /// Pre-determined settings for the layout.
    /// </summary>
    public LayoutInfo LayoutInfo { get; }

    /// <summary>
    /// Token that will cancel when the layout ends.
    /// </summary>
    public CancellationToken UnloadToken
    {
        get
        {
            try
            {
                return _cancellationTokenSource.Token;
            }
            catch (ObjectDisposedException)
            {
                return new CancellationToken(true);
            }
        }
    }

    /// <summary>
    /// The current phase rotation.
    /// </summary>
    public IReadOnlyList<ILayoutPhase> Phases { get; }

    /// <summary>
    /// The currently active phase, or <see langword="null"/> if there are no active phases.
    /// </summary>
    public ILayoutPhase? ActivePhase => _activePhase == -1 ? null : Phases[_activePhase];

    /// <summary>
    /// The phase after the current phase, or <see langword="null"/> if we are on the last phase.
    /// </summary>
    public ILayoutPhase? NextPhase => _activePhase < -1 || _activePhase + 1 >= Phases.Count ? null : Phases[_activePhase + 1];
    
    /// <summary>
    /// The phase before the current phase, or <see langword="null"/> if we are on the first phase.
    /// </summary>
    public ILayoutPhase? PreviousPhase => _activePhase < 1 || _activePhase > Phases.Count ? null : Phases[_activePhase - 1];

    /// <summary>
    /// Create a new <see cref="Layout"/>.
    /// </summary>
    /// <remarks>For any classes overriding this class, any services injecting the layout must be initialized using the <see cref="IServiceProvider"/> in the constructor.</remarks>
    public Layout(ILifetimeScope serviceProvider, LayoutInfo layoutInfo, List<IDisposable> disposableConfigs)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        ServiceProvider = serviceProvider;
        LayoutInfo = layoutInfo;
        Logger = (ILogger<Layout>)serviceProvider.Resolve(typeof(ILogger<>).MakeGenericType(GetType()));
        
        _disposableVariationConfigurationRoots = disposableConfigs;
        PhaseList = new List<ILayoutPhase>();
        Phases = new ReadOnlyCollection<ILayoutPhase>(PhaseList);

        // this NEEDS to come before services are injected so they can inject this gamemode.
        serviceProvider.Resolve<WarfareModule>().SetActiveLayout(this);

        // inject services
        _factory = serviceProvider.Resolve<LayoutFactory>();
        _appLifetime = serviceProvider.Resolve<WarfareLifetimeComponent>();

        // listens for changes to the config file
        _configListener = ChangeToken.OnChange(
            LayoutConfiguration.GetReloadToken,
            () =>
            {
                UniTask.Create(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await ApplyLayoutConfigurationUpdateAsync(CancellationToken.None);
                });
            });
    }

    /// <summary>
    /// Sets the winner of the game and moves to the next phase.
    /// </summary>
    public UniTask TriggerVictoryAsync(Team winner)
    {
        Data[KnownLayoutDataKeys.WinnerTeam] = winner;
        return MoveToNextPhase(CancellationToken.None);
    }

    /// <summary>
    /// Invoked when a change is made to <see cref="LayoutConfiguration"/>.
    /// </summary>
    protected virtual UniTask ApplyLayoutConfigurationUpdateAsync(CancellationToken token = default) => default;

    /// <summary>
    /// Parse a phase from the phases section of the config file.
    /// </summary>
    /// <returns>The new phase, or <see langword="null"/> to not use the phase. An error will be logged.</returns>
    protected virtual UniTask<ILayoutPhase?> ReadPhaseAsync(Type phaseType, IConfigurationSection configSection, CancellationToken token = default)
    {
        IConfiguration variedConfigSection = configSection;
        _factory.ApplyVariation(ref variedConfigSection, Accessor.Formatter.Format(phaseType), LayoutInfo.FilePath);

        try
        {
            ILayoutPhase? phase = (ILayoutPhase?)ReflectionUtility.CreateInstanceFixed(ServiceProvider.Resolve<IServiceProvider>(), phaseType, [ variedConfigSection ]);

            try
            {
                variedConfigSection.Bind(phase);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unable to bind phase {0} of type {1}.", configSection.Path, phaseType);
                if (variedConfigSection is IDisposable d)
                    d.Dispose();
                return UniTask.FromResult<ILayoutPhase?>(null);
            }

            if (variedConfigSection is IDisposable disposable)
                _disposableVariationConfigurationRoots.Add(disposable);

            return UniTask.FromResult(phase);
        }
        catch
        {
            if (variedConfigSection is IDisposable d)
                d.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Invoked just before <see cref="BeginLayoutAsync"/> as a chance for values to be initialized from <see cref="LayoutConfiguration"/>.
    /// </summary>
    protected internal virtual async UniTask InitializeLayoutAsync(GameRecord stats, CancellationToken token
#if TELEMETRY
        , Activity? activity
#endif
    )
    {
        LayoutId = stats.GameId;
        LayoutStats = stats;

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Read team info",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await ReadTeamInfoAsync(token);
        }

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Initialize ITeamManager",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await TeamManager.InitializeAsync(ServiceProvider.Resolve<IServiceProvider>(), token);
        }

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Read phases",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await ReadPhasesAsync(token);
            CheckEmptyPhases();
        }

        foreach (ILayoutPhase phase in PhaseList)
        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                $"Initialize {Accessor.ExceptionFormatter.Format(phase.GetType())} phase",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await phase.InitializePhaseAsync(token);
        }

        WasStarted = true;
    }


    /// <summary>
    /// Invoked when the layout first begins.
    /// </summary>
    protected internal virtual async UniTask BeginLayoutAsync(CancellationToken token
#if TELEMETRY
        , Activity? activity
#endif
    )
    {
#if TELEMETRY
        {
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Host layout",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
            await _factory.HostLayoutAsync(this, token, tempActivity);
        }
#else
        await _factory.HostLayoutAsync(this, token);
#endif

        await UniTask.SwitchToMainThread(token);
        IsActive = true;

        CheckEmptyPhases();
        _activePhase = -1;

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Begin TeamManager",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif

            await TeamManager.BeginAsync(token);
        }

#if TELEMETRY
        {
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Begin first phase",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
            await MoveToNextPhase(token, tempActivity);
        }
#else
        await MoveToNextPhase(token);
#endif
    }

    private async UniTask InvokePhaseListenerAction(ILayoutPhase phase, bool end, CancellationToken token
#if TELEMETRY
        , Activity? parentActivity
#endif
    )
    {
#if TELEMETRY
        Activity.Current = null;
        using Activity? activity = _factory.ActivitySource.StartActivity(
            $"InvokePhaseListenerAction for phase {Accessor.ExceptionFormatter.Format(phase.GetType())} {(end ? "ending" : "starting")}.",
            ActivityKind.Internal,
            parentActivity?.Context ?? default
        );
#endif

        Type intxType = typeof(ILayoutPhaseListener<>).MakeGenericType(phase.GetType());

        // find all services assignable from ILayoutPhaseListener<phase.GetType()>
        IEnumerable<object> listeners;

        try
        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.CreateActivity(
                "Gather services.",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            listeners = ServiceProvider.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<IServiceWithType>()
                .Where(x => x.ServiceType.IsConstructedGenericType
                            && x.ServiceType.GetGenericTypeDefinition() == typeof(ILayoutPhaseListener<>)
                            && intxType.IsAssignableFrom(x.ServiceType))
                .Select(x => x.ServiceType)
                .Distinct()
                .SelectMany(x => (IEnumerable<object>)ServiceProvider.Resolve(typeof(IEnumerable<>).MakeGenericType(x)))
                .OrderByDescending(x => x.GetType().GetPriority());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error gathering phase listener services.");
            return;
        }

        foreach (object service in listeners)
        {
            Type type = service.GetType();
#if TELEMETRY
            using Activity? hostActivity = _factory.ActivitySource.CreateActivity(
                $"Invoke ILayoutPhaseListener for phase {Accessor.ExceptionFormatter.Format(type)}.",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            Type implIntxType = type.GetInterfaces().First(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ILayoutPhaseListener<>) && intxType.IsAssignableFrom(x));

            // invoke method from an unknown generic interface type
            MethodInfo implementation = implIntxType.GetMethod(
                end ? nameof(ILayoutPhaseListener<>.OnPhaseEnded)
                    : nameof(ILayoutPhaseListener<>.OnPhaseStarted),
                BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception("Unable to find phase listener method.");

            implementation = Accessor.GetImplementedMethod(type, implementation) ?? throw new Exception("Unable to find phase listener implemented method.");

            try
            {
                if (end)
                    Logger.LogDebug("Ending phase {0} for ILayoutPhaseListener {1}.", phase.GetType(), type);
                else
                    Logger.LogDebug("Starting phase {0} for ILayoutPhaseListener {1}.", phase.GetType(), type);
#if TELEMETRY
                hostActivity?.Start();
#endif
                await (UniTask)implementation.Invoke(service, [ phase, token ]);
#if TELEMETRY
                hostActivity?.Stop();
                hostActivity?.SetStatus(ActivityStatusCode.Ok);
#endif
            }
            catch (TargetInvocationException ex)
            {
#if TELEMETRY
                WarfareModule.RecordActivityException(hostActivity, ex.InnerException ?? ex);
#endif
                if (end)
                {
                    Logger.LogError(ex.InnerException, "Failed to end phase {0}. Listener {1} failed.", phase.GetType(), type);
                    await _factory.StartNextLayout(CancellationToken.None);
                    throw new OperationCanceledException();
                }

                Logger.LogWarning(ex.InnerException, "Error beginning phase {0}. Listener {1} failed.", phase.GetType(), type);
                throw new OperationCanceledException();
            }
        }
    }

    public virtual async UniTask MoveToNextPhase(CancellationToken token = default
#if TELEMETRY
        , Activity? activity = null
#endif
    )
    {
        // keep moving to the next phase until one is activated by BeginPhase.
        ILayoutPhase? newPhase;
        do
        {
            await UniTask.SwitchToMainThread(token);

            try
            {
                // test to see if SP is disposed
                ServiceProvider.Resolve<Layout>();
            }
            catch (ObjectDisposedException)
            {
                Logger.LogCritical("MoveToNextPhase called after layout was disposed. Stack trace below." + Environment.NewLine + new StackTrace());
                return;
            }

            bool isEnd = _activePhase >= Phases.Count - 1;

            ILayoutPhase? oldPhase = ActivePhase;
            int oldPhaseIndex = _activePhase;

            if (!isEnd)
                _activePhase = Math.Max(0, _activePhase + 1);

            newPhase = isEnd ? null : Phases[_activePhase];

            if (oldPhase != null)
            {
                Type oldPhaseType = oldPhase.GetType();
                Logger.LogDebug("Ending phase: {0}.", oldPhaseType);
                try
                {
#if TELEMETRY
                    using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                        $"End phase {Accessor.ExceptionFormatter.Format(oldPhase.GetType())}",
                        ActivityKind.Internal,
                        activity?.Context ?? default
                    );
#endif
                    await oldPhase.EndPhaseAsync(token);
                }
                catch (Exception ex)
                {
                    if (oldPhase.IsActive)
                    {
                        Logger.LogError(ex, "Error ending phase {0}.", oldPhaseType);
                        await _factory.StartNextLayout(CancellationToken.None);
                        throw new OperationCanceledException();
                    }

                    Logger.LogWarning(ex, "Error ending phase {0}.", oldPhaseType);
                }

                if (oldPhase.IsActive)
                {
                    Logger.LogError("Failed to end phase {0}.", oldPhaseType);
                    await _factory.StartNextLayout(CancellationToken.None);
                    throw new OperationCanceledException();
                }


                await InvokePhaseListenerAction(oldPhase, end: true, CancellationToken.None
#if TELEMETRY
                    , activity
#endif
                );
                
                await DisposePhase(oldPhaseIndex);
                await UniTask.SwitchToMainThread(CancellationToken.None);
            }

            if (isEnd)
            {
                Logger.LogDebug("Ending layout: {0}.", LayoutInfo.FilePath);
                await _factory.StartNextLayout(CancellationToken.None
    #if TELEMETRY
                    , activity
    #endif
                );
                throw new OperationCanceledException();
            }

            Logger.LogDebug("Starting next phase: {0}.", newPhase!.GetType());

            try
            {
#if TELEMETRY
                using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                    $"Begin phase {Accessor.ExceptionFormatter.Format(newPhase.GetType())}",
                    ActivityKind.Internal,
                    activity?.Context ?? default
                );
#endif
                await newPhase.BeginPhaseAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (!newPhase.IsActive)
                {
                    Logger.LogError(ex, "Error beginning phase {0}.", newPhase.GetType());
                    await _factory.StartNextLayout(token);
                    throw new OperationCanceledException();
                }

                Logger.LogWarning(ex, "Error beginning phase {0}.", newPhase.GetType());
            }

            if (!newPhase.IsActive)
            {
                Logger.LogWarning("Skipping phase {0} because it didn't activate.", newPhase.GetType());
            }

            await InvokePhaseListenerAction(newPhase, end: false, CancellationToken.None
#if TELEMETRY
                , activity
#endif
            );
        }
        while (!newPhase.IsActive);
    }

    private void CheckEmptyPhases()
    {
        if (PhaseList.Count != 0)
            return;

        Logger.LogWarning("No phases available in layout {0}. Adding a null phase that will end the game instantly.", LayoutInfo.DisplayName);
        PhaseList.Add(new NullPhase(ConfigurationHelper.EmptySection));
    }

    /// <summary>
    /// Invoked when the layout ends. Try not to use <see cref="UniTask.SwitchToMainThread"/> here.
    /// </summary>
    protected internal virtual async UniTask EndLayoutAsync(CancellationToken token
#if TELEMETRY
        , Activity? activity = null
#endif
    )
    {
        _configListener.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            Logger.LogError(ex, "Error(s) while canceling layout cancellation token source in layout {0}.", LayoutInfo.DisplayName);
        }
        catch (ObjectDisposedException)
        {
            return;
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }

        await UniTask.SwitchToMainThread(token);

        if (_activePhase >= 0 && _activePhase < PhaseList.Count)
        {
            ILayoutPhase phase = PhaseList[_activePhase];
            try
            {
#if TELEMETRY
                using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                    $"End phase {Accessor.ExceptionFormatter.Format(phase.GetType())} abruptly",
                    ActivityKind.Internal,
                    activity?.Context ?? default
                );
#endif
                await phase.EndPhaseAsync(token);

                if (phase.IsActive)
                {
                    Logger.LogError($"Failed to end phase {phase.GetType()}.");
                }
            }
            catch (Exception ex)
            {
                if (phase.IsActive)
                {
                    Logger.LogError(ex, $"Error ending phase {phase.GetType()}.");
                }
                else
                {
                    Logger.LogWarning(ex, $"Error ending phase {phase.GetType()}.");
                }
            }
        }

        for (int i = 0; i < PhaseList.Count; i++)
        {
            await DisposePhase(i);
        }

        if (!IsActive)
            return;

        IsActive = false;

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "End ITeamManager",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await TeamManager.EndAsync(token);
        }

#if TELEMETRY
        {
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Unhost",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
            await _factory.UnhostLayoutAsync(this, token, tempActivity);
        }
#else
        await _factory.UnhostLayoutAsync(this, token);
#endif

        (LayoutInfo.Layout as IDisposable)?.Dispose();

        {
#if TELEMETRY
            using Activity? tempActivity = _factory.ActivitySource.StartActivity(
                "Update homebase",
                ActivityKind.Internal,
                activity?.Context ?? default
            );
#endif
            await _appLifetime.NotifyLayoutEnding(CancellationToken.None);
        }
    }

    /// <summary>
    /// Read information about which <see cref="ITeamManager{TTeam}"/> to use and create it.
    /// </summary>
    protected virtual UniTask ReadTeamInfoAsync(CancellationToken token = default)
    {
        IConfigurationSection teamSection = LayoutConfiguration.GetSection("Teams");
        if (!teamSection.GetChildren().Any())
        {
            Logger.LogInformation("Team section is not present in layout \"{0}\", assuming no teams should be loaded.", LayoutInfo.DisplayName);
            TeamManager = new NullTeamManager();
            return UniTask.CompletedTask;
        }

        // read the full type name from the config file
        string? managerTypeName = teamSection["ManagerType"];
        if (managerTypeName == null)
        {
            Logger.LogError("Team section is missing the \"ManagerType\" config value in layout \"{0}\".", LayoutInfo.DisplayName);
            TeamManager = new NullTeamManager();
            return UniTask.CompletedTask;
        }

        Type? managerType = ContextualTypeResolver.ResolveType(managerTypeName, typeof(ITeamManager<Team>));
        if (managerType == null || managerType.IsAbstract || !typeof(ITeamManager<Team>).IsAssignableFrom(managerType))
        {
            Logger.LogError("Unknown team manager type in layout \"{0}\": \"{1}\".", LayoutInfo.DisplayName, managerTypeName);
            TeamManager = new NullTeamManager();
            return UniTask.CompletedTask;
        }

        ITeamManager<Team> manager = (ITeamManager<Team>)ReflectionUtility.CreateInstanceFixed(ServiceProvider.Resolve<IServiceProvider>(), managerType, [ teamSection ]);
        
        manager.Configuration = teamSection;
        teamSection.Bind(manager);
        TeamManager = manager;
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Read layout phases by calling <see cref="ReadPhaseAsync"/> for each phase in the <c>phases</c> config section.
    /// </summary>
    private async UniTask ReadPhasesAsync(CancellationToken token = default)
    {
        IConfigurationSection phaseSection = LayoutConfiguration.GetSection("Phases");

        int index = -1;
        foreach (IConfigurationSection phaseConfig in phaseSection.GetChildren())
        {
            ++index;

            // read the full type name from the config file
            string? phaseTypeName = phaseConfig["Type"];
            if (phaseTypeName == null)
            {
                Logger.LogError("Phase at index {0} is missing the \"Type\" config value in layout \"{1}\" and will be skipped.", index, LayoutInfo.DisplayName);
                continue;
            }

            Type? phaseType = ContextualTypeResolver.ResolveType(phaseTypeName, typeof(ILayoutPhase));
            if (phaseType == null || phaseType.IsAbstract || !typeof(ILayoutPhase).IsAssignableFrom(phaseType))
            {
                Logger.LogError("Unknown type in phase at index {0} in layout \"{1}\" and will be skipped.", index, LayoutInfo.DisplayName);
                continue;
            }

            ILayoutPhase? layoutPhase = await ReadPhaseAsync(phaseType, phaseConfig, token);

            if (layoutPhase != null)
            {
                PhaseList.Add(layoutPhase);
            }
            else
            {
                Logger.LogWarning("Failed to read phase at index {0} in layout \"{1}\".", index, LayoutInfo.DisplayName);
            }
        }

        _phaseDisposedMask = new BitArray(PhaseList.Count);
    }

    private async UniTask DisposePhase(int index)
    {
        if (index < 0 || index >= PhaseList.Count)
        {
            return;
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (_phaseDisposedMask != null && _phaseDisposedMask[index])
        {
            // already disposed
            return;
        }

        ILayoutPhase phase = PhaseList[index];
        _phaseDisposedMask ??= new BitArray(PhaseList.Count);
        _phaseDisposedMask[index] = true;
        try
        {
            if (phase is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (phase is IDisposable disp)
            {
                disp.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error disposing phase {phase.GetType()}.");
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return LayoutInfo.DisplayName;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable disposable in _disposableVariationConfigurationRoots)
        {
            disposable.Dispose();
        }

        LayoutInfo.Dispose();
        _configListener.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException) { }
        catch (AggregateException ex)
        {
            Logger.LogError(ex, "Error(s) while disposing layout cancellation token source in layout \"{0}\".", LayoutInfo.DisplayName);
        }

        _cancellationTokenSource.Dispose();
    }
}