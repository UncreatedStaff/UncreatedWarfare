using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("respawn", "force"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayRespawnCommand : IExecutableCommand
{
    private readonly VehicleService _vehicleService;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayRespawnCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        VehicleService vehicleService,
        VehicleSpawnerStore spawnerStore)
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

        VehicleSpawnInfo? spawn = _spawnerStore.Spawns.FirstOrDefault(x => x.Spawner.Equals(buildable));
        if (spawn == null)
        {
            throw Context.Reply(_translations.SpawnNotRegistered);
        }

        if (spawn.LinkedVehicle != null)
        {
            await _vehicleService.DeleteVehicleAsync(spawn.LinkedVehicle, token);
            spawn.UnlinkVehicle();
        }

        await _vehicleService.SpawnVehicleAsync(spawn, token);
        Context.LogAction(ActionLogType.VehicleBayForceSpawn,
            $"{spawn.Vehicle.ToDisplayString()} - Spawner Instance ID: {spawn.Spawner.InstanceId} ({(spawn.Spawner.IsStructure ? "STRUCTURE" : "BARRICADE")}.");
        Context.Reply(_translations.VehicleBayForceSuccess!, spawn.Vehicle.GetAsset());
    }
}