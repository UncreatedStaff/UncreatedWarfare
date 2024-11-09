using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("check", "id", "wtf"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayCheckCommand : IExecutableCommand
{
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayCheckCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        VehicleSpawnerStore spawnerStore)
    {
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

        Context.Reply(_translations.VehicleBayCheck, spawn.Spawner.InstanceId, spawn.Vehicle.GetAsset()!, spawn.Vehicle.Guid);
    }
}