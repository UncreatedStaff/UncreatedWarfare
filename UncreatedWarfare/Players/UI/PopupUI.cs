using Microsoft.Extensions.DependencyInjection;
using SDG.NetTransport;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;

namespace Uncreated.Warfare.Players.UI;

public delegate void PopupButtonPressed(WarfarePlayer player, int button, ref bool consume, ref bool closeWindow);
public delegate void ToastPopupButtonPressed(WarfarePlayer player, int button, in ToastMessage message, ref bool consume, ref bool closeWindow);

[UnturnedUI(BasePath = "Container/MainBox")]
public class PopupUI : UnturnedUI
{
    public const string Cancel = "Cancel";
    public const string Okay = "OK";
    public const string Yes = "Yes";
    public const string No = "No";

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
    public PopupUI(AssetConfiguration assetConfig, IPlayerService playerService, ILoggerFactory loggerFactory) : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Toasts:Popup"), staticKey: true)
    {
        _playerService = playerService;
        for (int i = 0; i < Buttons.Length; ++i)
        {
            Buttons[i].OnClicked += OnButtonClicked;
        }
    }

    public static void SendToastCallback(WarfarePlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui, IServiceProvider serviceProvider)
    {
        CommonTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<CommonTranslations>>().Value;
        PopupUI popup = (PopupUI)ui;
        if (message.Argument != null)
        {
            popup.SendToPlayer(player.Connection, message.Argument, string.Empty, translations.PopupOkay.Translate(player));
            popup.EnableButtons(player.Connection, 1);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            int buttonCt;
            switch (message.Arguments.Length)
            {
                case 1:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, string.Empty, translations.PopupOkay.Translate(player));
                    buttonCt = 1;
                    break;
                case 2:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, translations.PopupOkay.Translate(player));
                    buttonCt = 1;
                    break;
                case 3:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, CheckForPresetArguments(player, message.Arguments[2], translations) ?? translations.PopupOkay.Translate(player));
                    buttonCt = 1;
                    break;
                default:
                    popup.SendToPlayer(player.Connection, message.Arguments[0] ?? string.Empty, message.Arguments[1] ?? string.Empty, CheckForPresetArguments(player, message.Arguments[2], translations) ?? translations.PopupOkay.Translate(player), CheckForPresetArguments(player, message.Arguments[3], translations) ?? string.Empty);
                    buttonCt = 1 + (!string.IsNullOrEmpty(message.Arguments[3]) ? 1 : 0);
                    if (message.Arguments.Length > 4)
                    {
                        string arg4 = message.Arguments[4];
                        if (!string.IsNullOrEmpty(arg4))
                        {
                            popup.Buttons[buttonCt].SetText(player.Connection, CheckForPresetArguments(player, arg4, translations));
                            ++buttonCt;
                        }
                        if (message.Arguments.Length > 5)
                        {
                            string arg5 = message.Arguments[5];
                            if (!string.IsNullOrEmpty(arg5))
                            {
                                popup.Buttons[buttonCt].SetText(player.Connection, CheckForPresetArguments(player, arg5, translations));
                                ++buttonCt;
                            }

                            string? img;
                            if (message.Arguments.Length > 6 && !string.IsNullOrEmpty(img = message.Arguments[6]))
                            {
                                popup.Image.SetImage(player.Connection, img);
                            }
                        }
                    }
                    break;
            }

            popup.EnableButtons(player.Connection, Math.Clamp(buttonCt, 1, 4));
        }
        else
        {
            popup.SendToPlayer(player.Connection, string.Empty, string.Empty, translations.PopupOkay.Translate(player));
            popup.EnableButtons(player.Connection, 1);
        }
    }

    [return: NotNullIfNotNull(nameof(messageArgument))]
    private static string? CheckForPresetArguments(WarfarePlayer player, string? messageArgument, CommonTranslations translations)
    {
        if (messageArgument is null)
            return null;
        int index = -1;
        if (ReferenceEquals(Okay, messageArgument))
            index = 0;
        else if (ReferenceEquals(Cancel, messageArgument))
            index = 1;
        else if (ReferenceEquals(No, messageArgument))
            index = 2;
        else if (ReferenceEquals(Yes, messageArgument))
            index = 3;

        if (index == -1)
            return messageArgument;

        return (index switch
        {
            0 => translations.PopupOkay,
            1 => translations.PopupCancel,
            2 => translations.PopupYes,
            _ => translations.PopupNo
        }).Translate(player);
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

public static class PopupExtensions
{
    public static void SendPopup(this WarfarePlayer player)
    {

    }
}