using DanielWillett.ReflectionTools.Emit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Layouts.UI.Leaderboards;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Stats.Leaderboard;
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
    private IDisposable? _statsFile;

    [UsedImplicitly]
    public LeaderboardPhaseStatInfo[] PlayerStats { get; set; } = Array.Empty<LeaderboardPhaseStatInfo>();

    [UsedImplicitly]
    public ValuablePlayerInfo[] ValuablePlayers { get; set; } = Array.Empty<ValuablePlayerInfo>();

    [UsedImplicitly]
    public string? PlayerStatsPath { get; set; }

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
        IConfiguration valuablePlayersConfig = Configuration;
        // read from file if specified
        if (PlayerStatsPath is { Length: > 0 } path)
        {
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(path, Path.GetDirectoryName(_session.LayoutInfo.FilePath));

            IConfigurationRoot statsConfig = new ConfigurationBuilder()
                .AddYamlFile(path, optional: false, reloadOnChange: false)
                .Build();

            if (statsConfig is IDisposable disp)
                _statsFile = disp;

            PlayerStats = statsConfig.GetSection("Stats").Get<LeaderboardPhaseStatInfo[]>()!;
            ValuablePlayers = statsConfig.GetSection("ValuablePlayers").Get<ValuablePlayerInfo[]>()!;
            valuablePlayersConfig = statsConfig;
        }

        PlayerStats ??= Array.Empty<LeaderboardPhaseStatInfo>();
        ValuablePlayers ??= Array.Empty<ValuablePlayerInfo>();
        for (int i = 0; i < ValuablePlayers.Length; ++i)
        {
            ValuablePlayerInfo valuablePlayer = ValuablePlayers[i];
            valuablePlayer.Configuration = valuablePlayersConfig.GetSection($"ValuablePlayers:{i}");

            if (!string.IsNullOrWhiteSpace(valuablePlayer.Name))
                continue;

            Logger.LogWarning("Missing 'Name' in Valuable Player #{0}", i);
            valuablePlayer.Name = $"vp-{i}";
        }

        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo stat = PlayerStats[i];
            stat.Index = i;

            if (!string.IsNullOrWhiteSpace(stat.Name))
            {
                if (stat.DisplayName is not { Count: > 0 })
                    (stat.DisplayName ??= new TranslationList()).Add((LanguageInfo?)null, stat.Name);

                continue;
            }

            Logger.LogWarning("Missing 'Name' in Stat #{0}", i);
            stat.Name = $"stat-{i.ToString(CultureInfo.InvariantCulture)}";
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

        _leaderboardUi.Open(CreateLeaderboardSets(), this);

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
        Interlocked.Exchange(ref _statsFile, null)?.Dispose();
    }

    public virtual LeaderboardSet[] CreateLeaderboardSets()
    {
        for (int i = 0; i < _players.Length; ++i)
        {
            List<LeaderboardPlayer> players = _players[i];
            foreach (LeaderboardPlayer player in players)
            {
                for (int j = 0; j < _players.Length; ++j)
                {
                    if (j == i)
                        continue;

                    List<LeaderboardPlayer> otherPlayers = _players[j];
                    int olderPlayer = otherPlayers.FindIndex(x => x.Player.Equals(player) && x.LastJoinedTeam <= player.LastJoinedTeam);
                    if (olderPlayer >= 0)
                        otherPlayers.RemoveAt(olderPlayer);
                }
            }
        }

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
            if (!pl.Player.Equals(player))
                continue;

            pl.LastJoinedTeam = Time.realtimeSinceStartup;
            return;
        }

        players.Add(new LeaderboardPlayer(player, team));
        Logger.LogDebug("Created leaderboard player for {0} on team {1}.", player, team);
    }
}

public class LeaderboardPhaseStatInfo
{
    internal int Index;
    internal RewardExpression? CachedExpression;

    /// <summary>
    /// Standard or custom .NET number format to convert the <see cref="double"/> value to a <see cref="string"/>.
    /// </summary>
    public string? NumberFormat { get; set; }

    /// <summary>
    /// Internal name of the stat. Also defaults to the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the stat.
    /// </summary>
    public TranslationList? DisplayName { get; set; }

    /// <summary>
    /// Name to use as the variable name for expressions using this stat.
    /// </summary>
    public string? FormulaName { get; set; }

    /// <summary>
    /// If the stat is showed in the leaderboard per-player.
    /// </summary>
    public bool IsLeaderboardColumn { get; set; }

    /// <summary>
    /// Header for the leaderboard per-player column.
    /// </summary>
    public string? ColumnHeader { get; set; }

    /// <summary>
    /// If the stat is shown as a sum of all players stats under the game stats section.
    /// </summary>
    public bool IsGlobalStat { get; set; }

    /// <summary>
    /// Evaluatable expression to calculate the stat from other stats.
    /// </summary>
    public string? Expression { get; set; }
}