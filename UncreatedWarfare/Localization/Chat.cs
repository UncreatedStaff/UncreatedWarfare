using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.Management.Legacy;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Chat
{
    public const int MaxMessageSize = 2047;
    internal static void SendSingleMessage(string text, Color color, EChatMode mode, string? iconURL, bool richText, SteamPlayer recipient)
    {
        ThreadUtil.assertIsGameThread();

        if (Data.SendChatIndividual == null)
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
            L.LogError("Error invoking ChatManager.onServerSendingMessage event: ");
            L.LogError(ex);
        }
        Data.SendChatIndividual.Invoke(ENetReliability.Reliable, recipient.transportConnection, CSteamID.Nil, iconURL ?? string.Empty, mode, color, richText, text);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this Player player, string message, Color color) => SendString(player.channel.owner, message, color);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this UCPlayer player, string message, Color color) => SendString(player.SteamPlayer, message, color);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this SteamPlayer player, string message, Color color)
    {
        if (player?.player == null)
            return;
        if (UCWarfare.IsMainThread)
            SendSingleMessage(message, color, EChatMode.SAY, null, true, player);
        else UCWarfare.RunOnMainThread(() => SendSingleMessage(message, color, EChatMode.SAY, null, true, player));
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this Player player, string message, string hex) => SendString(player.channel.owner, message, hex);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this UCPlayer player, string message, string hex) => SendString(player.SteamPlayer, message, hex);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this SteamPlayer player, string message, string hex)
    {
        if (player?.player == null)
            return;
        if (UCWarfare.IsMainThread)
            SendSingleMessage(message, hex.Hex(), EChatMode.SAY, null, true, player);
        else UCWarfare.RunOnMainThread(() => SendSingleMessage(message, hex.Hex(), EChatMode.SAY, null, true, player));
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this Player player, string message) => SendString(player.channel.owner, message);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this UCPlayer player, string message) => SendString(player.SteamPlayer, message);
    /// <remarks>Thread Safe</remarks>
    public static void SendString(this SteamPlayer player, string message)
    {
        if (player?.player == null)
            return;
        if (UCWarfare.IsMainThread)
            SendSingleMessage(message, Palette.AMBIENT, EChatMode.SAY, null, true, player);
        else UCWarfare.RunOnMainThread(() => SendSingleMessage(message, Palette.AMBIENT, EChatMode.SAY, null, true, player));
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat(this Player player, Translation translation) => SendChat(UCPlayer.FromPlayer(player)!, translation);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat(this SteamPlayer player, Translation translation) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat(this UCPlayer player, Translation translation)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player, out Color textColor, true);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0>(this Player player, Translation<T0> translation, T0 arg) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0>(this SteamPlayer player, Translation<T0> translation, T0 arg) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0>(this UCPlayer player, Translation<T0> translation, T0 arg0)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1>(this Player player, Translation<T0, T1> translation, T0 arg0, T1 arg1) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1>(this SteamPlayer player, Translation<T0, T1> translation, T0 arg0, T1 arg1) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1>(this UCPlayer player, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2>(this Player player, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2>(this SteamPlayer player, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2>(this UCPlayer player, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3>(this Player player, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3>(this SteamPlayer player, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3>(this UCPlayer player, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4>(this Player player, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4>(this UCPlayer player, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5>(this Player player, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5>(this UCPlayer player, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, arg5, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6>(this Player player, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6>(this UCPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7>(this Player player, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7>(this UCPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Player player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this UCPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Player player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this SteamPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    /// <remarks>Thread Safe</remarks>
    public static void SendChat<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UCPlayer player, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (player is not { IsOnline: true })
            return;
        string value = translation.Translate(player.Locale.LanguageInfo, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, out Color textColor, player, player.GetTeam(), player.Save.IMGUI, TranslationFlags.ForChat);
        CheckTranslationLength(player.Locale.LanguageInfo, ref value, translation, ref textColor, player.Save.IMGUI);
        SendTranslationChat(value, translation, textColor, player);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast(LanguageSet set, Translation translation)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0>(LanguageSet set, Translation<T0> translation, T0 arg0)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1>(LanguageSet set, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2>(LanguageSet set, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3>(LanguageSet set, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4>(LanguageSet set, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5>(LanguageSet set, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, arg5, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6>(LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, arg5, arg6, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(LanguageSet set, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out LanguageInfo lang, out Color textColor);
        value = translation.Translate(value, lang, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, null, set.Team, TranslationFlags.ForChat, set.IMGUI);
        CheckTranslationLength(lang, ref value, translation, ref textColor, set.IMGUI);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast(LanguageSet.LanguageSetEnumerator sets, Translation translation)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0>(LanguageSet.LanguageSetEnumerator sets, Translation<T0> translation, T0 arg0)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(LanguageSet.LanguageSetEnumerator sets, Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast(Translation translation)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation));
    }
    private static void BroadcastIntl(Translation translation)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation);
            Broadcast(LanguageSet.OnTeam(2), translation);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0>(Translation<T0> translation, T0 arg0)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0));
    }
    private static void BroadcastIntl<T0>(Translation<T0> translation, T0 arg)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg);
            Broadcast(LanguageSet.OnTeam(2), translation, arg);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1));
    }
    private static void BroadcastIntl<T0, T1>(Translation<T0, T1> translation, T0 arg0, T1 arg1)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2));
    }
    private static void BroadcastIntl<T0, T1, T2>(Translation<T0, T1, T2> translation, T0 arg0, T1 arg1, T2 arg2)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3));
    }
    private static void BroadcastIntl<T0, T1, T2, T3>(Translation<T0, T1, T2, T3> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4>(Translation<T0, T1, T2, T3, T4> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4, T5>(Translation<T0, T1, T2, T3, T4, T5> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4, arg5);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4, arg5);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4, arg5);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4, T5, T6>(Translation<T0, T1, T2, T3, T4, T5, T6> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4, T5, T6, T7>(Translation<T0, T1, T2, T3, T4, T5, T6, T7> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }
    /// <remarks>Thread Safe</remarks>
    public static void Broadcast<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (UCWarfare.IsMainThread)
            BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        else
            UCWarfare.RunOnMainThread(() => BroadcastIntl(translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
    }
    private static void BroadcastIntl<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            Broadcast(LanguageSet.OnTeam(2), translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }
    private static void GetBroadcastTranslationData(in LanguageSet set, Translation translation, out string value, out LanguageInfo lang, out Color textColor)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        lang = set.Language;
        value = translation.Translate(lang, out textColor, set.IMGUI);
    }
    private static void SendTranslationChat(string value, Translation translation, Color textColor, UCPlayer player)
    {
        if (UCWarfare.IsMainThread)
            SendSingleMessage(value, textColor, EChatMode.SAY, null, (translation.Flags & TranslationFlags.NoRichText) == 0, player.SteamPlayer);
        else
            UCWarfare.RunOnMainThread(() => SendSingleMessage(value, textColor, EChatMode.SAY, null, (translation.Flags & TranslationFlags.NoRichText) == 0, player.SteamPlayer));
    }
    private static void CheckTranslationLength(LanguageInfo lang, ref string value, Translation translation, ref Color textColor, bool imgui)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(value) > MaxMessageSize)
        {
            value = translation.Translate((LanguageInfo?)null, out textColor, imgui);
            if (System.Text.Encoding.UTF8.GetByteCount(value) > MaxMessageSize)
            {
                value = translation.Key;
                L.LogWarning(lang + " and default translation for {" + value + "} is too large for chat.", method: "CHAT");
            }
            else
                L.LogWarning(lang + " translation for {" + translation.Key + "} is too large for chat, falling back to default.", method: "CHAT");
        }
    }
}
