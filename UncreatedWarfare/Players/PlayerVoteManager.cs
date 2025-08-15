using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Players;

public class PlayerVoteManager : IPlayerVoteManager, IDisposable, IEventListener<PlayerJoined>, IEventListener<PlayerLeft>
{
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly ILogger<PlayerVoteManager> _logger;
    private readonly object _voteSync;

    private CurrentVoteInfo _voteInfo;

    public bool IsVoting { get; private set; }

    public DateTime VoteStart
    {
        get
        {
            lock (_voteSync)
            {
                AssertVoting();
                return field;
            }
        }
        private set;
    }

    public DateTime VoteEnd
    {
        get
        {
            lock (_voteSync)
            {
                AssertVoting();
                return field;
            }
        }
        private set;
    }

    public PlayerVoteManager(ILoopTickerFactory loopTickerFactory, ILogger<PlayerVoteManager> logger)
    {
        _voteSync = new object();
        _loopTickerFactory = loopTickerFactory;
        _logger = logger;
    }

    private void AssertVoting()
    {
        if (!IsVoting)
            throw new InvalidOperationException("Not voting.");
    }

    public async UniTask StartVoteAsync(VoteSettings settings, Action<IVoteResult>? callback, CancellationToken startCancellationToken = default)
    {
        await UniTask.SwitchToMainThread(startCancellationToken);

        lock (_voteSync)
        {
            VoteResultContainer results;

            if (IsVoting)
                throw new InvalidOperationException("Already voting.");

            CancellationToken ct = settings.VoteCancellationToken;
            if (ct.IsCancellationRequested)
            {
                results = new VoteResultContainer(settings)
                {
                    Votes = new Dictionary<CSteamID, PlayerVoteState>(0),
                    Result = VoteResult.Cancelled
                };

                if (settings.Display != null)
                {
                    InvokeStart(settings.Display, in settings);
                    InvokeFinished(settings.Display, results);
                }

                callback?.Invoke(results);
                return;
            }

            results = new VoteResultContainer(settings);
            _voteInfo.Callback = callback;
            _voteInfo.Container = results;
            _voteInfo.PendingVotes = new ConcurrentDictionary<ulong, PlayerVoteState>();
            IsVoting = true;

            InvokeStart(settings.Display, in settings);

            try
            {
                if (ct.CanBeCanceled)
                {
                    ct.Register(static state =>
                    {
                        UniTask.Create(async () =>
                        {
                            await UniTask.SwitchToMainThread();
                            ((PlayerVoteManager)state).CancelVote();
                        });
                    }, this);
                }
            }
            catch (ObjectDisposedException) { }

            if (settings.Duration.Ticks > 0)
            {
                _voteInfo.Timer = _loopTickerFactory.CreateTicker(settings.Duration, false, queueOnGameThread: true, OnTimerCompleted);
            }
        }
    }

    private void OnTimerCompleted(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        Interlocked.CompareExchange(ref _voteInfo.Timer, null, ticker)?.Dispose();

        lock (_voteSync)
        {
            if (!IsVoting)
                return;

            EndVoteIntl();
        }
    }

    public async UniTask EndVoteAsync(CancellationToken token = default, bool cancelled = false)
    {
        await UniTask.SwitchToMainThread(token);

        if (cancelled)
        {
            CancelVote();
        }
        else
        {
            lock (_voteSync)
            {
                AssertVoting();
                EndVoteIntl();
            }
        }
    }

    private void EndVoteIntl()
    {
        // assume: game thread, locked _voteSync
        AssertVoting();

        VoteResultContainer results = _voteInfo.Container;

        PlayerDictionary<PlayerVoteState> dict = new PlayerDictionary<PlayerVoteState>(_voteInfo.PendingVotes);
        results.Votes = dict;

        int yesCt = 0, noCt = 0;
        foreach (KeyValuePair<ulong, PlayerVoteState> vote in dict)
        {
            switch (vote.Value)
            {
                case PlayerVoteState.No:
                    ++noCt;
                    break;

                case PlayerVoteState.Yes:
                    ++yesCt;
                    break;
            }
        }

        if (yesCt == noCt)
        {
            results.Result = results.SettingsIntl.DefaultResult;
        }
        else
        {
            results.Result = yesCt > noCt ? VoteResult.Yes : VoteResult.No;
        }

        Action<IVoteResult>? callback = _voteInfo.Callback;

        InvokeFinished(results.SettingsIntl.Display, results);

        CleanupVote();

        InvokeCallback(callback, results);
    }

    private void CancelVote()
    {
        // assume: game thread
        lock (_voteSync)
        {
            AssertVoting();

            VoteResultContainer results = _voteInfo.Container;

            results.Result = VoteResult.Cancelled;
            results.Votes = new PlayerDictionary<PlayerVoteState>(_voteInfo.PendingVotes);

            Action<IVoteResult>? callback = _voteInfo.Callback;

            InvokeFinished(results.SettingsIntl.Display, results);

            CleanupVote();

            InvokeCallback(callback, results);
        }
    }

