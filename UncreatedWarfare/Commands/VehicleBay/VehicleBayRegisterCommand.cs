using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Commands;

[Command("register", "reg"), SubCommandOf(typeof(VehicleBayCommand))]
internal sealed class VehicleBayRegisterCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerService _spawnerStore;
    private readonly VehicleInfoStore _vehicleInfo;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public VehicleBayRegisterCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        BuildableSaver buildableSaver,
        VehicleSpawnerService spawnerService,
        VehicleInfoStore vehicleInfo)
    {
        _buildableSaver = buildableSaver;
        _spawnerStore = spawnerService;
        _vehicleInfo = vehicleInfo;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out string? uniqueName))
        {
            Context.SendHelp();
            return;
        }
        if (!Context.TryGet(1, out VehicleAsset? vehicleType, out _, remainder: true))
        {
            if (Context.HasArgs(2))
                Context.Reply(_translations.InvalidVehicleAsset, Context.GetRange(1)!);
            else
                Context.SendHelp();
            return;
        }

        if (!Context.TryGetBuildableTarget(out IBuildable? buildable))
        {
            throw Context.Reply(_translations.NoTarget);
        }

        WarfareVehicleInfo? vehicleInfo = _vehicleInfo.GetVehicleInfo(vehicleType.GUID);
        if (vehicleInfo == null)
        {
            throw Context.Reply(_translations.VehicleNotRegistered, vehicleType.ActionLogDisplay());
        }

        await _buildableSaver.SaveBuildableAsync(buildable, token);

        await UniTask.SwitchToMainThread(token);

        if (_spawnerStore.TryGetSpawner(uniqueName, out _))
        {
            throw Context.Reply(_translations.NameNotUnique, uniqueName);
        }

        if (_spawnerStore.TryGetSpawner(buildable, out _))
        {
            throw Context.Reply(_translations.SpawnAlreadyRegistered, vehicleType);
        }

        VehicleSpawner newSpawner = await _spawnerStore.RegisterNewSpawner(buildable, vehicleInfo, uniqueName, token);

        Context.LogAction(ActionLogType.RegisteredSpawn, newSpawner.ToDisplayString());

        Context.Reply(_translations.SpawnRegistered, newSpawner.SpawnInfo.UniqueName, vehicleType);
    }
}