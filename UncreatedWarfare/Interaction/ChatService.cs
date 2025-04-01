using SDG.NetTransport;
using StackCleaner;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Interaction;

public class ChatService
{
    public delegate void SendingChatMessage(WarfarePlayer recipient, string text, Color color, EChatMode mode, string? iconURL, bool richText, WarfarePlayer? fromPlayer, ref bool shouldReplicate);

    private readonly ITranslationService _translationService;
    private readonly ILogger<ChatService> _logger;
    private readonly IPlayerService _playerService;
    private readonly ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>? _sendChatIndividual;

    public const int MaxMessageSize = 2047;

    public event SendingChatMessage? OnSendingChatMessage;

    public ChatService(ITranslationService translationService, ILogger<ChatService> logger, IPlayerService playerService)
    {
        _translationService = translationService;
        _logger = logger;
        _playerService = playerService;

        _sendChatIndividual = ReflectionUtility.FindRpc<ChatManager, ClientStaticMethod<CSteamID, string, EChatMode, Color, bool, string>>("SendChatEntry");
    }

    /// <summary>
    /// Send a raw message directly to a player, and replace TMPro rich text with Unity tags if needed.
    /// </summary>
    public void Send(WarfarePlayer player, string text, Color color, EChatMode mode, string? iconUrl, bool richText, WarfarePlayer? fromPlayer = null)
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
            SendRawMessage(text, color, mode, iconUrl, richText, player, fromPlayer);
        }
        else
        {
            WarfarePlayer wp = player;
            string text2 = text;
            Color c2 = color;
            EChatMode mode2 = mode;
            string? icon2 = iconUrl;
            bool rt = richText;
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (wp.IsOnline)
                    SendRawMessage(text2, c2, mode2, icon2, rt, wp, fromPlayer2);
            });
        }
    }

    /// <summary>
    /// Send a raw message directly to all players, and replace TMPro rich text with Unity tags if needed.
    /// </summary>
    public void Broadcast(string text, Color color, EChatMode mode, string? iconUrl, bool richText, WarfarePlayer? fromPlayer = null)
    {
        if (!richText)
        {
            BroadcastNonRichText(text, color, mode, iconUrl, fromPlayer);
            return;
        }

        if (GameThread.IsCurrent)
        {
            BroadcastRichTextGameThread(text, color, mode, iconUrl, fromPlayer);
        }
        else
        {
            string text2 = text;
            Color c2 = color;
            EChatMode mode2 = mode;
            string? icon2 = iconUrl;
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                BroadcastRichTextGameThread(text2, c2, mode2, icon2, fromPlayer2);
            });
        }
    }

    /// <summary>
    /// Send a raw message directly to a set of players, and replace TMPro rich text with Unity tags if needed.
    /// </summary>
    public void Broadcast(in LanguageSet players, string text, Color color, EChatMode mode, string? iconUrl, bool richText, WarfarePlayer? fromPlayer = null)
    {
        if (!richText)
        {
            if (GameThread.IsCurrent)
            {
                BroadcastNonRichTextGameThread(text, color, mode, iconUrl, players, fromPlayer);
            }
            else
            {
                string text2 = text;
                Color c2 = color;
                EChatMode mode2 = mode;
                string? icon2 = iconUrl;
                LanguageSet set = players.Preserve();
                WarfarePlayer? fromPlayer2 = fromPlayer;
                UniTask.Create(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    BroadcastNonRichTextGameThread(text2, c2, mode2, icon2, set, fromPlayer2);
                });
            }
            return;
        }

        if (GameThread.IsCurrent)
        {
            BroadcastRichTextGameThread(text, color, mode, iconUrl, players, fromPlayer);
        }
        else
        {
            string text2 = text;
            Color c2 = color;
            EChatMode mode2 = mode;
            string? icon2 = iconUrl;
            LanguageSet set = players.Preserve();
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                BroadcastRichTextGameThread(text2, c2, mode2, icon2, set, fromPlayer2);
            });
        }
    }

    private void BroadcastNonRichText(string text, Color color, EChatMode mode, string? iconUrl, WarfarePlayer? fromPlayer)
    {
        if (GameThread.IsCurrent)
        {
            BroadcastNonRichTextGameThread(text, color, mode, iconUrl, fromPlayer);
        }
        else
        {
            string text2 = text;
            Color c2 = color;
            EChatMode mode2 = mode;
            string? icon2 = iconUrl;
            WarfarePlayer? fromPlayer2 = null;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                BroadcastNonRichTextGameThread(text2, c2, mode2, icon2, fromPlayer2);
            });
        }
    }

    private void BroadcastRichTextGameThread(string text, Color color, EChatMode mode, string? iconUrl, WarfarePlayer? fromPlayer)
    {
        foreach (LanguageSet set in _translationService.SetOf.AllPlayers())
        {
            BroadcastRichTextGameThread(text, color, mode, iconUrl, set, fromPlayer);
        }
    }

    private void BroadcastRichTextGameThread(string text, Color color, EChatMode mode, string? iconUrl, LanguageSet set, WarfarePlayer? fromPlayer)
    {
        Color? c = TranslationFormattingUtility.ExtractColor(text, out int index, out int length);
        color = c ?? color;
        text = set.IMGUI
            ? TranslationFormattingUtility.CreateIMGUIString(text.AsSpan(index, length))
            : text.Substring(index, length);

        PooledTransportConnectionList list = set.GatherTransportConnections();
        if (OnSendingChatMessage != null)
        {
            RemoveDisallowedFromTcList(ref set, text, color, mode, iconUrl, list, fromPlayer);
            if (list.Count == 0)
                return;
        }

        SendRawMessageBatch(text, color, mode, iconUrl, true, list, fromPlayer);
    }

    private void BroadcastNonRichTextGameThread(string text, Color color, EChatMode mode, string? iconUrl, WarfarePlayer? fromPlayer)
    {
        PooledTransportConnectionList list;
        if (OnSendingChatMessage != null)
        {
            list = null!;
            foreach (WarfarePlayer player in _playerService.OnlinePlayers)
            {
                bool shouldAllow = true;
                OnSendingChatMessage?.Invoke(player, text, color, mode, iconUrl, true, fromPlayer, ref shouldAllow);
                if (shouldAllow)
                {
                    (list ??= TransportConnectionPoolHelper.Claim(Provider.clients.Count)).Add(player.Connection);
                }
            }

            if (list == null)
                return;
        }
        else
        {
            list = Provider.GatherClientConnections();
        }

        SendRawMessageBatch(text, color, mode, iconUrl, false, list, fromPlayer);
    }

    private void BroadcastNonRichTextGameThread(string text, Color color, EChatMode mode, string? iconUrl, LanguageSet set, WarfarePlayer? fromPlayer)
    {
        PooledTransportConnectionList list = set.GatherTransportConnections();
        if (OnSendingChatMessage != null)
        {
            RemoveDisallowedFromTcList(ref set, text, color, mode, iconUrl, list, fromPlayer);
            if (list.Count == 0)
                return;
        }

        SendRawMessageBatch(text, color, mode, iconUrl, false, list, fromPlayer);
    }

    private void RemoveDisallowedFromTcList(ref LanguageSet set, string text, Color color, EChatMode mode, string? iconUrl, PooledTransportConnectionList list, WarfarePlayer? fromPlayer)
    {
        int ind = 0;
        while (set.MoveNext())
        {
            bool shouldAllow = true;
            OnSendingChatMessage?.Invoke(set.Next, text, color, mode, iconUrl, true, fromPlayer, ref shouldAllow);
            if (!shouldAllow)
            {
                list.RemoveAt(ind);
            }
            else
            {
                ++ind;
            }
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(WarfarePlayer player, string text, WarfarePlayer? fromPlayer = null)
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
            ReadOnlySpan<char> truncated = StringUtility.TruncateUtf8Bytes(textSpan, MaxMessageSize, out _);
            if (truncated.Length != textSpan.Length)
            {
                _logger.LogWarning("Raw text too long for chat message: \"{0}\".", text);
                text = new string(truncated);
            }

            rt = true;
        }

        if (GameThread.IsCurrent)
        {
            SendRawMessage(text, textColor ?? Color.white, EChatMode.SAY, null, rt, player, fromPlayer);
        }
        else
        {
            string vl2 = text;
            WarfarePlayer wp = player;
            Color cl2 = textColor ?? Color.white;
            bool rt2 = rt;
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (wp.IsOnline)
                    SendRawMessage(vl2, cl2, EChatMode.SAY, null, rt2, wp, fromPlayer2);
            });
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, fromPlayer);
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
    public void Send(WarfarePlayer player, string text, ConsoleColor textColor, WarfarePlayer? fromPlayer = null)
    {
        Send(player, text, TerminalColorHelper.FromConsoleColor(textColor), fromPlayer);
    }

    /// <summary>
    /// Send a raw unlocalized string to a player with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(WarfarePlayer player, string text, Color textColor, WarfarePlayer? fromPlayer = null)
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
            ReadOnlySpan<char> truncated = StringUtility.TruncateUtf8Bytes(text, MaxMessageSize, out _);
            if (truncated.Length != text.Length)
            {
                _logger.LogWarning("Raw text too long for chat message: \"{0}\".", text);
                text = new string(truncated);
            }

            rt = true;
        }

        if (GameThread.IsCurrent)
        {
            SendRawMessage(text, textColor, EChatMode.SAY, null, rt, player, fromPlayer);
        }
        else
        {
            string vl2 = text;
            WarfarePlayer wp = player;
            Color cl2 = textColor;
            bool rt2 = rt;
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (wp.IsOnline)
                    SendRawMessage(vl2, cl2, EChatMode.SAY, null, rt2, wp, fromPlayer2);
            });
        }
    }

    /// <summary>
    /// Send a raw unlocalized string to a user with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, Color textColor, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, textColor, fromPlayer);
            return;
        }

        Send(user, text, (Color32)textColor, fromPlayer);
    }

    /// <summary>
    /// Send a raw unlocalized string to a user with a given background color.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send(ICommandUser user, string text, ConsoleColor textColor, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, TerminalColorHelper.FromConsoleColor(textColor), fromPlayer);
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
                text = TerminalColorHelper.WrapMessageWithTerminalColorSequence(textColor, text);
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
    public void Send(ICommandUser user, string text, Color32 textColor, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, text, (Color)textColor, fromPlayer);
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
                StackColorFormatType.ExtendedANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToArgb(textColor), text),
                StackColorFormatType.ANSIColor => TerminalColorHelper.WrapMessageWithTerminalColorSequence(TerminalColorHelper.ToConsoleColor(TerminalColorHelper.ToArgb(textColor)), text),
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
    public void Send(WarfarePlayer player, Translation translation, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 1-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0>(WarfarePlayer player, Translation<T0> translation, T0 arg0, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 2-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1>(WarfarePlayer player, Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 3-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2>(WarfarePlayer player, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 4-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3>(WarfarePlayer player, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 5-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 6-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 7-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 8-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 9-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 10-arg translation to a player.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(WarfarePlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer? fromPlayer = null)
    {
        if (!(player ?? throw new ArgumentNullException(nameof(player))).IsOnline)
            return;

        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, out Color textColor, canUseIMGUI: true);
        SendTranslationMessage(value, textColor, translation, player, fromPlayer);
    }

    /// <summary>
    /// Send a 0-arg translation to a user.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline users are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    public void Send(ICommandUser user, Translation translation, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, fromPlayer);
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
    public void Send<T0>(ICommandUser user, Translation<T0> translation, T0 arg0, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, fromPlayer);
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
    public void Send<T0, T1>(ICommandUser user, Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, fromPlayer);
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
    public void Send<T0, T1, T2>(ICommandUser user, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, fromPlayer);
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
    public void Send<T0, T1, T2, T3>(ICommandUser user, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4>(ICommandUser user, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4, T5>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4, T5, T6>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, fromPlayer);
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
    public void Send<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ICommandUser user, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer? fromPlayer = null)
    {
        if (user is WarfarePlayer player)
        {
            Send(player, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, fromPlayer);
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
    public void Broadcast(in LanguageSet set, Translation translation, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 1-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0>(in LanguageSet set, Translation<T0> translation, T0 arg0, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 2-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1>(in LanguageSet set, Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 3-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2>(in LanguageSet set, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 4-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3>(in LanguageSet set, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 5-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4>(in LanguageSet set, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 6-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 7-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 8-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 9-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 10-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(in LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer? fromPlayer = null)
    {
        if (translation == null)
            throw new ArgumentNullException(nameof(translation));

        if (set.Count <= 0)
            return;

        string value = translation.Translate(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, in set, out Color textColor, canUseIMGUI: true);
        SendTranslationSet(value, textColor, translation, in set, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 0-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException">The translation has arguments.</exception>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast(LanguageSetEnumerator set, Translation translation, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        Translation t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 1-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0>(LanguageSetEnumerator set, Translation<T0> translation, T0 arg0, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0;
        Translation<T0> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 2-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1>(LanguageSetEnumerator set, Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1;
        Translation<T0, T1> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 3-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2>(LanguageSetEnumerator set, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2;
        Translation<T0, T1, T2> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 4-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3;
        Translation<T0, T1, T2, T3> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 5-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4;
        Translation<T0, T1, T2, T3, T4> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 6-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5;
        Translation<T0, T1, T2, T3, T4, T5> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 7-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6;
        Translation<T0, T1, T2, T3, T4, T5, T6> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 8-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 9-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7; T8 a8 = arg8;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7, a8, fromPlayer2);
            }
        });
    }

    /// <summary>
    /// Broadcast a 10-arg translation to a set of players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(LanguageSetEnumerator set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer? fromPlayer = null)
    {
        if (GameThread.IsCurrent)
        {
            while (set.MoveNext())
            {
                Broadcast(in set.Set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, fromPlayer);
            }

            set.Dispose();
            return;
        }

        LanguageSetEnumerator.Cache c = set.ToCache();
        set.Dispose();

        T0 a0 = arg0; T1 a1 = arg1; T2 a2 = arg2; T3 a3 = arg3; T4 a4 = arg4; T5 a5 = arg5; T6 a6 = arg6; T7 a7 = arg7; T8 a8 = arg8; T9 a9 = arg9;
        Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> t = translation;
        WarfarePlayer? fromPlayer2 = fromPlayer;
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();

            LanguageSet[] sets = c.Sets!;
            for (int i = 0; i < sets.Length; ++i)
            {
                Broadcast(in sets[i], t, a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, fromPlayer2);
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
    public void Broadcast(Translation translation, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 1-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0>(Translation<T0> translation, T0 arg0, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 2-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 3-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 4-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 5-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 6-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 7-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 8-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 9-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, fromPlayer);
    }

    /// <summary>
    /// Broadcast a 10-arg translation to all players.
    /// </summary>
    /// <remarks>Thread-safe. Messages to offline players are ignored.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, WarfarePlayer? fromPlayer = null)
    {
        Broadcast(_translationService.SetOf.AllPlayers(), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, fromPlayer);
    }

    /// <summary>
    /// Send a raw message directly to a player without processing.
    /// </summary>
    private void SendRawMessage(string text, Color color, EChatMode mode, string? iconURL, bool richText, WarfarePlayer recipient, WarfarePlayer? fromPlayer)
    {
        GameThread.AssertCurrent();

        iconURL ??= string.Empty;

        bool shouldAllow = true;
        OnSendingChatMessage?.Invoke(recipient, text, color, mode, iconURL, richText, fromPlayer, ref shouldAllow);
        if (!shouldAllow)
            return;

        if (_sendChatIndividual == null)
        {
            ChatManager.serverSendMessage(text, color, fromPlayer?.SteamPlayer, recipient.SteamPlayer, mode, iconURL, richText);
            return;
        }

        try
        {
            ChatManager.onServerSendingMessage?.Invoke(ref text, ref color, fromPlayer?.SteamPlayer, recipient.SteamPlayer, mode, ref iconURL, ref richText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking ChatManager.onServerSendingMessage event.");
        }

        _sendChatIndividual.Invoke(ENetReliability.Reliable, recipient.Connection, fromPlayer?.Steam64 ?? CSteamID.Nil, iconURL, mode, color, richText, text);
    }

    /// <summary>
    /// Send a raw message directly to a player without processing.
    /// </summary>
    private void SendRawMessageBatch(string text, Color color, EChatMode mode, string? iconURL, bool richText, PooledTransportConnectionList transportConnections, WarfarePlayer? fromPlayer)
    {
        GameThread.AssertCurrent();

        iconURL ??= string.Empty;
        
        if (_sendChatIndividual == null)
        {
            SteamPlayer? fromSteamPlayer = fromPlayer?.SteamPlayer;
            foreach (ITransportConnection tc in transportConnections)
            {
                SteamPlayer? pl = Provider.findPlayer(tc);
                if (pl != null)
                    ChatManager.serverSendMessage(text, color, fromSteamPlayer, pl, mode, iconURL, richText);
            }

            return;
        }

        if (fromPlayer != null)
        {
            text = text.Replace("%SPEAKER%", fromPlayer.Names.CharacterName);
        }

        _sendChatIndividual.Invoke(ENetReliability.Reliable, transportConnections, fromPlayer?.Steam64 ?? CSteamID.Nil, iconURL, mode, color, richText, text);
    }

    private void SendTranslationMessage(string value, Color textColor, Translation translation, WarfarePlayer player, WarfarePlayer? fromPlayer)
    {
        if (GameThread.IsCurrent)
        {
            CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
            SendRawMessage(value, textColor, EChatMode.SAY, null, (translation.Options & TranslationOptions.NoRichText) == 0, player, fromPlayer);
        }
        else
        {
            string vl2 = value;
            WarfarePlayer pl2 = player;
            LanguageInfo lang2 = player.Locale.LanguageInfo;
            Color cl2 = textColor;
            Translation tr2 = translation;
            WarfarePlayer? fromPlayer2 = null;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                if (!pl2.IsOnline)
                    return;

                CheckTranslationLength(lang2, ref vl2, tr2, ref cl2, pl2.Save.IMGUI);
                SendRawMessage(vl2, cl2, EChatMode.SAY, null, (tr2.Options & TranslationOptions.NoRichText) == 0, pl2, fromPlayer2);
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

    private void SendTranslationSet(string value, Color textColor, Translation translation, in LanguageSet set, WarfarePlayer? fromPlayer)
    {
        if (GameThread.IsCurrent)
        {
            CheckTranslationLength(set.Language, ref value, translation, ref textColor, set.IMGUI);

            PooledTransportConnectionList list = set.GatherTransportConnections();
            if (OnSendingChatMessage != null)
            {
                LanguageSet set2 = set;
                RemoveDisallowedFromTcList(ref set2, value, textColor, EChatMode.SAY, null, list, fromPlayer);
                if (list.Count == 0)
                    return;
            }

            SendRawMessageBatch(value, textColor, EChatMode.SAY, null, (translation.Options & TranslationOptions.NoRichText) == 0, set.GatherTransportConnections(), fromPlayer);
        }
        else
        {
            string vl2 = value;
            LanguageInfo lang = set.Language;
            bool imgui = set.IMGUI;
            Color cl2 = textColor;
            Translation tr2 = translation;
            LanguageSet set2 = set.Preserve();
            WarfarePlayer? fromPlayer2 = fromPlayer;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                CheckTranslationLength(lang, ref vl2, tr2, ref cl2, imgui);

                PooledTransportConnectionList list = set2.GatherTransportConnections();
                if (OnSendingChatMessage != null)
                {
                    RemoveDisallowedFromTcList(ref set2, vl2, cl2, EChatMode.SAY, null, list, fromPlayer2);
                    if (list.Count == 0)
                        return;
                }

                SendRawMessageBatch(vl2, cl2, EChatMode.SAY, null, (tr2.Options & TranslationOptions.NoRichText) == 0, list, fromPlayer2);
            });
        }
    }

    /// <summary>
    /// Use the translation key instead of the text if the text is too long for chat.
    /// </summary>
    private void CheckTranslationLength(LanguageInfo lang, ref string value, Translation translation, ref Color textColor, bool imgui)
    {
        ReadOnlySpan<char> truncated = StringUtility.TruncateUtf8Bytes(value, MaxMessageSize, out _);
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

            truncated = StringUtility.TruncateUtf8Bytes(value, MaxMessageSize, out _);
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