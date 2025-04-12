using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Squads.UI;

namespace Uncreated.Warfare.Commands;

[Command("squads", "sq"), MetadataFile]
internal sealed class SquadsCommand : IExecutableCommand
{
    private readonly SquadMenuUI _ui;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public SquadsCommand(SquadMenuUI ui)
    {
        _ui = ui;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        _ui.OpenUI(Context.Player);

        Context.Defer();

        return UniTask.CompletedTask;
    }
}