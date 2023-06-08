using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.SQL;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Components;

public struct LandmineData
{
    public static LandmineData Nil = new LandmineData(null, null);
    public Guid Barricade;
    public Player? Owner;
    public ulong OwnerId;
    public int InstanceId;
    public LandmineData(InteractableTrap? trap, BarricadeComponent? owner)
    {
        if (trap == null || owner == null)
        {
            Barricade = Guid.Empty;
            Owner = null;
            OwnerId = owner != null ? owner.Owner : 0ul;
            InstanceId = 0;
        }
        else
        {
            InstanceId = trap.GetInstanceID();
            Barricade = owner.BarricadeGUID;
            Owner = owner.Player;
            OwnerId = owner.Owner;
        }
    }

}
public class UCPlayerData : MonoBehaviour
{
    internal const int PingBufferSize = 256;
    internal BarricadeDrop? ExplodingLandmine;
    internal BarricadeDrop? TriggeringLandmine;
    internal ItemMagazineAsset LastProjectedAmmoType;
    internal Coroutine? CurrentTeleportRequest;
    internal PlayerDied? LastBleedingEvent;
    internal IDeployable? PendingDeploy;
    internal float[] PingBuffer = new float[PingBufferSize];
    internal int PingBufferIndex = -1;
    internal float LastAvgPingDifference;
    internal List<ThrowableComponent> ActiveThrownItems = new List<ThrowableComponent>(4);
    internal SqlItem<VehicleSpawn>? Currentlylinking;
    internal VehicleComponent? ExplodingVehicle;
    internal ThrowableComponent? TriggeringThrowable;
    internal KeyValuePair<ulong, DateTime> SecondLastAttacker;
    internal DeathMessageArgs LastBleedingArgs;
    internal Guid LastExplodedVehicle;
    internal Guid LastVehicleHitBy;
    internal Guid LastInfectableConsumed;
    internal Guid LastExplosiveConsumed;
    internal Guid LastChargeDetonated;
    internal Guid LastShreddedBy;
    internal Guid LastRocketShot;
    internal Guid LastRocketShotVehicle;
    internal Guid LastGunShot; // used for amc
    internal ulong LastAttacker;
    private float _currentTimeSeconds;
    public Player Player { get; private set; }
    public Gamemodes.Interfaces.IStats Stats { get; internal set; }
    public float JoinTime { get; private set; }

