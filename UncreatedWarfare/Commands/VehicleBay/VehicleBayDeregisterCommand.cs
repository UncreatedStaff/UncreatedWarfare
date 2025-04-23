using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("deregister", "dereg", "unregister", "unreg"), SubCommandOf(typeof(VehicleBayCommand))]
internal sealed class VehicleBayDeregisterCommand : IExecutableCommand
{
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleBayDeregisterCommand(
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
        VehicleSpawner? spawner;
        if (Context.TryGetRange(0, out string? vbName))
        {
            await UniTask.SwitchToMainThread(token);

            if (!_spawnerStore.TryGetSpawner(vbName, out spawner))
            {
                throw Context.Reply(_translations.SpawnNotRegistered);
            }
        }
        else
        {
            if (!Context.TryGetBuildableTarget(out IBuildable? buildable))
            {
                throw Context.Reply(_translations.NoTarget);
            }

            await UniTask.SwitchToMainThread(token);

            if (!_spawnerStore.TryGetSpawner(buildable, out spawner))
            {
                throw Context.Reply(_translations.SpawnNotRegistered);
            }
        }

        await _spawnerStore.DeregisterSpawner(spawner, token);

        List<IBuildable> signs = spawner.Signs.ToList();

        await UniTask.SwitchToMainThread(token);

        foreach (IBuildable sign in signs)
        {
            if (sign.GetDrop<BarricadeDrop>().interactable is InteractableSign s)
            {
                BarricadeManager.ServerSetSignText(s, string.Empty);
            }
        }

        // todo: Context.LogAction(ActionLogType.DeregisteredSpawn, spawner.ToDisplayString());
        Context.Reply(_translations.SpawnDeregistered!, spawner.SpawnInfo.UniqueName, spawner.VehicleInfo.VehicleAsset.GetAsset());
    }
}