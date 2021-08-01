using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SDG.Unturned;
using Steamworks;

namespace Uncreated.Warfare.Components
{
    public class BarricadeOwnerDataComponent : MonoBehaviour
    {
        public SteamPlayer owner;
        public ulong ownerID;
        public CSteamID ownerCSID;
        public ulong group;
        public Barricade barricade;
        public BarricadeData data;
        public BarricadeRegion region;
        public Transform barricadeTransform;
        public ulong lastDamaged;
        public void SetData(BarricadeData data, BarricadeRegion region, Transform transform)
        {
            owner = PlayerTool.getSteamPlayer(data.owner);
            this.ownerID = data.owner;
            if (owner != default)
            {
                group = F.GetTeam(owner);
                this.ownerCSID = owner.playerID.steamID;
            }
            else
            {
                group = F.GetTeamFromPlayerSteam64ID(data.owner);
                this.ownerCSID = CSteamID.Nil;
            }
            barricade = data.barricade;
            this.data = data;
            this.region = region;
            this.barricadeTransform = transform;
        }
    }
}
