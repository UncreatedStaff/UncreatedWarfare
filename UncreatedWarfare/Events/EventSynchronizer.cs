using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles syncing OnXxxRequested events so that only one can run at a time.
/// </summary>
public class EventSynchronizer : IDisposable
{
    private readonly ILogger<EventSynchronizer> _logger;

    private readonly SynchronizationGroup _globalGroup;
    private readonly PlayerDictionary<SynchronizationGroup> _playerGroups;
    private readonly List<SynchronizationEntry> _globalPlayerEntries;

    private int _timeoutCheckIndex;
    private bool _hasUpdatedHandler;

    public EventSynchronizer(ILogger<EventSynchronizer> logger, IPlayerService playerService)
    {
        _globalGroup = new SynchronizationGroup(null);
        _playerGroups = new PlayerDictionary<SynchronizationGroup>(96);
        _globalPlayerEntries = new List<SynchronizationEntry>(16);

        foreach (WarfarePlayer player in playerService.GetThreadsafePlayerList())
        {
            _playerGroups.Add(player, new SynchronizationGroup(player));
        }

        _logger = logger;

        if (!WarfareModule.IsActive)
            return;
        
        TimeUtility.updated += OnUpdate;
        _hasUpdatedHandler = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_hasUpdatedHandler)
            return;

