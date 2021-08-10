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
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Squads;

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
        public UncreatedPlayer UCPlayerStats;
        public Vehicles.VehicleSpawn currentlylinking;
        public void QueueMessage(ToastMessage message)
        {
            if (toastMessages.Count == 0 && toastMessageOpen == 0)
                SendToastMessage(message);
            else
                toastMessages.Enqueue(message);
        }
        Queue<ToastMessage> toastMessages;
        ushort toastMessageOpen;
        private void SendToastMessage(ToastMessage message)
        {
            switch(message.Severity)
            {
                default:
                case ToastMessageSeverity.INFO:
                    toastMessageOpen = UCWarfare.Config.ToastIDInfo;
                    break;
                case ToastMessageSeverity.WARNING:
                    toastMessageOpen = UCWarfare.Config.ToastIDWarning;
                    break;
                case ToastMessageSeverity.SEVERE:
                    toastMessageOpen = UCWarfare.Config.ToastIDSevere;
                    break;
                case ToastMessageSeverity.MINIXP:
                    toastMessageOpen = UCWarfare.Config.MiniToastXP;
                    break;
                case ToastMessageSeverity.MINIOFFICERPTS: 
                    toastMessageOpen = UCWarfare.Config.MiniToastOfficerPoints;
                    break;
                case ToastMessageSeverity.BIG:
                    toastMessageOpen = UCWarfare.Config.BigToast;
                    break;
            }
            if (message.Message != null)
            {
                if (message.SecondaryMessage != null)
                    EffectManager.sendUIEffect(toastMessageOpen, unchecked((short)toastMessageOpen), 
                        player.channel.owner.transportConnection, true, message.Message, message.SecondaryMessage);
                else
                    EffectManager.sendUIEffect(toastMessageOpen, unchecked((short)toastMessageOpen), 
                        player.channel.owner.transportConnection, true, message.Message);
            }
            StartCoroutine(ToastDelay(message.delay));
        }
        private IEnumerator<WaitForSeconds> ToastDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EffectManager.askEffectClearByID(toastMessageOpen, player.channel.owner.transportConnection);
            toastMessageOpen = 0;
            if (toastMessages.Count > 0)
                SendToastMessage(toastMessages.Dequeue());
        }
        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            UCPlayerStats = UncreatedPlayer.Load(player.channel.owner.playerID.steamID.m_SteamID);
            //F.Log("Started tracking " + F.GetPlayerOriginalNames(player).PlayerName + "'s playtime.", ConsoleColor.Magenta);
            this.thrown = new List<ThrowableOwnerDataComponent>();
            toastMessageOpen = 0;
            toastMessages = new Queue<ToastMessage>();
            F.Log("Started tracking playtime of " + player.name);
        }
        public void Update()
        {
            float dt = Time.deltaTime;
            CurrentTimeSeconds += dt;
            if (stats == null)
            {
                F.LogWarning("stats is null " + player.name);
                return;
            }
            if (player.IsOnFlag())
            {
                stats.AddToTimeOnPoint(dt);
                stats.AddToTimeDeployed(dt);
            }
            else if (!player.IsInMain())
                stats.AddToTimeDeployed(dt);
        }
        /// <summary>Start a delayed teleport on the player.</summary>
        /// <returns>True if there were no requests pending, false if there were.</returns>
        public bool TeleportDelayed(Vector3 position, float angle, float seconds, bool shouldCancelOnMove = false, bool shouldCancelOnDamage = false, bool shouldMessagePlayer = false, string locationName = "", FOB fob = null)
        {
            if(_currentTeleportRequest == default)
            {
                if (shouldMessagePlayer)
                    player.Message("deploy_standby", locationName, seconds.ToString(Data.Locale));
                _currentTeleportRequest = StartCoroutine(TeleportDelayedCoroutine(position, angle, seconds, shouldCancelOnMove, shouldCancelOnDamage, shouldMessagePlayer, locationName, fob));
                return true;
            }
            else
                player.Message("deploy_e_alreadydeploying");
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
                try
                {
                    if (player.life.isDead)
                    {
                        if (shouldMessagePlayer)
                            player.Message("deploy_c_dead");
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
                    if (fob != null && fob.Structure.GetServersideData().barricade.isDead)
                    {
                        if (shouldMessagePlayer)
                            player.Message("deploy_c_fobdead");
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    F.LogError("Failed to teleport " + player.channel.owner.playerID.playerName);
                    F.LogError(ex);
                }
                counter++;
                yield return new WaitForSeconds(0.5F);
            }
            try
            {
                player.teleportToLocationUnsafe(position, angle);

                _currentTeleportRequest = default;

                if (shouldMessagePlayer)
                    player.Message("deploy_s", locationName);
                CooldownManager.StartCooldown(UCPlayer.FromPlayer(player), ECooldownType.DEPLOY, CooldownManager.config.Data.DeployFOBCooldown);

                if (fob != null)
                {
                    UCPlayer FOBowner = UCPlayer.FromID(fob.Structure.GetServersideData().owner);
                    if (FOBowner != null)
                    {
                        if (FOBowner.CSteamID != player.channel.owner.playerID.steamID)
                        {
                            XP.XPManager.AddXP(FOBowner.Player, FOBowner.Player.GetTeam(), XP.XPManager.config.Data.FOBDeployedXP,
                                F.Translate("xp_deployed_fob", FOBowner));

                            if (FOBowner.IsSquadLeader() && FOBowner.Squad.Members.Exists(p => p.CSteamID == player.channel.owner.playerID.steamID))
                            {
                                Officers.OfficerManager.AddOfficerPoints(FOBowner.Player, FOBowner.Player.GetTeam(), 
                                    XP.XPManager.config.Data.FOBDeployedXP, F.Translate("ofp_deployed_fob", FOBowner));
                            }
                        }
                    }
                    else
                        Data.DatabaseManager.AddXP(fob.Structure.GetServersideData().owner, fob.Structure.GetServersideData().group.GetTeam(), XP.XPManager.config.Data.FOBDeployedXP);
                }
                yield break;
            }
            catch (Exception ex)
            {
                F.LogError("Failed to teleport " + player.channel.owner.playerID.playerName);
                F.LogError(ex);
            }
        }
    }
}
