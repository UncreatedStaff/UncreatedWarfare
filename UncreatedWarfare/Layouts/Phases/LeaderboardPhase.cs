using DanielWillett.ReflectionTools.Emit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.UI.Leaderboards;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Handles showing the leaderboard and keeping up with stats.
/// </summary>
public class LeaderboardPhase : BasePhase<PhaseTeamSettings>, IDisposable, IEventListener<PlayerJoined>, IEventListener<PlayerTeamChanged>
{
    private readonly Layout _session;
    private readonly ILoopTickerFactory _tickerFactory;
    private readonly IPlayerService _playerService;
    private readonly ILeaderboardUI _leaderboardUi;
    private readonly List<LeaderboardPlayer>[] _players;

    [UsedImplicitly]
    public LeaderboardPhaseStatInfo[] PlayerStats { get; set; }

    private ILoopTicker? _ticker;

    public LeaderboardPhase(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {
        _tickerFactory = serviceProvider.GetRequiredService<ILoopTickerFactory>();
        _session = serviceProvider.GetRequiredService<Layout>();
        _leaderboardUi = serviceProvider.GetRequiredService<ILeaderboardUI>();

        _playerService = serviceProvider.GetRequiredService<IPlayerService>();

        _players = new List<LeaderboardPlayer>[TeamManager.AllTeams.Count];
    }

    /// <inheritdoc />
    public override UniTask InitializePhaseAsync(CancellationToken token = default)
    {
        PlayerStats ??= Array.Empty<LeaderboardPhaseStatInfo>();
        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo stat = PlayerStats[i];
            stat.Index = i;

            if (stat.Name != null)
                continue;

            Logger.LogWarning("Missing 'StatName' in Stat #{0}", i);
            stat.Name = $"<UNKNOWN {i.ToString(CultureInfo.InvariantCulture)}>";
        }

        // initialize calculated stat expressions
        RewardExpression.IEmittableVariable[]? tempVars = null;
        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo stat = PlayerStats[i];
            if (stat.Expression == null)
                continue;

            stat.CachedExpression = new RewardExpression($"Stat_{stat.Name}_{Layout.LayoutInfo.DisplayName}",
                typeof(double), typeof(double), typeof(LeaderboardSet.LeaderboardRow), typeof(LeaderboardPhase),
                tempVars ??= GetVariables(), stat.Expression, Logger);
        }

        // initialize LeaderboardPlayer objects for existing players
        for (int i = 0; i < _players.Length; ++i)
        {
            Team team = TeamManager.AllTeams[i];
            List<LeaderboardPlayer> list = new List<LeaderboardPlayer>(Provider.maxPlayers);
            foreach (WarfarePlayer player in _playerService.OnlinePlayersOnTeam(team))
            {
                list.Add(new LeaderboardPlayer(player, team));
                Logger.LogDebug("Created leaderboard player for {0} on team {1} during startup.", player, team);
            }

            _players[i] = list;
        }

        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public override async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (Duration.Ticks <= 0)
            Duration = TimeSpan.FromSeconds(30d);

        _leaderboardUi.Open(CreateLeaderboardSets());

        _ticker = _tickerFactory.CreateTicker(TimeSpan.FromSeconds(1d), invokeImmediately: true, queueOnGameThread: true, (_, timeSinceStart, _) =>
        {
            TimeSpan timeLeft = Duration - timeSinceStart;
            if (timeLeft.Ticks <= 0)
            {
                _leaderboardUi.Close();
                Dispose();
                UniTask.Create(() => _session.MoveToNextPhase(CancellationToken.None));
            }
            else
            {
                _leaderboardUi.UpdateCountdown(timeLeft);
            }
        });

