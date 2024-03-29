﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Uncreated.SQL;
using UnityEngine;

namespace Uncreated.Warfare.Maps;
internal class MapScheduler : MonoBehaviour
{
    internal static MapScheduler Instance;

                                 // intentional is
    public static int Current => Instance is null ? -1 : Instance._map;
    // active map
    private int _map = -1;

    /* MAP DATA */
    private static readonly List<MapData> MapRotation =
    [
        new MapData("Fool's Road",      [ 2407566267, 2407740920 ], removeChildren: [ 2407566267 ]),
        new MapData("Goose Bay",        [ 2301006771 ]),
        new MapData("Nuijamaa",         [ 2557112412 ]),
        new MapData("Gulf of Aqaba",    [ 2726964335 ]),
        new MapData("Changbai Shan",    [ 2943688379, 2407740920 ]),
    ];

    /* MAP NAMES */
    public static readonly string FoolsRoad     = MapRotation[0].Name;
    public static readonly string GooseBay      = MapRotation[1].Name;
    public static readonly string Nuijamaa      = MapRotation[2].Name;
    public static readonly string GulfOfAqaba   = MapRotation[3].Name;
    public static readonly string ChangbaiShan  = MapRotation[4].Name;

    public static string GetMapName(uint index) => MapRotation[(int)index].Name;

    // Map to load if rotation is undefined
    private static readonly string DefaultMap = GulfOfAqaba;

    private static List<ulong> _originalMods;
    private static List<ulong> _originalIgnoreChildren;
    public static int MapCount => MapRotation.Count;

    [UsedImplicitly]
    void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
        WorkshopDownloadConfig config = WorkshopDownloadConfig.getOrLoad();
        _originalMods = config.File_IDs;
        _originalIgnoreChildren = config.Ignore_Children_File_IDs;
        TryLoadMap(DefaultMap);
    }

    public bool TryLoadMap(string name)
    {
        for (int i = 0; i < MapRotation.Count; ++i)
        {
            MapData d = MapRotation[i];
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
        MapData d = MapRotation[index];
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
            config.File_IDs = _originalMods.ToList();
            config.Ignore_Children_File_IDs = _originalIgnoreChildren.ToList();
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

            DirectoryInfo info = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "Servers", Provider.serverID, "Workshop", "Steam", "content", Provider.APP_ID.m_AppId.ToString(Data.AdminLocale)));
            if (info.Exists)
            {
                foreach (DirectoryInfo modFolder in info.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (!ulong.TryParse(modFolder.Name, NumberStyles.Number, Data.AdminLocale, out ulong mod))
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
