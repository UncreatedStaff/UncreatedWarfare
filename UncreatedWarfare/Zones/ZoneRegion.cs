using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Represents a zone or cluster of zones linked with their <see cref="IProximity"/> instances.
/// </summary>
public class ZoneRegion : IDisposable
{
    private readonly ZoneProximity[] _zones;
    private readonly ITeamManager<Team> _teamManager;
    private ZoneRegionData? _data;
    private bool _disposed;
    private readonly TrackingList<WarfarePlayer> _players = new TrackingList<WarfarePlayer>(8);
    private TeamCountTable? _teamCounts;

    /// <summary>
    /// Invoked when a player goes in proximity of any zone in the cluster.
    /// </summary>
    public event Action<ZoneRegion, WarfarePlayer>? OnPlayerEntered;

    /// <summary>
    /// Invoked when a player leaves proximity of any zone in the cluster.
    /// </summary>
    public event Action<ZoneRegion, WarfarePlayer>? OnPlayerExited;

    /// <summary>
    /// List of all players currently inside the zone.
    /// </summary>
    public ReadOnlyTrackingList<WarfarePlayer> Players { get; }

    /// <summary>
    /// List of all zones in this cluster.
    /// </summary>
    public IReadOnlyList<ZoneProximity> Zones { get; }

    /// <summary>
    /// Keeps track of how many players are on the flag for each team.
    /// </summary>
    public TeamCountTable TeamCounts
    {
        get
        {
            if (_teamCounts != null)
                return _teamCounts;

            if (_disposed)
                throw new ObjectDisposedException(nameof(ZoneRegion));

            lock (_players)
            {
                if (_teamCounts != null)
                    return _teamCounts;

                _teamCounts = new TeamCountTable(_teamManager.AllTeams);
                foreach (WarfarePlayer player in Players)
                {
                    if (player.Team.IsValid)
                        _teamCounts.IncrementPoints(player.Team, 1d);
                }
            }

            return _teamCounts;
        }
    }

    /// <summary>
    /// Abstract data linked to a zone.
    /// </summary>
    public ZoneRegionData Data
    {
        get
        {
            if (_data != null)
                return _data;

            if (_disposed)
                throw new ObjectDisposedException(nameof(ZoneRegion));

            ZoneRegionData data = new ZoneRegionData(this);
            if (Interlocked.CompareExchange(ref _data, data, null) == null)
            {
                data.Dispose();
            }

            return _data;
        }
    }

    /// <summary>
    /// The shared name of the cluster of zones.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The shared short name of the cluster of zones.
    /// </summary>
    public string? ShortName { get; }

    /// <summary>
    /// The zone marked as primary of this group.
    /// </summary>
    [JsonIgnore]
    public ref readonly ZoneProximity Primary => ref _zones[PrimaryIndex];

    /// <summary>
    /// The index of the primary zone in <see cref="Zones"/>.
    /// </summary>
    public int PrimaryIndex { get; }

    /// <summary>
    /// Total number of zones in this cluster.
    /// </summary>
    public int Count => _zones.Length;

    internal ZoneRegion(ZoneProximity[] zones, ITeamManager<Team> teamManager)
    {
        if (zones.Length == 0)
            throw new ArgumentException("A zone group must consist of at least one zone.", nameof(zones));

        Players = new ReadOnlyTrackingList<WarfarePlayer>(_players);

        // move primary to the front of the array
        for (int i = 1; i < zones.Length; ++i)
        {
            if (!zones[i].Zone.IsPrimary)
                continue;

            (zones[0], zones[i]) = (zones[i], zones[0]);
            break;
        }

        _zones = zones;
        _teamManager = teamManager;
        Zones = new ReadOnlyCollection<ZoneProximity>(_zones);

        int primaryIndex = -1;
        for (int i = 0; i < zones.Length; ++i)
        {
            if (!zones[i].Zone.IsPrimary)
                continue;

            if (primaryIndex != -1)
                throw new ArgumentException("A zone group must consist of exactly one primary zone.", nameof(zones));

            primaryIndex = i;
        }

        if (primaryIndex == -1)
        {
            throw new ArgumentException("A zone group must consist of exactly one primary zone.", nameof(zones));
        }

        PrimaryIndex = primaryIndex;

        Zone primary = Primary.Zone;

        Name = primary.Name;
        ShortName = primary.ShortName;

        for (int i = 0; i < zones.Length; ++i)
        {
            if (zones[i].Proximity is not ITrackingProximity<WarfarePlayer> proximity)
                continue;

            proximity.OnObjectEntered += OnObjectEnteredAnyZone;
            proximity.OnObjectExited += OnObjectExitedAnyZone;
        }
    }

