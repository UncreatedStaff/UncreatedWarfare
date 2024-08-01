using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Layouts;

/// <summary>
/// Lasts for one game. Responsible for loading layouts.
/// </summary>
public class Layout : IDisposable, IEventListenerProvider
{
    private int _activePhase = -1;
    private readonly LayoutInfo _sessionInfo;
    private readonly IDisposable _configListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    internal bool UnloadedHostedServices;
    private readonly IList<IDisposable> _disposableVariationConfigurationRoots;

    protected ILogger<Layout> Logger;

    private readonly LayoutFactory _factory;

    /// <summary>
    /// If the session is currently running.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Exposed to allow sub-classes to manually add phases if needed.
    /// </summary>
    protected IList<ILayoutPhase> PhaseList { get; }

    /// <summary>
    /// Configuration that defines how the layout of the game is loaded.
    /// </summary>
    public IConfigurationRoot LayoutConfiguration => _sessionInfo.Layout;

    /// <summary>
    /// Scoped service provider for this session.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Keeps track of all teams.
    /// </summary>
    public ITeamManager<Team> TeamManager { get; protected set; }

    /// <summary>
    /// Token that will cancel when the session ends.
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
    /// Create a new <see cref="Layout"/>.
    /// </summary>
    /// <remarks>For any classes overriding this class, any services injecting the session must be initialized using the <see cref="IServiceProvider"/> in the constructor.</remarks>
    public Layout(IServiceProvider serviceProvider, LayoutInfo sessionInfo)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        ServiceProvider = serviceProvider;
        _sessionInfo = sessionInfo;

        _disposableVariationConfigurationRoots = new List<IDisposable>();
        PhaseList = new List<ILayoutPhase>();
        Phases = new ReadOnlyCollection<ILayoutPhase>(PhaseList);

        // this NEEDS to come before services are injected so they can inject this gamemode.
        serviceProvider.GetRequiredService<WarfareModule>().SetActiveLayout(this);

