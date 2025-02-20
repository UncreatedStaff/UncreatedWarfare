using Microsoft.Extensions.Configuration;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Tweaks;

internal sealed class VehicleRestrictions :
    IEventListener<EquipUseableRequested>,
    IEventListener<ExitVehicleRequested>,
    IEventListener<EnterVehicleRequested>,
    IEventListener<VehicleSwapSeatRequested>,
    IEventListener<VehicleDespawned>
{
    private const float MaxHeightToExitVehicle = 50f;

    private readonly IAssetLink<ItemGunAsset>[]? _whitelistedGuns;
    private readonly ChatService _chatService;
    private readonly PlayersTranslations _translations;
    private readonly CooldownManager _cooldownManager;

    public VehicleRestrictions(AssetConfiguration assetConfig, ChatService chatService, TranslationInjection<PlayersTranslations> translations, CooldownManager cooldownManager)
    {
        _chatService = chatService;
        _cooldownManager = cooldownManager;
        _translations = translations.Value;
        _whitelistedGuns = assetConfig.GetSection("Items:AircraftWhitelistedGuns").Get<IAssetLink<ItemGunAsset>[]>();
    }

    /// <summary>
    /// Prevent equipping guns with projectiles in air vehicles.
    /// </summary>
    public void HandleEvent(EquipUseableRequested e, IServiceProvider serviceProvider)
    {
        if (e.Asset is not ItemGunAsset gun || gun.projectile is null)
            return;

        if (_whitelistedGuns.ContainsAsset(gun))
            return;

        InteractableVehicle? vehicle = e.Player.UnturnedPlayer.movement.getVehicle();
        if (vehicle == null || !vehicle.asset.engine.IsFlyingEngine() || e.Player.UnturnedPlayer.movement.getVehicleSeat()?.turret?.itemID == gun.id)
            return;

        _chatService.Send(e.Player, _translations.ProhibitedEquipLauncherInVehicle);
        e.Cancel();
    }

    /// <summary>
    /// Prevent players from jumping out of aircrafts too high in the air.
    /// </summary>
    public void HandleEvent(ExitVehicleRequested e, IServiceProvider serviceProvider)
    {
        InteractableVehicle? vehicle = e.Player.UnturnedPlayer.movement.getVehicle();
        if (vehicle == null || !vehicle.asset.engine.IsFlyingEngine())
            return;

        if (TerrainUtility.GetDistanceToGround(e.ExitLocation) <= MaxHeightToExitVehicle)
            return;

        _chatService.Send(e.Player, _translations.VehicleTooHigh);
        e.Cancel();
    }

    /// <summary>
    /// Prevent players from entering a vehicle when on vehicle interaction cooldown.
    /// </summary>
    public void HandleEvent(EnterVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (_cooldownManager.HasCooldown(e.Player, "Vehicle Interaction", e.Vehicle.Vehicle))
        {
            e.Cancel();
        }
    }

    /// <summary>
    /// Prevent players from swapping seats when on vehicle interaction cooldown.
    /// </summary>
    public void HandleEvent(VehicleSwapSeatRequested e, IServiceProvider serviceProvider)
    {
        if (_cooldownManager.HasCooldown(e.Player, "Vehicle Interaction", e.Vehicle.Vehicle))
        {
            e.Cancel();
        }
    }

    /// <summary>
    /// Clears irrelevant cooldowns from vehicle interactions.
    /// </summary>
    public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
    {
        _cooldownManager.RemoveCooldown("Vehicle Interaction", e.Vehicle.Vehicle);
    }
}
