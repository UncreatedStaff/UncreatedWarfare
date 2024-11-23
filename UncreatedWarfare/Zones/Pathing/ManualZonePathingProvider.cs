using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts.Phases;

namespace Uncreated.Warfare.Zones.Pathing;
public class ManualZonePathingProvider : IZonePathingProvider
{
    private readonly ILogger<ManualZonePathingProvider> _logger;
    private readonly ZoneStore _zones;

    public string[]? Zones { get; set; }

    public ManualZonePathingProvider(ILogger<ManualZonePathingProvider> logger, ZoneStore zones)
    {
        _logger = logger;
        _zones = zones;
    }

    public UniTask<IList<Zone>> CreateZonePathAsync(ILayoutPhase forPhase, CancellationToken token = default)
    {
        string[]? zoneNames = Zones;
        if (zoneNames == null || zoneNames.Length == 0)
        {
            _logger.LogError("Unable to detect the seed zone from the available zones. One main needs upstream zones defined, which will act as the seed zone.");
            Fail(forPhase);
        }

        List<Zone> zones = new List<Zone>(zoneNames!.Length);

        for (int i = 0; i < zoneNames.Length; ++i)
        {
            string zoneName = zoneNames[i];

            Zone? zone = _zones.Zones.FirstOrDefault(zone => zone.IsPrimary && zone.Name.Equals(zoneName, StringComparison.Ordinal));
            if (zone == null)
            {
                _logger.LogError("There is no zone by the name \"{0}\" (#{1}).", zoneName.Length, i);
                Fail(forPhase);
            }

            if (zones.Contains(zone!))
            {
                _logger.LogError("Duplicate zone \"{0}\" (#{1}) in zone list.", zoneName.Length, i);
                Fail(forPhase);
            }

            zones.Add(zone!);
        }

        if (zones.Count <= 2 || zones[0].Type != ZoneType.MainBase || zones[^1].Type != ZoneType.MainBase)
        {
            _logger.LogError("The first and last zones must be main bases to dictate the order of the zones.");
            Fail(forPhase);
        }

        return new UniTask<IList<Zone>>(zones);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [ContractAnnotation("=> halt")]
    private static void Fail(ILayoutPhase phase)
    {
        throw new LayoutConfigurationException(phase, "Failed to create a path using ManualZonePathingProvider.");
    }
}