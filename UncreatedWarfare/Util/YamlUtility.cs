using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Maps;
using YamlDotNet.RepresentationModel;

namespace Uncreated.Warfare.Util;
public static class YamlUtility
{
    /// <summary>
    /// Look for a Map or Maps property in the given yaml document and compare it to the current map.
    /// </summary>
    /// <remarks><see langword="true"/> if either there is no map filter or if the current map is included in the map filter, otherwise <see langword="false"/>.</remarks>
    public static bool CheckMatchesMapFilter(string filePath)
    {
        return CheckMatchesMapFilterAndReadWeight(filePath, out _);
    }

    /// <summary>
    /// Look for a Map or Maps property in the given yaml document and compare it to the current map, and read a weight (defaulting to 1).
    /// </summary>
    /// <remarks><see langword="true"/> if either there is no map filter or if the current map is included in the map filter, otherwise <see langword="false"/>.</remarks>
    public static bool CheckMatchesMapFilterAndReadWeight(string filePath, out double weight)
    {
        using StreamReader streamReader = new StreamReader(filePath);
        YamlStream stream = new YamlStream();
        stream.Load(streamReader);

        weight = 1;

        if (stream.Documents.FirstOrDefault()?.RootNode is not YamlMappingNode yaml)
        {
            return true;
        }

        bool wasFilteredOut = false;
        foreach (KeyValuePair<YamlNode, YamlNode> nodePair in yaml.Children)
        {
            if (nodePair.Key is not YamlScalarNode scalar)
                continue;

            if (string.Equals(scalar.Value, "Map", StringComparison.OrdinalIgnoreCase))
            {
                if (nodePair.Value is not YamlScalarNode { Value: { } map })
                {
                    continue;
                }

                if (!map.Equals("all", StringComparison.OrdinalIgnoreCase)
                    && !map.Equals(Provider.map, StringComparison.OrdinalIgnoreCase)
                    && (!int.TryParse(map, NumberStyles.Number, CultureInfo.InvariantCulture, out int mapId) || mapId != MapScheduler.Current))
                {
                    wasFilteredOut = true;
                    break;
                }
            }
            else if (string.Equals(scalar.Value, "Maps", StringComparison.OrdinalIgnoreCase))
            {
                if (nodePair.Value is not YamlSequenceNode sequence)
                {
                    continue;
                }

                if (!sequence.Any(val => val is YamlScalarNode { Value: { } map } &&
                                        (map.Equals("all", StringComparison.OrdinalIgnoreCase)
                                         || map.Equals(Provider.map, StringComparison.OrdinalIgnoreCase)
                                         || int.TryParse(map, NumberStyles.Number, CultureInfo.InvariantCulture, out int mapId) && mapId == MapScheduler.Current)
                                        ))
                {
                    wasFilteredOut = true;
                    break;
                }
            }
            else if (string.Equals(scalar.Value, "Weight", StringComparison.InvariantCultureIgnoreCase)
                     && nodePair.Value is YamlScalarNode { Value: { } weightStr }
                     && double.TryParse(weightStr, NumberStyles.Number, CultureInfo.InvariantCulture, out double readWeight))
            {
                weight = readWeight;
            }
        }

        return !wasFilteredOut;
    }
}
