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
    private ThrownComponent _thrownVehicleCrateComponent;

    public ThrownVehicleCrate(GameObject throwable, WarfarePlayer thrower, ItemThrowableAsset thrownAsset, EffectAsset resupplyEffectAsset, FobManager? fobManager, ZoneStore? zoneStore, AmmoTranslations translations)
        : base(throwable, thrownAsset, thrower)
    {
        _resupplyEffectAsset = resupplyEffectAsset;
        _fobManager = fobManager;
        _zoneStore = zoneStore;
        _translations = translations;
        _thrownVehicleCrateComponent = throwable.gameObject.AddComponent<ThrownComponent>();
        _thrownVehicleCrateComponent.OnThrowableDestroyed += OnThrowableDestroyed;
    }

    private void OnThrowableDestroyed()
    {
        // descending distance comparer
        IComparer<Component> comparer = new DistanceComparer<Component>(_throwable.transform.position, x => x.transform.position, false, reverse: false);

        int results = Physics.OverlapSphereNonAlloc(_throwable.transform.position, 5f, TempHitColliders, 1 << LayerMasks.VEHICLE);
        Array.Sort<Collider>(TempHitColliders, 0, results, comparer);
        Console.WriteLine("collided with vehicle colliders: " + results);
        WarfareVehicle? _warfareVehicle = null;
        for (int i = 0; i < results; i++)
        {
            Collider collider = TempHitColliders[i];
            WarfareVehicleComponent? warfareVehicleComponent = collider.GetComponentInParent<WarfareVehicleComponent>();
            if (warfareVehicleComponent != null)
            {
                _warfareVehicle = warfareVehicleComponent.WarfareVehicle;
                break;
            }
        }

        if (_warfareVehicle == null)
        {
            RespawnThrowableItem();
            _thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearVehicle.Translate(_thrower)));
            return;
        }
        
        if (_fobManager != null && !(_zoneStore != null && _zoneStore.IsInMainBase(_throwable.transform.position)))
        {
            ResourceFob? nearestFob = _fobManager.FindNearestResourceFob(_thrower.Team, _throwable.transform.position);
            if (nearestFob == null)
            {
                RespawnThrowableItem();
                _thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearFob.Translate(_thrower)));
                return;
            }
            
            int requiredAmmoCount = _warfareVehicle.Info.Rearm.AmmoConsumed;
            if (nearestFob.AmmoCount < _warfareVehicle.Info.Rearm.AmmoConsumed)
            {
                RespawnThrowableItem();
                _thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastInsufficientAmmo.Translate(nearestFob.AmmoCount, requiredAmmoCount, _thrower)));
            }
            
            nearestFob.ChangeSupplies(SupplyType.Ammo, requiredAmmoCount);
            _thrower.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(requiredAmmoCount, _thrower)));
        }
        
        DropSupplies(_warfareVehicle);
    }

    private void DropSupplies(WarfareVehicle warfareVehicle)
    {
        foreach (IAssetLink<ItemAsset> itemAsset in warfareVehicle.Info.Rearm.Items)
        {
            ItemAsset? asset = itemAsset.GetAsset();
            if (asset == null)
                continue;
            
            ItemManager.dropItem(new Item(asset, EItemOrigin.CRAFT), _thrower.Position, false, true, true);
        }
        
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_resupplyEffectAsset)
        {
            position = _throwable.transform.position,
            relevantDistance = 70,
            reliable = true
        });
    }
}