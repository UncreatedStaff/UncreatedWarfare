using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

// Create an instance of this class using an ItemDrop to make it turn into a Barricade after it hits the ground.
public class FallingBuildable : FallingItem
{
    private ItemBarricadeAsset _barricadeToPlace;
    private readonly EffectAsset _placementAffect;
    private readonly float _placementYaw;
    private readonly Action<IBuildable>? _onConvertedToBuildable;
    // only support barricades for now
    public FallingBuildable(ItemData itemData, ItemBarricadeAsset barricadeToConvert, EffectAsset placementAffect, Vector3 originalDropPosition, float placementYaw, Action<IBuildable>? onConvertedToBuildable = null)
        : base(itemData, originalDropPosition)
    {
        _barricadeToPlace = barricadeToConvert;
        _placementAffect = placementAffect;
        _placementYaw = placementYaw;
        _onConvertedToBuildable = onConvertedToBuildable;
    }
    protected override void OnHitGround()
    {
        // drop the barricade
        Transform transform = BarricadeManager.dropNonPlantedBarricade(
            new Barricade(_barricadeToPlace), FinalRestPosition + new Vector3(0, _barricadeToPlace.offset, 0), Quaternion.Euler(-90, _placementYaw, 0), 0, 0
        );

        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);

        _onConvertedToBuildable?.Invoke(new BuildableBarricade(barricade));

        // destroy the dropped item
        ItemUtility.DestroyDroppedItem(ItemData, true);
        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_placementAffect)
        {
            position = FinalRestPosition,
            relevantDistance = 70,
            reliable = true
        });
    }
}