        await base.BeginPhaseAsync(token);
    }

    /// <inheritdoc />
    public override async UniTask EndPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        _leaderboardUi.Close();
        Dispose();

        await base.EndPhaseAsync(token);
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _ticker, null)?.Dispose();
    }

    public virtual LeaderboardSet[] CreateLeaderboardSets()
    {
        LeaderboardSet[] set = new LeaderboardSet[TeamManager.AllTeams.Count];

        for (int i = 0; i < TeamManager.AllTeams.Count; ++i)
        {
            set[i] = new LeaderboardSet(CreateLeaderboardRow, PlayerStats, _players[i], TeamManager.AllTeams[i]);
        }

        return set;
    }

    private static void CreateLeaderboardRow(in LeaderboardSet.LeaderboardRow row, LeaderboardPhaseStatInfo[] visibleStats, Span<double> data)
    {
        double[] stats = row.Player.Player.Component<PlayerGameStatsComponent>().Stats;
        for (int i = 0; i < visibleStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo st = visibleStats[i];
            data[i] = st.CachedExpression != null ? (double)st.CachedExpression.TryEvaluate(row)! : stats[st.Index];
        }
    }

    public int GetStatIndex(string statName)
    {
        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            if (PlayerStats[i].Name.Equals(statName, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private RewardExpression.IEmittableVariable[] GetVariables()
    {
        RewardExpression.IEmittableVariable[] emittables = new RewardExpression.IEmittableVariable[PlayerStats.Length];
        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            emittables[i] = new ExpressionStatVariable(PlayerStats[i], i);
        }

        return emittables;
    }

    private class ExpressionStatVariable : RewardExpression.IEmittableVariable
    {
        private readonly int _index;
        public string[] Names { get; }
        public Type OutputType => typeof(double);

        public ExpressionStatVariable(LeaderboardPhaseStatInfo stat, int statIndex)
        {
            Names = stat.FormulaName != null ? [ stat.FormulaName, stat.Name ] : [ stat.Name ];
            _index = statIndex;
        }

        public void Preload(LocalReference local, IOpCodeEmitter emit, ILogger logger)
        {
            MethodInfo prop = typeof(LeaderboardSet.LeaderboardRow).GetProperty(nameof(LeaderboardSet.LeaderboardRow.Data), BindingFlags.Public | BindingFlags.Instance)
                                  ?.GetMethod ?? throw new InvalidOperationException("Failed to find LeaderboardSet.LeaderboardRow.Data.");

            MethodInfo getIndex = typeof(Span<double>).GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                  ?.GetMethod ?? throw new InvalidOperationException("Failed to find Span<double>.Item[int].");

            emit.LoadArgumentAddress(0)
                .LoadUnboxedAddress<LeaderboardSet.LeaderboardRow>()
                .Invoke(prop)
                .PopToLocal(typeof(Span<double>), out LocalBuilder lcl)
                .LoadLocalAddress(lcl)
                .LoadConstantInt32(_index)
                .Invoke(getIndex)
                .SetLocalValue(local);
        }
    }

    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        CheckPlayer(e.Player);
    }

    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        CheckPlayer(e.Player);
    }

    private void CheckPlayer(WarfarePlayer player)
    {
        GameThread.AssertCurrent();

        Team team = player.Team;
        if (!team.IsValid)
            return;

        int teamIndex = -1;
        IReadOnlyList<Team> allTeams = Layout.TeamManager.AllTeams;
        for (int i = 0; i < allTeams.Count; ++i)
        {
            if (allTeams[i] != team)
                continue;

            teamIndex = i;
            break;
        }

        if (teamIndex == -1)
            return;

        List<LeaderboardPlayer> players = _players[teamIndex];
        foreach (LeaderboardPlayer pl in players)
        {
            if (pl.Equals(player))
                return;
        }

        players.Add(new LeaderboardPlayer(player, team));
        Logger.LogDebug("Created leaderboard player for {0} on team {1}.", player, team);
    }
}

public class LeaderboardPhaseStatInfo
{
    internal int Index;
    internal RewardExpression CachedExpression;

    public string? NumberFormat { get; set; }

    public string Name { get; set; }
    public string? FormulaName { get; set; }

    public bool Visible { get; set; } = true;

    public string? Expression { get; set; }
}