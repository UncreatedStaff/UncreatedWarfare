using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using Uncreated.Warfare.Gamemodes.Flags.UI;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Gamemodes.Phases;

/// <summary>
/// Handles the preparation or staging phase and the UI countdown for it.
/// </summary>
public class PreparationPhase : BasePhase<PhaseTeamSettings>, IDisposable
{
    protected static readonly StagingUI StagingUI = new StagingUI();

    private readonly Layout _session;
    private readonly ILoopTickerFactory _tickerFactory;
    
    private ILoopTicker? _ticker;
    public PreparationPhase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _tickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _session = serviceProvider.GetRequiredService<Layout>();
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

        StagingUI.ClearFromAllPlayers();
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
            if (Duration.HasValue)
                StagingUI.SendToAll(Name, Duration.Value);
            else
                StagingUI.SendToAll(Name);
        }
        else if (Teams != null)
        {
            for (int i = 0; i < Teams.Length; i++)
            {
                PhaseTeamSettings settings = Teams[i];
                if (settings.Name is not { Count: > 0 } || settings.TeamInfo == null)
                    continue;

                if (Duration.HasValue)
                    StagingUI.SendToAll(LanguageSet.OnTeam((ulong)settings.TeamInfo.Id), settings.Name, Duration.Value);
                else
                    StagingUI.SendToAll(LanguageSet.OnTeam((ulong)settings.TeamInfo.Id), settings.Name);
            }
        }

        if (!Duration.HasValue)
            return;

        // tick down the UI timer
        _ticker = _tickerFactory.CreateTicker(TimeSpan.FromSeconds(1d), invokeImmediately: false, queueOnGameThread: true, (_, timeSinceStart, _) =>
        {
            if (timeSinceStart >= Duration.Value)
            {
                StagingUI.UpdateForAll(TimeSpan.Zero);
                UniTask.Create(() => _session.MoveToNextPhase(CancellationToken.None));
            }
            else
            {
                StagingUI.UpdateForAll(Duration.Value - timeSinceStart);
            }
        });
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
    }
}