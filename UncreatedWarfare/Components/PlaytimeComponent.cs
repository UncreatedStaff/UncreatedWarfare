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
using Uncreated.Warfare.FOBs;

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
        public Vehicles.VehicleSpawn currentlylinking;
        public void Init()
        {
            this.thrown = new List<ThrowableOwnerDataComponent>();
            toastMessageOpen = 0;
            toastMessages = new Queue<ToastMessage>();
        }
        public void QueueMessage(ToastMessage message)
        {
            if (toastMessages.Count == 0)
                SendToastMessage(message);
            else
                toastMessages.Enqueue(message);
        }
        Queue<ToastMessage> toastMessages;
        ushort toastMessageOpen;
        private const float TOAST_LIFETIME = 13f;
        private void SendToastMessage(ToastMessage message)
        {
            switch(message.Severity)
            {
                default:
                case ToastMessageSeverity.Info:
                    toastMessageOpen = UCWarfare.Config.ToastIDInfo;
                    break;
                case ToastMessageSeverity.Warning:
                    toastMessageOpen = UCWarfare.Config.ToastIDWarning;
                    break;
                case ToastMessageSeverity.Severe:
                    toastMessageOpen = UCWarfare.Config.ToastIDSevere;
                    break;
            }
            EffectManager.sendUIEffect(toastMessageOpen, unchecked((short)toastMessageOpen), player.channel.owner.transportConnection, true, message.Message);
            StartCoroutine(ToastDelay());
        }
        private IEnumerator<WaitForSeconds> ToastDelay()
        {
            yield return new WaitForSeconds(TOAST_LIFETIME);
            EffectManager.askEffectClearByID(toastMessageOpen, player.channel.owner.transportConnection);
            toastMessageOpen = 0;
            if (toastMessages.Count > 0)
                SendToastMessage(toastMessages.Dequeue());
        }


        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            UCPlayer = UncreatedPlayer.Load(player.channel.owner.playerID.steamID.m_SteamID);
            //F.Log("Started tracking " + F.GetPlayerOriginalNames(player).PlayerName + "'s playtime.", ConsoleColor.Magenta);
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
        public bool TeleportDelayed(Vector3 position, float angle, float seconds, bool shouldCancelOnMove = false, bool shouldCancelOnDamage = false, bool shouldMessagePlayer = false, string locationName = "", FOB fob = null)
        {
            if(_currentTeleportRequest == default)
            {
                if (shouldMessagePlayer)
                    player.Message("deploy_standby", locationName, seconds);
                _currentTeleportRequest = StartCoroutine(TeleportDelayedCoroutine(position, angle, seconds, shouldCancelOnMove, shouldCancelOnDamage, shouldMessagePlayer, locationName, fob));
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
        private IEnumerator<WaitForSeconds> TeleportDelayedCoroutine(Vector3 position, float angle, float seconds, bool shouldCancelOnMove, bool shouldCancelOnDamage, bool shouldMessagePlayer, string locationName, FOB fob)
        {
            byte originalhealth = player.life.health;
            Vector3 originalPosition = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

            int counter = 0;
            while (counter < seconds * 2)
            {
                if (player.life.isDead)
                {
                    if (shouldMessagePlayer)
                        player.Message("deploy_c_death");
                    yield break;
                }
                if (shouldCancelOnMove && player.transform.position != originalPosition)
                {
                    if (shouldMessagePlayer)
                        player.Message("deploy_c_moved");
                    yield break;
                }
                if (shouldCancelOnDamage && player.life.health != originalhealth)
                {
                    if (shouldMessagePlayer)
                        player.Message("deploy_c_damaged");
                    yield break;
                }
                if (fob != null && fob.Structure.barricade.isDead)
                {
                    if (shouldMessagePlayer)
                        player.Message("deploy_c_fobdestroyed");
                    yield break;
                }
                counter++;
                yield return new WaitForSeconds(0.5F);
            }
            player.teleportToLocationUnsafe(position, angle);

            if (shouldMessagePlayer)
                player.Message("deploy_s", locationName);
            CooldownManager.StartCooldown(Warfare.UCPlayer.FromPlayer(player), ECooldownType.DEPLOY, CooldownManager.config.data.DeployFOBCooldown);

            yield break;
        }
    }
}
