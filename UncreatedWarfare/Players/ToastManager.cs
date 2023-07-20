using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Players;
public sealed class ToastManager
{
    private static int _channelCount;
    public static ToastMessageInfo[] ToastMessages { get; private set; } = null!;
    public ToastMessageChannel[] Channels { get; }
    public UCPlayer Player { get; }
    public bool HasToasts { get; private set; }
    public bool Hold { get; set; }

    public ToastManager(UCPlayer player)
    {
        Player = player;
        Channels = new ToastMessageChannel[_channelCount];
        for (int i = 0; i < Channels.Length; ++i)
            Channels[i] = new ToastMessageChannel(this, i);
    }
    /// <remarks>Thread Safe</remarks>
    public void Queue(in ToastMessage message)
    {
        CheckOutOfBoundsToastMessageStyle(message.Style);

        if (UCWarfare.IsMainThread)
            QueueIntl(in message);
        else
        {
            ToastMessage msg2 = message;
            UCWarfare.RunOnMainThread(() =>
            {
                QueueIntl(in msg2);
            });
        }
    }
    /// <remarks>Thread Safe</remarks>
    public void SkipExpiration(ToastMessageStyle style)
    {
        CheckOutOfBoundsToastMessageStyle(style);
        ToastMessageInfo info = ToastMessages[(int)style];

        if (UCWarfare.IsMainThread)
            SkipExpirationIntl(info.Channel);
        else
        {
            UCWarfare.RunOnMainThread(() =>
            {
                SkipExpirationIntl(info.Channel);
            });
        }
    }
    /// <remarks>Thread Safe</remarks>
    public void SkipExpiration(int channel)
    {
        if (UCWarfare.IsMainThread)
            SkipExpirationIntl(channel);
        else
        {
            UCWarfare.RunOnMainThread(() =>
            {
                SkipExpirationIntl(channel);
            });
        }
    }
    public void BlockChannelFor(int channel, float time)
    {
        CheckOutOfBoundsChannel(channel);
        Channels[channel].BlockFor(time);
    }
    public bool TryFindCurrentToastInfo(ToastMessageStyle style, out ToastMessage message)
    {
        CheckOutOfBoundsToastMessageStyle(style);

        ToastMessageInfo info = ToastMessages[(int)style];
        ToastMessageChannel channel = Channels[info.Channel];
        bool hasToast = channel.HasToasts && channel.CurrentInfo == info;
        message = hasToast ? channel.CurrentMessage : default;
        return hasToast;
    }
    private static void CheckOutOfBoundsToastMessageStyle(ToastMessageStyle style)
    {
        if ((int)style >= ToastMessages.Length || (int)style < 0)
            throw new ArgumentOutOfRangeException(nameof(style), style, "ToastMessageStyle must be a valid and configured toast style.");
    }
    private void CheckOutOfBoundsChannel(int channel)
    {
        if (channel >= Channels.Length || channel < 0)
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Channel must be a valid and configured channel index (starting at zero).");
    }
    internal void Update()
    {
        if (!HasToasts || Hold)
            return;
        float time = Time.realtimeSinceStartup;
        bool updateAny = false;
        for (int i = 0; i < Channels.Length; ++i)
        {
            ToastMessageChannel channel = Channels[i];
            if (channel.HasToasts && time > channel.ExpireTime)
            {
                channel.Dequeue();
                updateAny = true;
            }
        }

        if (updateAny)
        {
            updateAny = false;
            for (int i = 0; i < Channels.Length; ++i)
            {
                if (Channels[i].HasToasts)
                {
                    updateAny = true;
                    break;
                }
            }

            HasToasts = updateAny;
        }
    }

