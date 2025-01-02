using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.UI;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.UI;

[PlayerComponent]
public sealed class ToastManager : IPlayerComponent, IDisposable
{
    internal static readonly Regex PluginKeyMatch = new Regex(@"\<plugin_\d\/\>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static int _channelCount;
    private static bool _initialized;
    private static IServiceProvider _serviceProvider;

    /// <summary>
    /// List of data for each <see cref="ToastMessageStyle"/> in use.
    /// </summary>
    /// <remarks>Use the integer value of the enum as index.</remarks>
    public static ToastMessageInfo[] ToastMessages { get; private set; } = null!;

    /// <summary>
    /// Overlapping toasts are split up into channels. Each channel can only show one toast at a time.
    /// </summary>
    public ToastMessageChannel[] Channels { get; private set; }

    public WarfarePlayer Player { get; private set; }

    public bool HasToasts { get; private set; }

    /// <summary>
    /// Makes toasts wait until this is set to false before continuing.
    /// </summary>
    public bool Hold { get; set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _serviceProvider = serviceProvider;
        if (!_initialized)
        {
            InitToastData(serviceProvider);
        }

        if (isOnJoin)
        {
            Channels = new ToastMessageChannel[_channelCount];
            for (int i = 0; i < Channels.Length; ++i)
                Channels[i] = new ToastMessageChannel(this, i);

            TimeUtility.updated += Update;
        }
    }

    void IDisposable.Dispose()
    {
        TimeUtility.updated -= Update;
    }

    private static void InitToastData(IServiceProvider serviceProvider)
    {
        AssetConfiguration configuration = serviceProvider.GetRequiredService<AssetConfiguration>();
        ILogger logger = serviceProvider.GetRequiredService<ILogger<ToastManager>>();

        configuration.OnChange += ReloadToastIds;

        ToastMessageStyle[] vals = (ToastMessageStyle[])typeof(ToastMessageStyle).GetEnumValues();
        int len = vals.Length == 0 ? 0 : (int)vals.Max() + 1;

        ToastMessages = new ToastMessageInfo[len];
        ToastMessages[(int)ToastMessageStyle.GameOver] = new ToastMessageInfo(ToastMessageStyle.GameOver, 0, serviceProvider.GetRequiredService<WinToastUI>(), WinToastUI.SendToastCallback, inturrupt: true)
        {
            ResendNames = [ "Canvas/Content/Header", "Canvas/Content/Header/Team1Tickets", "Canvas/Content/Header/Team2Tickets", "Canvas/Content/Header/Team1Image", "Canvas/Content/Header/Team2Image" ],
            ClearableSlots = 3
        };
        ToastMessages[(int)ToastMessageStyle.Large] = new ToastMessageInfo(ToastMessageStyle.Large, 0, configuration.GetAssetLink<EffectAsset>("UI:Toasts:Large"), canResend: true)
        {
            ResendNames = [ "Canvas/Content/Top", "Canvas/Content/Middle", "Canvas/Content/Bottom" ],
            ClearableSlots = 3
        };
        ToastMessages[(int)ToastMessageStyle.Medium] = new ToastMessageInfo(ToastMessageStyle.Medium, 0, configuration.GetAssetLink<EffectAsset>("UI:Toasts:Medium"), canResend: true)
        {
            ResendNames = [ "Canvas/Content/Middle" ],
            ClearableSlots = 1
        };
        ToastMessages[(int)ToastMessageStyle.Mini] = new ToastMessageInfo(ToastMessageStyle.Mini, 1, configuration.GetAssetLink<EffectAsset>("UI:Toasts:Mini"), canResend: true)
        {
            ResendNames = [ "Canvas/Content/Text" ],
            ClearableSlots = 1
        };
        ToastMessages[(int)ToastMessageStyle.ProgressBar] = new ToastMessageInfo(ToastMessageStyle.ProgressBar, 2, configuration.GetAssetLink<EffectAsset>("UI:Toasts:Progress"), inturrupt: true, canResend: true)
        {
            ResendNames = [ "Canvas/Content/Progress", "Canvas/Content/Bar" ],
            ClearableSlots = 1
        };
        ToastMessages[(int)ToastMessageStyle.Tip] = new ToastMessageInfo(ToastMessageStyle.Tip, 0, configuration.GetAssetLink<EffectAsset>("UI:Toasts:Tip"), canResend: true)
        {
            ResendNames = [ "Canvas/Content/Text" ],
            ClearableSlots = 1
        };
        ToastMessages[(int)ToastMessageStyle.Popup] = new ToastMessageInfo(ToastMessageStyle.Popup, 3, serviceProvider.GetRequiredService<PopupUI>(), PopupUI.SendToastCallback, requiresClearing: true)
        {
            Duration = 300,
            DisableFlags = EPluginWidgetFlags.ShowCenterDot | EPluginWidgetFlags.ShowInteractWithEnemy,
            EnableFlags = EPluginWidgetFlags.ForceBlur | EPluginWidgetFlags.Modal
        };
        ToastMessages[(int)ToastMessageStyle.FlashingWarning] = new ToastMessageInfo(ToastMessageStyle.FlashingWarning, 4, configuration.GetValue<IAssetLink<EffectAsset>>("UI:Toasts:Alert"), requiresClearing: true, canResend: true)
        {
            ResendNames = [ "Canvas/Text" ],
            ClearableSlots = 1
        };

        int maxChannel = -1;
        for (int i = 0; i < len; ++i)
        {
            ToastMessageInfo msg = ToastMessages[i];

            if (msg == null)
            {
                logger.LogWarning($"Toast not configured: {(ToastMessageStyle)i}.");
            }
            else if (msg.Channel > maxChannel)
            {
                maxChannel = msg.Channel;
            }
        }

        _channelCount = maxChannel + 1;

        _initialized = true;
    }

    internal static void ReloadToastIds(IConfiguration configuration)
    {
        ToastMessages[(int)ToastMessageStyle.GameOver].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:GameOver"));
        ToastMessages[(int)ToastMessageStyle.Large].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Large"));
        ToastMessages[(int)ToastMessageStyle.Medium].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Medium"));
        ToastMessages[(int)ToastMessageStyle.Mini].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Mini"));
        ToastMessages[(int)ToastMessageStyle.ProgressBar].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Progress"));
        ToastMessages[(int)ToastMessageStyle.Tip].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Tip"));
        ToastMessages[(int)ToastMessageStyle.Popup].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Popup"));
        ToastMessages[(int)ToastMessageStyle.FlashingWarning].UpdateAsset(configuration.GetAssetLink<EffectAsset>("UI:Toasts:Alert"));
    }

    /// <remarks>Thread Safe</remarks>
    public void Queue(in ToastMessage message)
    {
        CheckOutOfBoundsToastMessageStyle(message.Style);

        if (GameThread.IsCurrent)
            QueueIntl(in message);
        else
        {
            ToastMessage msg2 = message;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(Player.DisconnectToken);
                QueueIntl(in msg2);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void SkipExpiration(ToastMessageStyle style)
    {
        CheckOutOfBoundsToastMessageStyle(style);
        ToastMessageInfo info = ToastMessages[(int)style];

        if (GameThread.IsCurrent)
            SkipExpirationIntl(info.Channel);
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(Player.DisconnectToken);
                SkipExpirationIntl(info.Channel);
            });
        }
    }

