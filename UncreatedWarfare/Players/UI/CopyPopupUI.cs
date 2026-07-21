using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Framework.UI;
using Uncreated.Framework.UI.Presets;
using Uncreated.Framework.UI.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Collections;

namespace Uncreated.Warfare.Players.UI;

public delegate void CopyPopupButtonPressed(WarfarePlayer player, ref bool consume, ref bool closeWindow);
public delegate void ToastCopyPopupButtonPressed(WarfarePlayer player, in ToastMessage message, ref bool consume, ref bool closeWindow);
public delegate void CopyPopupTextPressed(WarfarePlayer player, in ToastMessage message, ref bool consume);
public delegate void ToastCopyPopupTextPressed(WarfarePlayer player, in ToastMessage message, ref bool consume);

/// <summary>
/// Variation of <see cref="PopupUI"/> that has a read-only text box for copying text.
/// </summary>
[UnturnedUI(BasePath = "Container/MainBox")]
public class CopyPopupUI : UnturnedUI
{
    private readonly IPlayerService _playerService;

    public event CopyPopupButtonPressed? OnButtonPressed;
    public event CopyPopupTextPressed? OnTextPressed;

    public event ToastCopyPopupButtonPressed? OnToastButtonPressed;
    public event ToastCopyPopupTextPressed? OnToastTextPressed;

    public UnturnedLabel CopyText { get; } = new UnturnedLabel("InputField");
    public UnturnedLabel Description { get; } = new UnturnedLabel("DescriptionLabel");

    public LabeledStateButton Button { get; } = new LabeledStateButton("Button", "./Label", "./State");
    public UnturnedButton TextButton { get; } = new UnturnedButton("DescriptionLabel/TextButton");

    public CopyPopupUI(AssetConfiguration assetConfig, IPlayerService playerService, ILoggerFactory loggerFactory)
        : base(loggerFactory, assetConfig.GetAssetLink<EffectAsset>("UI:Toasts:CopyPopup"), staticKey: true)
    {
        _playerService = playerService;
        Button.OnClicked += OnButtonClicked;
        TextButton.OnClicked += OnTextButtonClicked;
    }

    public static void SendToastCallback(WarfarePlayer player, in ToastMessage message, ToastMessageInfo info, UnturnedUI ui, IServiceProvider serviceProvider)
    {
        CommonTranslations translations = serviceProvider.GetRequiredService<TranslationInjection<CommonTranslations>>().Value;
        CopyPopupUI popup = (CopyPopupUI)ui;
        if (message.Argument != null)
        {
            popup.SendToPlayer(
                player.Connection,
                translations.PopupCopyText.Translate(player),
                string.Empty,
                translations.PopupOkay.Translate(player)
            );
            popup.CopyText.SetText(player, message.Argument);
            popup.Description.Hide(player);
        }
        else if (message.Arguments is { Length: > 0 })
        {
            switch (message.Arguments.Length)
            {
                case 1:
                    popup.SendToPlayer(
                        player.Connection,
                        translations.PopupCopyText.Translate(player),
                        string.Empty,
                        translations.PopupOkay.Translate(player)
                    );
                    popup.Description.Hide(player);
                    break;
                case 2:
                    popup.SendToPlayer(
                        player.Connection,
                        message.Arguments[1] ?? translations.PopupCopyText.Translate(player),
                        string.Empty,
                        translations.PopupOkay.Translate(player)
                    );
                    popup.Description.Hide(player);
                    break;
                case 3:
                    popup.SendToPlayer(
                        player.Connection,
                        message.Arguments[1] ?? translations.PopupCopyText.Translate(player),
                        FormatDescription(player, message.Arguments[2]),
                        translations.PopupOkay.Translate(player)
                    );
                    if (message.Arguments[2] == null)
                    {
                        popup.Description.Hide(player);
                    }
                    break;
                default:
                    message.Arguments[3] ??= PopupUI.Okay;
                    popup.SendToPlayer(
                        player.Connection,
                        message.Arguments[1] ?? translations.PopupCopyText.Translate(player),
                        FormatDescription(player, message.Arguments[2]),
                        CheckForPresetArguments(player, message.Arguments[3], translations)
                    );
                    if (message.Arguments[2] == null)
                    {
                        popup.Description.Hide(player);
                    }
                    break;
            }

            popup.CopyText.SetText(player, message.Arguments[0]);
        }
        else
        {
            popup.SendToPlayer(
                player.Connection,
                translations.PopupCopyText.Translate(player),
                string.Empty,
                translations.PopupOkay.Translate(player)
            );
            popup.Description.Hide(player);
        }
    }

