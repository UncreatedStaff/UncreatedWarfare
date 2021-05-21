using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UncreatedWarfare.Stats;

namespace UncreatedWarfare.Components
{
    public struct LandmineDataForPostAccess
    {
        public ushort barricadeID;
        public int barricadeInstId;
        public SteamPlayer owner;
        public LandmineDataForPostAccess(InteractableTrap trap, BarricadeOwnerDataComponent owner)
        {
            if(trap == default || owner == default)
            {
                barricadeID = 0;
                barricadeInstId = 0;
                this.owner = null;
            } else
            {
                this.barricadeID = owner.barricade.id;
                this.barricadeInstId = trap.GetInstanceID();
                this.owner = owner.owner;
            }
        }
    }
    public class PlaytimeComponent : MonoBehaviour
    {
        public float CurrentTimeSeconds;
        public PlayerCurrentGameStats stats;
        public Player player;
        public ushort lastShot;
        public ushort lastProjected;
        public List<ThrowableOwnerDataComponent> thrown;
        public LandmineDataForPostAccess LastLandmineTriggered;
        public LandmineDataForPostAccess LastLandmineExploded;
        public ushort lastExplodedVehicle;
        public ushort lastRoadkilledBy;
        public void Start()
        {
            this.thrown = new List<ThrowableOwnerDataComponent>();
        }
        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            F.Log("Started tracking " + F.GetPlayerOriginalNames(player).PlayerName + "'s playtime.", ConsoleColor.Magenta);
        }
        public void Update()
        {
            float dt = Time.deltaTime;
            CurrentTimeSeconds += dt;
            if (stats == null)
            {
                if (!Data.GameStats.TryGetPlayer(player.channel.owner.playerID.steamID.m_SteamID, out stats))
                {
                    stats = new PlayerCurrentGameStats(player);
                    Data.GameStats.playerstats.Add(player.channel.owner.playerID.steamID.m_SteamID, stats);
                }
            }
            if(player.IsOnFlag())
            {
                stats.AddToTimeOnPoint(dt);
                stats.AddToTimeDeployed(dt);
            }
            else if (!player.IsInMain())
                stats.AddToTimeDeployed(dt);
            InteractableVehicle veh = player.movement.getVehicle();
            if (veh != null)
            {
                veh.findPlayerSeat(player.channel.owner.playerID.steamID, out byte seat);
                if(seat == 0)
                    stats.AddToTimeDriving(dt);
            }
        }
    }
}
