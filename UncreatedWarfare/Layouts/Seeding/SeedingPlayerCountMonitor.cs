using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Stats.EventHandlers;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Layouts.Seeding;

internal class SeedingPlayerCountMonitor :
    IEventListener<PlayerJoined>,
    IEventListener<PlayerLeft>,
    IHostedService,
    ILayoutHostedService,
    IDisposable
{
    public class SeedingRules
    {
        /// <summary>
        /// Number of players that have to be left in the server to start a seeding vote.
        /// </summary>
        /// <remarks>seeding:start_vote_players</remarks>
        public int VotePlayerThreshold { get; set; }

        /// <summary>
        /// Number of players that have to join the server to start a real gamemode.
        /// </summary>
        /// <remarks>seeding:start_game_players</remarks>
        public int StartPlayerThreshold { get; set; }

        /// <summary>
        /// Whether or not seeding is enabled.
        /// </summary>
        /// <remarks>seeding:enabled</remarks>
        public bool Enabled { get; set; }

        /// <summary>
        /// Amount of time after enough players join to start the real gamemode.
        /// </summary>
        /// <remarks>seeding:countdown</remarks>
        public TimeSpan StartCountdownLength { get; set; }

        /// <summary>
        /// Amount of time the vote to start a seeding gamemode lasts.
        /// </summary>
        /// <remarks>seeding:vote_time</remarks>
        public TimeSpan VoteLength { get; set; }

        /// <summary>
        /// Whether or not to add to global stats during a seeding gamemode.
        /// </summary>
        public bool TrackStats { get; set; }

        /// <summary>
        /// Whether or not to add to XP and credits during a seeding gamemode.
        /// </summary>
        public bool TrackPoints { get; set; }
    }


    private readonly IConfiguration _systemConfig;
    private readonly WarfareModule _layoutHost;
    private readonly LayoutFactory _layoutFactory;
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly PointsRewardsEvents _pointsRewards;
    private readonly DatabaseStatsBuffer _databaseStats;
    private readonly ILogger<SeedingPlayerCountMonitor> _logger;
    private readonly IDisposable _changeToken;

    private bool _isStartingVote;
    private int _nextVotePlayerThreshold;

    private LayoutInfo? _pendingLayout;

    private SeedingPlayHud? _playHud;

    /// <summary>
    /// The approximate time at which the game should start.
    /// </summary>
    public DateTime AwaitDoneTime { get; private set; }

    // for UI
    private readonly IServiceProvider _serviceProvider;
    private ILoopTicker? _awaitStartTicker;

    public IPlayerVoteManager VoteManager { get; set; }
    public SeedingVoteHud? VoteHud { get; set; }

    public SeedingRules Rules { get; }

    public bool IsSeeding { get; private set; }
    public bool IsAwaitingStart => _awaitStartTicker != null;

    /// <summary>
    /// Invoked when config is updated. May not be invoked on the main thread.
    /// </summary>
    public event Action<SeedingRules>? RulesUpdated;

    public SeedingPlayerCountMonitor(
        IConfiguration systemConfig,
        WarfareModule layoutHost,
        LayoutFactory layoutFactory,
        ILoggerFactory loggerFactory,
        ILoopTickerFactory loopTickerFactory,
        PointsRewardsEvents pointsRewards,
        DatabaseStatsBuffer databaseStats,
        IServiceProvider serviceProvider)
    {
        _systemConfig = systemConfig;
        _layoutHost = layoutHost;
        _layoutFactory = layoutFactory;
        _loopTickerFactory = loopTickerFactory;
        _pointsRewards = pointsRewards;
        _databaseStats = databaseStats;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<SeedingPlayerCountMonitor>();
        Rules = new SeedingRules();

        ReloadRules(true);

        _changeToken = ChangeToken.OnChange(
            systemConfig.GetReloadToken,
            me => me.ReloadRules(false),
            this
        );

        IsSeeding = false;

        VoteManager = new PlayerVoteManager(loopTickerFactory, loggerFactory.CreateLogger<PlayerVoteManager>());
        VoteHud = null;

        _nextVotePlayerThreshold = Rules.VotePlayerThreshold;
    }

    private void ReloadRules(bool startup)
    {
        Rules.Enabled = _systemConfig.GetValue<bool>("seeding:enabled");
        Rules.VotePlayerThreshold = _systemConfig.GetValue("seeding:start_vote_players", 15);
        Rules.StartPlayerThreshold = _systemConfig.GetValue("seeding:start_game_players", 20);
        Rules.StartCountdownLength = _systemConfig.GetValue("seeding:countdown_time", TimeSpan.FromSeconds(30));
        Rules.VoteLength = _systemConfig.GetValue("seeding:vote_time", TimeSpan.FromMinutes(1));
        RulesUpdated?.Invoke(Rules);

        if (startup)
            return;

        if (Rules.Enabled)
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                CheckShouldStartVote();
                if (!VoteManager.IsVoting && !IsSeeding)
                    CheckShouldAwaitStart();
            });
            return;
        }

        Interlocked.Exchange(ref _awaitStartTicker, null)?.Dispose();
        try
        {
            if (VoteManager.IsVoting)
                _ = VoteManager.EndVoteAsync(cancelled: true);
        }
        catch (ObjectDisposedException) { }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        if (!_layoutHost.IsLayoutActive())
            return UniTask.CompletedTask;

        bool updateUi = true;
        if (_layoutHost.GetActiveLayout().LayoutInfo.IsSeeding)
        {
            if (!IsSeeding)
            {
                updateUi = false;
                StartSeeding();
                CheckShouldAwaitStart();
            }
        }
        else if (IsSeeding)
        {
            IsSeeding = false;
            _nextVotePlayerThreshold = Rules.VotePlayerThreshold;
            CheckShouldStartVote();
            UpdateGlobals(false);
        }
        else
        {
            return UniTask.CompletedTask;
        }

        if (updateUi || IsAwaitingStart)
        {
            _playHud?.UpdateStage();
        }

        return UniTask.CompletedTask;
    }

    private void UpdateGlobals(bool isSeeding)
    {
        if (isSeeding)
        {
            _pointsRewards.TrackPoints = Rules.TrackPoints;
            _databaseStats.TrackStats = Rules.TrackStats;
        }
        else
        {
            _pointsRewards.TrackPoints = true;
            _databaseStats.TrackStats = true;
        }
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        if (!IsSeeding && VoteManager.IsVoting)
        {
            return VoteManager.EndVoteAsync(token, cancelled: true);
        }
        
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        if (!Rules.Enabled || _layoutFactory.NextLayout != null)
        {
            if (IsSeeding)
            {
                IsSeeding = false;
                UpdateGlobals(false);
            }
            return UniTask.CompletedTask;
        }
        
        try
        {
            StartSeeding(delayStart: true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No seeding layouts configured, booting into a normal layout.");
        }

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (!Rules.Enabled)
            return;

        CheckShouldAwaitStart();
        if (IsSeeding)
        {
            _logger.LogInformation($"Sending UI to {e.Player}.");
            _playHud?.UpdateStage();
        }
    }

    private void CheckShouldAwaitStart()
    {
        if (!IsSeeding || IsAwaitingStart)
        {
            return;
        }

        if (Provider.clients.Count < Rules.StartPlayerThreshold)
            return;

        _logger.LogDebug($"Awaiting start, enough players {Provider.clients.Count} joined.");

        TimeSpan countdown = Rules.StartCountdownLength;

        _awaitStartTicker = _loopTickerFactory.CreateTicker(
            TimeSpan.FromSeconds(1d),
            invokeImmediately: false,
            queueOnGameThread: true,
            onTick: AwaitStartCompleted
        );

        AwaitDoneTime = DateTime.UtcNow + countdown;
    }

    private void CheckShouldStartVote()
    {
        if (IsSeeding)
        {
            if (IsAwaitingStart && Provider.clients.Count < Rules.StartPlayerThreshold - 1)
            {
                Interlocked.Exchange(ref _awaitStartTicker, null)?.Dispose();
            }

            return;
        }

        if (Provider.clients.Count <= 1)
        {
            if (VoteManager.IsVoting)
                _ = VoteManager.EndVoteAsync(cancelled: true);

            StartSeeding();
            return;
        }

        if (!(_isStartingVote || VoteManager.IsVoting) && Provider.clients.Count < _nextVotePlayerThreshold)
        {
            StartVote();
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (!Rules.Enabled)
            return;

        CheckShouldStartVote();
    }

    private void StartVote()
    {
        _pendingLayout?.Dispose();
        _pendingLayout = _layoutFactory.SelectRandomLayout(seeding: true);
        VoteHud ??= ActivatorUtilities.CreateInstance<SeedingVoteHud>(_serviceProvider, VoteManager, _pendingLayout.DisplayName);

        _isStartingVote = true;
        UniTask.Create(async () =>
        {
            try
            {
                await VoteManager.StartVoteAsync(new VoteSettings
                {
                    Duration = Rules.VoteLength,
                    //RequiredYes = 0.5,
                    //RequiredNo = 0.75,
                    DefaultResult = VoteResult.Yes,
                    Display = VoteHud,
                    OwnsDisplay = false,
                    VoteCancellationToken = CancellationToken.None
                }, result =>
                {
                    if (result.Result == VoteResult.Yes)
                        StartSeeding();
                    else
                        DelayVote();
                });
            }
            finally
            {
                _isStartingVote = false;
            }
        });
    }

    private void AwaitStartCompleted(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        // assume on game thread
        if (DateTime.UtcNow >= AwaitDoneTime)
        {
            ticker.Dispose();
            Interlocked.CompareExchange(ref _awaitStartTicker, null, ticker);
            AwaitDoneTime = DateTime.UtcNow;

            EndSeeding();
        }
        else
        {
            _playHud?.UpdateProgress();
        }
    }


    private void DelayVote()
    {
        _nextVotePlayerThreshold = (int)Math.Ceiling(_nextVotePlayerThreshold / 1.5);
    }

    private void EndSeeding()
    {
        // assume on game thread
        if (!IsSeeding)
            return;

        IsSeeding = false;
        UpdateGlobals(false);
        _playHud?.UpdateStage();

        _logger.LogInformation("Ending seeding.");
        _ = _layoutFactory.StartNextLayout();
    }

    private void StartSeeding(bool delayStart = false)
    {
        if (IsSeeding)
            return;

        _logger.LogInformation("Starting seeding for PC.");
        UpdateGlobals(true);
        _nextVotePlayerThreshold = Rules.VotePlayerThreshold;

        _playHud ??= _serviceProvider.GetRequiredService<SeedingPlayHud>();
        IsSeeding = true;

        if (_layoutHost.IsLayoutActive())
        {
            _playHud.UpdateStage();
        }

        if (_layoutFactory.NextLayout == null)
        {
            using LayoutInfo newLayout = _pendingLayout ?? _layoutFactory.SelectRandomLayout(seeding: true);
            Interlocked.CompareExchange(ref _pendingLayout, null, newLayout);
            _layoutFactory.NextLayout = new FileInfo(newLayout.FilePath);
            _logger.LogInformation($"Selected seeding layout: {newLayout.DisplayName}.");
        }
        else
        {
            _logger.LogInformation($"Using specified next layout: {_layoutFactory.NextLayout.Name}.");
        }

        if (!delayStart)
        {
            _logger.LogInformation("Starting seeding layout explicitly.");
            _ = _layoutFactory.StartNextLayout(CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _changeToken.Dispose();
        Interlocked.Exchange(ref _pendingLayout, null)?.Dispose();
        Interlocked.Exchange(ref _awaitStartTicker, null)?.Dispose();

        if (VoteManager is IDisposable disp)
            disp.Dispose();

        VoteHud?.Dispose();
        VoteHud = null;
    }
}