        // inject services
        _factory = ServiceProvider.GetRequiredService<LayoutFactory>();
        Logger = (ILogger<Layout>)ServiceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));

        // listens for changes to the config file
        _configListener = LayoutConfiguration.GetReloadToken().RegisterChangeCallback(_ =>
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                await ApplyLayoutConfigurationUpdateAsync(CancellationToken.None);
            });
        }, null);
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
        ILayoutPhase? phase;
        List<PhaseVariationInfo>? variations = ReadPhaseVariations(phaseType, configSection);

        if (variations is not { Count: > 0 })
        {
            phase = (ILayoutPhase?)ActivatorUtilities.CreateInstance(ServiceProvider, phaseType, configSection);

            if (phase != null)
            {
                configSection.Bind(phase);
            }

            return UniTask.FromResult(phase);
        }


        int index = RandomUtility.GetIndex(variations, info => info.Weight);
        PhaseVariationInfo info = variations[index];

        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        
        IConfigurationRoot root = configBuilder
                                    .AddYamlFile(info.FileName, true, true)
                                    .Add(new ChainedConfigurationSource { Configuration = configSection, ShouldDisposeConfiguration = false })
                                    .Build();

        if (root is IDisposable disposable)
            _disposableVariationConfigurationRoots.Add(disposable);

        try
        {
            phase = (ILayoutPhase?)ActivatorUtilities.CreateInstance(ServiceProvider, phaseType, root);

            if (phase != null)
            {
                root.Bind(phase);
            }

            return UniTask.FromResult(phase);
        }
        catch
        {
            if (root is not IDisposable d)
                throw;

            d.Dispose();
            _disposableVariationConfigurationRoots.Remove(d);
            throw;
        }
    }

    protected virtual List<PhaseVariationInfo>? ReadPhaseVariations(Type phaseType, IConfigurationSection configSection)
    {
        string? variationsPath = configSection["Variations"];
        if (string.IsNullOrEmpty(variationsPath))
            return null;

        if (!Path.IsPathRooted(variationsPath))
        {
            string? fileDir = Path.GetDirectoryName(_sessionInfo.FilePath);
            if (!string.IsNullOrEmpty(fileDir))
                variationsPath = Path.Join(fileDir, variationsPath);
        }

        if (!Directory.Exists(variationsPath))
        {
            Logger.LogWarning("Variation directory {0} does not exist for phase {1}.", variationsPath, Accessor.Formatter.Format(phaseType));
            return null;
        }

        // find variation files
        List<PhaseVariationInfo> variationFiles = new List<PhaseVariationInfo>();
        foreach (string variationFile in Directory.GetFiles(variationsPath, "*.yml", SearchOption.TopDirectoryOnly))
        {
            if (!YamlUtility.CheckMatchesMapFilterAndReadWeight(variationFile, out double weight))
                continue;

            PhaseVariationInfo variation = default;
            variation.Weight = weight;
            variation.FileName = variationFile;

            variationFiles.Add(variation);
        }

        return variationFiles;
    }

    /// <summary>
    /// Invoked just before <see cref="BeginSessionAsync"/> as a chance for values to be initialized from <see cref="LayoutConfiguration"/>.
    /// </summary>
    protected internal virtual async UniTask InitializeSessionAsync(CancellationToken token = default)
    {
        await ReadTeamInfoAsync(token);

        await ReadPhasesAsync(token);

        CheckEmptyPhases();

        await TeamManager.InitializeAsync(token);

        foreach (ILayoutPhase phase in PhaseList)
        {
            await phase.InitializePhaseAsync(token);
        }
    }


    /// <summary>
    /// Invoked when the session first begins.
    /// </summary>
    protected internal virtual async UniTask BeginSessionAsync(CancellationToken token = default)
    {
        await _factory.HostSessionAsync(this, token);

        IsActive = true;

        CheckEmptyPhases();
        _activePhase = -1;
        await MoveToNextPhase(token);
    }

    public virtual async UniTask<bool> MoveToNextPhase(CancellationToken token = default)
    {
        // keep moving to the next phase until one is activated by BeginPhase.
        ILayoutPhase newPhase;
        do
        {
            await UniTask.SwitchToMainThread(token);

            if (_activePhase >= Phases.Count - 1)
            {
                await _factory.StartNextLayout(CancellationToken.None);
                throw new OperationCanceledException();
            }

            ILayoutPhase? oldPhase = ActivePhase;

            _activePhase = Math.Max(0, _activePhase + 1);

            newPhase = Phases[_activePhase];

            if (oldPhase != null)
            {
                string phaseTypeFormat = Accessor.Formatter.Format(oldPhase.GetType());
                Logger.LogDebug("Ending phase: {0}.", phaseTypeFormat);
                await oldPhase.EndPhaseAsync(token);
                if (oldPhase.IsActive)
                {
                    Logger.LogError("Failed to end phase {0}.", phaseTypeFormat);
                    return false;
                }

                await UniTask.SwitchToMainThread(token);
                try
                {
                    if (oldPhase is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                        await UniTask.SwitchToMainThread(token);
                    }
                    else if (oldPhase is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    await UniTask.SwitchToMainThread(token);
                    Logger.LogError(ex, "Error disposing phase {0}.", phaseTypeFormat);
                }
            }

            Logger.LogDebug("Starting next phase: {0}.", Accessor.Formatter.Format(newPhase.GetType()));
            await newPhase.BeginPhaseAsync(CancellationToken.None);
        }
        while (!newPhase.IsActive);

        return true;
    }

    private void CheckEmptyPhases()
    {
        if (PhaseList.Count != 0)
            return;

        Logger.LogWarning("No phases available in layout {0}. Adding a null phase that will end the game instantly.", _sessionInfo.DisplayName);
        PhaseList.Add(ActivatorUtilities.CreateInstance<NullPhase>(ServiceProvider, ConfigurationHelper.EmptySection));
    }

    /// <summary>
    /// Invoked when the session ends. Try not to use <see cref="UniTask.SwitchToMainThread"/> here.
    /// </summary>
    protected internal virtual UniTask EndSessionAsync(CancellationToken token = default)
    {
        IsActive = false;

        _configListener.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            Logger.LogError(ex, "Error(s) while canceling layout cancellation token source in layout {0}.", _sessionInfo.DisplayName);
        }

        return _factory.UnhostSessionAsync(this, token);
    }

    /// <summary>
    /// Read information about which <see cref="ITeamManager{TTeam}"/> to use and create it.
    /// </summary>
    protected virtual UniTask ReadTeamInfoAsync(CancellationToken token = default)
    {
        IConfigurationSection teamSection = LayoutConfiguration.GetSection("Teams");
        if (!teamSection.GetChildren().Any())
        {
            Logger.LogInformation("Team section is not present in layout \"{0}\", assuming no teams should be loaded.", _sessionInfo.DisplayName);
            TeamManager = ActivatorUtilities.CreateInstance<NullTeamManager>(ServiceProvider, ConfigurationHelper.EmptySection);
            return UniTask.CompletedTask;
        }

        // read the full type name from the config file
        string? managerTypeName = teamSection["ManagerType"];
        if (managerTypeName == null)
        {
            Logger.LogError("Team section is missing the \"ManagerType\" config value in layout \"{0}\".", _sessionInfo.DisplayName);
            TeamManager = ActivatorUtilities.CreateInstance<NullTeamManager>(ServiceProvider, ConfigurationHelper.EmptySection);
            return UniTask.CompletedTask;
        }

        Type? managerType = Type.GetType(managerTypeName) ?? Assembly.GetExecutingAssembly().GetType(managerTypeName);
        if (managerType == null || managerType.IsAbstract || !typeof(ILayoutPhase).IsAssignableFrom(managerType))
        {
            Logger.LogError("Unknown team manager type in layout \"{0}\".", _sessionInfo.DisplayName);
            TeamManager = ActivatorUtilities.CreateInstance<NullTeamManager>(ServiceProvider, ConfigurationHelper.EmptySection);
            return UniTask.CompletedTask;
        }

        ITeamManager<Team> manager = (ITeamManager<Team>)ActivatorUtilities.CreateInstance(ServiceProvider, managerType, teamSection);
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

        Assembly thisAsm = Assembly.GetExecutingAssembly();

        int index = -1;
        foreach (IConfigurationSection phaseConfig in phaseSection.GetChildren())
        {
            ++index;

            // read the full type name from the config file
            string? phaseTypeName = phaseConfig["Type"];
            if (phaseTypeName == null)
            {
                Logger.LogError("Phase at index {0} is missing the \"Type\" config value in layout \"{1}\" and will be skipped.", index, _sessionInfo.DisplayName);
                continue;
            }

            Type? phaseType = Type.GetType(phaseTypeName) ?? thisAsm.GetType(phaseTypeName);
            if (phaseType == null || phaseType.IsAbstract || !typeof(ILayoutPhase).IsAssignableFrom(phaseType))
            {
                Logger.LogError("Unknown type in phase at index {0} in layout \"{1}\" and will be skipped.", index, _sessionInfo.DisplayName);
                continue;
            }

            ILayoutPhase? layoutPhase = await ReadPhaseAsync(phaseType, phaseConfig, token);

            if (layoutPhase != null)
            {
                PhaseList.Add(layoutPhase);
            }
            else
            {
                Logger.LogWarning("Failed to read phase at index {0} in layout \"{1}\".", index, _sessionInfo.DisplayName);
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _sessionInfo.DisplayName;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable disposable in _disposableVariationConfigurationRoots)
        {
            disposable.Dispose();
        }

        _sessionInfo.Dispose();
        _configListener.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            Logger.LogError(ex, "Error(s) while disposing layout cancellation token source in layout \"{0}\".", _sessionInfo.DisplayName);
        }

        _cancellationTokenSource.Dispose();
    }

    // allows the current phase to handle events
    IEnumerable<IEventListener<TEventArgs>> IEventListenerProvider.EnumerateNormalListeners<TEventArgs>()
    {
        return ActivePhase is IEventListener<TEventArgs> phase
            ? Enumerable.Repeat(phase, 1)
            : Enumerable.Empty<IEventListener<TEventArgs>>();
    }

    IEnumerable<IAsyncEventListener<TEventArgs>> IEventListenerProvider.EnumerateAsyncListeners<TEventArgs>()
    {
        return ActivePhase is IAsyncEventListener<TEventArgs> phase
            ? Enumerable.Repeat(phase, 1)
            : Enumerable.Empty<IAsyncEventListener<TEventArgs>>();
    }
}