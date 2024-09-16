using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;

// ReSharper disable InconsistentlySynchronizedField

namespace Uncreated.Warfare.Sessions;
public class SessionManager : IHostedService, IEventListener<PlayerLeft>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<ulong, SessionRecord> _sessions = new ConcurrentDictionary<ulong, SessionRecord>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SessionManager>>();
    }

    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        _semaphore.Release();

        await CheckForTerminatedSessions(token).ConfigureAwait(false);

        await RestartSessionsForAll(false, true, token);

        SquadManager.SquadStatusUpdated += OnSquadChanged;
        KitManager.OnManualKitChanged += OnKitChanged;
        EventDispatcher.GroupChanged += OnGroupChanged;
        GamemodeOld.OnGamemodeChanged += OnGamemodeChanged;
    }

    async UniTask IHostedService.StopAsync(CancellationToken token)
    {
        GamemodeOld.OnGamemodeChanged -= OnGamemodeChanged;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        KitManager.OnManualKitChanged -= OnKitChanged;
        SquadManager.SquadStatusUpdated -= OnSquadChanged;

        KeyValuePair<ulong, SessionRecord>[] sessions;
        lock (_sessions)
        {
            sessions = _sessions.ToArray();
            _sessions.Clear();
        }

        using IServiceScope scope = _serviceProvider.CreateScope();
        await using IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();

        foreach (KeyValuePair<ulong, SessionRecord> session in sessions)
            EndSession(dbContext, session.Value, true);

        L.Log($"[SESSIONS] Finalizing session(s): {sessions.Length}.", ConsoleColor.Magenta);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task<SessionRecord> RestartSession(WarfarePlayer player, bool lockpSync, bool startedGame, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (lockpSync)
                await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await UniTask.SwitchToMainThread(token);
                using IServiceScope scope = _serviceProvider.CreateScope();
                await using IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();
                SessionRecord record = StartCreatingSession(dbContext, player, startedGame, out SessionRecord? previousSession);
                player.CurrentSession = record;

                await dbContext.AddAsync(record, token).ConfigureAwait(false);
                FixupSession(dbContext, record);
                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                if (previousSession != null)
                {
                    previousSession.NextSessionId = record.SessionId;
                    dbContext.Update(previousSession);
                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                }

                L.LogDebug($"[SESSIONS] Created session {record.SessionId} for: {player}.");
                return record;
            }
            finally
            {
                if (lockpSync)
                    player.PurchaseSync.Release();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task RestartSessionsForAll(bool startedGame, bool lockpSync, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await UniTask.SwitchToMainThread(token);

            WarfarePlayer[] onlinePlayers = PlayerManager.OnlinePlayers.ToArray();
            BitArray waitMask = new BitArray(onlinePlayers.Length);

            try
            {
                List<(SessionRecord, SessionRecord?)> records = new List<(SessionRecord, SessionRecord?)>(onlinePlayers.Length);
                List<Task> tasks = new List<Task>(onlinePlayers.Length);
                bool anyPrev = false;

                using IServiceScope scope = _serviceProvider.CreateScope();
                await using IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();

                for (int i = 0; i < onlinePlayers.Length; ++i)
                {
                    WarfarePlayer player = onlinePlayers[i];

                    SessionRecord record = StartCreatingSession(dbContext, player, startedGame, out SessionRecord? previousSession);
                    player.CurrentSession = record;
                    records.Add((record, previousSession));
                    anyPrev |= previousSession != null;

                    if (!lockpSync)
                        continue;

                    Task task = player.PurchaseSync.WaitAsync(token);
                    if (task.IsCompletedSuccessfully)
                    {
                        waitMask[i] = true;
                        continue;
                    }

                    int index = i; // i will increment
                    tasks.Add(Task.Run(async () =>
                    {
                        await task.ConfigureAwait(false);
                        waitMask[index] = true;
                        L.LogDebug($"[SESSIONS]   Done waiting for purchase sync for player {player.Steam64}.");
                    }, token));

                    L.LogDebug($"[SESSIONS] Waiting for purchase sync for player {player.Steam64}.");
                }

                if (tasks.Count > 0 && lockpSync)
                    await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach ((SessionRecord record, SessionRecord? _) in records)
                {
                    await dbContext.AddAsync(record, token).ConfigureAwait(false);
                    if (record.Kit != null)
                        dbContext.Remove(record.Kit);
                }

                await dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                if (anyPrev)
                {
                    foreach ((SessionRecord record, SessionRecord? previous) in records)
                    {
                        if (previous == null)
                            continue;
                        previous.NextSessionId = record.SessionId;
                        dbContext.Update(previous);
                    }

                    await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
                }
            }
            finally
            {
                for (int i = 0; i < onlinePlayers.Length; ++i)
                {
                    if (waitMask[i])
                        onlinePlayers[i].PurchaseSync.Release();
                }
            }

            L.LogDebug("[SESSIONS] Created sessions for all players.");
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private SessionRecord StartCreatingSession(IGameDataDbContext dbContext, WarfarePlayer player, bool startedGame, out SessionRecord? previous)
    {
        GameThread.AssertCurrent();
        SessionRecord record;
        lock (_sessions)
        {
            _sessions.TryGetValue(player.Steam64, out previous);

            KitPlayerComponent kitComp = player.Component<KitPlayerComponent>();

            record = new SessionRecord
            {
                Steam64 = player.Steam64,
                StartedTimestamp = DateTimeOffset.UtcNow,
                FactionId = player.Team.Faction?.PrimaryKey,
                PreviousSessionId = previous?.SessionId,
                NextSessionId = null,
                GameId = Data.Gamemode.GameId,
                StartedGame = startedGame,
                KitId = kitComp.ActiveKitKey,
                KitName = kitComp.ActiveKitId,
                MapId = MapScheduler.Current,
                SeasonId = UCWarfare.Season,
                SquadName = player.Squad?.Name,
                SquadLeader = player.Squad?.Leader?.Steam64,
                FinishedGame = false,
                Team = player.Team.Id,
                UnexpectedTermination = true
            };

            if (previous is { UnexpectedTermination: true })
                EndSession(dbContext, previous, startedGame, false);

            _sessions[player.Steam64] = record;
        }

        return record;
    }
    private static void EndSession(IGameDataDbContext dbContext, SessionRecord record, bool endGame, bool update = true)
    {
        record.EndedTimestamp = DateTimeOffset.UtcNow;
        record.FinishedGame = endGame;
        record.UnexpectedTermination = false;

        if (!update)
            return;

        dbContext.Update(record);
        FixupSession(dbContext, record);

        L.LogDebug($"[SESSIONS] Ended session {record.SessionId} for {record.Steam64}.");
    }
    private void OnSquadChanged(WarfarePlayer player, Squad? oldsquad, Squad? newsquad, bool oldisleader, bool newisleader)
    {
        if (player.IsLeaving || !player.HasInitedOnce)
            return;

        if (Data.Gamemode.State is not State.Staging and not State.Active || player.IsInitializing)
        {
            L.LogDebug($"[SESSIONS] Skipped creating session for {player.Steam64}. (gamemode initializing)");
            return;
        }

        if (IsSessionExpired(player))
        {
            UCWarfare.RunTask(RestartSession, player, true, false, CancellationToken.None);
            L.LogDebug($"[SESSIONS] Creating session for {player.Steam64}. (squad changed)");
        }
        else
        {
            L.LogDebug($"[SESSIONS] Skipping creating session for {player.Steam64}. (squad changed)");
        }
    }
    private void OnKitChanged(WarfarePlayer player, Kit? kit, Kit? oldkit)
    {
        if (!player.HasInitedOnce)
            return;

        if (Data.Gamemode.State is not State.Staging and not State.Active || player.IsInitializing)
        {
            L.LogDebug($"[SESSIONS] Skipped creating session for {player.Steam64}. (gamemode initializing)");
            return;
        }

        if (IsSessionExpired(player))
        {
            UCWarfare.RunTask(RestartSession, player, true, false, CancellationToken.None);
            L.LogDebug($"[SESSIONS] Creating session for {player.Steam64}. (kit changed)");
        }
        else
        {
            L.LogDebug($"[SESSIONS] Skipping creating session for {player.Steam64}. (kit changed)");
        }
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (!e.Player.HasInitedOnce)
            return;

        if (Data.Gamemode.State is not State.Staging and not State.Active || e.Player.IsInitializing)
        {
            L.LogDebug($"[SESSIONS] Skipped creating session for {e.Steam64}. (gamemode initializing)");
            return;
        }

        if (IsSessionExpired(e.Player))
        {
            UCWarfare.RunTask(RestartSession, e.Player, true, false, CancellationToken.None);
            L.LogDebug($"[SESSIONS] Creating session for {e.Steam64}. (group changed)");
        }
        else
        {
            L.LogDebug($"[SESSIONS] Skipping creating session for {e.Player.Steam64}. (group changed)");
        }
    }
    private void OnGamemodeChanged()
    {
        UCWarfare.RunTask(RestartSessionsForAll, true, true, CancellationToken.None);
        L.LogDebug("[SESSIONS] Creating session for all players. (gamemode changed)");
    }
    private static bool IsSessionExpired(WarfarePlayer player)
    {
        SessionRecord? currentSession = player.CurrentSession;
        if (currentSession == null)
            return player.IsOnline;

        if (player.ActiveKit != currentSession.KitId)
            return true;

        FactionInfo? faction = player.Faction;

        if (faction == null != !currentSession.FactionId.HasValue || faction != null && currentSession.FactionId.HasValue && faction.PrimaryKey.Key != currentSession.FactionId!.Value)
            return true;

        if (Data.Gamemode is not null && Data.Gamemode.GameId != currentSession.GameId)
            return true;

        if (player.Squad != null)
        {
            if (!currentSession.SquadLeader.HasValue || currentSession.SquadLeader.Value != player.Squad.Leader.Steam64)
                return true;

            if (currentSession.SquadName == null || !currentSession.SquadName.Equals(player.Squad.Name, StringComparison.Ordinal))
                return true;
        }
        else if (currentSession.SquadLeader.HasValue || currentSession.SquadName != null)
            return true;


        return false;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        SessionRecord record;
        lock (_sessions)
        {
            if (!_sessions.TryRemove(e.Steam64.m_SteamID, out record))
                return;
        }

        Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                await using IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>();

                EndSession(dbContext, record, Data.Gamemode != null && Data.Gamemode.State is State.Finished or State.Discarded, false);
                await SaveSession(dbContext, record, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling player disconnect.");
            }
        });
    }

    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(WarfarePlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        if (Data.Gamemode is null)
            return;

        if (!wasAlreadyOnline && IsSessionExpired(player))
        {
            L.LogDebug($"[SESSIONS] Creating session for {player.Steam64}. (initing)");
            await RestartSession(player, false, false, token);
        }
        else
        {
            L.LogDebug($"[SESSIONS] Skipping creating session for {player.Steam64}. (initing)");
        }
    }
    private static void FixupSession(IGameDataDbContext dbContext, SessionRecord session)
    {
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
    private static async Task SaveSession(IGameDataDbContext dbContext, SessionRecord session, CancellationToken token = default)
    {
        dbContext.Update(session);
        FixupSession(dbContext, session);
        await dbContext.SaveChangesAsync(token).ConfigureAwait(false);
    }
    /// <summary>
    /// Fixes the dates on any sessions that didn't get terminated properly (server crashed, etc). Accurate to +/- 10 seconds.
    /// </summary>
    public async Task CheckForTerminatedSessions(CancellationToken token = default)
    {
        DateTimeOffset? lastHeartbeat = ServerHeartbeatTimer.GetLastBeat();
        if (lastHeartbeat.HasValue)
        {
            L.Log($"Server last online: {lastHeartbeat.Value} ({(DateTime.UtcNow - lastHeartbeat.Value.UtcDateTime):g} ago). Checking for sessions that were terminated unexpectedly.", ConsoleColor.Magenta);

            int ct;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            await using (IGameDataDbContext dbContext = scope.ServiceProvider.GetRequiredService<WarfareDbContext>())
            {
                byte region = UCWarfare.Config.RegionKey;
                List<SessionRecord> records = await dbContext.Sessions.Where(x => x.UnexpectedTermination && x.EndedTimestamp == null && x.Game.Region == region).ToListAsync(token);

                ct = records.Count;

                foreach (SessionRecord record in records)
                {
                    record.EndedTimestamp = lastHeartbeat.Value;
                    record.LengthSeconds = (lastHeartbeat.Value.UtcDateTime - record.StartedTimestamp.UtcDateTime).TotalSeconds;
                }

                dbContext.UpdateRange(records);
                await dbContext.SaveChangesAsync(CancellationToken.None);
            }

            L.Log($"Migrated {ct} session(s) after server didn't shut down cleanly.", ConsoleColor.Magenta);
        }
        else
            L.Log("Unknown last server heartbeat.", ConsoleColor.Magenta);
    }
}