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
    protected readonly float PlacementYaw;
    private readonly Action<FallingBuildable, IBuildable>? _onConvertedToBuildable;
    protected static readonly Collider?[] WorkingColliderBuffer = new Collider?[1];

    public FallingBuildable(WarfarePlayer player, ItemData itemData, Vector3 landingPoint, Vector3 dropPoint, ItemPlaceableAsset buildableToPlace, EffectAsset? placementEffect, float placementYaw, Action<FallingBuildable, IBuildable>? onConvertedToBuildable = null)
        : base(player, itemData, landingPoint, dropPoint)
    {
        _buildableToPlace = buildableToPlace;
        _placementEffect = placementEffect;
        PlacementYaw = placementYaw;
        _onConvertedToBuildable = onConvertedToBuildable;
    }

    protected virtual void GetHitTransform(out Vector3 position, out Quaternion rotation)
    {
        float placementYaw = PlacementYaw;

        bool isBehindPlayer = Player.Transform.InverseTransformPoint(FinalRestPosition).z < 0;
        if (isBehindPlayer)
            placementYaw += 180;

        const float distanceBetweenAdjacentCrateCenters = 1.5f;
        position = FinalRestPosition;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            int numberOfOverlaps = Physics.OverlapSphereNonAlloc(position, distanceBetweenAdjacentCrateCenters / 2 - 0.1f, WorkingColliderBuffer, RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.SMALL);
            WorkingColliderBuffer[0] = null;
            if (numberOfOverlaps <= 0)
                break;

            position += Quaternion.Euler(0, placementYaw, 0) * new Vector3(0, 0, distanceBetweenAdjacentCrateCenters);
            position.y = TerrainUtility.GetHighestPoint(position, 0);
        }

        rotation = Quaternion.Euler(-90f, placementYaw, 0f);
    }

    protected override void OnHitGround()
    {
        // drop the barricade

        float offset = _buildableToPlace is ItemBarricadeAsset b ? b.offset : 0;

        GetHitTransform(out Vector3 position, out Quaternion rotation);

        IBuildable buildable = BuildableExtensions.DropBuildable(
            _buildableToPlace,
            position + Vector3.up * offset,
            rotation,
            owner: Player.Steam64,
            group: Team.GroupId
        );

        // destroy the dropped item
        ItemUtility.DestroyDroppedItem(ItemData, true);

        _onConvertedToBuildable?.Invoke(this, buildable);

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
