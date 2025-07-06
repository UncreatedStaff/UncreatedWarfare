using Autofac.Core;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    protected internal virtual async UniTask InitializeLayoutAsync(GameRecord stats, CancellationToken token = default)
    {
        LayoutId = stats.GameId;
        LayoutStats = stats;

        await ReadTeamInfoAsync(token);

        await ReadPhasesAsync(token);

        CheckEmptyPhases();

        await TeamManager.InitializeAsync(token);

        foreach (ILayoutPhase phase in PhaseList)
        {
            await phase.InitializePhaseAsync(token);
        }

        WasStarted = true;
    }


    /// <summary>
    /// Invoked when the layout first begins.
    /// </summary>
    protected internal virtual async UniTask BeginLayoutAsync(CancellationToken token = default)
    {
        await _factory.HostLayoutAsync(this, token);

        await UniTask.SwitchToMainThread(token);
        IsActive = true;

        CheckEmptyPhases();
        _activePhase = -1;

        await TeamManager.BeginAsync(token);

        await MoveToNextPhase(token);
    }

    private async UniTask InvokePhaseListenerAction(ILayoutPhase phase, bool end, CancellationToken token)
    {
        Type intxType = typeof(ILayoutPhaseListener<>).MakeGenericType(phase.GetType());

        // find all services assignable from ILayoutPhaseListener<phase.GetType()>
        IEnumerable<object> listeners;

        try
        {
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
            Type implIntxType = type.GetInterfaces().First(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ILayoutPhaseListener<>) && intxType.IsAssignableFrom(x));

            // invoke method from an unknown generic interface type
            MethodInfo implementation = implIntxType.GetMethod(
                end ? nameof(ILayoutPhaseListener<PreparationPhase>.OnPhaseEnded)
                    : nameof(ILayoutPhaseListener<PreparationPhase>.OnPhaseStarted),
                BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception("Unable to find phase listener method.");

            implementation = Accessor.GetImplementedMethod(type, implementation) ?? throw new Exception("Unable to find phase listener implemented method.");

            try
            {
                if (end)
                    Logger.LogDebug("Ending phase {0} for ILayoutPhaseListener {1}.", phase.GetType(), type);
                else
                    Logger.LogDebug("Starting phase {0} for ILayoutPhaseListener {1}.", phase.GetType(), type);
                await (UniTask)implementation.Invoke(service, [ phase, token ]);
            }
            catch (TargetInvocationException ex)
            {
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

    public virtual async UniTask MoveToNextPhase(CancellationToken token = default)
    {
        // keep moving to the next phase until one is activated by BeginPhase.
        ILayoutPhase? newPhase;
        do
        {
            await UniTask.SwitchToMainThread(token);

            bool isEnd = _activePhase >= Phases.Count - 1;

            ILayoutPhase? oldPhase = ActivePhase;

            if (!isEnd)
                _activePhase = Math.Max(0, _activePhase + 1);

            newPhase = isEnd ? null : Phases[_activePhase];

            if (oldPhase != null)
            {
                Type oldPhaseType = oldPhase.GetType();
                Logger.LogDebug("Ending phase: {0}.", oldPhaseType);
                try
                {
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

                await InvokePhaseListenerAction(oldPhase, end: true, CancellationToken.None);

                await UniTask.SwitchToMainThread(CancellationToken.None);
                try
                {
                    if (oldPhase is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                        await UniTask.SwitchToMainThread(CancellationToken.None);
                    }
                    else if (oldPhase is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                    Logger.LogError(ex, "Error disposing phase {0}.", oldPhase);
                }
            }

            if (isEnd)
            {
                Logger.LogDebug("Ending layout: {0}.", LayoutInfo.FilePath);
                await _factory.StartNextLayout(CancellationToken.None);
                throw new OperationCanceledException();
            }

            Logger.LogDebug("Starting next phase: {0}.", newPhase!.GetType());

            try
            {
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

            await InvokePhaseListenerAction(newPhase, end: false, CancellationToken.None);
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
    protected internal virtual async UniTask EndLayoutAsync(CancellationToken token = default)
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

        await UniTask.SwitchToMainThread(token);
        if (!IsActive)
            return;

        IsActive = false;

        await TeamManager.EndAsync(token);

        await _factory.UnhostLayoutAsync(this, token);

        (LayoutInfo.Layout as IDisposable)?.Dispose();

        await _appLifetime.NotifyLayoutEnding(CancellationToken.None);
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