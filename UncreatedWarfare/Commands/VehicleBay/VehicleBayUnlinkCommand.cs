using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("unlink"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayUnlinkCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayUnlinkCommand(
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

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? barricade) || barricade.interactable is not InteractableSign sign)
        {
            throw Context.Reply(_translations.NoTarget);
        }

        await UniTask.SwitchToMainThread(token);

        IBuildable buildable = new BuildableBarricade(barricade);

        bool anyUpdates = false;
        VehicleAsset? firstUnlink = null;
        foreach (VehicleSpawnInfo spawner in _spawnerStore.Spawns)
        {
            int index = spawner.Signs.IndexOf(buildable);
            if (index == -1)
            {
                continue;
            }

            spawner.Signs.RemoveAt(index);
            anyUpdates = true;
            firstUnlink ??= spawner.Vehicle.GetAsset();
        }

        if (!anyUpdates)
        {
            throw Context.Reply(_translations.SpawnNotRegistered);
        }

        BarricadeManager.ServerSetSignText(sign, string.Empty);
        await _buildableSaver.DiscardBuildableAsync(buildable, token);
        await _spawnerStore.SaveAsync(token);
        Context.Reply(_translations.VehicleBayUnlinked, firstUnlink!);
    }
}