        _hasUpdatedHandler = false;
        TimeUtility.updated -= OnUpdate;
    }

    // occasionally check for timeouts, spreading the groups out between frames
    private void OnUpdate()
    {
        DateTime now = DateTime.UtcNow;
        if (_playerGroups.Count > 0)
        {
            ++_timeoutCheckIndex;
            if (_timeoutCheckIndex >= _playerGroups.Count)
            {
                _globalGroup.CheckForTimeouts(_logger, now);
                _timeoutCheckIndex = 0;
            }
            else
            {
                _playerGroups.Values.ElementAt(_timeoutCheckIndex).CheckForTimeouts(_logger, now);
            }

            return;
        }

        ++_timeoutCheckIndex;
        if (_timeoutCheckIndex > 10)
        {
            _globalGroup.CheckForTimeouts(_logger, now);
            _timeoutCheckIndex = 0;
        }
    }

    internal void CheckForAllTimeouts()
    {
        DateTime now = DateTime.UtcNow;
        foreach (SynchronizationGroup group in _playerGroups.Values)
        {
            group.CheckForTimeouts(_logger, now);
        }

        _globalGroup.CheckForTimeouts(_logger, now);
    }

    /*
     *
     *  Globally syncronized events need to lock in a few ways:
     *   1. if they have any tags, lock only on those tags + for all players
     *   2. else lock on the type + for all players, new players should inherit global locks
     *
     *  IPlayerEvent models can also be synchronized per player:
     *   1. if they have any tags, lock on the player
     *   2. else lock on the type for the player
     */
    
    /// <summary>
    /// Wait for an event to be able to start and lock the necessary buckets.
    /// </summary>
    internal UniTask<SynchronizationEntry?> EnterEvent<TEventArgs>(TEventArgs args) where TEventArgs : class
    {
        // used for tests
        return EnterEvent(args, 0, typeof(TEventArgs).GetAttributeSafe<EventModelAttribute>());
    }

    /// <summary>
    /// Wait for an event to be able to start and lock the necessary buckets.
    /// </summary>
    internal UniTask<SynchronizationEntry?> EnterEvent<TEventArgs>(TEventArgs args, long eventId, EventModelAttribute? modelInfo) where TEventArgs : class
    {
        GameThread.AssertCurrent();

        if (modelInfo == null)
            return UniTask.FromResult<SynchronizationEntry?>(null);

        switch (modelInfo.SynchronizationContext)
        {
            case EventSynchronizationContext.PerPlayer:

                if (args is IPlayerEvent)
                    return EnterPerPlayerEvent(args, modelInfo, eventId);

                // PerPlayers should implement IPlayerEvent
                _logger.LogWarning($"Model {args.GetType()} has PerPlayer synchronization mode but doesn't implement {typeof(IPlayerEvent)}.");
                goto case EventSynchronizationContext.Global;

            case EventSynchronizationContext.Global:

                return EnterGlobalEvent(args, modelInfo, eventId);
        }

        return UniTask.FromResult<SynchronizationEntry?>(null);
    }

    private UniTask<SynchronizationEntry?> EnterGlobalEvent<TEventArgs>(TEventArgs args, EventModelAttribute modelInfo, long eventId) where TEventArgs : class
    {
        SynchronizationEntry entry = new SynchronizationEntry(args, modelInfo, eventId);

        DateTime now = DateTime.UtcNow;

        _globalGroup.EnterEvent(entry, now, _logger);

        foreach (SynchronizationGroup group in _playerGroups.Values)
        {
            group.EnterEvent(entry, now, _logger);
        }

        _globalPlayerEntries.Add(entry);

        return entry.WaitEvent?.Task ?? UniTask.FromResult<SynchronizationEntry?>(entry);
    }

    private UniTask<SynchronizationEntry?> EnterPerPlayerEvent<TEventArgs>(TEventArgs args, EventModelAttribute modelInfo, long eventId) where TEventArgs : class
    {
        SynchronizationEntry entry = new SynchronizationEntry(args, modelInfo, eventId);

        WarfarePlayer player = ((IPlayerEvent)args).Player;

        DateTime now = DateTime.UtcNow;

        SynchronizationGroup group = GetOrCreatePlayerGroup(player, now);

        group.EnterEvent(entry, now, _logger);

        return entry.WaitEvent?.Task ?? UniTask.FromResult<SynchronizationEntry?>(entry);
    }

    private SynchronizationGroup GetOrCreatePlayerGroup(WarfarePlayer player, DateTime now)
    {
        if (_playerGroups.TryGetValue(player, out SynchronizationGroup? group))
        {
            return group;
        }

        group = new SynchronizationGroup(player);
        foreach (SynchronizationEntry entry in _globalPlayerEntries)
        {
            group.EnterEvent(entry, now, _logger);
        }

        _playerGroups.Add(player, group);
        return group;
    }

    /// <summary>
    /// Tell <see cref="EventSynchronizer"/> this event has been completed. This MUST be ran no matter how the event ends.
    /// </summary>
    /// <remarks>Continues events remaining in the queues.</remarks>
    internal void ExitEvent(SynchronizationEntry entry)
    {
        // used for tests
        ExitEvent(entry, entry.Model.GetType().GetAttributeSafe<EventModelAttribute>());
    }

    /// <summary>
    /// Tell <see cref="EventSynchronizer"/> this event has been completed. This MUST be ran no matter how the event ends.
    /// </summary>
    /// <remarks>Continues events remaining in the queues.</remarks>
    internal void ExitEvent(SynchronizationEntry entry, EventModelAttribute? modelInfo)
    {
        GameThread.AssertCurrent();

        if (modelInfo == null)
            return;

        object args = entry.Model;
        switch (modelInfo.SynchronizationContext)
        {
            case EventSynchronizationContext.Global:

                _globalGroup.ExitEvent(args, _logger);

                List<ulong>? playersToRemove = null;

                foreach (SynchronizationGroup playerGroup in _playerGroups.Values)
                {
                    if (!playerGroup.ExitEvent(args, _logger))
                        continue;

                    playersToRemove ??= ListPool<ulong>.claim();
                    playersToRemove.Add(playerGroup.Player!.Steam64.m_SteamID);
                }

                // remove empty groups from players that are offline
                if (playersToRemove != null)
                {
                    foreach (ulong s64 in playersToRemove)
                    {
                        _playerGroups.Remove(s64);
                    }

                    ListPool<ulong>.release(playersToRemove);
                }

                _globalPlayerEntries.Remove(entry);
                break;

            case EventSynchronizationContext.PerPlayer:

                if (args is not IPlayerEvent pe)
                    goto case EventSynchronizationContext.Global;

                WarfarePlayer player = pe.Player;
                if (!_playerGroups.TryGetValue(player.Steam64, out SynchronizationGroup? grp))
                {
                    break;
                }

                if (grp.ExitEvent(args, _logger))
                {
                    _playerGroups.Remove(player);
                }

                break;
        }
    }
}

internal class SynchronizationGroup
{
    public readonly WarfarePlayer? Player;

    public Dictionary<string, SynchronizationBucket> Tags = new Dictionary<string, SynchronizationBucket>(32, StringComparer.Ordinal);
    public Dictionary<Type, SynchronizationBucket> Types = new Dictionary<Type, SynchronizationBucket>(32);

    public SynchronizationGroup(WarfarePlayer? player)
    {
        Player = player;
    }

    public void EnterEvent(SynchronizationEntry entry, DateTime now, ILogger logger)
    {
        string[]? tags = entry.ModelInfo.SynchronizedModelTags;
        
        if (tags is { Length: > 0 })
        {
            foreach (string tag in tags)
            {
                if (!Tags.TryGetValue(tag, out SynchronizationBucket? bkt))
                {
                    Tags.Add(tag, bkt = new SynchronizationBucket($"Tag: \"{tag}\" for {Player?.ToString() ?? "Global"}"));
                }

                bkt.EnterEvent(entry, now, logger);
            }
        }
        else
        {
            if (!Types.TryGetValue(entry.ModelType, out SynchronizationBucket? bkt))
            {
                Types.Add(entry.ModelType, bkt = new SynchronizationBucket($"Type: \"{entry.ModelType.Name}\" for {Player?.ToString() ?? "Global"}"));
            }

            bkt.EnterEvent(entry, now, logger);
        }
    }

