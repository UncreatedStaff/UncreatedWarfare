using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("link"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayLinkCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayLinkCommand(
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
            if (buildable.IsStructure || buildable.GetDrop<BarricadeDrop>() is not { interactable: InteractableSign } drop)
                throw Context.Reply(_translations.SpawnNotRegistered);

            VehicleSpawnInfo? info = VehicleSpawnerComponent.EndLinkingSign(Context.Player);
                
            if (info == null)
                throw Context.Reply(_translations.SpawnNotRegistered);

            await _buildableSaver.SaveBuildableAsync(buildable, token);

            await UniTask.SwitchToMainThread(token);

            info.SignInstanceIds.Add(buildable);
            await _spawnerStore.AddOrUpdateSpawnAsync(info, token);

            await UniTask.SwitchToMainThread(token);

            // updates sign instance via the SignTextChanged event
            BarricadeUtility.SetServersideSignText(drop, "vbs_" + info.Vehicle.Guid.ToString("N", CultureInfo.InvariantCulture));

            Context.Reply(_translations.VehicleBayLinkFinished, info.Vehicle.GetAsset()!);
            Context.LogAction(ActionLogType.LinkedVehicleBaySign,
                $"{drop.asset.ActionLogDisplay()} ID: {drop.instanceID} - Spawner Instance ID: {info.Spawner.InstanceId} ({(info.Spawner.IsStructure ? "STRUCTURE" : "BARRICADE")}");
        }
        else if (spawn.Spawner.Model.TryGetComponent(out VehicleSpawnerComponent component))
        {
            VehicleSpawnerComponent.StartLinkingSign(component, Context.Player);
            Context.Reply(_translations.VehicleBayLinkStarted);
        }
        else
        {
            Context.Reply(_translations.SpawnNotRegistered);
        }
    }
}