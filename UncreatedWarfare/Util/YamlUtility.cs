using System;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Uncreated.Warfare.Util;

public static class YamlUtility
{
    /// <inheritdoc cref="CheckMatchesMapFilter(string,string)"/>
    public static bool CheckMatchesMapFilter(string filePath)
    {
        return CheckMatchesMapFilter(filePath, Provider.map);
    }

    /// <summary>
    /// Look for a Map or Maps property in the given yaml document and compare it a given map.
    /// </summary>
    /// <remarks><see langword="true"/> if either there is no map filter or if the given map is included in the map filter, otherwise <see langword="false"/>.</remarks>
    public static bool CheckMatchesMapFilter(string filePath, string map)
    {
        return CheckMatchesMapFilterAndReadWeight(filePath, map, out _);
    }

    /// <inheritdoc cref="CheckMatchesMapFilterAndReadWeight(string,string,out double)"/>
    public static bool CheckMatchesMapFilterAndReadWeight(string filePath, out double weight)
    {
        return CheckMatchesMapFilterAndReadWeight(filePath, Provider.map, out weight);
    }

    /// <summary>
    /// Look for a Map or Maps property in the given yaml document and compare it to the current map, and read a weight (defaulting to 1).
    /// </summary>
    /// <remarks><see langword="true"/> if either there is no map filter or if the current map is included in the map filter, otherwise <see langword="false"/>.</remarks>
    public static bool CheckMatchesMapFilterAndReadWeight(string filePath, string map, out double weight)
    {
        try
        {
            using StreamReader streamReader = new StreamReader(filePath);
            YamlStream stream = new YamlStream();
            stream.Load(streamReader);

            weight = 1;

            YamlNode? yamlRoot = stream.Documents.FirstOrDefault()?.RootNode;
            if (yamlRoot is not YamlMappingNode yaml)
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
                    if (nodePair.Value is not YamlScalarNode { Value: { } mapFilter })
                    {
                        continue;
                    }

                    if (!mapFilter.Equals("all", StringComparison.OrdinalIgnoreCase)
                        && !mapFilter.Equals(map, StringComparison.OrdinalIgnoreCase))
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

                    if (!sequence.Any(val => val is YamlScalarNode { Value: { } mapFilter } &&
                                            (mapFilter.Equals("all", StringComparison.OrdinalIgnoreCase)
                                             || mapFilter.Equals(map, StringComparison.OrdinalIgnoreCase))
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
        catch (IOException ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            weight = 0;
            return false;
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, $"Error checking {filePath} for a map filter.");
            weight = 0;
            return false;
        }
    }
}