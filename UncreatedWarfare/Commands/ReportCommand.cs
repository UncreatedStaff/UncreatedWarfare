using System;
using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("report")]
[MetadataFile(nameof(GetHelpMetadata))]
public class ReportCommand : IExecutableCommand
{
    private const string Syntax = "/report <\"reasons\" | player> <reason> <custom message...>";
    private const string Help = "Use to report a player for specific actions.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Reasons")
                {
                    Aliases = [ "reports", "types" ],
                    Description = "Lists the various report types."
                },
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Description = "Report a player for breaking the rules.",
                    Parameters =
                    [
                        new CommandParameter("Reason", typeof(string))
                        {
                            IsRemainder = false,
                            Description = "Report a player for a special reason (do <#fff>/report reasons</color> for examples).",
                            Parameters =
                            [
                                new CommandParameter("Message", typeof(string))
                                {
                                    IsRemainder = true,
                                    Description = "Report a player for breaking the rules. Message is only required for custom reports."
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
#if false
        if (Data.Reporter == null)
            throw Context.Reply(T.ReportNotConnected);

        // /report john griefing keeps using the mortar on the fobs 
        // /report john teamkilling teamkilled 5 teammates

        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);
        Context.AssertArgs(2, Syntax);

        if (!UCWarfare.CanUseNetCall || !UCWarfare.Config.EnableReporter)
            throw Context.Reply(T.ReportNotConnected);

        ReportType type;
        string message;
        ulong target;
        if (Context.HasArgsExact(2))
        {
            string inPlayer = Context.Get(0)!;

            if (Context.MatchParameter(1, "help"))
                goto Help;
            if (Context.MatchParameter(1, "reports", "reasons", "types"))
                goto Types;

            bool linked = await CheckLinked(Context.Player, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (!linked)
                goto DiscordNotLinked;

            message = string.Empty;
            type = GetReportType(Context.Get(1)!);
            if (FormattingUtility.TryParseSteamId(inPlayer, out CSteamID id) && id.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                target = id.m_SteamID;
            else
            {
                UCPlayer.NameSearch search = GetNameType(type);
                UCPlayer? temptarget = UCPlayer.FromName(inPlayer, search);
                target = temptarget == null ? Data.Reporter.RecentPlayerNameCheck(inPlayer, search) : temptarget.Steam64;
                if (target == 0)
                    goto PlayerNotFound;
            }
        }
        else
        {
            string inPlayer = Context.Get(0)!;

            bool linked = await CheckLinked(Context.Caller, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);
            if (!linked)
                goto DiscordNotLinked;

            type = GetReportType(Context.Get(1)!);
            message = type == ReportType.Custom ? Context.GetRange(1)! : Context.GetRange(2)!;

            if (FormattingUtility.TryParseSteamId(inPlayer, out CSteamID id) && id.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                target = id.m_SteamID;
            else
            {
                UCPlayer.NameSearch search = GetNameType(type);
                target = Data.Reporter.RecentPlayerNameCheck(inPlayer, search);
                if (target == 0)
                    goto PlayerNotFound;
            }
            if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, NumberStyles.Number, Data.LocalLocale, out target)))
            {
                UCPlayer.NameSearch search = GetNameType(type);
                UCPlayer? temptarget = UCPlayer.FromName(inPlayer, search);
                target = temptarget == null ? Data.Reporter.RecentPlayerNameCheck(inPlayer, search) : temptarget.Steam64;
                if (target == 0)
                    goto PlayerNotFound;
            }
        }
        
        if (!UCWarfare.CanUseNetCall)
        {
            Context.Reply(T.ReportNotConnected);
            return;
        }

        PlayerNames targetNames = await F.GetPlayerOriginalNamesAsync(target, token);
        await UniTask.SwitchToMainThread(token);

        if (CooldownManager.HasCooldownNoStateCheck(Context.Caller, CooldownType.Report, out Cooldown cd) && cd.Parameters.Length > 0 && cd.Parameters[0] is ulong ul && ul == target)
        {
            Context.Reply(T.ReportCooldown, targetNames);
            return;
        }

        Context.Reply(T.ReportConfirm, target, targetNames);
        Context.LogAction(ActionLogType.StartReport, string.Join(", ", Context.Parameters));
        bool didConfirm = await CommandWaiter.WaitAsync(Context.Caller, "confirm", 10000);
        await UCWarfare.ToUpdate();
        if (!didConfirm)
        {
            Context.Reply(T.ReportCancelled);
            return;
        }
        if (!UCWarfare.CanUseNetCall)
        {
            Context.Reply(T.ReportNotConnected);
            return;
        }
        CooldownManager.StartCooldown(Context.Caller, CooldownType.Report, 3600f, target);
        Report? report = Data.Reporter.CreateReport(Context.CallerID, target, message, type);
        if (report == null)
        {
            Context.SendUnknownError();
            return;
        }

        UCPlayer? targetPl = UCPlayer.FromID(target);
        await Data.DatabaseManager.AddReport(report, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        string typename = GetName(type);
        NotifyAdminsOfReport(targetNames, Context.Caller.Name, report, typename);
        if (!T.ReportSuccessMessage.HasLanguage(Context.LanguageInfo) || Context.IMGUI)
        {
            Context.Reply(T.ReportSuccessMessage1, targetNames, string.IsNullOrEmpty(message) ? "---" : message, typename);
            Context.Reply(T.ReportSuccessMessage2);
        }
        else
        {
            Context.Reply(T.ReportSuccessMessage, targetNames, string.IsNullOrEmpty(message) ? "---" : message, typename);
        }
        L.Log($"{Context.Caller.Name.PlayerName} ({Context.CallerID}) reported {targetNames.PlayerName} ({target}) for \"{report.Message}\" as a {typename} report.", ConsoleColor.Cyan);
        byte[] jpgData =
            targetPl == null || (type != EReportType.CUSTOM && type < EReportType.SOLOING_VEHICLE)
                ? Array.Empty<byte>()
                : await SpyTask.RequestScreenshot(targetPl.SteamPlayer);
        report.JpgData = jpgData;
        if (!UCWarfare.CanUseNetCall)
        {
            Context.Reply(T.ReportNotConnected);
            return;
        }
        RequestResponse res = await Reporter.NetCalls.SendReportInvocation.Request(
            Reporter.NetCalls.ReceiveInvocationResponse, UCWarfare.I.NetClient!, report, targetPl != null);
        await UCWarfare.ToUpdate();
        if (targetPl is { IsOnline: true })
        {
            ToastMessage.QueueMessage(targetPl, ToastMessage.Popup(T.ReportNotifyViolatorToastTitle.Translate(targetPl), T.ReportNotifyViolatorToast.Translate(targetPl, typename), T.ButtonOK.Translate(targetPl)));
            // todo remove this check once translations are available
            if (targetPl.Locale.IsDefaultLanguage && !targetPl.Save.IMGUI)
            {
                targetPl.SendChat(T.ReportNotifyViolatorMessage, typename, message);
            }
            else
            {
                targetPl.SendChat(T.ReportNotifyViolatorMessage1, typename, message);
                targetPl.SendChat(T.ReportNotifyViolatorMessage2);
            }
        }

        PlayerNames names = await F.GetPlayerOriginalNamesAsync(target, token).ConfigureAwait(false);

        if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success &&
            success && res.Parameters[1] is string messageUrl)
        {
            //await UCWarfare.ToUpdate();
            //F.SendURL(targetPl, Translation.Translate("report_popup", targetPl, typename), messageUrl);
            L.Log($"Report against {names.PlayerName} ({target}) record: \"{messageUrl}\".", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.ConfirmReport, report + ", Report URL: " + messageUrl, Context.Caller);
        }
        else
        {
            L.Log($"Report against {names.PlayerName} ({target}) failed to send to UCHB.", ConsoleColor.Cyan);
            ActionLog.Add(ActionLogType.ConfirmReport, report + ", Report did not reach the discord bot.", Context.Caller);
        }
        return;
    PlayerNotFound:
        throw Context.Reply(T.PlayerNotFound);
    DiscordNotLinked:
        Context.Reply(T.DiscordNotLinked);
        throw Context.Reply(T.DiscordNotLinked2, Context.Caller);
    Help:
        Context.SendCorrectUsage(Syntax + " - " + Help);
    Types: // not returning here is intentional
        throw Context.Reply(T.ReportReasons);
#endif
    }

    public static readonly KeyValuePair<string, ReportType>[] ReportTypeAliases =
    {
        new KeyValuePair<string, ReportType>("custom",                  ReportType.Custom),
        new KeyValuePair<string, ReportType>("none",                    ReportType.Custom),
        new KeyValuePair<string, ReportType>("chat abuse",              ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("racism",                  ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("n word",                  ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("chat",                    ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("chat racism",             ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("voice chat abuse",        ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("voice chat",              ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("voice chat racism",       ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("vc abuse",                ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("vc racism",               ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("vc",                      ReportType.ChatAbuse),
        new KeyValuePair<string, ReportType>("soloing",                 ReportType.Griefing),
        new KeyValuePair<string, ReportType>("solo",                    ReportType.Griefing),
        new KeyValuePair<string, ReportType>("soloing vehicles",        ReportType.Griefing),
        new KeyValuePair<string, ReportType>("asset waste",             ReportType.Griefing),
        new KeyValuePair<string, ReportType>("asset wasteing",          ReportType.Griefing),
        new KeyValuePair<string, ReportType>("wasteing assets",         ReportType.Griefing),
        new KeyValuePair<string, ReportType>("asset wasting",           ReportType.Griefing),
        new KeyValuePair<string, ReportType>("wasting assets",          ReportType.Griefing),
        new KeyValuePair<string, ReportType>("intentional teamkilling", ReportType.Griefing),
        new KeyValuePair<string, ReportType>("teamkilling",             ReportType.Griefing),
        new KeyValuePair<string, ReportType>("teamkill",                ReportType.Griefing),
        new KeyValuePair<string, ReportType>("tk",                      ReportType.Griefing),
        new KeyValuePair<string, ReportType>("tking",                   ReportType.Griefing),
        new KeyValuePair<string, ReportType>("intentional",             ReportType.Griefing),
        new KeyValuePair<string, ReportType>("fob greifing",            ReportType.Griefing),
        new KeyValuePair<string, ReportType>("structure greifing",      ReportType.Griefing),
        new KeyValuePair<string, ReportType>("base greifing",           ReportType.Griefing),
        new KeyValuePair<string, ReportType>("hab greifing",            ReportType.Griefing),
        new KeyValuePair<string, ReportType>("greifing",                ReportType.Griefing),
        new KeyValuePair<string, ReportType>("fob griefing",            ReportType.Griefing),
        new KeyValuePair<string, ReportType>("structure griefing",      ReportType.Griefing),
        new KeyValuePair<string, ReportType>("base griefing",           ReportType.Griefing),
        new KeyValuePair<string, ReportType>("hab griefing",            ReportType.Griefing),
        new KeyValuePair<string, ReportType>("griefing",                ReportType.Griefing),
        new KeyValuePair<string, ReportType>("cheating",                ReportType.Custom),
        new KeyValuePair<string, ReportType>("hacking",                 ReportType.Custom),
        new KeyValuePair<string, ReportType>("wallhacks",               ReportType.Custom),
        new KeyValuePair<string, ReportType>("hacks",                   ReportType.Custom),
        new KeyValuePair<string, ReportType>("cheats",                  ReportType.Custom),
        new KeyValuePair<string, ReportType>("hacker",                  ReportType.Custom),
        new KeyValuePair<string, ReportType>("cheater",                 ReportType.Custom)
    };
    public UCPlayer.NameSearch GetNameType(ReportType type)
    {
        return type switch
        {
            ReportType.Custom or ReportType.Griefing => UCPlayer.NameSearch.NickName,
            ReportType.ChatAbuse => UCPlayer.NameSearch.CharacterName,
            _ => UCPlayer.NameSearch.CharacterName,
        };
    }
    public string GetName(ReportType type)
    {
        return type switch
        {
            ReportType.Griefing => "Greifing",
            ReportType.ChatAbuse => "Chat Abuse",
            _ => "Other",
        };
    }
    public ReportType GetReportType(string input)
    {
        for (int i = 0; i < ReportTypeAliases.Length; ++i)
        {
            ref KeyValuePair<string, ReportType> type = ref ReportTypeAliases[i];
            if (type.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
                return type.Value;
        }
        return ReportType.Custom;
    }
    public async Task<bool> CheckLinked(UCPlayer player, CancellationToken token) =>
        (await Data.DatabaseManager.GetDiscordID(player.Steam64, token).ConfigureAwait(false)) != 0;
    public void NotifyAdminsOfReport(PlayerNames violator, PlayerNames reporter, Report report, string typename)
    {
        Chat.Broadcast(LanguageSet.AllStaff(), T.ReportNotifyAdmin, reporter, violator, report.Message!, typename);
    }
}
