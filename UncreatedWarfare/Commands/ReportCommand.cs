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
            /*
            if (player.Steam64 != 76561198267927009)
            {
                player.SendChat("Reports are currently disabled.");
                return;
            }*/
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
                    if (CooldownManager.HasCooldown(player, ECooldownType.REPORT, out _, target))
                    {
                        player.SendChat("report_cooldown", targetNames.CharacterName);
                        return;
                    }

                    player.SendChat("report_confirm", target.ToString(Data.Locale), targetNames.CharacterName);
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
                    if (targetPl != null)
                    {
                        ToastMessage.QueueMessage(targetPl, new ToastMessage(Translation.Translate("report_notify_violator", targetPl, typename), EToastMessageSeverity.SEVERE));
                        targetPl.SendChat("report_notify_violator_chat_p1", typename, message);
                        targetPl.SendChat("report_notify_violator_chat_p2");
                    }
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
                    if (targetPl == null)
                    {
                        if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success2 &&
                            success2 && res.Parameters[1] is string messageUrl2)
                        {
                            L.Log(
                                Translation.Translate("report_console_record", JSONMethods.DEFAULT_LANGUAGE,
                                    string.Empty, "0", messageUrl2), ConsoleColor.Cyan);
                        }
                        else
                        {
                            L.Log(
                                Translation.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                                    string.Empty, "0"), ConsoleColor.Cyan);
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
                    }
                    else
                    {
                        L.Log(
                            Translation.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                                targetPl.playerID.playerName,
                                targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
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
        public Dictionary<string, EReportType> types = new Dictionary<string, EReportType>(20)
        {
            { "custom", EReportType.CUSTOM },
            { "none", EReportType.CUSTOM },
            { "chat abuse", EReportType.CHAT_ABUSE },
            { "racism", EReportType.CHAT_ABUSE },
            { "n word", EReportType.CHAT_ABUSE },
            { "chat", EReportType.CHAT_ABUSE },
            { "chat racism", EReportType.CHAT_ABUSE },
            { "voice chat abuse", EReportType.VOICE_CHAT_ABUSE },
            { "voice chat", EReportType.VOICE_CHAT_ABUSE },
            { "voice chat racism", EReportType.VOICE_CHAT_ABUSE },
            { "vc abuse", EReportType.VOICE_CHAT_ABUSE },
            { "vc racism", EReportType.VOICE_CHAT_ABUSE },
            { "vc", EReportType.VOICE_CHAT_ABUSE },
            { "soloing", EReportType.SOLOING_VEHICLE },
            { "soloing vehicles", EReportType.SOLOING_VEHICLE },
            { "asset waste", EReportType.WASTEING_ASSETS },
            { "asset wasteing", EReportType.WASTEING_ASSETS },
            { "wasteing assets", EReportType.WASTEING_ASSETS },
            { "intentional teamkilling", EReportType.INTENTIONAL_TEAMKILL },
            { "teamkilling", EReportType.INTENTIONAL_TEAMKILL },
            { "fob greifing", EReportType.GREIFING_FOBS },
            { "structure greifing", EReportType.GREIFING_FOBS },
            { "base greifing", EReportType.GREIFING_FOBS },
            { "hab greifing", EReportType.GREIFING_FOBS },
            { "greifing", EReportType.GREIFING_FOBS }
        };
        public UCPlayer.ENameSearchType GetNameType(EReportType type)
        {
            return type switch
            {
                EReportType.CUSTOM or EReportType.INTENTIONAL_TEAMKILL or EReportType.GREIFING_FOBS or EReportType.SOLOING_VEHICLE or EReportType.VOICE_CHAT_ABUSE or EReportType.WASTEING_ASSETS => UCPlayer.ENameSearchType.NICK_NAME,
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
                _ => "Custom",
            };
        }
        public EReportType GetReportType(string input)
        {
            if (!types.TryGetValue(input, out EReportType reportType))
                reportType = EReportType.CUSTOM;
            return reportType;
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
