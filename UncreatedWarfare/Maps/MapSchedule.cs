using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Uncreated.SQL;
using UnityEngine;

namespace Uncreated.Warfare.Maps;
internal class MapScheduler : MonoBehaviour
{
    internal static MapScheduler Instance;

    // todo
    internal static readonly Schema MAPS_TABLE = new Schema("map_data", new Schema.Column[]
    {
        new Schema.Column("MapId", SqlTypes.INT)
        {
            PrimaryKey = true
        },
        new Schema.Column("Display Name", SqlTypes.STRING_255),
        new Schema.Column("Faction_1", "varchar(16)"),
        new Schema.Column("Faction_2", "varchar(16)")
    }, true, null);
                                 // intentional is
    public static int Current => Instance is null ? -1 : Instance._map;
    // active map
    private int _map = -1;
    private const int STATIC_MAP = 3;

    /* MAP DATA */
    private static readonly List<MapData> mapRotation = new List<MapData>()
    {
        new MapData("Fool's Road",      new ulong[] { 2407566267, 2407740920 }, removeChildren: new ulong[] { 2407566267 }),
        new MapData("Goose Bay",        new ulong[] { 2301006771 }),
        new MapData("Nuijamaa",         new ulong[] { 2557112412 }),
        new MapData("Gulf of Aqaba",    new ulong[] { 2726964335 }),
        new MapData("S3 Map",           new ulong[] { 0 }),
    };

    /* MAP NAMES */
    public static readonly string FoolsRoad = mapRotation[0].Name;
    public static readonly string GooseBay = mapRotation[1].Name;
    public static readonly string Nuijamaa = mapRotation[2].Name;
    public static readonly string GulfOfAqaba = mapRotation[3].Name;
    public static readonly string S3Map = mapRotation[4].Name;

    private static List<ulong> originalMods;
    private static List<ulong> originalIgnoreChildren;

    void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();
        originalMods = config.File_IDs;
        originalIgnoreChildren = config.Ignore_Children_File_IDs;
        LoadMap(STATIC_MAP);
    }

    public bool TryLoadMap(string name)
    {
        for (int i = 0; i < mapRotation.Count; ++i)
        {
            MapData d = mapRotation[i];
            if (d.Name.Equals(name, StringComparison.Ordinal))
            {
                LoadMap(i);
                return true;
            }
        }
        return false;
    }
    private void LoadMap(int index)
    {
        MapData d = mapRotation[index];
        if (Level.info != null)
        {
            // trigger restart or something idk
            L.LogWarning("Tried to switch maps after the level " + Level.info.name + " was already loaded.");
        }
        else
        {
            L.Log("Selected " + d.Name + " to load.", ConsoleColor.Blue);
            Provider.map = d.Name;
            WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();
            config.File_IDs = originalMods.ToList();
            config.Ignore_Children_File_IDs = originalIgnoreChildren.ToList();
            for (int i = 0; i < d.AddMods.Length; ++i)
            {
                ulong mod = d.AddMods[i];
                for (int j = 0; j < config.File_IDs.Count; ++j)
                {
                    if (config.File_IDs[j] == mod) goto c;
                }

                L.Log("Added " + mod + " to the workshop queue.", ConsoleColor.Magenta);
                config.File_IDs.Add(mod);
            c:;
            }

            if (d.RemoveMods is not null)
            {
                for (int i = 0; i < d.RemoveMods.Length; ++i)
                {
                    ulong mod = d.RemoveMods[i];
                    for (int j = config.File_IDs.Count - 1; j >= 0; --j)
                    {
                        if (config.File_IDs[j] == mod)
                        {
                            config.File_IDs.RemoveAt(j);
                            L.Log("Removed " + mod + " from the workshop queue.", ConsoleColor.Magenta);
                        }
                    }
                }
            }

            if (d.RemoveChildren is not null)
                config.Ignore_Children_File_IDs.AddRange(d.RemoveChildren);

            DirectoryInfo info = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "Servers", Provider.serverID, "Workshop", "Steam", "content", Provider.APP_ID.m_AppId.ToString(Data.Locale)));
            if (info.Exists)
            {
                foreach (DirectoryInfo modFolder in info.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (!ulong.TryParse(modFolder.Name, NumberStyles.Number, Data.Locale, out ulong mod))
                        continue;
                    for (int i = 0; i < config.File_IDs.Count; ++i)
                    {
                        if (config.File_IDs[i] == mod) goto c;
                    }

                    L.Log("Deleting unused mod folder " + mod + " from workshop directory.", ConsoleColor.Magenta);
                    modFolder.Delete(true);
                c:;
                }
            }
            else
            {
                L.Log(info.FullName + " does not exist: ");
            }
            _map = index;
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
