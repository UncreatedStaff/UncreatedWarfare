using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Commands;

[Command("mute")]
[MetadataFile(nameof(GetHelpMetadata))]
public class MuteCommand : IExecutableCommand
{
#if false
    private const string SYNTAX = "/mute <voice|text|both> <name or steam64> <permanent | duration in minutes> <reason...>";
    private const string HELP = "Mute players in either voice chat or text chat.";
#endif

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Mute players in either voice chat or text chat.",
            Parameters =
            [
                new CommandParameter("Type", "Voice", "Text", "Both")
                {
                    ChainDisplayCount = 4,
                    Parameters =
                    [
                        new CommandParameter("Player", typeof(IPlayer))
                        {
                            Parameters =
                            [
                                new CommandParameter("Duration", typeof(TimeSpan), "Permanent")
                                {
                                    Parameters =
                                    [
                                        new CommandParameter("Reason", typeof(string))
                                        {
                                            IsRemainder = true
                                        }
                                    ]
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
        Context.AssertHelpCheck(0, SYNTAX + HELP);
        Context.AssertHelpCheck(1, SYNTAX + HELP);

        if (!Context.HasArgs(4))
            throw Context.SendCorrectUsage(SYNTAX);

        MuteType type = Context.MatchParameter(0, "voice")
            ? MuteType.Voice
            : (Context.MatchParameter(0, "text")
                ? MuteType.Text
                : (Context.MatchParameter(0, "both")
                    ? MuteType.Both
                    : throw Context.SendCorrectUsage(SYNTAX)));

        int duration = Util.ParseTime(Context.Get(2)!);

        if (duration < -1 || duration == 0)
            throw Context.Reply(T.InvalidTime);

        if (!Context.TryGet(1, out ulong targetId, out _))
            throw Context.Reply(T.PlayerNotFound);

        try
        {
            await OffenseManager.MutePlayerAsync(targetId, Context.CallerID, type, duration, Context.GetRange(3)!, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            L.LogError("Error muting " + targetId + ".");
            L.LogError(ex);
        }

        Context.Defer();
#endif
    }
}
[Translatable("Mute Severity")]
[Flags]
public enum MuteType : byte
{
    None = 0,
    [Translatable(Languages.ChineseSimplified, "语音交流")]
    [Translatable("Voice Chat")]
    Voice = 1,
    [Translatable(Languages.ChineseSimplified, "文字交流")]
    [Translatable("Text Chat")]
    Text = 2,
    [Translatable(Languages.ChineseSimplified, "语音和文字交流")]
    [Translatable("Voice and Text Chat")]
    Both = Voice | Text
}