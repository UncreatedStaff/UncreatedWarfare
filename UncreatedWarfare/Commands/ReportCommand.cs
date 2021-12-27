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
            // /report john greifing keeps using the mortar on the fobs 
            // /report john teamkilling teamkilled 5 teammates
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);
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
                    UCPlayer temptarget = UCPlayer.FromName(inPlayer, search);
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
            Report report;
            switch (type)
            {
                default:
                case EReportType.CUSTOM:
                    report = Data.Reporter.CreateReport(player.Steam64, target, message);
                    break;
                case EReportType.CHAT_ABUSE:
                    report = Data.Reporter.CreateChatAbuseReport(player.Steam64, target, message);
                    break;
                case EReportType.VOICE_CHAT_ABUSE:
                    report = Data.Reporter.CreateVoiceChatAbuseReport(player.Steam64, target, message);
                    break;
                case EReportType.SOLOING_VEHICLE:
                    report = Data.Reporter.CreateSoloingReport(player.Steam64, target, message);
                    break;
                case EReportType.WASTEING_ASSETS:
                    report = Data.Reporter.CreateWasteingAssetsReport(player.Steam64, target, message);
                    break;
                case EReportType.INTENTIONAL_TEAMKILL:
                    report = Data.Reporter.CreateIntentionalTeamkillReport(player.Steam64, target, message);
                    break;
                case EReportType.GREIFING_FOBS:
                    report = Data.Reporter.CreateGreifingFOBsReport(player.Steam64, target, message);
                    break;
            }
            if (report == null)
                goto UnknownError;
            SteamPlayer targetPl = PlayerTool.getSteamPlayer(target);
            Data.DatabaseManager.AddReport(report);
            FPlayerName targetNames = F.GetPlayerOriginalNames(target);
            string typename = GetName(type);
            NotifyAdminsOfReport(targetNames, player.Name, report, type, typename);
            player.SendChat("report_success_p1", targetNames.CharacterName, string.IsNullOrEmpty(message) ? "---" : message, typename);
            player.SendChat("report_success_p2");
            if (targetPl != null)
            {
                ToastMessage.QueueMessage(targetPl, Translation.Translate("report_notify_violator", targetPl, typename), EToastMessageSeverity.SEVERE);
                targetPl.SendChat("report_notify_violator_chat_p1", typename, message);
                targetPl.SendChat("report_notify_violator_chat_p2");
            }
            L.Log(Translation.Translate("report_console", JSONMethods.DefaultLanguage,
                player.Player.channel.owner.playerID.playerName, player.Steam64.ToString(Data.Locale),
                targetNames.PlayerName, target.ToString(Data.Locale), report.Message, typename), ConsoleColor.Cyan);
            Task.Run(
            async () =>
            {
                NetTask.Response res = await Reporter.SendReportInvocation.Request(Reporter.ReceiveInvocationResponse, Data.NetClient.connection, report, targetPl != null);
                if (targetPl == null)
                {
                    L.LogError("player null");
                    return;
                }
                if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success && success && res.Parameters[1] is string messageUrl)
                {
                    await UCWarfare.ToUpdate();
                    //F.SendURL(targetPl, Translation.Translate("report_popup", targetPl, typename), messageUrl);
                    L.Log(Translation.Translate("report_console_record", JSONMethods.DefaultLanguage, targetPl.playerID.playerName, targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale), messageUrl), ConsoleColor.Cyan);
                }
                else
                {
                    L.Log(Translation.Translate("report_console_record_failed", JSONMethods.DefaultLanguage, targetPl.playerID.playerName, targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                }
            });
            return;
        PlayerNotFound:
            player.SendChat("report_player_not_found", player.Steam64.ToString(Data.Locale));
            return;
        UnknownError:
            player.SendChat("report_unknown_error");
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
            { "chat", EReportType.CHAT_ABUSE },
            { "chat racism", EReportType.CHAT_ABUSE },
            { "voice chat abuse", EReportType.VOICE_CHAT_ABUSE },
            { "voice chat", EReportType.VOICE_CHAT_ABUSE },
            { "voice chat racism", EReportType.VOICE_CHAT_ABUSE },
            { "vc abuse", EReportType.VOICE_CHAT_ABUSE },
            { "vc racism", EReportType.VOICE_CHAT_ABUSE },
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
            switch (type)
            {
                case EReportType.CUSTOM:
                case EReportType.INTENTIONAL_TEAMKILL:
                case EReportType.GREIFING_FOBS:
                case EReportType.SOLOING_VEHICLE:
                case EReportType.VOICE_CHAT_ABUSE:
                case EReportType.WASTEING_ASSETS:
                    return UCPlayer.ENameSearchType.NICK_NAME;
                case EReportType.CHAT_ABUSE:
                    return UCPlayer.ENameSearchType.CHARACTER_NAME;
                default:
                    return UCPlayer.ENameSearchType.CHARACTER_NAME;
            }
        }
        public string GetName(EReportType type)
        {
            switch (type)
            {
                default:
                case EReportType.CUSTOM:
                    return "Custom";
                case EReportType.CHAT_ABUSE:
                    return "Chat Abuse / Racism";
                case EReportType.VOICE_CHAT_ABUSE:
                    return "Voice Chat Abuse / Racism";
                case EReportType.SOLOING_VEHICLE:
                    return "Soloing Vehicle";
                case EReportType.WASTEING_ASSETS:
                    return "Wasteing Assets / Vehicle Greifing";
                case EReportType.INTENTIONAL_TEAMKILL:
                    return "Intentional Teamkilling";
                case EReportType.GREIFING_FOBS:
                    return "FOB / Friendly Structure Greifing";
            }
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
                    ToastMessage.QueueMessage(set.Next, translation, EToastMessageSeverity.INFO);
                }
            }
        }
    }
}
