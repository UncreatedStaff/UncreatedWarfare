using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Handles showing the leaderboard.
/// </summary>
public class LeaderboardPhase : BasePhase<PhaseTeamSettings>, IDisposable
{
    private readonly Layout _session;
    private readonly ILoopTickerFactory _tickerFactory;

    private ILoopTicker? _ticker;
    public LeaderboardPhase(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {
        _tickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _session = serviceProvider.GetRequiredService<Layout>();
    }

    /// <inheritdoc />
    public override async UniTask BeginPhaseAsync(object[] dataFromPreviousPhase, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (Duration.Ticks <= 0)
            Duration = TimeSpan.FromSeconds(30d);

        // todo show leaderboard and count-down timer
        _ticker = _tickerFactory.CreateTicker(Duration, invokeImmediately: false, queueOnGameThread: true, (_, _, _) =>
        {
            _ticker?.Dispose();
            _ticker = null;
            UniTask.Create(() => _session.MoveToNextPhase(CancellationToken.None));
        });

        await base.BeginPhaseAsync(dataFromPreviousPhase, token);
    }

    /// <inheritdoc />
    public override async UniTask EndPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _ticker?.Dispose();

        await base.EndPhaseAsync(token);
    }

    public void Dispose()
    {
        _ticker?.Dispose();
        _ticker = null;
    }
}