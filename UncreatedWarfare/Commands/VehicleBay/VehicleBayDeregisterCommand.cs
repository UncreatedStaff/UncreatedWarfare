using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("deregister", "dereg", "unregister", "unreg"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayDeregisterCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayDeregisterCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        BuildableSaver buildableSaver,
        VehicleSpawnerStore spawnerStore)
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

        VehicleSpawnInfo? spawn = _spawnerStore.Spawns.FirstOrDefault(x => x.Spawner.Equals(buildable));
        if (spawn == null)
        {
            throw Context.Reply(_translations.SpawnNotRegistered);
        }

        await _spawnerStore.RemoveSpawnAsync(spawn, token);
        await _buildableSaver.DiscardBuildableAsync(buildable, token);

        List<IBuildable> signs = spawn.SignInstanceIds.ToList();
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

        Context.LogAction(ActionLogType.DeregisteredSpawn,
            $"{spawn.Vehicle.ToDisplayString()} - Spawner Instance ID: {spawn.Spawner.InstanceId} ({(spawn.Spawner.IsStructure ? "STRUCTURE" : "BARRICADE")}.");
        Context.Reply(_translations.SpawnDeregistered!, spawn.Vehicle.GetAsset());
    }
}