    [return: NotNullIfNotNull(nameof(messageArgument))]
    private static string? CheckForPresetArguments(WarfarePlayer player, string? messageArgument, CommonTranslations translations)
    {
        if (messageArgument is null)
            return null;
        int index = -1;
        if (ReferenceEquals(PopupUI.Okay, messageArgument))
            index = 0;
        else if (ReferenceEquals(PopupUI.Cancel, messageArgument))
            index = 1;
        else if (ReferenceEquals(PopupUI.No, messageArgument))
            index = 2;
        else if (ReferenceEquals(PopupUI.Yes, messageArgument))
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


    private static string FormatDescription(WarfarePlayer player, string? messageArgument)
    {
        if (messageArgument == null)
            return string.Empty;

        string copy = player.SteamPlayer.clientPlatform == EClientPlatform.Mac
            ? "⌘<space=0.15em>+<space=0.15em>C"
            : "Ctrl<space=0.15em>+<space=0.15em>C";

        return messageArgument.Replace("<copy/>", copy) ?? string.Empty;
    }

    private void OnTextButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);
        bool consumed = false;
        bool closeWindow = true;

        if (warfarePlayer.Component<ToastManager>().TryFindCurrentToastInfo(ToastMessageStyle.CopyPopup, out ToastMessage message))
        {
            if (message.State is CopyPopupCallbacks callbacks)
            {
                callbacks.Text?.Invoke(warfarePlayer, in message, ref consumed);
            }

            ToastCopyPopupTextPressed? onToastTextPressed = OnToastTextPressed;
            if (!consumed && onToastTextPressed != null)
            {
                foreach (ToastCopyPopupButtonPressed action in onToastTextPressed.GetInvocationList().Cast<ToastCopyPopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, in message, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                warfarePlayer.Component<ToastManager>().SkipExpiration(ToastMessageStyle.CopyPopup);
        }
        else
        {
            CopyPopupTextPressed? onTextPressed = OnTextPressed;
            if (onTextPressed != null)
            {
                foreach (CopyPopupButtonPressed action in onTextPressed.GetInvocationList().Cast<CopyPopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                ClearFromPlayer(warfarePlayer.Connection);
        }
    }

    private void OnButtonClicked(UnturnedButton button, Player player)
    {
        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(player);
        bool consumed = false;
        bool closeWindow = true;

        if (warfarePlayer.Component<ToastManager>().TryFindCurrentToastInfo(ToastMessageStyle.CopyPopup, out ToastMessage message))
        {
            if (message.State is CopyPopupCallbacks callbacks)
            {
                callbacks.Button?.Invoke(warfarePlayer, in message, ref consumed, ref closeWindow);
            }

            ToastCopyPopupButtonPressed? onToastButtonPressed = OnToastButtonPressed;
            if (!consumed && onToastButtonPressed != null)
            {
                foreach (ToastCopyPopupButtonPressed action in onToastButtonPressed.GetInvocationList().Cast<ToastCopyPopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, in message, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                warfarePlayer.Component<ToastManager>().SkipExpiration(ToastMessageStyle.CopyPopup);
        }
        else
        {
            CopyPopupButtonPressed? onButtonPressed = OnButtonPressed;
            if (onButtonPressed != null)
            {
                foreach (CopyPopupButtonPressed action in onButtonPressed.GetInvocationList().Cast<CopyPopupButtonPressed>())
                {
                    action.Invoke(warfarePlayer, ref consumed, ref closeWindow);
                    if (consumed)
                        break;
                }
            }

            if (closeWindow)
                ClearFromPlayer(warfarePlayer.Connection);
        }
    }
}

public struct CopyPopupCallbacks
{
    public ToastCopyPopupButtonPressed? Button;
    public ToastCopyPopupTextPressed? Text;

    public CopyPopupCallbacks(ToastCopyPopupButtonPressed? button = null, ToastCopyPopupTextPressed? text = null)
    {
        Button = button;
        Text = text;
    }
}