﻿using SDG.NetTransport;
using System;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Players.UI;

public delegate void PopupButtonPressed(WarfarePlayer player, int button, ref bool consume, ref bool closeWindow);
public delegate void ToastPopupButtonPressed(WarfarePlayer player, int button, in ToastMessage message, ref bool consume, ref bool closeWindow);

[UnturnedUI(BasePath = "Container/MainBox")]
public class PopupUI : UnturnedUI
{
    private readonly IPlayerService _playerService;

    public event PopupButtonPressed? OnButtonPressed;
    public event PopupButtonPressed? OnToastButtonPressed;
    public LabeledStateButton[] Buttons { get; } =
    [
        new LabeledStateButton("ButtonContainer/Button1", "./Button1Label", "./Button1State"),
        new LabeledStateButton("ButtonContainer/Button2", "./Button2Label", "./Button2State"),
        new LabeledStateButton("ButtonContainer/Button3", "./Button3Label", "./Button3State"),
        new LabeledStateButton("ButtonContainer/Button4", "./Button4Label", "./Button4State")
    ];

    public UnturnedImage Image { get; } = new UnturnedImage("Image");
    public PopupUI(AssetConfiguration assetConfig, IPlayerService playerService, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Toasts:Popup"))
    {
        _playerService = playerService;
        for (int i = 0; i < Buttons.Length; ++i)
        {
            Buttons[i].OnClicked += OnButtonClicked;
        }
    }
    public static void SendToastCallback(WarfarePlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui)
    {
        PopupUI popup = (PopupUI)ui;
        if (message.Argument != null)
        {
            popup.SendToPlayer(player.Connection, message.Argument, string.Empty, T.ButtonOK.Translate(player));
            popup.EnableButtons(player.Connection, 1);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            switch (message.Arguments.Length)
            {
                case 1:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, string.Empty, T.ButtonOK.Translate(player));
                    break;
                case 2:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, T.ButtonOK.Translate(player));
                    break;
                case 3:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, message.Arguments[2] ?? T.ButtonOK.Translate(player));
                    break;
                default:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, message.Arguments[2] ?? T.ButtonOK.Translate(player), message.Arguments[3] ?? string.Empty);
                    if (message.Arguments.Length > 4)
                    {
                        if (!string.IsNullOrEmpty(message.Arguments[4]))
                            popup.Buttons[2].SetText(player.Connection, message.Arguments[4]);
                        if (message.Arguments.Length > 5)
                        {
                            if (!string.IsNullOrEmpty(message.Arguments[5]))
                                popup.Buttons[3].SetText(player.Connection, message.Arguments[5]);
                            if (message.Arguments.Length > 6 && !string.IsNullOrEmpty(message.Arguments[6]))
                            {
                                popup.Image.SetImage(player.Connection, message.Arguments[6]);
                            }
                        }
                    }
                    break;
            }

            int buttonCt = 1;
            int ct = Math.Min(6, message.Arguments.Length);
            for (int i = 2; i < ct; ++i)
            {
                if (!string.IsNullOrEmpty(message.Arguments[i]))
                    ++buttonCt;
                else break;
            }
            popup.EnableButtons(player.Connection, Math.Max(1, Math.Min(4, buttonCt)));
        }
        else
        {
            popup.SendToPlayer(player.Connection, string.Empty, string.Empty, T.ButtonOK.Translate(player));
            popup.EnableButtons(player.Connection, 1);
        }
    }
    private void OnButtonClicked(UnturnedButton button, Player player)
    {
        int index = Array.FindIndex(Buttons, x => x.Button == button);
        if (index == -1)
            return;

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);
        bool consumed = false;
        bool closeWindow = true;

        if (warfarePlayer.Component<ToastManager>().TryFindCurrentToastInfo(ToastMessageStyle.Popup, out ToastMessage message))
        {
            if (OnToastButtonPressed != null)
            {
                foreach (ToastPopupButtonPressed action in OnToastButtonPressed.GetInvocationList().Cast<ToastPopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, index, in message, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                warfarePlayer.Component<ToastManager>().SkipExpiration(ToastMessageStyle.Popup);
        }
        else
        {
            if (OnButtonPressed != null)
            {
                foreach (PopupButtonPressed action in OnButtonPressed.GetInvocationList().Cast<PopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, index, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                ClearFromPlayer(warfarePlayer.Connection);
        }
    }
    private void EnableButtons(ITransportConnection connection, int count)
    {
        for (int i = 0; i < Buttons.Length; ++i)
        {
            Buttons[i].Button.SetVisibility(connection, i < count);
        }
    }
}