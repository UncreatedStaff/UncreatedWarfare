using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class VehicleSupplyCrate : FallingItem
{
    private readonly WarfarePlayer _resupplier;
    private readonly EffectAsset _resupplyEffect;
    private readonly FobManager _fobManager;
    private readonly AmmoTranslations _translations;

    public VehicleSupplyCrate(ItemData itemData, Vector3 originalDropPosition, WarfarePlayer resupplier, EffectAsset resupplyEffect, IServiceProvider serviceProvider)
        : base(itemData, originalDropPosition)
    {
        _resupplier = resupplier;
        _resupplyEffect = resupplyEffect;
        _fobManager = serviceProvider.GetRequiredService<FobManager>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<AmmoTranslations>>().Value;
    }

    protected override void OnHitGround()
    {
        
        Collider[] hitColliders = new Collider[4];
        Physics.OverlapSphereNonAlloc(FinalRestPosition, 5, hitColliders, LayerMasks.VEHICLE);
        
        foreach (Collider collider in hitColliders.OrderBy(c => (c.transform.position - FinalRestPosition).sqrMagnitude))
        {
            WarfareVehicleComponent? warfareVehicleComponent = collider.GetComponentInParent<WarfareVehicleComponent>();
            if (warfareVehicleComponent == null)
                continue;

            ResourceFob? nearestFob = _fobManager.FindNearestResourceFob(_resupplier.Team, FinalRestPosition);
            if (nearestFob == null)
            {
                _resupplier.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastAmmoNotNearFob.Translate(_resupplier)));
                return;
            }

            int requiredAmmoCount = warfareVehicleComponent.WarfareVehicle.Info.Rearm.AmmoConsumed;
            if (nearestFob.AmmoCount < warfareVehicleComponent.WarfareVehicle.Info.Rearm.AmmoConsumed)
            {
                _resupplier.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastInsufficientAmmo.Translate(nearestFob.AmmoCount, requiredAmmoCount, _resupplier)));
                return;
            }
            
            ResupplyVehicle(warfareVehicleComponent.WarfareVehicle, nearestFob);
            return;
        }
    }

    private void ResupplyVehicle(WarfareVehicle warfareVehicle, ResourceFob fob)
    {
        int ammoToConsume = warfareVehicle.Info.Rearm.AmmoConsumed;
        foreach (IAssetLink<ItemAsset> itemAsset in warfareVehicle.Info.Rearm.Items)
        {
            ItemAsset? asset = itemAsset.GetAsset();
            if (asset == null)
                continue;
            
            ItemManager.dropItem(new Item(asset, EItemOrigin.CRAFT), FinalRestPosition, false, true, true);
        }
        fob.ChangeSupplies(SupplyType.Ammo, ammoToConsume);
        
        // destroy the dropped item
        ItemUtility.DestroyDroppedItem(ItemData, true);
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_resupplyEffect)
        {
            position = FinalRestPosition,
            relevantDistance = 70,
            reliable = true
        });
        
        _resupplier.SendToast(new ToastMessage(ToastMessageStyle.Tip, _translations.ToastLoseAmmo.Translate(ammoToConsume, _resupplier)));
    }
}