    /// <returns>If the group is empty and can be removed.</returns>
    public bool ExitEvent(object args, ILogger logger)
    {
        bool canBeRemoved = Player is { IsDisconnected: true };
        bool any = false;
        foreach (SynchronizationBucket bucket in Tags.Values)
        {
            any |= bucket.ExitEvent(args, logger);
            if (canBeRemoved && bucket.Current != null)
                canBeRemoved = false;
        }

        if (any && !canBeRemoved)
            return false;

        foreach (SynchronizationBucket bucket in Types.Values)
        {
            if (canBeRemoved && bucket.Current != null)
                canBeRemoved = false;
            if (!any && bucket.ExitEvent(args, logger))
                break;
        }

        return canBeRemoved;
    }

    public void CheckForTimeouts(ILogger logger, DateTime now)
    {
        foreach (SynchronizationBucket bucket in Tags.Values)
        {
            bucket.CheckForTimeout(logger, now, null);
        }
    }
}

internal class SynchronizationBucket
{
    private readonly string _context;
    public Queue<SynchronizationEntry> Queue = new Queue<SynchronizationEntry>(2);
    public SynchronizationEntry? Current;

    public SynchronizationBucket(string context)
    {
        _context = context;
    }

    private static readonly TimeSpan MaxTimeout = TimeSpan.FromSeconds(15);

    public void CheckForTimeout(ILogger logger, DateTime now, SynchronizationEntry? newEntry)
    {
        do
        {
            if (Current == null || now - Current.CreateTime <= MaxTimeout)
                return;

            logger.LogWarning($"Timeout reached in bucket {_context} from" +
                              $" {Current.ModelType.Name} (#{Current.EventId}): {now - Current.CreateTime}. " +
                              $"(adding entry: {newEntry?.ModelType.Name}) " +
                              $"Create time: {Current.CreateTime} UTC."
            );

            if (Queue.Count == 0)
            {
                logger.LogTrace($"Queueing {newEntry?.EventId} after timeout, none to dequeue in {_context}.");
                Current = newEntry;
                if (newEntry != null && newEntry.CreateTime < now)
                    newEntry.CreateTime = now;
                return;
            }

            SynchronizationEntry nextEntry = Queue.Dequeue();
            logger.LogTrace($"Exiting dequeued event {Current?.EventId}, dequeued {nextEntry.EventId} to current in {_context}.");
            --nextEntry.WaitCount;
            Current = nextEntry;
            if (nextEntry.WaitCount <= 0)
            {
                nextEntry.WaitEvent?.TrySetResult(nextEntry);
            }

        } while (true);
    }


    public void EnterEvent(SynchronizationEntry entry, DateTime now, ILogger logger)
    {
        if (Current == null)
        {
            Current = entry;
            logger.LogTrace($"Entered current event {entry.EventId} in {_context}.");
            return;
        }

        CheckForTimeout(logger, now, entry);
        if (Current == entry)
            return;

        ++entry.WaitCount;
        entry.WaitEvent ??= new UniTaskCompletionSource<SynchronizationEntry?>();
        logger.LogTrace($"Enqueued event {entry.EventId} in {_context}.");
        Queue.Enqueue(entry);
    }

    public bool ExitEvent(object args, ILogger logger)
    {
        SynchronizationEntry? entry = Current;
        if (entry?.Model != args)
            return false;

        if (!Queue.TryDequeue(out SynchronizationEntry? newEntry))
        {
            logger.LogTrace($"Exiting dequeued event {Current?.EventId}, none to dequeue in {_context}.");
            Current = null;
            return true;
        }

        --newEntry.WaitCount;
        logger.LogTrace($"Exiting dequeued event {Current?.EventId}, dequeued {newEntry.EventId} to current in {_context}.");
        Current = newEntry;
        if (newEntry.WaitCount <= 0)
        {
            newEntry.WaitEvent?.TrySetResult(newEntry);
        }

        return true;
    }
}

internal class SynchronizationEntry
{
    public readonly object Model;
    public readonly Type ModelType;
    public readonly EventModelAttribute ModelInfo;
    public readonly long EventId;

    public DateTime CreateTime;
    public int WaitCount;
    public UniTaskCompletionSource<SynchronizationEntry?>? WaitEvent;

    public SynchronizationEntry(object model, EventModelAttribute modelInfo, long eventId)
    {
        ModelType = model.GetType();
        Model = model;
        ModelInfo = modelInfo;
        EventId = eventId;
        CreateTime = DateTime.UtcNow;
    }
}