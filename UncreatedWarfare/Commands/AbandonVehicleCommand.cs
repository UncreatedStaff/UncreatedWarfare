using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("abandon", "av"), MetadataFile]
internal sealed class AbandonVehicleCommand : IExecutableCommand
{
    private readonly AbandonService _abandonService;
    private readonly ZoneStore _zoneStore;
    private readonly VehicleService _vehicleService;
    private readonly AbandonTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public AbandonVehicleCommand(AbandonService abandonService,
                                 TranslationInjection<AbandonTranslations> translations,
                                 ZoneStore zoneStore,
                                 VehicleService vehicleService)
    {
        _abandonService = abandonService;
        _zoneStore = zoneStore;
        _vehicleService = vehicleService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            throw Context.Reply(_translations.AbandonNoTarget);
        }

        if (!_zoneStore.IsInMainBase(vehicle.transform.position))
        {
            throw Context.Reply(_translations.AbandonNotInMain);
        }

        WarfareVehicle warfareVehicle = _vehicleService.GetVehicle(vehicle);

        if (!warfareVehicle.Info.Abandon.AllowAbandon)
        {
            throw Context.Reply(_translations.AbandonNotAllowed, vehicle);
        }

        if (!Context.Player.Equals(vehicle.lockedOwner))
        {
            throw Context.Reply(_translations.AbandonNotOwned, vehicle);
        }

        if ((float)vehicle.health / vehicle.asset.health < 0.9f)
        {
            throw Context.Reply(_translations.AbandonDamaged, vehicle);
        }

        if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
        {
            throw Context.Reply(_translations.AbandonNeedsFuel, vehicle);
        }

        if (warfareVehicle.Spawn == null || warfareVehicle.Spawn.LinkedVehicle != vehicle)
        {
            throw Context.Reply(_translations.AbandonNoSpace, vehicle);
        }

        if (await _abandonService.AbandonVehicleAsync(vehicle, respawn: true, token))
        {
            Context.Reply(_translations.AbandonSuccess, vehicle);
        }
        else
        {
            Context.SendUnknownError();
        }
    }
}