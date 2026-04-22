using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits.UI;

namespace Uncreated.Warfare.Commands;

[Command("ui"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitUICommand : IExecutableCommand
{
    private readonly KitSelectionUI _ui;

    public required CommandContext Context { get; init; }

    public KitUICommand(KitSelectionUI ui)
    {
        _ui = ui;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        await _ui.OpenAsync(Context.Player, token);
        Context.Defer();
    }
}
