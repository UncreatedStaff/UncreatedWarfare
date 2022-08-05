﻿using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public static class Localization
{
    public const string UNITY_RICH_TEXT_COLOR_BASE_START = "<color=#";
    public const string RICH_TEXT_COLOR_END = ">";
    public const string TMPRO_RICH_TEXT_COLOR_BASE = "<#";
    public const string RICH_TEXT_COLOR_CLOSE = "</color>";
    [Obsolete("Use the new generics system instead.")]
    public static class Common
    {
        public const string NOT_ENABLED = "not_enabled";
        public const string NOT_IMPLEMENTED = "todo";
        public const string CORRECT_USAGE = "correct_usage";
        public const string CONSOLE_ONLY = "command_e_no_console";
        public const string PLAYERS_ONLY = "command_e_no_player";
        public const string PLAYER_NOT_FOUND = "command_e_player_not_found";
        public const string UNKNOWN_ERROR = "command_e_unknown_error";
        public const string GAMEMODE_ERROR = "command_e_gamemode";
        public const string NO_PERMISSIONS = "no_permissions";
        public const string NO_PERMISSIONS_ON_DUTY = "no_permissions_on_duty";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Colorize(string hex, string inner, TranslationFlags flags)
    {
        return (flags & TranslationFlags.NoColor) == TranslationFlags.NoColor ? inner : (((flags & TranslationFlags.TranslateWithUnityRichText) == TranslationFlags.TranslateWithUnityRichText)
            ? (UNITY_RICH_TEXT_COLOR_BASE_START + hex + RICH_TEXT_COLOR_END + inner + RICH_TEXT_COLOR_CLOSE)
            : (TMPRO_RICH_TEXT_COLOR_BASE + hex + RICH_TEXT_COLOR_END + inner + RICH_TEXT_COLOR_CLOSE));
    }
    [Obsolete("Use the new generics system instead.")]
    public static string ObjectTranslate(string key, string language, params object[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Translation? newTranslation = Translation.FromLegacyId(key);

        if (newTranslation is not null)
        {
            string s = newTranslation.Translate(language);
            try
            {
                return string.Format(s, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
            }
        }

        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        if (data.TryGetValue(key, out TranslationData translation))
        {
            try
            {
                return string.Format(translation.Original, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else if (language != L.DEFAULT)
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            if (data.TryGetValue(key, out translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else
        {
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
        }
    }
    [Obsolete("Use the new generics system instead.")]
    public static string ObjectTranslate(string key, ulong player, params object[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (key == null)
        {
            string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? string.Empty : ": ")}{args}");
            return args;
        }

        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : string.Empty;
        }

        Translation? newTranslation = Translation.FromLegacyId(key);
        if (player == 0)
        {
            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(L.DEFAULT);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(L.DEFAULT, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                if (data.TryGetValue(key, out TranslationData translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = L.DEFAULT;
            }
            else lang = L.DEFAULT;

            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(lang);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else if (data.TryGetValue(key, out TranslationData translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
    }
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, UCPlayer player, params string[] formatting) =>
        Translate(key, player.Steam64, formatting);
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, UCPlayer player, out Color color, params string[] formatting) =>
        Translate(key, player.Steam64, out color, formatting);
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, SteamPlayer player, params string[] formatting) =>
        Translate(key, player.playerID.steamID.m_SteamID, formatting);
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, SteamPlayer player, out Color color, params string[] formatting) =>
        Translate(key, player.playerID.steamID.m_SteamID, out color, formatting);
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, Player player, params string[] formatting) =>
        Translate(key, player.channel.owner.playerID.steamID.m_SteamID, formatting);
    [Obsolete("Use the new generics system instead.")]
    public static string Translate(string key, Player player, out Color color, params string[] formatting) =>
        Translate(key, player.channel.owner.playerID.steamID.m_SteamID, out color, formatting);
    [Obsolete("Use the new generics system instead.")]
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the Original message (non-color removed)
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="player">The player to check language on, pass 0 to use the <see cref="L.DEFAULT">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <returns>A localized string based on the player's language.</returns>
    public static string Translate(string key, ulong player, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (key == null)
        {
            string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? string.Empty : ": ")}{args}");
            return args;
        }
        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : string.Empty;
        }
        Translation? newTranslation = Translation.FromLegacyId(key);
        if (player == 0)
        {
            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(L.DEFAULT);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(L.DEFAULT, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                if (data.TryGetValue(key, out TranslationData translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = L.DEFAULT;
            }
            else lang = L.DEFAULT;
            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(lang);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else if (data.TryGetValue(key, out TranslationData translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
    }
    [Obsolete("Use the new generics system instead.")]
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="player">The player to check language on, pass 0 to use the <see cref="L.DEFAULT">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <param name="color">Color of the message.</param>
    /// <returns>A localized string based on the player's language.</returns>
    public static string Translate(string key, ulong player, out Color color, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (key == null)
        {
            string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? string.Empty : ": ")}{args}");
            color = UCWarfare.GetColor("default");
            return args;
        }
        if (key.Length == 0)
        {
            color = UCWarfare.GetColor("default");
            return formatting.Length > 0 ? string.Join(", ", formatting) : string.Empty;
        }
        Translation? newTranslation = Translation.FromLegacyId(key);
        if (player == 0)
        {
            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(L.DEFAULT, out color);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(L.DEFAULT, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        color = translation.Color;
                        try
                        {
                            return string.Format(translation.Message, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                if (data.TryGetValue(key, out TranslationData translation))
                {
                    color = translation.Color;
                    try
                    {
                        return string.Format(translation.Message, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = L.DEFAULT;
            }
            else lang = L.DEFAULT;
            if (newTranslation is not null)
            {
                string s = newTranslation.Translate(lang, out color);
                try
                {
                    return string.Format(s, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                }
            }

            if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        color = translation.Color;
                        try
                        {
                            return string.Format(translation.Message, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else if (data.TryGetValue(key, out TranslationData translation))
            {
                color = translation.Color;
                try
                {
                    return string.Format(translation.Message, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                color = UCWarfare.GetColor("default");
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
    }
    [Obsolete("Use the new generics system instead.")]

    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="language">The first language to translate with, pass null to use <see cref="L.DEFAULT">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <param name="color">Color of the message.</param>
    /// <returns>A localized string based on <paramref name="language"/>.</returns>
    public static string Translate(string key, string language, out Color color, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Translation? newTranslation = Translation.FromLegacyId(key);

        if (newTranslation is not null)
        {
            string s = newTranslation.Translate(language, out color);
            try
            {
                return string.Format(s, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
            }
        }

        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        if (data.TryGetValue(key, out TranslationData translation))
        {
            color = translation.Color;
            try
            {
                return string.Format(translation.Message, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
                return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else if (language != L.DEFAULT)
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            if (data.TryGetValue(key, out translation))
            {
                color = translation.Color;
                try
                {
                    return string.Format(translation.Message, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                color = UCWarfare.GetColor("default");
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else
        {
            color = UCWarfare.GetColor("default");
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
        }
    }
    [Obsolete("Use the new generics system instead.")]
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the message with color still in it.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="language">The first language to translate with, pass null to use <see cref="L.DEFAULT">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <returns>A localized string based on <paramref name="language"/>.</returns>
    public static string Translate(string key, string language, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Translation? newTranslation = Translation.FromLegacyId(key);

        if (newTranslation is not null)
        {
            string s = newTranslation.Translate(language);
            try
            {
                return string.Format(s, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
            }
        }

        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
        }
        if (data.TryGetValue(key, out TranslationData translation))
        {
            try
            {
                return string.Format(translation.Original, formatting);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else if (language != L.DEFAULT)
        {
            if (!Data.Localization.TryGetValue(L.DEFAULT, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            if (data.TryGetValue(key, out translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
            }
        }
        else
        {
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : string.Empty);
        }
    }

    public static string Translate(Translation translation, UCPlayer? player) =>
        Translate(translation, player is null ? 0 : player.Steam64);
    public static string Translate(Translation translation, ulong player)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang);
    }
    public static string Translate(Translation translation, ulong player, out Color color)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, out color);
    }
    public static string Translate<T>(Translation<T> translation, UCPlayer? player, T arg)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2>(Translation<T1, T2> translation, UCPlayer? player, T1 arg1, T2 arg2)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3>(Translation<T1, T2, T3> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4>(Translation<T1, T2, T3, T4> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5>(Translation<T1, T2, T3, T4, T5> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6>(Translation<T1, T2, T3, T4, T5, T6> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7>(Translation<T1, T2, T3, T4, T5, T6, T7> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8>(Translation<T1, T2, T3, T4, T5, T6, T7, T8> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, player, player is null ? 0 : player.GetTeam());
    }
    public static string Translate<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Translation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> translation, UCPlayer? player, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.Translate(lang, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, UCPlayer? player, object[] formatting)
    {
        if (player == null || !Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, formatting, player, player is null ? 0 : player.GetTeam());
    }
    public static string TranslateUnsafe(Translation translation, ulong player, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, formatting, null, 0);
    }
    public static string TranslateUnsafe(Translation translation, ulong player, out Color color, object[] formatting)
    {
        if (player == 0 || !Data.Languages.TryGetValue(player, out string lang))
            lang = L.DEFAULT;
        return translation.TranslateUnsafe(lang, out color, formatting, null, 0);
    }
    public static string GetTimeFromSeconds(this int seconds, ulong player)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(player);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural)    .Translate(player)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural)      .Translate(player)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(player)}" +
               $"{(overflow == 0 ? string.Empty :           $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural)  .Translate(player)}")}";
    }
    public static string GetTimeFromSeconds(this int seconds, IPlayer player)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(player);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(player)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(player)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural)    .Translate(player)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(player)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural)      .Translate(player)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(player)}" +
               $"{(overflow == 0 ? string.Empty :           $" {(T.TimeAnd).Translate(player)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural)  .Translate(player)}")}";
    }
    public static string GetTimeFromSeconds(this int seconds, string language)
    {
        if (seconds < 0)
            return T.TimePermanent.Translate(language);
        if (seconds == 0)
            seconds = 1;
        if (seconds < 60) // < 1 minute
            return seconds.ToString(Data.Locale) + ' ' + (seconds == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language);
        int val;
        int overflow;
        if (seconds < 3600) // < 1 hour
        {
            val = F.DivideRemainder(seconds, 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeSecondSingle : T.TimeSecondPlural).Translate(language)}")}";
        }
        if (seconds < 86400) // < 1 day 
        {
            val = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out overflow);
            return $"{val} {(val == 1 ? T.TimeHourSingle : T.TimeHourPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeMinuteSingle : T.TimeMinutePlural).Translate(language)}")}";
        }
        if (seconds < 2565000) // < 1 month (29.6875 days) (365.25/12)
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out overflow);
            return $"{val} {(val == 1 ? T.TimeDaySingle : T.TimeDayPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeHourSingle : T.TimeHourPlural)    .Translate(language)}")}";
        }
        if (seconds < 31536000) // < 1 year
        {
            val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out overflow);
            return $"{val} {(val == 1 ? T.TimeMonthSingle : T.TimeMonthPlural).Translate(language)}" +
                   $"{(overflow == 0 ? string.Empty :       $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeDaySingle : T.TimeDayPlural)      .Translate(language)}")}";
        }
        // > 1 year

        val = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out overflow);
        return $"{val} {(val == 1 ? T.TimeYearSingle : T.TimeYearPlural).Translate(language)}" +
               $"{(overflow == 0 ? string.Empty :           $" {(T.TimeAnd).Translate(language)} {overflow} {(overflow == 1 ? T.TimeMonthSingle : T.TimeMonthPlural)  .Translate(language)}")}";
    }
    public static string GetTimeFromMinutes(this int minutes, ulong player)    => GetTimeFromSeconds(minutes * 60, player);
    public static string GetTimeFromMinutes(this int minutes, IPlayer player)  => GetTimeFromSeconds(minutes * 60, player);
    public static string GetTimeFromMinutes(this int minutes, string language) => GetTimeFromSeconds(minutes * 60, language);
    public static string TranslateSign(string key, string language, UCPlayer ucplayer, bool important = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (key == null) return string.Empty;
            if (!key.StartsWith("sign_")) return key;
            string key2 = key.Substring(5);

            if (key2.StartsWith("loadout_"))
            {
                return TranslateLoadoutSign(key2, language, ucplayer);
            }
            else if (KitManager.KitExists(key2, out Kit kit))
            {
                return TranslateKitSign(language, kit, ucplayer);
            }
            else
            {
                Translation? tr = Translation.FromSignId(key2);
                if (tr != null) return tr.Translate(language);

                return Translate(key, language);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error translating sign: ");
            L.LogError(ex);
            return ex.GetType().Name;
        }
    }
    public static string TranslateLoadoutSign(string key, string language, UCPlayer ucplayer)
    {
        if (ucplayer != null && key.Length > 8 && ushort.TryParse(key.Substring(8), System.Globalization.NumberStyles.Any, Data.Locale, out ushort loadoutid))
        {
            ulong team = ucplayer.GetTeam();
            List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && KitManager.HasAccessFast(k, ucplayer));
            loadouts.Sort((k1, k2) => k1.Name.CompareTo(k2.Name));
            if (loadouts.Count > 0)
            {
                if (loadoutid > 0 && loadoutid <= loadouts.Count)
                {
                    Kit kit = loadouts[loadoutid - 1];

                    string name;
                    bool keepline = false;
                    if (!ucplayer.OnDuty())
                    {
                        if (!kit.SignTexts.TryGetValue(language, out name))
                            if (!kit.SignTexts.TryGetValue(L.DEFAULT, out name))
                                if (kit.SignTexts.Count > 0)
                                    name = kit.SignTexts.First().Value;
                                else
                                    name = kit.DisplayName ?? kit.Name;
                        foreach (char @char in name)
                        {
                            if (@char == '\n')
                            {
                                keepline = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        name = kit.Name;
                        if (name.Length > 18 && ulong.TryParse(name.Substring(0, 17), System.Globalization.NumberStyles.Any, Data.Locale, out ulong id) && OffenseManager.IsValidSteam64ID(id) && id == ucplayer.Steam64)
                        {
                            name = "PL #" + (name.Substring(17)[1] - 48).ToString(Data.Locale);
                        }
                    }
                    string cost = Translate("loadout_name_owned", language, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
                    if (!keepline) cost = "\n" + cost;

                    string playercount = string.Empty;

                    if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                    {
                        playercount = Translate("kit_unlimited", language).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
                    }
                    else if (kit.IsClassLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : team, true))
                    {
                        playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
                    }
                    else
                    {
                        playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
                    }
                    return Translate("sign_kit_request", language,
                        name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")),
                        cost,
                        string.IsNullOrEmpty(kit.Weapons) ? " " : Translate("kit_weapons", language, kit.Weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list"))),
                        playercount
                        );
                }
            }
            return Translate("sign_kit_request", language,
                Translate("loadout_name", language, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_public_header")),
                string.Empty,
                ObjectTranslate("kit_price_dollars", language, UCWarfare.Config.LoadoutCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars")),
                string.Empty
            );
        }
        return key;
    }
    public static string TranslateKitSign(string language, Kit kit, UCPlayer ucplayer)
    {
        ulong playerteam = 0;
        ref Ranks.RankData playerrank = ref Ranks.RankManager.GetRank(ucplayer, out bool success);

        bool keepline = false;
        string name;
        if (!ucplayer.OnDuty() && kit.SignTexts != null)
        {
            if (!kit.SignTexts.TryGetValue(language, out name))
                if (!kit.SignTexts.TryGetValue(L.DEFAULT, out name))
                    if (kit.SignTexts.Count > 0)
                        name = kit.SignTexts.First().Value;
                    else
                        name = kit.DisplayName ?? kit.Name;

            foreach (char @char in name)
            {
                if (@char == '\n')
                {
                    keepline = true;
                    break;
                }
            }
        }
        else
        {
            name = kit.Name;
        }
        name = Translate("kit_name", language, name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")));
        string weapons = kit.Weapons ?? string.Empty;
        if (weapons != string.Empty)
            weapons = Translate("kit_weapons", language, weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list")));
        string cost = string.Empty;
        string playercount;
        if (kit.IsPremium && (kit.PremiumCost > 0 || kit.PremiumCost == -1))
        {
            if (ucplayer != null)
                if (KitManager.HasAccessFast(kit, ucplayer))
                    cost = ObjectTranslate("kit_premium_owned", language).Colorize(UCWarfare.GetColorHex("kit_level_dollars_owned"));
                else if (kit.PremiumCost == -1)
                    cost = Translate("kit_premium_exclusive", language).Colorize(UCWarfare.GetColorHex("kit_level_dollars_exclusive"));
                else
                    cost = ObjectTranslate("kit_price_dollars", language, kit.PremiumCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
        }
        else if (kit.UnlockRequirements != null && kit.UnlockRequirements.Length != 0)
        {
            for (int i = 0; i < kit.UnlockRequirements.Length; i++)
            {
                BaseUnlockRequirement req = kit.UnlockRequirements[i];
                if (req.CanAccess(ucplayer)) continue;
                cost = req.GetSignText(ucplayer);
                break;
            }
        }
        if (cost == string.Empty && kit.CreditCost > 0)
        {
            if (ucplayer != null)
                if (!KitManager.HasAccessFast(kit, ucplayer))
                    cost = ObjectTranslate("kit_cost", language, kit.CreditCost);
        }

        if (!keepline) cost = "\n" + cost;
        if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
        {
            playercount = Translate("kit_unlimited", language).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
        }
        else if (kit.IsLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : playerteam, true))
        {
            playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
        }
        else
        {
            playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
        }
        return Translate("sign_kit_request", language, name, cost, weapons, playercount);
    }
    public static string TranslateSign(string key, UCPlayer player, bool important = true)
    {
        if (player == null) return string.Empty;
        if (!Data.Languages.TryGetValue(player.Steam64, out string lang))
            lang = L.DEFAULT;
        return TranslateSign(key, lang, player, important);
    }

    private static readonly Guid F15 = new Guid("423d31c55cf84396914be9175ea70d0c");
    public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, string language)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleBayComponent comp;
        if (spawn.type == Structures.EStructType.STRUCTURE)
            if (spawn.StructureDrop != null)
                comp = spawn.StructureDrop.model.gameObject.GetComponent<VehicleBayComponent>();
            else
                return spawn.VehicleID.ToString("N");
        else if (spawn.BarricadeDrop != null)
            comp = spawn.BarricadeDrop.model.gameObject.GetComponent<VehicleBayComponent>();
        else return spawn.VehicleID.ToString("N");
        if (comp == null) return spawn.VehicleID.ToString("N");

        string unlock = string.Empty;
        if (data.UnlockLevel > 0)
            unlock += RankData.GetRankAbbreviation(data.UnlockLevel).Colorize("f0b589");
        if (data.CreditCost > 0)
        {
            if (unlock != string.Empty)
                unlock += "    ";

            unlock += $"<color=#b8ffc1>C</color> {data.CreditCost.ToString(Data.Locale)}";
        }

        string finalformat =
            $"{(spawn.VehicleID == F15 ? "F15-E" : (Assets.find(spawn.VehicleID) is VehicleAsset asset ? asset.vehicleName : spawn.VehicleID.ToString("N")))}\n" +
            $"<color=#{UCWarfare.GetColorHex("vbs_branch")}>{TranslateEnum(data.Branch, language)}</color>\n" +
            (data.TicketCost > 0 ? $"<color=#{UCWarfare.GetColorHex("vbs_ticket_number")}>{data.TicketCost.ToString(Data.Locale)}</color><color=#{UCWarfare.GetColorHex("vbs_ticket_label")}> {Translate("vbs_tickets_postfix", language)}</color>\n" : "\n") +
            (unlock) +
            $"{{0}}\n";

        finalformat = finalformat.Colorize("ffffff");

        if (comp.State == EVehicleBayState.DEAD) // vehicle is dead
        {
            float rem = data.RespawnTime - comp.DeadTime;
            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_dead")}>{Translate("vbs_state_dead", language, Mathf.FloorToInt(rem / 60f).ToString(), (Mathf.FloorToInt(rem) % 60).ToString("D2"))}</color>";
        }
        else if (comp.State == EVehicleBayState.IN_USE)
        {
            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_active")}>{Translate("vbs_state_active", language, comp.CurrentLocation)}</color>";
        }
        else if (comp.State == EVehicleBayState.IDLE)
        {
            float rem = data.RespawnTime - comp.IdleTime;
            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_idle")}>{Translate("vbs_state_idle", language, Mathf.FloorToInt(rem / 60f).ToString(), (Mathf.FloorToInt(rem) % 60).ToString("D2"))}</color>";
        }
        else
        {
            if (data.IsDelayed(out Delay delay))
            {
                if (delay.type == EDelayType.OUT_OF_STAGING)
                {
                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_staging", language)}</color>";
                }
                else if (delay.type == EDelayType.TIME)
                {
                    float timeLeft = delay.value - Data.Gamemode.SecondsSinceStart;
                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_time", language, Mathf.FloorToInt(timeLeft / 60f).ToString(), Mathf.FloorToInt(timeLeft % 60).ToString("D2"))}</color>";
                }
                else if (delay.type == EDelayType.FLAG || delay.type == EDelayType.FLAG_PERCENT)
                {
                    if (Data.Is(out Invasion invasion))
                    {
                        int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(invasion.Rotation.Count * (delay.value / 100f));
                        int ct2;
                        if (data.Team == 1)
                        {
                            if (invasion.AttackingTeam == 1)
                                ct2 = ct - invasion.ObjectiveT1Index;
                            else
                                ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                        }
                        else if (data.Team == 2)
                        {
                            if (invasion.AttackingTeam == 2)
                                ct2 = ct - (invasion.Rotation.Count - invasion.ObjectiveT2Index - 1);
                            else
                                ct2 = ct - invasion.ObjectiveT1Index;
                        }
                        else ct2 = ct;
                        int ind = ct - ct2;
                        if (invasion.AttackingTeam == 2) ind = invasion.Rotation.Count - ind - 1;
                        if (ct2 == 1 && invasion.Rotation.Count > 0 && ind < invasion.Rotation.Count)
                        {
                            if (data.Team == invasion.AttackingTeam)
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_1", language, invasion.Rotation[ind].ShortName)}</color>";
                            else if (data.Team == invasion.DefendingTeam)
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_lose_1", language, invasion.Rotation[ind].ShortName)}</color>";
                            else
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_2+", language, ct2.ToString(Data.Locale))}</color>";
                        }
                        else if (data.Team == invasion.DefendingTeam)
                            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_lose_2+", language, ct2.ToString(Data.Locale))}</color>";
                        else
                            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_2+", language, ct2.ToString(Data.Locale))}</color>";
                    }
                    else if (Data.Is(out IFlagTeamObjectiveGamemode flags))
                    {
                        int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(flags.Rotation.Count * (delay.value / 100f));
                        int ct2;
                        if (data.Team == 1)
                            ct2 = ct - flags.ObjectiveT1Index;
                        else if (data.Team == 2)
                            ct2 = ct - (flags.Rotation.Count - flags.ObjectiveT2Index - 1);
                        else ct2 = ct;
                        int ind = ct - ct2;
                        if (data.Team == 2) ind = flags.Rotation.Count - ind - 1;
                        if (ct2 == 1 && flags.Rotation.Count > 0 && ind < flags.Rotation.Count)
                        {
                            if (data.Team == 1 || data.Team == 2)
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_1", language, flags.Rotation[ind].ShortName)}</color>";
                            else
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_2+", language, ct2.ToString(Data.Locale))}</color>";
                        }
                        else
                        {
                            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_2+", language, ct2.ToString(Data.Locale))}</color>";
                        }
                    }
                    else if (Data.Is(out Insurgency ins))
                    {
                        int ct = delay.type == EDelayType.FLAG ? Mathf.RoundToInt(delay.value) : Mathf.FloorToInt(ins.Caches.Count * (delay.value / 100f));
                        int ct2;
                        ct2 = ct - ins.CachesDestroyed;
                        int ind = ct - ct2;
                        if (ct2 == 1 && ins.Caches.Count > 0 && ind < ins.Caches.Count)
                        {
                            if (data.Team == ins.AttackingTeam)
                            {
                                if (ins.Caches[ind].IsDiscovered)
                                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_atk_1", language, ins.Caches[ind].Cache.ClosestLocation)}</color>";
                                else
                                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_atk_undiscovered_1", language)}</color>";
                            }
                            else if (data.Team == ins.DefendingTeam)
                                if (ins.Caches[ind].IsActive)
                                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_def_1", language, ins.Caches[ind].Cache.ClosestLocation)}</color>";
                                else
                                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_def_undiscovered_1", language)}</color>";
                            else
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_flags_2+", language, ct2.ToString(Data.Locale))}</color>";
                        }
                        else
                        {
                            if (data.Team == ins.AttackingTeam)
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_atk_2+", language, ct2.ToString(Data.Locale))}</color>";
                            else
                                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_delay")}>{Translate("vbs_state_delay_caches_def_2+", language, ct2.ToString(Data.Locale))}</color>";
                        }
                    }
                }
            }
            return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_ready")}>{Translate("vbs_state_ready", language)}</color>";
        }
    }
    [Obsolete]
    private static readonly List<LanguageSet> languages = new List<LanguageSet>(Data.Localization == null ? 3 : Data.Localization.Count);
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets()
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<SteamPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(players.Current);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(params ulong[] exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                for (int j = 0; j < exclude.Length; j++)
                    if (pl.Steam64 == exclude[j]) goto next;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
                next: ;
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSetsExclude(ulong exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Steam64 == exclude) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumeratePermissions(EAdminType type)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if ((type & pl.PermissionLevel) < type) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(ulong team)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.GetTeam() != team) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(Squads.Squad squad)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Squad != squad) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = squad.Team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    [Obsolete]
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(Predicate<UCPlayer> selector)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!selector(pl)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static string TranslateEnum<TEnum>(TEnum value, string language)
    {
        if (enumTranslations.TryGetValue(typeof(TEnum), out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (L.DEFAULT.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.DEFAULT, out v)))
                v = t.Values.FirstOrDefault();
            string strRep = value!.ToString();
            if (v == null || !v.TryGetValue(strRep, out string v2))
                return strRep.ToProperCase();
            else return v2;
        }
        else return value!.ToString().ToProperCase();
    }
    public static string TranslateEnum<TEnum>(TEnum value, ulong player)
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnum(value, language);
        else return TranslateEnum(value, L.DEFAULT);
    }
    private const string ENUM_NAME_PLACEHOLDER = "%NAME%";
    public static string TranslateEnumName(Type type, string language)
    {
        if (enumTranslations.TryGetValue(type, out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (L.DEFAULT.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(L.DEFAULT, out v)))
                v = t.Values.FirstOrDefault();
            if (v == null || !v.TryGetValue(ENUM_NAME_PLACEHOLDER, out string v2))
                return ENUM_NAME_PLACEHOLDER.ToProperCase();
            else return v2;
        }
        else
        {
            string name = type.Name;
            if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                name = name.Substring(1);
            return name;
        }
    }
    public static string TranslateEnumName<TEnum>(string language) where TEnum : struct, Enum => TranslateEnumName(typeof(TEnum), language);
    public static string TranslateEnumName<TEnum>(ulong player) where TEnum : struct, Enum
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName<TEnum>(language);
        else return TranslateEnumName<TEnum>(L.DEFAULT);
    }
    public static string TranslateEnumName(Type type, ulong player)
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName(type, language);
        else return TranslateEnumName(type, L.DEFAULT);
    }
    private static readonly Dictionary<Type, Dictionary<string, Dictionary<string, string>>> enumTranslations = new Dictionary<Type, Dictionary<string, Dictionary<string, string>>>();
    private static readonly string ENUM_TRANSLATION_FILE_NAME = "Enums" + Path.DirectorySeparatorChar;
    public static void ReadEnumTranslations(List<KeyValuePair<Type, string?>> extEnumTypes)
    {
        enumTranslations.Clear();
        string def = Path.Combine(Data.Paths.LangStorage, L.DEFAULT) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(def))
            Directory.CreateDirectory(def);
        DirectoryInfo info = new DirectoryInfo(Data.Paths.LangStorage);
        if (!info.Exists) info.Create();
        DirectoryInfo[] langDirs = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < langDirs.Length; ++i)
        {
            if (langDirs[i].Name.Equals(L.DEFAULT, StringComparison.Ordinal))
            {
                string p = Path.Combine(langDirs[i].FullName, ENUM_TRANSLATION_FILE_NAME);
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
            }
        }
        foreach (KeyValuePair<Type, TranslatableAttribute> enumType in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsEnum)
                     .Select(x => new KeyValuePair<Type, TranslatableAttribute>(x, (Attribute.GetCustomAttribute(x, typeof(TranslatableAttribute)) as TranslatableAttribute)!))
                     .Where(t => t.Value != null)
                     .Concat(extEnumTypes
                         .Where(x => x.Key.IsEnum)
                         .Select(x => new KeyValuePair<Type, TranslatableAttribute>(x.Key, new TranslatableAttribute(x.Value)))))
        {
            if (enumTranslations.ContainsKey(enumType.Key)) continue;
            Dictionary<string, Dictionary<string, string>> k = new Dictionary<string, Dictionary<string, string>>();
            enumTranslations.Add(enumType.Key, k);
            string fn = Path.Combine(def, ENUM_TRANSLATION_FILE_NAME, enumType.Key.FullName + ".json");
            FieldInfo[] fields = enumType.Key.GetFields(BindingFlags.Public | BindingFlags.Static);
            string[] values = fields.Select(x => x.GetValue(null).ToString()).ToArray();
            if (!File.Exists(fn))
            {
                Dictionary<string, string> k2 = new Dictionary<string, string>(values.Length + 1);
                using (FileStream stream = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    writer.WritePropertyName(ENUM_NAME_PLACEHOLDER);
                    string name;
                    if (enumType.Value.Default != null)
                    {
                        name = enumType.Value.Default;
                        writer.WriteStringValue(name);
                    }
                    else
                    {
                        name = enumType.Key.Name;
                        if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                            name = name.Substring(1);
                        writer.WriteStringValue(name.ToProperCase());
                    }
                    for (int i = 0; i < values.Length; ++i)
                    {
                        string k0 = values[i];
                        string k1 = fields[i].GetCustomAttribute(typeof(TranslatableAttribute)) is TranslatableAttribute attr && attr.Default != null ? attr.Default : k0.ToProperCase();
                        k2.Add(k0, k1);
                        writer.WritePropertyName(k0);
                        writer.WriteStringValue(k1);
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                }

                k.Add(L.DEFAULT, k2);
            }
            for (int i = 0; i < langDirs.Length; ++i)
            {
                DirectoryInfo dir = langDirs[i];
                if (k.ContainsKey(dir.Name)) continue;
                fn = Path.Combine(dir.FullName, ENUM_TRANSLATION_FILE_NAME, enumType.Key.FullName + ".json");
                if (!File.Exists(fn)) continue;
                Dictionary<string, string> k2 = new Dictionary<string, string>(values.Length + 1);
                k.Add(dir.Name, k2);
                using (FileStream stream = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > int.MaxValue)
                    {
                        L.LogWarning("Enum file \"" + fn + "\" is too big to read.");
                        continue;
                    }
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string? key = reader.GetString();
                            if (reader.Read() && key != null)
                            {
                                string? value = reader.GetString();
                                if (value != null)
                                    k2.Add(key, value);
                            }
                        }
                    }
                }
            }
        }
    }

    internal static string GetLang(ulong player) => Data.Languages.TryGetValue(player, out string lang) ? lang : L.DEFAULT;

    [Obsolete]
    private class LanguageSetEnumerator : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
        }
        public IEnumerator<LanguageSet> GetEnumerator() => ((IEnumerable<LanguageSet>)Sets).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sets.GetEnumerator();
    }
}
/// <summary>Disposing calls <see cref="Reset"/>.</summary>
public struct LanguageSet : IEnumerator<UCPlayer>
{
    public readonly string Language;
    public ulong Team = 0;
    public readonly List<UCPlayer> Players;
    private int index;
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public UCPlayer Next;
    UCPlayer IEnumerator<UCPlayer>.Current => Next;
    object IEnumerator.Current => Next;
    public LanguageSet(UCPlayer player)
    {
        if (!Data.Languages.TryGetValue(player.Steam64, out Language))
            Language = L.DEFAULT;
        Players = new List<UCPlayer>(1) { player };
        index = -1;
        Next = null!;
        Team = player.GetTeam();
    }
    public LanguageSet(string lang)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.DEFAULT ? Provider.clients.Count : 2);
        this.index = -1;
        this.Next = null!;
    }
    public LanguageSet(string lang, UCPlayer first)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == L.DEFAULT ? Provider.clients.Count : 2) { first };
        this.index = -1;
        this.Next = null!;
    }
    public void Add(UCPlayer pl) => this.Players.Add(pl);
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public bool MoveNext()
    {
        if (index < this.Players.Count - 1 && index > -2)
        {
            Next = this.Players[++index];
            return true;
        }
        else
            return false;
    }
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public void Reset()
    {
        Next = null!;
        index = -1;
    }
    public void Dispose() => Reset();
    public override string ToString()
    {
        return index.ToString(Data.Locale) + "   " + string.Join(", ", Players.Select(x => x == null ? "null" : x.CharacterName)) + "   Current: " + (Next == null ? "null" : Next.CharacterName);
    }
    private class LanguageSetEnumerator : IEnumerable<LanguageSet>
    {
        public readonly LanguageSet[] Sets;
        public LanguageSetEnumerator(LanguageSet[] sets)
        {
            Sets = sets;
        }
        public IEnumerator<LanguageSet> GetEnumerator() => ((IEnumerable<LanguageSet>)Sets).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sets.GetEnumerator();
    }

    private static readonly List<LanguageSet> languages = new List<LanguageSet>(Data.Localization == null ? 3 : Data.Localization.Count);
    public static IEnumerable<LanguageSet> All()
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InRegions(byte x, byte y, byte regionDistance)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!Regions.checkArea(x, y, pl.Player.movement.region_x, pl.Player.movement.region_y, regionDistance)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<SteamPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(players.Current);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(params ulong[] exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                for (int j = 0; j < exclude.Length; j++)
                    if (pl.Steam64 == exclude[j]) goto next;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
                next:;
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> AllBut(ulong exclude)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Steam64 == exclude) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<Player> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer? pl = UCPlayer.FromPlayer(players.Current);
                if (pl == null) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OfPermission(EAdminType type, PermissionComparison comparison = PermissionComparison.AtLeast)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!pl.PermissionLevel.IsOfPermission(type, comparison)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> All(IEnumerator<UCPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer pl = players.Current;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            players.Dispose();
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> OnTeam(ulong team)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.GetTeam() != team) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> InSquad(Squads.Squad squad)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.Squad != squad) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl) { Team = squad.Team });
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
    public static IEnumerable<LanguageSet> Where(Predicate<UCPlayer> selector)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!selector(pl)) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = L.DEFAULT;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language.Equals(lang, StringComparison.Ordinal))
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            LanguageSetEnumerator rtn = new LanguageSetEnumerator(languages.ToArray());
            languages.Clear();
            return rtn;
        }
    }
}

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class TranslatableAttribute : Attribute
{
    private readonly string? _default;
    public TranslatableAttribute(string? @default = null)
    {
        _default = @default;
    }

    public string? Default => _default;
}
