using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("badtacks"), SubCommandOf(typeof(StructureDestroyCommand))]
internal sealed class StructureDestroyUnlinkedTacksCommand : IExecutableCommand
{
    private readonly StrategyMapManager _strategyMapManager;
    private readonly ZoneStore _globalZoneStore;

    public required CommandContext Context { get; init; }

    public StructureDestroyUnlinkedTacksCommand(StrategyMapManager strategyMapManager, ZoneStore globalZoneStore)
    {
        _strategyMapManager = strategyMapManager;
        _globalZoneStore = globalZoneStore;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Zone? warRoom = _globalZoneStore.EnumerateInsideZones(Context.Player.Position, ZoneType.WarRoom).FirstOrDefault();
        if (warRoom == null)
        {
            throw Context.Reply(Context.CommonTranslations.NotInWarRoom);
        }

        foreach (StrategyMap map in _strategyMapManager.StrategyMaps)
        {
            map.DestroyOldMapTacks();
        }

        throw Context.ReplyString("Destroyed all unreferenced map tacks.");
    }
}