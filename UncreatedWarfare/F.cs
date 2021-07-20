﻿using Newtonsoft.Json;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;
using System.Reflection;
using Uncreated.Players;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.XP;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Flags;

namespace Uncreated.Warfare
{
    public static class F
    {
        public const float SPAWN_HEIGHT_ABOVE_GROUND = 0.5f;
        public const char INFINITY_SYMBOL = '∞';
        public static readonly List<char> vowels = new List<char> { 'a', 'e', 'i', 'o', 'u' };
        /// <summary>
        /// Convert an HTMLColor string to a actual color.
        /// </summary>
        /// <param name="htmlColorCode">A hexadecimal/HTML color key.</param>
        public static Color Hex(this string htmlColorCode)
        {
            string code = "#";
            if (htmlColorCode.Length > 0 && htmlColorCode[0] != '#')
                code += htmlColorCode;
            else
                code = htmlColorCode;
            if (ColorUtility.TryParseHtmlString(code, out Color color))
                return color;
            else if (ColorUtility.TryParseHtmlString(htmlColorCode, out color))
                return color;
            else return Color.white;
        }
        public static string MakeRemainder(this string[] array, int startIndex = 0, int length = -1, string deliminator = " ")
        {
            string temp = string.Empty;
            for (int i = startIndex; i < (length == -1 ? array.Length : length); i++)
                temp += (i == startIndex ? "" : deliminator) + array[i];
            return temp;
        }
        public static string GetTimeFromSeconds(this uint seconds)
        {
            if (seconds < 60) // < 1 minute
            {
                return seconds.ToString(Data.Locale) + " second" + seconds.S();
            }
            else if (seconds < 3600) // < 1 hour
            {
                int minutes = DivideRemainder(seconds, 60, out int secondOverflow);
                return $"{minutes} minute{minutes.S()}{(secondOverflow == 0 ? "" : $" and {secondOverflow} second{secondOverflow.S()}")}";
            }
            else if (seconds < 86400) // < 1 day 
            {
                int hours = DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
                return $"{hours} hour{hours.S()}{(minutesOverflow == 0 ? "" : $" and {minutesOverflow} minute{minutesOverflow.S()}")}";
            }
            else if (seconds < 2628000) // < 1 month (30.4166667 days) (365/12)
            {
                uint days = DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out uint hoursOverflow);
                return $"{days} day{days.S()}{(hoursOverflow == 0 ? "" : $" and {hoursOverflow} hour{hoursOverflow.S()}")}";
            }
            else if (seconds < 31536000) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.4166667m, out uint daysOverflow);
                return $"{months} month{months.S()}{(daysOverflow == 0 ? "" : $" and {daysOverflow} day{daysOverflow.S()}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.4166667m, out _), 12, out uint monthOverflow);
                return $"{years} year{years.S()}{(monthOverflow == 0 ? "" : $" and {monthOverflow} month{monthOverflow.S()}")}";
            }
        }
        public static string GetTimeFromMinutes(this uint minutes)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString(Data.Locale) + " minute" + (minutes == 1 ? "" : "s");
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} hour{hours.S()}{(minutesOverflow == 0 ? "" : $" and {minutesOverflow} minute{minutesOverflow.S()}")}";
            }
            else if (minutes < 43800) // < 1 month (30.4166667 days)
            {
                uint days = DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} day{days.S()}{(hoursOverflow == 0 ? "" : $" and {hoursOverflow} hour{hoursOverflow.S()}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.4166667m, out uint daysOverflow);
                return $"{months} month{months.S()}{(daysOverflow == 0 ? "" : $" and {daysOverflow} day{daysOverflow.S()}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.4166667m, out _), 12, out uint monthOverflow);
                return $"{years} year{years.S()}{(monthOverflow == 0 ? "" : $" and {monthOverflow} month{monthOverflow.S()}")}";
            }
        }
        public static int DivideRemainder(float divisor, float dividend, out int remainder)
        {
            float answer = divisor / dividend;
            remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * dividend);
            return (int)Mathf.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, uint dividend, out uint remainder)
        {
            decimal answer = (decimal)divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, decimal dividend, out uint remainder)
        {
            decimal answer = divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static string ObjectTranslate(string key, ulong player, params object[] formatting)
        {
            if (key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
                return args;
            }
            if (key.Length == 0)
            {
                return formatting.Length > 0 ? string.Join(", ", formatting) : "";
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
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
                                F.LogError(ex);
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
                            F.LogError(ex);
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
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
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
                                F.LogError(ex);
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
                        F.LogError(ex);
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
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage"/>.</param>
        /// <returns>A translation structure.</returns>
        public static TranslationData GetTranslation(string key, ulong player)
        {
            if (key == null)
            {
                LogError($"Message to be sent to {player} was null.");
                return TranslationData.Nil;
            }
            if (key.Length == 0)
            {
                return TranslationData.Nil;
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
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
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
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
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, params string[] formatting)
        {
            if (key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
                return args;
            }
            if (key.Length == 0)
            {
                return formatting.Length > 0 ? string.Join(", ", formatting) : "";
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
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
                                F.LogError(ex);
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
                            F.LogError(ex);
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
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
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
                                F.LogError(ex);
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
                        F.LogError(ex);
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
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, out Color color, params string[] formatting)
        {
            if(key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
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
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
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
                                F.LogError(ex);
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
                            F.LogError(ex);
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
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
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
                                F.LogError(ex);
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
                        F.LogError(ex);
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
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, Color textColor, params string[] formatting) => 
            SendChat(player.CSteamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, params string[] formatting) => 
            SendChat(player.CSteamID, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, Color textColor, params string[] formatting) => 
            SendChat(player.Player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, params string[] formatting) => 
            SendChat(player.Player.channel.owner.playerID.steamID, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, Color textColor, params string[] formatting) => 
            SendChat(player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, params string[] formatting) => 
            SendChat(player.channel.owner.playerID.steamID, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, Color textColor, params string[] formatting) => 
            SendChat(player.player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, params string[] formatting) => 
            SendChat(player.player.channel.owner.playerID.steamID, text, formatting);
        /// <summary>
        /// Max amount of bytes that can be sent in an Unturned Chat Message.
        /// </summary>
        const int MaxChatSizeAmount = 2047;
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, Color textColor, params string[] formatting)
        {
            string localizedString = Translate(text, player.m_SteamID, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                UCWarfare.I.QueueMainThreadAction(() => ChatManager.say(player, localizedString, textColor, localizedString.Contains("</")));
            else
            {
                LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
                string defaultMessage = text;
                string newMessage;
                if (JSONMethods.DefaultTranslations.ContainsKey(text))
                    defaultMessage = JSONMethods.DefaultTranslations[text];
                try
                {
                    newMessage = string.Format(defaultMessage, formatting);
                }
                catch (FormatException)
                {
                    newMessage = defaultMessage + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
                if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                    UCWarfare.I.QueueMainThreadAction( () => ChatManager.say(player, newMessage, textColor, newMessage.Contains("</")));
                else
                    LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Send a message in chat using the translation file, automatically extrapolates the color.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, params string[] formatting)
        {
            string localizedString = Translate(text, player.m_SteamID, out Color textColor, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                UCWarfare.I.QueueMainThreadAction(() => ChatManager.say(player, localizedString, textColor, localizedString.Contains("</")));
            else
            {
                LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
                string defaultMessage = text;
                string newMessage;
                if (JSONMethods.DefaultTranslations.ContainsKey(text))
                    defaultMessage = JSONMethods.DefaultTranslations[text];
                try
                {
                    newMessage = string.Format(defaultMessage, formatting);
                }
                catch (FormatException)
                {
                    newMessage = defaultMessage + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
                if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                    UCWarfare.I.QueueMainThreadAction(() => ChatManager.say(player, newMessage, textColor, newMessage.Contains("</")));
                else
                    LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Send a white message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this UnturnedPlayer player, string message, params string[] formatting) => 
            SendChat(player.CSteamID, message, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this Player player, string message, params string[] formatting) => 
            SendChat(player.channel.owner.playerID.steamID, message, formatting);
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, Color textColor, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients)
                SendChat(player.playerID.steamID, text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para>
        /// <para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients)
                SendChat(player.playerID.steamID, text, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, Color textColor, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                SendChat(player.playerID.steamID, text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                SendChat(player.playerID.steamID, text, formatting);
        }
        public static bool OnDuty(this UnturnedPlayer player) => player.Player.channel.owner.isAdmin || R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool OffDuty(this UnturnedPlayer player) => !OnDuty(player);
        public static bool IsIntern(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool IsAdmin(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup);
        /// <summary>Ban someone for <paramref name="duration"/> seconds.</summary>
        /// <param name="duration">Duration of ban IN SECONDS</param>
        public static void OfflineBan(ulong BannedID, uint IPAddress, CSteamID BannerID, string reason, uint duration)
        {
            CSteamID banned = new CSteamID(BannedID);
            Provider.ban(banned, reason, duration);
            for (int index = 0; index < SteamBlacklist.list.Count; ++index)
            {
                if (SteamBlacklist.list[index].playerID.m_SteamID == BannedID)
                {
                    SteamBlacklist.list[index].judgeID = BannerID;
                    SteamBlacklist.list[index].reason = reason;
                    SteamBlacklist.list[index].duration = duration;
                    SteamBlacklist.list[index].banned = Provider.time;
                    return;
                }
            }
            SteamBlacklist.list.Add(new SteamBlacklistID(banned, IPAddress, BannerID, reason, duration, Provider.time));
        }
        public static string An(this string word) => (word.Length > 0 && vowels.Contains(word[0].ToString().ToLower()[0])) ? "n" : "";
        public static string An(this char letter) => vowels.Contains(letter.ToString().ToLower()[0]) ? "n" : "";
        public static string S(this int number) => number == 1 ? "" : "s";
        public static string S(this float number) => number == 1 ? "" : "s";
        public static string S(this uint number) => number == 1 ? "" : "s";
        public enum EFlagStatus
        {
            CAPTURING,
            LOSING,
            SECURED,
            CONTESTED,
            NOT_OBJECTIVE,
            CLEARING,
            BLANK,
            NOT_OWNED,
            DONT_DISPLAY
        }
        public static Color GetTeamColor(this SteamPlayer player) => GetTeamColor(player.player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this Player player) => GetTeamColor(player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this ulong groupID)
        {
            if (groupID == TeamManager.Team1ID) return TeamManager.Team1Color;
            else if (groupID == TeamManager.Team2ID) return TeamManager.Team2Color;
            else if (groupID == TeamManager.AdminID) return TeamManager.AdminColor;
            else return TeamManager.NeutralColor;
        }
        public static string GetTeamColorHex(this SteamPlayer player) => GetTeamColorHex(player.player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this Player player) => GetTeamColorHex(player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this ulong groupID)
        {
            if (groupID == TeamManager.Team1ID) return TeamManager.Team1ColorHex;
            else if (groupID == TeamManager.Team2ID) return TeamManager.Team2ColorHex;
            else if (groupID == TeamManager.AdminID) return TeamManager.AdminColorHex;
            else return TeamManager.NeutralColorHex;
        }
        public static string GetTeamNumberColorHex(this ulong team)
        {
            if (team == 1) return TeamManager.Team1ColorHex;
            else if (team == 2) return TeamManager.Team2ColorHex;
            else if (team == 3) return TeamManager.AdminColorHex;
            else return TeamManager.NeutralColorHex;
        }
        public static Color GetTeamNumberColor(this ulong team)
        {
            if (team == 1) return TeamManager.Team1Color;
            else if (team == 2) return TeamManager.Team2Color;
            else if (team == 3) return TeamManager.AdminColor;
            else return TeamManager.NeutralColor;
        }
        public static ulong GetTeamFromPlayerSteam64ID(this ulong s64)
        {
            SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
            if (pl == default)
            {
                if (PlayerManager.HasSave(s64, out PlayerSave save))
                    return save.Team;
                else return 0;
            }
            else return pl.GetTeam();
        }
        public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this UnturnedPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this ulong groupID)
        {
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this UnturnedPlayer player) => GetTeamByte(player.Player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this ulong groupID)
        {
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, SendUIParameters p, SteamPlayer player, ITransportConnection connection, ulong translationID) =>
            UIOrChat(charactericon, useui, uiid, pts, progresschars, p.team, p.status, p.chatTranslation, p.chatColor, connection, player, p.points, translationID,
                p.sendChat, p.sendUI, p.absoluteCap, p.overrideChatConfig, p.formatting, p.team1count, p.team2count);
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, ulong team, EFlagStatus type, string translation_key, Color color, SteamPlayer player, int circleAmount,
            ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true, bool absolute = true, bool sendChatOverride = false,
            string[] formatting = null, int team1count = 0, int team2count = 0)
            => UIOrChat(charactericon, useui, uiid, pts, progresschars, team, type, translation_key, color, Provider.findTransportConnection(player.playerID.steamID), player, circleAmount, playerID,
                SendChatIfConfiged, SendUIIfConfiged, absolute, sendChatOverride, formatting, team1count, team2count);
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, ulong team, EFlagStatus type, string translation_key, Color color, ITransportConnection PlayerConnection, SteamPlayer player,
            int c, ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true,
            bool absolute = true, bool sendChatOverride = false, string[] formatting = null, int team1count = 0, int team2count = 0)
        {
            if (type == EFlagStatus.DONT_DISPLAY)
            {
                if(useui && SendUIIfConfiged)
                    EffectManager.askEffectClearByID(uiid, PlayerConnection);
                return;
            }
            int circleAmount = absolute ? Math.Abs(c) : c;
            if (useui && SendUIIfConfiged)
            {
                EffectManager.askEffectClearByID(uiid, PlayerConnection);
                short key = unchecked((short)uiid);
                switch (type)
                {
                    case EFlagStatus.CAPTURING:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1_words")}>{Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2_words")}>{Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    default:
                    case EFlagStatus.BLANK:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(0, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(0, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case EFlagStatus.LOSING:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1_words")}>{Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("losing_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2_words")}>{Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("losing_team_2_bkgr"));
                        break;
                    case EFlagStatus.SECURED:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1_words")}>{Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("secured_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2_words")}>{Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("secured_team_2_bkgr"));
                        break;
                    case EFlagStatus.CONTESTED:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1_words")}>{Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("contested_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2_words")}>{Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("contested_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OBJECTIVE:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1_words")}>{Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("nocap_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2_words")}>{Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("nocap_team_2_bkgr"));
                        break;
                    case EFlagStatus.CLEARING:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1_words")}>{Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("clearing_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2_words")}>{Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("clearing_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OWNED:
                        if (team == TeamManager.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_1_words")}>{Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("notowned_team_1_bkgr"));
                        else if (team == TeamManager.Team2ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2_words")}>{Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MaxPoints})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("notowned_team_2_bkgr"));
                        break;
                }
                if (team1count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1")}>{team1count}</color>");
                } else
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", "");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", "");
                }
                if (team2count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2")}>{team2count}</color>");
                } else
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2CountIcon", "");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2Count", "");
                }
            }
            if (sendChatOverride || (UCWarfare.Config.FlagSettings.UseChat && SendChatIfConfiged))
            {
                if (formatting == null)
                    player.SendChat(translation_key, color);
                else
                    player.SendChat(translation_key, color, formatting);
            }
        }
        public static string EncodeURIComponent(this string input) => Uri.EscapeUriString(input);
        public static Vector3 GetBaseSpawn(this SteamPlayer player) => player.player.GetBaseSpawn();
        public static Vector3 GetBaseSpawn(this SteamPlayer player, out ulong team) => player.player.GetBaseSpawn(out team);
        public static Vector3 GetBaseSpawn(this Player player)
        {
            ulong team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this Player player, out ulong team)
        {
            team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this ulong playerID, out ulong team)
        {
            team = playerID.GetTeamFromPlayerSteam64ID();
            return team.GetBaseSpawnFromTeam();
        }
        public static Vector3 GetBaseSpawnFromTeam(this ulong team)
        {
            if (team == 1) return TeamManager.Team1Main.Center3D;
            else if (team == 2) return TeamManager.Team2Main.Center3D;
            else return TeamManager.LobbySpawn;
        }
        public static float GetBaseAngle(this ulong team)
        {
            if (team == 1) return TeamManager.Team1SpawnAngle;
            else if (team == 2) return TeamManager.Team2SpawnAngle;
            else return TeamManager.LobbySpawnAngle;
        }
        public static Vector3 GetBaseSpawn(this ulong playerID) => playerID.GetBaseSpawn(out _);
        public static string QuickSerialize(object obj) => JsonConvert.SerializeObject(obj);
        public static T QuickDeserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
        public static async Task InvokeSignUpdateFor(SteamPlayer client, byte x, byte y, ushort plant, ushort index, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
                newtext = await TranslateSign(text, client.playerID.steamID.m_SteamID, false);
            Data.SendUpdateSign.Invoke(ENetReliability.Reliable, client.transportConnection, x, y, plant, index, newtext);
        }
        public static async Task InvokeSignUpdateForAll(byte x, byte y, ushort plant, ushort index, string text)
        {
            Dictionary<string, List<SteamPlayer>> playergroups = new Dictionary<string, List<SteamPlayer>>();
            IEnumerator<SteamPlayer> connections = EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            while (connections.MoveNext())
            {
                SteamPlayer client = connections.Current;
                if (Data.Languages.ContainsKey(client.playerID.steamID.m_SteamID))
                {
                    if (playergroups.TryGetValue(Data.Languages[client.playerID.steamID.m_SteamID], out List<SteamPlayer> players))
                        players.Add(client);
                    else
                        playergroups.Add(Data.Languages[client.playerID.steamID.m_SteamID], new List<SteamPlayer> { client });
                }
                else
                {
                    if (playergroups.TryGetValue(JSONMethods.DefaultLanguage, out List<SteamPlayer> players))
                        players.Add(client);
                    else
                        playergroups.Add(JSONMethods.DefaultLanguage, new List<SteamPlayer> { client });
                }
            }
            connections.Dispose();
            foreach (KeyValuePair<string, List<SteamPlayer>> languageGroup in playergroups)
            {
                if (languageGroup.Value.Count > 0)
                {
                    string newtext = text;
                    if (text.StartsWith("sign_"))
                        newtext = await TranslateSign(text, languageGroup.Value[0].playerID.steamID.m_SteamID, false);
                    List<ITransportConnection> toSendTo = new List<ITransportConnection>();
                    languageGroup.Value.ForEach(l => toSendTo.Add(l.transportConnection));
                    Data.SendUpdateSign.Invoke(ENetReliability.Reliable, toSendTo, x, y, plant, index, newtext);
                }
            }
        }
        /// <summary>Runs one player at a time instead of one language at a time. Used for kit signs.</summary>
        public static async Task InvokeSignUpdateForAllKits(byte x, byte y, ushort plant, ushort index, string text)
        {
            if (text == null) return;
            IEnumerator<SteamPlayer> connections = EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            while(connections.MoveNext())
            {
                string newtext = text;
                if (text.StartsWith("sign_"))
                    newtext = await TranslateSign(text, connections.Current.playerID.steamID.m_SteamID, false);
                Data.SendUpdateSign.Invoke(ENetReliability.Reliable, connections.Current.transportConnection, x, y, plant, index, newtext);
            }
            connections.Dispose();
        }
        public static IEnumerable<SteamPlayer> EnumerateClients_Remote(byte x, byte y, byte distance)
        {
            foreach (SteamPlayer client in Provider.clients)
            {
                if (client.player != null && Regions.checkArea(x, y, client.player.movement.region_x, client.player.movement.region_y, distance))
                    yield return client;
            }
        }
        public static async Task InvokeSignUpdateFor(SteamPlayer client, byte x, byte y, ushort plant, ushort index, BarricadeRegion region, bool changeText = false, string text = "")
        {
            if (text == default || client == default || region == default) return;
            string newtext;
            if (!changeText)
            {
                newtext = GetSignText(index, region);
            }
            else newtext = text;
            if (newtext.StartsWith("sign_"))
                newtext = await TranslateSign(newtext ?? "", client.playerID.steamID.m_SteamID, false);
            Data.SendUpdateSign.Invoke(ENetReliability.Reliable, client.transportConnection, x, y, plant, index, newtext);
        }
        public static string GetSignText(ushort index, BarricadeRegion region)
        {
            if (region.drops[index].model.TryGetComponent(out InteractableSign sign))
                return sign.text;
            else return string.Empty;
        }
        public static string GetSignText(Transform transform)
        {
            if (BarricadeManager.tryGetInfo(transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
                return GetSignText(index, region);
            else return string.Empty;
        }
        public static bool IsSign(this BarricadeDrop barricade) => barricade.model.TryGetComponent(out InteractableSign _);
        public static bool IsSign(this BarricadeData barricade, BarricadeRegion region)
        {
            if (barricade == null || region == null) return false;
            int index = region.barricades.FindIndex(b => b != null && b.instanceID == barricade.instanceID);
            if (index < region.drops.Count)
            {
                BarricadeDrop drop = region.drops[index];
                if (drop != null && drop.interactable != null && drop.interactable.GetType() == typeof(InteractableSign)) return true;
                else return false;
            }
            else return false;
        }
        public static float GetTerrainHeightAt2DPoint(Vector2 position, float above = 0) => GetTerrainHeightAt2DPoint(position.x, position.y, above: above);
        public static float GetTerrainHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.GROUND | RayMasks.GROUND2))
                return h.point.y + above;
            else return defaultY;
        }
        public static string ReplaceCaseInsensitive(this string source, string replaceIf, string replaceWith = "")
        {
            if (source == null) return null;
            if (replaceIf == null || replaceWith == null || source.Length == 0 || replaceIf.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = source.ToLower().ToCharArray();
            char[] replaceIfChars = replaceIf.ToLower().ToCharArray();
            StringBuilder buffer = new StringBuilder();
            int replaceIfLength = replaceIfChars.Length;
            StringBuilder newString = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (buffer.Length < replaceIfLength)
                {
                    if (lowerchars[i] == replaceIfChars[buffer.Length]) buffer.Append(chars[i]);
                    else
                    {
                        if (buffer.Length != 0)
                            newString.Append(buffer.ToString());
                        buffer.Clear();
                        newString.Append(chars[i]);
                    }
                }
                else
                {
                    if (replaceWith.Length != 0) newString.Append(replaceWith);
                    newString.Append(chars[i]);
                }
            }
            return newString.ToString();
        }
        public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
        {
            if (source == null) return null;
            if (replacables.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
            char[] lowerrepls;
            if (!caseSensitive)
            {
                lowerrepls = new char[replacables.Length];
                for (int i = 0; i < replacables.Length; i++)
                {
                    lowerrepls[i] = char.ToLower(replacables[i]);
                }
            }
            else lowerrepls = replacables;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                bool found = false;
                for (int c = 0; c < lowerrepls.Length; c++)
                {
                    if (lowerrepls[c] == lowerchars[i])
                    {
                        found = true;
                    }
                }
                if (!found) sb.Append(chars[i]);
            }
            return sb.ToString();
        }
        public static void TriggerEffectReliable(ushort ID, CSteamID player, Vector3 Position)
        {
            TriggerEffectParameters p = new TriggerEffectParameters(ID)
            {
                position = Position,
                reliable = true,
                relevantPlayerID = player
            };
            EffectManager.triggerEffect(p);
        }
        public static bool SavePhotoToDisk(string path, Texture2D texture)
        {
            byte[] data = texture.EncodeToPNG();
            try
            {
                FileStream stream = File.Create(path);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch { return false; }
        }
        // https://answers.unity.com/questions/244417/create-line-on-a-texture.html
        public static void DrawLine(Texture2D texture, Line line, Color color, bool apply = true, float thickness = 1)
        {
            if (thickness == 0) return;
            Vector2 point1 = new Vector2(line.pt1.x + texture.width / 2, line.pt1.y + texture.height / 2);
            Vector2 point2 = new Vector2(line.pt2.x + texture.width / 2, line.pt2.y + texture.height / 2);
            Vector2 t = point1;
            float frac = 1 / Mathf.Sqrt(Mathf.Pow(point2.x - point1.x, 2) + Mathf.Pow(point2.y - point1.y, 2));
            float ctr = 0;

            while ((int)t.x != (int)point2.x || (int)t.y != (int)point2.y)
            {
                t = Vector2.Lerp(point1, point2, ctr);
                ctr += frac;
                texture.SetPixelClamp((int)t.x, (int)t.y, color);
                if(thickness > 1)
                {
                    float distance = thickness / 2f;
                    for(float i = -distance; i <= distance; i += 0.5f)
                        texture.SetPixelClamp(Mathf.RoundToInt(t.x + i), Mathf.RoundToInt(t.y + i), color);
                }
            }
            if (apply)
                texture.Apply();
        }
        // https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity
        public static void FillCircle(Texture2D texture, float x, float y, float radius, Color color, bool apply = true)
        {
            float rSquared = radius * radius;

            for (float u = x - radius; u < x + radius + 1; u++)
                for (float v = y - radius; v < y + radius + 1; v++)
                    if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                        texture.SetPixelClamp((int)Math.Round(u), (int)Math.Round(v), color);
            if (apply)
                texture.Apply();
        }
        public static void SetPixelClamp(this Texture2D texture, int x, int y, Color color)
        {
            if (x <= texture.width && x >= 0 && y <= texture.height && y >= 0) texture.SetPixel(x, y, color);
        }
        public static void DrawCircle(Texture2D texture, float x, float y, float radius, Color color, float thickness = 1, bool apply = true, bool drawLineToOutside = false, float polygonResolutionScale = 1f)
        {
            if (thickness == 0) return;
            float sides_radians = (Mathf.PI / 180) * polygonResolutionScale;
            float increment = (Mathf.PI * 2) * sides_radians;
            int x1;
            int y1;
            x1 = Mathf.RoundToInt(x + radius);
            y1 = Mathf.RoundToInt(y);
            for (float r = 0; r < sides_radians; r += increment)
            {
                Vector2 p = GetPositionOnCircle(r, radius);
                int x2 = Mathf.RoundToInt(p.x + x);
                int y2 = Mathf.RoundToInt(p.y + y);
                F.DrawLine(texture, new Line(new Vector2(x1, y1), new Vector2(x2, y2)), color, false, thickness);
                x1 = x2;
                y1 = y2;
            }
            if (drawLineToOutside) DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x + radius, y)), color, false, thickness);
            if (drawLineToOutside) DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x - radius, y)), color, false, thickness);
            if (drawLineToOutside) DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x, y + radius)), color, false, thickness);
            if (drawLineToOutside) DrawLine(texture, new Line(new Vector2(x, y), new Vector2(x, y - radius)), color, false, thickness);
            if (apply)
                texture.Apply();
        }
        public static Vector2 GetPositionOnCircle(float radians, float radius = 1) => new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
        public static Texture2D FlipVertical(Texture2D original)
        {
            Texture2D rtn = new Texture2D(original.width, original.height);
            for (int i = 0; i < original.height; i++)
                rtn.SetPixels(0, original.height - 1 - i, original.width, 1, original.GetPixels(0, i, original.width, 1));
            rtn.Apply();
            return rtn;
        }
        public static Texture2D FlipHorizontal(Texture2D original)
        {
            Texture2D rtn = new Texture2D(original.width, original.height);
            for (int i = 0; i < original.width; i++)
                rtn.SetPixels(original.width - 1 - i, 9, 1, original.height, original.GetPixels(i, 0, 1, original.height));
            rtn.Apply();
            return rtn;
        }
        public static bool TryGetPlaytimeComponent(this Player player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static bool TryGetPlaytimeComponent(this CSteamID player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static bool TryGetPlaytimeComponent(this ulong player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static PlaytimeComponent GetPlaytimeComponent(this Player player, out bool success)
        {
            if (Data.PlaytimeComponents.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
            {
                success = Data.PlaytimeComponents[player.channel.owner.playerID.steamID.m_SteamID] != null;
                return Data.PlaytimeComponents[player.channel.owner.playerID.steamID.m_SteamID];
            }
            else if (player == null || player.transform == null)
            {
                success = false;
                return null;
            }
            else if (player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
        public static void SetPrivatePlayerCount(byte amount)
        {
            if (Provider.maxPlayers == amount) return;
            try
            {
                FieldInfo field = typeof(Provider).GetField("_maxPlayers", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, amount);
            }
            catch (Exception ex)
            {
                LogError("Error setting player count:");
                LogError(ex);
            }
        }
        public static PlaytimeComponent GetPlaytimeComponent(this CSteamID player, out bool success)
        {
            if (Data.PlaytimeComponents.ContainsKey(player.m_SteamID))
            {
                success = Data.PlaytimeComponents[player.m_SteamID] != null;
                return Data.PlaytimeComponents[player.m_SteamID];
            }
            else if (player == default || player == CSteamID.Nil)
            {
                success = false;
                return null;
            }
            else 
            {
                Player p = PlayerTool.getPlayer(player);
                if(p == null)
                {
                    success = false;
                    return null;
                }
                if (p.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static PlaytimeComponent GetPlaytimeComponent(this ulong player, out bool success)
        {
            if (player == 0)
            {
                success = false;
                return default;
            }
            if (Data.PlaytimeComponents.ContainsKey(player))
            {
                success = Data.PlaytimeComponents[player] != null;
                return Data.PlaytimeComponents[player];
            }
            else 
            {
                SteamPlayer p = PlayerTool.getSteamPlayer(player);
                if(p == default || p.player == default)
                {
                    success = false;
                    return null;
                }
                if (p.player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static float GetCurrentPlaytime(this Player player)
        {
            if (player.TryGetPlaytimeComponent(out PlaytimeComponent playtimeObj))
                return playtimeObj.CurrentTimeSeconds;
            else return 0f;
        }
        public static FPlayerName GetPlayerOriginalNames(UCPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(SteamPlayer player) => GetPlayerOriginalNames(player.player);
        public static FPlayerName GetPlayerOriginalNames(UnturnedPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(Player player)
        {
            if (Data.OriginalNames.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
                return Data.OriginalNames[player.channel.owner.playerID.steamID.m_SteamID];
            else return new FPlayerName(player);
        }
        public static FPlayerName GetPlayerOriginalNames(ulong player)
        {
            if (Data.OriginalNames.ContainsKey(player))
                return Data.OriginalNames[player];
            else
            {
                SteamPlayer pl = PlayerTool.getSteamPlayer(player);
                if (pl == default) return new FPlayerName()
                {
                    CharacterName = player.ToString(Data.Locale),
                    NickName = player.ToString(Data.Locale),
                    PlayerName = player.ToString(Data.Locale),
                    Steam64 = player
                };
                else return new FPlayerName()
                {
                    CharacterName = pl.playerID.characterName,
                    NickName = pl.playerID.nickName,
                    PlayerName = pl.playerID.playerName,
                    Steam64 = player
                };
            }
        }
        public static bool TryGetPlayerOriginalNamesFromS64(ulong player, out FPlayerName originalNames) => Data.OriginalNames.TryGetValue(player, out originalNames);
        public static bool IsInMain(this Player player)
        {
            ulong team = player.GetTeam();
            if (team == 1) return TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsOnFlag(this Player player) => Data.Gamemode is FlagGamemode fg && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
        public static async Task<string> TranslateSign(string key, ulong player, bool important, params string[] formatting)
        {
            string norm = Translate(key, player, formatting);
            if (!key.StartsWith("sign_") || norm != key) return norm;
            if (key.Length <= 5) return key;
            string kitname = key.Substring(5);
            if (kitname.StartsWith("vbs_") && ushort.TryParse(kitname.Substring(4), System.Globalization.NumberStyles.Any, Data.Locale, out ushort vehicleid))
            {
                if (Vehicles.VehicleBay.VehicleExists(vehicleid, out Vehicles.VehicleData data))
                {
                    VehicleAsset asset = UCAssetManager.FindVehicleAsset(vehicleid);
                    if (asset == default) return norm;
                    if (data.RequiredLevel > 0)
                    {
                        Rank rank = XPManager.GetRankFromLevel(data.RequiredLevel);
                        Rank playerrank = player == 0 ? null : XPManager.GetRank(await XPManager.GetXP(player, player.GetTeamFromPlayerSteam64ID(), false), out _, out _);
                        if (rank == default) return norm;
                        return Translate("vehiclebay_sign_min_level", player, asset.vehicleName, UCWarfare.GetColorHex("vbs_vehicle_name_color"), rank.TranslateName(player), 
                            player != 0 && rank.level > playerrank.level ? UCWarfare.GetColorHex("vbs_locked_vehicle_color") : UCWarfare.GetColorHex("vbs_rank_color"), data.TicketCost.ToString(Data.Locale),
                            UCWarfare.GetColorHex("vbs_ticket_cost"), UCWarfare.GetColorHex("vbs_background"));
                    }
                    else
                    {
                        return Translate("vehiclebay_sign_no_min_level", player, asset.vehicleName, UCWarfare.GetColorHex("vbs_vehicle_name_color"), data.TicketCost.ToString(Data.Locale), 
                            UCWarfare.GetColorHex("vbs_ticket_cost"), UCWarfare.GetColorHex("vbs_background"));
                    }
                }
                else return norm;
            } 
            else if (KitManager.KitExists(kitname, out Kit kit))
            {
                ulong playerteam = GetTeamFromPlayerSteam64ID(player);
                string line2string;
                string line2color;
                if(kit.IsPremium)
                {
                    if (kit.PremiumCost != 0)
                    {
                        line2color = kit.AllowedUsers.Contains(player) ? (kit.Cost == 0 ? UCWarfare.GetColorHex("kit_price_owned") : UCWarfare.GetColorHex("kit_level"))
                            : UCWarfare.GetColorHex("kit_price_dollars");
                        line2string = kit.AllowedUsers.Contains(player) ? (kit.Cost == 0 ? Translate("kit_owned", player) : Translate("kit_price_credits", player, kit.Cost.ToString(Data.Locale)))
                            : Translate("kit_price_dollars", player, kit.PremiumCost.ToString(Data.Locale));
                    }
                    else if (kit.RequiredLevel == 0)
                    {
                        line2string = Translate("kit_available", player);
                        line2color = UCWarfare.GetColorHex("kit_price_free");
                    }
                    else
                    {
                        Rank rank = XPManager.GetRankFromLevel(kit.RequiredLevel);
                        Rank playerrank = player == 0 ? null : XPManager.GetRank(await XPManager.GetXP(player, playerteam, important), out _, out _);
                        line2string = Translate("kit_required_level", player, kit.RequiredLevel.ToString(Data.Locale), 
                            player != 0 && rank.level > playerrank.level ? UCWarfare.GetColorHex("vbs_locked_vehicle_color") : UCWarfare.GetColorHex("vbs_rank_color"));
                        line2color = UCWarfare.GetColorHex("kit_price_tickets");
                    }
                }
                else if (kit.RequiredLevel == 0)
                {
                    line2string = Translate("kit_available", player);
                    line2color = UCWarfare.GetColorHex("kit_price_free");
                }
                else
                {
                    Rank rank = XPManager.GetRankFromLevel(kit.RequiredLevel);
                    Rank playerrank = player == 0 ? null : XPManager.GetRank(await XPManager.GetXP(player, playerteam, important), out _, out _);
                    line2string = Translate("kit_required_level", player, kit.RequiredLevel.ToString(Data.Locale),
                        player != 0 && rank.level > playerrank.level ? UCWarfare.GetColorHex("vbs_locked_vehicle_color") : UCWarfare.GetColorHex("vbs_rank_color"));
                    line2color = UCWarfare.GetColorHex("kit_level");
                }
                bool full = kit.IsLimited(out int current, out int max, playerteam, true);
                string lang = DecideLanguage(player, kit.SignTexts);
                return Translate("sign_kit_request", player, kit.SignTexts.TryGetValue(lang, out string val) ? val : kit.Name, GetTeamNumberColorHex(kit.Team), line2string, line2color, 
                    current.ToString(Data.Locale), kit.IsPremium || kit.TeamLimit == 1f ? INFINITY_SYMBOL.ToString() : max.ToString(Data.Locale), full ? UCWarfare.GetColorHex("vbs_locked_vehicle_color") : UCWarfare.GetColorHex("vbs_rank_color"));
            }
            else return key;
        }
        public static string DecideLanguage<TVal>(ulong player, Dictionary<string, TVal> searcher)
        {
            if (player == 0)
            {
                if (!searcher.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (searcher.Count > 0)
                    {
                        return searcher.ElementAt(0).Key;
                    }
                    else return JSONMethods.DefaultLanguage;
                }
                else return JSONMethods.DefaultLanguage;
            }
            else
            {
                if (!Data.Languages.TryGetValue(player, out string lang) || !searcher.ContainsKey(lang))
                {
                    if (searcher.Count > 0)
                    {
                        return searcher.ElementAt(0).Key;
                    }
                    else return JSONMethods.DefaultLanguage;
                }
                return lang;
            }
        }
        public static string TranslateLimb(ulong player, ELimb limb)
        {
            if (player == 0)
            {
                if (!Data.LimbLocalization.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        if (Data.LimbLocalization.ElementAt(0).Value.ContainsKey(limb))
                        {
                            return Data.LimbLocalization.ElementAt(0).Value[limb];
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else
                {
                    if (Data.LimbLocalization[JSONMethods.DefaultLanguage].ContainsKey(limb))
                    {
                        return Data.LimbLocalization[JSONMethods.DefaultLanguage][limb];
                    }
                    else return limb.ToString();
                }
            }
            else
            {
                string lang = JSONMethods.DefaultLanguage;
                if (Data.Languages.ContainsKey(player))
                {
                    lang = Data.Languages[player];
                    if (!Data.LimbLocalization.ContainsKey(lang) || !Data.LimbLocalization[lang].ContainsKey(limb))
                        lang = JSONMethods.DefaultLanguage;
                }
                if (!Data.LimbLocalization.ContainsKey(lang))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        if (Data.LimbLocalization.ElementAt(0).Value.ContainsKey(limb))
                        {
                            return Data.LimbLocalization.ElementAt(0).Value[limb];
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else if (Data.LimbLocalization[lang].ContainsKey(limb))
                {
                    return Data.LimbLocalization[lang][limb];
                }
                else return limb.ToString();
            }
        }
        /// <param name="backupcause">Used in case the key can not be found.</param>
        public static string TranslateDeath(ulong player, string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, ELimb limb, string itemName, float distance, bool usePlayerName = false, bool translateKillerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = ColorizeName(deadname, deadTeam);
            string murderername = translateKillerName ? Translate(killerName.PlayerName, player) : (usePlayerName ? killerName.PlayerName : killerName.CharacterName);
            if (colorize) murderername = ColorizeName(murderername, killerTeam);
            string dis = Math.Round(distance).ToString(Data.Locale) + 'm';
            if (player == 0)
            {
                if (!Data.DeathLocalization.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(backupcause.ToString()))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[backupcause.ToString()], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                }
                else
                {
                    if (Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(key))
                    {
                        try
                        {
                            return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                        }
                        catch (FormatException ex)
                        {
                            LogError(ex);
                            return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                        }
                    }
                    else if (Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(backupcause.ToString()))
                    {
                        try
                        {
                            return string.Format(Data.DeathLocalization[JSONMethods.DefaultLanguage][backupcause.ToString()], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                        }
                        catch (FormatException ex)
                        {
                            LogError(ex);
                            return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                        }
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                }
            }
            else
            {
                string lang = JSONMethods.DefaultLanguage;
                if (Data.Languages.ContainsKey(player))
                {
                    lang = Data.Languages[player];
                    if (!Data.DeathLocalization.ContainsKey(lang) || !Data.DeathLocalization[lang].ContainsKey(key))
                        lang = JSONMethods.DefaultLanguage;
                }
                if (!Data.DeathLocalization.ContainsKey(lang))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(backupcause.ToString()))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[backupcause.ToString()], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                }
                else if (Data.DeathLocalization[lang].ContainsKey(key))
                {
                    try
                    {
                        return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                    }
                    catch (FormatException ex)
                    {
                        LogError(ex);
                        return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                }
                else if (Data.DeathLocalization[lang].ContainsKey(backupcause.ToString()))
                {
                    try
                    {
                        return string.Format(Data.DeathLocalization[lang][backupcause.ToString()], deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                    }
                    catch (FormatException ex)
                    {
                        LogError(ex);
                        return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                }
                else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
            }
        }
        public static string TranslateLandmineDeath(ulong player, string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, bool usePlayerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = ColorizeName(deadname, deadTeam);
            string murderername = usePlayerName ? killerName.PlayerName : killerName.CharacterName;
            if (colorize) murderername = ColorizeName(murderername, killerTeam);
            string triggerername = usePlayerName ? triggererName.PlayerName : triggererName.CharacterName;
            if (colorize) triggerername = ColorizeName(triggerername, triggererTeam);
            if (player == 0)
            {
                if (!Data.DeathLocalization.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                }
                else
                {
                    if (Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(key))
                    {
                        try
                        {
                            return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
                        }
                        catch (FormatException ex)
                        {
                            LogError(ex);
                            return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                        }
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                }
            }
            else
            {
                string lang = JSONMethods.DefaultLanguage;
                if (Data.Languages.ContainsKey(player))
                {
                    if (Data.DeathLocalization.ContainsKey(lang) && Data.DeathLocalization[lang].ContainsKey(key))
                        lang = Data.Languages[player];
                }
                if (!Data.DeathLocalization.ContainsKey(lang))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        if (Data.DeathLocalization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                }
                else if (Data.DeathLocalization[lang].ContainsKey(key))
                {
                    try
                    {
                        return string.Format(Data.DeathLocalization.ElementAt(0).Value[key], deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
                    }
                    catch (FormatException ex)
                    {
                        LogError(ex);
                        return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                    }
                }
                else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
            }
        }
        public static string ColorizeName(string innerText, ulong team)
        {
            if (team == TeamManager.ZombieTeamID) return $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>";
            else if (team == TeamManager.Team1ID) return $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>";
            else if (team == TeamManager.Team2ID) return $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>";
            else if (team == TeamManager.AdminID) return $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>";
            else return $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>";
        }
        /// <param name="backupcause">Used in case the key can not be found.</param>
        public static void BroadcastDeath(string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, bool translateKillerName, ulong killerTeam, ELimb limb, string itemName, float distance, out string message, bool broadcast = true)
        {
            if(broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string killer = translateKillerName ? Translate(killerName.CharacterName, player) : killerName.CharacterName;
                    string localizedString = TranslateDeath(player.playerID.steamID.m_SteamID, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, false, translateKillerName);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString, UCWarfare.GetColor("death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string defaultMessage = key;
                        string newMessage;
                        if (JSONMethods.DefaultDeathTranslations.ContainsKey(key))
                            defaultMessage = JSONMethods.DefaultDeathTranslations[key];
                        else if (JSONMethods.DefaultDeathTranslations.ContainsKey(backupcause.ToString()))
                            defaultMessage = JSONMethods.DefaultDeathTranslations[backupcause.ToString()];
                        try
                        {
                            newMessage = string.Format(defaultMessage, ColorizeName(dead.CharacterName, deadTeam), ColorizeName(killer, killerTeam),
                                TranslateLimb(player.playerID.steamID.m_SteamID, limb), itemName, Math.Round(distance).ToString(Data.Locale));
                        }
                        catch (FormatException)
                        {
                            newMessage = key + $" ({ColorizeName(dead.CharacterName, deadTeam)}, {ColorizeName(killer, killerTeam)}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + key + "\"");
                        }
                        if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                            ChatManager.say(player.playerID.steamID, newMessage, UCWarfare.GetColor("death_background"), newMessage.Contains("</"));
                        else
                            LogError("There's been an error sending a chat message. Default message for \"" + key + "\" is longer than "
                                + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
                    }
                }
            }
            message = TranslateDeath(0, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, true, translateKillerName, false);
        }
        public static void BroadcastLandmineDeath(string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, out string message, bool broadcast = true)
        {
            if(broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string localizedString = TranslateLandmineDeath(player.playerID.steamID.m_SteamID, key, dead, deadTeam, killerName, killerTeam, triggererName, triggererTeam, limb, landmineName, false);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString, UCWarfare.GetColor("death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string defaultMessage = key;
                        string newMessage;
                        if (JSONMethods.DefaultDeathTranslations.ContainsKey(key))
                            defaultMessage = JSONMethods.DefaultDeathTranslations[key];
                        try
                        {
                            newMessage = string.Format(defaultMessage, ColorizeName(dead.CharacterName, deadTeam), ColorizeName(killerName.CharacterName, killerTeam),
                                TranslateLimb(player.playerID.steamID.m_SteamID, limb), landmineName, "0", ColorizeName(triggererName.CharacterName, triggererTeam));
                        }
                        catch (FormatException)
                        {
                            newMessage = key + $" ({ColorizeName(dead.CharacterName, deadTeam)}, {ColorizeName(killerName.CharacterName, killerTeam)}, {limb}, {landmineName}, {triggererName.CharacterName}";
                            LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + key + "\"");
                        }
                        if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                            ChatManager.say(player.playerID.steamID, newMessage, UCWarfare.GetColor("death_background"), newMessage.Contains("</"));
                        else
                            LogError("There's been an error sending a chat message. Default message for \"" + key + "\" is longer than "
                                + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
                    }
                }
            }
            message = TranslateLandmineDeath(0, key, dead, deadTeam, killerName, killerTeam, triggererName, triggererTeam, limb, landmineName, true, false);
        }
        public static void CheckDir(string path, out bool success, bool unloadIfFail = false)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    success = true;
                    Log("Created directory: \"" + path + "\".", ConsoleColor.Magenta);
                }
                catch (Exception ex)
                {
                    LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                    success = false;
                    if(unloadIfFail)
                        UCWarfare.I?.UnloadPlugin();
                }
            }
            else success = true;
        }
        public static void AddLine(string text, ConsoleColor color)
        {
            try
            {
                if (Data.AppendConsoleMethod != null && Data.defaultIOHandler != null)
                {
                    Data.AppendConsoleMethod.Invoke(Data.defaultIOHandler, new object[] { text, color });
                }
            }
            catch
            {
                switch (color)
                {
                    case ConsoleColor.Gray:
                    default:
                        CommandWindow.Log(text);
                        break;
                    case ConsoleColor.Yellow:
                        CommandWindow.LogWarning(text);
                        break;
                    case ConsoleColor.Red:
                        CommandWindow.LogError(text);
                        break;
                }
            }
        }
        public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Gray || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.Log(info);
                }
                else
                {
                    AddLine(info, color);
                    UnturnedLog.info($"[IN] {info}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = info, RCON = true, Severity = Rocket.Core.Logging.ELogType.Info });
                }
            } catch (Exception ex)
            {
                CommandWindow.Log(info);
                LogError(ex);
            }
        }
        public static void LogWarning(string warning, ConsoleColor color = ConsoleColor.Yellow)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Yellow || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogWarning(warning);
                }
                else
                {
                    AddLine(warning, color);
                    UnturnedLog.warn($"[WA] {warning}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = warning, RCON = true, Severity = Rocket.Core.Logging.ELogType.Warning });
                }
            }
            catch (Exception ex)
            {
                CommandWindow.LogWarning(warning);
                LogError(ex);
            }
        }
        public static void LogError(string error, ConsoleColor color = ConsoleColor.Red)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogError(error);
                }
                else
                {
                    AddLine(error, color);
                    UnturnedLog.warn($"[ER] {error}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = error, RCON = true, Severity = Rocket.Core.Logging.ELogType.Error });
                }
            }
            catch (Exception ex)
            {
                CommandWindow.LogError(error);
                UnturnedLog.error(ex);
            }
        }
        public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            string message = $"EXCEPTION \n\n{ex.Message}\n{ex.StackTrace}\n\nFINISHED";
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || Data.AppendConsoleMethod == default)
                {
                    CommandWindow.LogError(message);
                }
                else
                {
                    AddLine(message, color);
                    UnturnedLog.warn($"[EX] {ex.Message}");
                    UnturnedLog.warn($"[ST] {ex.StackTrace}");
                    Rocket.Core.Logging.AsyncLoggerQueue.Current?.Enqueue(new Rocket.Core.Logging.LogEntry() { Message = message, RCON = true, Severity = Rocket.Core.Logging.ELogType.Exception });
                }
            }
            catch (Exception ex2)
            {
                CommandWindow.LogError($"{message}\nEXCEPTION LOGGING \n\n{ex2.Message}\n{ex2.StackTrace}\n\nFINISHED");
            }
        }
        public static string GetKitDisplayName(string kitname)
        {
            IEnumerable<Kits.Kit> kits = Kits.KitManager.GetKitsWhere(x => x.Name == kitname);
            if (kits.Count() > 0) return kits.ElementAt(0).DisplayName;
            else return kitname;
        }
        public static float GetDistanceFromClosestObjective(Vector3 position, out Flag objective, bool includeOutOfRotation = false) => Mathf.Sqrt(GetSqrDistanceFromClosestObjective(position, out objective, includeOutOfRotation));
        public static float GetSqrDistanceFromClosestObjective(Vector3 position, out Flag objective, bool includeOutOfRotation = false)
        {
            if (Data.Gamemode is FlagGamemode fg)
            {
                Vector2 position2d = new Vector2(position.x, position.z);
                Flag closestFlag = default;
                float? closestSqrDistance = default;
                foreach (Flag flag in includeOutOfRotation ? fg.AllFlags : fg.Rotation)
                {
                    float distance = (flag.Position2D - position2d).sqrMagnitude;
                    if (!closestSqrDistance.HasValue)
                    {
                        closestFlag = flag;
                        closestSqrDistance = distance;
                    }
                    else if (distance < closestSqrDistance)
                    {
                        closestFlag = flag;
                        closestSqrDistance = distance;
                    }
                }
                if (closestSqrDistance.HasValue)
                {
                    objective = closestFlag;
                    return closestSqrDistance.Value;
                }
                else
                {
                    objective = default;
                    return float.NaN;
                }
            } else
            {
                objective = null;
                return float.NaN;
            }
        }
        /// <summary>Dimensions controlled by <see cref="Stats.Playstyle.GRID_SIZE"/>.</summary>
        public static Vector2 RoundLocationToGrid(Vector3 position) 
        {
            float gridSquareSize = Level.size / Stats.Playstyle.GRID_SIZE;
            return new Vector2(Mathf.Floor(position.x / gridSquareSize) * gridSquareSize, Mathf.Floor(position.z / gridSquareSize) * gridSquareSize);
        }
        public static UncreatedPlayer GetPlayerStats(UnturnedPlayer player) => GetPlayerStats(player.Player.channel.owner.playerID.steamID.m_SteamID);
        public static UncreatedPlayer GetPlayerStats(Player player) => GetPlayerStats(player.channel.owner.playerID.steamID.m_SteamID);
        public static UncreatedPlayer GetPlayerStats(SteamPlayer player) => GetPlayerStats(player.playerID.steamID.m_SteamID);
        public static UncreatedPlayer GetPlayerStats(CSteamID player) => GetPlayerStats(player.m_SteamID);
        public static UncreatedPlayer GetPlayerStats(ulong player)
        {
            if (TryGetPlaytimeComponent(player, out PlaytimeComponent c))
            {
                return c.UCPlayerStats;
            } else
            {
                return UncreatedPlayer.Load(player);
            }
        }
        public static bool Between<T>(this T number, T highBound, T lowBound, bool inclusiveHigh = true, bool inclusiveLow = false) where T : IComparable
        {
            int high = number.CompareTo(highBound);
            int low = number.CompareTo(lowBound);
            return (low == 1 || (inclusiveLow && low == 0)) && (high == -1 || (inclusiveHigh && high == 0));
        }
        public static string GetClosestNode(this Vector3 position)
        {
            if (!Level.isLoaded) return string.Empty;
            float? smallest = null;
            int index = 0;
            for(int i = 0; i < LevelNodes.nodes.Count; i++)
            {
                float distance = (position - LevelNodes.nodes[i].point).sqrMagnitude;
                if (LevelNodes.nodes[i] is LocationNode)
                {
                    if (!smallest.HasValue || distance < smallest)
                    {
                        index = i;
                        smallest = distance;
                    }
                }
            }
            if (LevelNodes.nodes[index] is LocationNode name) return name.name;
            else return string.Empty;
        }
        public static void SendSteamURL(this SteamPlayer player, string message, ulong SteamID) => player.SendURL(message, $"https://steamcommunity.com/profiles/{SteamID}/");
        public static void SendURL(this SteamPlayer player, string message, string url)
        {
            if (player == default || url == default) return;
            player.player.sendBrowserRequest(message, url);
        }
        public static BarricadeData GetBarricadeFromInstID(uint instanceID, out BarricadeDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.barricades.Count; i++)
                    {
                        if (region.barricades[i].instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.barricades[i];
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.barricades.Count; i++)
                {
                    if (region.barricades[i].instanceID == instanceID)
                    {
                        drop = region.drops[i];
                        return region.barricades[i];
                    }
                }
            }
            drop = default;
            return default;
        }
        public static StructureData GetStructureFromInstID(uint instanceID, out StructureDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.structures.Count; i++)
                    {
                        if (region.structures[i].instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.structures[i];
                        }
                    }
                }
            }
            drop = default;
            return default;
        }
        public static BarricadeData GetBarricadeFromTransform(SerializableTransform location, out BarricadeDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.barricades.Count; i++)
                    {
                        if (location == region.drops[i].model.transform)
                        {
                            drop = region.drops[i];
                            return region.barricades[i];
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.barricades.Count; i++)
                {
                    if (location == region.drops[i].model.transform)
                    {
                        drop = region.drops[i];
                        return region.barricades[i];
                    }
                }
            }
            drop = default;
            return default;
        }
        public static StructureData GetStructureFromTransform(SerializableTransform location, out StructureDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.structures.Count; i++)
                    {
                        if (location == region.drops[i].model.transform)
                        {
                            drop = region.drops[i];
                            return region.structures[i];
                        }
                    }
                }
            }
            drop = default;
            return default;
        }
        public static string TranslateBranch(EBranch branch, UCPlayer player)
        {
            string branchName = "team";
            if (player.IsTeam1())
                branchName += "1_";
            else if (player.IsTeam2())
                branchName += "2_";

            return Translate(branchName + branch.ToString().ToLower(), player.Steam64, out _);
        }
        public static Vector3 TraceForceParabola(Vector3 direction, Vector3 start, int Raymask)
        {
            Vector3 prev = start;
            F.Log("START: " + prev.ToString() + ", DIRECTION: " + direction.ToString());
            for (int i = 1; ; i++)
            {
                float t = Time.fixedDeltaTime * i * 10;
                if (t > 60f) break;
                Vector3 pos = start + direction * t + Physics.gravity * t * t * 0.5f;
                F.Log(i.ToString(Data.Locale) + " (" + t.ToString(Data.Locale) + ").");
                F.Log(pos.ToString());
                if (Physics.Linecast(prev, pos, out RaycastHit hit, Raymask) && hit.transform != null && !hit.transform.gameObject.CompareTag("Border")) return hit.point;
                if(Provider.clients.Count > 0)
                    EffectManager.sendEffectReliable(Squads.SquadManager.config.Data.EmptyMarker, Provider.clients[0].transportConnection, pos);
                prev = pos;
            }
            F.Log("FAILED");
            return Vector3.zero;
        }
        public static string GetLayer(Vector3 direction, Vector3 origin, int Raymask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 8192f, Raymask))
            {
                if (hit.transform != null)
                    return hit.transform.gameObject.layer.ToString();
                else return "nullHitNoTransform";
            }
            else return "nullNoHit";
        }
    }
}