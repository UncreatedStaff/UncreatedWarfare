using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("dumpvehicles"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DumpVehicleSpawner : IExecutableCommand
{
    public required CommandContext Context { get; init; }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        VehicleSpawner.DumpVehicleSpawner();

        throw Context.Defer();
    }
}