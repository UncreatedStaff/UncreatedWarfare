using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Chat
{
    const int MAX_CHAT_MESSAGE_SIZE = 2047;
    internal static void SendSingleMessage(string text, Color color, EChatMode mode, string? iconURL, bool richText, SteamPlayer recipient)
    {
        try
        {
            ThreadUtil.assertIsGameThread();
        }
        catch
        {
            L.LogWarning("Tried to send a chat message on non-game thread.");
            return;
        }
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
    public static void SendString(this Player player, string message, Color color) => SendString(player.channel.owner, message, color);
    public static void SendString(this UCPlayer player, string message, Color color) => SendString(player.SteamPlayer, message, color);
    public static void SendString(this SteamPlayer player, string message, Color color)
    {
        SendSingleMessage(message, color, EChatMode.SAY, null, true, player);
    }
    public static void SendString(this Player player, string message, string hex) => SendString(player.channel.owner, message, hex);
    public static void SendString(this UCPlayer player, string message, string hex) => SendString(player.SteamPlayer, message, hex);
    public static void SendString(this SteamPlayer player, string message, string hex)
    {
        SendSingleMessage(message, hex.Hex(), EChatMode.SAY, null, true, player);
    }
    public static void SendString(this Player player, string message) => SendString(player.channel.owner, message);
    public static void SendString(this UCPlayer player, string message) => SendString(player.SteamPlayer, message);
    public static void SendString(this SteamPlayer player, string message)
    {
        SendSingleMessage(message, Palette.AMBIENT, EChatMode.SAY, null, true, player);
    }
    public static void SendChat(this Player player, Translation translation) => SendChat(UCPlayer.FromPlayer(player)!, translation);
    public static void SendChat(this SteamPlayer player, Translation translation) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation);
    public static void SendChat(this UCPlayer player, Translation translation)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T>(this Player player, Translation<T> translation, T arg) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg);
    public static void SendChat<T>(this SteamPlayer player, Translation<T> translation, T arg) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg);
    public static void SendChat<T>(this UCPlayer player, Translation<T> translation, T arg)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2>(this Player player, Translation<T1, T2> translation, T1 arg1, T2 arg2) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2);
    public static void SendChat<T1, T2>(this SteamPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2);
    public static void SendChat<T1, T2>(this UCPlayer player, Translation<T1, T2> translation, T1 arg1, T2 arg2)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3>(this Player player, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3);
    public static void SendChat<T1, T2, T3>(this SteamPlayer player, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3);
    public static void SendChat<T1, T2, T3>(this UCPlayer player, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4>(this Player player, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4);
    public static void SendChat<T1, T2, T3, T4>(this SteamPlayer player, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4);
    public static void SendChat<T1, T2, T3, T4>(this UCPlayer player, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5>(this Player player, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5);
    public static void SendChat<T1, T2, T3, T4, T5>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5);
    public static void SendChat<T1, T2, T3, T4, T5>(this UCPlayer player, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5, T6>(this Player player, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void SendChat<T1, T2, T3, T4, T5, T6>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void SendChat<T1, T2, T3, T4, T5, T6>(this UCPlayer player, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7>(this Player player, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7>(this UCPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8>(this Player player, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8>(this UCPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Player player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this UCPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Player player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => SendChat(UCPlayer.FromPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this SteamPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) => SendChat(UCPlayer.FromSteamPlayer(player)!, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    public static void SendChat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this UCPlayer player, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        GetTranslationData(player, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player, player.GetTeam(), translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        SendTranslationChat(value, translation, textColor, player);
    }
    public static void Broadcast(LanguageSet set, Translation translation)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T>(LanguageSet set, Translation<T> translation, T arg1)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2>(LanguageSet set, Translation<T1, T2> translation, T1 arg1, T2 arg2)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3>(LanguageSet set, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4>(LanguageSet set, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5>(LanguageSet set, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6>(LanguageSet set, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7>(LanguageSet set, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8>(LanguageSet set, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(LanguageSet set, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LanguageSet set, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        GetBroadcastTranslationData(in set, translation, out string value, out string lang, out Color textColor);
        value = translation.Translate(value, lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, null, set.Team, translation.Flags | TranslationFlags.ForChat);
        CheckTranslationLength(lang, ref value, translation, ref textColor);
        while (set.MoveNext())
            SendTranslationChat(value, translation, textColor, set.Next);
    }
    public static void Broadcast(IEnumerable<LanguageSet> sets, Translation translation)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation);
    }
    public static void Broadcast<T>(IEnumerable<LanguageSet> sets, Translation<T> translation, T arg1)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1);
    }
    public static void Broadcast<T1, T2>(IEnumerable<LanguageSet> sets, Translation<T1, T2> translation, T1 arg1, T2 arg2)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2);
    }
    public static void Broadcast<T1, T2, T3>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3);
    }
    public static void Broadcast<T1, T2, T3, T4>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4);
    }
    public static void Broadcast<T1, T2, T3, T4, T5>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IEnumerable<LanguageSet> sets, Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        foreach (LanguageSet set in sets)
            Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
    }
    public static void Broadcast(Translation translation)
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
    public static void Broadcast<T>(Translation<T> translation, T arg)
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
    public static void Broadcast<T1, T2>(Translation<T1, T2> translation, T1 arg1, T2 arg2)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2);
        }
    }
    public static void Broadcast<T1, T2, T3>(Translation<T1, T2, T3> translation, T1 arg1, T2 arg2, T3 arg3)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3);
        }
    }
    public static void Broadcast<T1, T2, T3, T4>(Translation<T1, T2, T3, T4> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5>(Translation<T1, T2, T3, T4, T5> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6>(Translation<T1, T2, T3, T4, T5, T6> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5, arg6);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7>(Translation<T1, T2, T3, T4, T5, T6, T7> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
    }
    public static void Broadcast<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if ((translation.Flags & TranslationFlags.PerPlayerTranslation) == TranslationFlags.PerPlayerTranslation)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                SendChat(PlayerManager.OnlinePlayers[i], translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        else if ((translation.Flags & TranslationFlags.PerTeamTranslation) == TranslationFlags.PerTeamTranslation)
        {
            Broadcast(LanguageSet.OnTeam(1), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            Broadcast(LanguageSet.OnTeam(2), translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        else
        {
            foreach (LanguageSet set in LanguageSet.All())
                Broadcast(set, translation, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
    }
    private static void GetTranslationData(UCPlayer player, Translation translation, out string value, out string lang, out Color textColor)
    {
        if (player is null || player.Player == null) throw new ArgumentNullException(nameof(player));
        if (translation is null) throw new ArgumentNullException(nameof(translation));

        if (Data.Languages is null || !Data.Languages.TryGetValue(player.Steam64, out lang))
            lang = L.DEFAULT;

        value = translation.Translate(lang, out textColor);
    }
    private static void GetBroadcastTranslationData(in LanguageSet set, Translation translation, out string value, out string lang, out Color textColor)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        lang = set.Language;
        value = translation.Translate(lang, out textColor);
    }
    private static void SendTranslationChat(string value, Translation translation, Color textColor, UCPlayer player)
    {
        SendSingleMessage(value, textColor, EChatMode.SAY, null, (translation.Flags & TranslationFlags.NoRichText) == 0, player.SteamPlayer);
    }
    private static void CheckTranslationLength(string lang, ref string value, Translation translation, ref Color textColor)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(value) > MAX_CHAT_MESSAGE_SIZE)
        {
            value = translation.Translate(null, out textColor);
            if (System.Text.Encoding.UTF8.GetByteCount(value) > MAX_CHAT_MESSAGE_SIZE)
            {
                value = translation.Key;
                L.LogWarning(lang + " and default translation for {" + value + "} is too large for chat.");
            }
            else
                L.LogWarning(lang + " translation for {" + translation.Key + "} is too large for chat, falling back to default.");
        }
    }
}
