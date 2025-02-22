using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("printzones"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugPrintActiveZonesCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    public required CommandContext Context { get; init; }

    public DebugPrintActiveZonesCommand(ZoneStore zoneStore)
    {
        _zoneStore = zoneStore;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
        
        Context.ReplyString("Zones: " + string.Join(", ", _zoneStore.EnumerateInsideZones(Context.Player.Position).Select(x => x.Name)));
        return UniTask.CompletedTask;
    }
}