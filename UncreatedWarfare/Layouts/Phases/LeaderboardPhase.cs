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
using Uncreated.Warfare.Players.UI;
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
    private readonly HudManager _hudManager;
    private readonly List<LeaderboardPlayer>[] _players;
    private IDisposable? _statsFile;
    private IDisposable? _hudHideHandle;

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
        _hudManager = serviceProvider.GetRequiredService<HudManager>();

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
                // interning the string allows us to use reference comparison before value comparison
                //   - interning a string is basically replacing the reference with the stored assembly reference
                //     (from the constant in KnownStatNames).
                string? internedString = string.IsInterned(stat.Name);
                if (internedString != null)
                    stat.Name = internedString;
                
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
                typeof(double), typeof(double), typeof(ILeaderboardRow), typeof(LeaderboardPhase),
                tempVars ??= GetVariables(), stat.Expression, Logger);
        }

        // extra validation/warnings
        bool hasDefault = false;
        for (int i = 0; i < PlayerStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo stat = PlayerStats[i];
            if (!string.IsNullOrWhiteSpace(stat.DefaultLeaderboardSort))
            {
                if (!stat.IsLeaderboardColumn)
                {
                    Logger.LogWarning("Stat #{0} ({1}) has DefaultLeaderboardSort set when IsLeaderboardColumn is false.", i, stat.Name);
                    stat.DefaultLeaderboardSort = null;
                }
                else if (hasDefault)
                {
                    Logger.LogWarning("Stat #{0} ({1}) has a duplicate DefaultLeaderboardSort set.", i, stat.Name);
                    stat.DefaultLeaderboardSort = null;
                }
                else if (stat.DefaultLeaderboardSort is not "Ascending" and not "Descending")
                {
                    Logger.LogWarning("Stat #{0} ({1}) has an invalid value for DefaultLeaderboardSort: {2}.", i, stat.Name, stat.DefaultLeaderboardSort);
                    stat.DefaultLeaderboardSort = null;
                }
                else
                {
                    hasDefault = true;
                }
            }
        }

        // initialize LeaderboardPlayer objects for existing players
        for (int i = 0; i < _players.Length; ++i)
        {
            _players[i] = new List<LeaderboardPlayer>(Provider.maxPlayers / Layout.TeamManager.AllTeams.Count + 6);
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            CheckPlayer(player);
        }

        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public override async UniTask BeginPhaseAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (Duration.Ticks <= 0)
            Duration = TimeSpan.FromSeconds(30d);

        _hudHideHandle = _hudManager.HideHud();

        // try statement prevents the game loop from getting stuck after the leaderboard
        try
        {
            _leaderboardUi.Open(CreateLeaderboardSets(), this);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening leaderboard.");

            // skip leaderboard
            Duration = TimeSpan.Zero;
        }

        _ticker = _tickerFactory.CreateTicker(TimeSpan.FromSeconds(1d), invokeImmediately: true, queueOnGameThread: true, (_, timeSinceStart, _) =>
        {
            TimeSpan timeLeft = Duration - timeSinceStart;
            if (timeLeft.Ticks <= 0)
            {
                try
                {
                    _leaderboardUi.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error closing leaderboard.");
                }
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
        Interlocked.Exchange(ref _hudHideHandle, null)?.Dispose();
    }

    public void AddToOfflineStat(int index, double amount, CSteamID steam64, Team team)
    {
        if (index < 0 || index >= PlayerStats.Length)
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

        if (teamIndex < 0)
            return;

        List<LeaderboardPlayer> players = _players[teamIndex];
        for (int i = 0; i < players.Count; ++i)
        {
            if (!players[i].Player.Equals(steam64))
                continue;

            players[i].Stats[index] += amount;
#if DEBUG
            //Logger.LogConditional("Leaderboard stat updated {0}: {1} -> {2} (+{3}) for player {4} on team {5}.", PlayerStats[index].Name, players[i].Stats[index] - amount, players[i].Stats[index], amount, players[i].Player, teamIndex);
#endif
            return;
        }
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
        ILeaderboardRow? rowBox = null;
        double[] stats = row.Player.Stats;
        for (int i = 0; i < visibleStats.Length; ++i)
        {
            LeaderboardPhaseStatInfo st = visibleStats[i];
            if (st.CachedExpression == null)
                data[st.Index] = stats[st.Index];
            else
            {
                double value = (double)st.CachedExpression.TryEvaluate(rowBox ??= row)!;
                data[st.Index] = double.IsFinite(value) ? value : 0;
            }
        }
    }

    public int GetStatIndex(string? statName)
    {
        if (statName is null)
            return -1;

        LeaderboardPhaseStatInfo[] stats = PlayerStats;
        for (int i = 0; i < stats.Length; ++i)
        {
            if ((object)stats[i].Name == statName)
                return i;
        }

        for (int i = 0; i < stats.Length; ++i)
        {
            if (stats[i].Name.Equals(statName, StringComparison.Ordinal))
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

        PlayerGameStatsComponent gameStats = player.Component<PlayerGameStatsComponent>();

        Team team = player.Team;
        if (!team.IsValid)
        {
            gameStats.Stats = Array.Empty<double>();
            return;
        }

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
        {
            gameStats.Stats = Array.Empty<double>();
            return;
        }

        List<LeaderboardPlayer> players = _players[teamIndex];
        foreach (LeaderboardPlayer pl in players)
        {
            if (!pl.Player.Equals(player))
                continue;

            pl.LastJoinedTeam = Time.realtimeSinceStartup;
            gameStats.Stats = pl.Stats;
            return;
        }

        gameStats.Stats = new double[PlayerStats.Length];
        players.Add(new LeaderboardPlayer(player, team));
        Logger.LogDebug("Created leaderboard player for {0} on team {1}.", player, team);
    }

    /// <summary>
    /// Allows using stats in expressions as variables (like k/d).
    /// </summary>
    private class ExpressionStatVariable : RewardExpression.IEmittableVariable
    {
        private readonly int _index;
        public string[] Names { get; }
        public ExpressionStatVariable(LeaderboardPhaseStatInfo stat, int statIndex)
        {
            Names = !string.IsNullOrWhiteSpace(stat.FormulaName) ? [ stat.FormulaName, stat.Name ] : [ stat.Name ];
            _index = statIndex;
        }

        public void Preload(LocalReference local, IOpCodeEmitter emit, ILogger logger)
        {
            MethodInfo prop = typeof(ILeaderboardRow).GetProperty(nameof(ILeaderboardRow.Stats), BindingFlags.Public | BindingFlags.Instance)
                ?.GetMethod ?? throw new InvalidOperationException("Failed to find LeaderboardSet.LeaderboardRow.Data.");

            MethodInfo getIndex = typeof(Span<double>).GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetMethod ?? throw new InvalidOperationException("Failed to find Span<double>.Item[int].");

            emit.LoadArgument(0)
                .Invoke(prop)
                .PopToLocal(typeof(Span<double>), out LocalBuilder lcl)
                .LoadLocalAddress(lcl)
                .LoadConstantInt32(_index)
                .Invoke(getIndex)
                .LoadAddressValue<double>()
                .SetLocalValue(local);
        }
    }
}

public interface ILeaderboardRow
{
    Span<double> Stats { get; }
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
    /// If the stat should be skipped on the statistic section of the points UI.
    /// </summary>
    public bool DisablePointsUIDisplay { get; set; }

    /// <summary>
    /// Header for the leaderboard per-player column.
    /// </summary>
    public string? ColumnHeader { get; set; }

    /// <summary>
    /// If the stat is shown as a sum of all players stats under the game stats section.
    /// </summary>
    public bool IsGlobalStat { get; set; }

    /// <summary>
    /// How this stat is sorted if it's the default sort stat. Valid values: 'Descending', 'Ascending'.
    /// </summary>
    public string? DefaultLeaderboardSort { get; set; }

    /// <summary>
    /// Evaluatable expression to calculate the stat from other stats.
    /// </summary>
    public string? Expression { get; set; }
}