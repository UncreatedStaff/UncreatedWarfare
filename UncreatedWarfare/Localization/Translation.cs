using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.XP;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Translation
    {
        public static string ObjectTranslate(string key, ulong player, params object[] formatting)
        {
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
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage"/>.</param>
        /// <returns>A translation structure.</returns>
        public static TranslationData GetTranslation(string key, ulong player)
        {
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
                L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
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
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, out Color color, params string[] formatting)
        {
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
        
        public static string GetTimeFromSeconds(this uint seconds, ulong player)
        {
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
                uint days = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (seconds < 31536000) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
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
                uint hours = F.DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
            }
            else if (minutes < 43800) // < 1 month (30.416 days)
            {
                uint days = F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), player)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
            }
        }
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
                    if (!(Assets.find(EAssetType.VEHICLE, vehicleid) is VehicleAsset va)) return key;
                    if (Vehicles.VehicleBay.VehicleExists(va.GUID, out Vehicles.VehicleData data))
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
                L.LogError("Error translating sign: ");
                L.LogError(ex);
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
            if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
            string murderername = translateKillerName ? Translate(killerName.PlayerName, player) : (usePlayerName ? killerName.PlayerName : killerName.CharacterName);
            if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
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
                                L.LogError(ex);
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
                                L.LogError(ex);
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
                            L.LogError(ex);
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
                            L.LogError(ex);
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
                                L.LogError(ex);
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
                                L.LogError(ex);
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
                        L.LogError(ex);
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
                        L.LogError(ex);
                        return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
                    }
                }
                else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString(Data.Locale) + "m"}";
            }
        }
        public static string TranslateLandmineDeath(ulong player, string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, bool usePlayerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
            string murderername = usePlayerName ? killerName.PlayerName : killerName.CharacterName;
            if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
            string triggerername = usePlayerName ? triggererName.PlayerName : triggererName.CharacterName;
            if (colorize) triggerername = F.ColorizeName(triggerername, triggererTeam);
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
                                L.LogError(ex);
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
                            L.LogError(ex);
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
                                L.LogError(ex);
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
                        L.LogError(ex);
                        return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
                    }
                }
                else return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
            }
        }
        public static string TranslateBranch(EBranch branch, UCPlayer player)
        {
            string branchName = "team";
            ulong team = player.GetTeam();
            if (team == 1)
                branchName += "1_";
            else if (team == 2)
                branchName += "2_";

            return Translate(branchName + branch.ToString().ToLower(), player.Steam64, out _);
        }
        public static string TranslateBranch(EBranch branch, ulong player)
        {
            string branchName = "team";
            ulong team = F.GetTeamFromPlayerSteam64ID(player);
            if (team == 1)
                branchName += "1_";
            else if (team == 2)
                branchName += "2_";
            return Translate(branchName + branch.ToString().ToLower(), player, out _);
        }
    }
}
