using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Commands;

[Command("ammo", "am"), MetadataFile]
public class AmmoCommand : IExecutableCommand
{
    private readonly DroppedItemTracker _itemTracker;
    private readonly IPlayerService _playerService;
    private readonly AmmoCommandTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public AmmoCommand(DroppedItemTracker itemTracker, TranslationInjection<AmmoCommandTranslations> translations, IPlayerService playerService)
    {
        _itemTracker = itemTracker;
        _playerService = playerService;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();
#if false
        VehicleBay? bay = Data.Singletons.GetSingleton<VehicleBay>();
        if (bay == null || !bay.IsLoaded)
            throw Context.SendGamemodeError();

        if (Context.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            if (vehicle.lockedOwner.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                throw Context.Reply(_translations.AmmoVehicleCantRearm);

            SqlItem<VehicleData>? data = await bay.GetDataProxy(vehicle.asset.GUID, token).ConfigureAwait(false);
            if (data?.Item == null)
                throw Context.Reply(_translations.AmmoVehicleCantRearm);

            await data.Enter(token).ConfigureAwait(false);
            try
            {
                await UniTask.SwitchToMainThread(token);
                VehicleData vehicleData = data.Item;
                if (vehicleData.Metadata is { TrunkItems: not null } && vehicleData.Items.Length == 0 && vehicleData.Type is VehicleType.LogisticsGround or VehicleType.TransportAir)
                {
                    throw Context.Reply(_translations.AmmoAutoSupply);
                }

                bool isInMain = F.IsInMain(vehicle.transform.position);

                if (!Gamemode.Config.BarricadeRepairStation.TryGetGuid(out _))
                {
                    Context.ReplyString("No asset for repair station. Contact admin.");
                    throw Context.Reply(_translations.AmmoNotNearRepairStation);
                }

                if (!VehicleData.IsEmplacement(vehicleData.Type) && !isInMain)
                {
                    if (!BarricadeUtility.IsBarricadeInRange(
                            vehicle.transform.position,
                            10f,
                            Context.Player.UnturnedPlayer.quests.groupID.m_SteamID,
                            Gamemode.Config.BarricadeRepairStation,
                            horizontalDistanceOnly: false
                        ))
                    {
                        throw Context.Reply(_translations.AmmoNotNearRepairStation);
                    }
                }

                FOB? fob = Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(vehicle.transform.position, vehicle.lockedGroup.m_SteamID.GetTeam());

                if (fob == null && !isInMain)
                    throw Context.Reply(_translations.AmmoNotNearFOB);

                if (!isInMain && fob!.AmmoSupply < vehicleData.RearmCost)
                    throw Context.Reply(_translations.AmmoOutOfStock, fob.AmmoSupply, vehicleData.RearmCost);

                if (vehicle.lockedGroup.m_SteamID != 0 && vehicle.lockedGroup.m_SteamID != Context.Player.GetTeam())
                    throw Context.Reply(_translations.AmmoVehicleCantRearm);

                vehicle.TryGetComponent(out VehicleComponent? vcomp);
                if (VehicleData.IsFlyingEngine(vehicle.asset.engine) && (vehicle.isDriven ||
                    vcomp == null || Time.realtimeSinceStartup - vcomp.LastDriverTime > 1.25f) &&
                    vehicle.transform.position.y - F.GetTerrainHeightAt2DPoint(vehicle.transform.position) > 1f)
                {
                    L.LogDebug("Detected in-air vehicle.");
                    UCPlayer? lastDriver = vcomp == null ? null : UCPlayer.FromID(vcomp.LastDriver);
                    if (lastDriver != null)
                    {
                        if (lastDriver.CurrentVehicle == null)
                        {
                            Vector3 pos = lastDriver.Position;
                            if (pos.y - F.GetTerrainHeightAt2DPoint(pos) > 2f)
                            {
                                CooldownManager.StartCooldown(lastDriver, CooldownType.InteractVehicleSeats, 0.75f,
                                    vehicle);
                                L.LogDebug("Started cooldown after /ammoing outside flying vehicle.");
                            }

                            L.LogDebug("Starting passsenger cooldowns.");
                            // prevent players from switching seats
                            foreach (Passenger passenger in vehicle.passengers)
                            {
                                if (passenger.player != null && UCPlayer.FromSteamPlayer(passenger.player) is { } player)
                                    CooldownManager.StartCooldown(player, CooldownType.InteractVehicleSeats, 5f, vehicle);
                            }
                        }
                        else if (lastDriver.CurrentVehicle == vehicle)
                            throw Context.Reply(_translations.AmmoInVehicle);
                    }
                }

                if (vehicleData.Items.Length == 0)
                    throw Context.Reply(_translations.AmmoVehicleFullAlready);
                if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                    EffectUtility.TriggerEffect(effect, EffectManager.SMALL, vehicle.transform.position, true);

                foreach (Guid item in vehicleData.Items)
                    if (Assets.find(item) is ItemAsset a)
                        ItemManager.dropItem(new Item(a.id, true), Context.Player.Position, true, true, true);

                if (vcomp != null)
                    vcomp.ReloadCountermeasures();

                if (!isInMain)
                {
                    fob!.ModifyAmmo(-vehicleData.RearmCost);

                    FOBManager.ShowResourceToast(new LanguageSet(Context.Player), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(Context.Player));

                    if (vehicle.TryGetComponent(out VehicleComponent comp) && _playerService.GetOnlinePlayerOrNull(comp.LastDriver) is { } lastDriver && lastDriver.Steam64.m_SteamID != Context.CallerId.m_SteamID)
                        FOBManager.ShowResourceToast(new LanguageSet(lastDriver), ammo: -vehicleData.RearmCost, message: T.FOBResourceToastRearmVehicle.Translate(lastDriver));

                    Context.Reply(_translations.AmmoResuppliedVehicle, vehicleData, vehicleData.RearmCost, fob.AmmoSupply);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE");
                }
                else
                {
                    Context.Reply(_translations.AmmoResuppliedVehicleMain, vehicleData, vehicleData.RearmCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR VEHICLE IN MAIN");
                }
            }
            finally
            {
                data.Release();
            }
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            Context.AssertGamemode(out IKitRequests req);

            if (!Context.Player.IsOnTeam)
                throw Context.Reply(T.NotOnCaptureTeam);

            Kit? kit = await Context.Player.GetActiveKit(token).ConfigureAwait(false);
            if (kit == null)
                throw Context.Reply(T.AmmoNoKit);

            await UniTask.SwitchToMainThread(token);

            FOBManager? fobManager = Data.Singletons.GetSingleton<FOBManager>();

            int ammoCost = KitDefaults.GetAmmoCost(kit.Class);

            if (Data.Gamemode.CanRefillAmmoAt(barricade.asset))
            {
                bool isInMain = false;
                bool isCache = Gamemode.Config.BarricadeInsurgencyCache.MatchGuid(barricade.asset.GUID);
                if (fobManager?.FindFob(new UCBarricade(barricade)) is not FOB fob)
                {
                    fob = null!;
                    if (!isCache)
                    {
                        if (F.IsInMain(barricade.model.transform.position))
                        {
                            isInMain = true;
                        }
                        else
                            throw Context.Reply(_translations.AmmoNotNearFOB);
                    }
                }
                if (isInMain && FOBManager.Config.AmmoCommandCooldown > 0 && CooldownManager.HasCooldown(Context.Player, CooldownType.Ammo, out Cooldown cooldown))
                    throw Context.Reply(_translations.AmmoCooldown, cooldown);

                if (!isInMain && !isCache && fob.AmmoSupply < ammoCost)
                    throw Context.Reply(_translations.AmmoOutOfStock, fob.AmmoSupply, ammoCost);

                if (barricade.GetServersideData().group != Context.Player.GetTeam())
                    throw Context.Reply(_translations.AmmoWrongTeam);

                if (!isInMain && !isCache)
                {
                    Context.AssertCommandNotOnIsolatedCooldown();
                    Context.IsolatedCommandCooldownTime = 15f;
                }

                await _itemTracker.DestroyItemsDroppedByPlayerAsync(Context.CallerId, false, token);
                await req.KitManager.Requests.ResupplyKit(Context.Player, kit, token: token).ConfigureAwait(false);
                await UniTask.SwitchToMainThread(token);

                if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                    EffectUtility.TriggerEffect(effect, EffectManager.SMALL, Context.Player.Position, true);

                if (isInMain)
                {
                    Context.Reply(_translations.AmmoResuppliedKitMain, ammoCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT IN MAIN");

                    if (FOBManager.Config.AmmoCommandCooldown > 0)
                        CooldownManager.StartCooldown(Context.Player, CooldownType.Ammo, FOBManager.Config.AmmoCommandCooldown);
                }
                else if (isCache)
                {
                    Context.Reply(_translations.AmmoResuppliedKitMain, ammoCost);
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM CACHE");
                }
                else
                {
                    fob.ModifyAmmo(-ammoCost);
                    FOBManager.ShowResourceToast(new LanguageSet(Context.Player), ammo: -ammoCost, message: T.FOBResourceToastRearmPlayer.Translate(Context.Player));
                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BOX");
                    Context.Reply(_translations.AmmoResuppliedKit, ammoCost, fob.AmmoSupply);
                }
            }
            else if (Gamemode.Config.BarricadeAmmoBag.MatchGuid(barricade.asset.GUID))
            {
                if (barricade.model.TryGetComponent(out AmmoBagComponent ammobag))
                {
                    if (barricade.GetServersideData().group != Context.Player.GetTeam())
                        throw Context.Reply(_translations.AmmoWrongTeam);

                    if (ammobag.Ammo < ammoCost)
                        throw Context.Reply(_translations.AmmoOutOfStock, ammobag.Ammo, ammoCost);

                    await ammobag.ResupplyPlayer(Context.Player, kit, ammoCost, token).ConfigureAwait(false);
                    await UniTask.SwitchToMainThread(token);

                    if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                        EffectUtility.TriggerEffect(effect, EffectManager.SMALL, Context.Player.Position, true);

                    Context.LogAction(ActionLogType.RequestAmmo, "FOR KIT FROM BAG");

                    await _itemTracker.DestroyItemsDroppedByPlayerAsync(Context.CallerId, false, token);
                    Context.Reply(_translations.AmmoResuppliedKit, ammoCost, ammobag.Ammo);
                }
                else
                    L.LogError("ERROR: Missing AmmoBagComponent on an ammo bag");
            }
            else throw Context.Reply(_translations.AmmoNoTarget);
        }
        else throw Context.Reply(_translations.AmmoNoTarget);
#endif
    }
}

public class AmmoCommandTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Ammo Stocking";

    public readonly Translation AmmoNoTarget = new Translation("<#ffab87>Look at an <#cedcde>AMMO CRATE</color>, <#cedcde>AMMO BAG</color> or <#cedcde>VEHICLE</color> in order to resupply.");
    
    public readonly Translation<int, int> AmmoResuppliedKit = new Translation<int, int>("<#d1bda7>Resupplied kit. Consumed: <#c$ammo$>{0} AMMO</color> <#948f8a>({1} left)</color>.");
    
    public readonly Translation<int> AmmoResuppliedKitMain = new Translation<int>("<#d1bda7>Resupplied kit. Consumed: <#c$ammo$>{0} AMMO</color>.");
    
    public readonly Translation AmmoAutoSupply = new Translation("<#b3a6a2>This vehicle will <#cedcde>AUTO RESUPPLY</color> when in main. You can also use '<color=#c9bfad>/load <color=#c$build$>build</color>|<color=#c$ammo$>ammo</color> <amount></color>'.");
    
    public readonly Translation AmmoNotNearFOB = new Translation("<#b3a6a2>This ammo crate is not built on a friendly FOB.");
    
    public readonly Translation<int, int> AmmoOutOfStock = new Translation<int, int>("<#b3a6a2>Insufficient ammo. Required: <#c$ammo$>{0}/{1} AMMO</color>.");
    
    public readonly Translation AmmoNoKit = new Translation("<#b3a6a2>You don't have a kit yet. Go request one from the armory in your team's headquarters.");
    
    public readonly Translation AmmoWrongTeam = new Translation("<#b3a6a2>You cannot rearm with enemy ammunition.");
    
    public readonly Translation<Cooldown> AmmoCooldown = new Translation<Cooldown>("<#b7bab1>More <#cedcde>AMMO</color> arriving in: <color=#de95a8>{0}</color>", arg0Fmt: Cooldown.FormatTimeShort);
    
    public readonly Translation AmmoNotRifleman = new Translation("<#b7bab1>You must be a <#cedcde>RIFLEMAN</color> in order to place this <#cedcde>AMMO BAG</color>.");
    
    public readonly Translation AmmoNotNearRepairStation = new Translation("<#b3a6a2>Your vehicle must be next to a <#cedcde>REPAIR STATION</color> in order to rearm.");
#if false

    public readonly Translation<VehicleData, int, int> AmmoResuppliedVehicle = new Translation<VehicleData, int, int>("<#d1bda7>Resupplied {0}. Consumed: <#c$ammo$>{1} AMMO</color> <#948f8a>({2} left)</color>.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation<VehicleData, int> AmmoResuppliedVehicleMain = new Translation<VehicleData, int>("<#d1bda7>Resupplied {0}. Consumed: <#c$ammo$>{1} AMMO</color>.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation AmmoVehicleCantRearm = new Translation("<#d1bda7>You cannot ressuply this vehicle.");
    
    public readonly Translation AmmoInVehicle = new Translation("<#d1bda7>You cannot ressuply this vehicle while flying, try exiting the vehicle.");
    
    public readonly Translation<VehicleData> AmmoVehicleFullAlready = new Translation<VehicleData>("<#b3a6a2>Your {0} does not need to be resupplied.", arg0Fmt: VehicleData.FormatColoredName);
    
    public readonly Translation<VehicleData> AmmoVehicleNotNearRepairStation = new Translation<VehicleData>("<#b3a6a2>Your {0} must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.", arg0Fmt: VehicleData.FormatColoredName);
#endif
}