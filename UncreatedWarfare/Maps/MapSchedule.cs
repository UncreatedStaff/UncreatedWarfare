using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Maps;

public class MapScheduler : BaseAlternateConfigurationFile
{
    private readonly ILogger<MapScheduler> _logger;
    private readonly int _configuredMap;
    private readonly string? _configuredMapNameOverride;

    private List<ulong>? _originalMods;
    private List<ulong>? _originalIgnoreChildren;

    private readonly MapConfiguration[] _maps;

    // these are just fallback values so the file isn't required to boot.
    private static readonly MapConfiguration[] DefaultMapRotation =
    [
        new MapConfiguration { Name = "Fool's Road",   WorkshopId = 2407566267, RequiredDependencies = [ 2407740920 ] },
        new MapConfiguration { Name = "Goose Bay",     WorkshopId = 2301006771, RequiredDependencies = [ 2407740920 ] },
        new MapConfiguration { Name = "Nuijamaa",      WorkshopId = 2557112412, RequiredDependencies = [ 2407740920 ] },
        new MapConfiguration { Name = "Gulf of Aqaba", WorkshopId = 2726964335, RequiredDependencies = [ 2407740920 ] },
        new MapConfiguration { Name = "Changbai Shan", WorkshopId = 2943688379, RequiredDependencies = [ 2407740920 ] },
        new MapConfiguration { Name = "Yellowknife",   WorkshopId = 3456355722, RequiredDependencies = [ 2407740920 ] }
    ];

    /// <summary>
    /// The index of the current map in <see cref="DefaultMapRotation"/>. Lines up with the primary key in the season info database.
    /// </summary>
    public int Current { get; private set; } = -1;

    /// <summary>
    /// Number of maps in rotation.
    /// </summary>
    public static int MapCount => DefaultMapRotation.Length;

    public MapScheduler(IConfiguration systemConfiguration, ILogger<MapScheduler> logger) : base("Maps.yml", reload: false)
    {
        _logger = logger;

        IConfigurationSection section = GetSection("Maps");

        MapConfiguration[]? maps = section.Get<MapConfiguration[]>();

        if (maps == null || maps.Length == 0)
        {
            maps = DefaultMapRotation;
        }

        _maps = maps;

        string? mapName = systemConfiguration["map"];

        int map = FindMap(mapName);
        if (map < 0)
        {
            throw new InvalidOperationException("Map not configured or doesn't match an existing map.");
        }

                                     // this is so map names can be hidden from source code
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
    public int FindMap(string? mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
            return -1;

        for (int i = 0; i < DefaultMapRotation.Length; ++i)
        {
            MapConfiguration map = DefaultMapRotation[i];
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
        return index < 0 || index >= DefaultMapRotation.Length
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : DefaultMapRotation[index].Name;
    }

    private void LoadMap(int index)
    {
        MapConfiguration map = _maps[index];
        if (Level.info != null)
        {
            throw new InvalidOperationException("Map has already been loaded, too late to change maps.");
        }

        Provider.map = _configuredMapNameOverride ?? map.Name;
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();

        config.File_IDs = _originalMods?.ToList() ?? new List<ulong>();
        config.Ignore_Children_File_IDs = _originalIgnoreChildren?.ToList() ?? new List<ulong>();

        if (map.RequiredDependencies != null)
        {
            for (int i = 0; i < map.RequiredDependencies.Length; ++i)
            {
                ulong mod = map.RequiredDependencies[i];
                for (int j = 0; j < config.File_IDs.Count; ++j)
                {
                    if (config.File_IDs[j] == mod) goto c;
                }

                _logger.LogInformation("Added {0} to the workshop queue.", mod);
                config.File_IDs.Add(mod);
                c:;
            }
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
}
