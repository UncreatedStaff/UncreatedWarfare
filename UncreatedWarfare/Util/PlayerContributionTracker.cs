using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Util;

/// <summary>
/// A thread-safe collection of ticks associated with players. Used to calculate player contribution to a collective task.
/// </returns>
public class PlayerContributionTracker : IEnumerable<PlayerWork>
{
    private PlayerWork[] _work;
    private int _workCount;
    private float _totalWorkDone;

    /// <summary>
    /// Total number of ticks in the collection.
    /// </returns>
    public float TotalWorkDone => _totalWorkDone;

    public PlayerContributionTracker() : this(4) { }
    public PlayerContributionTracker(int capacity)
    {
        _work = new PlayerWork[capacity];
    }

    public int ContributorCount => _workCount;

    public void Clear()
    {
        lock (_work)
        {
            _workCount = 0;
            Array.Clear(_work, 0, _work.Length);
        }
    }

    /// <returns>The percentage of the work <paramref name="player"/> has done (from 0 to 1).</returns>
    public float GetContributionPercentage(CSteamID player)
    {
        float contribution = GetContribution(player, out float total);
        return total != 0 ? contribution / total : 0;
    }

    public float GetContributionPercentage(CSteamID player, DateTime after)
    {
        float contribution = GetContribution(player, after, out float total);
        return total != 0 ? contribution / total : 0;
    }

    /// <returns>
    /// A <see cref="PlayerWork"/> indicating how much work the player <paramref name="player"/> has contributed.
    /// </returns>
    public float GetContribution(CSteamID player, out float total)
    {
        total = _totalWorkDone;
        for (int i = 0; i < _workCount; ++i)
        {
            ref PlayerWork w = ref _work[i];
            if (w.PlayerId.m_SteamID != player.m_SteamID)
                continue;
            
            lock (_work)
            {
                return w.WorkPoints;
            }
        }

        return 0f;
    }

    /// <returns>
    /// A <see cref="PlayerWork"/> indicating how much work the player <paramref name="player"/> has contributed.
    /// </returns>
    public float GetContribution(CSteamID player, DateTime after, out float total)
    {
        after = after.ToUniversalTime();
        float totalPoints = 0;
        float points = 0;
        lock (_work)
        {
            for (int i = 0; i < _workCount; ++i)
            {
                ref PlayerWork w = ref _work[i];
                if (w.LastUpdated < after || w.PlayerId.m_SteamID == 0)
                    continue;

                totalPoints += w.WorkPoints;
                if (w.PlayerId.m_SteamID == player.m_SteamID)
                    points = w.WorkPoints;
                break;
            }
        }

        total = totalPoints;
        return points;
    }

    /// <summary>
    /// Increment a player's work points by <paramref name="workPoints"/>.
    /// </summary>
    public void RecordWork(CSteamID player, float workPoints, DateTime? timePerformed = null)
    {
        DateTime time = timePerformed?.ToUniversalTime() ?? DateTime.UtcNow;
        lock (_work)
        {
            _totalWorkDone += workPoints;

            for (int i = 0; i < _workCount; ++i)
            {
                ref PlayerWork w = ref _work[i];
                if (w.PlayerId.m_SteamID != player.m_SteamID)
                    continue;

                w.WorkPoints += workPoints;
                w.LastUpdated = time;
                return;
            }
            
            if (_workCount >= _work.Length)
            {
                PlayerWork[] newWork = new PlayerWork[_work.Length * 2];
                lock (newWork)
                {
                    _work = newWork;
                    _work[_workCount] = new PlayerWork(player, workPoints, time);
                    ++_workCount;
                    return;
                }
            }

            _work[_workCount] = new PlayerWork(player, workPoints, time);
            ++_workCount;
        }
    }

    public ContributorEnumerator Contributors => new ContributorEnumerator(this);
    public PlayerWorkEnumerator GetEnumerator() => new PlayerWorkEnumerator(this);

    IEnumerator<PlayerWork> IEnumerable<PlayerWork>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct ContributorEnumerator : IEnumerator<CSteamID>, IEnumerable<CSteamID>
    {
        private readonly PlayerContributionTracker _tracker;
        private int _index;

        /// <inheritdoc />
        public CSteamID Current { get; private set; }

        public ContributorEnumerator(PlayerContributionTracker tracker)
        {
            _tracker = tracker;
        }

        public bool MoveNext()
        {
            PlayerWork work;
            do
            {
                ++_index;
                if (_index >= _tracker._workCount)
                    return false;

                work = _tracker._work[_index];
            } while (work.PlayerId.m_SteamID == 0);

            Current = work.PlayerId;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        object IEnumerator.Current => Current;

        public ContributorEnumerator GetEnumerator()
        {
            ContributorEnumerator n = this;
            n._index = -1;
            return n;
        }
        
        IEnumerator<CSteamID> IEnumerable<CSteamID>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose() { }
    }

    public struct PlayerWorkEnumerator : IEnumerator<PlayerWork>
    {
        private readonly PlayerContributionTracker _tracker;
        private int _index;

        /// <inheritdoc />
        public PlayerWork Current { get; private set; }

        public PlayerWorkEnumerator(PlayerContributionTracker tracker)
        {
            _tracker = tracker;
        }

        public bool MoveNext()
        {
            PlayerWork work;
            do
            {
                ++_index;
                if (_index >= _tracker._workCount)
                    return false;

                work = _tracker._work[_index];
            } while (work.PlayerId.m_SteamID == 0);

            Current = work;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        object IEnumerator.Current => Current;
        public void Dispose() { }
    }
}

public struct PlayerWork
{
    public readonly CSteamID PlayerId;
    public float WorkPoints;
    public DateTime LastUpdated;
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