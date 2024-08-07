using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("god")]
[MetadataFile(nameof(GetHelpMetadata))]
public class GodCommand : IExecutableCommand
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
            Description = "Toggles your ability to take damage."
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, "/god - Toggles your ability to take damage.");

        Context.AssertRanByPlayer();

        Context.AssertOnDuty();

        Context.Player.GodMode = !Context.Player.GodMode;

        if (Context.Player.GodMode)
        {
            Context.Player.UnturnedPlayer.life.sendRevive();
            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(Context.Player);
            Context.Reply(T.GodModeEnabled);
        }
        else
        {
            Context.Reply(T.GodModeDisabled);
        }

        return default;
    }
}