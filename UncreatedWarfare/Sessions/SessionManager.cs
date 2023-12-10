using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Database;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.GameData;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Singletons;

// ReSharper disable InconsistentlySynchronizedField

namespace Uncreated.Warfare.Sessions;
public class SessionManager : BaseAsyncSingleton, IPlayerConnectListenerAsync, IPlayerDisconnectListener, IPlayerPostInitListenerAsync
{
    private readonly ConcurrentDictionary<ulong, SessionRecord> _sessions = new ConcurrentDictionary<ulong, SessionRecord>();
    private readonly UCSemaphore _semaphore = new UCSemaphore(0, 1);

    public override bool AwaitLoad => true;
    protected override async Task LoadAsync(CancellationToken token)
    {
        _semaphore.Release();
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        await RestartSessionsForAll(false, token);

        KitManager.OnManualKitChanged += OnKitChanged;
        EventDispatcher.GroupChanged += OnGroupChanged;
        Gamemode.OnGamemodeChanged += OnGamemodeChanged;
    }
    protected override async Task UnloadAsync(CancellationToken token)
    {
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        Gamemode.OnGamemodeChanged -= OnGamemodeChanged;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        KitManager.OnManualKitChanged -= OnKitChanged;

        KeyValuePair<ulong, SessionRecord>[] sessions;
        lock (_sessions)
        {
            sessions = _sessions.ToArray();
            _sessions.Clear();
        }
        foreach (KeyValuePair<ulong, SessionRecord> session in sessions)
        {
            EndSession(session.Value, true);
        }

        L.Log($"[SESSIONS] Finalizing session(s): {sessions.Length}.", ConsoleColor.Magenta);
        await WarfareDatabases.GameData.WaitAsync(token);
        try
        {
            await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WarfareDatabases.GameData.Release();
        }
    }
    public async Task<SessionRecord> RestartSession(UCPlayer player, bool startedGame, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            await player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
            try
            {
#if DEBUG
                using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
                await UCWarfare.ToUpdate(token);
                SessionRecord record = StartCreatingSession(player, startedGame, out SessionRecord? previousSession);
                player.CurrentSession = record;
                await WarfareDatabases.GameData.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    await WarfareDatabases.GameData.AddAsync(record, token).ConfigureAwait(false);
                    await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);

                    if (previousSession != null)
                    {
                        previousSession.NextSessionId = record.SessionId;
                        WarfareDatabases.GameData.Update(record);
                        await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    WarfareDatabases.GameData.Release();
                }

                L.LogDebug($"[SESSIONS] Created session {record.SessionId} for: {player}.");
                return record;
            }
            finally
            {
                player.PurchaseSync.Release();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    public async Task RestartSessionsForAll(bool startedGame, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
#if DEBUG
            using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
            await UCWarfare.ToUpdate(token);

            UCPlayer[] onlinePlayers = PlayerManager.OnlinePlayers.ToArray();
            BitArray waitMask = new BitArray(onlinePlayers.Length);

            try
            {
                List<(SessionRecord, SessionRecord?)> records =
                    new List<(SessionRecord, SessionRecord?)>(onlinePlayers.Length);
                List<Task> tasks = new List<Task>();
                bool anyPrev = false;

                for (int i = 0; i < onlinePlayers.Length; ++i)
                {
                    UCPlayer player = onlinePlayers[i];

                    SessionRecord record =
                        StartCreatingSession(player, startedGame, out SessionRecord? previousSession);
                    player.CurrentSession = record;
                    records.Add((record, previousSession));
                    anyPrev |= previousSession != null;

                    Task task = player.PurchaseSync.WaitAsync(token);
                    if (task.IsCompleted)
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

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks).ConfigureAwait(false);

                await WarfareDatabases.GameData.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    foreach ((SessionRecord record, SessionRecord? _) in records)
                        await WarfareDatabases.GameData.AddAsync(record, token).ConfigureAwait(false);

                    await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);

                    if (anyPrev)
                    {
                        foreach ((SessionRecord record, SessionRecord? previous) in records)
                        {
                            if (previous == null)
                                continue;
                            previous.NextSessionId = record.SessionId;
                            WarfareDatabases.GameData.Update(previous);
                        }

                        await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    WarfareDatabases.GameData.Release();
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
    private SessionRecord StartCreatingSession(UCPlayer player, bool startedGame, out SessionRecord? previous)
    {
        ThreadUtil.assertIsGameThread();
        SessionRecord record;
        lock (_sessions)
        {
            _sessions.TryGetValue(player.Steam64, out previous);
            record = new SessionRecord
            {
                Steam64 = player.Steam64,
                StartedTimestamp = DateTimeOffset.UtcNow,
                FactionId = player.Faction?.PrimaryKey,
                PreviousSessionId = previous?.SessionId,
                PreviousSession = previous,
                NextSessionId = null,
                GameId = Data.Gamemode.GameId,
                StartedGame = startedGame,
                KitId = player.ActiveKit,
                MapId = MapScheduler.Current,
                SeasonId = UCWarfare.Season,
                FinishedGame = false,
                Team = (byte)player.GetTeam(),
                UnexpectedTermination = true
            };

            if (previous is { UnexpectedTermination: true })
                EndSession(previous, startedGame, false);

            _sessions[player.Steam64] = record;
        }

        return record;
    }
    private static void EndSession(SessionRecord record, bool endGame, bool update = true)
    {
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        record.EndedTimestamp = DateTimeOffset.UtcNow;
        record.FinishedGame = endGame;
        record.UnexpectedTermination = false;

        if (!update)
            return;

        WarfareDatabases.GameData.Wait();
        try
        {
            WarfareDatabases.GameData.Update(record);
        }
        finally
        {
            WarfareDatabases.GameData.Release();
        }

        L.LogDebug($"[SESSIONS] Ended session {record.SessionId} for {record.Steam64}.");
    }
    private void OnKitChanged(UCPlayer player, Kit? kit, Kit? oldkit)
    {
        if (Data.Gamemode.State is not (State.Staging or State.Active) || player.IsInitializing)
        {
            L.LogDebug($"[SESSIONS] Skipped creating session for {player.Steam64}. (gamemode initializing)");
            return;
        }
        
        UCWarfare.RunTask(RestartSession, player, false, CancellationToken.None);
        L.LogDebug($"[SESSIONS] Creating session for {player.Steam64}. (kit changed)");
    }
    private void OnGroupChanged(GroupChanged e)
    {
        if (Data.Gamemode.State is not (State.Staging or State.Active) || e.Player.IsInitializing)
        {
            L.LogDebug($"[SESSIONS] Skipped creating session for {e.Steam64}. (gamemode initializing)");
            return;
        }

        UCWarfare.RunTask(RestartSession, e.Player, false, CancellationToken.None);
        L.LogDebug($"[SESSIONS] Creating session for {e.Steam64}. (group changed)");
    }
    private void OnGamemodeChanged()
    {
        UCWarfare.RunTask(RestartSessionsForAll, true, CancellationToken.None);
        L.LogDebug("[SESSIONS] Creating session for all players. (gamemode changed)");
    }

    async Task IPlayerConnectListenerAsync.OnPlayerConnecting(UCPlayer player, CancellationToken token)
    {
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode is null)
            return;

        await RestartSession(player, false, token);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        SessionRecord record;
        lock (_sessions)
        {
            if (!_sessions.TryRemove(player.Steam64, out record))
                return;
        }
        
        EndSession(record, Data.Gamemode != null && Data.Gamemode.State is State.Finished or State.Discarded, false);
        UCWarfare.RunTask(SaveSession, record, CancellationToken.None, ctx: "Save session after player disconnects.");
    }
    async Task IPlayerPostInitListenerAsync.OnPostPlayerInit(UCPlayer player, CancellationToken token)
    {
#if DEBUG
        using IDisposable disposable = ProfilingUtils.StartTracking();
#endif
        if (Data.Gamemode is null)
            return;

        await RestartSession(player, false, token);
    }
    private static async Task SaveSession(SessionRecord session, CancellationToken token = default)
    {
        await WarfareDatabases.GameData.WaitAsync(token).ConfigureAwait(false);
        try
        {
            WarfareDatabases.GameData.Update(session);
            await WarfareDatabases.GameData.SaveChangesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            WarfareDatabases.GameData.Release();
        }
    }

}