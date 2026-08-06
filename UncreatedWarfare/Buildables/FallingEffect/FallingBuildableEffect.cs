using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

// Create an instance of this class using an ItemDrop to make it turn into a Buildable after it hits the ground.
public class FallingBuildableEffect<TArgs> : FallingEffect<TArgs> where TArgs : FallingBuildableArgs<TArgs>
{
#nullable disable
    private TArgs _args;
#nullable restore

    public ItemPlaceableAsset Buildable => _args.Buildable;

    protected internal override void Initialize(ref TArgs args, in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        _args = args;
        base.Initialize(ref args, in config, onSettle);
    }

    public void PlayPlacementEffect()
    {
        if (_args.PlacementEffect == null)
            return;

        // spawn a nice effect
        LogMessage("Triggered placement effect.");
        EffectManager.triggerEffect(new TriggerEffectParameters(_args.PlacementEffect)
        {
            position = transform.position,
            relevantDistance = 70,
            reliable = true
        });
    }

    protected override void OnSettle()
    {
        if (_args.ShouldConvert != null && !_args.ShouldConvert.Invoke(this))
        {
            LogMessage("Convert blocked by ShouldConvert handler.");
            return;
        }

        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        TransformSpawnPosition(ref position, ref rotation);

        IBuildable buildable = BuildableExtensions.DropBuildable(
            _args.Buildable,
            position,
            rotation * BarricadeUtility.DefaultBarricadeRotation,
            owner: Owner?.Steam64 ?? CSteamID.Nil,
            group: Team.GroupId
        );

        LogMessage($"Converted to {buildable.Asset.FriendlyName} buildable.");
        OnConverted(buildable);

        _args.OnConverted?.Invoke(this, buildable);

        PlayPlacementEffect();

        base.OnSettle();
    }

    protected virtual void OnConverted(IBuildable buildable)
    {

    }

    protected virtual void TransformSpawnPosition(ref Vector3 position, ref Quaternion rotation)
    {
        if (!BuildableExtensions.TryGetBuildableBounds(_args.Buildable, out Bounds bounds))
            return;

#if FALLING_EFFECT_DEBUG_LOGGING
        EffectUtility.TriggerDebugEffect(position, Quaternion.LookRotation(Vector3.down, Vector3.forward), clear: false, scale: 0.2f);
        EffectUtility.TriggerDebugEffect(
            position + Vector3.down * bounds.size.y,
            Quaternion.LookRotation(Vector3.down, Vector3.forward),
            clear: false,
            scale: 0.2f
        );
#endif
        bool hasLockedToGnd = false;

        castDown:
        if (!Physics.Raycast(
                position,
                direction: Vector3.down,
                out RaycastHit hit,
                maxDistance: 1024f,
                RayMasks.BLOCK_COLLISION & ~RayMasks.VEHICLE,
                QueryTriggerInteraction.Ignore
            ))
        {
            // fix for barricades ghosting through the ground
            // (even though they use continuous collision it still happens somehow)
            if (hasLockedToGnd)
                return;

            float pt = TerrainUtility.GetHighestPoint(in position, float.NaN);
            position.y = pt + 0.5f;
            hasLockedToGnd = true;
            goto castDown;
        }

        // attempt to lock the barricade to the ground's normal
        Vector3 normal = hit.normal;
        position = hit.point;
        rotation = Quaternion.LookRotation(Vector3.Cross(rotation * Vector3.right, normal), normal);
        if (_args.Buildable is ItemBarricadeAsset barricade)
        {
            position += rotation * new Vector3(0f, barricade.offset, 0f);
        }

#if FALLING_EFFECT_DEBUG_LOGGING
        EffectUtility.TriggerDebugEffect(position, rotation, clear: false);
#endif
    }
}

public class FallingBuildableArgs : FallingBuildableArgs<FallingBuildableArgs>;
public class FallingBuildableArgs<TSelf> where TSelf : FallingBuildableArgs<TSelf>
{
    public required ItemPlaceableAsset Buildable { get; init; }
    public EffectAsset? PlacementEffect { get; init; }
    public Func<FallingBuildableEffect<TSelf>, bool>? ShouldConvert { get; set; }
    public Action<FallingBuildableEffect<TSelf>, IBuildable>? OnConverted { get; set; }
}