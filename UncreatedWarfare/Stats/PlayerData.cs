using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Uncreated.Warfare.Stats
{
    public class PlayerStatsCoroutineData
    {
        public ulong id;
        public float x;
        public float y;
        [JsonConstructor]
        public PlayerStatsCoroutineData(ulong id, float x, float y)
        {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }
}
