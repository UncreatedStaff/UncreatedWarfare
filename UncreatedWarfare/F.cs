using Newtonsoft.Json;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UncreatedWarfare.Flags;
using UnityEngine;
using static Rocket.Core.Logging.Logger;

namespace UncreatedWarfare
{
    public static class F
    {
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
            else ColorUtility.TryParseHtmlString(htmlColorCode, out color);
            return color;
        }
        public static string MakeRemainder(this string[] array, int startIndex = 0, int length = -1, string deliminator = " ")
        {
            string temp = string.Empty;
            for (int i = startIndex; i < (length == -1 ? array.Length : length); i++)
                temp += (i == startIndex ? "" : deliminator) + array[i];
            return temp;
        }
        public static string GetTime(this uint minutes)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString() + " minute" + (minutes == 1 ? "" : "s");
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} hour{(hours == 1 ? "" : "s")}{(minutesOverflow == 0 ? "" : $" and {minutesOverflow} minute{(minutesOverflow == 1 ? "" : "s")}")}";
            }
            else if (minutes < 43800) // < 1 month (30.4166667 days)
            {
                uint days = DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow);
                return $"{days} day{(days == 1 ? "" : "s")}{(hoursOverflow == 0 ? "" : $" and {hoursOverflow} hour{(hoursOverflow == 1 ? "" : "s")}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow), 30.4166667m, out uint daysOverflow);
                return $"{months} month{(months == 1 ? "" : "s")}{(daysOverflow == 0 ? "" : $" and {daysOverflow} day{(daysOverflow == 1 ? "" : "s")}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow), 30.4166667m, out uint daysOverflow), 12, out uint monthOverflow);
                return $"{years} year{(years == 1 ? "" : "s")}{(monthOverflow == 0 ? "" : $" and {monthOverflow} month{(monthOverflow == 1 ? "" : "s")}")}";
            }
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
        /// <summary>
        /// Tramslate an unlocalized string to a localized string using the Rocket translations file.
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, params object[] formatting)
        {
            if (player == 0)
            {
                if (!UCWarfare.I.Localization.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (UCWarfare.I.Localization.Count > 0)
                    {
                        if (UCWarfare.I.Localization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(UCWarfare.I.Localization.ElementAt(0).Value[key], formatting);
                            }
                            catch (FormatException)
                            {
                                return UCWarfare.I.Localization.ElementAt(0).Value[key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        } else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    } else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                } else
                {
                    if (UCWarfare.I.Localization[JSONMethods.DefaultLanguage].ContainsKey(key))
                    {
                        try
                        {
                            return string.Format(UCWarfare.I.Localization[JSONMethods.DefaultLanguage][key], formatting);
                        }
                        catch (FormatException)
                        {
                            return UCWarfare.I.Localization[JSONMethods.DefaultLanguage][key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    } else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else 
            {
                string lang = JSONMethods.DefaultLanguage;
                if (UCWarfare.I.Languages.ContainsKey(player))
                {
                    lang = UCWarfare.I.Languages[player];
                    if (!UCWarfare.I.Localization.ContainsKey(lang))
                        lang = JSONMethods.DefaultLanguage;
                }
                if (!UCWarfare.I.Localization.ContainsKey(lang))
                {
                    if (UCWarfare.I.Localization.Count > 0)
                    {
                        if (UCWarfare.I.Localization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(UCWarfare.I.Localization.ElementAt(0).Value[key], formatting);
                            }
                            catch (FormatException)
                            {
                                return UCWarfare.I.Localization.ElementAt(0).Value[key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                    else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                } else if (UCWarfare.I.Localization[lang].ContainsKey(key))
                {
                    try
                    {
                        return string.Format(UCWarfare.I.Localization[lang][key], formatting);
                    } catch (FormatException)
                    {
                        return UCWarfare.I.Localization[lang][key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                } else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, Color textColor, params object[] formatting) => SendChat(player.CSteamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="Player"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this Player player, string text, Color textColor, params object[] formatting) => SendChat(player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="SteamPlayer"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, Color textColor, params object[] formatting) => SendChat(player.player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Max amount of bytes that can be sent in an Unturned Chat Message.
        /// </summary>
        const int MaxChatSizeAmount = 2047;
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="CSteamID"/> to send the chat to.</param>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, Color textColor, params object[] formatting)
        {
            string localizedString = Translate(text, player.m_SteamID, formatting);
            bool isRich = false;
            if (localizedString.Contains("</"))
                isRich = true;
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                ChatManager.say(player, localizedString, textColor, isRich);
            else
            {
                Log($"'{localizedString}' is too long, sending default message instead, consider shortening your translation of {text}.");
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
                    CommandWindow.LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
                if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                    ChatManager.say(player, newMessage, textColor, isRich);
                else
                    CommandWindow.LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
                        + MaxChatSizeAmount.ToString() + " bytes in UTF-8. Arguments may be too long.");
            }
        }
        /// <summary>
        /// Send a white message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player"><see cref="UnturnedPlayer"/> to send the chat to.</param>
        /// <param name="message"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="formatting">Params array of strings to replace the {#}s in the translations.</param>
        public static void Message(this UnturnedPlayer player, string message, params object[] formatting) => SendChat(player, message, Color.white, formatting);
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void Broadcast(string text, Color textColor, params object[] formatting)
        {
            foreach(SteamPlayer player in Provider.clients)
                SendChat(player.playerID.steamID, text, textColor, formatting);
        }
        /// <summary>
        /// Send a message in chat to everyone except for those in the list of excluded <see cref="CSteamID"/>s.
        /// </summary>
        /// <param name="text"><para>The unlocalized <see cref="string"/> to match with the translation dictionary.</para><para>After localization, the chat message can only be &lt;= 2047 bytes, encoded in UTF-8 format.</para></param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {#}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, Color textColor, params object[] formatting)
        {
            foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                SendChat(player.playerID.steamID, text, textColor, formatting);
        }
        public static bool OnDuty(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool OffDuty(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup);
        public static bool IsIntern(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup);
        public static bool IsAdmin(this UnturnedPlayer player) => R.Permissions.GetGroups(player, false).Exists(x => x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || x.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup);
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
        public static string S<T>(this T number) where T : IComparable => number.CompareTo(1) == 0 ? "" : "s";
        public enum UIOption
        {
            Capturing,
            Losing,
            Secured,
            Contested,
            NoCap,
            Clearing,
            Blank,
            NotOwned
        }
        public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this UnturnedPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this ulong groupID)
        {
            if (groupID == UCWarfare.Config.Team1ID) return 1;
            else if (groupID == UCWarfare.Config.Team2ID) return 2;
            else return 0;
        }
        public static void UIOrChat(ulong team, UIOption type, string translation_key, Color color, SteamPlayer player, int circleAmount, ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true, bool absolute = true, bool sendChatOverride = false, object[] formatting = null)
            => UIOrChat(team, type, translation_key, color, Provider.findTransportConnection(player.playerID.steamID), player, circleAmount, playerID, SendChatIfConfiged, SendUIIfConfiged, absolute, sendChatOverride, formatting);
        public static void UIOrChat(ulong team, UIOption type, string translation_key, Color color, ITransportConnection PlayerConnection, SteamPlayer player, int c, ulong playerID = 0, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true,
            bool absolute = true, bool sendChatOverride = false, object[] formatting = null)
        {
            int circleAmount = absolute ? Math.Abs(c) : c;
            if (UCWarfare.Config.FlagSettings.UseUI && SendUIIfConfiged)
            {
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, PlayerConnection);
                switch (type)
                {
                    case UIOption.Capturing:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1_words")}>{Translate("ui_capturing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2_words")}>{Translate("ui_capturing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case UIOption.Blank:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"", 
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"", 
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case UIOption.Losing:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1_words")}>{Translate("ui_losing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("losing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2_words")}>{Translate("ui_losing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("losing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_2_bkgr"));
                        break;
                    case UIOption.Secured:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1_words")}>{Translate("ui_secured", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("secured_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2_words")}>{Translate("ui_secured", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("secured_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_2_bkgr"));
                        break;
                    case UIOption.Contested:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1_words")}>{Translate("ui_contested", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("contested_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2_words")}>{Translate("ui_contested", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("contested_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_2_bkgr"));
                        break;
                    case UIOption.NoCap:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1_words")}>{Translate("ui_nocap", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("nocap_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2_words")}>{Translate("ui_nocap", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("nocap_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_2_bkgr"));
                        break;
                    case UIOption.Clearing:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1_words")}>{Translate("ui_clearing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("clearing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, 
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2_words")}>{Translate("ui_clearing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("clearing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_2_bkgr"));
                        break;
                    case UIOption.NotOwned:
                        if (team == UCWarfare.I.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_1_words")}>{Translate("ui_notowned", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_1_bkgr"));
                        else if (team == UCWarfare.I.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2_words")}>{Translate("ui_notowned", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_2_bkgr"));
                        break;
                }
            }
            if (sendChatOverride || (UCWarfare.Config.FlagSettings.UseChat && SendChatIfConfiged))
            {
                if(formatting == null)
                    player.SendChat(translation_key, color);
                else
                    player.SendChat(translation_key, color, formatting);
            }
        }
        public static string EncodeURIComponent(this string input) => Uri.EscapeUriString(input);
        public static string ShortenName(this string input)
        {
            return input;
        }
        public static Vector3 GetBaseSpawn(this SteamPlayer player)
        {
            ulong team = player.GetTeam();
            if (team == 1) return UCWarfare.I.TeamManager.Team1.Main.GetPosition();
            else if (team == 2) return UCWarfare.I.TeamManager.Team2.Main.GetPosition();
            else if (team == 3) return UCWarfare.I.TeamManager.Neutral.Main.GetPosition();
            else return UCWarfare.I.ExtraPoints["lobby_spawn"];
        }
        public static string QuickSerialize(object obj) => JsonConvert.SerializeObject(obj);
        public static T QuickDeserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
        public static void InvokeSignUpdateFor(SteamPlayer client, byte x, byte y, ushort plant, ushort index, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
                newtext = Translate(text, client.playerID.steamID.m_SteamID);
            CommandWindow.LogWarning("Invoking sign update for " + client.playerID.playerName + "\n" + text + " -> " + newtext);
            UCWarfare.SendUpdateSign.Invoke(ENetReliability.Unreliable, client.transportConnection, x, y, plant, index, newtext);
        }
        public static void InvokeSignUpdateForAll(byte x, byte y, ushort plant, ushort index, string text, BarricadeRegion region)
        {
            Dictionary<string, List<SteamPlayer>> playergroups = new Dictionary<string, List<SteamPlayer>>();
            foreach(SteamPlayer client in Provider.clients)
            {
                if(UCWarfare.I.Languages.ContainsKey(client.playerID.steamID.m_SteamID))
                {
                    if (playergroups.ContainsKey(UCWarfare.I.Languages[client.playerID.steamID.m_SteamID]))
                        playergroups[UCWarfare.I.Languages[client.playerID.steamID.m_SteamID]].Add(client);
                    else
                        playergroups.Add(UCWarfare.I.Languages[client.playerID.steamID.m_SteamID], new List<SteamPlayer> { client });
                } else
                {
                    if (playergroups.ContainsKey(JSONMethods.DefaultLanguage))
                        playergroups[JSONMethods.DefaultLanguage].Add(client);
                    else
                        playergroups.Add(JSONMethods.DefaultLanguage, new List<SteamPlayer> { client });
                }
            }
            CommandWindow.LogWarning("Invoking sign update for " + Provider.clients.Count + " player" + S(Provider.clients.Count) + ": \n" + text);
            foreach(KeyValuePair<string, List<SteamPlayer>> languageGroup in playergroups)
            {
                if(languageGroup.Value.Count > 0)
                {
                    string newtext = text;
                    if (text.StartsWith("sign_"))
                        newtext = Translate(text, languageGroup.Value[0].playerID.steamID.m_SteamID);
                    List<ITransportConnection> connections = new List<ITransportConnection>();
                    languageGroup.Value.ForEach(l => connections.Add(l.transportConnection));
                    CommandWindow.LogWarning("Invoking sign update for " + languageGroup.Value.Count.ToString() + " players in language " + languageGroup.Key + "\n" + text + " -> " + newtext);
                    UCWarfare.SendUpdateSign.Invoke(ENetReliability.Unreliable, connections, x, y, plant, index, newtext);
                    CommandWindow.LogWarning("looped back: " + languageGroup.Key);
                }
            }
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, byte x, byte y, ushort plant, ushort index, BarricadeRegion region, bool changeText = false, string text = "")
        {
            string newtext;
            string oldtext = text;
            if (!changeText)
            {
                newtext = GetSignText(index, region);
                oldtext = newtext;
            }
            else newtext = text;
            CommandWindow.LogWarning("Invoking sign update for " + client.playerID.playerName + "\n" + newtext);
            if (newtext.StartsWith("sign_"))
                newtext = Translate(newtext ?? "", client.playerID.steamID.m_SteamID);
            CommandWindow.LogWarning("Translated \"" + (changeText ? text : oldtext) + "\" -> \"" + newtext + '\"');
            UCWarfare.SendUpdateSign.Invoke(ENetReliability.Unreliable, client.transportConnection, x, y, plant, index, newtext);
        }
        public static string GetSignText(ushort index, BarricadeRegion region)
        {
            if (region.drops[index].model.TryGetComponent(out InteractableSign sign))
                return sign.text;
            else return string.Empty;
        }
        public static string GetSignText(Transform transform)
        {
            if (BarricadeManager.tryGetInfo(transform, out byte _, out byte _, out ushort _, out ushort index, out BarricadeRegion region))
                return GetSignText(index, region);
            else return string.Empty;
        }
        public static bool IsSign(this BarricadeDrop barricade) => barricade.model.TryGetComponent(out InteractableSign _);
        public static bool IsSign(this BarricadeData barricade, BarricadeRegion region)
        {
            int index = region.barricades.FindIndex(b => b.instanceID == barricade.instanceID);
            if (index < region.drops.Count)
            {
                BarricadeDrop drop = region.drops[index];
                if (drop.interactable.GetType() == typeof(InteractableSign)) return true;
                else return false;
            }
            else return false;
        }
    }
}
