using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Util;

/// <summary>
/// A thread-safe collection of ticks associated with players. Used to calculate player contribution to a collective task.
/// </returns>
public class PlayerContributionTracker : IEnumerable<PlayerWork>
{
    private readonly Dictionary<CSteamID, PlayerWork> _contributions;
    private float _totalWorkDone;
    private bool _anyDuplicateSessions;

    /// <summary>
    /// Total number of ticks in the collection.
    /// </returns>
    public float TotalWorkDone
    {
        get
        {
            lock (_contributions)
                return _totalWorkDone;
        }
    }

    public PlayerContributionTracker() : this(4) { }
    public PlayerContributionTracker(int capacity)
    {
        _contributions = new Dictionary<CSteamID, PlayerWork>(capacity);
    }

    public int NumberOfContributors
    {
        get
        {
            lock (_contributions)
                return _contributions.Count;
        }
    }

    public void RetrieveLock() => Monitor.Enter(_contributions);
    public void ReturnLock() => Monitor.Exit(_contributions);

    public float GetWorkDoneNoLock() => _totalWorkDone;

    /// <returns>The percentage of the work <paramref name="player"/> has done (from 0 to 1).</returns>
    public float GetContributionPercentage(CSteamID player)
    {
        lock (_contributions)
        {
            if (_contributions.TryGetValue(player, out PlayerWork work))
                return work.WorkPoints / _totalWorkDone;
        }

        return 0f;
    }
    public float GetContributionPercentage(CSteamID player, DateTime after)
    {
        lock (_contributions)
        {
            if (_contributions.TryGetValue(player, out PlayerWork work) && work.LastUpdated >= after)
            {
                float totalWorkDoneAfterSpecifiedTime = _contributions.Values.Where(w => w.LastUpdated >= after).Sum(w => w.WorkPoints);

                return work.WorkPoints / totalWorkDoneAfterSpecifiedTime;
            }
        }

        return 0f;
    }
    /// <returns>A <see cref="PlayerWork"/> indicating how much work the player <paramref name="player"/> has contributed.</returns>
    public PlayerWork? GetContribution(CSteamID player)
    {
        if (_contributions.TryGetValue(player, out PlayerWork work))
            return work;

        return null;
    }
    /// <returns>A <see cref="PlayerWork"/> indicating how much work the player <paramref name="player"/> has contributed.</returns>
    public PlayerWork? GetContribution(CSteamID player, DateTime after)
    {
        if (_contributions.TryGetValue(player, out PlayerWork work) && work.LastUpdated >= after)
            return work;

        return null;
    }
    /// <summary>
    /// Increment a player's work points by <paramref name="workPoints"/>.
    /// </summary>
    public void RecordWork(CSteamID player, float workPoints, DateTime? timePerformed = null)
    {
        lock (_contributions)
        {
            _totalWorkDone += workPoints;

            if (_contributions.TryGetValue(player, out PlayerWork work))
            {
                _contributions[player] = new PlayerWork(player, work.WorkPoints + workPoints, timePerformed ?? DateTime.Now);
            }
            else
            {
                _contributions[player] = new PlayerWork(player, workPoints, timePerformed ?? DateTime.Now);
            }
        }
    }
    public IEnumerable<CSteamID> Contributors => _contributions.Keys;

    public IEnumerator<PlayerWork> GetEnumerator() => _contributions.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public readonly struct PlayerWork
{
    public readonly CSteamID PlayerId;
    public readonly float WorkPoints;
    public readonly DateTime LastUpdated;
    public PlayerWork(CSteamID playerId, float ticks)
    {
        PlayerId = playerId;
        WorkPoints = ticks;
        LastUpdated = DateTime.Now;
    }
    public PlayerWork(CSteamID steam64, float workPoints, DateTime lastUpdated)
    {
        PlayerId = steam64;
        WorkPoints = workPoints;
        LastUpdated = lastUpdated;
    }
}