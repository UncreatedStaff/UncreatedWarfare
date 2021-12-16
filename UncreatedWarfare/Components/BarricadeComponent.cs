using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public class BarricadeComponent : MonoBehaviour
    {
        public ulong Owner;
        public Player Player;
        public Guid BarricadeGUID;
        public ulong LastDamager;
    }
}
