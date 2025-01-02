using DanielWillett.ModularRpcs.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Discord;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("report", "rep"), MetadataFile, SynchronizedCommand]
internal sealed class ReportCommand : IExecutableCommand
{
    private readonly ReportService _reportService;
    private readonly IRpcConnectionLifetime _rpcLifetime;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly CooldownManager _cooldownManager;
    private readonly IPlayerService _playerService;
    private readonly IConfiguration _configuration;
    private readonly ChatService _chatService;
    private readonly UserPermissionStore _userPermissionStore;
    private readonly DatabaseInterface _moderationSql;
    private readonly ReportTranslations _translations;
    private readonly AccountLinkingService _accountLinkingService;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public ReportCommand(
        ReportService reportService,
        TranslationInjection<ReportTranslations> translations,
        IRpcConnectionLifetime rpcLifetime,
        CommandDispatcher commandDispatcher,
        CooldownManager cooldownManager,
        IPlayerService playerService,
        IConfiguration configuration,
        ChatService chatService,
        DatabaseInterface moderationSql,
        UserPermissionStore userPermissionStore,
        AccountLinkingService accountLinkingService)
    {
        _reportService = reportService;
        _rpcLifetime = rpcLifetime;
        _commandDispatcher = commandDispatcher;
        _cooldownManager = cooldownManager;
        _playerService = playerService;
        _configuration = configuration;
        _chatService = chatService;
        _moderationSql = moderationSql;
        _userPermissionStore = userPermissionStore;
        _accountLinkingService = accountLinkingService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertArgs(1);
        if (!Context.TryGet(0, out CSteamID steam64, out WarfarePlayer? onlinePlayer, _reportService.SelectPlayers))
        {
            throw Context.Reply(_translations.ReportPlayerNotFound);
        }

        // it helps with testing to be able to report yourself in development
#if RELEASE
        if (steam64 == Context.CallerId)
        {
            throw Context.Reply(_translations.CannotReportSelf);
        }
#endif

        if (_cooldownManager.HasCooldown(Context.CallerId, CooldownType.Report, steam64.m_SteamID))
        {
            throw Context.Reply(_translations.ReportCooldown, onlinePlayer);
        }

        ReportType reportType = GetReportType(out int endIndex);

        if (reportType == ReportType.Custom && !Context.HasArgument(endIndex))
            throw Context.Reply(_translations.ReportReasons);

        string? message = Context.GetRange(endIndex);
        if (string.IsNullOrWhiteSpace(message))
            message = null;

        if (_rpcLifetime.ForEachRemoteConnection(_ => false) <= 0)
        {
            throw Context.Reply(_translations.ReportNotConnected);
        }

        if (await _accountLinkingService.IsInGuild(Context.CallerId, token) is GuildStatusResult.NotLinked or GuildStatusResult.NotInGuild)
        {
            throw Context.Reply(_translations.ReportNotInDiscordServer);
        }

        Context.Reply(_translations.ReportConfirm, steam64, onlinePlayer);

        CommandWaitResult result = await _commandDispatcher.WaitForCommand(
            typeof(ConfirmCommand),
            Context.Caller,
            TimeSpan.FromSeconds(10),
            CommandWaitOptions.AbortOnOtherCommandExecuted | CommandWaitOptions.BlockOriginalExecution
        );

        await UniTask.SwitchToMainThread(token);

        if (result.IsTimedOut)
        {
            if (Context.Player is { IsOnline: false })
                return;

            throw Context.Reply(_translations.ReportCancelled);
        }

        if (!result.IsSuccessfullyExecuted || Context.Player is { IsOnline: false })
            return;
        
        if (_rpcLifetime.ForEachRemoteConnection(_ => false) <= 0)
        {
            throw Context.Reply(_translations.ReportNotConnected);
        }

        Context.Reply(_translations.ReportStarted);
        (Report report, bool sent) = await _reportService.StartReport(
            steam64,
            Context.Caller.GetModerationActor(),
            message,
            reportType,
            token
        );

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (!sent)
        {
            report.Removed = true;
            report.RemovedBy = ConsoleActor.Instance;
            report.RemovedMessage = "Failed to send.";
            report.RemovedTimestamp = report.StartedTimestamp;
            await _moderationSql.AddOrUpdate(report, CancellationToken.None);
            throw Context.Reply(_translations.ReportNotConnected);
        }

        _cooldownManager.StartCooldown(Context.CallerId, CooldownType.Report, 3600f /* 1 hr */, steam64.m_SteamID);

        string reason = message ?? _translations.TranslationService.ValueFormatter.FormatEnum(report.Type, Context.Language);

        if (Context.IMGUI)
        {
            Context.Reply(_translations.ReportSuccessMessage1, onlinePlayer, report.Type, reason);
            Context.Reply(_translations.ReportSuccessMessage2);
        }
        else
        {
            Context.Reply(_translations.ReportSuccessMessage, onlinePlayer, report.Type, reason);
        }

        IConfigurationSection permSection = _configuration.GetSection("permissions");

        string staffOffDuty = permSection["staff_off_duty"] ?? throw Context.SendUnknownError();
        string staffOnDuty  = permSection["staff_on_duty" ] ?? throw Context.SendUnknownError();

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            IReadOnlyList<PermissionGroup> perms = await _userPermissionStore.GetPermissionGroupsAsync(player.Steam64, forceRedownload: false, CancellationToken.None);
            if (perms.Any(x => x.Id.Equals(staffOffDuty, StringComparison.Ordinal) || x.Id.Equals(staffOnDuty, StringComparison.Ordinal)))
            {
                _chatService.Send(player, _translations.ReportNotifyAdmin, Context.Player!, onlinePlayer, reason, report.Type);
            }
        }

