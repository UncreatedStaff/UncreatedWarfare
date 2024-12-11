using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.FOBs.Construction;

/// <summary>
/// A thread-safe collection of ticks associated with players. Used to calculate player contribution to a collective task.
/// </returns>
public class TickResponsibilityCollection : IEnumerable<TickResponsibility>
{
    private readonly List<TickResponsibility> _list;
    private float _total;
    private bool _anyDuplicateSessions;

    /// <summary>
    /// Total number of ticks in the collection.
    /// </returns>
    public float Ticks
    {
        get
        {
            lock (_list)
                return _total;
        }
    }

    public TickResponsibilityCollection() : this(4) { }
    public TickResponsibilityCollection(int capacity)
    {
        _list = new List<TickResponsibility>(capacity);
    }

    public int Count
    {
        get
        {
            lock (_list)
                return _list.Count;
        }
    }

    public void RetrieveLock() => Monitor.Enter(_list);
    public void ReturnLock() => Monitor.Exit(_list);

    public float GetTicksNoLock() => _total;

    /// <returns>The percentage of the work <paramref name="player"/> has done (from 0 to 1).</returns>
    public float this[IPlayer player] => this[player.Steam64.m_SteamID];

    /// <returns>The percentage of the work <paramref name="steam64"/> has done (from 0 to 1).</returns>
    public float this[ulong steam64]
    {
        get
        {
            lock (_list)
            {
                for (int i = 0; i < _list.Count; ++i)
                {
                    if (_list[i].Steam64 == steam64)
                        return _list[i].Ticks / _total;
                }
            }

            return 0f;
        }
    }

    /// <returns>The percentage of the work <paramref name="player"/> has done (from 0 to 1).</returns>
    public float this[IPlayer player, ulong sessionId] => this[player.Steam64.m_SteamID, sessionId];

    /// <returns>The percentage of the work <paramref name="steam64"/> has done (from 0 to 1).</returns>
    public float this[ulong steam64, ulong sessionId]
    {
        get
        {
            lock (_list)
            {
                for (int i = 0; i < _list.Count; ++i)
                {
                    TickResponsibility responsibility = _list[i];
                    if (responsibility.Steam64 == steam64 && responsibility.SessionId == sessionId)
                        return responsibility.Ticks / _total;
                }
            }

            return 0f;
        }
    }

    public float GetLastUpdatedTime(ulong steam64)
    {
        lock (_list)
        {
            float min = float.NaN;
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility responsibility = _list[i];
                if (responsibility.Steam64 != steam64)
                    continue;

                if (float.IsNaN(min) || min > responsibility.LastUpdated)
                    min = responsibility.LastUpdated;
            }

            return float.IsNaN(min) ? 0f : min;
        }
    }

    /// <summary>
    /// Adds a player to the list, or increment their tick count by <paramref name="ticks"/>.
    /// </summary>
    public void Increment(ulong player, float ticks, ulong sessionId)
    {
        lock (_list)
        {
            _total += ticks;
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility r = _list[i];
                if (r.Steam64 != player)
                    continue;

                if (r.SessionId != sessionId)
                {
                    _anyDuplicateSessions = true;
                    continue;
                }

                _list[i] = new TickResponsibility(player, sessionId, r.Ticks + ticks);
                return;
            }

            _list.Add(new TickResponsibility(player, sessionId, ticks));
        }
    }
    /// <summary>
    /// Adds a player to the list, or set their tick count by <paramref name="ticks"/>.
    /// </summary>
    public void Set(ulong player, float ticks, ulong sessionId)
    {
        lock (_list)
        {
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility r = _list[i];
                if (r.Steam64 != player)
                    continue;

                if (r.SessionId != sessionId)
                {
                    _anyDuplicateSessions = true;
                    continue;
                }

                _total += ticks - r.Ticks;
                _list[i] = new TickResponsibility(player, sessionId, ticks);
                return;
            }

            _total += ticks;
            _list.Add(new TickResponsibility(player, sessionId, ticks));
        }
    }
    public void Clear()
    {
        lock (_list)
        {
            _total = 0;
            _list.Clear();
        }
    }
    /// <summary>
    /// Remove a player's contribution (effectively setting it to 0).
    /// </returns>
    public bool Remove(IPlayer player) => Remove(player.Steam64.m_SteamID);
    /// <summary>
    /// Remove a player's contribution (effectively setting it to 0).
    /// </returns>
    public bool Remove(ulong player)
    {
        bool any = false;
        lock (_list)
        {
            for (int i = _list.Count - 1; i >= 0; --i)
            {
                if (_list[i].Steam64 != player)
                    continue;

                _total -= _list[i].Ticks;
                _list.RemoveAt(i);
                any = true;
            }
        }

        return any;
    }
    /// <summary>
    /// Remove a player's contribution (effectively setting it to 0).
    /// </returns>
    public bool Remove(IPlayer player, ulong sessionId) => Remove(player.Steam64.m_SteamID, sessionId);
    /// <summary>
    /// Remove a player's contribution (effectively setting it to 0).
    /// </returns>
    public bool Remove(ulong player, ulong sessionId)
    {
        bool any = false;
        lock (_list)
        {
            for (int i = _list.Count - 1; i >= 0; --i)
            {
                TickResponsibility responsibility = _list[i];
                if (responsibility.Steam64 != player || responsibility.SessionId != sessionId)
                    continue;

                _total -= responsibility.Ticks;
                _list.RemoveAt(i);
                any = true;
            }
        }

        return any;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<TickResponsibility> GetEnumerator()
    {
        lock (_list)
            return _list.GetEnumerator();
    }
    public IEnumerable<TickResponsibility> GetGroupedEnumerator()
    {
        lock (_list)
        {
            if (!_anyDuplicateSessions)
                return _list;

            TickResponsibility[] totals = new TickResponsibility[_list.Count];
            int c = -1;
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility res = _list[i];

                bool found = false;
                for (int j = 0; j <= c; ++j)
                {
                    ref TickResponsibility res2 = ref totals[i];
                    if (res.Steam64 != res2.Steam64)
                        continue;

                    res2 = new TickResponsibility(res.Steam64, 0, res2.Ticks + res.Ticks, Mathf.Max(res.LastUpdated, res2.LastUpdated));
                    found = true;
                    break;
                }

                if (!found)
                    totals[++c] = res;
            }

            // ReSharper disable NotDisposedResourceIsReturned
            return c != totals.Length - 1 ? totals.Take(c + 1) : totals;

            // ReSharper restore NotDisposedResourceIsReturned
        }
    }
}

public readonly struct TickResponsibility
{
    public readonly ulong Steam64;
    public readonly ulong SessionId;
    public readonly float Ticks;
    public readonly float LastUpdated;
    public TickResponsibility(ulong steam64, ulong sessionId, float ticks)
    {
        Steam64 = steam64;
        SessionId = sessionId;
        Ticks = ticks;
        LastUpdated = Time.realtimeSinceStartup;
    }
    public TickResponsibility(ulong steam64, ulong sessionId, float ticks, float lastUpdated)
    {
        Steam64 = steam64;
        SessionId = sessionId;
        Ticks = ticks;
        LastUpdated = lastUpdated;
    }
}