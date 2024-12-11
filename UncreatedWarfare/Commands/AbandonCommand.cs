//using Uncreated.Warfare.Components;
//using Uncreated.Warfare.Interaction.Commands;
//using Uncreated.Warfare.Translations;
//using Uncreated.Warfare.Vehicles;
//using Uncreated.Warfare.Zones;

//namespace Uncreated.Warfare.Commands;

//[Command("abandon", "av"), MetadataFile]
//internal sealed class AbandonCommand : IExecutableCommand
//{
//    private readonly ZoneStore _zoneStore;
//    private readonly VehicleInfoStore _vehicleInfo;
//    private readonly AbandonService _abandonService;
//    private readonly AbandonTranslations _translations;

//    /// <inheritdoc />
//    public required CommandContext Context { get; init; }

//    public AbandonCommand(TranslationInjection<AbandonTranslations> translations, ZoneStore zoneStore, VehicleInfoStore vehicleInfo, AbandonService abandonService)
//    {
//        _zoneStore = zoneStore;
//        _vehicleInfo = vehicleInfo;
//        _abandonService = abandonService;
//        _translations = translations.Value;
//    }

//    /// <inheritdoc />
//    public async UniTask ExecuteAsync(CancellationToken token)
//    {
//        Context.AssertRanByPlayer();

//        if (!_zoneStore.IsInMainBase(Context.Player))
//            throw Context.Reply(_translations.AbandonNotInMain);

//        if (!Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
//            throw Context.Reply(_translations.AbandonNoTarget);
        
//        WarfareVehicleInfo? vehicleData = _vehicleInfo.GetVehicleInfo(vehicle.asset.GUID);
        
//        if (vehicleData == null)
//            throw Context.Reply(_translations.AbandonNoTarget);

//        if (!vehicleData.Abandon.AllowAbandon)
//            throw Context.Reply(_translations.AbandonNotAllowed);

//        if (vehicle.lockedOwner.m_SteamID != Context.CallerId.m_SteamID)
//            throw Context.Reply(_translations.AbandonNotOwned, vehicle);

//        if ((float)vehicle.health / vehicle.asset.health < 0.9f)
//            throw Context.Reply(_translations.AbandonDamaged, vehicle);

//        if ((float)vehicle.fuel / vehicle.asset.fuel < 0.9f)
//            throw Context.Reply(_translations.AbandonNeedsFuel, vehicle);

//        if (!vehicle.TryGetComponent(out VehicleComponent vehicleComponent) || vehicleComponent.Spawn == null)
//            throw Context.Reply(_translations.AbandonNoSpace, vehicle);

//        if (await _abandonService.AbandonVehicle(vehicle, true, token))
//            Context.Reply(_translations.AbandonSuccess, vehicle);
//        else
//            throw Context.SendUnknownError();
//    }
//}