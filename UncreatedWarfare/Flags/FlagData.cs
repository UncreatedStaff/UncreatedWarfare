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
        [JsonIgnore]
        public string color;
        [JsonIgnore]
        public Vector2 Position2D { get => new Vector2(x, y); }
        [JsonConstructor]
        public FlagData(int id, string name, float x, float y, ZoneData zone)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.zone = zone;
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