    private void SkipExpirationIntl(int channel)
    {
        ToastMessageChannel chnl = Channels[channel];
        if (!chnl.HasToasts)
            return;

        chnl.Dequeue();
    }
    private void QueueIntl(in ToastMessage message)
    {
        ToastMessageInfo info = ToastMessages[(int)message.Style];
        ToastMessageChannel channel = Channels[info.Channel];

        if (!Hold)
        {
            if (info is { Inturrupt: true })
            {
                if (info.Key == -1)
                    EffectManager.ClearEffectByGuid(info.Guid, Player.Connection);
                Send(in message, info, channel);
                return;
            }
        }

        if (channel.HasToasts)
            channel.Queue.Enqueue(message);
        else if (Hold)
            channel.HoldMessage(in message);
        else
            Send(in message, info, channel);
    }
    private void Send(in ToastMessage message, ToastMessageInfo info, ToastMessageChannel channel)
    {
        L.LogDebug($"Queued toast: {message.Style}.");
        HasToasts = true;
        channel.UpdateInfo(in message, info);
        if (info.UI != null)
        {
            info.UI.SendToPlayer(Player.Connection);
            info.SendCallback?.Invoke(Player, in message, info, info.UI);
        }
        else if (message.Argument != null)
        {
            EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable, message.Argument);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            switch (message.Arguments.Length)
            {
                case 1:
                    EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable, message.Arguments[0]);
                    break;
                case 2:
                    EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1]);
                    break;
                case 3:
                    EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], message.Arguments[2]);
                    break;
                default:
                    EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], message.Arguments[2], message.Arguments[3]);
                    break;
            }
        }
        else
        {
            EffectManager.sendUIEffect(info.Id, info.Key, Player.Connection, info.Reliable);
        }

        EnableFlags(info);
    }
    internal static void ReloadToastIds()
    {
        ToastMessages[(int)ToastMessageStyle.GameOver].UpdateAsset(Gamemode.Config.UIToastWin);
        ToastMessages[(int)ToastMessageStyle.Large].UpdateAsset(Gamemode.Config.UIToastLarge);
        ToastMessages[(int)ToastMessageStyle.Medium].UpdateAsset(Gamemode.Config.UIToastMedium);
        ToastMessages[(int)ToastMessageStyle.Mini].UpdateAsset(Gamemode.Config.UIToastXP);
        ToastMessages[(int)ToastMessageStyle.ProgressBar].UpdateAsset(Gamemode.Config.UIToastProgress);
        ToastMessages[(int)ToastMessageStyle.Tip].UpdateAsset(Gamemode.Config.UIToastTip);
        ToastMessages[(int)ToastMessageStyle.Popup].UpdateAsset(Gamemode.Config.UIPopup);
        ToastMessages[(int)ToastMessageStyle.FlashingWarning].UpdateAsset(Gamemode.Config.UIFlashingWarning);
    }
    internal static void Init()
    {
        ToastMessageStyle[] vals = (ToastMessageStyle[])typeof(ToastMessageStyle).GetEnumValues();
        int len = vals.Length == 0 ? 0 : ((int)vals.Max() + 1);

        ToastMessages = new ToastMessageInfo[len];
        ToastMessages[(int)ToastMessageStyle.GameOver] = new ToastMessageInfo(ToastMessageStyle.GameOver, 0, Gamemode.Config.UIToastWin);
        ToastMessages[(int)ToastMessageStyle.Large] = new ToastMessageInfo(ToastMessageStyle.Large, 0, Gamemode.Config.UIToastLarge);
        ToastMessages[(int)ToastMessageStyle.Medium] = new ToastMessageInfo(ToastMessageStyle.Medium, 0, Gamemode.Config.UIToastMedium);
        ToastMessages[(int)ToastMessageStyle.Mini] = new ToastMessageInfo(ToastMessageStyle.Mini, 1, Gamemode.Config.UIToastXP);
        ToastMessages[(int)ToastMessageStyle.ProgressBar] = new ToastMessageInfo(ToastMessageStyle.ProgressBar, 2, Gamemode.Config.UIToastProgress, false, true);
        ToastMessages[(int)ToastMessageStyle.Tip] = new ToastMessageInfo(ToastMessageStyle.Tip, 0, Gamemode.Config.UIToastTip);
        ToastMessages[(int)ToastMessageStyle.Popup] = new ToastMessageInfo(ToastMessageStyle.Popup, 3, PopupUI.Instance, PopupUI.SendToastCallback, true, false)
        {
            Duration = 300,
            DisableFlags = EPluginWidgetFlags.ShowCenterDot | EPluginWidgetFlags.ShowInteractWithEnemy,
            EnableFlags = EPluginWidgetFlags.ForceBlur | EPluginWidgetFlags.Modal
        };
        ToastMessages[(int)ToastMessageStyle.FlashingWarning] = new ToastMessageInfo(ToastMessageStyle.FlashingWarning, 4, Gamemode.Config.UIFlashingWarning, true, false);

        int maxChannel = -1;
        for (int i = 0; i < len; ++i)
        {
            if (ToastMessages[i].Channel > maxChannel)
                maxChannel = ToastMessages[i].Channel;
        }

        _channelCount = maxChannel + 1;
    }
    private void EnableFlags(ToastMessageInfo info)
    {
        if (info.DisableFlags != EPluginWidgetFlags.None || info.EnableFlags != EPluginWidgetFlags.None)
        {
            Player.Player.setAllPluginWidgetFlags((Player.Player.pluginWidgetFlags | info.EnableFlags) & ~info.DisableFlags);
            if ((info.EnableFlags & EPluginWidgetFlags.Modal) != 0)
                Player.ModalNeeded = true;
        }
    }
    private void DisableFlags(ToastMessageInfo info)
    {
        if (info.DisableFlags != EPluginWidgetFlags.None || info.EnableFlags != EPluginWidgetFlags.None)
        {
            Player.Player.setAllPluginWidgetFlags((Player.Player.pluginWidgetFlags | info.DisableFlags) & ~info.EnableFlags);
            if ((info.EnableFlags & EPluginWidgetFlags.Modal) != 0)
                Player.ModalNeeded = Player.TeamSelectorData is { IsSelecting: true };
        }
    }
    public sealed class ToastMessageChannel
    {
        public ToastManager Manager { get; }
        public int Channel { get; }
        public ToastMessageInfo? CurrentInfo { get; private set; }
        public ToastMessage CurrentMessage { get; private set; }
        public float ExpireTime { get; private set; }
        public bool HasToasts { get; private set; }
        public Queue<ToastMessage> Queue { get; } = new Queue<ToastMessage>();

        internal ToastMessageChannel(ToastManager manager, int channel)
        {
            Manager = manager;
            Channel = channel;
        }
        internal void BlockFor(float time)
        {
            HasToasts = true;
            Manager.HasToasts = true;
            ExpireTime = Time.realtimeSinceStartup + time;
        }
        internal void HoldMessage(in ToastMessage message)
        {
            HasToasts = true;
            ExpireTime = Time.realtimeSinceStartup;
            CurrentInfo = null;
            Queue.Enqueue(message);
        }
        internal void UpdateInfo(in ToastMessage message, ToastMessageInfo info)
        {
            CurrentMessage = message;
            HasToasts = true;
            ExpireTime = Time.realtimeSinceStartup;
            if (!info.Inturrupt)
            {
                ExpireTime += message.OverrideDuration ?? info.Duration;
            }
            CurrentInfo = info;
        }
        internal void Dequeue()
        {
            if (CurrentInfo != null)
            {
                if (CurrentInfo.RequiresClearing || CurrentInfo.UI == null && CurrentInfo.Key != -1)
                    EffectManager.ClearEffectByGuid(CurrentInfo.Guid, Manager.Player.Connection);
            }
            if (Queue.Count > 0)
            {
                ToastMessage message = Queue.Dequeue();
                ToastMessageInfo info = ToastMessages[(int)message.Style];
                if (CurrentInfo != info && CurrentInfo != null)
                    Manager.DisableFlags(CurrentInfo);
                Manager.Send(in message, info, this);
            }
            else
            {
                if (CurrentInfo != null)
                    Manager.DisableFlags(CurrentInfo);
                HasToasts = false;
                ExpireTime = 0f;
                CurrentInfo = null;
            }
        }
    }
}