        await UniTask.SwitchToMainThread(CancellationToken.None);

        if (onlinePlayer.IsOnline)
        {
            onlinePlayer.SendToast(ToastMessage.Popup(
                _translations.ReportNotifyViolatorToastTitle.Translate(onlinePlayer),
                _translations.ReportNotifyViolatorToast.Translate(reason, onlinePlayer),
                PopupUI.Okay));

            if (onlinePlayer.Save.IMGUI)
            {
                _chatService.Send(onlinePlayer, _translations.ReportNotifyViolatorMessage1, reason);
                _chatService.Send(onlinePlayer, _translations.ReportNotifyViolatorMessage2);
            }
            else
            {
                _chatService.Send(onlinePlayer, _translations.ReportNotifyViolatorMessage, reason);
            }
        }
    }

    private static readonly string[][] ReportKeywords =
    [
        // Griefing
        [ "griefing", "grief", "greifing", "greif", "tk", "tks", "teamkill", "teamkills", "cache", "fob", "griefed", "greifed" ],
        
        // ChatAbuse
        [ "nword", "chat", "abuse", "slur", "slurs", "curse", "cursing", "curses", "chatabuse" ],
    
        // VoiceChatAbuse
        [ "vc", "voicechat", "voicechatabuse", "vcabuse", "vca", "talk", "voice", "mic", "spam", "micspam", "music" ],
    
        // Cheating
        [ "cheats", "cheating", "cheat", "hacking", "hacks", "hack", "aimbot", "wallhacks", "wallhack", "wallhacking", "suspected", "esp", "wh", "walls" ]
    ];

    private ReportType GetReportType(out int endIndex)
    {
        endIndex = 2;

        if (Context.MatchParameter(1, ReportKeywords[0]))
            return ReportType.Griefing;

        if (Context.MatchParameter(1, ReportKeywords[1]))
            return ReportType.ChatAbuse;

        if (Context.MatchParameter(1, ReportKeywords[2]))
            return ReportType.VoiceChatAbuse;

        if (Context.MatchParameter(1, ReportKeywords[3]))
            return ReportType.Cheating;

        if (Context.MatchParameter(1, "custom", "other"))
            return ReportType.Custom;

        endIndex = 1;
        return ReportType.Custom;
    }
}

