using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Point;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    public struct LandmineData
    {
        public static LandmineData Nil = new LandmineData(null, null);
        public Guid barricadeGUID;
        public Player? owner;
        public ulong ownerID;
        public int instanceID;
        public LandmineData(InteractableTrap? trap, BarricadeComponent? owner)
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
        public float JoinTime = 0f;
        public Gamemodes.Interfaces.IStats stats;
        public Player player;
        public Guid lastShot;
        public Guid lastProjected;
        public ulong lastAttacker;
        public KeyValuePair<ulong, DateTime> secondLastAttacker;
        public List<ThrowableOwner> thrown;
        public LandmineData LastLandmineTriggered;
        public LandmineData LastLandmineExploded;
        public Guid lastExplodedVehicle;
        public Guid lastRoadkilled;
        private Coroutine? _currentTeleportRequest;
        public Vehicles.VehicleSpawn? currentlylinking;
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
                    L.Log("Unable to find effect asset with GUID " + this.guid.ToString("N") + " in toast messages.");
                else 
                    this.id = ea.id;
            }
        }
        private static readonly ToastMessageInfo[] TOASTS = new ToastMessageInfo[]
        {
            new ToastMessageInfo(EToastMessageSeverity.INFO,        new Guid("d7504683-4b32-4ed4-9191-4b4136ab1bc8"), 0, 12f),      // info
            new ToastMessageInfo(EToastMessageSeverity.WARNING,     new Guid("5678a559-695e-4d99-9dfe-a9a771b6616f"), 0, 12f),      // warning
            new ToastMessageInfo(EToastMessageSeverity.SEVERE,      new Guid("26fed656-4ccf-4c46-aac1-df01dbba0aab"), 0, 12f),      // error
            new ToastMessageInfo(EToastMessageSeverity.MINI,        new Guid("a213915d-61ad-41ce-bab3-4fb12fe6870c"), 1, 1.58f),    // xp
            new ToastMessageInfo(EToastMessageSeverity.MEDIUM,      new Guid("5f695955-f0da-4d19-adac-ac39140da797"), 2, 4f),       // xp
            new ToastMessageInfo(EToastMessageSeverity.BIG,         new Guid("9de82ffe-a139-46b3-9109-0eb918bf3991"), 3, 5.5f),     // big
            new ToastMessageInfo(EToastMessageSeverity.PROGRESS,    new Guid("a113a0f2-d0af-4db8-b5e5-bcbc17fc96c9"), 4, 1.6f),     // progress
            new ToastMessageInfo(EToastMessageSeverity.TIP,         new Guid("abbf74e8-6f1c-4665-9258-84c70b9433ba"), 1, 4f),       // tip
        };
        private struct ToastChannel
        {
            public byte channel;
            public ToastMessageInfo info;
            public ToastMessage message;
            public float timeRemaining;
            public bool InUse => timeRemaining > 0f;
            public bool hasPending = false;
            public ToastChannel(byte channel)
            {
                this.info = default;
                this.message = default;
                this.timeRemaining = 0f;
                this.channel = channel;
            }
            public void SetMessage(ToastMessageInfo info, ToastMessage message)
            {
                this.info = info;
                this.message = message;
                this.timeRemaining = info.time;
            }
            /// <returns><see langword="true"/> if there is a message currently playing on the channel, otherwise <see langword="false"/>.</returns>
            public bool Update(float dt)
            {
                if (this.timeRemaining <= 0f) return hasPending;
                this.timeRemaining -= dt;
                if (this.timeRemaining <= 0f)
                {
                    this.timeRemaining = 0f;
                    return hasPending;
                }
                return false;
            }
        }

        private ToastChannel[] channels;
        public void QueueMessage(ToastMessage message, bool priority = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                L.LogWarning("Undefined toast message type: " + message.Severity.ToString());
                return;
            }
            if (priority || (pendingToastMessages.Count(x => x.Value.channel == info.channel) == 0 && !channels[info.channel].InUse))
                SendToastMessage(message, info);
            else
            {
                pendingToastMessages.Insert(0, new KeyValuePair<ToastMessage, ToastMessageInfo>(message, info));
                for (int i = 0; i < channels.Length; i++)
                {
                    if (channels[i].channel == info.channel)
                    {
                        channels[i].hasPending = true;
                        break;
                    }
                }
            }
        }   
        readonly List<KeyValuePair<ToastMessage, ToastMessageInfo>> pendingToastMessages = new List<KeyValuePair<ToastMessage, ToastMessageInfo>>();
        private void SendToastMessage(ToastMessage message, ToastMessageInfo info)
        {
            EffectManager.sendUIEffect(info.id, unchecked((short)info.id), player.channel.owner.transportConnection, true, message.Message1 ?? "", message.Message2 ?? "", message.Message3 ?? "" );
            channels[info.channel].SetMessage(info, message);
            for (int i = pendingToastMessages.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ToastMessage, ToastMessageInfo> t = pendingToastMessages[i];
                if (t.Key == message)
                {
                    pendingToastMessages.RemoveAt(i);
                    break;
                }
            }
        }
        public void StartTracking(Player player)
        {
            this.player = player;
            CurrentTimeSeconds = 0.0f;
            JoinTime = Time.realtimeSinceStartup;
            this.thrown = new List<ThrowableOwner>();
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
            channels = new ToastChannel[max];
            for (byte i = 0; i < channels.Length; i++)
                channels[i] = new ToastChannel(i);
        }
        public void TryUpdateAttackers(ulong newLastAttacker)
        {
            if (newLastAttacker == lastAttacker) return;

            secondLastAttacker = new KeyValuePair<ulong, DateTime>(lastAttacker, DateTime.Now);
            lastAttacker = newLastAttacker;
        }
        public void ResetAttackers()
        {
            lastAttacker = 0;
            secondLastAttacker = new KeyValuePair<ulong, DateTime>(0, DateTime.Now);
        }
        public void Update()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float dt = Time.deltaTime;
            CurrentTimeSeconds += dt;
            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i].Update(dt))
                {
                    ToastChannel channel = channels[i];
                    for (int j = pendingToastMessages.Count - 1; j >= 0; j--)
                    {
                        KeyValuePair<ToastMessage, ToastMessageInfo> t = pendingToastMessages[j];
                        if (t.Value.channel == channel.channel)
                        {
                            SendToastMessage(t.Key, t.Value);
                            goto next;
                        }
                    }
                    channel.hasPending = false;
                    next: ;
                }
            }
        }
        public void CancelTeleport()
        {
            if (_currentTeleportRequest != default)
            {
                StopCoroutine(_currentTeleportRequest);
                _currentTeleportRequest = default;
            }
        }
        public bool TeleportTo(object location, float delay, bool shouldCancelOnMove, bool startCoolDown = true, float yawOverride = -1)
        {
            UCPlayer? player = UCPlayer.FromPlayer(this.player);

            if (player != null)
            {
                if (_currentTeleportRequest == default)
                {
                    _currentTeleportRequest = StartCoroutine(TeleportCoroutine(player, location, delay, shouldCancelOnMove, startCoolDown, yawOverride));
                    return true;
                }
                else
                    player.Message("deploy_e_alreadydeploying");
            }
            return false;
        }

        private IEnumerator<WaitForSeconds> TeleportCoroutine(UCPlayer player, object structure, float delay, bool shouldCancelOnMove = false, bool startCoolDown = true, float yawOverride = -1)
        {
            bool isFOB = structure is FOB;
            bool isSpecialFOB = structure is SpecialFOB;
            bool isCache = structure is Cache;
            bool isMain = structure is Vector3;

            PendingFOB = structure;

            FOB? fob = null;
            SpecialFOB? special = null;
            Cache? cache = null;

            if (isFOB)
                fob = structure as FOB;
            else if (isSpecialFOB)
                special = structure as SpecialFOB;
            else if (isCache)
                cache = structure as Cache;


            if (isFOB)
                player.Message("deploy_fob_standby", fob!.UIColor, fob.Name, delay.ToString());
            else if (isSpecialFOB)
                player.Message("deploy_fob_standby", special!.UIColor, special.Name, delay.ToString());
            else if (isCache)
                player.Message("deploy_fob_standby", cache!.UIColor, cache.Name, delay.ToString());
            else if (isMain)
                player.Message("deploy_fob_standby", "f0c28d", "MAIN", delay.ToString());

            int counter = 0;

            Vector3 originalPosition = player.Position;

            while (counter < delay * 4)
            {
                yield return new WaitForSeconds(0.25F);

#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                try
                {
                    if (player.Player.life.isDead)
                    {
                        player.Message("deploy_c_dead");

                        CancelTeleport();
                        yield break;
                    }
                    if (shouldCancelOnMove && player.Position != originalPosition)
                    {
                        player.Message("deploy_c_moved");

                        CancelTeleport();
                        yield break;
                    }
                    if (isFOB)
                    {
                        if (fob!.NearbyEnemies.Count != 0)
                        {
                            player.Message("deploy_c_enemiesNearby");

                            CancelTeleport();
                            yield break;
                        }
                        if (fob.IsBleeding)
                        {
                            player.Message("deploy_c_bleeding");

                            CancelTeleport();
                            yield break;
                        }
                        if (!fob.IsSpawnable)
                        {
                            player.Message("deploy_c_notspawnable");

                            CancelTeleport();
                            yield break;
                        }
                    }
                    else if (isCache)
                    {
                        if (cache!.NearbyAttackers.Count != 0)
                        {
                            player.Message("deploy_c_enemiesNearby");

                            CancelTeleport();
                            yield break;
                        }
                        if (cache == null || cache.Structure.GetServersideData().barricade.isDead)
                        {
                            player.Message("deploy_c_cachedead");

                            CancelTeleport();
                            yield break;
                        }
                    }
                    else if (isSpecialFOB)
                    {
                        if (special == null || !special.IsActive)
                        {
                            player.Message("deploy_c_notactive");

                            CancelTeleport();
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    L.LogError("Failed to teleport to FOB: " + player.Player.channel.owner.playerID.playerName);
                    L.LogError(ex);

                    CancelTeleport();
                }

                counter++;
            }

            Vector3 position = Vector3.zero;
            float rotation = player.Player.transform.eulerAngles.y;
            if (isFOB)
            {
                if (fob!.Bunker != null)
                {
                    position = fob.Bunker.model.position;
                    rotation = fob.Bunker.model.eulerAngles.y;
                    ActionLog.Add(EActionLogType.DEPLOY_TO_LOCATION, "FOB BUNKER " + fob.Name + " TEAM " + Teams.TeamManager.TranslateName(fob.Team, 0), player);
                }
            }
            else if (isCache)
            {
                position = cache!.Structure.model.TransformPoint(new Vector3(3, 0, 0));
                rotation = cache.Structure.model.eulerAngles.y;
                ActionLog.Add(EActionLogType.DEPLOY_TO_LOCATION, "CACHE " + cache.Name + " TEAM " + Teams.TeamManager.TranslateName(cache.Team, 0), player);
            }
            else if (isSpecialFOB)
            {
                position = special!.Point;
                ActionLog.Add(EActionLogType.DEPLOY_TO_LOCATION, "SPECIAL FOB " + special.Name + " TEAM " + Teams.TeamManager.TranslateName(special.Team, 0), player);
            }
            else if (structure is Vector3 vector)
            {
                position = vector;
                ActionLog.Add(EActionLogType.DEPLOY_TO_LOCATION, "MAIN BASE " + Teams.TeamManager.TranslateName(player.GetTeam(), 0), player);
            }

            if (yawOverride != -1)
                rotation = yawOverride;

            player.Player.teleportToLocationUnsafe(position, rotation);

            _currentTeleportRequest = default;

            if (isFOB)
            {
                player.Message("deploy_s", fob!.UIColor, fob.Name);

                Points.TryAwardFOBCreatorXP(fob, Points.XPConfig.FOBDeployedXP, "xp_fob_in_use");

                if (fob!.Bunker!.model.TryGetComponent(out BuiltBuildableComponent comp))
                    Quests.QuestManager.OnPlayerSpawnedAtBunker(comp, fob!, player);
            }
            else if (isSpecialFOB)
                player.Message("deploy_s", special!.UIColor, special.Name);
            else if (isCache)
                player.Message("deploy_s", cache!.UIColor, cache.Name);
            else if (structure is Vector3)
            {
                player.Message("deploy_s", "f0c28d", "MAIN");
            }

            if (startCoolDown)
                CooldownManager.StartCooldown(player, ECooldownType.DEPLOY, CooldownManager.config.data.DeployFOBCooldown);
        }

        public object PendingFOB;
    }
}
