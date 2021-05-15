using Newtonsoft.Json;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UnityEngine;
using static Rocket.Core.Logging.Logger;
using Color = UnityEngine.Color;

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
        public static string GetTimeFromSeconds(this uint seconds)
        {
            if (seconds < 60) // < 1 minute
            {
                return seconds.ToString() + " seconds" + seconds.S();
            } else if (seconds < 3600) // < 1 hour
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
                return minutes.ToString() + " minute" + (minutes == 1 ? "" : "s");
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} hour{hours.S()}{(minutesOverflow == 0 ? "" : $" and {minutesOverflow} minute{minutesOverflow.S()}")}";
            }
            else if (minutes < 43800) // < 1 month (30.4166667 days)
            {
                uint days = DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow);
                return $"{days} day{days.S()}{(hoursOverflow == 0 ? "" : $" and {hoursOverflow} hour{hoursOverflow.S()}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow), 30.4166667m, out uint daysOverflow);
                return $"{months} month{months.S()}{(daysOverflow == 0 ? "" : $" and {daysOverflow} day{daysOverflow.S()}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out uint minutesOverflow), 24, out uint hoursOverflow), 30.4166667m, out uint daysOverflow), 12, out uint monthOverflow);
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
        public static string Translate(string key, SteamPlayer player, params object[] formatting) => Translate(key, player.playerID.steamID.m_SteamID, formatting);
        public static string Translate(string key, Player player, params object[] formatting) => Translate(key, player.channel.owner.playerID.steamID.m_SteamID, formatting);
        public static string Translate(string key, UnturnedPlayer player, params object[] formatting) => Translate(key, player.Player.channel.owner.playerID.steamID.m_SteamID, formatting);
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
                            catch (FormatException ex)
                            {
                                CommandWindow.Log(ex);
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
                        catch (FormatException ex)
                        {
                            CommandWindow.Log(ex);
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
                            catch (FormatException ex)
                            {
                                CommandWindow.Log(ex);
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
                    } catch (FormatException ex)
                    {
                        CommandWindow.Log(ex);
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
        public static string S(this int number) => number == 1 ? "" : "s";
        public static string S(this float number) => number == 1 ? "" : "s";
        public static string S(this uint number) => number == 1 ? "" : "s";
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
        public static Team GetTeam(this ETeam team)
        {
            if (team == ETeam.TEAM1) return UCWarfare.I.TeamManager.Team1;
            else if (team == ETeam.TEAM2) return UCWarfare.I.TeamManager.Team2;
            else return UCWarfare.I.TeamManager.Neutral;
        }
        public static Color GetTeamColor(this SteamPlayer player) => GetTeamColor(player.player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this Player player) => GetTeamColor(player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this ulong groupID)
        {
            if (groupID == UCWarfare.Config.Team1ID) return UCWarfare.I.TeamManager.Team1.UnityColor;
            else if (groupID == UCWarfare.Config.Team2ID) return UCWarfare.I.TeamManager.Team2.UnityColor;
            else return UCWarfare.I.TeamManager.Neutral.UnityColor;
        }
        public static string GetTeamColorHex(this SteamPlayer player) => GetTeamColorHex(player.player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this Player player) => GetTeamColorHex(player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this ulong groupID)
        {
            if (groupID == UCWarfare.Config.Team1ID) return UCWarfare.I.TeamManager.Team1.Color;
            else if (groupID == UCWarfare.Config.Team2ID) return UCWarfare.I.TeamManager.Team2.Color;
            else return UCWarfare.I.TeamManager.Neutral.Color;
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
                    UCWarfare.SendUpdateSign.Invoke(ENetReliability.Unreliable, connections, x, y, plant, index, newtext);
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
        public static float GetTerrainHeightAt2DPoint(Vector2 position) => GetTerrainHeightAt2DPoint(position.x, position.y);
        public static float GetTerrainHeightAt2DPoint(float x, float z, float defaultY = 0)
        {
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.GROUND | RayMasks.GROUND2))
                return h.point.y;
            else return defaultY;
        }
        public static string ReplaceCaseInsensitive(this string source, string replaceIf, string replaceWith = "")
        {
            if (source.Length == 0 || replaceIf.Length == 0 || replaceIf == null || replaceWith == null) return source;
            if (source == null) return null;
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
        public static void SendTextureToPlayer(Player destination, Texture2D image)
        {
            if (image == null || destination == null) return;
            byte[] data = image.EncodeToJPG(50);
            if(data.Length > ushort.MaxValue)
            {
                CommandWindow.LogError("Screenshot too large: " + data.Length.ToString() + " bytes.");
                return;
            }
            ITransportConnection transportConnection = destination.channel.owner.transportConnection;
            if (transportConnection != null)
            {
                UCWarfare.SendScreenshotDestination.Invoke(destination.GetNetId(), ENetReliability.Reliable, transportConnection, writer =>
                {
                    writer.WriteUInt16((ushort)data.Length);
                    writer.WriteBytes(data);
                });
                CommandWindow.Log("invoked function");
            }
            else CommandWindow.Log("transport connection was null");
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
            } catch { return false; }
        }
        // https://answers.unity.com/questions/244417/create-line-on-a-texture.html
        public static void DrawLine(Texture2D texture, Line line, Color color, bool apply = true)
        {
            Vector2 point1 = new Vector2(line.pt1.x + texture.width / 2, line.pt1.y + texture.height / 2);
            Vector2 point2 = new Vector2(line.pt2.x + texture.width / 2, line.pt2.y + texture.height / 2);
            Vector2 t = point1;
            float frac = 1 / Mathf.Sqrt(Mathf.Pow(point2.x - point1.x, 2) + Mathf.Pow(point2.y - point1.y, 2));
            float ctr = 0;

            while ((int)t.x != (int)point2.x || (int)t.y != (int)point2.y)
            {
                t = Vector2.Lerp(point1, point2, ctr);
                ctr += frac;
                texture.SetPixel((int)t.x, (int)t.y, color);
            }
            if (apply)
                texture.Apply();
        }
        // https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity
        public static void DrawCircle(Texture2D texture, float x, float y, float radius, Color color, bool apply = true)
        {
            float rSquared = radius * radius;

            for (float u = x - radius; u < x + radius + 1; u++)
                for (float v = y - radius; v < y + radius + 1; v++)
                    if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                        texture.SetPixel((int)Math.Round(u), (int)Math.Round(v), color);
            if (apply)
                texture.Apply();
        }
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
        public static float GetCurrentPlaytime(Player player)
        {
            if (player.transform.TryGetComponent(out Stats.PlaytimeComponent playtimeObj))
                return playtimeObj.CurrentTimeSeconds;
            else return 0f;
        }
        public static FPlayerName GetPlayerOriginalNames(Player player)
        {
            if (UCWarfare.I.OriginalNames.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
                return UCWarfare.I.OriginalNames[player.channel.owner.playerID.steamID.m_SteamID];
            else return new FPlayerName(player);
        }
        public static bool IsInMain(this Player player)
        {
            ulong team = player.GetTeam();
            if (team == 1) return UCWarfare.I.TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return UCWarfare.I.TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsOnFlag(this Player player) => UCWarfare.I.FlagManager.FlagRotation.Exists(F => F.ZoneData.IsInside(player.transform.position));
    }
}
