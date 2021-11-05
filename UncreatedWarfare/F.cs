using Rocket.API;
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
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.XP;
using UnityEngine;
using Color = UnityEngine.Color;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare
{
    public static class F
    {
        public const float SPAWN_HEIGHT_ABOVE_GROUND = 0.5f;
        public static readonly List<char> vowels = new List<char> { 'a', 'e', 'i', 'o', 'u' };
        /// <summary>Convert an HTMLColor string to a actual color.</summary>
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
        public static string[] ReadStringArray(ByteReader R)
        {
            int length = R.ReadInt32();
            string[] rtn = new string[length];
            for (int i = 0; i < length; i++)
                rtn[i] = R.ReadString();
            return rtn;
        }
        public static void WriteStringArray(ByteWriter W, string[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
                W.Write(A[i]);
        }
        public static string GetTimeFromSeconds(this uint seconds, ulong player)
        {
            if (seconds < 60) // < 1 minute
            {
                return (seconds + 1).ToString(Data.Locale) + ' ' + Translate("time_second" + seconds.S(), player);
            }
            else if (seconds < 3600) // < 1 hour
            {
                int minutes = DivideRemainder(seconds, 60, out int secondOverflow);
                return $"{minutes} {Translate("time_minute" + minutes.S(), player)}{(secondOverflow == 0 ? "" : $" {Translate("time_and", player)} {secondOverflow} {Translate("time_second" + secondOverflow.S(), player)}")}";
            }
            else if (seconds < 86400) // < 1 day 
            {
                int hours = DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
            }
            else if (seconds < 2628000) // < 1 month (30.416 days) (365/12)
            {
                uint days = DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (seconds < 31536000) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), player)}{years.S()}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
            }
        }
        public static string GetTimeFromMinutes(this uint minutes, ulong player)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString(Data.Locale) + ' ' + Translate("time_minute" + minutes.S(), player);
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
            }
            else if (minutes < 43800) // < 1 month (30.416 days)
            {
                uint days = DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), player)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
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
            if (key == null)
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
            SendChat(player.Player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, Color textColor, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="UCPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UCPlayer player, string text, params string[] formatting) =>
            SendChat(player.Player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, Color textColor, params string[] formatting) =>
            SendChat(player.channel.owner, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, params string[] formatting) =>
            SendChat(player.channel.owner, text, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, Color textColor, params string[] formatting)
        {
            string localizedString = Translate(text, player.playerID.steamID.m_SteamID, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                SendSingleMessage(localizedString, textColor, EChatMode.SAY, null, localizedString.Contains("</"), player);
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
                    SendSingleMessage(newMessage, textColor, EChatMode.SAY, null, newMessage.Contains("</"), player);
                else
                    LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, params string[] formatting)
        {
            string localizedString = Translate(text, player.playerID.steamID.m_SteamID, out Color textColor, formatting);
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                SendSingleMessage(localizedString, textColor, EChatMode.SAY, null, localizedString.Contains("</"), player);
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
                    SendSingleMessage(newMessage, textColor, EChatMode.SAY, null, newMessage.Contains("</"), player);
                else
                    LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MaxChatSizeAmount.ToString(Data.Locale) + " bytes in UTF-8. Arguments may be too long.");
            }
        }
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
            SteamPlayer sp = PlayerTool.getSteamPlayer(player);
            if (sp != null)
                sp.SendChat(text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat using the translation file, automatically extrapolates the color.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, params string[] formatting)
        {
            SteamPlayer sp = PlayerTool.getSteamPlayer(player);
            if (sp != null)
                sp.SendChat(text, formatting);
        }
        /// <summary>
        /// Send a white message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this UnturnedPlayer player, string message, params string[] formatting) =>
            SendChat(player.Player.channel.owner, message, formatting);
        /// <summary>
        /// Send a message in chat using the translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.
        /// </para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this Player player, string message, params string[] formatting) =>
            SendChat(player.channel.owner, message, formatting);
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, Color textColor, params string[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients)
                SendChat(player, text, textColor, formatting);
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
                SendChat(player, text, formatting);
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
                SendChat(player, text, textColor, formatting);
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
                SendChat(player, text, formatting);
        }
        public static bool OnDutyOrAdmin(this IRocketPlayer player) => (player is UnturnedPlayer pl && pl.Player.channel.owner.isAdmin) || (player is UCPlayer upl && upl.Player.channel.owner.isAdmin) || R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool OnDuty(this IRocketPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool OffDutyAndNotAdmin(this IRocketPlayer player) => !OnDutyOrAdmin(player);
        public static bool OffDuty(this IRocketPlayer player) => !OnDuty(player);
        public static bool IsIntern(this IRocketPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool IsAdmin(this IRocketPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup);
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
        public static void SendSingleMessage(string text, Color color, EChatMode mode, string iconURL, bool richText, SteamPlayer recipient)
        {
            try
            {
                ThreadUtil.assertIsGameThread();
            }
            catch
            {
                LogWarning("Tried to send a chat message on non-game thread.");
                return;
            }
            if (Data.SendChatIndividual == null)
            {
                ChatManager.serverSendMessage(text, color, null, recipient, mode, iconURL, richText);
                return;
            }
            if (!Provider.isServer)
            {
                LogWarning("Tried to send a chat message from client.");
                return;
            }
            if (recipient == null)
            {
                LogWarning("Tried to send a chat message to null recipient.");
                return;
            }
            try
            {
                try
                {
                    ChatManager.onServerSendingMessage?.Invoke(ref text, ref color, null, recipient, mode, ref iconURL, ref richText);
                }
                catch (Exception ex)
                {
                    LogError("Error invoking ChatManager.onServerSendingMessage event: ");
                    LogError(ex);
                }
                if (iconURL == null) iconURL = string.Empty;
                Data.SendChatIndividual.Invoke(ENetReliability.Reliable, recipient.transportConnection, CSteamID.Nil, iconURL, mode, color, richText, text);
            }
            catch
            {
                ChatManager.serverSendMessage(text, color, null, recipient, mode, iconURL, richText);
            }
        }
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
            DONT_DISPLAY,
            IN_VEHICLE,
            LOCKED
        }
        public static ulong GetTeamFromPlayerSteam64ID(this ulong s64)
        {
            if (!(Data.Gamemode is TeamGamemode))
            {
                SteamPlayer pl2 = PlayerTool.getSteamPlayer(s64);
                if (pl2 == null) return 0;
                else return pl2.player.quests.groupID.m_SteamID;
            }
            SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
            if (pl == default)
            {
                if (PlayerManager.HasSave(s64, out PlayerSave save))
                    return save.Team;
                else return 0;
            }
            else return pl.GetTeam();
        }
        public static ulong GetTeam(this UCPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this UnturnedPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this ulong groupID)
        {
            if (!(Data.Gamemode is TeamGamemode)) return groupID;
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
            if (!(Data.Gamemode is TeamGamemode)) return groupID > byte.MaxValue ? byte.MaxValue : (byte)groupID;
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, SendUIParameters p, SteamPlayer player, ITransportConnection connection, ulong translationID) =>
            UIOrChat(charactericon, useui, uiid, pts, progresschars, p.team, p.status, p.chatTranslation, p.chatColor, connection, player, p.points, translationID,
                p.sendChat, p.sendUI, p.absoluteCap, p.overrideChatConfig, p.formatting, p.team1count, p.team2count);
        public static void UIOrChat(char charactericon, bool useui, ushort uiid, bool pts, string progresschars, ulong team, EFlagStatus type, string translation_key, Color color, ITransportConnection PlayerConnection, SteamPlayer player,
            int c, ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true,
            bool absolute = true, bool sendChatOverride = false, string[] formatting = null, int team1count = 0, int team2count = 0)
        {
            if (type == EFlagStatus.DONT_DISPLAY)
            {
                if (useui && SendUIIfConfiged)
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
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1_words")}>{Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2_words")}>{Translate("ui_capturing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    default:
                    case EFlagStatus.BLANK:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(0, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(0, progresschars)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case EFlagStatus.IN_VEHICLE:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_1_words")}>{Translate("ui_in_vehicle", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("in_vehicle_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_2_words")}>{Translate("ui_in_vehicle", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("in_vehicle_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("in_vehicle_team_2_bkgr"));
                        break;
                    case EFlagStatus.LOSING:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1_words")}>{Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("losing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2_words")}>{Translate("ui_losing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("losing_team_2_bkgr"));
                        break;
                    case EFlagStatus.SECURED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1_words")}>{Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("secured_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2_words")}>{Translate("ui_secured", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("secured_team_2_bkgr"));
                        break;
                    case EFlagStatus.CONTESTED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1_words")}>{Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("contested_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2_words")}>{Translate("ui_contested", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("contested_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OBJECTIVE:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1_words")}>{Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("nocap_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2_words")}>{Translate("ui_nocap", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("nocap_team_2_bkgr"));
                        break;
                    case EFlagStatus.LOCKED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("locked_team_1_words")}>{Translate("ui_locked", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("locked_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("locked_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("locked_team_2_words")}>{Translate("ui_locked", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("locked_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("locked_team_2_bkgr"));
                        break;
                    case EFlagStatus.CLEARING:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1_words")}>{Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("clearing_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2_words")}>{Translate("ui_clearing", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("clearing_team_2_bkgr"));
                        break;
                    case EFlagStatus.NOT_OWNED:
                        if (team == 1)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_1_words")}>{Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("notowned_team_1_bkgr"));
                        else if (team == 2)
                            EffectManager.sendUIEffect(uiid, key, PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2_words")}>{Translate("ui_notowned", playerID)}{(pts ? $" ({circleAmount}/{Flag.MAX_POINTS})" : "")}</color>",
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{progresschars[CTFUI.FromMax(circleAmount, progresschars)]}</color>", UCWarfare.GetColorHex("notowned_team_2_bkgr"));
                        break;
                }
                if (team1count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_1")}>{team1count}</color>");
                }
                else
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1CountIcon", "");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T1Count", "");
                }
                if (team2count > 0 && UCWarfare.Config.FlagSettings.EnablePlayerCount)
                {
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2CountIcon", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2_icon")}>{charactericon}</color>");
                    EffectManager.sendUIEffectText(key, PlayerConnection, true, "T2Count", $"<color=#{UCWarfare.GetColorHex("team_count_ui_color_team_2")}>{team2count}</color>");
                }
                else
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
        public static Vector3 GetBaseSpawn(this SteamPlayer player, out ulong team) => player.player.GetBaseSpawn(out team);
        public static Vector3 GetBaseSpawn(this Player player)
        {
            if (!(Data.Gamemode is ITeams)) return TeamManager.LobbySpawn;
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
            if (!(Data.Gamemode is ITeams))
            {
                team = player.quests.groupID.m_SteamID;
                return TeamManager.LobbySpawn;
            }
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
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawn;
            }
            return team.GetBaseSpawnFromTeam();
        }
        public static Vector3 GetBaseSpawnFromTeam(this ulong team)
        {
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawn;
            }
            if (team == 1) return TeamManager.Team1Main.Center3D;
            else if (team == 2) return TeamManager.Team2Main.Center3D;
            else return TeamManager.LobbySpawn;
        }
        public static float GetBaseAngle(this ulong team)
        {
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawnAngle;
            }
            if (team == 1) return TeamManager.Team1SpawnAngle;
            else if (team == 2) return TeamManager.Team2SpawnAngle;
            else return TeamManager.LobbySpawnAngle;
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
                newtext = TranslateSign(text, client.playerID.steamID.m_SteamID, false);
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        public static void InvokeSignUpdateForAll(InteractableSign sign, byte x, byte y, string text)
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
                        newtext = TranslateSign(text, languageGroup.Value[0].playerID.steamID.m_SteamID, false);
                    List<ITransportConnection> toSendTo = new List<ITransportConnection>();
                    languageGroup.Value.ForEach(l => toSendTo.Add(l.transportConnection));
                    Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, toSendTo, newtext);
                }
            }
        }
        /// <summary>Runs one player at a time instead of one language at a time. Used for kit signs.</summary>
        public static void InvokeSignUpdateForAllKits(InteractableSign sign, byte x, byte y, string text)
        {
            if (text == null) return;
            IEnumerator<SteamPlayer> connections = EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            while (connections.MoveNext())
            {
                string newtext = text;
                if (text.StartsWith("sign_"))
                    newtext = TranslateSign(text, connections.Current.playerID.steamID.m_SteamID, false);
                Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, connections.Current.transportConnection, newtext);
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
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, bool changeText = false, string text = "")
        {
            if (text == default || client == default) return;
            string newtext;
            if (!changeText)
                newtext = sign.text;
            else newtext = text;
            if (newtext.StartsWith("sign_"))
                newtext = TranslateSign(newtext ?? "", client.playerID.steamID.m_SteamID, false);
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        public static float GetTerrainHeightAt2DPoint(Vector2 position, float above = 0) => GetTerrainHeightAt2DPoint(position.x, position.y, above: above);
        public static float GetTerrainHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            return LevelGround.getHeight(new Vector3(x, 0, z));
            /*
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.GROUND | RayMasks.GROUND2))
                return h.point.y + above;
            else return defaultY; */
        }
        public static float GetHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
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
                if (thickness > 1)
                {
                    float distance = thickness / 2f;
                    for (float i = -distance; i <= distance; i += 0.5f)
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
                if (p == null)
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
                if (p == default || p.player == default)
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
                if (pl == default)
                    return Data.DatabaseManager.GetUsernames(player);
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
            if (!(Data.Gamemode is TeamGamemode)) return false;
            ulong team = player.GetTeam();
            if (team == 1) return TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsInMain(Vector3 point)
        {
            if (!(Data.Gamemode is TeamGamemode)) return false;
            return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
        }
        public static bool IsOnFlag(this Player player) => player != null && Data.Is(out IFlagRotation fg) && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
        public static bool IsOnFlag(this Player player, out Flag flag)
        {
            if (player != null && Data.Is(out IFlagRotation fg))
            {
                if (fg.OnFlag == null)
                {
                    F.LogError("onflag null");
                    if (fg.Rotation == null) F.LogError("rot null");
                    flag = null;
                    return false;
                }
                else if (fg.Rotation == null)
                {
                    F.LogError("rot null");
                    if (fg.OnFlag == null) F.LogError("onflag null");
                    flag = null;
                    return false;
                }
                if (fg.OnFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int id))
                {
                    flag = fg.Rotation.Find(x => x.ID == id);
                    return flag != null;
                }
            }
            flag = null;
            return false;
        }
        public static string Colorize(this string inner, string colorhex) => $"<color=#{colorhex}>{inner}</color>";
        public static string TranslateSign(string key, ulong player, bool important)
        {
            try
            {
                if (key == null) return string.Empty;
                string norm = Translate(key, player);
                if (!key.StartsWith("sign_") || norm != key) return norm;
                string kitname = key.Substring(5);
                if (kitname.StartsWith("vbs_") && ushort.TryParse(kitname.Substring(4), System.Globalization.NumberStyles.Any, Data.Locale, out ushort vehicleid))
                {
                    if (Vehicles.VehicleBay.VehicleExists(vehicleid, out Vehicles.VehicleData data))
                    {
                        VehicleAsset asset = UCAssetManager.FindVehicleAsset(vehicleid);
                        if (asset == default) return norm;
                        if (data.RequiredLevel > 0)
                        {
                            UCPlayer ucplayer = UCPlayer.FromID(player);
                            Rank playerrank = null;
                            if (ucplayer != null)
                            {
                                playerrank = ucplayer.XPRank();
                            }
                            Rank rank = data.RequiredRank;
                            if (rank == default) return norm;
                            string level;
                            if (player != 0 && rank != null && rank.level > playerrank.level)
                            {
                                level = Translate("kit_required_level", player, data.RequiredLevel.ToString(Data.Locale),
                                    UCWarfare.GetColorHex("kit_level_unavailable"),
                                rank.TranslateAbbreviation(player), UCWarfare.GetColorHex("kit_level_unavailable_abbr"));
                            }
                            else if (rank != null)
                            {
                                level = Translate("kit_required_level", player, data.RequiredLevel.ToString(Data.Locale),
                                    UCWarfare.GetColorHex("kit_level_available"),
                                rank.TranslateAbbreviation(player), UCWarfare.GetColorHex("kit_level_available_abbr"));
                            }
                            else
                            {
                                level = "\n";
                            }
                            return Translate("vehiclebay_sign_min_level", player, asset.vehicleName, UCWarfare.GetColorHex("vbs_vehicle_name_color"),
                                level, data.TicketCost.ToString(Data.Locale), UCWarfare.GetColorHex("vbs_ticket_cost"), UCWarfare.GetColorHex("vbs_background"));
                        }
                        else
                        {
                            return Translate("vehiclebay_sign_no_min_level", player, asset.vehicleName, UCWarfare.GetColorHex("vbs_vehicle_name_color"), data.TicketCost.ToString(Data.Locale),
                                UCWarfare.GetColorHex("vbs_ticket_cost"), UCWarfare.GetColorHex("vbs_background"));
                        }
                    }
                    else return norm;
                }
                else if (kitname.StartsWith("loadout_") && ushort.TryParse(kitname.Substring(8), System.Globalization.NumberStyles.Any, Data.Locale, out ushort loadoutid))
                {
                    UCPlayer ucplayer = UCPlayer.FromID(player);
                    if (ucplayer != null)
                    {
                        ulong team = ucplayer.GetTeam();
                        List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && k.AllowedUsers.Contains(player)).ToList();

                        if (loadouts.Count > 0)
                        {
                            if (loadoutid > 0 && loadoutid <= loadouts.Count)
                            {
                                Kit kit = loadouts[loadoutid - 1];

                                string lang = DecideLanguage(player, kit.SignTexts);
                                if (!kit.SignTexts.TryGetValue(lang, out string name))
                                    name = kit.DisplayName ?? kit.Name;
                                bool keepline = false;
                                foreach (char @char in name)
                                {
                                    if (@char == '\n')
                                    {
                                        keepline = true;
                                        break;
                                    }
                                }
                                string cost = Translate("loadout_name_owned", player, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
                                if (!keepline) cost = "\n" + cost;

                                string playercount = "";

                                if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                                {
                                    playercount = Translate("kit_unlimited", player).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
                                }
                                else if (kit.IsClassLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : team, true))
                                {
                                    playercount = Translate("kit_player_count", player, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                                        .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
                                }
                                else
                                {
                                    playercount = Translate("kit_player_count", player, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                                        .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
                                }

                                return Translate("sign_kit_request", player,
                                    name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")),
                                    cost,
                                    kit.Weapons == "" ? " " : Translate("kit_weapons", player, kit.Weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list"))),
                                    playercount
                                    );
                            }
                        }
                    }

                    return Translate("sign_kit_request", player,
                                Translate("loadout_name", player, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_public_header")),
                                string.Empty,
                                ObjectTranslate("kit_price_dollars", player, UCWarfare.Config.LoadoutCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars")),
                                string.Empty
                                );
                }
                else if (KitManager.KitExists(kitname, out Kit kit))
                {
                    UCPlayer ucplayer = UCPlayer.FromID(player);
                    ulong playerteam = 0;
                    Rank playerrank = null;
                    if (ucplayer != null)
                    {
                        playerteam = ucplayer.GetTeam();
                        playerrank = ucplayer.XPRank();
                    }
                    string lang = DecideLanguage(player, kit.SignTexts);
                    if (!kit.SignTexts.TryGetValue(lang, out string name))
                        name = kit.DisplayName ?? kit.Name;
                    bool keepline = false;
                    foreach (char @char in name)
                    {
                        if (@char == '\n')
                        {
                            keepline = true;
                            break;
                        }
                    }
                    name = Translate("kit_name", player, name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")));
                    string weapons = kit.Weapons ?? string.Empty;
                    if (weapons != string.Empty)
                        weapons = Translate("kit_weapons", player, weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list")));
                    string cost;
                    string playercount;
                    if (kit.IsPremium && (kit.PremiumCost > 0 || kit.PremiumCost == -1))
                    {
                        if (kit.AllowedUsers.Contains(player))
                            cost = ObjectTranslate("kit_owned", player).Colorize(UCWarfare.GetColorHex("kit_level_dollars_owned"));
                        else if (kit.PremiumCost == -1)
                            cost = Translate("kit_price_exclusive", player).Colorize(UCWarfare.GetColorHex("kit_level_dollars_exclusive"));
                        else
                            cost = ObjectTranslate("kit_price_dollars", player, kit.PremiumCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
                    }
                    else if (kit.RequiredLevel > 0)
                    {
                        Rank reqrank = kit.RequiredRank();
                        if (playerrank == null || playerrank.level < reqrank.level)
                        {
                            cost = Translate("kit_required_level", player, kit.RequiredLevel.ToString(), UCWarfare.GetColorHex("kit_level_unavailable"),
                                reqrank.TranslateAbbreviation(player), UCWarfare.GetColorHex("kit_level_unavailable_abbr"));
                        }
                        else
                        {
                            cost = Translate("kit_required_level", player, kit.RequiredLevel.ToString(), UCWarfare.GetColorHex("kit_level_available"),
                                reqrank.TranslateAbbreviation(player), UCWarfare.GetColorHex("kit_level_available_abbr"));
                        }
                    }
                    else
                    {
                        cost = string.Empty;
                    }
                    if (!keepline) cost = "\n" + cost;
                    if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                    {
                        playercount = Translate("kit_unlimited", player).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
                    }
                    else if (kit.IsLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : playerteam, true))
                    {
                        playercount = Translate("kit_player_count", player, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
                    }
                    else
                    {
                        playercount = Translate("kit_player_count", player, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
                    }
                    return Translate("sign_kit_request", player, name, cost, weapons, playercount);
                }
                else return key;
            }
            catch (Exception ex)
            {
                LogError("Error translating sign: ");
                LogError(ex);
                return ex.GetType().Name;
            }
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
                if (!Data.LimbLocalization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<ELimb, string> loc))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        loc = Data.LimbLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(limb, out string v))
                        {
                            return v;
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else
                {
                    if (loc.TryGetValue(limb, out string v))
                    {
                        return v;
                    }
                    else return limb.ToString();
                }
            }
            else
            {
                if (!Data.Languages.TryGetValue(player, out string lang) || !Data.LimbLocalization.TryGetValue(lang, out Dictionary<ELimb, string> loc) || !loc.ContainsKey(limb))
                {
                    lang = JSONMethods.DefaultLanguage;
                }
                if (!Data.LimbLocalization.TryGetValue(lang, out loc))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        loc = Data.LimbLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(limb, out string v))
                        {
                            return v;
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else if (loc.TryGetValue(limb, out string v))
                {
                    return v;
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
                if (!Data.DeathLocalization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, string> loc))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        loc = Data.DeathLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(key, out string v))
                        {
                            try
                            {
                                return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else if (loc.TryGetValue(backupcause.ToString(), out v))
                        {
                            try
                            {
                                return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
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
                    if (loc.TryGetValue(key, out string v))
                    {
                        try
                        {
                            return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                        }
                        catch (FormatException ex)
                        {
                            LogError(ex);
                            return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                        }
                    }
                    else if (loc.TryGetValue(backupcause.ToString(), out v))
                    {
                        try
                        {
                            return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
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
                if (!Data.Languages.TryGetValue(player, out string lang) || !Data.DeathLocalization.TryGetValue(lang, out Dictionary<string, string> loc) || (!loc.ContainsKey(key) && !loc.ContainsKey(backupcause.ToString())))
                    lang = JSONMethods.DefaultLanguage;
                if (!Data.DeathLocalization.TryGetValue(lang, out loc))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        loc = Data.DeathLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(key, out string v))
                        {
                            try
                            {
                                return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                            }
                            catch (FormatException ex)
                            {
                                LogError(ex);
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                            }
                        }
                        else if (loc.TryGetValue(backupcause.ToString(), out v))
                        {
                            try
                            {
                                return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
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
                else if (loc.TryGetValue(key, out string v))
                {
                    try
                    {
                        return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
                    }
                    catch (FormatException ex)
                    {
                        LogError(ex);
                        return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                }
                else if (loc.TryGetValue(backupcause.ToString(), out v))
                {
                    try
                    {
                        return string.Format(v, deadname, murderername, TranslateLimb(player, limb), itemName, dis);
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
                if (!Data.DeathLocalization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, string> loc))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        loc = Data.DeathLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(key, out string t))
                        {
                            try
                            {
                                return string.Format(t, deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
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
                    if (loc.TryGetValue(key, out string t))
                    {
                        try
                        {
                            return string.Format(t, deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
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
                if (!Data.Languages.TryGetValue(player, out string lang) || !Data.DeathLocalization.TryGetValue(lang, out Dictionary<string, string> loc) || (!loc.ContainsKey(key) && !loc.ContainsKey("LANDMINE")))
                    lang = JSONMethods.DefaultLanguage;
                if (!Data.DeathLocalization.TryGetValue(lang, out loc))
                {
                    if (Data.DeathLocalization.Count > 0)
                    {
                        loc = Data.DeathLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(key, out string t))
                        {
                            try
                            {
                                return string.Format(t, deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
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
                else if (loc.TryGetValue(key, out string t))
                {
                    try
                    {
                        return string.Format(t, deadname, murderername, TranslateLimb(player, limb), landmineName, "0", triggerername);
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
            if (!(Data.Gamemode is TeamGamemode)) return innerText;
            if (team == TeamManager.ZOMBIE_TEAM_ID) return $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>";
            else if (team == TeamManager.Team1ID) return $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>";
            else if (team == TeamManager.Team2ID) return $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>";
            else if (team == TeamManager.AdminID) return $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>";
            else return $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>";
        }
        /// <param name="backupcause">Used in case the key can not be found.</param>
        public static void BroadcastDeath(string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, bool translateKillerName, ulong killerTeam, ELimb limb, string itemName, float distance, out string message, bool broadcast = true)
        {
            if (broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string killer = translateKillerName ? Translate(killerName.CharacterName, player) : killerName.CharacterName;
                    string localizedString = TranslateDeath(player.playerID.steamID.m_SteamID, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, false, translateKillerName);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString,
                            UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 ? "death_background_teamkill" : "death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string newMessage;
                        if (!JSONMethods.DefaultDeathTranslations.TryGetValue(key, out string defaultMessage) && !JSONMethods.DefaultDeathTranslations.TryGetValue(backupcause.ToString(), out defaultMessage))
                            defaultMessage = key;
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
                            ChatManager.say(player.playerID.steamID, newMessage,
                                UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 && dead.Steam64 != killerName.Steam64 ? "death_background_teamkill" : "death_background"), newMessage.Contains("</"));
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
            if (broadcast)
            {
                foreach (SteamPlayer player in Provider.clients)
                {
                    string localizedString = TranslateLandmineDeath(player.playerID.steamID.m_SteamID, key, dead, deadTeam, killerName, killerTeam, triggererName, triggererTeam, limb, landmineName, false);
                    if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                    {
                        ChatManager.say(player.playerID.steamID, localizedString,
                            UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 ? "death_background_teamkill" : "death_background"), localizedString.Contains("</"));
                    }
                    else
                    {
                        LogWarning($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {key}.");
                        string newMessage;
                        if (!JSONMethods.DefaultDeathTranslations.TryGetValue(key, out string defaultMessage))
                            defaultMessage = key;
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
                            ChatManager.say(player.playerID.steamID, newMessage,
                                UCWarfare.GetColor(deadTeam == killerTeam && deadTeam != 0 && dead.Steam64 != killerName.Steam64 ? "death_background_teamkill" : "death_background"), newMessage.Contains("</"));
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
                    if (unloadIfFail)
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
            }
            catch (Exception ex)
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
            string message = $"EXCEPTION - {ex.GetType().Name}\n\n{ex.Message}\n{ex.StackTrace}\n\nFINISHED";
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
            IEnumerable<Kit> kits = KitManager.GetKitsWhere(x => x.Name == kitname);
            if (kits.Count() > 0) return kits.ElementAt(0).DisplayName;
            else return kitname;
        }
        public static float GetSqrDistanceFromClosestObjective(Vector3 position, out Flag objective, bool includeOutOfRotation = false)
        {
            if (Data.Is(out IFlagRotation fg))
            {
                Vector2 position2d = new Vector2(position.x, position.z);
                Flag closestFlag = default;
                float? closestSqrDistance = default;
                foreach (Flag flag in includeOutOfRotation ? fg.LoadedFlags : fg.Rotation)
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
            }
            else
            {
                objective = null;
                return float.NaN;
            }
        }
        public static bool Between<T>(this T number, T highBound, T lowBound, bool inclusiveHigh = true, bool inclusiveLow = false) where T : IComparable
        {
            int high = number.CompareTo(highBound);
            int low = number.CompareTo(lowBound);
            return (low == 1 || (inclusiveLow && low == 0)) && (high == -1 || (inclusiveHigh && high == 0));
        }
        public static void SendSteamURL(this SteamPlayer player, string message, ulong SteamID) => player.SendURL(message, $"https://steamcommunity.com/profiles/{SteamID}/");
        public static void SendURL(this SteamPlayer player, string message, string url)
        {
            if (player == default || url == default) return;
            player.player.sendBrowserRequest(message, url);
        }
        public static SDG.Unturned.BarricadeData GetBarricadeFromInstID(uint instanceID, out BarricadeDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.drops[i].GetServersideData();
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (region.drops[i].instanceID == instanceID)
                    {
                        drop = region.drops[i];
                        return region.drops[i].GetServersideData();
                    }
                }
            }
            drop = default;
            return default;
        }
        public static BarricadeDrop GetBarricadeFromInstID(uint instanceID)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            for (int vr = 0; vr < BarricadeManager.vehicleRegions.Count; vr++)
            {
                VehicleBarricadeRegion region = BarricadeManager.vehicleRegions[vr];
                for (int i = 0; i < region.drops.Count; i++)
                {
                    if (region.drops[i].instanceID == instanceID)
                    {
                        return region.drops[i];
                    }
                }
            }
            return default;
        }
        public static SDG.Unturned.StructureData GetStructureFromInstID(uint instanceID, out StructureDrop drop)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            drop = region.drops[i];
                            return region.drops[i].GetServersideData();
                        }
                    }
                }
            }
            drop = default;
            return default;
        }
        public static StructureDrop GetStructureFromInstID(uint instanceID)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (region.drops[i].GetServersideData().instanceID == instanceID)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            return default;
        }
        public static BarricadeDrop GetBarriadeBySerializedTransform(SerializableTransform t)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (t == region.drops[i].model)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            return null;
        }
        public static StructureDrop GetStructureBySerializedTransform(SerializableTransform t)
        {
            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];
                    if (region == default) continue;
                    for (int i = 0; i < region.drops.Count; i++)
                    {
                        if (t == region.drops[i].model)
                        {
                            return region.drops[i];
                        }
                    }
                }
            }
            return null;
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
        public static string TranslateBranch(EBranch branch, ulong player)
        {
            string branchName = "team";
            ulong team = GetTeamFromPlayerSteam64ID(player);
            if (team == 1)
                branchName += "1_";
            else if (team == 2)
                branchName += "2_";
            return Translate(branchName + branch.ToString().ToLower(), player, out _);
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
        public static bool CanStandAtLocation(Vector3 source)
        {
            return Physics.OverlapCapsuleNonAlloc(source + new Vector3(0.0f, PlayerStance.RADIUS + 0.01f, 0.0f), source +
                new Vector3(0.0f, PlayerMovement.HEIGHT_STAND + 0.5f - PlayerStance.RADIUS, 0.0f), PlayerStance.RADIUS, PlayerStance.checkColliders,
                RayMasks.BLOCK_STANCE, QueryTriggerInteraction.Ignore) == 0;
        }
        public static void NetInvoke(this NetCall call) =>
            call.Invoke(Data.NetClient.connection);
        public static void NetInvoke<T>(this NetCallRaw<T> call, T arg) =>
            call.Invoke(Data.NetClient.connection, arg);
        public static void NetInvoke<T1, T2>(this NetCallRaw<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCallRaw<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCallRaw<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1>(this NetCall<T1> call, T1 arg1) =>
            call.Invoke(Data.NetClient.connection, arg1);
        public static void NetInvoke<T1, T2>(this NetCall<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCall<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCall<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1, T2, T3, T4, T5>(this NetCall<T1, T2, T3, T4, T5> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6>(this NetCall<T1, T2, T3, T4, T5, T6> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7>(this NetCall<T1, T2, T3, T4, T5, T6, T7> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        public static bool FilterName(string original, out string final)
        {
            if (UCWarfare.Config.DisableNameFilter || UCWarfare.Config.MinAlphanumericStringLength <= 0)
            {
                final = original;
                return false;
            }
            IEnumerator<char> charenum = original.GetEnumerator();
            int counter = 0;
            int alphanumcount = 0;
            while (charenum.MoveNext())
            {
                counter++;
                char ch = charenum.Current;
                int c = ch;
                if (c > 32 && c < 127)
                {
                    if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                    {
                        final = original;
                        charenum.Dispose();
                        return false;
                    }
                    else
                    {
                        alphanumcount++;
                    }
                }
                else
                {
                    alphanumcount = 0;
                }
            }
            charenum.Dispose();
            final = original;
            return alphanumcount != original.Length;
        }
    }
}