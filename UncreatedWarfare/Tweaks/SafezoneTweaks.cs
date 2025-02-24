using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

public class SafezoneTweaks :
    IEventListener<PlayerUseableEquipped>,
    IEventListener<EquipUseableRequested>,
    IEventListener<PlayerEnteredZone>,
    IEventListener<PlayerExitedZone>,
    IEventListener<ChangeFiremodeRequested>,
    IEventListener<IDamageBuildableRequestedEvent>,
    IEventListener<DamageVehicleRequested>
{
    private readonly ZoneStore _zoneStore;
    private readonly KitRequestService _kitRequestService;
    private readonly ILogger<SafezoneTweaks> _logger;

    public SafezoneTweaks(ZoneStore zoneStore, KitRequestService kitRequestService, ILogger<SafezoneTweaks> logger)
    {
        _zoneStore = zoneStore;
        _kitRequestService = kitRequestService;
        _logger = logger;
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(PlayerUseableEquipped e, IServiceProvider serviceProvider)
    {
        if (!_zoneStore.IsInMainBase(e.Player.Position))
            return;

        // if the player is dequipping a gun in main, it's convenient to turn
        // safety off for them in to save them from having to do it themselves later
        if (e.DequippedItem?.GetAsset() is ItemGunAsset dequippedGunAsset && (EFiremode)e.DequippedItem.item.state[11] == EFiremode.SAFETY)
        {
            e.DequippedItem.item.state[11] = (byte)ItemUtility.GetDefaultFireMode(dequippedGunAsset);
            e.Inventory.sendUpdateInvState((byte)e.DequippedItemPage, e.DequippedItem.x, e.DequippedItem.y, e.DequippedItem.item.state);
        }
        else if (e.DequippedVehicle != null)
        {
            // exiting a turret
            InteractableVehicle vehicle = e.DequippedVehicle;
            Passenger passenger = vehicle.passengers[e.DequippedSeat];
            TurretInfo turret = passenger.turret;
            if (Assets.find(EAssetType.ITEM, turret.itemID) is ItemGunAsset gunAsset)
            {
                passenger.state[11] = (byte)ItemUtility.GetDefaultFireMode(gunAsset);
                if (passenger.player != null)
                {
                    PlayerEquipment eq = passenger.player.player.equipment;
                    eq.sendUpdateState();
                }
            }
        }

        // turn on safety for guns in main if not on duty
        if (e.Useable is not UseableGun || (e.Player.IsOnDuty && !e.Equipment.isTurret))
            return;
        
        e.Equipment.state[11] = (byte) EFiremode.SAFETY;
        e.Equipment.sendUpdateState();
    }

    public void HandleEvent(EquipUseableRequested e, IServiceProvider serviceProvider)
    {
        if (e.Player.IsOnDuty)
            return;

        if (!_zoneStore.IsInMainBase(e.Player.Position))
            return;
        
        if (e.Asset is not ItemThrowableAsset)
            return;
        
        // prevent equipping all throwables in main if not on duty
        e.Cancel();
    }

    [EventListener(MustRunInstantly = true)]
    public void HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type is ZoneType.MainBase or ZoneType.Lobby)
        {
            // heal player
            if (!e.Player.UnturnedPlayer.life.isDead)
                e.Player.UnturnedPlayer.life.sendRevive();

            // give safezone kit if they dont have one
            KitPlayerComponent component = e.Player.Component<KitPlayerComponent>();
            if (!component.ActiveKitKey.HasValue
                || component.ActiveClass == Class.None
                || component.ActiveClass == Class.Unarmed && e.Player.Team.Faction.UnarmedKit != component.ActiveKitKey.Value)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"Giving player {e.Player} on team {e.Player.Team} their unarmed kit.");
                        await _kitRequestService.GiveUnarmedKitAsync(e.Player, silent: true, e.Player.DisconnectToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error giving safezone kit.");
                    }
                });
            }
        }

        if (e.Player.IsOnDuty && !e.Equipment.isTurret)
            return;

        if (e.Zone.Type is not ZoneType.MainBase)
            return;
        
        if (e.Equipment.useable is not UseableGun)
            return;
        
        // turn on safety for guns when entering main
        e.Equipment.state[11] = (byte) EFiremode.SAFETY;
        e.Equipment.sendUpdateState();
    }

    public void HandleEvent(PlayerExitedZone e, IServiceProvider serviceProvider)
    {
        if (e.Zone.Type is not ZoneType.MainBase || _zoneStore.IsInMainBase(e.Player))
            return;
        
        if (e.Equipment.useable is not UseableGun gun || (EFiremode)e.Equipment.state[11] != EFiremode.SAFETY)
            return;
        
        // turn off safety for guns when leaving main to spare the player from having to do it themselves
        e.Equipment.state[11] = (byte)ItemUtility.GetDefaultFireMode(gun.equippedGunAsset);
        e.Equipment.sendUpdateState();
    }

    public void HandleEvent(ChangeFiremodeRequested e, IServiceProvider serviceProvider)
    {
        if (e.Player.IsOnDuty || !_zoneStore.IsInMainBase(e.Player))
            return;

        e.Cancel();
    }

    // prevent damage to buildables and vehicles in sz
    public void HandleEvent(IDamageBuildableRequestedEvent e, IServiceProvider serviceProvider)
    {
        if (_zoneStore.IsInsideZone(e.Buildable.Position, ZoneType.MainBase, null))
        {
            e.Cancel();
        }
    }

    public void HandleEvent(DamageVehicleRequested e, IServiceProvider serviceProvider)
    {
        if (_zoneStore.IsInsideZone(e.Vehicle.Position, ZoneType.MainBase, null))
        {
            e.Cancel();
        }
    }
}