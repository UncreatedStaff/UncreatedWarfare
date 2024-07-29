using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones.Pathing;

/// <summary>
/// Creates a path of zones from one base to another using upstream zones (previously called adjacencies), where each relationship is assigned a weight.
/// </summary>
public class UpstreamZonePathingProvider : IZonePathingProvider
{
    private readonly ZoneStore _zones;
    private readonly ILayoutPhase _phase;
    private readonly ILogger<UpstreamZonePathingProvider> _logger;

    public UpstreamZonePathingProvider(ZoneStore zones, ILayoutPhase phase, ILogger<UpstreamZonePathingProvider> logger)
    {
        _zones = zones;
        _phase = phase;
        _logger = logger;
    }

    public UniTask<IList<Zone>> CreateZonePathAsync(CancellationToken token = default)
    {
        Zone? seedZone = FindSeedZone();
        if (seedZone == null)
        {
            _logger.LogError("Unable to detect the seed zone from the available zones. One main needs upstream zones defined, which will act as the seed zone.");
            Fail();
        }

        List<Zone> outputList = new List<Zone>(8) { seedZone! };

        int nextIndex = ChooseUpstreamZone(seedZone!);
        while (_zones.Zones[nextIndex].Type != ZoneType.MainBase)
        {
            Zone zone = _zones.Zones[nextIndex];
            if (outputList.Contains(zone))
            {
                outputList.Add(zone);
                _logger.LogError("Circular reference detected in the following path: {{{0}}}.", string.Join(" -> ", outputList.Select(zone => zone.Name)));
                Fail();
            }

            outputList.Add(zone);
            nextIndex = ChooseUpstreamZone(zone);
        }

        outputList.Add(_zones.Zones[nextIndex]);

        if (outputList[^1] == outputList[0])
        {
            _logger.LogError("Zone path started and ended at the same main base from the following path: {{{0}}}.", string.Join(" -> ", outputList.Select(zone => zone.Name)));
            Fail();
        }

        if (outputList.Count < 3)
        {
            _logger.LogError("Zone path started was not able to create a path longer than just the two bases.");
            Fail();
        }

        return new UniTask<IList<Zone>>(outputList);
    }

    private int ChooseUpstreamZone(Zone zone)
    {
        if (zone.UpstreamZones.Count == 0)
        {
            _logger.LogError("Zone {0} doesn't have any upstream zones.", zone.Name);
            Fail();
        }

        int upstreamIndex = RandomUtility.GetIndex(zone.UpstreamZones, upstream => upstream.Weight);
        
        string zoneName = zone.UpstreamZones[upstreamIndex].ZoneName;
        int index = FindZoneIndex(zoneName);
        if (index == -1)
        {
            _logger.LogError("Unknown zone \"{0}\" in upstream list for zone {1}.", zoneName, zone.Name);
            Fail();
        }

        return index;
    }

    private int FindZoneIndex(string zoneName)
    {
        for (int i = 0; i < _zones.Zones.Count; ++i)
        {
            Zone zone = _zones.Zones[i];
            if (!zone.IsPrimary || !zoneName.Equals(zone.Name, StringComparison.Ordinal))
                continue;

            return i;
        }

        return -1;
    }

    /// <summary>
    /// Find the main base zone with upstream zones connected to it, this is the 'seed' zone.
    /// </summary>
    private Zone? FindSeedZone()
    {
        Zone? foundZone = null;
        foreach (Zone zone in _zones.Zones)
        {
            if (!zone.IsPrimary || zone.Type != ZoneType.MainBase || zone.Faction == null || zone.UpstreamZones.Count == 0)
                continue;

            if (foundZone != null)
                return null;

            foundZone = zone;
        }

        return foundZone;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Fail()
    {
        throw new LayoutConfigurationException(_phase, "Failed to create a path using UpstreamZonePathingProvider.");
    }
}