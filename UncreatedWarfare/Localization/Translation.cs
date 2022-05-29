using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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

public static class Translation
{
    public static string ObjectTranslate(string key, string language, params object[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (language != JSONMethods.DEFAULT_LANGUAGE)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else
        {
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
        }
    }
    public static string ObjectTranslate(string key, ulong player, params object[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (key == null)
        {
            string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
            return args;
        }
        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (player == 0)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out Dictionary<string, TranslationData> data))
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
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
            }
            else lang = JSONMethods.DEFAULT_LANGUAGE;
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
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
    }
    public static string Translate(string key, UCPlayer player, params string[] formatting) =>
        Translate(key, player.Steam64, formatting);
    public static string Translate(string key, UCPlayer player, out Color color, params string[] formatting) =>
        Translate(key, player.Steam64, out color, formatting);
    public static string Translate(string key, SteamPlayer player, params string[] formatting) =>
        Translate(key, player.playerID.steamID.m_SteamID, formatting);
    public static string Translate(string key, SteamPlayer player, out Color color, params string[] formatting) =>
        Translate(key, player.playerID.steamID.m_SteamID, out color, formatting);
    public static string Translate(string key, Player player, params string[] formatting) =>
        Translate(key, player.channel.owner.playerID.steamID.m_SteamID, formatting);
    public static string Translate(string key, Player player, out Color color, params string[] formatting) =>
        Translate(key, player.channel.owner.playerID.steamID.m_SteamID, out color, formatting);
    public static string Translate(string key, UnturnedPlayer player, params string[] formatting) =>
        Translate(key, player.Player.channel.owner.playerID.steamID.m_SteamID, formatting);
    public static string Translate(string key, UnturnedPlayer player, out Color color, params string[] formatting) =>
        Translate(key, player.Player.channel.owner.playerID.steamID.m_SteamID, out color, formatting);
    /// <summary>
    /// Tramslate an unlocalized string to a localized translation structure using the translations file.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DEFAULT_LANGUAGE"/>.</param>
    /// <returns>A translation structure.</returns>
    public static TranslationData GetTranslation(string key, ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (key == null)
        {
            L.LogError($"Message to be sent to {player} was null.");
            return TranslationData.Nil;
        }
        if (key.Length == 0)
        {
            return TranslationData.Nil;
        }
        if (player == 0)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        return translation;
                    }
                    else
                    {
                        return TranslationData.Nil;
                    }
                }
                else
                {
                    return TranslationData.Nil;
                }
            }
            else
            {
                if (data.TryGetValue(key, out TranslationData translation))
                {
                    return translation;
                }
                else
                {
                    return TranslationData.Nil;
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
            }
            else lang = JSONMethods.DEFAULT_LANGUAGE;
            if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
            {
                if (Data.Localization.Count > 0)
                {
                    if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                    {
                        return translation;
                    }
                    else
                    {
                        return TranslationData.Nil;
                    }
                }
                else
                {
                    return TranslationData.Nil;
                }
            }
            else if (data.TryGetValue(key, out TranslationData translation))
            {
                return translation;
            }
            else
            {
                return TranslationData.Nil;
            }
        }
    }
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the Original message (non-color removed)
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DEFAULT_LANGUAGE">Default Language</see>.</param>
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
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
            return args;
        }
        if (key.Length == 0)
        {
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (player == 0)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out Dictionary<string, TranslationData> data))
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
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
            }
            else lang = JSONMethods.DEFAULT_LANGUAGE;
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
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
    }
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DEFAULT_LANGUAGE">Default Language</see>.</param>
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
            L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
            color = UCWarfare.GetColor("default");
            return args;
        }
        if (key.Length == 0)
        {
            color = UCWarfare.GetColor("default");
            return formatting.Length > 0 ? string.Join(", ", formatting) : "";
        }
        if (player == 0)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out Dictionary<string, TranslationData> data))
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
                            return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                        return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }
        else
        {
            if (Data.Languages.TryGetValue(player, out string lang))
            {
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
            }
            else lang = JSONMethods.DEFAULT_LANGUAGE;
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
                            return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                color = UCWarfare.GetColor("default");
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
    }

    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="language">The first language to translate with, pass null to use <see cref="JSONMethods.DEFAULT_LANGUAGE">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <param name="color">Color of the message.</param>
    /// <returns>A localized string based on <paramref name="language"/>.</returns>
    public static string Translate(string key, string language, out Color color, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (language != JSONMethods.DEFAULT_LANGUAGE)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                color = UCWarfare.GetColor("default");
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else
        {
            color = UCWarfare.GetColor("default");
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
        }
    }
    /// <summary>
    /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the message with color still in it.
    /// </summary>
    /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
    /// <param name="language">The first language to translate with, pass null to use <see cref="JSONMethods.DEFAULT_LANGUAGE">Default Language</see>.</param>
    /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
    /// <returns>A localized string based on <paramref name="language"/>.</returns>
    public static string Translate(string key, string language, params string[] formatting)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else if (language != JSONMethods.DEFAULT_LANGUAGE)
        {
            if (!Data.Localization.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out data))
            {
                if (Data.Localization.Count > 0)
                {
                    data = Data.Localization.First().Value;
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        else
        {
            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
        }
    }

    public static string GetTimeFromSeconds(this uint seconds, ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (seconds < 60) // < 1 minute
        {
            return (seconds + 1).ToString(Data.Locale) + ' ' + Translate("time_second" + seconds.S(), player);
        }
        else if (seconds < 3600) // < 1 hour
        {
            int minutes = F.DivideRemainder(seconds, 60, out int secondOverflow);
            return $"{minutes} {Translate("time_minute" + minutes.S(), player)}{(secondOverflow == 0 ? "" : $" {Translate("time_and", player)} {secondOverflow} {Translate("time_second" + secondOverflow.S(), player)}")}";
        }
        else if (seconds < 86400) // < 1 day 
        {
            int hours = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
            return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
        }
        else if (seconds < 2628000) // < 1 month (30.416 days) (365/12)
        {
            int days = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out int hoursOverflow);
            return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
        }
        else if (seconds < 31536000) // < 1 year
        {
            int months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out int daysOverflow);
            return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
        }
        else // > 1 year
        {
            int years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out int monthOverflow);
            return $"{years} {Translate("time_year" + years.S(), player)}{years.S()}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
        }
    }
    public static string GetTimeFromSeconds(this int seconds, string language)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (seconds < 60) // < 1 minute
        {
            return (seconds + 1).ToString(Data.Locale) + ' ' + Translate("time_second" + seconds.S(), language);
        }
        else if (seconds < 3600) // < 1 hour
        {
            int minutes = F.DivideRemainder(seconds, 60, out int secondOverflow);
            return $"{minutes} {Translate("time_minute" + minutes.S(), language)}{(secondOverflow == 0 ? "" : $" {Translate("time_and", language)} {secondOverflow} {Translate("time_second" + secondOverflow.S(), language)}")}";
        }
        else if (seconds < 86400) // < 1 day 
        {
            int hours = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
            return $"{hours} {Translate("time_hour" + hours.S(), language)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", language)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), language)}")}";
        }
        else if (seconds < 2628000) // < 1 month (30.416 days) (365/12)
        {
            int days = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out int hoursOverflow);
            return $"{days} {Translate("time_day" + days.S(), language)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", language)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), language)}")}";
        }
        else if (seconds < 31536000) // < 1 year
        {
            int months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out int daysOverflow);
            return $"{months} {Translate("time_month" + months.S(), language)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", language)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), language)}")}";
        }
        else // > 1 year
        {
            int years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out int monthOverflow);
            return $"{years} {Translate("time_year" + years.S(), language)}{years.S()}{(monthOverflow == 0 ? "" : $" {Translate("time_and", language)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), language)}")}";
        }
    }
    public static string GetTimeFromMinutes(this int minutes, ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (minutes < 60) // < 1 hour
        {
            return minutes.ToString(Data.Locale) + ' ' + Translate("time_minute" + minutes.S(), player);
        }
        else if (minutes < 1440) // < 1 day 
        {
            int hours = F.DivideRemainder(minutes, 60, out int minutesOverflow);
            return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
        }
        else if (minutes < 43800) // < 1 month (30.416 days)
        {
            int days = F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out int hoursOverflow);
            return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
        }
        else if (minutes < 525600) // < 1 year
        {
            int months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out int daysOverflow);
            return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
        }
        else // > 1 year
        {
            int years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out int monthOverflow);
            return $"{years} {Translate("time_year" + years.S(), player)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
        }
    }
    public static string GetTimeFromMinutes(this int minutes, string language)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (minutes < 60) // < 1 hour
        {
            return minutes.ToString(Data.Locale) + ' ' + Translate("time_minute" + minutes.S(), language);
        }
        else if (minutes < 1440) // < 1 day 
        {
            int hours = F.DivideRemainder(minutes, 60, out int minutesOverflow);
            return $"{hours} {Translate("time_hour" + hours.S(), language)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", language)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), language)}")}";
        }
        else if (minutes < 43800) // < 1 month (30.416 days)
        {
            int days = F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out int hoursOverflow);
            return $"{days} {Translate("time_day" + days.S(), language)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", language)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), language)}")}";
        }
        else if (minutes < 525600) // < 1 year
        {
            int months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out int daysOverflow);
            return $"{months} {Translate("time_month" + months.S(), language)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", language)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), language)}")}";
        }
        else // > 1 year
        {
            int years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out int monthOverflow);
            return $"{years} {Translate("time_year" + years.S(), language)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", language)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), language)}")}";
        }
    }
    public static string TranslateSign(string key, string language, UCPlayer ucplayer, bool important = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (key == null) return string.Empty;
            if (!key.StartsWith("sign_")) return Translate(key, language);
            string key2 = key.Substring(5);
            if (key2.StartsWith("loadout_"))
            {
                return TranslateLoadoutSign(key2, language, ucplayer);
            }
            else if (KitManager.KitExists(key2, out Kit kit))
            {
                return TranslateKitSign(language, kit, ucplayer);
            }
            else return Translate(key, language);
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
            List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && KitManager.HasAccessFast(k, ucplayer)).ToList();
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
                            if (!kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out name))
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
                if (!kit.SignTexts.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out name))
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
            lang = JSONMethods.DEFAULT_LANGUAGE;
        return TranslateSign(key, lang, player, important);
    }
    public static string DecideLanguage<TVal>(ulong player, Dictionary<string, TVal> searcher)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == 0)
        {
            if (!searcher.ContainsKey(JSONMethods.DEFAULT_LANGUAGE))
            {
                if (searcher.Count > 0)
                {
                    return searcher.ElementAt(0).Key;
                }
                else return JSONMethods.DEFAULT_LANGUAGE;
            }
            else return JSONMethods.DEFAULT_LANGUAGE;
        }
        else
        {
            if (!Data.Languages.TryGetValue(player, out string lang) || !searcher.ContainsKey(lang))
            {
                if (searcher.Count > 0)
                {
                    return searcher.ElementAt(0).Key;
                }
                else return JSONMethods.DEFAULT_LANGUAGE;
            }
            return lang;
        }
    }
    /// <param name="backupcause">Used in case the key can not be found.</param>
    public static string TranslateDeath(string language, string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, ELimb limb, string itemName, float distance, bool usePlayerName = false, bool translateKillerName = false, bool colorize = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
        if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
        string murderername = translateKillerName ? Translate(killerName.PlayerName, language) : (usePlayerName ? killerName.PlayerName : killerName.CharacterName);
        if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
        string dis = Mathf.RoundToInt(distance).ToString(Data.Locale) + 'm';

        return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Mathf.RoundToInt(distance).ToString(Data.Locale) + "m"}";
    }
    public static string TranslateLandmineDeath(string language, string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, bool usePlayerName = false, bool colorize = true)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
        if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
        string murderername = usePlayerName ? killerName.PlayerName : killerName.CharacterName;
        if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
        string triggerername = usePlayerName ? triggererName.PlayerName : triggererName.CharacterName;
        if (colorize) triggerername = F.ColorizeName(triggerername, triggererTeam);

        return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
    }
    public static string TranslateBranch(EBranch branch, UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string branchName = "team";
        ulong team = player.GetTeam();
        if (team == 1)
            branchName += "1_";
        else if (team == 2)
            branchName += "2_";
        else
            branchName += "1_";
        return Translate(branchName + branch.ToString().ToLower(), player.Steam64, out _);
    }
    public static string TranslateBranch(EBranch branch, ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string branchName = "team";
        ulong team = player.GetTeamFromPlayerSteam64ID();
        if (team == 1)
            branchName += "1_";
        else if (team == 2)
            branchName += "2_";
        else
            branchName += "1_";
        return Translate(branchName + branch.ToString().ToLower(), player, out _);
    }
    public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, ulong player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (player == 0)
        {
            return TranslateVBS(spawn, data, JSONMethods.DEFAULT_LANGUAGE);
        }
        else
        {
            if (!Data.Languages.TryGetValue(player, out string lang))
                lang = JSONMethods.DEFAULT_LANGUAGE;
            return TranslateVBS(spawn, data, lang);
        }
    }
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

        string unlock = "";
        if (data.UnlockLevel > 0)
            unlock += RankData.GetRankAbbreviation(data.UnlockLevel).Colorize("f0b589");
        if (data.CreditCost > 0)
        {
            if (unlock != "")
                unlock += "    ";

            unlock += $"<color=#b8ffc1>C</color> {data.CreditCost.ToString(Data.Locale)}";
        }

        string finalformat =
            $"{(Assets.find(spawn.VehicleID) is VehicleAsset asset ? asset.vehicleName : spawn.VehicleID.ToString("N"))}\n" +
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
    private static readonly List<LanguageSet> languages = new List<LanguageSet>(Data.Localization == null ? 3 : Data.Localization.Count);
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(byte x, byte y, byte regionDistance)
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
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
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
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
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<Player> players)
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
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
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
    public static IEnumerable<LanguageSet> EnumeratePermissions(EAdminType type = EAdminType.MODERATE_PERMS)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if ((type & pl.GetPermissions()) != type) continue;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
    public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<UCPlayer> players)
    {
        lock (languages)
        {
            if (languages.Count > 0)
                languages.Clear();
            while (players.MoveNext())
            {
                UCPlayer pl = players.Current;
                if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
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
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
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
                    lang = JSONMethods.DEFAULT_LANGUAGE;
                bool found = false;
                for (int i2 = 0; i2 < languages.Count; i2++)
                {
                    if (languages[i2].Language == lang)
                    {
                        languages[i2].Add(pl);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    languages.Add(new LanguageSet(lang, pl));
            }
            for (int i = 0; i < languages.Count; i++)
            {
                yield return languages[i];
            }
            languages.Clear();
        }
    }
    public static string TranslateEnum<TEnum>(TEnum value, string language) where TEnum : struct, Enum
    {
        if (enumTranslations.TryGetValue(typeof(TEnum), out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (JSONMethods.DEFAULT_LANGUAGE.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out v)))
                v = t.Values.FirstOrDefault();
            string strRep = value.ToString();
            if (v == null || !v.TryGetValue(strRep, out string v2))
                return strRep.ToProperCase();
            else return v2;
        }
        else return value.ToString().ToProperCase();
    }
    public static string TranslateEnum<TEnum>(TEnum value, ulong player) where TEnum : struct, Enum
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnum(value, language);
        else return TranslateEnum(value, JSONMethods.DEFAULT_LANGUAGE);
    }
    private const string ENUM_NAME_PLACEHOLDER = "%NAME%";
    public static string TranslateEnumName<TEnum>(string language) where TEnum : struct, Enum
    {
        Type t2 = typeof(TEnum);
        if (enumTranslations.TryGetValue(t2, out Dictionary<string, Dictionary<string, string>> t))
        {
            if (!t.TryGetValue(language, out Dictionary<string, string> v) &&
                (JSONMethods.DEFAULT_LANGUAGE.Equals(language, StringComparison.Ordinal) ||
                 !t.TryGetValue(JSONMethods.DEFAULT_LANGUAGE, out v)))
                v = t.Values.FirstOrDefault();
            if (v == null || !v.TryGetValue(ENUM_NAME_PLACEHOLDER, out string v2))
                return ENUM_NAME_PLACEHOLDER.ToProperCase();
            else return v2;
        }
        else
        {
            string name = t2.Name;
            if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                name = name.Substring(1);
            return name.ToProperCase();
        }
    }
    public static string TranslateEnumName<TEnum>(ulong player) where TEnum : struct, Enum
    {
        if (player != 0 && Data.Languages.TryGetValue(player, out string language))
            return TranslateEnumName<TEnum>(language);
        else return TranslateEnumName<TEnum>(JSONMethods.DEFAULT_LANGUAGE);
    }
    private static readonly Dictionary<Type, Dictionary<string, Dictionary<string, string>>> enumTranslations = new Dictionary<Type, Dictionary<string, Dictionary<string, string>>>();
    private const string ENUM_TRANSLATION_FILE_NAME = "Enums\\";
    public static void ReadEnumTranslations(List<Type> extEnumTypes)
    {
        enumTranslations.Clear();
        string def = Data.LangStorage + JSONMethods.DEFAULT_LANGUAGE + "\\";
        if (!Directory.Exists(def))
            Directory.CreateDirectory(def);
        DirectoryInfo info = new DirectoryInfo(Data.LangStorage);
        if (!info.Exists) info.Create();
        DirectoryInfo[] langDirs = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < langDirs.Length; ++i)
        {
            if (langDirs[i].Name.Equals(JSONMethods.DEFAULT_LANGUAGE, StringComparison.Ordinal))
            {
                string p = langDirs[i].FullName + "\\" + ENUM_TRANSLATION_FILE_NAME;
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
            }
        }
        foreach (Type enumType in UCWarfare.Instance.Assembly.GetTypes().Where(t => t.IsEnum && Attribute.GetCustomAttribute(t, typeof(TranslatableAttribute)) is not null).Concat(extEnumTypes.Where(x => x.IsEnum)))
        {
            if (enumTranslations.ContainsKey(enumType)) continue;
            Dictionary<string, Dictionary<string, string>> k = new Dictionary<string, Dictionary<string, string>>();
            enumTranslations.Add(enumType, k);
            string fn = def + ENUM_TRANSLATION_FILE_NAME + enumType.FullName + ".json";
            string[] values = enumType.GetEnumNames();
            if (!File.Exists(fn))
            {
                Dictionary<string, string> k2 = new Dictionary<string, string>(values.Length + 1);
                using (FileStream stream = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    writer.WritePropertyName(ENUM_NAME_PLACEHOLDER);
                    string name = enumType.Name;
                    if (name.Length > 1 && name[0] == 'E' && char.IsUpper(name[1]))
                        name = name.Substring(1);
                    writer.WriteStringValue(name.ToProperCase());
                    for (int i = 0; i < values.Length; ++i)
                    {
                        string k0 = values[i];
                        string k1 = k0.ToProperCase();
                        k2.Add(k0, k1);
                        writer.WritePropertyName(k0);
                        writer.WriteStringValue(k1);
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                }

                k.Add(JSONMethods.DEFAULT_LANGUAGE, k2);
            }
            for (int i = 0; i < langDirs.Length; ++i)
            {
                DirectoryInfo dir = langDirs[i];
                if (k.ContainsKey(dir.Name)) continue;
                fn = dir.FullName + "\\" +  ENUM_TRANSLATION_FILE_NAME + enumType.FullName + ".json";
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
}
/// <summary>Disposing calls <see cref="Reset"/>.</summary>
public struct LanguageSet : IEnumerator<UCPlayer>
{
    public string Language;
    public List<UCPlayer> Players;
    private int nextIndex;
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public UCPlayer Next;

    UCPlayer IEnumerator<UCPlayer>.Current => Next;

    object IEnumerator.Current => Next;
    public LanguageSet(UCPlayer player)
    {
        if (!Data.Languages.TryGetValue(player.Steam64, out Language))
            Language = JSONMethods.DEFAULT_LANGUAGE;
        Players = new List<UCPlayer>(1) { player };
        nextIndex = 0;
        Next = null!;
    }
    public LanguageSet(string lang)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == JSONMethods.DEFAULT_LANGUAGE ? Provider.clients.Count : 4);
        this.nextIndex = 0;
        this.Next = null!;
    }
    public LanguageSet(string lang, UCPlayer first)
    {
        this.Language = lang;
        this.Players = new List<UCPlayer>(lang == JSONMethods.DEFAULT_LANGUAGE ? Provider.clients.Count : 4) { first };
        this.nextIndex = 0;
        this.Next = null!;
    }
    public void Add(UCPlayer pl) => this.Players.Add(pl);
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public bool MoveNext()
    {
        if (nextIndex < this.Players.Count)
        {
            Next = this.Players[nextIndex];
            nextIndex++;
            return true;
        }
        else
            return false;
    }
    /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
    public void Reset()
    {
        Next = null!;
        nextIndex = 0;
    }
    public void Dispose() => Reset();
}

[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class TranslatableAttribute : Attribute
{
    public TranslatableAttribute() { }
}
