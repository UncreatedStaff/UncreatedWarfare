using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("respawn", "force"), SubCommandOf(typeof(VehicleBayCommand))]
internal sealed class VehicleBayRespawnCommand : IExecutableCommand
{
    private readonly VehicleService _vehicleService;
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleBayRespawnCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        VehicleService vehicleService,
        VehicleSpawnerService spawnerStore)
    {
        _vehicleService = vehicleService;
        _spawnerStore = spawnerStore;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetBuildableTarget(out IBuildable? buildable))
        {
            throw Context.Reply(_translations.NoTarget);
        }

        await UniTask.SwitchToMainThread(token);

        if (!_spawnerStore.TryGetSpawner(buildable, out VehicleSpawner? spawner))
        {
            throw Context.Reply(_translations.SpawnNotRegistered);
        }

        if (spawner.LinkedVehicle != null)
        {
            await _vehicleService.DeleteVehicleAsync(spawner.LinkedVehicle, token);
            spawner.UnlinkVehicle();
        }

        await _vehicleService.SpawnVehicleAsync(spawner, token);
        // todo: Context.LogAction(ActionLogType.VehicleBayForceSpawn, spawner.ToDisplayString());
        Context.Reply(_translations.VehicleBayForceSuccess!, spawner.SpawnInfo.VehicleAsset.GetAsset());
    }
}