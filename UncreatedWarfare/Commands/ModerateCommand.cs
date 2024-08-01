using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Commands;

[Command("moderate", "mod", "m")]
[HelpMetadata(nameof(GetHelpMetadata))]
public sealed class ModerateCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Opens the moderation menu."
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertOnDuty();

        Context.AssertRanByPlayer();
        
        await ModerationUI.Instance.Open(Context.Player, token).ConfigureAwait(false);
        Context.Defer();
    }
}
