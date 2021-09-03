using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    // +x > right, +y > up
    public class FlagData
    {
        public int id;
        public string name;
        public float x;
        public float y;
        public ZoneData zone;
        public bool use_map_size_multiplier;
        /// <summary>
        /// T1 (0) -&gt; T2 (max)
        /// </summary>
        public int level;
        public float minHeight;
        public float maxHeight;
        public Dictionary<int, float> adjacencies;
        [JsonIgnore]
        public string color;
        [JsonIgnore]
        public Vector2 Position2D { get => new Vector2(x, y); }
        public FlagData(int id, string name, float x, float y, ZoneData zone, bool use_map_size_multiplier, int level, float minHeight, float maxHeight)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
            this.level = level;
            this.use_map_size_multiplier = use_map_size_multiplier;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
            this.minHeight = minHeight == default ? -1 : minHeight;
            this.maxHeight = maxHeight == default ? -1 : maxHeight;
        }
        [JsonConstructor]
        public FlagData(int id, string name, float x, float y, ZoneData zone, bool use_map_size_multiplier, int level, float minHeight, float maxHeight, Dictionary<int, float> adjacencies)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
            this.level = level;
            this.use_map_size_multiplier = use_map_size_multiplier;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
            this.minHeight = minHeight == default ? -1 : minHeight;
            this.maxHeight = maxHeight == default ? -1 : maxHeight;
            this.adjacencies = adjacencies ?? new Dictionary<int, float>();
        }
    }
}