public class ReportTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Commands/Report";

    [TranslationData("Possible report arguments, do not translate the reasons.")]
    public readonly Translation ReportReasons = new Translation("<#9cffb3>Report reasons: -none-, \"griefing\", \"chat abuse\", \"cheating\", \"voice chat abuse\".");

    [TranslationData("Sent when someone tries to report a player that wasn't recently online.")]
    public readonly Translation ReportPlayerNotFound = new Translation("<#9cffb3>Unable to find a player with that name, you can only report online players or players that have disconnected within the last two hours.");

    [TranslationData("Sent when someone tries to report themselves.")]
    public readonly Translation CannotReportSelf = new Translation("<#9cffb3>You can not report yourself.");

    [TranslationData("Sent when the server can't communicate with the discord bot.")]
    public readonly Translation ReportNotConnected = new Translation("<#ff8c69>The report system is not available right now, please try again later.");

    [TranslationData("Sent when the reporting player is not a member of the discord server.")]
    public readonly Translation ReportNotInDiscordServer = new Translation("<#ff8c69>You must join the <#7483c4>Discord</color> server (/discord) and link your account with /link to use this feature.");

    [TranslationData("Sent when a report is confirmed to provide some feedback since the reporting process can take a few seconds.")]
    public readonly Translation ReportStarted = new Translation("<#521e62>Sending report... this may take a few seconds.");

    [TranslationData]
    public readonly Translation<IPlayer, ReportType, string> ReportSuccessMessage = new Translation<IPlayer, ReportType, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report. If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.", arg0Fmt: WarfarePlayer.FormatCharacterName);

    [TranslationData]
    public readonly Translation<IPlayer, ReportType, string> ReportSuccessMessage1 = new Translation<IPlayer, ReportType, string>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report.", arg0Fmt: WarfarePlayer.FormatCharacterName);

    [TranslationData]
    public readonly Translation ReportSuccessMessage2 = new Translation("<#c480d9>If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.");

    [TranslationData]
    public readonly Translation<IPlayer, IPlayer, string, ReportType> ReportNotifyAdmin = new Translation<IPlayer, IPlayer, string, ReportType>("<#c480d9>{0} reported {1} for <#fff>{2}</color> as a <#00ffff>{3}</color> report. Check <#c480d9>#player-reports</color> for more information.", arg0Fmt: WarfarePlayer.FormatCharacterName, arg1Fmt: WarfarePlayer.FormatCharacterName);

    [TranslationData]
    public readonly Translation<string> ReportNotifyViolatorToast = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>.\nCheck <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.", TranslationOptions.TMProUI);

    [TranslationData]
    public readonly Translation ReportNotifyViolatorToastTitle = new Translation("You Were Reported", TranslationOptions.TMProUI);

    [TranslationData]
    public readonly Translation<string> ReportNotifyViolatorMessage = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>. Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");

    [TranslationData]
    public readonly Translation<string> ReportNotifyViolatorMessage1 = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>.");

    [TranslationData]
    public readonly Translation ReportNotifyViolatorMessage2 = new Translation("<#c480d9>Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");

    [TranslationData]
    public readonly Translation<IPlayer> ReportCooldown = new Translation<IPlayer>("<#9cffb3>You've already reported {0} in the past hour.", arg0Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData]
    public readonly Translation<CSteamID, IPlayer> ReportConfirm = new Translation<CSteamID, IPlayer>("<#c480d9>Did you mean to report {1} <i><#444>{0}</color></i>? Type <#ff8c69>/confirm</color> to continue.", arg1Fmt: WarfarePlayer.FormatColoredCharacterName);

    [TranslationData]
    public readonly Translation ReportCancelled = new Translation("<#ff8c69>You didn't confirm your report in time.");
}