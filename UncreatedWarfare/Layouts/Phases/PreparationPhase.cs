using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Handles the preparation or staging phase and the UI countdown for it.
/// </summary>
public class PreparationPhase : BasePhase<PhaseTeamSettings>, IDisposable
{
    private readonly Layout _session;
    private readonly ILoopTickerFactory _tickerFactory;
    private readonly StagingUI _stagingUi;
    private readonly ITranslationService _translationService;
    
    private ILoopTicker? _ticker;
    public PreparationPhase(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {
        _tickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _session = serviceProvider.GetRequiredService<Layout>();
        _stagingUi = serviceProvider.GetRequiredService<StagingUI>();
        _translationService = serviceProvider.GetRequiredService<ITranslationService>();
    }

    /// <inheritdoc />
    public override UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        StartBroadcastingStagingUI();

        return base.BeginPhaseAsync(token);
    }

    /// <inheritdoc />
    public override UniTask EndPhaseAsync(CancellationToken token = default)
    {
        _stagingUi.ClearFromAllPlayers();
        UnturnedUIDataSource.RemoveOwner(_stagingUi);
        _ticker?.Dispose();

        return base.EndPhaseAsync(token);
    }

    /// <summary>
    /// Send toast to all players.
    /// </summary>
    protected void StartBroadcastingStagingUI()
    {
        TranslationList? globalTranslationList = null;
        if (Name is { Count: > 0 })
        {
            globalTranslationList = Name;
        }
        else if (Teams == null)
        {
            globalTranslationList = new TranslationList(Path.GetFileNameWithoutExtension(Layout.LayoutInfo.FilePath));
        }
        else foreach (PhaseTeamSettings settings in Teams)
        {
            if (settings.Name is not { Count: > 0 } || settings.TeamInfo == null)
                continue;

            if (Duration.Ticks > 0)
                _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(settings.TeamInfo), settings.Name, Duration);
            else
                _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(settings.TeamInfo), settings.Name);
        }

        if (globalTranslationList != null)
        {
            if (Duration.Ticks > 0)
                _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(), globalTranslationList, Duration);
            else
                _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(), globalTranslationList);
        }

        if (Duration.Ticks <= 0)
            return;

        // tick down the UI timer
        _ticker = _tickerFactory.CreateTicker(TimeSpan.FromSeconds(1d), invokeImmediately: false, state: (this, Name: globalTranslationList), queueOnGameThread: true, static (ticker, timeSinceStart, _) =>
        {
            (PreparationPhase phase, TranslationList? globalTranslationList) = ticker.State;

            bool isCompleted = timeSinceStart >= phase.Duration;

            TimeSpan timeLeft = isCompleted ? TimeSpan.Zero : phase.Duration - timeSinceStart;

            if (globalTranslationList != null)
            {
                phase._stagingUi.UpdateForAll(phase._translationService.SetOf.PlayersOnTeam(), globalTranslationList, timeLeft);
            }
            else foreach (PhaseTeamSettings settings in phase.Teams!)
            {
                if (settings.Name is not { Count: > 0 } || settings.TeamInfo == null)
                    continue;

                phase._stagingUi.UpdateForAll(phase._translationService.SetOf.PlayersOnTeam(settings.TeamInfo), settings.Name, timeLeft);
            }

            if (!isCompleted)
                return;
            
            phase._ticker?.Dispose();
            phase._ticker = null;
            PreparationPhase phase2 = phase;
            UniTask.Create(() => phase2._session.MoveToNextPhase(CancellationToken.None));
        });
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
    }
}