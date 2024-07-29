using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Proximity;
using UnityEngine;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Represents a zone or cluster of zones linked with their <see cref="IProximity"/> instances.
/// </summary>
public class ActiveZoneCluster
{
    private readonly ZoneProximity[] _zones;
    private readonly int _primaryIndex;

    /// <summary>
    /// List of all zones in this cluster.
    /// </summary>
    public IReadOnlyList<ZoneProximity> Zones { get; }

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
    public ref readonly ZoneProximity Primary => ref _zones[_primaryIndex];
    internal ActiveZoneCluster(ZoneProximity[] zones)
    {
        if (zones.Length == 0)
            throw new ArgumentException("A zone group must consist of at least one zone.", nameof(zones));

        _zones = zones;
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

        _primaryIndex = primaryIndex;

        Zone primary = Primary.Zone;

        Name = primary.Name;
        ShortName = primary.ShortName;
    }

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
}

public readonly struct ZoneProximity(IProximity proximity, Zone zone)
{
    public IProximity Proximity { get; } = proximity;
    public Zone Zone { get; } = zone;
}