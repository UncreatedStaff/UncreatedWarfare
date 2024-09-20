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

        Utf8JsonReader reader;
        using (Utf8JsonPreProcessingStream stream = new Utf8JsonPreProcessingStream(mapPath))
        {
            if (stream.Length is > int.MaxValue or 0)
            {
                return Enumerable.Empty<Zone>();
            }

            reader = new Utf8JsonReader(stream.ReadAllBytes(), ConfigurationSettings.JsonReaderOptions);
        }

        List<Zone>? zones = JsonSerializer.Deserialize<ZoneConfig>(ref reader, ConfigurationSettings.JsonSerializerSettings)?.Zones;

        return zones is not { Count: > 0 } ? Enumerable.Empty<Zone>() : zones;
    }

    private class ZoneConfig
    {
        [JsonPropertyName("zones")]
        public List<Zone>? Zones { get; init; }
    }
}