using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Sessions;

namespace Uncreated.Warfare.Commands;

[Command("quickwin"), SubCommandOf(typeof(WarfareDevCommand))]
internal class DebugQuickWinCommand : IExecutableCommand
{
    private readonly Layout _layout;
    public CommandContext Context { get; set; }

    public DebugQuickWinCommand(Layout layout)
    {
        _layout = layout;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        while (_layout.IsActive)
        {
            await _layout.MoveToNextPhase(token);
        }
    }
}