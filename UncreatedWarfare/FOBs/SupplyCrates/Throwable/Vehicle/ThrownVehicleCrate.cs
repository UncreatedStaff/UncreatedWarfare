using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.FOBs.SupplyCrates.Throwable;

public class ThrownVehicleCrate : ThrownSupplyCrate
{
    private readonly EffectAsset _resupplyEffectAsset;
    private readonly FobManager? _fobManager;
    private readonly ZoneStore? _zoneStore;
    private readonly AmmoTranslations _translations;
    private ThrownVehicleCrateComponent _thrownVehicleCrateComponent;
    private WarfareVehicle? _warfareVehicle;

    public ThrownVehicleCrate(GameObject throwable, WarfarePlayer thrower, ItemThrowableAsset thrownAsset, EffectAsset resupplyEffectAsset, FobManager? fobManager, ZoneStore? zoneStore, AmmoTranslations translations)
        : base(throwable, thrownAsset, thrower)
    {
        _resupplyEffectAsset = resupplyEffectAsset;
        _fobManager = fobManager;
        _zoneStore = zoneStore;
        _translations = translations;
        _thrownVehicleCrateComponent = throwable.gameObject.AddComponent<ThrownVehicleCrateComponent>();
        _thrownVehicleCrateComponent.OnCollideWithVehicle += OnCollideWithVehicle;
        _thrownVehicleCrateComponent.OnThrowableDestroyed += OnThrowableDestroyed;
    }

    private void OnThrowableDestroyed()
    {
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
    
    private void OnCollideWithVehicle(InteractableVehicle vehicle)
    {
        WarfareVehicleComponent warfareVehicleComponent = vehicle.GetComponentInParent<WarfareVehicleComponent>();
        if (warfareVehicleComponent == null)
            return;
        
        _warfareVehicle = warfareVehicleComponent.WarfareVehicle;
    }

    private void DropSupplies(WarfareVehicle warfareVehicle)
    {
        foreach (IAssetLink<ItemAsset> itemAsset in warfareVehicle.Info.Rearm.Items)
        {
            ItemAsset? asset = itemAsset.GetAsset();
            if (asset == null)
                continue;
            
            ItemManager.dropItem(new Item(asset, EItemOrigin.CRAFT), _throwable.transform.position, false, true, true);
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