using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Fobs.SupplyCrates;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.FOBs.SupplyCrates.VehicleResupply;

public class VehicleSupplyCrate : FallingItem
{
    private static readonly Collider[] TempHitColliders = new Collider[4];
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
        // descending distance comparer
        IComparer<Component> comparer = new DistanceComparer<Component>(FinalRestPosition, x => x.transform.position, false, reverse: false);

        int results = Physics.OverlapSphereNonAlloc(FinalRestPosition, 5, TempHitColliders, LayerMasks.VEHICLE);
        Array.Sort<Collider>(TempHitColliders, 0, results, comparer);

        for (int i = 0; i < results; i++)
        {
            Collider collider = TempHitColliders[i];
            WarfareVehicleComponent? warfareVehicleComponent = collider.GetComponentInParent<WarfareVehicleComponent>();
            if (warfareVehicleComponent == null)
                continue;

            // todo: do we need to give back the item if it fails?
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