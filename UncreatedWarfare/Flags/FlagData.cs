using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace UncreatedWarfare.Flags
{
    public class FlagData
    {
        public int id;
        public string name;
        public float x;
        public float y;
        public float z;
        public float sizeX;
        public float sizeY;
        [JsonIgnore]
        public string color;
        [JsonIgnore]
        public Vector3 Position { get => new Vector3(x, y, z); }
        [JsonIgnore]
        public Vector3 Position2D { get => new Vector2(x, z); }
        [JsonConstructor]
        public FlagData(int id, string name, float x, float y, float z, float sizeX, float sizeY)
        {
            this.id = id;
            this.name = name;
            this.x = x;
            this.y = y;
            this.z = z;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.color = UCWarfare.Config.FlagSettings.NeutralColor;
        }
    }
}
