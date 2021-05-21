using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SDG.Unturned;
using Steamworks;

namespace UncreatedWarfare.Components
{
    public class BarricadeOwnerDataComponent : MonoBehaviour
    {
        public SteamPlayer owner;
        public ulong group;
        public Barricade barricade;
        public BarricadeData data;
        public BarricadeRegion region;
        public Transform barricadeTransform;
        public void SetData(BarricadeData data, BarricadeRegion region, Transform transform)
        {
            owner = PlayerTool.getSteamPlayer(data.owner);
            if(owner != null)
                group = F.GetTeam(owner);
            barricade = data.barricade;
            this.data = data;
            this.region = region;
            this.barricadeTransform = transform;
        }
    }
}