    private void OnObjectExitedAnyZone(IEventBasedProximity<WarfarePlayer> prox, WarfarePlayer player)
    {
        // check to make sure they're not already in another part of the cluster
        if (_zones.Length > 0)
        {
            bool isInAnotherZone = false;
            for (int i = 0; i < _zones.Length; ++i)
            {
                if (_zones[i].Proximity is not ITrackingProximity<WarfarePlayer> proximity || !proximity.Contains(player))
                    continue;

                isInAnotherZone = true;
                break;
            }

            if (isInAnotherZone)
            {
                if (!_players.AddIfNotExists(player))
                    return;

                OnPlayerEntered?.Invoke(this, player);
                UpdateTeamCounts();

                return;
            }
        }

        if (!_players.Remove(player))
            return;

        OnPlayerExited?.Invoke(this, player);
        UpdateTeamCounts();
    }

    private void OnObjectEnteredAnyZone(IEventBasedProximity<WarfarePlayer> prox, WarfarePlayer player)
    {

        if (!_players.AddIfNotExists(player))
            return;

        OnPlayerEntered?.Invoke(this, player);
        UpdateTeamCounts();
    }

    private void UpdateTeamCounts()
    {
        if (_teamCounts == null)
            return;

        lock (_players)
        {
            _teamCounts.ClearScores();
            foreach (WarfarePlayer player in Players)
            {
                if (player.Team.IsValid)
                    _teamCounts.IncrementPoints(player.Team, 1d);
            }
        }
    }

    /// <summary>
    /// Check if a position is within the proximity.
    /// </summary>
    public bool TestPoint(Vector3 position)
    {
        for (int i = 0; i < _zones.Length; ++i)
        {
            ref ZoneProximity proximity = ref _zones[i];
            if (proximity.Proximity.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a position is within the proximity while ignoring Y position.
    /// </summary>
    public bool TestPoint(Vector2 position)
    {
        for (int i = 0; i < _zones.Length; ++i)
        {
            ref ZoneProximity proximity = ref _zones[i];
            if (proximity.Proximity.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }

    // clean-up colliders and GameObjects
    public void Dispose()
    {
        if (GameThread.IsCurrent)
        {
            DisposeIntl();
        }
        else
        {
            ZoneRegionData? data = Interlocked.Exchange(ref _data, null);
            data?.Dispose();
            if (_disposed)
                return;

            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                DisposeIntl();
            });
        }
    }

    private void DisposeIntl()
    {
        ZoneRegionData? data = Interlocked.Exchange(ref _data, null);

        lock (_players)
            _teamCounts = null;

        data?.Dispose();
        if (_disposed)
            return;

        _disposed = true;
        for (int i = 0; i < _zones.Length; ++i)
        {
            if (_zones[i].Proximity is IEventBasedProximity<WarfarePlayer> proximity)
            {
                proximity.OnObjectEntered -= OnObjectEnteredAnyZone;
                proximity.OnObjectExited -= OnObjectExitedAnyZone;
            }

            switch (_zones[i].Proximity)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;

                case Object component when component != null:
                    Object.Destroy(component);
                    break;
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ShortName != null ? $"{Name} [{ShortName}]" : Name;
    }
}

/// <summary>
/// Links zone info with it's <see cref="IProximity"/> instance.
/// </summary>
public readonly struct ZoneProximity(IProximity proximity, Zone zone)
{
    public IProximity Proximity { get; } = proximity;
    public Zone Zone { get; } = zone;
}