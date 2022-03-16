using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using Uncreated.Players;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.ReportSystem;

namespace Uncreated.Warfare.Commands
{
    public class ReportCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "report";
        public string Help => "Use to report a player for specific actions. Use /report reasons for examples.";
        public string Syntax => "/report <\"reasons\" | player> <reason> <custom message...>";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.report" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            // /report john greifing keeps using the mortar on the fobs 
            // /report john teamkilling teamkilled 5 teammates
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null)
            {
                L.LogWarning("This command can't be called from console.");
                return;
            }
            if (command.Length < 2)
            {
                goto Help;
            }
            EReportType type;
            string message;
            ulong target;
            if (command.Length == 2)
            {
                string inPlayer = command[0];
                string arg1 = command[1].ToLower();
                if (arg1 == "help")
                    goto Help;
                else if (arg1 == "reports" || arg1 == "reasons" || arg1 == "types")
                    goto Types;

                if (!CheckLinked(player))
                    goto DiscordNotLinked;
                message = string.Empty;
                type = GetReportType(arg1);
                if (type == EReportType.CUSTOM)
                    goto Help;
                else
                {
                    if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out target)))
                    {
                        UCPlayer.ENameSearchType search = GetNameType(type);
                        target = Data.Reporter.RecentPlayerNameCheck(inPlayer, search);
                        if (target == 0)
                            goto PlayerNotFound;
                    }
                    goto Report;
                }
            }
            else
            {
                string inPlayer = command[0];
                string arg1 = command[1].ToLower();
                string arg2p;

                if (!CheckLinked(player))
                    goto DiscordNotLinked;

                type = GetReportType(arg1);
                if (type == EReportType.CUSTOM)
                    arg2p = string.Join(" ", command, 1, command.Length - 1);
                else
                    arg2p = string.Join(" ", command, 2, command.Length - 2);
                message = arg2p;

                if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out target)))
                {
                    UCPlayer.ENameSearchType search = GetNameType(type);
                    UCPlayer? temptarget = UCPlayer.FromName(inPlayer, search);
                    if (temptarget == null)
                    {
                        target = Data.Reporter.RecentPlayerNameCheck(inPlayer, search);
                    }
                    else target = temptarget.Steam64;
                    if (target == 0)
                    {
                        goto PlayerNotFound;
                    }
                }
                goto Report;
            }


        Report:
            Task.Run(async () =>
            {
                try
                {
                    await UCWarfare.ToUpdate();
                    FPlayerName targetNames = F.GetPlayerOriginalNames(target);
                    if (CooldownManager.HasCooldownNoStateCheck(player, ECooldownType.REPORT, out Cooldown cd) && cd.data.Length > 0 && cd.data[0] is ulong ul && ul == target)
                    {
                        player.SendChat("report_cooldown", targetNames.CharacterName);
                        return;
                    }

                    player.SendChat("report_confirm", target.ToString(Data.Locale), targetNames.CharacterName);
                    ActionLog.Add(EActionLogType.START_REPORT, string.Join(", ", command), player);
                    bool didConfirm = await CommandWaitTask.WaitForCommand(player, "confirm", 10000);
                    await UCWarfare.ToUpdate();
                    if (!didConfirm)
                    {
                        player.SendChat("report_cancelled", targetNames.CharacterName);
                        return;
                    }
                    CooldownManager.StartCooldown(player, ECooldownType.REPORT, 3600f, target);
                    Report? report;
                    report = type switch
                    {
                        EReportType.CHAT_ABUSE => Data.Reporter.CreateChatAbuseReport(player.Steam64, target, message),
                        EReportType.VOICE_CHAT_ABUSE => Data.Reporter.CreateVoiceChatAbuseReport(player.Steam64, target, message),
                        EReportType.SOLOING_VEHICLE => Data.Reporter.CreateSoloingReport(player.Steam64, target, message),
                        EReportType.WASTEING_ASSETS => Data.Reporter.CreateWasteingAssetsReport(player.Steam64, target, message),
                        EReportType.INTENTIONAL_TEAMKILL => Data.Reporter.CreateIntentionalTeamkillReport(player.Steam64, target, message),
                        EReportType.GREIFING_FOBS => Data.Reporter.CreateGreifingFOBsReport(player.Steam64, target, message),
                        EReportType.CHEATING => Data.Reporter.CreateCheatingReport(player.Steam64, target, message),
                        _ => Data.Reporter.CreateReport(player.Steam64, target, message),
                    };
                    if (report == null)
                    {
                        player.SendChat("report_unknown_error");
                        return;
                    }
                    SteamPlayer? targetPl = PlayerTool.getSteamPlayer(target);
                    Data.DatabaseManager.AddReport(report);
                    string typename = GetName(type);
                    NotifyAdminsOfReport(targetNames, player.Name, report, type, typename);
                    player.SendChat("report_success_p1", targetNames.CharacterName, string.IsNullOrEmpty(message) ? "---" : message, typename);
                    player.SendChat("report_success_p2");
                    L.Log(Translation.Translate("report_console", JSONMethods.DEFAULT_LANGUAGE,
                        player.Player.channel.owner.playerID.playerName, player.Steam64.ToString(Data.Locale),
                        targetNames.PlayerName, target.ToString(Data.Locale), report.Message, typename), ConsoleColor.Cyan);
                    byte[] jpgData =
                        targetPl == null || (type != EReportType.CUSTOM && type < EReportType.SOLOING_VEHICLE)
                            ? new byte[0]
                            : await SpyTask.RequestScreenshot(targetPl);
                    report.JpgData = jpgData;
                    L.Log(report.JpgData.Length.ToString());
                    NetTask.Response res = await Reporter.SendReportInvocation.Request(
                        Reporter.ReceiveInvocationResponse, Data.NetClient.connection, report, targetPl != null);
                    await UCWarfare.ToUpdate();
                    if (targetPl != null)
                    {
                        ToastMessage.QueueMessage(targetPl, new ToastMessage(Translation.Translate("report_notify_violator", targetPl, typename), EToastMessageSeverity.SEVERE));
                        targetPl.SendChat("report_notify_violator_chat_p1", typename, message);
                        targetPl.SendChat("report_notify_violator_chat_p2");
                    }
                    else
                    {
                        if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success2 &&
                            success2 && res.Parameters[1] is string messageUrl2)
                        {
                            L.Log(
                                Translation.Translate("report_console_record", JSONMethods.DEFAULT_LANGUAGE,
                                    string.Empty, "0", messageUrl2), ConsoleColor.Cyan);
                            ActionLog.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report URL: " + messageUrl2, player);
                        }
                        else
                        {
                            L.Log(
                                Translation.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                                    string.Empty, "0"), ConsoleColor.Cyan);
                            ActionLog.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report did not reach the discord bot.", player);
                        }
                        return;
                    }

                    if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success &&
                        success && res.Parameters[1] is string messageUrl)
                    {
                        //await UCWarfare.ToUpdate();
                        //F.SendURL(targetPl, Translation.Translate("report_popup", targetPl, typename), messageUrl);
                        L.Log(
                            Translation.Translate("report_console_record", JSONMethods.DEFAULT_LANGUAGE,
                                targetPl.playerID.playerName,
                                targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale), messageUrl),
                            ConsoleColor.Cyan);
                        ActionLog.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report URL: " + messageUrl, player);
                    }
                    else
                    {
                        L.Log(
                            Translation.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                                targetPl.playerID.playerName,
                                targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                        ActionLog.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report did not reach the discord bot.", player);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                }
            });
            return;
        PlayerNotFound:
            player.SendChat("report_player_not_found", player.Steam64.ToString(Data.Locale));
            return;
        DiscordNotLinked:
            player.SendChat("report_discord_not_linked", player.Steam64.ToString(Data.Locale));
            return;
        Help:
            player.SendChat("report_syntax");
        Types:
            player.SendChat("report_reasons");
            return;
        }

        public KeyValuePair<string, EReportType>[] types = new KeyValuePair<string, EReportType>[]
        {
            new KeyValuePair<string, EReportType>("custom", EReportType.CUSTOM),
            new KeyValuePair<string, EReportType>("none", EReportType.CUSTOM),
            new KeyValuePair<string, EReportType>("chat abuse", EReportType.CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("racism", EReportType.CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("n word", EReportType.CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("chat", EReportType.CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("chat racism", EReportType.CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("voice chat abuse", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("voice chat", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("voice chat racism", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("vc abuse", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("vc racism", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("vc", EReportType.VOICE_CHAT_ABUSE),
            new KeyValuePair<string, EReportType>("soloing", EReportType.SOLOING_VEHICLE),
            new KeyValuePair<string, EReportType>("soloing vehicles", EReportType.SOLOING_VEHICLE),
            new KeyValuePair<string, EReportType>("asset waste", EReportType.WASTEING_ASSETS),
            new KeyValuePair<string, EReportType>("asset wasteing", EReportType.WASTEING_ASSETS),
            new KeyValuePair<string, EReportType>("wasteing assets", EReportType.WASTEING_ASSETS),
            new KeyValuePair<string, EReportType>("intentional teamkilling", EReportType.INTENTIONAL_TEAMKILL),
            new KeyValuePair<string, EReportType>("teamkilling", EReportType.INTENTIONAL_TEAMKILL),
            new KeyValuePair<string, EReportType>("fob greifing", EReportType.GREIFING_FOBS),
            new KeyValuePair<string, EReportType>("structure greifing", EReportType.GREIFING_FOBS),
            new KeyValuePair<string, EReportType>("base greifing", EReportType.GREIFING_FOBS),
            new KeyValuePair<string, EReportType>("hab greifing", EReportType.GREIFING_FOBS),
            new KeyValuePair<string, EReportType>("greifing", EReportType.GREIFING_FOBS),
            new KeyValuePair<string, EReportType>("cheating", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("hacking", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("wallhacks", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("hacks", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("cheats", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("hacker", EReportType.CHEATING),
            new KeyValuePair<string, EReportType>("cheater", EReportType.CHEATING)
        };
        public UCPlayer.ENameSearchType GetNameType(EReportType type)
        {
            return type switch
            {
                EReportType.CUSTOM or EReportType.INTENTIONAL_TEAMKILL or EReportType.GREIFING_FOBS or EReportType.SOLOING_VEHICLE or EReportType.VOICE_CHAT_ABUSE or EReportType.WASTEING_ASSETS or EReportType.CHEATING => UCPlayer.ENameSearchType.NICK_NAME,
                EReportType.CHAT_ABUSE => UCPlayer.ENameSearchType.CHARACTER_NAME,
                _ => UCPlayer.ENameSearchType.CHARACTER_NAME,
            };
        }
        public string GetName(EReportType type)
        {
            return type switch
            {
                EReportType.CHAT_ABUSE => "Chat Abuse / Racism",
                EReportType.VOICE_CHAT_ABUSE => "Voice Chat Abuse / Racism",
                EReportType.SOLOING_VEHICLE => "Soloing Vehicle",
                EReportType.WASTEING_ASSETS => "Wasteing Assets / Vehicle Greifing",
                EReportType.INTENTIONAL_TEAMKILL => "Intentional Teamkilling",
                EReportType.GREIFING_FOBS => "FOB / Friendly Structure Greifing",
                EReportType.CHEATING => "Cheating",
                _ => "Custom",
            };
        }
        public EReportType GetReportType(string input)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                ref KeyValuePair<string, EReportType> type = ref types[i];
                if (type.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
                    return type.Value;
            }
            return EReportType.CUSTOM;
        }
        public bool CheckLinked(UCPlayer player) => Data.DatabaseManager.GetDiscordID(player.Steam64, out ulong discordID) && discordID != 0;
        public void NotifyAdminsOfReport(FPlayerName violator, FPlayerName reporter, Report report, EReportType type, string typename)
        {
            foreach (LanguageSet set in Translation.EnumeratePermissions(EAdminType.MODERATE_PERMS))
            {
                string translation = Translation.Translate("report_notify_admin", set.Language, reporter.CharacterName, violator.CharacterName, report.Message, typename);
                while (set.MoveNext())
                {
                    ToastMessage.QueueMessage(set.Next, new ToastMessage(translation, EToastMessageSeverity.INFO));
                }
            }
        }
    }
}
