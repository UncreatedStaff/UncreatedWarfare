using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.StrategyMaps;

namespace Uncreated.Warfare.Commands;

[Command("badtacks"), SubCommandOf(typeof(StructureDestroyCommand))]
internal sealed class StructureDestroyUnlinkedTacksCommand : IExecutableCommand
{
    private readonly StrategyMapManager _strategyMapManager;

    public required CommandContext Context { get; init; }

    public StructureDestroyUnlinkedTacksCommand(StrategyMapManager strategyMapManager)
    {
        _strategyMapManager = strategyMapManager;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        foreach (StrategyMap map in _strategyMapManager.StrategyMaps)
        {
            map.DestroyOldMapTacks();
        }

        throw Context.ReplyString("Destroyed all unreferenced map tacks.");
    }
}