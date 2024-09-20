﻿using SDG.NetTransport;
using StackCleaner;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction;
public class ChatService
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<ChatService> _logger;
    private readonly ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>? _sendChatIndividual;

    public const int MaxMessageSize = 2047;

    public ChatService(ITranslationService translationService, ILogger<ChatService> logger)
    {
        _translationService = translationService;
        _logger = logger;

        _sendChatIndividual = ReflectionUtility.FindRpc<ChatManager, ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>>("SendChatEntry");
    }

    /// <summary>
    /// Send a raw message directly to a player, and replace TMPro rich text with Unity tags if needed.
    /// </summary>
    public void Send(WarfarePlayer player, string text, Color color, EChatMode mode, string? iconUrl, bool richText)
    {
        if (richText)
        {
            Color? c = TranslationFormattingUtility.ExtractColor(text, out int index, out int length);
            color = c ?? color;
            text = player.Save.IMGUI
                ? TranslationFormattingUtility.CreateIMGUIString(text.AsSpan(index, length))
                : text.Substring(index, length);
        }

        if (GameThread.IsCurrent)
        {
            SendRawMessage(text, color, mode, iconUrl, richText, player.SteamPlayer);
        }
        else
        {
            SteamPlayer steamPlayer = player.SteamPlayer;
            string text2 = text;
            Color c2 = color;
            EChatMode mode2 = mode;
            string? icon2 = iconUrl;
            bool rt = richText;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (steamPlayer.player != null)
                    SendRawMessage(text2, c2, mode2, icon2, rt, steamPlayer);
            });
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(WarfarePlayer player, string text)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        Color32? textColor;
        bool rt;
        if (text == null)
        {
            text = string.Empty;
            textColor = null;
            rt = false;
        }
        else
        {
            if (player.Save.IMGUI)
                text = TranslationFormattingUtility.CreateIMGUIString(text);
            textColor = TranslationFormattingUtility.ExtractColor(text, out int startIndex, out int length);
            ReadOnlySpan<char> textSpan = text.AsSpan(startIndex, length);
            ReadOnlySpan<char> truncated = FormattingUtility.TruncateUtf8Bytes(textSpan, MaxMessageSize, out _);
            if (truncated.Length != textSpan.Length)
            {
                _logger.LogWarning("Raw text too long for chat message: \"{0}\".", text);
                text = new string(truncated);
            }

            rt = true;
        }

        if (GameThread.IsCurrent)
        {
            SendRawMessage(text, textColor ?? Color.white, EChatMode.SAY, null, rt, player.SteamPlayer);
        }
        else
        {
            string vl2 = text;
            SteamPlayer steamPlayer = player.SteamPlayer;
            Color cl2 = textColor ?? Color.white;
            bool rt2 = rt;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (steamPlayer.player != null)
                    SendRawMessage(vl2, cl2, EChatMode.SAY, null, rt2, steamPlayer);
            });
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text);
            return;
        }

        text ??= string.Empty;
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.IMGUI)
            text = TranslationFormattingUtility.CreateIMGUIString(text);
        if (user.IsTerminal)
        {
            text = TerminalColorHelper.ConvertRichTextToVirtualTerminalSequences(text, _translationService.TerminalColoring);
        }

        SendTranslationMessage(text, user);
    }

    /// <summary>
    /// Send a raw unlocalized string to a player with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(WarfarePlayer player, string text, ConsoleColor textColor)
    {
        Send(player, text, TerminalColorHelper.FromConsoleColor(textColor));
    }

    /// <summary>
    /// Send a raw unlocalized string to a player with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(WarfarePlayer player, string text, Color textColor)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        textColor.a = 1f;

        bool rt;
        if (text == null)
        {
            text = string.Empty;
            rt = false;
        }
        else
        {
            if (player.Save.IMGUI)
                text = TranslationFormattingUtility.CreateIMGUIString(text);
            ReadOnlySpan<char> truncated = FormattingUtility.TruncateUtf8Bytes(text, MaxMessageSize, out _);
            if (truncated.Length != text.Length)
            {
                _logger.LogWarning("Raw text too long for chat message: \"{0}\".", text);
                text = new string(truncated);
            }

            rt = true;
        }

        if (GameThread.IsCurrent)
        {
            SendRawMessage(text, textColor, EChatMode.SAY, null, rt, player.SteamPlayer);
        }
        else
        {
            string vl2 = text;
            SteamPlayer steamPlayer = player.SteamPlayer;
            Color cl2 = textColor;
            bool rt2 = rt;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (steamPlayer.player != null)
                    SendRawMessage(vl2, cl2, EChatMode.SAY, null, rt2, steamPlayer);
            });
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a user with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, Color textColor)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, textColor);
            return;
        }

        Send(user, text, (Color32)textColor);
    }

    /// <summary>
    /// Send a raw unlocalized string to a user with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, ConsoleColor textColor)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, TerminalColorHelper.FromConsoleColor(textColor));
            return;
        }

        text ??= string.Empty;
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.IMGUI)
            text = TranslationFormattingUtility.CreateIMGUIString(text);
        if (user.IsTerminal)
        {
            text = TerminalColorHelper.ConvertRichTextToVirtualTerminalSequences(text, _translationService.TerminalColoring);
            if (_translationService.TerminalColoring is StackColorFormatType.ANSIColor or StackColorFormatType.ExtendedANSIColor)
            {
                text = TerminalColorHelper.WrapMessageWithColor(textColor, text);
            }
        }
        else
        {
            text = _translationService.ValueFormatter.Colorize(text, TerminalColorHelper.FromConsoleColor(textColor), TranslationOptions.TranslateWithUnityRichText);
        }

        SendTranslationMessage(text, user);
    }

    /// <summary>
    /// Send a raw unlocalized string to a user with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, Color32 textColor)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, (Color)textColor);
            return;
        }

        textColor.a = 255;

        text ??= string.Empty;
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.IMGUI)
            text = TranslationFormattingUtility.CreateIMGUIString(text);
        if (user.IsTerminal)
        {
            text = TerminalColorHelper.ConvertRichTextToVirtualTerminalSequences(text, _translationService.TerminalColoring);
            text = _translationService.TerminalColoring switch
            {
                StackColorFormatType.ExtendedANSIColor => TerminalColorHelper.WrapMessageWithColor(TerminalColorHelper.ToArgb(textColor), text),
                StackColorFormatType.ANSIColor => TerminalColorHelper.WrapMessageWithColor(TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(textColor)), text),
                _ => text
            };
        }
        else
        {
            text = _translationService.ValueFormatter.Colorize(text, textColor, TranslationOptions.TranslateWithUnityRichText);
        }

        SendTranslationMessage(text, user);
    }

    /// <summary>
    /// Send a 0-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public void Send(WarfarePlayer player, Translation translation)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 1-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0>(WarfarePlayer player, Translation<T0> translation, T0 arg0)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 2-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1>(WarfarePlayer player, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 3-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2>(WarfarePlayer player, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 4-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3>(WarfarePlayer player, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 5-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 6-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 7-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 8-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 9-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 10-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player);
    }

    /// <summary>
    /// Send a 0-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public void Send(ICommandUser user, Translation translation)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 1-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0>(ICommandUser user, Translation<T0> translation, T0 arg0)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 2-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1>(ICommandUser user, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 3-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2>(ICommandUser user, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 4-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3>(ICommandUser user, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 5-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4>(ICommandUser user, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 6-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 7-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 8-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 9-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Send a 10-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return;
        }

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, user, canUseIMGUI: user.IMGUI);
        SendTranslationMessage(value, user);
    }

    /// <summary>
    /// Broadcast a 0-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public void Broadcast(in LanguageSet set, Translation translation)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 1-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0>(in LanguageSet set, Translation<T0> translation, T0 arg0)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 2-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1>(in LanguageSet set, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 3-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2>(in LanguageSet set, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 4-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3>(in LanguageSet set, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 5-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4>(in LanguageSet set, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 6-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 7-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 8-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 9-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 10-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set);
    }

    /// <summary>
    /// Broadcast a 0-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast(LanguageSetEnumerator set, Translation translation)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        Translation t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t);
            }
        });
    }

    /// <summary>
    /// Broadcast a 1-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0>(LanguageSetEnumerator set, Translation<T0> translation, T0 arg0)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0;
        Translation<T0> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0);
            }
        });
    }

    /// <summary>
    /// Broadcast a 2-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1>(LanguageSetEnumerator set, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1;
        Translation<T0, T1> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1);
            }
        });
    }

    /// <summary>
    /// Broadcast a 3-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2>(LanguageSetEnumerator set, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2;
        Translation<T0, T1, T2> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 4-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3;
        Translation<T0, T1, T2, T3> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3);
            }
        });
    }

    /// <summary>
    /// Broadcast a 5-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4;
        Translation<T0, T1, T2, T3, T4> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4);
            }
        });
    }

    /// <summary>
    /// Broadcast a 6-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5;
        Translation<T0, T1, T2, T3, T4, T5> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5);
            }
        });
    }

    /// <summary>
    /// Broadcast a 7-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6;
        Translation<T0, T1, T2, T3, T4, T5, T6> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6);
            }
        });
    }

    /// <summary>
    /// Broadcast a 8-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7);
            }
        });
    }

    /// <summary>
    /// Broadcast a 9-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7; T8 a8 = arg8;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7, a8);
            }
        });
    }

    /// <summary>
    /// Broadcast a 10-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7; T8 a8 = arg8; T9 a9 = arg9;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> t = translation;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9);
            }
        });
    }

    /// <summary>
    /// Broadcast a 0-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast(Translation translation)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation);
    }

    /// <summary>
    /// Broadcast a 1-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0>(Translation<T0> translation, T0 arg0)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0);
    }

    /// <summary>
    /// Broadcast a 2-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1);
    }

    /// <summary>
    /// Broadcast a 3-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2);
    }

    /// <summary>
    /// Broadcast a 4-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    /// Broadcast a 5-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    /// Broadcast a 6-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5);
    }

    /// <summary>
    /// Broadcast a 7-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    /// <summary>
    /// Broadcast a 8-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }

    /// <summary>
    /// Broadcast a 9-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }

    /// <summary>
    /// Broadcast a 10-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }

    /// <summary>
    /// Send a raw message directly to a player without processing.
    /// </summary>
    private void SendRawMessage(string text, Color color, EChatMode mode, string? iconURL, bool richText, SteamPlayer recipient)
    {
        GameThread.AssertCurrent();

        iconURL ??= string.Empty;

        if (_sendChatIndividual == null)
        {
            ChatManager.serverSendMessage(text, color, null, recipient, mode, iconURL, richText);
            return;
        }

        try
        {
            ChatManager.onServerSendingMessage?.Invoke(ref text, ref color, null, recipient, mode, ref iconURL, ref richText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ChatManager.onServerSendingMessage event.");
        }

        _sendChatIndividual.Invoke(ENetReliability.Reliable, recipient.transportConnection, CSteamID.Nil, iconURL, mode, color, richText, text);
    }

    /// <summary>
    /// Send a raw message directly to a player without processing.
    /// </summary>
    private void SendRawMessageBatch(string text, Color color, EChatMode mode, string? iconURL, bool richText, PooledTransportConnectionList transportConnections)
    {
        GameThread.AssertCurrent();

        iconURL ??= string.Empty;

        if (_sendChatIndividual == null)
        {
            foreach (ITransportConnection tc in transportConnections)
            {
                SteamPlayer? pl = Provider.findPlayer(tc);
                if (pl != null)
                    ChatManager.serverSendMessage(text, color, null, pl, mode, iconURL, richText);
            }

            return;
        }

        _sendChatIndividual.Invoke(ENetReliability.Reliable, transportConnections, CSteamID.Nil, iconURL, mode, color, richText, text);
    }

    private void SendTranslationMessage(string value, Color textColor, Translation translation, WarfarePlayer player)
    {
        if (GameThread.IsCurrent)
        {
            CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
            SendRawMessage(value, textColor, EChatMode.SAY, null, (translation.Options & TranslationOptions.NoRichText) == 0, player.SteamPlayer);
        }
        else
        {
            string vl2 = value;
            WarfarePlayer pl2 = player;
            LanguageInfo lang2 = player.Locale.LanguageInfo;
            Color cl2 = textColor;
            Translation tr2 = translation;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (!pl2.IsOnline)
                    return;

                CheckTranslationLength(lang2, ref vl2, tr2, ref cl2, pl2.Save.IMGUI);
                SendRawMessage(vl2, cl2, EChatMode.SAY, null, (tr2.Options & TranslationOptions.NoRichText) == 0, pl2.SteamPlayer);
            });
        }
    }

    private static void SendTranslationMessage(string value, ICommandUser user)
    {
        if (GameThread.IsCurrent)
        {
            user.SendMessage(value);
        }
        else
        {
            string vl2 = value;
            ICommandUser user2 = user;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                user2.SendMessage(vl2);
            });
        }
    }

    private void SendTranslationSet(string value, Color textColor, Translation translation, in LanguageSet set)
    {
        if (GameThread.IsCurrent)
        {
            CheckTranslationLength(set.Language, ref value, translation, ref textColor, set.IMGUI);
            SendRawMessageBatch(value, textColor, EChatMode.SAY, null, (translation.Options & TranslationOptions.NoRichText) == 0, set.GatherTransportConnections());
        }
        else
        {
            string vl2 = value;
            WarfarePlayer[] players = set.Players.ToArray();
            LanguageInfo lang = set.Language;
            bool imgui = set.IMGUI;
            Color cl2 = textColor;
            Translation tr2 = translation;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                CheckTranslationLength(lang, ref vl2, tr2, ref cl2, imgui);
                PooledTransportConnectionList list = Data.GetPooledTransportConnectionList(players.Length);
                for (int i = 0; i < players.Length; ++i)
                {
                    WarfarePlayer p = players[i];
                    if (p.IsOnline)
                        list.Add(p.Connection);
                }

                SendRawMessageBatch(vl2, cl2, EChatMode.SAY, null, (tr2.Options & TranslationOptions.NoRichText) == 0, list);
            });
        }
    }

    /// <summary>
    /// Use the translation key instead of the text if the text is too long for chat.
    /// </summary>
    private void CheckTranslationLength(LanguageInfo lang, ref string value, Translation translation, ref Color textColor, bool imgui)
    {
        ReadOnlySpan<char> truncated = FormattingUtility.TruncateUtf8Bytes(value, MaxMessageSize, out _);
        if (truncated.Length == value.Length)
        {
            return;
        }

        _logger.LogWarning("Language {0} translation for {1}/{2} (IMGUI = {3}) is too large for a chat message.",
            lang.Code, translation.Key, translation.Collection, imgui
        );
        if (!lang.IsDefault)
        {
            value = translation.Translate(out textColor, imgui);

            truncated = FormattingUtility.TruncateUtf8Bytes(value, MaxMessageSize, out _);
            if (truncated.Length == value.Length)
            {
                return;
            }

            value = new string(truncated);
            _logger.LogWarning("Default language {0} translation for {1}/{2} (IMGUI = {3}) is too large for a chat message.",
                _translationService.LanguageService.GetDefaultLanguage().Code, translation.Key, translation.Collection, imgui
            );
        }
        else
        {
            value = new string(truncated);
        }
    }
}