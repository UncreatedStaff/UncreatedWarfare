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
    }


    private readonly IConfiguration _systemConfig;
    private readonly WarfareModule _layoutHost;
    private readonly LayoutFactory _layoutFactory;
    private readonly ILoopTickerFactory _loopTickerFactory;
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

    public event Action<SeedingRules>? RulesUpdated;

    public SeedingPlayerCountMonitor(
        IConfiguration systemConfig,
        WarfareModule layoutHost,
        LayoutFactory layoutFactory,
        ILoggerFactory loggerFactory,
        ILoopTickerFactory loopTickerFactory,
        IServiceProvider serviceProvider)
    {
        _systemConfig = systemConfig;
        _layoutHost = layoutHost;
        _layoutFactory = layoutFactory;
        _loopTickerFactory = loopTickerFactory;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<SeedingPlayerCountMonitor>();
        Rules = new SeedingRules();

        ReloadRules();

        _changeToken = ChangeToken.OnChange(
            systemConfig.GetReloadToken,
            me => me.ReloadRules(),
            this
        );

        IsSeeding = false;

        VoteManager = new PlayerVoteManager(loopTickerFactory, loggerFactory.CreateLogger<PlayerVoteManager>());
        VoteHud = null;

        _nextVotePlayerThreshold = Rules.VotePlayerThreshold;
    }

    private void ReloadRules()
    {
        Rules.Enabled = _systemConfig.GetValue<bool>("seeding:enabled");
        Rules.VotePlayerThreshold = _systemConfig.GetValue("seeding:start_vote_players", 15);
        Rules.StartPlayerThreshold = _systemConfig.GetValue("seeding:start_game_players", 20);
        Rules.StartCountdownLength = _systemConfig.GetValue("seeding:countdown_time", TimeSpan.FromSeconds(30));
        Rules.VoteLength = _systemConfig.GetValue("seeding:vote_time", TimeSpan.FromMinutes(1));
        RulesUpdated?.Invoke(Rules);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _logger.LogInformation("Hosted SPCM.");
        if (!_layoutHost.IsLayoutActive())
            return UniTask.CompletedTask;

        bool updateUi = true;
        if (_layoutHost.GetActiveLayout().LayoutInfo.IsSeeding)
        {
            _logger.LogInformation("seeding layout.");
            if (!IsSeeding)
            {
                _logger.LogInformation("Starting seeding.");
                updateUi = false;
                StartSeeding();
                CheckShouldAwaitStart();
            }
        }
        else if (IsSeeding)
        {
            _logger.LogInformation("Stopping seeding.");
            IsSeeding = false;
            _nextVotePlayerThreshold = Rules.VotePlayerThreshold;
            CheckShouldStartVote();
        }
        else
        {
            _logger.LogInformation("Seeding state correct.");
            return UniTask.CompletedTask;
        }

        if (updateUi || IsAwaitingStart)
        {
            _playHud?.UpdateStage();
        }

        return UniTask.CompletedTask;
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
            IsSeeding = false;
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
        _playHud?.UpdateStage();

        _logger.LogInformation("Ending seeding.");
        _ = _layoutFactory.StartNextLayout();
    }

    private void StartSeeding(bool delayStart = false)
    {
        if (IsSeeding)
            return;

        _logger.LogInformation("Starting seeding for PC.");
        _nextVotePlayerThreshold = Rules.VotePlayerThreshold;

        _playHud ??= _serviceProvider.GetRequiredService<SeedingPlayHud>();
        IsSeeding = true;

        if (_layoutHost.IsLayoutActive())
        {
            _playHud.UpdateStage();
        }

        using (LayoutInfo newLayout = _pendingLayout ?? _layoutFactory.SelectRandomLayout(seeding: true))
        {
            Interlocked.CompareExchange(ref _pendingLayout, null, newLayout);
            _layoutFactory.NextLayout = new FileInfo(newLayout.FilePath);
            _logger.LogInformation($"Selected seeding layout: {newLayout.DisplayName}.");
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
