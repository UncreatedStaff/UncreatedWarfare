using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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
    public override async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        StartBroadcastingStagingUI();

        await base.BeginPhaseAsync(token);
    }

    /// <inheritdoc />
    public override async UniTask EndPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _stagingUi.ClearFromAllPlayers();
        _ticker?.Dispose();

        await base.EndPhaseAsync(token);
    }

    /// <summary>
    /// Send toast to all players.
    /// </summary>
    protected void StartBroadcastingStagingUI()
    {
        if (Name is { Count: > 0 })
        {
            if (Duration.Ticks > 0)
                _stagingUi.SendToAll(_translationService.SetOf.AllPlayers(), Name, Duration);
            else
                _stagingUi.SendToAll(_translationService.SetOf.AllPlayers(), Name);
        }
        else if (Teams != null)
        {
            for (int i = 0; i < Teams.Count; i++)
            {
                PhaseTeamSettings settings = Teams[i];
                if (settings.Name is not { Count: > 0 } || settings.TeamInfo == null)
                    continue;

                if (Duration.Ticks > 0)
                    _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(settings.TeamInfo), settings.Name, Duration);
                else
                    _stagingUi.SendToAll(_translationService.SetOf.PlayersOnTeam(settings.TeamInfo), settings.Name);
            }
        }

        if (Duration.Ticks <= 0)
            return;

        // tick down the UI timer
        _ticker = _tickerFactory.CreateTicker(TimeSpan.FromSeconds(1d), invokeImmediately: false, queueOnGameThread: true, (_, timeSinceStart, _) =>
        {
            if (timeSinceStart >= Duration)
            {
                _stagingUi.UpdateForAll(_translationService.SetOf.AllPlayers(), TimeSpan.Zero);
                UniTask.Create(() => _session.MoveToNextPhase(CancellationToken.None));
            }
            else
            {
                _stagingUi.UpdateForAll(_translationService.SetOf.AllPlayers(), Duration - timeSinceStart);
            }
        });
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
    }
}