using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

// apparently unity doesn't support closed generic components ???
internal sealed class FallingBunkerFobCrateEffect : FallingCrateEffect<BunkerFob>;

/// <summary>
/// A type of falling effect that instantiates a crate buildable. It can also optionally get sucked into a nearby FOB before it's instantiated.
/// </summary>
/// <typeparam name="TFobType">The type of FOBs this crate can be dropped near.</typeparam>
public class FallingCrateEffect<TFobType> : FallingBuildableEffect<FallingCrateArgs<TFobType>> where TFobType : IFob, ITransformObject
{
#nullable disable
    private FobManager _fobManager;

    public SupplyCrateInfo CrateInfo { get; private set; }

#nullable restore

    private SupplyCrateStack? _stack;
    private int _stackLevel = -1, _stackIndex = -1;

    private FallingCrateEffectDroppedNearFobHandler<TFobType>? _onDroppedNearFob;

    private bool _hasCalledOnDroppedNearFob;
    private FallingCrateEffectNearbyFobSelector<TFobType>? _nearbyFobSelector;

    /// <inheritdoc />
    protected internal override void Initialize(ref FallingCrateArgs<TFobType> args, in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        base.Initialize(ref args, in config, onSettle);

        _onDroppedNearFob = args.OnDroppedNearFob;
        if (_onDroppedNearFob != null)
        {
            _nearbyFobSelector = args.NearbyFobSelector;

            if (_nearbyFobSelector == null)
            {
                // default selectors
                if (typeof(TFobType).IsSubclassOf(typeof(ResourceFob)))
                {
                    _nearbyFobSelector = (f, _, pos) => MathUtility.WithinRange(f.Position, pos, ((ResourceFob)(object)f).EffectiveRadius);
                }
            }
        }

        _fobManager = ServiceProvider.GetRequiredService<FobManager>();

        ItemPlaceableAsset buildableAsset = args.Buildable;

        SupplyCrateInfo? crateInfo = _fobManager.Configuration.SupplyCrates.FirstOrDefault(x => x.SupplyItemAsset.MatchAsset(buildableAsset));
        CrateInfo = crateInfo ?? throw new ArgumentException("CrateInfo doesn't exist for this crate type.", nameof(args));

        LogMessage($"Initialized falling crate effect: {CrateInfo.SupplyItemAsset}.");
    }

    /// <inheritdoc />
    protected override void OnSettle()
    {
        if (_hasCalledOnDroppedNearFob)
            return;

        base.OnSettle();
    }

    protected override void OnConverted(IBuildable buildable)
    {
        base.OnConverted(buildable);

        SupplyCrate supplyCrate = new SupplyCrate(
            CrateInfo,
            buildable,
            ServiceProvider,
            Team,
            _stack,
            _stackLevel,
            _stackIndex,
            // while saving buildable state we don't want it to reset
            Owner is not { IsOnDuty: true }
        );

        LogMessage($"Registering {CrateInfo.SupplyItemAsset} crate.");
        _fobManager.RegisterFobEntity(supplyCrate);
    }

    protected override void TransformSpawnPosition(ref Vector3 position, ref Quaternion rotation)
    {
        if (_stack == null)
            base.TransformSpawnPosition(ref position, ref rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_stack != null)
        {
            LogMessage($"Trigger hit {other.name}, but already has a stack.");
            return;
        }

        GameObject @object = other.gameObject;
        if (@object.layer != LayerMasks.LOGIC)
        {
            LogMessage($"Trigger hit {other.name}, not the correct layer.");
            return;
        }

        SupplyStackComponent? stack = @object.GetComponentInParent<SupplyStackComponent>();
        if (stack == null || stack.Stack.Asset.GUID != Buildable.GUID || !stack.Stack.TryGetNextCratePosition(out int level, out int index, out Vector3 position))
        {
            LogMessage($"Trigger hit {other.name}, stack not found or not the right asset.");
            return;
        }

        LogMessage($"Trigger hit stack of {stack.Stack.Crates.Count} {stack.Stack.Asset.FriendlyName}(s).");
        _stackLevel = level;
        _stackIndex = index;
        _stack = stack.Stack;

        float offset = Buildable is ItemBarricadeAsset b ? b.offset : 0;

        float terrainHeight = TerrainUtility.GetHighestPoint(in position, float.NaN);

        if (position.y < terrainHeight)
        {
            position.y = terrainHeight + offset;
        }

        transform.SetPositionAndRotation(position, _stack.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation);
        SkipWaitForSettle();
    }

    private bool _hasCheckedNearbyFobs;

    protected override void StillTick(float timeStill)
    {
        if (_onDroppedNearFob == null)
            return;

        // once its still for a little while check for a nearby FOB only once
        // if it moves after this it'll recheck next time its still
        if (_hasCheckedNearbyFobs || timeStill < 0.3f)
            return;
        
        _hasCheckedNearbyFobs = true;
        Func<TFobType, bool>? selector = null;

        if (_nearbyFobSelector != null)
        {
            Vector3 pos = transform.position;
            selector = f => _nearbyFobSelector(f, this, pos);
        }

        TFobType? nearestFob = _fobManager.FindNearestFob(Team, transform.position, selector);
        if (nearestFob == null)
        {
            return;
        }

        _onDroppedNearFob(nearestFob, this);
        _hasCalledOnDroppedNearFob = true;
    }

    protected override void MovedTick()
    {
        // reset FOB check if it starts moving again
        _hasCheckedNearbyFobs = _hasCalledOnDroppedNearFob;
    }
}

/// <returns>Whether or not to consider this <paramref name="fob"/>.</returns>
public delegate bool FallingCrateEffectNearbyFobSelector<TFobType>(
    TFobType fob,
    FallingCrateEffect<TFobType> effect,
    Vector3 position
) where TFobType : IFob, ITransformObject;

/// <returns>Whether or not to stop looking for nearby FOBs, because this <paramref name="fob"/> handled the crate.</returns>
public delegate bool FallingCrateEffectDroppedNearFobHandler<TFobType>(
    TFobType fob,
    FallingCrateEffect<TFobType> effect
) where TFobType : IFob, ITransformObject;

/// <summary>
/// Options for instantiating <see cref="FallingCrateEffect{TFobType}"/> components.
/// </summary>
/// <typeparam name="TFobType">The type of FOBs this crate can be dropped near.</typeparam>
public class FallingCrateArgs<TFobType> : FallingBuildableArgs<FallingCrateArgs<TFobType>>
    where TFobType : IFob, ITransformObject
{
    /// <summary>
    /// Decides whether or not to consider a FOB for invoking <see cref="OnDroppedNearFob"/> on.
    /// </summary>
    public FallingCrateEffectNearbyFobSelector<TFobType>? NearbyFobSelector { get; set; }

    /// <summary>
    /// Invoked when a crate settles near a FOB before it's converted to a buildable.
    /// </summary>
    /// <remarks>Usually used to add supplies to an existing FOB.</remarks>
    public FallingCrateEffectDroppedNearFobHandler<TFobType>? OnDroppedNearFob { get; set; }
}