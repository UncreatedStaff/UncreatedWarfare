using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Commands;

[Command("moderate", "mod", "m")]
[MetadataFile(nameof(GetHelpMetadata))]
public sealed class ModerateCommand : IExecutableCommand
{
    private readonly ModerationUI _ui;

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

    public ModerateCommand(ModerationUI ui)
    {
        _ui = ui;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertOnDuty();

        Context.AssertRanByPlayer();
        
        await _ui.Open(Context.Player, token);
        Context.Defer();
    }
}
