using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Migrations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;
using UnityEngine;

namespace Uncreated.Warfare.Components;
// Add this script to an ItemDrop to make it turn into a Barricade after it hits the ground.
internal class FallingBuildable
{
    private ItemData _itemData;
    private ItemBarricadeAsset _barricadeToPlace;
    private readonly EffectAsset _placementAffect;
    private readonly float _placementYaw;
    private readonly Action<IBuildable>? _onConvertedToBuildable;
    // only support barricades for now
    public FallingBuildable(ItemData itemData, ItemBarricadeAsset barricadeToConvert, EffectAsset placementAffect, Vector3 originalDropPosition, float placementYaw, Action<IBuildable>? onConvertedToBuildable = null)
    {
        _itemData = itemData;
        _barricadeToPlace = barricadeToConvert;
        _placementAffect = placementAffect;
        _placementYaw = placementYaw;
        _onConvertedToBuildable = onConvertedToBuildable;
        float distanceFallen = (originalDropPosition - _itemData.point).magnitude;
        float timeUntilConversion = Mathf.Sqrt(2 * 9.8f * distanceFallen) / 9.8f; // calculated using an equation of motion
        timeUntilConversion += 1; // add 1 second for good vibes

        ILoopTicker ticker = new UnityLoopTickerFactory().CreateTicker(TimeSpan.FromSeconds(timeUntilConversion), false, true);

        ticker.OnTick += (ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime) =>
        {
            ConvertToBuildable();
            ticker.Dispose();
        };
    }
    private void ConvertToBuildable()
    {
        // drop the barricade
        Transform transform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(_barricadeToPlace), _itemData.point + new Vector3(0, _barricadeToPlace.offset, 0), Quaternion.Euler(-90, _placementYaw, 0), 0, 0
        );

        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);

        _onConvertedToBuildable?.Invoke(new BuildableBarricade(barricade));

        // destroy the dropped item
        ItemUtility.DestroyDroppedItem(_itemData, true);
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_placementAffect)
        {
            position = _itemData.point,
            relevantDistance = 70,
            reliable = true
        });
    }
}