    #region TOASTS
    public struct ToastMessageInfo
    {
        public static readonly ToastMessageInfo Nil = new ToastMessageInfo(0, Guid.Empty, 0, 0f, null);
        public byte Channel;
        public ushort Id;
        public ToastMessageSeverity Type;
        public Guid Guid;
        public float Time;
        public string? TextContainerName;
        public ToastMessageInfo(ToastMessageSeverity type, Guid guid, byte channel, float time, string? text1container = null)
        {
            Type = type;
            Channel = channel;
            Guid = guid;
            Time = time;
            Id = 0;
            TextContainerName = text1container;
            ReloadAsset();
        }
        public void ReloadAsset()
        {
            if (Assets.find(Guid) is not EffectAsset ea)
                L.Log("Unable to find effect asset with GUID " + Guid.ToString("N") + " in toast messages.");
            else
                Id = ea.id;
        }
    }
    internal static void ReloadToastIDs()
    {
        ref ToastMessageInfo i = ref Toasts[0];
        Gamemode.Config.UIToastInfo.ValidReference(out i.Guid);
        i = ref Toasts[1];
        Gamemode.Config.UIToastWarning.ValidReference(out i.Guid);
        i = ref Toasts[2];
        Gamemode.Config.UIToastSevere.ValidReference(out i.Guid);
        i = ref Toasts[3];
        Gamemode.Config.UIToastXP.ValidReference(out i.Guid);
        i = ref Toasts[4];
        Gamemode.Config.UIToastMedium.ValidReference(out i.Guid);
        i = ref Toasts[5];
        Gamemode.Config.UIToastLarge.ValidReference(out i.Guid);
        i = ref Toasts[6];
        Gamemode.Config.UIToastProgress.ValidReference(out i.Guid);
        i = ref Toasts[7];
        Gamemode.Config.UIToastTip.ValidReference(out i.Guid);
    }
    public static readonly ToastMessageInfo[] Toasts =
    {
        new ToastMessageInfo(ToastMessageSeverity.Info,        Gamemode.Config.UIToastInfo.ValidReference(out Guid guid) ? guid : Guid.Empty, 0, 12f),  // info
        new ToastMessageInfo(ToastMessageSeverity.Warning,     Gamemode.Config.UIToastWarning.ValidReference(out guid) ? guid : Guid.Empty, 0, 12f),    // warning
        new ToastMessageInfo(ToastMessageSeverity.Severe,      Gamemode.Config.UIToastSevere.ValidReference(out guid) ? guid : Guid.Empty, 0, 12f),     // error
        new ToastMessageInfo(ToastMessageSeverity.Mini,        Gamemode.Config.UIToastXP.ValidReference(out guid) ? guid : Guid.Empty, 1, 1.58f),       // xp
        new ToastMessageInfo(ToastMessageSeverity.Medium,      Gamemode.Config.UIToastMedium.ValidReference(out guid) ? guid : Guid.Empty, 3, 5.5f),    // medium
        new ToastMessageInfo(ToastMessageSeverity.Big,         Gamemode.Config.UIToastLarge.ValidReference(out guid) ? guid : Guid.Empty, 3, 5.5f),     // big
        new ToastMessageInfo(ToastMessageSeverity.Progress,    Gamemode.Config.UIToastProgress.ValidReference(out guid) ? guid : Guid.Empty, 4, 1.6f),  // progress
        new ToastMessageInfo(ToastMessageSeverity.Tip,         Gamemode.Config.UIToastTip.ValidReference(out guid) ? guid : Guid.Empty, 2, 4f, "Text"), // tip
    };
    public struct ToastChannel
    {
        public byte Channel;
        public ToastMessageInfo Info;
        public ToastMessage Message;
        public float TimeRemaining;
        public bool InUse => TimeRemaining > 0f;
        public bool HasPending = false;
        public ToastChannel(byte channel)
        {
            Info = default;
            Message = default;
            TimeRemaining = 0f;
            Channel = channel;
        }
        public void SetMessage(ToastMessageInfo info, ToastMessage message)
        {
            Info = info;
            Message = message;
            TimeRemaining = info.Time;
        }
        /// <returns><see langword="true"/> if there is a message currently playing on the channel, otherwise <see langword="false"/>.</returns>
        public bool Update(float dt)
        {
            if (TimeRemaining <= 0f) return HasPending;
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                return HasPending;
            }
            return false;
        }
    }
    public ToastChannel[] Channels;
    public void QueueMessage(ToastMessage message, bool priority = false)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ToastMessageInfo info = ToastMessageInfo.Nil;
        for (int i = 0; i < Toasts.Length; i++)
        {
            if (Toasts[i].Type == message.Severity)
            {
                info = Toasts[i];
                break;
            }
        }
        if (info.Guid == Guid.Empty)
        {
            L.LogWarning("Undefined toast message type: " + message.Severity.ToString());
            return;
        }
        if (priority || (_pendingToastMessages.Count(x => x.Value.Channel == info.Channel) == 0 && !Channels[info.Channel].InUse))
            SendToastMessage(message, info);
        else
        {
            _pendingToastMessages.Insert(0, new KeyValuePair<ToastMessage, ToastMessageInfo>(message, info));
            for (int i = 0; i < Channels.Length; i++)
            {
                if (Channels[i].Channel == info.Channel)
                {
                    Channels[i].HasPending = true;
                    break;
                }
            }
        }
    }
    private readonly List<KeyValuePair<ToastMessage, ToastMessageInfo>> _pendingToastMessages = new List<KeyValuePair<ToastMessage, ToastMessageInfo>>();
    private void SendToastMessage(ToastMessage message, ToastMessageInfo info)
    {
        EffectManager.sendUIEffect(info.Id, unchecked((short)info.Id), Player.channel.owner.transportConnection, true, message.Message1 ?? "", message.Message2 ?? "", message.Message3 ?? "");

        if (message.ResendText && !string.IsNullOrEmpty(info.TextContainerName))
        {
            EffectManager.sendUIEffectText(unchecked((short)info.Id), Player.channel.owner.transportConnection, true, info.TextContainerName, message.Message1 ?? message.Message2 ?? message.Message3);
        }
        Channels[info.Channel].SetMessage(info, message);
        for (int i = _pendingToastMessages.Count - 1; i >= 0; i--)
        {
            KeyValuePair<ToastMessage, ToastMessageInfo> t = _pendingToastMessages[i];
            if (t.Key == message)
            {
                _pendingToastMessages.RemoveAt(i);
                break;
            }
        }
    }
    #endregion
    public void StartTracking(Player player)
    {
        Player = player;
        _currentTimeSeconds = 0.0f;
        JoinTime = Time.realtimeSinceStartup;
        byte max = 0;
        bool cont0 = false;
        for (int i = 0; i < Toasts.Length; i++)
        {
            ref ToastMessageInfo toast = ref Toasts[i];
            if (toast.Channel == 0)
                cont0 = true;
            else if (max < toast.Channel)
                max = toast.Channel;
        }
        if (cont0) max++;
        Channels = new ToastChannel[max];
        for (byte i = 0; i < Channels.Length; i++)
            Channels[i] = new ToastChannel(i);
    }
    public void AddPing(float value)
    {
        ++PingBufferIndex;
        PingBuffer[PingBufferIndex % PingBufferSize] = value;
    }
    public void TryUpdateAttackers(ulong newLastAttacker)
    {
        if (newLastAttacker == LastAttacker) return;

        SecondLastAttacker = new KeyValuePair<ulong, DateTime>(LastAttacker, DateTime.Now);
        LastAttacker = newLastAttacker;
    }
    public void ResetAttackers()
    {
        LastAttacker = 0;
        SecondLastAttacker = new KeyValuePair<ulong, DateTime>(0, DateTime.Now);
    }
    public void Update()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float dt = Time.deltaTime;
        _currentTimeSeconds += dt;
        for (int i = 0; i < Channels.Length; i++)
        {
            if (Channels[i].Update(dt))
            {
                ref ToastChannel channel = ref Channels[i];
                for (int j = _pendingToastMessages.Count - 1; j >= 0; j--)
                {
                    KeyValuePair<ToastMessage, ToastMessageInfo> t = _pendingToastMessages[j];
                    if (t.Value.Channel == channel.Channel)
                    {
                        SendToastMessage(t.Key, t.Value);
                        goto next;
                    }
                }
                channel.HasPending = false;
            next:;
            }
        }
    }
    public void CancelDeployment()
    {
        if (CurrentTeleportRequest != null)
        {
            StopCoroutine(CurrentTeleportRequest);
            CurrentTeleportRequest = null;
            PendingDeploy = null;
        }
    }
}
