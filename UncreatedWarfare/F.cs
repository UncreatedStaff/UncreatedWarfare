using ColoredConsolePatch;
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
using UncreatedWarfare.Components;
using UncreatedWarfare.Flags;
using UncreatedWarfare.Teams;
using UnityEngine;
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
                return seconds.ToString() + " second" + seconds.S();
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
                if (!Data.Localization.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.Localization.ElementAt(0).Value[key], formatting);
                            }
                            catch (FormatException ex)
                            {
                                F.LogError(ex);
                                return Data.Localization.ElementAt(0).Value[key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                    else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
                else
                {
                    if (Data.Localization[JSONMethods.DefaultLanguage].ContainsKey(key))
                    {
                        try
                        {
                            return string.Format(Data.Localization[JSONMethods.DefaultLanguage][key], formatting);
                        }
                        catch (FormatException ex)
                        {
                            F.LogError(ex);
                            return Data.Localization[JSONMethods.DefaultLanguage][key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                string lang = JSONMethods.DefaultLanguage;
                if (Data.Languages.ContainsKey(player))
                {
                    lang = Data.Languages[player];
                    if (!Data.Localization.ContainsKey(lang))
                        lang = JSONMethods.DefaultLanguage;
                }
                if (!Data.Localization.ContainsKey(lang))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.ContainsKey(key))
                        {
                            try
                            {
                                return string.Format(Data.Localization.ElementAt(0).Value[key], formatting);
                            }
                            catch (FormatException ex)
                            {
                                F.LogError(ex);
                                return Data.Localization.ElementAt(0).Value[key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                    else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
                else if (Data.Localization[lang].ContainsKey(key))
                {
                    try
                    {
                        return string.Format(Data.Localization[lang][key], formatting);
                    }
                    catch (FormatException ex)
                    {
                        F.LogError(ex);
                        return Data.Localization[lang][key] + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
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
            if (Encoding.UTF8.GetByteCount(localizedString) <= MaxChatSizeAmount)
                ChatManager.say(player, localizedString, textColor, localizedString.Contains("</"));
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
                    ChatManager.say(player, newMessage, textColor, newMessage.Contains("</"));
                else
                    LogError("There's been an error sending a chat message. Default message for \"" + text + "\" is longer than "
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
            foreach (SteamPlayer player in Provider.clients)
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
            if (Data.TeamManager == null) return null;
            if (team == ETeam.TEAM1) return Data.TeamManager.Team1;
            else if (team == ETeam.TEAM2) return Data.TeamManager.Team2;
            else return Data.TeamManager.Neutral;
        }
        public static ETeam GetTeamEnum(this SteamPlayer player) => GetTeamEnum(player.player.quests.groupID.m_SteamID);
        public static ETeam GetTeamEnum(this Player player) => GetTeamEnum(player.quests.groupID.m_SteamID);
        public static ETeam GetTeamEnum(this UnturnedPlayer player) => GetTeamEnum(player.Player.quests.groupID.m_SteamID);
        public static ETeam GetTeamEnum(this ulong groupID)
        {
            if (groupID == UCWarfare.Config.Team1ID) return ETeam.TEAM1;
            else if (groupID == UCWarfare.Config.Team2ID) return ETeam.TEAM2;
            else return ETeam.NEUTRAL;
        }
        public static Color GetTeamColor(this SteamPlayer player) => GetTeamColor(player.player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this Player player) => GetTeamColor(player.quests.groupID.m_SteamID);
        public static Color GetTeamColor(this ulong groupID)
        {
            if (Data.TeamManager == null) return UCWarfare.GetColor("default");
            if (groupID == UCWarfare.Config.Team1ID) return Data.TeamManager.Team1.UnityColor;
            else if (groupID == UCWarfare.Config.Team2ID) return Data.TeamManager.Team2.UnityColor;
            else return Data.TeamManager.Neutral.UnityColor;
        }
        public static string GetTeamColorHex(this SteamPlayer player) => GetTeamColorHex(player.player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this Player player) => GetTeamColorHex(player.quests.groupID.m_SteamID);
        public static string GetTeamColorHex(this ulong groupID)
        {
            if (Data.TeamManager == null) return null;
            if (groupID == UCWarfare.Config.Team1ID) return Data.TeamManager.Team1.Color;
            else if (groupID == UCWarfare.Config.Team2ID) return Data.TeamManager.Team2.Color;
            else return Data.TeamManager.Neutral.Color;
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
        public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this UnturnedPlayer player) => GetTeamByte(player.Player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this ulong groupID)
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
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1_words")}>{Translate("ui_capturing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2_words")}>{Translate("ui_capturing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case UIOption.Blank:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"",
                                $"<color=#{UCWarfare.GetColorHex("capturing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.GetColorHex("capturing_team_2_bkgr"));
                        break;
                    case UIOption.Losing:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_1_words")}>{Translate("ui_losing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("losing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("losing_team_2_words")}>{Translate("ui_losing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("losing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("losing_team_2_bkgr"));
                        break;
                    case UIOption.Secured:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_1_words")}>{Translate("ui_secured", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("secured_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("secured_team_2_words")}>{Translate("ui_secured", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("secured_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("secured_team_2_bkgr"));
                        break;
                    case UIOption.Contested:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_1_words")}>{Translate("ui_contested", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("contested_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("contested_team_2_words")}>{Translate("ui_contested", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("contested_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("contested_team_2_bkgr"));
                        break;
                    case UIOption.NoCap:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_1_words")}>{Translate("ui_nocap", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("nocap_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("nocap_team_2_words")}>{Translate("ui_nocap", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("nocap_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("nocap_team_2_bkgr"));
                        break;
                    case UIOption.Clearing:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_1_words")}>{Translate("ui_clearing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("clearing_team_1")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("clearing_team_2_words")}>{Translate("ui_clearing", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("clearing_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("clearing_team_2_bkgr"));
                        break;
                    case UIOption.NotOwned:
                        if (team == Data.TeamManager.Team1.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_1_words")}>{Translate("ui_notowned", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_1_bkgr"));
                        else if (team == Data.TeamManager.Team2.GroupID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true,
                                $"<color=#{UCWarfare.GetColorHex("notowned_team_2_words")}>{Translate("ui_notowned", playerID)}</color>", $"<color=#{UCWarfare.GetColorHex("notowned_team_2")}>" +
                                $"{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.GetColorHex("notowned_team_2_bkgr"));
                        break;
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
        public static Vector3 GetBaseSpawn(this SteamPlayer player)
        {
            ulong team = player.GetTeam();
            if (team == 1)
            {
                Vector2 v2 = Data.TeamManager.Team1Main.Center;
                return new Vector3(v2.x, GetTerrainHeightAt2DPoint(v2.x, v2.y), v2.y);
            }
            else if (team == 2)
            {
                Vector2 v2 = Data.TeamManager.Team2Main.Center;
                return new Vector3(v2.x, GetTerrainHeightAt2DPoint(v2.x, v2.y), v2.y);
            }
            else if (team == 3) return Data.TeamManager.LobbySpawn;
            else return Data.ExtraPoints["lobby_spawn"];
        }
        public static string QuickSerialize(object obj) => JsonConvert.SerializeObject(obj);
        public static T QuickDeserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
        public static void InvokeSignUpdateFor(SteamPlayer client, byte x, byte y, ushort plant, ushort index, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
                newtext = Translate(text, client.playerID.steamID.m_SteamID);
            Log("Invoking sign update for " + client.playerID.playerName + "\n" + text + " -> " + newtext, ConsoleColor.White);
            Data.SendUpdateSign.Invoke(ENetReliability.Unreliable, client.transportConnection, x, y, plant, index, newtext);
        }
        public static void InvokeSignUpdateForAll(byte x, byte y, ushort plant, ushort index, string text, BarricadeRegion region)
        {
            Dictionary<string, List<SteamPlayer>> playergroups = new Dictionary<string, List<SteamPlayer>>();
            foreach (SteamPlayer client in Provider.clients)
            {
                if (Data.Languages.ContainsKey(client.playerID.steamID.m_SteamID))
                {
                    if (playergroups.ContainsKey(Data.Languages[client.playerID.steamID.m_SteamID]))
                        playergroups[Data.Languages[client.playerID.steamID.m_SteamID]].Add(client);
                    else
                        playergroups.Add(Data.Languages[client.playerID.steamID.m_SteamID], new List<SteamPlayer> { client });
                }
                else
                {
                    if (playergroups.ContainsKey(JSONMethods.DefaultLanguage))
                        playergroups[JSONMethods.DefaultLanguage].Add(client);
                    else
                        playergroups.Add(JSONMethods.DefaultLanguage, new List<SteamPlayer> { client });
                }
            }
            Log("Invoking sign update for " + Provider.clients.Count + " player" + S(Provider.clients.Count) + ": \n" + text, ConsoleColor.White);
            foreach (KeyValuePair<string, List<SteamPlayer>> languageGroup in playergroups)
            {
                if (languageGroup.Value.Count > 0)
                {
                    string newtext = text;
                    if (text.StartsWith("sign_"))
                        newtext = Translate(text, languageGroup.Value[0].playerID.steamID.m_SteamID);
                    List<ITransportConnection> connections = new List<ITransportConnection>();
                    languageGroup.Value.ForEach(l => connections.Add(l.transportConnection));
                    Data.SendUpdateSign.Invoke(ENetReliability.Unreliable, connections, x, y, plant, index, newtext);
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
            Log("Invoking sign update for " + client.playerID.playerName + "\n" + newtext, ConsoleColor.White);
            if (newtext.StartsWith("sign_"))
                newtext = Translate(newtext ?? "", client.playerID.steamID.m_SteamID);
            Log("Translated \"" + (changeText ? text : oldtext) + "\" -> \"" + newtext + '\"', ConsoleColor.White);
            Data.SendUpdateSign.Invoke(ENetReliability.Unreliable, client.transportConnection, x, y, plant, index, newtext);
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
            else if (player == null || player == CSteamID.Nil)
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
        public static float GetCurrentPlaytime(this Player player)
        {
            if (player.TryGetPlaytimeComponent(out PlaytimeComponent playtimeObj))
                return playtimeObj.CurrentTimeSeconds;
            else return 0f;
        }
        public static FPlayerName GetPlayerOriginalNames(SteamPlayer player) => GetPlayerOriginalNames(player.player);
        public static FPlayerName GetPlayerOriginalNames(Player player)
        {
            if (Data.OriginalNames.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
                return Data.OriginalNames[player.channel.owner.playerID.steamID.m_SteamID];
            else return new FPlayerName(player);
        }
        public static bool IsInMain(this Player player)
        {
            ulong team = player.GetTeam();
            if (team == 1) return Data.TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return Data.TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsOnFlag(this Player player) => Data.FlagManager.FlagRotation.Exists(F => F.ZoneData.IsInside(player.transform.position));
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
                    if (!Data.LimbLocalization.ContainsKey(lang))
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
        public static string TranslateDeath(ulong player, string key, EDeathCause backupcause, FPlayerName dead, ETeam deadTeam, FPlayerName killerName, ETeam killerTeam, ELimb limb, string itemName, float distance, bool usePlayerName = false, bool translateKillerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = ColorizeName(deadname, deadTeam);
            string murderername = translateKillerName ? Translate(killerName.PlayerName, player) : (usePlayerName ? killerName.PlayerName : killerName.CharacterName);
            if (colorize) murderername = ColorizeName(murderername, killerTeam);
            string dis = Math.Round(distance).ToString() + 'm';
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
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                                return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                            return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                            return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                        }
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                }
            }
            else
            {
                string lang = JSONMethods.DefaultLanguage;
                if (Data.Languages.ContainsKey(player))
                {
                    lang = Data.Languages[player];
                    if (!Data.DeathLocalization.ContainsKey(lang))
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
                                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                                return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                            }
                        }
                        else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                    }
                    else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                        return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
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
                        return backupcause.ToString() + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                    }
                }
                else return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
            }
        }
        public static string TranslateLandmineDeath(ulong player, string key, FPlayerName dead, ETeam deadTeam, FPlayerName killerName, ETeam killerTeam, FPlayerName triggererName, ETeam triggererTeam, ELimb limb, string landmineName, bool usePlayerName = false, bool colorize = true)
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
                    if (Data.DeathLocalization.ContainsKey(lang))
                        lang = Data.Languages[player];
                    else
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
        public static string ColorizeName(string innerText, ETeam team)
        {
            Team teamObj = GetTeam(team);
            if (teamObj == null) return innerText;
            else if (team == ETeam.TURNED) return $"<color=#{teamObj.Color}>{innerText}</color>";
            else return $"<color=#{teamObj.Color}>{innerText}</color>";
        }
        /// <param name="backupcause">Used in case the key can not be found.</param>
        public static void BroadcastDeath(string key, EDeathCause backupcause, FPlayerName dead, ETeam deadTeam, FPlayerName killerName, bool translateKillerName, ETeam killerTeam, ELimb limb, string itemName, float distance, out string message, bool broadcast = true)
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
                                TranslateLimb(player.playerID.steamID.m_SteamID, limb), itemName, Math.Round(distance).ToString());
                        }
                        catch (FormatException)
                        {
                            newMessage = key + $" ({ColorizeName(dead.CharacterName, deadTeam)}, {ColorizeName(killer, killerTeam)}, {limb}, {itemName}, {Math.Round(distance).ToString() + "m"}";
                            LogWarning("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + key + "\"");
                        }
                        if (Encoding.UTF8.GetByteCount(newMessage) <= MaxChatSizeAmount)
                            ChatManager.say(player.playerID.steamID, newMessage, UCWarfare.GetColor("death_background"), newMessage.Contains("</"));
                        else
                            LogError("There's been an error sending a chat message. Default message for \"" + key + "\" is longer than "
                                + MaxChatSizeAmount.ToString() + " bytes in UTF-8. Arguments may be too long.");
                    }
                }
            }
            message = TranslateDeath(0, key, backupcause, dead, deadTeam, killerName, killerTeam, limb, itemName, distance, true, translateKillerName, false);
        }
        public static void BroadcastLandmineDeath(string key, FPlayerName dead, ETeam deadTeam, FPlayerName killerName, ETeam killerTeam, FPlayerName triggererName, ETeam triggererTeam, ELimb limb, string landmineName, out string message, bool broadcast = true)
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
                                + MaxChatSizeAmount.ToString() + " bytes in UTF-8. Arguments may be too long.");
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
        public static void Log(string info, ConsoleColor color = ConsoleColor.Gray)
        {
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Gray || UnturnedThreadedColoredConsole.I == null || UnturnedThreadedColoredConsole.I.console == null)
                {
                    CommandWindow.Log(info);
                }
                else
                {
                    UnturnedThreadedColoredConsole.I.console.AddLine(info, color);
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
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Yellow || UnturnedThreadedColoredConsole.I == null || UnturnedThreadedColoredConsole.I.console == null)
                {
                    CommandWindow.LogWarning(warning);
                }
                else
                {
                    UnturnedThreadedColoredConsole.I.console.AddLine(warning, color);
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
                if (!UCWarfare.Config.UseColoredConsoleModule || color == ConsoleColor.Red || UnturnedThreadedColoredConsole.I == null || UnturnedThreadedColoredConsole.I.console == null)
                {
                    CommandWindow.LogError(error);
                }
                else
                {
                    UnturnedThreadedColoredConsole.I.console.AddLine(error, color);
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
        public static void Log(object obj, ConsoleColor color = ConsoleColor.Gray) => Log(obj.ToString(), color);
        public static void LogWarning(object obj, ConsoleColor color = ConsoleColor.Yellow) => LogWarning(obj.ToString(), color);
        public static void LogError(object obj, ConsoleColor color = ConsoleColor.Red) => LogError(obj.ToString(), color);
        public static void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        {
            string message = $"EXCEPTION \n\n{ex.Message}\n{ex.StackTrace}\n\nFINISHED";
            try
            {
                if (!UCWarfare.Config.UseColoredConsoleModule || UnturnedThreadedColoredConsole.I == null || UnturnedThreadedColoredConsole.I.console == null)
                {
                    CommandWindow.LogError(message);
                }
                else
                {
                    UnturnedThreadedColoredConsole.I.console.AddLine(message, color);
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
    }
}
