using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Commands;

[Command("moderate", "mod", "m"), MetadataFile]
internal sealed class ModerateCommand : IExecutableCommand
{
    private readonly ModerationUI _ui;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public ModerateCommand(ModerationUI ui)
    {
        _ui = ui;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        
        await _ui.Open(Context.Player, token);
        Context.Defer();
    }
}
