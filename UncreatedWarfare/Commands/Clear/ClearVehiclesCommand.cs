using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("vehicles", "veh", "v"), SubCommandOf(typeof(ClearCommand))]
internal sealed class ClearVehiclesCommand : IExecutableCommand
{
    private readonly VehicleService _vehicleService;
    private readonly ClearTranslations _translations;

    public required CommandContext Context { get; init; }

    public ClearVehiclesCommand(VehicleService vehicleService, TranslationInjection<ClearTranslations> translations)
    {
        _vehicleService = vehicleService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        await _vehicleService.DeleteAllVehiclesAsync(token);
        // todo respawn all vehicles

        Context.LogAction(ActionLogType.ClearVehicles);
        Context.Reply(_translations.ClearVehicles);
    }
}