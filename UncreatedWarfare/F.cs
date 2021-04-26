using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static string Translate(string key, params object[] formatting) => UCWarfare.I.Translations.Instance.Translate(key, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player">UnturnedPlayer to send the chat to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void SendChat(this UnturnedPlayer player, string text, Color textColor, params object[] formatting) => SendChat(player.CSteamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player">Player to send the chat to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void SendChat(this Player player, string text, Color textColor, params object[] formatting) => SendChat(player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player">SteamPlayer to send the chat to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void SendChat(this SteamPlayer player, string text, Color textColor, params object[] formatting) => SendChat(player.player.channel.owner.playerID.steamID, text, textColor, formatting);
        /// <summary>
        /// Send a message in chat using the RocketMod translation file.
        /// </summary>
        /// <param name="player">CSteamID to send the chat to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void SendChat(this CSteamID player, string text, Color textColor, params object[] formatting)
        {
            bool isRich = false;
            if (Translate(text, formatting).Contains("</"))
                isRich = true;
            try
            {
                ChatManager.say(player, Translate(text, formatting), textColor, isRich);
            }
            catch
            {
                try
                {
                    Log($"'{Translate(text, formatting)}' is too long, sending default message instead, consider shortening your translation of {text}.");
                    ChatManager.say(player, UCWarfare.I.DefaultTranslations.Translate(text, formatting), textColor, isRich);
                }
                catch (FormatException)
                {
                    Log("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
            }
        }
        /// <summary>
        /// Send a message in chat to everyone.
        /// </summary>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void Broadcast(string text, Color textColor, params object[] formatting)
        {
            bool isRich = false;
            if (Translate(text, formatting).Contains("</"))
                isRich = true;
            try
            {
                ChatManager.say(Translate(text, formatting), textColor, isRich);
            }
            catch
            {
                try
                {
                    Log($"'{Translate(text, formatting)}' is too long, sending default message instead, consider shortening your translation of {text}.");
                    ChatManager.say(UCWarfare.I.DefaultTranslations.Translate(text, formatting), textColor, isRich);
                }
                catch (FormatException)
                {
                    Log("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
            }
        }
        /// <summary>
        /// Send a message in chat to everyone except one person.
        /// </summary>
        /// <param name="LeaveOut">The one person not to send a message to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void BroadcastToAllExceptOne(CSteamID LeaveOut, string text, Color textColor, params object[] formatting)
        {
            bool isRich = false;
            if (Translate(text, formatting).Contains("</"))
                isRich = true;
            try
            {
                foreach (SteamPlayer player in Provider.clients)
                    if (player.playerID.steamID.m_SteamID != LeaveOut.m_SteamID)
                        ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
            }
            catch
            {
                try
                {
                    Log($"'{Translate(text, formatting)}' is too long, sending default message instead, consider shortening your translation of {text}.");
                    foreach (SteamPlayer player in Provider.clients)
                        if (player.playerID.steamID.m_SteamID != LeaveOut.m_SteamID)
                            ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
                }
                catch (FormatException)
                {
                    Log("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
            }
        }
        /// <summary>
        /// Send a message in chat to everyone except two people.
        /// </summary>
        /// <param name="LeaveOut">The one person not to send a message to.</param>
        /// <param name="LeaveOut2">The one person not to send a message to.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void BroadcastToAllExceptTwo(CSteamID LeaveOut, CSteamID LeaveOut2, string text, Color textColor, params object[] formatting)
        {
            bool isRich = false;
            if (Translate(text, formatting).Contains("</"))
                isRich = true;
            try
            {
                foreach (SteamPlayer player in Provider.clients)
                    if (player.playerID.steamID.m_SteamID != LeaveOut.m_SteamID && player.playerID.steamID.m_SteamID != LeaveOut2.m_SteamID)
                        ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
            }
            catch
            {
                try
                {
                    Log($"'{Translate(text, formatting)}' is too long, sending default message instead, consider shortening your translation of {text}.");
                    foreach (SteamPlayer player in Provider.clients)
                        if (player.playerID.steamID.m_SteamID != LeaveOut.m_SteamID && player.playerID.steamID.m_SteamID != LeaveOut2.m_SteamID)
                            ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
                }
                catch (FormatException)
                {
                    Log("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
            }
        }
        /// <summary>
        /// Send a message in chat to everyone except a list of people. Somewhat ineffecient.
        /// </summary>
        /// <param name="Excluded">List of people to exclude the message from.</param>
        /// <param name="text">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="textColor">The color of the chat.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        public static void BroadcastToAllExcept(this List<CSteamID> Excluded, string text, Color textColor, params object[] formatting)
        {
            bool isRich = false;
            if (Translate(text, formatting).Contains("</"))
                isRich = true;
            try
            {
                foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                    ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
            }
            catch
            {
                try
                {
                    Log($"'{Translate(text, formatting)}' is too long, sending default message instead, consider shortening your translation of {text}.");
                    foreach (SteamPlayer player in Provider.clients.Where(x => !Excluded.Exists(y => y.m_SteamID == x.playerID.steamID.m_SteamID)))
                        ChatManager.say(player.playerID.steamID, Translate(text, formatting), textColor, isRich);
                }
                catch (FormatException)
                {
                    Log("There's been an error sending a chat message. Please make sure that you don't have invalid formatting symbols in \"" + text + "\"");
                }
            }
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
        public static string S(this int number) => number == 1 ? "n" : "";
        public static string S(this uint number) => number == 1 ? "n" : "";
        public static string S(this sbyte number) => number == 1 ? "n" : "";
        public static string S(this byte number) => number == 1 ? "n" : "";
        public static string S(this short number) => number == 1 ? "n" : "";
        public static string S(this ushort number) => number == 1 ? "n" : "";
        public static string S(this long number) => number == 1 ? "n" : "";
        public static string S(this ulong number) => number == 1 ? "n" : "";
        public enum UIOption
        {
            Capturing,
            Losing,
            Secured,
            Contested,
            NoCap,
            Clearing,
            Blank
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
        public static void UIOrChat(ulong team, UIOption type, string chatmessage, Color color, SteamPlayer player, int circleAmount, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true, bool absolute = true, object[] formatting = null)
            => UIOrChat(team, type, chatmessage, color, Provider.findTransportConnection(player.playerID.steamID), player, circleAmount, SendChatIfConfiged, SendUIIfConfiged, absolute, formatting);
        public static void UIOrChat(ulong team, UIOption type, string translation_key, Color color, ITransportConnection PlayerConnection, SteamPlayer player, int c, bool SendChatIfConfiged = true, bool SendUIIfConfiged = true, bool absolute = true, object[] formatting = null)
        {
            int circleAmount = Math.Abs(c);
            if (UCWarfare.Config.FlagSettings.UseUI && SendUIIfConfiged)
            {
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, PlayerConnection);
                switch (type)
                {
                    case UIOption.Capturing:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["capturing_team_1_words"]}>{UCWarfare.Config.FlagSettings.CapturingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["capturing_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["capturing_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["capturing_team_2_words"]}>{UCWarfare.Config.FlagSettings.CapturingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["capturing_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["capturing_team_2_bkgr"]);
                        break;
                    case UIOption.Blank:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"", $"<color=#{UCWarfare.I.ColorsHex["capturing_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.I.ColorsHex["capturing_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"", $"<color=#{UCWarfare.I.ColorsHex["capturing_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(0)]}</color>", UCWarfare.I.ColorsHex["capturing_team_2_bkgr"]);
                        break;
                    case UIOption.Losing:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["losing_team_1_words"]}>{UCWarfare.Config.FlagSettings.LosingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["losing_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["losing_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["losing_team_2_words"]}>{UCWarfare.Config.FlagSettings.LosingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["losing_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["losing_team_2_bkgr"]);
                        break;
                    case UIOption.Secured:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["secured_team_1_words"]}>{UCWarfare.Config.FlagSettings.SecuredText}</color>", $"<color=#{UCWarfare.I.ColorsHex["secured_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["secured_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["secured_team_2_words"]}>{UCWarfare.Config.FlagSettings.SecuredText}</color>", $"<color=#{UCWarfare.I.ColorsHex["secured_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["secured_team_2_bkgr"]);
                        break;
                    case UIOption.Contested:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["contested_team_1_words"]}>{UCWarfare.Config.FlagSettings.ContestedText}</color>", $"<color=#{UCWarfare.I.ColorsHex["contested_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["contested_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["contested_team_2_words"]}>{UCWarfare.Config.FlagSettings.ContestedText}</color>", $"<color=#{UCWarfare.I.ColorsHex["contested_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["contested_team_2_bkgr"]);
                        break;
                    case UIOption.NoCap:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["nocap_team_1_words"]}>{UCWarfare.Config.FlagSettings.NoCapText}</color>", $"<color=#{UCWarfare.I.ColorsHex["nocap_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["nocap_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["nocap_team_2_words"]}>{UCWarfare.Config.FlagSettings.NoCapText}</color>", $"<color=#{UCWarfare.I.ColorsHex["nocap_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["nocap_team_2_bkgr"]);
                        break;
                    case UIOption.Clearing:
                        if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["clearing_team_1_words"]}>{UCWarfare.Config.FlagSettings.ClearingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["clearing_team_1"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["clearing_team_1_bkgr"]);
                        else if (team == UCWarfare.Config.Team1ID)
                            EffectManager.sendUIEffect(UCWarfare.Config.FlagSettings.UIID, (short)(UCWarfare.Config.FlagSettings.UIID - short.MaxValue), PlayerConnection, true, $"<color=#{UCWarfare.I.ColorsHex["clearing_team_2_words"]}>{UCWarfare.Config.FlagSettings.ClearingText}</color>", $"<color=#{UCWarfare.I.ColorsHex["clearing_team_2"]}>{UCWarfare.Config.FlagSettings.charactersForUI[FlagManager.FromMax(circleAmount)]}</color>", UCWarfare.I.ColorsHex["clearing_team_2_bkgr"]);
                        break;
                }
            }
            if (UCWarfare.Config.FlagSettings.UseChat && SendChatIfConfiged)
            {
                if(formatting == null)
                    player.SendChat(translation_key, color);
                else
                    player.SendChat(translation_key, color, formatting);
            }
        }
    }
}