    /// <remarks>Thread Safe</remarks>
    public void SkipExpiration(int channel)
    {
        if (GameThread.IsCurrent)
            SkipExpirationIntl(channel);
        else
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(Player.DisconnectToken);
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

    private void Update()
    {
        if (!HasToasts || Hold)
            return;
        float time = Time.realtimeSinceStartup;
        bool updateAny = false;
        for (int i = 0; i < Channels.Length; ++i)
        {
            ToastMessageChannel channel = Channels[i];
            if (!channel.HasToasts || time <= channel.ExpireTime)
            {
                continue;
            }

            channel.Dequeue();
            updateAny = true;
        }

        if (!updateAny)
            return;
        
        updateAny = false;
        for (int i = 0; i < Channels.Length; ++i)
        {
            if (!Channels[i].HasToasts)
                continue;
            
            updateAny = true;
            break;
        }

        HasToasts = updateAny;
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

        if (channel.HasToasts && !info.Inturrupt)
            channel.Queue.Enqueue(message);
        else if (Hold)
            channel.HoldMessage(in message);
        else
            Send(in message, info, channel);
    }

    private void Send(in ToastMessage message, ToastMessageInfo info, ToastMessageChannel channel)
    {
        HasToasts = true;
        channel.UpdateInfo(in message, info);
        ushort id = info.Asset.Id;
        if (info.UI != null)
        {
            info.UI.SendToPlayer(Player.Connection);
            info.SendCallback?.Invoke(Player, in message, info, info.UI, _serviceProvider);
        }
        else if (id != 0)
        {
            if (message.Argument != null)
            {
                switch (info.ClearableSlots)
                {
                    case <= 1:
                        EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Argument);
                        break;

                    case 2:
                        EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Argument, string.Empty);
                        break;

                    case 3:
                        EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Argument, string.Empty, string.Empty);
                        break;

                    default:
                        EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Argument, string.Empty, string.Empty, string.Empty);
                        break;
                }

