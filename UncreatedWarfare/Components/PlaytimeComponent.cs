using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public struct LandmineData
    {
        public static LandmineData Nil = new LandmineData(null, null);
        public Guid barricadeGUID;
        public Player owner;
        public ulong ownerID;
        public int instanceID;
        public LandmineData(InteractableTrap trap, BarricadeComponent owner)
        {
            if (trap == null || owner == null)
            {
                barricadeGUID = Guid.Empty;
                this.owner = null;
                if (owner != null)
                    this.ownerID = owner.Owner;
                else this.ownerID = 0;
                instanceID = 0;
            }
            else
            {
                this.instanceID = trap.GetInstanceID();
                this.barricadeGUID = owner.BarricadeGUID;
                this.owner = owner.Player;
                this.ownerID = owner.Owner;
            }
        }
        
    }
    public class PlaytimeComponent : MonoBehaviour
    {
        public float CurrentTimeSeconds;
        public Gamemodes.Interfaces.IStats stats;
        public Player player;
        public Guid lastShot;
        public Guid lastProjected;
        public List<ThrowableOwner> thrown;
        public LandmineData LastLandmineTriggered;
        public LandmineData LastLandmineExploded;
        public Guid lastExplodedVehicle;
        public Guid lastRoadkilled;
        private Coroutine _currentTeleportRequest;
        public Vehicles.VehicleSpawn currentlylinking;
        private struct ToastMessageInfo
        {
            public static readonly ToastMessageInfo Nil = new ToastMessageInfo(0, Guid.Empty, 0, 0f);
            public byte channel;
            public ushort id;
            public EToastMessageSeverity type;
            public Guid guid;
            public float time;
            public ToastMessageInfo(EToastMessageSeverity type, Guid guid, byte channel, float time)
            {
                this.type = type;
                this.channel = channel;
                this.guid = guid;
                this.time = time;
                this.id = 0;
                ReloadAsset();
            }
            public void ReloadAsset()
            {
                if (!(Assets.find(this.guid) is EffectAsset ea))
                    F.Log("Unable to find effect asset with GUID " + this.guid.ToString("N") + " in toast messages.");
                else 
                    this.id = ea.id;
            }
        }
        private static readonly ToastMessageInfo[] TOASTS = new ToastMessageInfo[]
        {
            new ToastMessageInfo(EToastMessageSeverity.INFO, new Guid("d7504683-4b32-4ed4-9191-4b4136ab1bc8"), 0, 12f), // info
            new ToastMessageInfo(EToastMessageSeverity.WARNING, new Guid("5678a559-695e-4d99-9dfe-a9a771b6616f"), 0, 12f), // warning
            new ToastMessageInfo(EToastMessageSeverity.SEVERE, new Guid("26fed656-4ccf-4c46-aac1-df01dbba0aab"), 0, 12f), // error
            new ToastMessageInfo(EToastMessageSeverity.MINIXP, new Guid("a213915d-61ad-41ce-bab3-4fb12fe6870c"), 1, 4f), // xp
            new ToastMessageInfo(EToastMessageSeverity.MINIOFFICERPTS, new Guid("5f695955-f0da-4d19-adac-ac39140da797"), 1, 4f), // ofp
            new ToastMessageInfo(EToastMessageSeverity.BIG, new Guid("9de82ffe-a139-46b3-9109-0eb918bf3991"), 2, 5.5f), // big
            new ToastMessageInfo(EToastMessageSeverity.PROGRESS, new Guid("a113a0f2d0af4db8b5e5bcbc17fc96c9"), 3, 1.5f), // progress
        };
        private static readonly bool[] channels;
        static PlaytimeComponent()
        {
            byte max = 0;
            bool cont0 = false;
            for (int i = 0; i < TOASTS.Length; i++)
            {
                ToastMessageInfo toast = TOASTS[i];
                if (toast.channel == 0)
                    cont0 = true;
                else if (max < toast.channel)
                    max = toast.channel;
            }
            if (cont0) max++;
            channels = new bool[max];
        }
        public void QueueMessage(ToastMessage message, bool priority = false)
        {
            ToastMessageInfo info = ToastMessageInfo.Nil;
            for (int i = 0; i < TOASTS.Length; i++)
            {
                if (TOASTS[i].type == message.Severity)
                {
                    info = TOASTS[i];
                    break;
                }
            }
            if (info.guid == Guid.Empty)
            {
                F.LogWarning("Undefined toast message type: " + message.Severity.ToString());
                return;
            }
            if (priority || (pendingToastMessages.Count(x => x.Value.channel == info.channel) == 0 && !channels[info.channel]))
                SendToastMessage(message, info);
            else
                pendingToastMessages.Insert(0, new KeyValuePair<ToastMessage, ToastMessageInfo>(message, info));
        }   
        readonly List<KeyValuePair<ToastMessage, ToastMessageInfo>> pendingToastMessages = new List<KeyValuePair<ToastMessage, ToastMessageInfo>>();
        private Coroutine _toastDelay = null;
        private void SendToastMessage(ToastMessage message, ToastMessageInfo info)
        {
            if (message.Message != null)
            {
                if (message.SecondaryMessage != null)
                    EffectManager.sendUIEffect(info.id, unchecked((short)info.id),
                        player.channel.owner.transportConnection, true, message.Message, message.SecondaryMessage);
                else
                    EffectManager.sendUIEffect(info.id, unchecked((short)info.id),
                        player.channel.owner.transportConnection, true, message.Message);
            }
            channels[info.channel] = true;
            for (int i = pendingToastMessages.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ToastMessage, ToastMessageInfo> t = pendingToastMessages[i];
                if (t.Key == message)
                {
                    pendingToastMessages.RemoveAt(i);
                    break;
                }
            }
            if (_toastDelay != null)
                StopCoroutine(_toastDelay);
            _toastDelay = StartCoroutine(ToastDelay(message, info));
        }
        private IEnumerator<WaitForSeconds> ToastDelay(ToastMessage message, ToastMessageInfo info)
        {
            yield return new WaitForSeconds(info.time);
            channels[info.channel] = false;
            EffectManager.askEffectClearByID(info.id, player.channel.owner.transportConnection);
            for (int i = pendingToastMessages.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ToastMessage, ToastMessageInfo> t = pendingToastMessages[i];
                if (t.Value.channel == info.channel)
                {
                    SendToastMessage(t.Key, t.Value);
                    break;
                }
            }
        }
        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            //F.Log("Started tracking " + F.GetPlayerOriginalNames(player).PlayerName + "'s playtime.", ConsoleColor.Magenta);
            this.thrown = new List<ThrowableOwner>();
            for (int i = 0; i < channels.Length; i++)
                channels[i] = false;
            F.Log("Started tracking playtime of " + player.name);
        }
        public void Update()
        {
            float dt = Time.deltaTime;
            CurrentTimeSeconds += dt;
        }
        /// <summary>Start a delayed teleport on the player.</summary>
        /// <returns>True if there were no requests pending, false if there were.</returns>
        public bool TeleportDelayed(Vector3 position, float angle, float seconds, bool shouldCancelOnMove = false, bool shouldCancelOnDamage = false, bool shouldMessagePlayer = false, string locationName = "", object deployable = null)
        {
            if (_currentTeleportRequest == default)
            {
                if (shouldMessagePlayer)
                    player.Message("deploy_standby", locationName, seconds.ToString(Data.Locale));
                _currentTeleportRequest = StartCoroutine(TeleportDelayedCoroutine(position, angle, seconds, shouldCancelOnMove, shouldCancelOnDamage, shouldMessagePlayer, locationName, deployable));
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
        public object PendingFOB;
        private IEnumerator<WaitForSeconds> TeleportDelayedCoroutine(Vector3 position, float angle, float seconds, bool shouldCancelOnMove, bool shouldCancelOnDamage, bool shouldMessagePlayer, string locationName, object deployable)
        {
            PendingFOB = deployable;
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

                        CancelTeleport();
                        yield break;
                    }
                    if (shouldCancelOnMove && player.transform.position != originalPosition)
                    {
                        if (shouldMessagePlayer)
                            player.Message("deploy_c_moved");

                        CancelTeleport();
                        yield break;
                    }
                    if (shouldCancelOnDamage && player.life.health != originalhealth)
                    {
                        if (shouldMessagePlayer)
                            player.Message("deploy_c_damaged");

                        CancelTeleport();
                        yield break;
                    }
                    if (deployable is FOB fob)
                    {
                        if (fob.nearbyEnemies.Count != 0)
                        {
                            if (shouldMessagePlayer)
                                player.Message("deploy_c_enemiesNearby");

                            CancelTeleport();
                            yield break;
                        }
                        if (fob.Structure.GetServersideData().barricade.isDead)
                        {
                            if (shouldMessagePlayer)
                                player.Message("deploy_c_fobdead");

                            CancelTeleport();
                            yield break;
                        }
                    }
                    if (deployable is SpecialFOB special)
                    {
                        if (!special.IsActive)
                        {
                            if (shouldMessagePlayer)
                                player.Message("deploy_c_notactive");

                            CancelTeleport();
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    F.LogError("Failed to teleport " + player.channel.owner.playerID.playerName);
                    F.LogError(ex);

                    CancelTeleport();
                }
                counter++;
                yield return new WaitForSeconds(0.5F);
            }
            try
            {
                if (deployable is FOB fob)
                {
                    if (fob.IsCache)
                    {
                        position = fob.Structure.model.TransformPoint(new Vector3(0, 3, 0));
                    }
                    else
                    {
                        UCPlayer FOBowner = UCPlayer.FromID(fob.Structure.GetServersideData().owner);
                        if (FOBowner != null)
                        {
                            if (FOBowner.CSteamID != player.channel.owner.playerID.steamID)
                            {
                                XP.XPManager.AddXP(FOBowner.Player, XP.XPManager.config.Data.FOBDeployedXP,
                                    F.Translate("xp_deployed_fob", FOBowner));

                                if (FOBowner.IsSquadLeader() && FOBowner.Squad.Members.Exists(p => p.CSteamID == player.channel.owner.playerID.steamID))
                                {
                                    Officers.OfficerManager.AddOfficerPoints(FOBowner.Player, XP.XPManager.config.Data.FOBDeployedXP, F.Translate("ofp_deployed_fob", FOBowner));
                                }
                            }
                        }
                        else
                            Data.DatabaseManager.AddXP(fob.Structure.GetServersideData().owner, XP.XPManager.config.Data.FOBDeployedXP);
                    }
                }

                player.teleportToLocationUnsafe(position, angle);

                _currentTeleportRequest = default;

                if (shouldMessagePlayer)
                    player.Message("deploy_s", locationName);
                CooldownManager.StartCooldown(UCPlayer.FromPlayer(player), ECooldownType.DEPLOY, CooldownManager.config.Data.DeployFOBCooldown);

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
