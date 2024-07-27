using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Proximity;

namespace Uncreated.Warfare.Zones;
public class MapZoneProvider : IZoneProvider
{
    public ValueTask<IEnumerable<IProximity>> GetZones(CancellationToken token = default)
    {
        return new ValueTask<IEnumerable<IProximity>>(GetZonesIntl());
    }

    private IEnumerable<IProximity> GetZonesIntl()
    {
        string mapPath = Path.GetFullPath(Level.info.path + "/Uncreated/zones.json");
        if (!File.Exists(mapPath))
        {
            return Enumerable.Empty<IProximity>();
        }

        using FileStream stream = new FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (stream.Length is > int.MaxValue or 0)
        {
            return Enumerable.Empty<IProximity>();
        }

        int length = (int)stream.Length;

        byte[] bytes = new byte[length];
        length = stream.Read(bytes, 0, bytes.Length);

        Utf8JsonReader reader = new Utf8JsonReader(bytes.AsSpan(0, length), ConfigurationSettings.JsonReaderOptions);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartObject)
                continue;


        }

        
    }
}