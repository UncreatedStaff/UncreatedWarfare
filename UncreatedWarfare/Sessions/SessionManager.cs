using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Kits;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Squads;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

// ReSharper disable InconsistentlySynchronizedField

namespace Uncreated.Warfare.Sessions;

using SessionRecordPair = (SessionRecord Current, SessionRecord? Previous);

public class SessionManager :
    ILayoutHostedService,
    IHostedService,
    IEventListener<PlayerLeft>,
    IEventListener<PlayerJoined>,
    IEventListener<PlayerTeamChanged>,
    IEventListener<SquadLeaderUpdated>,
    IEventListener<SquadMemberJoined>,
    IEventListener<SquadMemberLeft>,
    IEventListener<PlayerKitChanged>
{
    private readonly IPlayerService _playerService;
    private readonly IGameDataDbContext _dbContext;
    private readonly ServerHeartbeatTimer _heartbeatTimer;
    private readonly ConcurrentDictionary<ulong, SessionRecord> _sessions = new ConcurrentDictionary<ulong, SessionRecord>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger<SessionManager> _logger;
    private readonly MapScheduler _mapScheduler;
    private readonly WarfareModule _module;
    private readonly byte _region;

    public SessionManager(
        MapScheduler mapScheduler,
        ILogger<SessionManager> logger,
        IPlayerService playerService,
        ServerHeartbeatTimer heartbeatTimer,
        IConfiguration systemConfig,
        IGameDataDbContext dbContext,
        WarfareModule module)
    {
        _mapScheduler = mapScheduler;
        _logger = logger;
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        _playerService = playerService;
        _heartbeatTimer = heartbeatTimer;
        _region = systemConfig.GetValue<byte>("region");
        _module = module;
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        await CheckForTerminatedSessions(token).ConfigureAwait(false);
    }

    async UniTask IHostedService.StopAsync(CancellationToken token)
    {
        // end all sessions

        DateTimeOffset now = DateTimeOffset.UtcNow;
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            KeyValuePair<ulong, SessionRecord>[] sessions = _sessions.ToArray();
            _sessions.Clear();

            _logger.LogInformation("Finalizing session(s): {0}.", sessions.Length);
            foreach (KeyValuePair<ulong, SessionRecord> session in sessions)
                EndSession(session.Value, endGame: true, now);

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    async UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        try
        {
            await StartNewSessionForAllPlayers(true, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uncaught exception restarting new sessions.");
        }
    }

    async UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            foreach (KeyValuePair<ulong, SessionRecord> session in _sessions)
            {
                session.Value.FinishedGame = true;
                _dbContext.Update(session.Value);
            }

            await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // indicates that a session has no real meaning and can be cut from the full list
    private static bool IsInsignificant(SessionRecord session)
    {
        return session is { EventCount: 0, LengthSeconds: <= 60 };
    }

    /// <summary>
    /// Starts a new session after ending the current session if there is one. Insignificant sessions will be cut.
    /// </summary>
    /// <param name="startedGame">If the player was there at the start of the game.</param>
    public async Task<SessionRecord> StartNewSession(WarfarePlayer player, bool startedGame, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);
            SessionRecord record = StartCreatingSession(player, startedGame, out SessionRecord? previousSession);

            _dbContext.Add(record);
            FixupSession(_dbContext, record);
            
            player.CurrentSession = record;

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            if (previousSession != null)
            {
                bool removeAndResave = EndPreviousSessionIntl(previousSession, record, record.Steam64);
                await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                if (removeAndResave)
                {
                    _dbContext.Remove(previousSession);
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }

            _logger.LogConditional("Created session {0} for: {1}.", record.SessionId, player);
            return record;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StartNewSessionForAllPlayers(bool startedGame, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);

            WarfarePlayer[] onlinePlayers = _playerService.OnlinePlayers.ToArray();
            SessionRecordPair[] sessionData = new SessionRecordPair[onlinePlayers.Length];
            bool anyPrev = false;
            List<SessionRecord>? previousToRemove = null;

            for (int i = 0; i < onlinePlayers.Length; ++i)
            {
                WarfarePlayer player = onlinePlayers[i];

                SessionRecord record = StartCreatingSession(player, startedGame, out SessionRecord? previousSession);
                player.CurrentSession = record;
                sessionData[i] = (record, previousSession);
                anyPrev |= previousSession != null;
            }

            for (int i = 0; i < onlinePlayers.Length; ++i)
            {
                SessionRecord record = sessionData[i].Current;
                _dbContext.Add(record);
                FixupSession(_dbContext, record);
            }

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            if (anyPrev)
            {
                for (int i = 0; i < onlinePlayers.Length; ++i)
                {
                    SessionRecord? previous = sessionData[i].Previous;
                    if (previous == null)
                        continue;

                    if (!EndPreviousSessionIntl(previous, sessionData[i].Current, previous.Steam64))
                        continue;

                    (previousToRemove ??= new List<SessionRecord>(4)).Add(previous);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

                if (previousToRemove != null)
                {
                    foreach (SessionRecord r in previousToRemove)
                        _dbContext.Remove(r);

                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                }
            }

            _logger.LogConditional("Created sessions for all players.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new sessions, hard clearing...");
            _dbContext.ChangeTracker.Clear();

            await UniTask.SwitchToMainThread(token);

            WarfarePlayer[] onlinePlayers = _playerService.OnlinePlayers.ToArray();
            foreach (WarfarePlayer player in onlinePlayers)
            {
                SessionRecord record = StartCreatingSession(player, startedGame, out SessionRecord? previousSession);
                player.CurrentSession = record;
            }

            try
            {
                await _dbContext.SaveChangesAsync(token);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Error saving ended sessions");
                _dbContext.ChangeTracker.Clear();
            }

            foreach (WarfarePlayer player in onlinePlayers)
            {
                _dbContext.Add(player.CurrentSession);
            }

            try
            {
                await _dbContext.SaveChangesAsync(token);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Error saving new sessions.");
                _dbContext.ChangeTracker.Clear();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <returns>If <paramref name="previousSession"/> needs to be removed after saving changes.</returns>
    private bool EndPreviousSessionIntl(SessionRecord previousSession, SessionRecord record, ulong player)
    {
        bool needsRemove = false;
        // check if session is insignificant (no events and elapsed time < 5 seconds)
        if (IsInsignificant(previousSession))
        {
            SessionRecord? doublePreviousSession = previousSession.PreviousSession;
            if (doublePreviousSession != null)
            {
                // update the next session ID for the previous session's previous session
                record.PreviousSessionId = doublePreviousSession.SessionId;
                record.PreviousSession = doublePreviousSession;
                doublePreviousSession.NextSessionId = record.SessionId;
                doublePreviousSession.FinishedGame = previousSession.FinishedGame;
                doublePreviousSession.EndedTimestamp = previousSession.EndedTimestamp;
                doublePreviousSession.LengthSeconds = (doublePreviousSession.EndedTimestamp!.Value - doublePreviousSession.StartedTimestamp).TotalSeconds;

                FixupSession(_dbContext, doublePreviousSession);
                _dbContext.Update(doublePreviousSession);
                if (doublePreviousSession.PreviousSession != null)
                {
                    _dbContext.Entry(doublePreviousSession.PreviousSession).State = EntityState.Detached;
                    doublePreviousSession.PreviousSession = null;
                }
                needsRemove = true;
            }
            else
            {
                record.PreviousSessionId = null;

                if (!record.StartedGame)
                {
                    record.StartedTimestamp = previousSession.StartedTimestamp;
                    record.StartedGame = previousSession.StartedGame;
                }
            }

            _dbContext.Update(record);
            if (!needsRemove)
                _dbContext.Remove(previousSession);

            _logger.LogConditional("Cut insignificant session {0} for {1}.", previousSession.SessionId, new CSteamID(player));
        }
        else
        {
            previousSession.NextSessionId = record.SessionId;
            _dbContext.Update(previousSession);

            if (previousSession.PreviousSession != null)
            {
                // prevent memory leak by keeping every single session since the player joined. the ID is still set
                _dbContext.Entry(previousSession.PreviousSession).State = EntityState.Detached;
                previousSession.PreviousSession = null;
            }
        }

        FixupSession(_dbContext, previousSession);
        FixupSession(_dbContext, record);
        return needsRemove;
    }

    private SessionRecord StartCreatingSession(WarfarePlayer player, bool startedGame, out SessionRecord? previous)
    {
        GameThread.AssertCurrent();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();

        Squad? squad = player.GetSquad();
        SessionRecord record = new SessionRecord
        {
            Steam64 = player.Steam64.m_SteamID,
            StartedTimestamp = now,
            FactionId = player.Team.IsValid ? player.Team.Faction.PrimaryKey : null,
            GameId = _module.GetActiveLayout().LayoutId,
            StartedGame = startedGame,
            KitId = kitComp.ActiveKitKey,
            KitName = kitComp.ActiveKitId,
            MapId = _mapScheduler.Current,
            SeasonId = WarfareModule.Season,
            SquadName = squad?.Name,
            SquadLeader = squad?.Leader?.Steam64.m_SteamID,
            FinishedGame = false,
            Team = player.Team.Id,
            UnexpectedTermination = true
        };

        SessionRecord? prev = null;

        // threadsafe exchange
        _sessions.AddOrUpdate(
            player.Steam64.m_SteamID,
            _        => { prev = null; return record; },
            (_, old) => { prev = old ; return record; }
        );

        previous = prev;

        if (prev != null)
        {
            record.PreviousSessionId = prev.SessionId;
            record.PreviousSession = prev;
        }

        if (prev is { UnexpectedTermination: true })
            EndSession(prev, startedGame, now, false);

        return record;
    }

    private void EndSession(SessionRecord record, bool endGame, DateTimeOffset dt, bool update = true)
    {
        record.EndedTimestamp = dt;
        record.LengthSeconds = (record.EndedTimestamp.Value - record.StartedTimestamp).TotalSeconds;
        record.FinishedGame = endGame;
        record.UnexpectedTermination = false;

        if (!update)
            return;

        _dbContext.Update(record);
        FixupSession(_dbContext, record);

        _logger.LogConditional("Ended session {0} for {1}.", record.SessionId, record.Steam64);
    }

    private void TryStartCreateSessionForPlayerIfNeeded(WarfarePlayer player, string context)
    {
        if (player.IsDisconnected || !IsSessionExpired(player))
        {
            _logger.LogConditional("Skipping new session for player {0} (" + context + ").", player);
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await StartNewSession(player, false, player.DisconnectToken);
                _logger.LogConditional("Started new session for player {0} (" + context + ").", player);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new session for player {0} (" + context + ").", player);
            }
        });
    }
    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        TryStartCreateSessionForPlayerIfNeeded(e.Player, "team changed");
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<SquadLeaderUpdated>.HandleEvent(SquadLeaderUpdated e, IServiceProvider serviceProvider)
    {
        foreach (WarfarePlayer member in e.Squad.Members)
            TryStartCreateSessionForPlayerIfNeeded(member, "squad leader changed");
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<SquadMemberJoined>.HandleEvent(SquadMemberJoined e, IServiceProvider serviceProvider)
    {
        TryStartCreateSessionForPlayerIfNeeded(e.Player, "squad member joined");
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<SquadMemberLeft>.HandleEvent(SquadMemberLeft e, IServiceProvider serviceProvider)
    {
        TryStartCreateSessionForPlayerIfNeeded(e.Player, "squad member left");
    }
    
    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerKitChanged>.HandleEvent(PlayerKitChanged e, IServiceProvider serviceProvider)
    {
        TryStartCreateSessionForPlayerIfNeeded(e.Player, "kit changed");
    }

    private bool IsSessionExpired(WarfarePlayer player)
    {
        SessionRecord? currentSession = player.CurrentSession;
        if (currentSession == null)
            return player.IsOnline;

        if (player.Component<KitPlayerComponent>().ActiveKitKey != currentSession.KitId)
            return true;

        FactionInfo? faction = player.Team.Faction;

        if (faction == null != !currentSession.FactionId.HasValue || faction != null && currentSession.FactionId.HasValue && faction.PrimaryKey != currentSession.FactionId!.Value)
            return true;

        if (_module.IsLayoutActive() && _module.GetActiveLayout().LayoutId != currentSession.GameId)
            return true;

        if (player.GetSquad() is { } squad)
        {
            if (!currentSession.SquadLeader.HasValue || currentSession.SquadLeader.Value != squad.Leader.Steam64.m_SteamID)
                return true;
        
            if (currentSession.SquadName == null || !currentSession.SquadName.Equals(squad.Name, StringComparison.Ordinal))
                return true;
        }
        else if (currentSession.SquadLeader.HasValue || currentSession.SquadName != null)
        {
            return true;
        }

        return false;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerJoined>.HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        _logger.LogConditional("Creating session for {0}. (joined)", e.Player);
        if (_sessions.TryRemove(e.Steam64.m_SteamID, out SessionRecord r))
        {
            _logger.LogWarning("Removed pre-existing session for {0} on join. This should never happen.", e.Player);
            Task.Run(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    _dbContext.Entry(r).State = EntityState.Detached;
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }

        Task.Run(async () =>
        {
            try
            {
                await StartNewSession(e.Player, false, e.Player.DisconnectToken);
            }
            catch (OperationCanceledException) when (e.Player.IsDisconnected || e.Player.IsDisconnecting) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting session on player {0} join.", e.Player);
            }
        });
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        DateTimeOffset leaveTime = DateTimeOffset.UtcNow;

        if (!_sessions.TryRemove(e.Steam64.m_SteamID, out SessionRecord record))
            return;

        Task t = _semaphore.WaitAsync(CancellationToken.None);
        Task.Run(async () =>
        {
            await t.ConfigureAwait(false);
            try
            {
                EndSession(record, false, leaveTime);
                await _dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling player disconnect.");
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }

    private static void FixupSession(IGameDataDbContext dbContext, SessionRecord session)
    {
        // makes sure all the random referenced models aren't included in the update query
        if (session.Kit != null)
            dbContext.Entry(session.Kit).State = EntityState.Detached;
        if (session.Game != null)
            dbContext.Entry(session.Game).State = EntityState.Detached;
        if (session.Map != null)
            dbContext.Entry(session.Map).State = EntityState.Detached;
        if (session.PlayerData != null)
            dbContext.Entry(session.PlayerData).State = EntityState.Detached;
        if (session.SquadLeaderData != null)
            dbContext.Entry(session.SquadLeaderData).State = EntityState.Detached;
    }

    /// <summary>
    /// Fixes the dates on any sessions that didn't get terminated properly (server crashed, etc). Accurate to +/- 10 seconds.
    /// </summary>
    private async Task CheckForTerminatedSessions(CancellationToken token = default)
    {
        DateTimeOffset? lastHeartbeat = _heartbeatTimer.GetLastBeat();
        if (!lastHeartbeat.HasValue)
        {
            _logger.LogInformation("Unknown last server heartbeat.");
            return;
        }

        _logger.LogInformation(
            "Server last online: {0} ({1} ago). Checking for sessions that were terminated unexpectedly.",
            lastHeartbeat.Value,
            (DateTime.UtcNow - lastHeartbeat.Value.UtcDateTime).ToString("g", CultureInfo.InvariantCulture));

        List<SessionRecord> records = await _dbContext.Sessions
            .Where(x => x.UnexpectedTermination && x.EndedTimestamp == null && x.Game.Region == _region)
            .ToListAsync(token);

        int ct = records.Count;

        foreach (SessionRecord record in records)
        {
            record.EndedTimestamp = lastHeartbeat.Value;
            record.LengthSeconds = (lastHeartbeat.Value.UtcDateTime - record.StartedTimestamp.UtcDateTime)
                .TotalSeconds;
        }

        _dbContext.UpdateRange(records);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        _logger.LogInformation("Migrated {0} session(s) after server didn't shut down cleanly.", ct);
    }
}