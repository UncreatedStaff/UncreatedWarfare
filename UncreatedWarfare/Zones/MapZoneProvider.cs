using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Reads zones from the current map at '[MapFolder]/Uncreated/zones.json'.
/// </summary>
public class MapZoneProvider : IZoneProvider
{
    public ValueTask<IEnumerable<Zone>> GetZones(CancellationToken token = default)
    {
        return new ValueTask<IEnumerable<Zone>>(GetZonesIntl());
    }

    private static IEnumerable<Zone> GetZonesIntl()
    {
        string mapPath = Path.GetFullPath(Level.info.path + "/Uncreated/zones.json");
        if (!File.Exists(mapPath))
        {
            return Enumerable.Empty<Zone>();
        }

        List<Zone>? zones;

        using (FileStream fs = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            zones = JsonSerializer.Deserialize<ZoneConfig>(fs, ConfigurationSettings.JsonSerializerSettings)?.Zones;
        }

        if (zones is not { Count: > 0 })
            return Enumerable.Empty<Zone>();

        foreach (Zone zone in zones)
        {
            if (zone.IsPrimary)
                continue;

            Zone? primary = zones.FirstOrDefault(x => x.IsPrimary && x.Name.Equals(zone.Name, StringComparison.Ordinal));
            if (primary == null)
            {
                zone.IsPrimary = true;
                break;
            }

            zone.Faction = primary.Faction;
            zone.Name = primary.Name;
            zone.ShortName = primary.ShortName;
            zone.GridObjects = primary.GridObjects;
            zone.Spawn = primary.Spawn;
            zone.SpawnYaw = primary.SpawnYaw;
            zone.Type = primary.Type;
            zone.UpstreamZones = primary.UpstreamZones;
        }

        return zones;
    }

    [UsedImplicitly]
    private class ZoneConfig
    {
        [JsonPropertyName("zones")]
        public List<Zone>? Zones { get; init; }
    }
}