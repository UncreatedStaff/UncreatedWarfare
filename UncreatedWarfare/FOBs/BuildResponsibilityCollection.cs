using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Uncreated.Warfare.FOBs;
/// <summary>
/// A thread-safe collection of ticks associated with players. Used to calculate player contribution to a collective task.
/// </returns>
public class TickResponsibilityCollection : IEnumerable<TickResponsibility>
{
    private readonly List<TickResponsibility> _list;
    private float _total;

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
    public float this[IPlayer player] => this[player.Steam64];

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

    public float GetLastUpdatedTime(ulong steam64)
    {
        lock (_list)
        {
            for (int i = 0; i < _list.Count; ++i)
            {
                if (_list[i].Steam64 == steam64)
                    return _list[i].LastUpdated;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Adds a player to the list, or increment their tick count by <paramref name="ticks"/>.
    /// </summary>
    public void Increment(ulong player, float ticks)
    {
        lock (_list)
        {
            _total += ticks;
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility r = _list[i];
                if (r.Steam64 == player)
                {
                    _list[i] = new TickResponsibility(player, r.Ticks + ticks);
                    return;
                }
            }

            _list.Add(new TickResponsibility(player, ticks));
        }
    }
    /// <summary>
    /// Adds a player to the list, or set their tick count by <paramref name="ticks"/>.
    /// </summary>
    public void Set(ulong player, float ticks)
    {
        lock (_list)
        {
            for (int i = 0; i < _list.Count; ++i)
            {
                TickResponsibility r = _list[i];
                if (r.Steam64 == player)
                {
                    _total += ticks - r.Ticks;
                    _list[i] = new TickResponsibility(player, ticks);
                    return;
                }
            }

            _total += ticks;
            _list.Add(new TickResponsibility(player, ticks));
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
    public bool Remove(IPlayer player) => Remove(player.Steam64);
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
                if (_list[i].Steam64 == player)
                {
                    _total -= _list[i].Ticks;
                    _list.RemoveAt(i);
                    any = true;
                    break;
                }
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
}

public readonly struct TickResponsibility
{
    public readonly ulong Steam64;
    public readonly float Ticks;
    public readonly float LastUpdated;
    public TickResponsibility(ulong steam64, float ticks)
    {
        Steam64 = steam64;
        Ticks = ticks;
        LastUpdated = Time.realtimeSinceStartup;
    }
}