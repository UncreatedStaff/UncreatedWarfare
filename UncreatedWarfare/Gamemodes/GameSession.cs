using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Gamemodes.Layouts;

namespace Uncreated.Warfare.Gamemodes;

/// <summary>
/// Lasts for one game. Responsible for loading layouts.
/// </summary>
public class GameSession : IDisposable
{
    private int _activePhase = -1;
    private readonly GameSessionInfo _sessionInfo;
    private readonly IDisposable _configListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    internal bool UnloadedHostedServices;

    private readonly GameSessionFactory _factory;

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
    /// Create a new <see cref="GameSession"/>.
    /// </summary>
    /// <remarks>For any classes overriding this class, any services injecting the session must be initialized using the <see cref="IServiceProvider"/> in the constructor.</remarks>
    public GameSession(IServiceProvider serviceProvider, GameSessionInfo sessionInfo)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        ServiceProvider = serviceProvider;
        _sessionInfo = sessionInfo;

        PhaseList = new List<ILayoutPhase>();
        Phases = new ReadOnlyCollection<ILayoutPhase>(PhaseList);

        // this NEEDS to come before services are injected so they can inject this gamemode.
        serviceProvider.GetRequiredService<WarfareModule>().SetActiveGameSession(this);

        // inject services
        _factory = ServiceProvider.GetRequiredService<GameSessionFactory>();

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
    protected virtual UniTask<ILayoutPhase?> ReadPhase(Type phaseType, IConfigurationSection configSection)
    {
        ILayoutPhase? phase = (ILayoutPhase?)ActivatorUtilities.CreateInstance(ServiceProvider, phaseType);

        if (phase != null)
        {
            configSection.Bind(phase);
        }

        return UniTask.FromResult(phase);
    }

    /// <summary>
    /// Invoked just before <see cref="BeginSessionAsync"/> as a chance for values to be initialized from <see cref="LayoutConfiguration"/>.
    /// </summary>
    protected internal virtual async UniTask InitializeSession(CancellationToken token = default)
    {
        await ReadPhases();

        CheckEmptyPhases();
    }


    /// <summary>
    /// Invoked when the session first begins.
    /// </summary>
    protected internal virtual async UniTask BeginSessionAsync(CancellationToken token = default)
    {
        await _factory.HostSessionAsync(this, token);

        CheckEmptyPhases();
        _activePhase = -1;
        await MoveToNextPhase(token);

        IsActive = true;
    }

    protected virtual async UniTask MoveToNextPhase(CancellationToken token = default)
    {
        // keep moving to the next phase until one is activated by BeginPhase.
        ILayoutPhase newPhase;
        do
        {
            await UniTask.SwitchToMainThread(token);

            if (_activePhase >= Phases.Count - 1)
            {
                await _factory.StartNextGameSession(CancellationToken.None);
                throw new OperationCanceledException();
            }

            ILayoutPhase? oldPhase = ActivePhase;

            _activePhase = Math.Max(0, _activePhase + 1);

            newPhase = Phases[_activePhase];

            if (oldPhase != null)
            {
                L.LogDebug($"Ending phase: {Accessor.Formatter.Format(oldPhase.GetType())}.");
                await oldPhase.EndPhaseAsync(token);
                await UniTask.SwitchToMainThread(token);
            }

            L.LogDebug($"Starting next phase: {Accessor.Formatter.Format(newPhase.GetType())}.");
            await newPhase.BeginPhaseAsync(CancellationToken.None);
        }
        while (!newPhase.IsActive);
    }

    private void CheckEmptyPhases()
    {
        if (PhaseList.Count != 0)
            return;

        L.LogWarning($"No phases available in layout {_sessionInfo.DisplayName}. Adding a null phase that will end the game instantly.");
        PhaseList.Add(ActivatorUtilities.CreateInstance<NullPhase>(ServiceProvider));
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
            L.LogError($"Error(s) while canceling game session cancellation token source in layout {_sessionInfo.DisplayName}.");
            L.LogError(ex);
        }

        return _factory.UnhostSessionAsync(this, token);
    }

    /// <summary>
    /// Read layout phases by calling <see cref="ReadPhase"/> for each phase in the <c>phases</c> config section.
    /// </summary>
    private async UniTask ReadPhases()
    {
        IConfigurationSection phaseSection = LayoutConfiguration.GetSection("phases");

        Assembly thisAsm = Assembly.GetExecutingAssembly();

        int index = -1;
        foreach (IConfigurationSection phaseConfig in phaseSection.GetChildren())
        {
            ++index;

            // read the full type name from the config file
            string? phaseTypeName = phaseConfig["type"];
            if (phaseTypeName == null)
            {
                L.LogError($"Phase at index {index} is missing the \"type\" config value in layout \"{_sessionInfo.DisplayName}\" and will be skipped.");
                continue;
            }

            Type? phaseType = Type.GetType(phaseTypeName) ?? thisAsm.GetType(phaseTypeName);
            if (phaseType == null)
            {
                L.LogError($"Unknown type in phase at index {index} in layout \"{_sessionInfo.DisplayName}\" and will be skipped.");
                continue;
            }

            ILayoutPhase? layoutPhase = await ReadPhase(phaseType, phaseConfig);

            if (layoutPhase != null)
            {
                PhaseList.Add(layoutPhase);
            }
            else
            {
                L.LogWarning($"Failed to read phase at index {index} in layout \"{_sessionInfo.DisplayName}\".");
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
        _sessionInfo.Dispose();
        _configListener.Dispose();

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (AggregateException ex)
        {
            L.LogError("Error(s) while disposing game session cancellation token source.");
            L.LogError(ex);
        }

        _cancellationTokenSource.Dispose();
    }
}