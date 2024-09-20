using DanielWillett.ReflectionTools;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Uncreated.Warfare.Maps;

[Priority(int.MaxValue)]
public class MapScheduler
{
    private readonly ILogger<MapScheduler> _logger;
    private readonly int _configuredMap;
    private readonly string? _configuredMapNameOverride;

    private List<ulong> _originalMods;
    private List<ulong> _originalIgnoreChildren;

    // todo this is bad and needs replaced eventually
    public static int CurrentStatic => WarfareModule.Singleton.ServiceProvider.Resolve<MapScheduler>().Current;

    // todo add to config
    private static readonly MapData[] MapRotation =
    [
        new MapData("Fool's Road",      [ 2407566267, 2407740920 ], removeChildren: [ 2407566267 ]),
        new MapData("Goose Bay",        [ 2301006771, 2407740920 ]),
        new MapData("Nuijamaa",         [ 2557112412, 2407740920 ]),
        new MapData("Gulf of Aqaba",    [ 2726964335, 2407740920 ]),
        new MapData("Changbai Shan",    [ 2943688379, 2407740920 ]),
        new MapData("S4Map" /* todo */, [ 2407740920 ])
    ];

    /// <summary>
    /// The index of the current map in <see cref="MapRotation"/>. Lines up with the primary key in the season info database.
    /// </summary>
    public int Current { get; private set; } = -1;

    /// <summary>
    /// Number of maps in rotation.
    /// </summary>
    public static int MapCount => MapRotation.Length;

    public MapScheduler(IConfiguration systemConfiguration, ILogger<MapScheduler> logger)
    {
        _logger = logger;

        string mapName = systemConfiguration["map"];

        int map = FindMap(mapName);
        if (map < 0)
        {
            throw new InvalidOperationException("Map not configured or doesn't match an existing map.");
        }

        _configuredMapNameOverride = systemConfiguration["map_name_override"];
        _configuredMap = map;
    }

    internal void ApplyMapSetting()
    {
        // save currently configured workshop items
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();
        _originalMods = config.File_IDs;
        _originalIgnoreChildren = config.Ignore_Children_File_IDs;

        _logger.LogInformation("Loading map {0}: \"{1}\".", _configuredMap, GetMapName(_configuredMap));

        LoadMap(_configuredMap);
    }

    /// <summary>
    /// Find the index of a map from it's name or ID as a string.
    /// </summary>
    public int FindMap(string mapName)
    {
        if (int.TryParse(mapName, NumberStyles.Number, CultureInfo.InvariantCulture, out int mapNumber) && mapNumber >= 0 && mapNumber < MapRotation.Length)
        {
            return mapNumber;
        }

        for (int i = 0; i < MapRotation.Length; ++i)
        {
            ref MapData map = ref MapRotation[i];
            if (map.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the name of a map from it's index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public string GetMapName(int index)
    {
        return index < 0 || index >= MapRotation.Length
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : MapRotation[index].Name;
    }

    private void LoadMap(int index)
    {
        ref MapData map = ref MapRotation[index];
        if (Level.info != null)
        {
            throw new InvalidOperationException("Map has already been loaded, too late to change maps.");
        }

        Provider.map = _configuredMapNameOverride ?? map.Name;
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();

        config.File_IDs = _originalMods.ToList();
        config.Ignore_Children_File_IDs = _originalIgnoreChildren.ToList();

        for (int i = 0; i < map.AddMods.Length; ++i)
        {
            ulong mod = map.AddMods[i];
            for (int j = 0; j < config.File_IDs.Count; ++j)
            {
                if (config.File_IDs[j] == mod) goto c;
            }

            _logger.LogInformation("Added {0} to the workshop queue.", mod);
            config.File_IDs.Add(mod);
            c:;
        }

        if (map.RemoveMods is not null)
        {
            for (int i = 0; i < map.RemoveMods.Length; ++i)
            {
                ulong mod = map.RemoveMods[i];
                for (int j = config.File_IDs.Count - 1; j >= 0; --j)
                {
                    if (config.File_IDs[j] != mod)
                        continue;

                    config.File_IDs.RemoveAt(j);
                    _logger.LogInformation("Removed {0} from the workshop queue.", mod);
                }
            }
        }

        if (map.RemoveChildren is not null)
        {
            config.Ignore_Children_File_IDs.AddRange(map.RemoveChildren);
        }

        Current = index;

        string baseGamePath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string expectedModPath = Path.Combine(baseGamePath, "Servers", Provider.serverID, "Workshop", "Steam", "content", Provider.APP_ID.m_AppId.ToString(CultureInfo.InvariantCulture));

        DirectoryInfo expectedModFolder = new DirectoryInfo(expectedModPath);
        if (!expectedModFolder.Exists)
            return;

        foreach (DirectoryInfo modFolder in expectedModFolder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            if (!ulong.TryParse(modFolder.Name, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong mod))
            {
                continue;
            }
            
            if (config.File_IDs.Contains(mod))
            {
                continue;
            }

            string displayPath = Path.GetRelativePath(baseGamePath, modFolder.FullName);
            try
            {
                modFolder.Delete(true);
                _logger.LogInformation("Deleted unused mod folder {0} from workshop directory: \"{1}\".", mod, displayPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to delete unused mod folder {0} from workshop directory: \"{1}\".", mod, displayPath);
            }
        }
    }

    private readonly struct MapData
    {
        /// <summary>Should the map be used in rotation?</summary>
        public readonly bool InRotation;
        /// <summary>Technical name of the map.</summary>
        public readonly string Name;
        /// <summary>Mods to add to the server.</summary>
        public readonly ulong[] AddMods;
        /// <summary>Mods to remove from the server (or null if there are none to remove).</summary>
        public readonly ulong[]? RemoveMods;
        /// <summary>Mods to remove from the server (or null if there are none to remove).</summary>
        public readonly ulong[]? RemoveChildren;
        public MapData(string name, ulong[] addMods, bool inRotation = true, ulong[]? removeMods = null, ulong[]? removeChildren = null)
        {
            Name = name;
            AddMods = addMods;
            InRotation = inRotation;
            RemoveMods = removeMods;
            RemoveChildren = removeChildren;
        }
    }
}