                if (channel.Key != -1)
                {
                    Resend(in message, info);
                }
            }
            else if (message.Arguments != null)
            {
                switch (message.Arguments.Length)
                {
                    case 0:
                        switch (info.ClearableSlots)
                        {
                            case <= 0:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable);
                                break;
                            case 1:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, string.Empty);
                                break;
                            case 2:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, string.Empty, string.Empty);
                                break;
                            case 3:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, string.Empty, string.Empty, string.Empty);
                                break;
                            default:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, string.Empty, string.Empty, string.Empty, string.Empty);
                                break;
                        }
                        break;
                        
                    case 1:
                        switch (info.ClearableSlots)
                        {
                            case <= 1:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0]);
                                break;
                            case 2:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], string.Empty);
                                break;
                            case 3:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], string.Empty, string.Empty);
                                break;
                            default:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], string.Empty, string.Empty, string.Empty);
                                break;
                        }
                        break;

                    case 2:
                        switch (info.ClearableSlots)
                        {
                            case <= 2:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1]);
                                break;
                            case 3:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], string.Empty);
                                break;
                            default:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], string.Empty, string.Empty);
                                break;
                        }
                        break;

                    case 3:
                        switch (info.ClearableSlots)
                        {
                            case <= 3:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], message.Arguments[2]);
                                break;
                            default:
                                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], message.Arguments[2], string.Empty);
                                break;
                        }
                        break;

                    default:
                        EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable, message.Arguments[0], message.Arguments[1], message.Arguments[2], message.Arguments[3]);
                        break;
                }

                Resend(in message, info);
            }
            else
            {
                EffectManager.sendUIEffect(id, info.Key, Player.Connection, info.Reliable);
            }
        }

        EnableFlags(info);
    }

    private static bool CheckResend(in ToastMessage message, ToastMessageInfo info)
    {
        return (message.Resend || info.RequiresResend) && info.CanResend && info.Key != -1;
    }

    private void Resend(in ToastMessage message, ToastMessageInfo info)
    {
        if (!CheckResend(in message, info))
            return;

        if (message.Argument != null)
        {
            if (info.ResendNames.Length > 0)
                EffectManager.sendUIEffectText(info.Key, Player.Connection, info.Reliable, info.ResendNames[0], message.Argument);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            int ct = Math.Min(message.Arguments.Length, info.ResendNames.Length);
            ITransportConnection connection = Player.Connection;
            for (int i = 0; i < ct; ++i)
                EffectManager.sendUIEffectText(info.Key, connection, info.Reliable, info.ResendNames[i], message.Arguments[i]);
        }
    }

    private void EnableFlags(ToastMessageInfo info)
    {
        if (info.DisableFlags != EPluginWidgetFlags.None || info.EnableFlags != EPluginWidgetFlags.None)
        {
            Player.UnturnedPlayer.setAllPluginWidgetFlags((Player.UnturnedPlayer.pluginWidgetFlags | info.EnableFlags) & ~info.DisableFlags);
            if ((info.EnableFlags & EPluginWidgetFlags.Modal) != 0)
            {
                // todo Player.ModalNeeded = true;
            }
        }
    }

    private void DisableFlags(ToastMessageInfo info)
    {
        if (info.DisableFlags != EPluginWidgetFlags.None || info.EnableFlags != EPluginWidgetFlags.None)
        {
            Player.UnturnedPlayer.setAllPluginWidgetFlags((Player.UnturnedPlayer.pluginWidgetFlags | info.DisableFlags) & ~info.EnableFlags);
            if ((info.EnableFlags & EPluginWidgetFlags.Modal) != 0)
            {
                // todo Player.ModalNeeded = Player.TeamSelectorData is { IsSelecting: true };
            }
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
    public sealed class ToastMessageChannel
    {
        public ToastManager Manager { get; }
        public int Channel { get; }
        public ToastMessageInfo? CurrentInfo { get; private set; }
        public ToastMessage CurrentMessage { get; private set; }
        public float ExpireTime { get; private set; }
        public bool HasToasts { get; private set; }
        public short Key { get; private set; }
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
            ExpireTime = Time.realtimeSinceStartup + (message.OverrideDuration ?? info.Duration);
            CurrentInfo = info;
            Key = info.Key;
            if (Key != -1 && !CheckResend(in message, info) && !info.RequiresClearing)
                Key = -1;
        }

        internal void Dequeue()
        {
            if (CurrentInfo != null)
            {
                if ((CurrentInfo.RequiresClearing || CurrentInfo.UI == null && Key != -1) && CurrentInfo.Asset.TryGetGuid(out Guid guid))
                    EffectManager.ClearEffectByGuid(guid, Manager.Player.Connection);
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