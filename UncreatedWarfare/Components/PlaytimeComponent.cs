using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Uncreated.Warfare.Stats;
using Uncreated.Players;

namespace Uncreated.Warfare.Components
{
    public struct LandmineDataForPostAccess
    {
        public ushort barricadeID;
        public int barricadeInstId;
        public SteamPlayer owner;
        public ulong ownerID;
        public LandmineDataForPostAccess(InteractableTrap trap, BarricadeOwnerDataComponent owner)
        {
            if(trap == default || owner == default)
            {
                barricadeID = 0;
                barricadeInstId = 0;
                this.owner = null;
                if (owner != default)
                    this.ownerID = owner.ownerID;
                else this.ownerID = 0;
            } else
            {
                this.barricadeID = owner.barricade.id;
                this.barricadeInstId = trap.GetInstanceID();
                this.owner = owner.owner;
                this.ownerID = owner.ownerID;
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
        public ushort lastRoadkilled;
        private Coroutine _currentTeleportRequest;
        public UncreatedPlayer UCPlayer;
        public void Start()
        {
            this.thrown = new List<ThrowableOwnerDataComponent>();
        }
        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            UCPlayer = UncreatedPlayer.Load(player.channel.owner.playerID.steamID.m_SteamID);
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
            if (player.IsOnFlag())
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
                if (seat == 0)
                    stats.AddToTimeDriving(dt);
            }
        }
        /// <summary>Start a delayed teleport on the player.</summary>
        /// <returns>True if there were no requests pending, false if there were.</returns>
        public bool TeleportDelayed(Vector3 Location, float y_euler, float seconds)
        {
            if(_currentTeleportRequest == default)
            {
                _currentTeleportRequest = StartCoroutine(TeleportDelayedCoroutine(Location, y_euler, seconds));
                return true;
            }
            return false;
        }
        public void CancelTeleport()
        {
            if (_currentTeleportRequest != default)
            {
                StopCoroutine(_currentTeleportRequest);
                _currentTeleportRequest = default;
            }

        }
        private IEnumerator<WaitForSeconds> TeleportDelayedCoroutine(Vector3 Location, float y_euler, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (!player.teleportToLocation(Location, y_euler))
                F.LogError($"Failed to teleport {F.GetPlayerOriginalNames(player).PlayerName} to ({Location.x}, {Location.y}, {Location.z}) at {Math.Round(y_euler, 2)}°");
        }
    }
}
