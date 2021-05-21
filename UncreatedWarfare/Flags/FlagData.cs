using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace UncreatedWarfare.Flags
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
        [JsonIgnore]
        public string color;
        [JsonIgnore]
        public Vector2 Position2D { get => new Vector2(x, y); }
        [JsonConstructor]
        public FlagData(int id, string name, float x, float y, ZoneData zone, bool use_map_size_multiplier, int level)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
            this.level = level;
            this.use_map_size_multiplier = use_map_size_multiplier;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
        }
    }
    public class ZoneData
    {
        public string type;
        public string data;
        [JsonConstructor]
        public ZoneData(string type, string data)
        {
            this.type = type;
            this.data = data;
        }
    }
}
