using SDG.Unturned;
using System;
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
