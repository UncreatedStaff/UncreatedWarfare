using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("shutdown"), Priority(1), MetadataFile]
internal sealed class ShutdownCommand : IExecutableCommand
{
    private readonly WarfareLifetimeComponent _appLifetime;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }
    
    public ShutdownCommand(WarfareLifetimeComponent appLifetime)
    {
        _appLifetime = appLifetime;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.Defer();

        string shutdownReason = $"Intentional shutdown by {(Context.Player == null ? "an operator" : Context.Player.Names.GetDisplayNameOrPlayerName())}";
        if (!Context.HasArgs(1) || Context.MatchParameter(0, "instant", "inst"))
        {
            // shutdown [inst] [reason]
            Context.Logger.LogInformation("Shutting down.");
            _appLifetime.QueueShutdownInstant(Context.GetRange(1) ?? shutdownReason);
        }
        else if (Context.MatchParameter(0, "after"))
        {
            // shutdown after [reason]
            Context.Logger.LogInformation("Shutting down after layout ends.");
            _appLifetime.QueueShutdownAtLayoutEnd(Context.GetRange(1) ?? shutdownReason);
        }
        else if (Context.MatchParameter(0, "cancel"))
        {
            // shutdown cancel
            if (_appLifetime.QueuedShutdownType == ShutdownMode.None)
            {
                throw Context.ReplyString("A shutdown is not queued.");
            }

            _appLifetime.CancelShutdown();
            Context.Logger.LogInformation("Cancelled queued shutdown.");
        }
        else
        {
            TimeSpan ts = Context.HasArgument(0) ? FormattingUtility.ParseTimespan(Context.Get(0)!) : default;
            if (ts.Ticks == 0)
            {
                // shutdown [reason]
                if (Context.HasArgument(0))
                    shutdownReason = Context.GetRange(0)!;

                Context.Logger.LogInformation("Shutting down.");
                _appLifetime.QueueShutdownInstant(Context.GetRange(0) ?? shutdownReason);
                return UniTask.CompletedTask;
            }

            // shutdown time [reason]
            if (Context.HasArgument(1))
            {
                shutdownReason = Context.GetRange(1)!;
            }

            Context.Logger.LogInformation($"Shutting down after time: {FormattingUtility.ToTimeString(ts)}.");
            _appLifetime.QueueShutdownInTime(ts, shutdownReason);
        }

        return UniTask.CompletedTask;
    }
}

internal sealed class ShutdownTranslations : PropertiesTranslationCollection
{
    /// <inheritdoc />
    protected override string FileName => "Shutdowns";

    [TranslationData("Sent when the a shutdown is scheduled after the current game ends.")]
    public readonly Translation<string, TimeSpan> ShutdownBroadcastAfterGame = new Translation<string, TimeSpan>("<#00ffcc>A shutdown has been scheduled after this game because: \"<#6699ff>{0}</color>\". Time left: <#ddd>{1}</color>.", arg1Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Sent occasionally when the server will shutdown after the current game ends.")]
    public readonly Translation<string, TimeSpan> ShutdownBroadcastAfterGameReminder = new Translation<string, TimeSpan>("<#00ffcc>A shutdown is scheduled to occur after this game because: \"<#6699ff>{0}</color>\". Time left: <#ddd>{1}</color>.", arg1Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Sent when a shutdown is cancelled that was previously scheduled.")]
    public readonly Translation ShutdownBroadcastCancelled = new Translation("<#00ffcc>The scheduled shutdown has been canceled.");

    [TranslationData("Sent when the a shutdown is scheduled after a certain amount of time.")]
    public readonly Translation<TimeSpan, string> ShutdownBroadcastTime = new Translation<TimeSpan, string>("<#00ffcc>A shutdown has been scheduled in <#ddd>{0}</color> because: \"<color=#6699ff>{1}</color>\".", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Sent occasionally when the server will shutdown after a certain amount of time.")]
    public readonly Translation<TimeSpan, string> ShutdownBroadcastTimeReminder = new Translation<TimeSpan, string>("<#00ffcc>A shutdown has been scheduled in <#ddd>{0}</color> because: \"<color=#6699ff>{1}</color>\".", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));
}