    private void CleanupVote()
    {
        // assume: locked _voteSync
        IsVoting = false;
        VoteStart = DateTime.MinValue;
        VoteEnd = DateTime.MinValue;

        Interlocked.Exchange(ref _voteInfo.Timer, null)?.Dispose();
        if (_voteInfo.Container.SettingsIntl is { OwnsDisplay: true, Display: IDisposable disposable })
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing vote display.");
            }
        }

        _voteInfo = default;
    }

    public PlayerVoteState GetVoteState(CSteamID player)
    {
        ConcurrentDictionary<ulong, PlayerVoteState>? states = _voteInfo.PendingVotes;
        if (states == null || !IsVoting)
        {
            throw new InvalidOperationException("Not voting.");
        }

        return states.GetValueOrDefault(player.m_SteamID, PlayerVoteState.Unanswered);
    }

    public PlayerVoteState RegisterVote(CSteamID player, PlayerVoteState vote)
    {
        ConcurrentDictionary<ulong, PlayerVoteState>? states = _voteInfo.PendingVotes;
        IVoteDisplay? voteDisplay = _voteInfo.Container?.SettingsIntl.Display;
        if (states == null || !IsVoting)
        {
            throw new InvalidOperationException("Not voting.");
        }

        PlayerVoteState old;
        if (vote == PlayerVoteState.Unanswered)
        {
            if (!states.TryRemove(player.m_SteamID, out old))
                old = PlayerVoteState.Unanswered;
        }
        else
        {
            PlayerVoteState other = vote switch
            {
                PlayerVoteState.Yes => PlayerVoteState.No,
                PlayerVoteState.No => PlayerVoteState.Yes,
                _ => throw new ArgumentOutOfRangeException(nameof(vote))
            };

            if (!states.TryUpdate(player.m_SteamID, vote, other))
            {
                old = states.TryAdd(player.m_SteamID, vote) ? PlayerVoteState.Unanswered : vote;
            }
            else
            {
                old = other;
            }
        }

        if (voteDisplay == null || old == vote)
            return old;

        // invoke update
        if (GameThread.IsCurrent)
        {
            InvokePlayerVoteUpdated(voteDisplay, player, vote, old);
        }
        else
        {
            PlayerVoteState vote2 = vote, old2 = old;
            CSteamID pid = player;
            IVoteDisplay disp = voteDisplay;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(_voteInfo.Container?.SettingsIntl.Display, disp))
                {
                    InvokePlayerVoteUpdated(disp, pid, vote2, old2);
                }
            });
        }

        return old;
    }

    private void InvokeCallback(Action<IVoteResult>? callback, IVoteResult results)
    {
        if (callback == null)
            return;

        try
        {
            callback.Invoke(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking vote callback.");
        }
    }

    private void InvokeStart(IVoteDisplay? display, in VoteSettings settings)
    {
        if (display == null)
            return;

        try
        {
            display.VoteStarted(in settings, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking VoteStarted callback.");
        }
    }

    private void InvokeFinished(IVoteDisplay? display, IVoteResult result)
    {
        if (display == null)
            return;

        try
        {
            display.VoteFinished(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking VoteFinished callback.");
        }
    }

    private void InvokePlayerVoteUpdated(IVoteDisplay? display, CSteamID playerId, PlayerVoteState newVote, PlayerVoteState oldVote)
    {
        if (display == null)
            return;

        try
        {
            display.PlayerVoteUpdated(playerId, newVote, oldVote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking PlayerVoteUpdated callback.");
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        if (!IsVoting)
            return;

        IVoteDisplay? disp = _voteInfo.Container?.SettingsIntl.Display;
        if (disp == null)
            return;

        try
        {
            disp.PlayerJoinedVote(e.Player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking PlayerJoinedVote callback.");
        }
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        if (!IsVoting)
            return;

        IVoteDisplay? disp = _voteInfo.Container?.SettingsIntl.Display;
        if (disp == null)
            return;

        try
        {
            disp.PlayerLeftVote(e.Player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking PlayerLeftVote callback.");
        }
    }

    private struct CurrentVoteInfo
    {
        public VoteResultContainer Container;
        public Action<IVoteResult>? Callback;
        public ILoopTicker? Timer;
        public ConcurrentDictionary<ulong, PlayerVoteState> PendingVotes;
    }

    public void Dispose()
    {
        lock (_voteSync)
        {
            CleanupVote();
        }
    }

    private class VoteResultContainer : IVoteResult
    {
        public readonly VoteSettings SettingsIntl;
#nullable disable
        public VoteResult Result { get; set; }
        public IReadOnlyDictionary<CSteamID, PlayerVoteState> Votes { get; set; }

        public VoteSettings Settings => SettingsIntl;

        public VoteResultContainer(VoteSettings settings)
        {
            SettingsIntl = settings;
            Votes = null!;
        }
#nullable restore
    }
}
