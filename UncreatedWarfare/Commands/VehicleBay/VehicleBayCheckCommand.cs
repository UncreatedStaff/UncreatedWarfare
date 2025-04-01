using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("check", "id", "wtf"), SubCommandOf(typeof(VehicleBayCommand))]
internal sealed class VehicleBayCheckCommand : IExecutableCommand
{
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleBayCheckCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        VehicleSpawnerService spawnerStore)
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

        if (!_spawnerStore.TryGetSpawner(buildable, out VehicleSpawner? spawner))
        {
            throw Context.Reply(_translations.SpawnNotRegistered);
        }

        Context.Reply(_translations.VehicleBayCheck, spawner.SpawnInfo.UniqueName, spawner.SpawnInfo.BuildableInstanceId, spawner.SpawnInfo.VehicleAsset.GetAsset()!, spawner.SpawnInfo.VehicleAsset.Guid);
    }
}