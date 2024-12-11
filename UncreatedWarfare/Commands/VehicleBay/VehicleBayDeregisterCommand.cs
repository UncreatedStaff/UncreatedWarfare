using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("deregister", "dereg", "unregister", "unreg"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayDeregisterCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayDeregisterCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        BuildableSaver buildableSaver,
        VehicleSpawnerService spawnerStore)
    {
        _buildableSaver = buildableSaver;
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

        await _spawnerStore.DeregisterSpawner(spawner, token);
        await _buildableSaver.DiscardBuildableAsync(buildable, token);

        List<IBuildable> signs = spawner.Signs.ToList();
        foreach (IBuildable sign in signs)
        {
            await _buildableSaver.DiscardBuildableAsync(sign, token);
        }

        await UniTask.SwitchToMainThread(token);
        foreach (IBuildable sign in signs)
        {
            if (sign.GetDrop<BarricadeDrop>().interactable is InteractableSign s)
            {
                BarricadeManager.ServerSetSignText(s, string.Empty);
            }
        }

        Context.LogAction(ActionLogType.DeregisteredSpawn, spawner.ToDisplayString());
        Context.Reply(_translations.SpawnDeregistered!, spawner.SpawnInfo.UniqueName, spawner.VehicleInfo.VehicleAsset.GetAsset());
    }
}