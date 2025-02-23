using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Models.Zones;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Tweaks;

public class SafezoneTweaks :
    IEventListener<PlayerUseableEquipped>,
    IEventListener<EquipUseableRequested>,
    IEventListener<PlayerEnteredZone>,
    IEventListener<PlayerExitedZone>
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
            e.Equipment.sendUpdateState();
        }
        
        // turn on safety for guns in main
        if (e.Useable is not UseableGun)
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
        if (!_zoneStore.IsInMainBase(e.Player.Position))
            return;
        
        if (e.Asset is not ItemThrowableAsset)
            return;
        
        // prevent equipping all throwables in main
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
}