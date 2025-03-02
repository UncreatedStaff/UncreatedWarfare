using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("vehicles", "veh", "v"), SubCommandOf(typeof(ClearCommand))]
internal sealed class ClearVehiclesCommand : IExecutableCommand
{
    private readonly VehicleService _vehicleService;
    private readonly VehicleSpawnerService _spawners;
    private readonly ClearTranslations _translations;

    public required CommandContext Context { get; init; }

    public ClearVehiclesCommand(VehicleService vehicleService, VehicleSpawnerService spawners, TranslationInjection<ClearTranslations> translations)
    {
        _vehicleService = vehicleService;
        _spawners = spawners;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        await _vehicleService.DeleteAllVehiclesAsync(token);

        foreach (VehicleSpawner spawner in _spawners.Spawners)
        {
            await _vehicleService.SpawnVehicleAsync(spawner, token);
        }
        // todo respawn all vehicles

        Context.LogAction(ActionLogType.ClearVehicles);
        Context.Reply(_translations.ClearVehicles);
    }
}