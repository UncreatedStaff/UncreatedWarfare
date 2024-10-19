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

        return zones is not { Count: > 0 } ? Enumerable.Empty<Zone>() : zones;
    }

    private class ZoneConfig
    {
        [JsonPropertyName("zones")]
        public List<Zone>? Zones { get; init; }
    }
}