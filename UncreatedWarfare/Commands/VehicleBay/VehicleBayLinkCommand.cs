using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.Spawners;

namespace Uncreated.Warfare.Commands;

[Command("link"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayLinkCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayLinkCommand(
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

        // if looking at a sign
        if (!buildable.IsStructure && buildable.GetDrop<BarricadeDrop>() is { interactable: InteractableSign } signDrop)
        {
            VehicleSpawner? spawnerFromSign = VehicleSpawner.EndLinkingSign(Context.Player);
                
            if (spawnerFromSign == null)
                throw Context.Reply(_translations.SpawnNotRegistered);

            await UniTask.SwitchToMainThread(token);

            if (!spawnerFromSign.Signs.Any(s => s.Equals(buildable)))
                spawnerFromSign.Signs.Add(buildable);
            spawnerFromSign.SpawnInfo.SignInstanceIds = spawnerFromSign.Signs.Select(s => s.InstanceId).ToList();

            await _spawnerStore.SpawnerStore.AddOrUpdateSpawnAsync(spawnerFromSign.SpawnInfo);

            await UniTask.SwitchToMainThread(token);

            Context.Reply(_translations.VehicleBayLinkFinished, spawnerFromSign.VehicleInfo.VehicleAsset.GetAsset()!);
            Context.LogAction(ActionLogType.LinkedVehicleBaySign, spawnerFromSign.ToDisplayString());

            // updates sign instance via the SignTextChanged event
            BarricadeUtility.SetServersideSignText((BarricadeDrop)buildable.Drop, spawnerFromSign.ServerSignText);

            await _buildableSaver.SaveBuildableAsync(buildable, token);

        }
        // if looking at a spawner
        else if (_spawnerStore.TryGetSpawner(buildable, out VehicleSpawner? spawner))
        {
            VehicleSpawner.StartLinkingSign(spawner, Context.Player);
            Context.Reply(_translations.VehicleBayLinkStarted);
        }
        else
        {
            Context.Logger.LogConditional("Buildable is not a registered spawner.");
            Context.Reply(_translations.SpawnNotRegistered);
        }
    }
}