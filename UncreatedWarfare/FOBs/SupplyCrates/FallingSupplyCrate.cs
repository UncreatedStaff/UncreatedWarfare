using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class FallingSupplyCrate : FallingBuildable
{
    private readonly SupplyCrateInfo _supplyCrateInfo;
    private readonly IServiceProvider _serviceProvider;
    private readonly Action<SupplyCrate>? _onConvertedToBuildable;

    private static readonly Collider?[] SupplyStackBuffer = new Collider?[8];

    private SupplyCrateStack? _stack;

    private int _level = -1, _index = -1;

    /// <inheritdoc />
    public FallingSupplyCrate(
        WarfarePlayer player,
        ItemData itemData,
        Vector3 landingPoint,
        Vector3 dropPoint,
        SupplyCrateInfo supplyCrateInfo,
        float placementYaw,
        IServiceProvider serviceProvider,
        Action<SupplyCrate>? onConvertedToBuildable = null
    )
        : base(player,
            itemData,
            landingPoint,
            dropPoint,
            supplyCrateInfo.SupplyItemAsset.GetAssetOrFail(),
            supplyCrateInfo.PlacementEffect.GetAsset(),
            placementYaw,
            static (thisObj, buildable) => ((FallingSupplyCrate)thisObj).HandlePlaced(buildable)
        )
    {
        _supplyCrateInfo = supplyCrateInfo;
        _serviceProvider = serviceProvider;
        _onConvertedToBuildable = onConvertedToBuildable;
    }

    /// <inheritdoc />
    protected override void GetHitTransform(out Vector3 position, out Quaternion rotation)
    {
        if (ItemData.item.GetAsset<ItemPlaceableAsset>() is not { } asset
            || !BuildableExtensions.TryGetBuildableBounds(asset, out Bounds bounds))
        {
            base.GetHitTransform(out position, out rotation);
            return;
        }

        Vector3 size = bounds.size;

        int ct = Physics.OverlapSphereNonAlloc(FinalRestPosition + bounds.center, Math.Max(Math.Max(size.x, size.y), size.z) * 2,
            SupplyStackBuffer, RayMasks.LOGIC, QueryTriggerInteraction.Collide);

        if (ct == 0)
        {
            base.GetHitTransform(out position, out rotation);
            return;
        }

        for (int i = 0; i < ct; ++i)
        {
            if (!SupplyStackBuffer[i]!.TryGetComponent(out SupplyStackComponent component))
            {
                continue;
            }

            if (component.Stack.Asset.id != ItemData.item.id || !component.Stack.TryGetNextCratePosition(out int level, out int index, out position))
                continue;

            _stack = component.Stack;
            rotation = _stack.Rotation;
            _level = level;
            _index = index;
            return;
        }

        Array.Clear(SupplyStackBuffer, 0, ct);
        base.GetHitTransform(out position, out rotation);
    }

    private void HandlePlaced(IBuildable buildable)
    {                                                                                                          
        SupplyCrate supplyCrate = new SupplyCrate(
            _supplyCrateInfo,
            buildable,
            _serviceProvider,
            Team,
            _stack, 
            _level,
            _index,
            // while saving buildable state we don't want it to reset
            !Player.IsOnDuty
        );

        _onConvertedToBuildable?.Invoke(supplyCrate);
    }
}