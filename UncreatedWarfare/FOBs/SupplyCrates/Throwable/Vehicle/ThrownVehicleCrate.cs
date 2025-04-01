using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable.Vehicle;

public class ThrownVehicleCrate : ThrownSupplyCrate
{
    private static readonly Collider[] TempHitColliders = new Collider[4];
    private readonly EffectAsset _resupplyEffectAsset;
    private readonly FobManager? _fobManager;
    private readonly ZoneStore? _zoneStore;
    private readonly AmmoTranslations _translations;

    public ThrownVehicleCrate(GameObject throwable, WarfarePlayer thrower, ItemThrowableAsset thrownAsset, EffectAsset resupplyEffectAsset, FobManager? fobManager, ZoneStore? zoneStore, AmmoTranslations translations)
        : base(throwable, thrownAsset, thrower)
    {
        _resupplyEffectAsset = resupplyEffectAsset;
        _fobManager = fobManager;
        _zoneStore = zoneStore;
        _translations = translations;
        ThrownComponent thrownVehicleCrateComponent = throwable.gameObject.AddComponent<ThrownComponent>();
        thrownVehicleCrateComponent.OnThrowableDestroyed += OnThrowableDestroyed;
    }

    private void OnThrowableDestroyed()
    {
        // descending distance comparer
        IComparer<Component> comparer = new LookAtComparer<Component>(Throwable.transform.forward, x => x.transform.position - Throwable.transform.position, reverse: false);

        int results = Physics.OverlapSphereNonAlloc(Throwable.transform.position, 5f, TempHitColliders, 1 << LayerMasks.VEHICLE);
        Array.Sort<Collider>(TempHitColliders, 0, results, comparer);
        WarfareVehicle? warfareVehicle = null;
        for (int i = 0; i < results; i++)
        {
            Collider collider = TempHitColliders[i];
            WarfareVehicleComponent? warfareVehicleComponent = collider.GetComponentInParent<WarfareVehicleComponent>();
            if (warfareVehicleComponent != null)
            {
                warfareVehicle = warfareVehicleComponent.WarfareVehicle;
                break;
            }
        }

        if (warfareVehicle == null)
        {
            RespawnThrowableItem();
            Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearVehicle.Translate(Thrower)));
            return;
        }
        
        if (_fobManager != null && !(_zoneStore != null && _zoneStore.IsInMainBase(Throwable.transform.position)))
        {
            ResourceFob? nearestFob = _fobManager.FindNearestResourceFob(Thrower.Team, Throwable.transform.position);
            if (nearestFob == null)
            {
                RespawnThrowableItem();
                Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearFob.Translate(Thrower)));
                return;
            }
            
            int requiredAmmoCount = warfareVehicle.Info.Rearm.AmmoConsumed;
            if (nearestFob.AmmoCount < warfareVehicle.Info.Rearm.AmmoConsumed)
            {
                RespawnThrowableItem();
                Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastInsufficientAmmo.Translate(nearestFob.AmmoCount, requiredAmmoCount, Thrower)));
                return;
            }
            
            nearestFob.ChangeSupplies(SupplyType.Ammo, -requiredAmmoCount);
            Thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(requiredAmmoCount, Thrower)));
        }
        
        DropSupplies(warfareVehicle);
        if (warfareVehicle.FlareEmitter != null)
            warfareVehicle.FlareEmitter.ReloadFlares();
    }

    private void DropSupplies(WarfareVehicle warfareVehicle)
    {
        foreach (IAssetLink<ItemAsset> itemAsset in warfareVehicle.Info.Rearm.Items)
        {
            ItemAsset? asset = itemAsset.GetAsset();
            if (asset == null)
                continue;
            
            ItemManager.dropItem(new Item(asset, EItemOrigin.CRAFT), Throwable.transform.position, false, true, true);
        }
        
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_resupplyEffectAsset)
        {
            position = Throwable.transform.position,
            relevantDistance = 70,
            reliable = true
        });
    }
}