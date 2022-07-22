using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.ReportSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;

public class ReportCommand : Command
{
    private const string SYNTAX = "/report <\"reasons\" | player> <reason> <custom message...>";
    private const string HELP = "Use to report a player for specific actions. Use /report reasons for examples.";
    public ReportCommand() : base("report", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        // /report john greifing keeps using the mortar on the fobs 
        // /report john teamkilling teamkilled 5 teammates

        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);
        ctx.AssertArgs(2, SYNTAX);

        if (!UCWarfare.CanUseNetCall || !UCWarfare.Config.EnableReporter)
            throw ctx.Reply("report_not_connected");

        EReportType type;
        string message;
        ulong target;
        if (ctx.HasArgsExact(2))
        {
            string inPlayer = ctx.Get(0)!;

            if (ctx.MatchParameter(1, "help"))
                goto Help;
            if (ctx.MatchParameter(1, "reports", "reasons", "types"))
                goto Types;

            if (!CheckLinked(ctx.Caller))
                goto DiscordNotLinked;
            message = string.Empty;
            type = GetReportType(ctx.Get(1)!);
            if (type == EReportType.CUSTOM)
                goto Help;
            else
            {
                if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, NumberStyles.Any, Data.Locale, out target)))
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
            string inPlayer = ctx.Get(0)!;
            string arg2p;

            if (!CheckLinked(ctx.Caller))
                goto DiscordNotLinked;

            type = GetReportType(ctx.Get(1)!);
            if (type == EReportType.CUSTOM)
                arg2p = ctx.GetRange(1)!;
            else
                arg2p = ctx.GetRange(2)!;

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
                if (!UCWarfare.CanUseNetCall)
                    throw ctx.Reply("report_not_connected");
                FPlayerName targetNames = F.GetPlayerOriginalNames(target);

                if (CooldownManager.HasCooldownNoStateCheck(ctx.Caller, ECooldownType.REPORT, out Cooldown cd) && cd.data.Length > 0 && cd.data[0] is ulong ul && ul == target)
                    throw ctx.Reply("report_cooldown", targetNames.CharacterName);

                ctx.Reply("report_confirm", target.ToString(Data.Locale), targetNames.CharacterName);
                ctx.LogAction(EActionLogType.START_REPORT, string.Join(", ", ctx.Parameters));
                bool didConfirm = await CommandWaitTask.WaitForCommand(ctx.Caller, "confirm", 10000);
                await UCWarfare.ToUpdate();
                if (!didConfirm)
                {
                    ctx.Reply("report_cancelled", targetNames.CharacterName);
                    return;
                }
                if (!UCWarfare.CanUseNetCall)
                    throw ctx.Reply("report_not_connected");
                CooldownManager.StartCooldown(ctx.Caller, ECooldownType.REPORT, 3600f, target);
                Report? report;
                report = type switch
                {
                    EReportType.CHAT_ABUSE => Data.Reporter.CreateChatAbuseReport(ctx.CallerID, target, message),
                    EReportType.VOICE_CHAT_ABUSE => Data.Reporter.CreateVoiceChatAbuseReport(ctx.CallerID, target, message),
                    EReportType.SOLOING_VEHICLE => Data.Reporter.CreateSoloingReport(ctx.CallerID, target, message),
                    EReportType.WASTING_ASSETS => Data.Reporter.CreateWastingAssetsReport(ctx.CallerID, target, message),
                    EReportType.INTENTIONAL_TEAMKILL => Data.Reporter.CreateIntentionalTeamkillReport(ctx.CallerID, target, message),
                    EReportType.GREIFING_FOBS => Data.Reporter.CreateGreifingFOBsReport(ctx.CallerID, target, message),
                    EReportType.CHEATING => Data.Reporter.CreateCheatingReport(ctx.CallerID, target, message),
                    _ => Data.Reporter.CreateReport(ctx.CallerID, target, message),
                };
                if (report == null)
                {
                    ctx.Reply("report_unknown_error");
                    return;
                }
                SteamPlayer? targetPl = PlayerTool.getSteamPlayer(target);
                Data.DatabaseManager.AddReport(report);
                string typename = GetName(type);
                NotifyAdminsOfReport(targetNames, ctx.Caller.Name, report, type, typename);
                ctx.Reply("report_success_p1", targetNames.CharacterName, string.IsNullOrEmpty(message) ? "---" : message, typename);
                ctx.Reply("report_success_p2");
                L.Log(Localization.Translate("report_console", JSONMethods.DEFAULT_LANGUAGE,
                    ctx.Caller.Name.PlayerName, ctx.CallerID.ToString(Data.Locale),
                    targetNames.PlayerName, target.ToString(Data.Locale), report.Message!, typename), ConsoleColor.Cyan);
                byte[] jpgData =
                    targetPl == null || (type != EReportType.CUSTOM && type < EReportType.SOLOING_VEHICLE)
                        ? new byte[0]
                        : await SpyTask.RequestScreenshot(targetPl);
                report.JpgData = jpgData;
                L.Log(report.JpgData.Length.ToString());
                if (!UCWarfare.CanUseNetCall)
                    throw ctx.Reply("report_not_connected");
                RequestResponse res = await Reporter.NetCalls.SendReportInvocation.Request(
                    Reporter.NetCalls.ReceiveInvocationResponse, Data.NetClient!, report, targetPl != null);
                await UCWarfare.ToUpdate();
                if (targetPl != null)
                {
                    ToastMessage.QueueMessage(targetPl, new ToastMessage(Localization.Translate("report_notify_violator", targetPl, typename), EToastMessageSeverity.SEVERE));
                    targetPl.SendChat("report_notify_violator_chat_p1", typename, message);
                    targetPl.SendChat("report_notify_violator_chat_p2");
                }
                else
                {
                    if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success2 &&
                        success2 && res.Parameters[1] is string messageUrl2)
                    {
                        L.Log(
                            Localization.Translate("report_console_record", JSONMethods.DEFAULT_LANGUAGE,
                                string.Empty, "0", messageUrl2), ConsoleColor.Cyan);
                        ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report URL: " + messageUrl2, ctx.Caller);
                    }
                    else
                    {
                        L.Log(
                            Localization.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                                string.Empty, "0"), ConsoleColor.Cyan);
                        ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report did not reach the discord bot.", ctx.Caller);
                    }
                    return;
                }

                if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success &&
                    success && res.Parameters[1] is string messageUrl)
                {
                    //await UCWarfare.ToUpdate();
                    //F.SendURL(targetPl, Translation.Translate("report_popup", targetPl, typename), messageUrl);
                    L.Log(
                        Localization.Translate("report_console_record", JSONMethods.DEFAULT_LANGUAGE,
                            targetPl.playerID.playerName,
                            targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale), messageUrl),
                        ConsoleColor.Cyan);
                    ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report URL: " + messageUrl, ctx.Caller);
                }
                else
                {
                    L.Log(
                        Localization.Translate("report_console_record_failed", JSONMethods.DEFAULT_LANGUAGE,
                            targetPl.playerID.playerName,
                            targetPl.playerID.steamID.m_SteamID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report did not reach the discord bot.", ctx.Caller);
                }
            }
            catch (Exception ex)
            {
                L.LogError(ex);
            }
        });
        ctx.Defer();
        return;
    PlayerNotFound:
        throw ctx.Reply("report_player_not_found", ctx.TryGet(0, out string pl) ? pl : "null");
    DiscordNotLinked:
        throw ctx.Reply("report_discord_not_linked", ctx.CallerID.ToString(Data.Locale));
    Help:
        ctx.Reply("report_syntax");
    Types: // not returning here is intentional
        throw ctx.Reply("report_reasons");
    }

    public KeyValuePair<string, EReportType>[] types = new KeyValuePair<string, EReportType>[]
    {
        new KeyValuePair<string, EReportType>("custom",                  EReportType.CUSTOM),
        new KeyValuePair<string, EReportType>("none",                    EReportType.CUSTOM),
        new KeyValuePair<string, EReportType>("chat abuse",              EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("racism",                  EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("n word",                  EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("chat",                    EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("chat racism",             EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat abuse",        EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat",              EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat racism",       EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc abuse",                EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc racism",               EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc",                      EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("soloing",                 EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("solo",                    EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("soloing vehicles",        EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("asset waste",             EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("asset wasteing",          EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("wasteing assets",         EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("asset wasting",           EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("wasting assets",          EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("intentional teamkilling", EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("teamkilling",             EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("fob greifing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("structure greifing",      EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("base greifing",           EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("hab greifing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("greifing",                EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("fob griefing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("structure griefing",      EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("base griefing",           EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("hab griefing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("griefing",                EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("cheating",                EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacking",                 EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("wallhacks",               EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacks",                   EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("cheats",                  EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacker",                  EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("cheater",                 EReportType.CHEATING)
    };
    public UCPlayer.ENameSearchType GetNameType(EReportType type)
    {
        return type switch
        {
            EReportType.CUSTOM or EReportType.INTENTIONAL_TEAMKILL or EReportType.GREIFING_FOBS or EReportType.SOLOING_VEHICLE or EReportType.VOICE_CHAT_ABUSE or EReportType.WASTING_ASSETS or EReportType.CHEATING => UCPlayer.ENameSearchType.NICK_NAME,
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
            EReportType.WASTING_ASSETS => "Wasting Assets / Vehicle Greifing",
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
        foreach (LanguageSet set in Localization.EnumeratePermissions(EAdminType.MODERATOR))
        {
            string translation = Localization.Translate("report_notify_admin", set.Language, reporter.CharacterName, violator.CharacterName, report.Message!, typename);
            while (set.MoveNext())
            {
                ToastMessage.QueueMessage(set.Next, new ToastMessage(translation, EToastMessageSeverity.INFO));
            }
        }
    }
}
