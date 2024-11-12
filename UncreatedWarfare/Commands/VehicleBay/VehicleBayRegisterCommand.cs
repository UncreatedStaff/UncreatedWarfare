using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("register", "reg"), SubCommandOf(typeof(VehicleBayCommand))]
public class VehicleBayRegisterCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly VehicleSpawnerStore _spawnerStore;
    private readonly VehicleInfoStore _vehicleInfo;
    private readonly WarfareModule _module;
    private readonly VehicleBayCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public VehicleBayRegisterCommand(
        TranslationInjection<VehicleBayCommandTranslations> translations,
        BuildableSaver buildableSaver,
        VehicleSpawnerStore spawnerStore,
        VehicleInfoStore vehicleInfo,
        WarfareModule module)
    {
        _buildableSaver = buildableSaver;
        _spawnerStore = spawnerStore;
        _vehicleInfo = vehicleInfo;
        _module = module;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out VehicleAsset? vehicleType, out _, remainder: true))
        {
            if (Context.HasArgs(1))
                Context.Reply(_translations.InvalidInput, Context.Get(0)!);
            else
                Context.SendHelp();
            return;
        }

        if (!Context.TryGetBuildableTarget(out IBuildable? buildable))
        {
            throw Context.Reply(_translations.NoTarget);
        }

        WarfareVehicleInfo? info = _vehicleInfo.GetVehicleInfo(vehicleType.GUID);
        if (info == null)
        {
            throw Context.Reply(_translations.InvalidInput, vehicleType.ActionLogDisplay());
        }

        await _buildableSaver.SaveBuildableAsync(buildable, token);

        await UniTask.SwitchToMainThread(token);

        VehicleSpawnInfo? spawn = _spawnerStore.Spawns.FirstOrDefault(x => x.Spawner.Equals(buildable));
        if (spawn != null)
        {
            if (spawn.Vehicle.MatchAsset(vehicleType))
            {
                throw Context.Reply(_translations.SpawnAlreadyRegistered, vehicleType);
            }

            await _spawnerStore.RemoveSpawnAsync(spawn, token);
            await UniTask.SwitchToMainThread(token);

            Context.Reply(_translations.SpawnDeregistered!, spawn.Vehicle.GetAsset());
        }

        spawn = new VehicleSpawnInfo
        {
            Spawner = buildable,
            Vehicle = AssetLink.Create(vehicleType)
        };

        await _spawnerStore.AddOrUpdateSpawnAsync(spawn, token);
        Context.LogAction(ActionLogType.RegisteredSpawn,
            $"{spawn.Vehicle.ToDisplayString()} - Spawner Instance ID: {spawn.Spawner.InstanceId} ({(spawn.Spawner.IsStructure ? "STRUCTURE" : "BARRICADE")}.");

        if (_module.IsLayoutActive())
        {
            spawn.Spawner.Model.GetOrAddComponent<VehicleSpawnerComponent>().Init(spawn, info, _module.GetActiveLayout().ServiceProvider.Resolve<IServiceProvider>());
        }

        Context.Reply(_translations.SpawnRegistered, vehicleType);
    }
}