using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

[Command("abandon", "av")]
[HelpMetadata(nameof(GetHelpMetadata))]
public class AbandonCommand : IExecutableCommand
{
    private readonly ZoneStore _zoneStore;
    private readonly VehicleInfoStore _vehicleInfo;
    private readonly AbandonService _abandonService;
    private readonly AbandonTranslations _translations;

    private const string Syntax = "/abandon | /av";
    private const string Help = "If you no longer want to use your vehicle, you can return it to the vehicle pool.";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public AbandonCommand(TranslationInjection<AbandonTranslations> translations, ZoneStore zoneStore, VehicleInfoStore vehicleInfo, AbandonService abandonService)
    {
        _zoneStore = zoneStore;
        _vehicleInfo = vehicleInfo;
        _abandonService = abandonService;
        _translations = translations.Value;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "If you no longer want to use your vehicle, you can return it to the vehicle pool."
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        if (!_zoneStore.IsInMainBase(Context.Player))
            throw Context.Reply(_translations.AbandonNotInMain);

        if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
            throw Context.Reply(_translations.AbandonNoTarget);
        
        WarfareVehicleInfo? vehicleData = _vehicleInfo.GetVehicleInfo(vehicle.asset.GUID);
        
        if (vehicleData == null)
            throw Context.Reply(_translations.AbandonNoTarget);

        if (!vehicleData.Abandon.AllowAbandon)
            throw Context.Reply(_translations.AbandonNotAllowed);

        if (vehicle.lockedOwner.m_SteamID != Context.CallerId.m_SteamID)
            throw Context.Reply(_translations.AbandonNotOwned, vehicle);

        if ((float)vehicle.health / vehicle.asset.health < 0.9f)
            throw Context.Reply(_translations.AbandonDamaged, vehicle);

        if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
            throw Context.Reply(_translations.AbandonNeedsFuel, vehicle);

        if (!vehicle.TryGetComponent(out VehicleComponent vehicleComponent) || vehicleComponent.Spawn == null)
            throw Context.Reply(_translations.AbandonNoSpace, vehicle);

        if (await _abandonService.AbandonVehicle(vehicle, true, token))
            Context.Reply(_translations.AbandonSuccess, vehicle);
        else
            throw Context.SendUnknownError();
    }
}