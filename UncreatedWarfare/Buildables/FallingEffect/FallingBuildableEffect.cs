using System;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

// Create an instance of this class using an ItemDrop to make it turn into a Buildable after it hits the ground.
public class FallingBuildableEffect : FallingEffect<FallingBuildableArgs>
{
    private FallingBuildableArgs _args;

    public ItemPlaceableAsset Buildable => _args.Buildable;

    protected internal override void Initialize(ref FallingBuildableArgs args, in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        base.Initialize(ref args, in config, onSettle);
        _args = args;
    }

    public void PlayPlacementEffect()
    {
        if (_args.PlacementEffect == null)
            return;

        // spawn a nice effect
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
            return;

        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        IBuildable buildable = BuildableExtensions.DropBuildable(
            _args.Buildable,
            position,
            rotation,
            owner: Owner?.Steam64 ?? CSteamID.Nil,
            group: Owner != null && _args.GroupId == CSteamID.Nil ? Owner.Team.GroupId : _args.GroupId
        );

        OnConverted(buildable);

        _args.OnConverted?.Invoke(this, buildable);

        PlayPlacementEffect();

        base.OnSettle();
    }

    protected virtual void OnConverted(IBuildable buildable)
    {

    }
}

public readonly struct FallingBuildableArgs
{
    public required ItemPlaceableAsset Buildable { get; init; }
    public EffectAsset? PlacementEffect { get; init; }
    public Func<FallingBuildableEffect, bool>? ShouldConvert { get; init; }
    public Action<FallingBuildableEffect, IBuildable>? OnConverted { get; init; }
    public CSteamID GroupId { get; init; }
}