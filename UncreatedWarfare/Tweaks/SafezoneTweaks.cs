using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Events.Models.Zones;
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

    public SafezoneTweaks(ZoneStore zoneStore)
    {
        _zoneStore = zoneStore;
    }
    
    public void HandleEvent(PlayerUseableEquipped e, IServiceProvider serviceProvider)
    {
        if (!_zoneStore.IsInMainBase(e.Player.Position))
            return;

        // if the player is dequipping a gun in main, it's convenient to turn safety off for them in to save them from having todo it themselves later
        if (e.DequippedItem?.GetAsset() is ItemGunAsset dequippedGunAsset)
        {
            e.DequippedItem.item.state[11] = (byte)GetDefaultFireMode(dequippedGunAsset);
            byte index = e.Player.UnturnedPlayer.inventory.getIndex((byte)e.DequippedItemPage, e.DequippedItem.x, e.DequippedItem.y);
            if (index != byte.MaxValue)
                e.Inventory.updateState((byte)e.DequippedItemPage, index, e.DequippedItem.item.state);
        }
        
        // turn on safety for guns in main if not on duty
        if (e.Useable is not UseableGun || e.Player.IsOnDuty)
            return;
        
        e.Equipment.state[11] = (byte) EFiremode.SAFETY;
        e.Equipment.sendUpdateState();
    }

    private static EFiremode GetDefaultFireMode(ItemGunAsset gunAsset)
    {
        if (gunAsset.hasAuto)
            return EFiremode.AUTO;
        if (gunAsset.hasSemi)
            return EFiremode.SEMI;
        if (gunAsset.hasBurst)
            return EFiremode.BURST;

        return EFiremode.SAFETY;
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

    public void HandleEvent(PlayerEnteredZone e, IServiceProvider serviceProvider)
    {
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
        if (e.Zone.Type is not ZoneType.MainBase)
            return;
        
        if (e.Equipment.useable is not UseableGun gun)
            return;
        
        // turn off safety for guns when leaving main to spare the player from having to do it themselves
        e.Equipment.state[11] = (byte)GetDefaultFireMode(gun.equippedGunAsset);
        e.Equipment.sendUpdateState();
    }

    public void HandleEvent(ChangeFiremodeRequested e, IServiceProvider serviceProvider)
    {
        if (e.Player.IsOnDuty)
            return;

        e.Firemode = EFiremode.SAFETY;
        e.Cancel(cancelAction: false);
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