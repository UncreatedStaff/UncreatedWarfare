using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

// Create an instance of this class using an ItemDrop to make it turn into a Buildable after it hits the ground.
public class FallingBuildable : FallingItem
{
    private readonly ItemPlaceableAsset _buildableToPlace;
    private readonly EffectAsset? _placementEffect;
    private readonly float _placementYaw;
    private readonly Action<IBuildable>? _onConvertedToBuildable;

    public FallingBuildable(WarfarePlayer player, ItemData itemData, ItemPlaceableAsset buildableToPlace, EffectAsset? placementEffect, Vector3 originalDropPosition, float placementYaw, Action<IBuildable>? onConvertedToBuildable = null)
        : base(player, itemData, originalDropPosition)
    {
        _buildableToPlace = buildableToPlace;
        _placementEffect = placementEffect;
        _placementYaw = placementYaw;
        _onConvertedToBuildable = onConvertedToBuildable;
    }

    protected override void OnHitGround()
    {
        // drop the barricade

        float offset = _buildableToPlace is ItemBarricadeAsset b ? b.offset : 0;

        IBuildable buildable = BuildableExtensions.DropBuildable(
            _buildableToPlace,
            FinalRestPosition + Vector3.up * offset,
            Quaternion.Euler(-90, _placementYaw, 0),
            owner: Player.Steam64,
            group: Team.GroupId
        );

        _onConvertedToBuildable?.Invoke(buildable);

        // destroy the dropped item
        ItemUtility.DestroyDroppedItem(ItemData, true);

        if (_placementEffect == null)
            return;

        // spawn a nice effect
        EffectManager.triggerEffect(new TriggerEffectParameters(_placementEffect)
        {
            position = FinalRestPosition,
            relevantDistance = 70,
            reliable = true
        });
    }
}
