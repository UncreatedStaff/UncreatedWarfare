using System;
using System.Collections.Immutable;
using System.Linq;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

public delegate void ConfigureFallingEffect(ref FallingEffectInstantiationArgs args);

public sealed class FallingEffectManager : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FallingEffectManager> _logger;
    private readonly List<FallingEffect> _nonReplicatedEffects = new List<FallingEffect>();

    public ImmutableArray<FallingEffectGroup> Groups { get; private set; }

    public FallingEffectManager(IServiceProvider serviceProvider, ILogger<FallingEffectManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // todo: temporarily hard-coded
        Groups =
        [
            new FallingEffectGroup(
                Assets.find<ItemAsset>(new Guid("a2eb76590cf74401aeb7ff4b4b79fd86")),       // SupplyCrate_1
                [
                    Assets.find<EffectAsset>(new Guid("2ea62e93ddd34715b32010981a733dd5")), // Physical_SupplyCrate_1_Inst1
                    Assets.find<EffectAsset>(new Guid("52f9d74f2e81434e9069f4d408870e96")), // Physical_SupplyCrate_1_Inst2
                    Assets.find<EffectAsset>(new Guid("5987fcbb3c244671a1e1d36ec070c569")), // Physical_SupplyCrate_1_Inst3
                    Assets.find<EffectAsset>(new Guid("d78d4f43e5c54af093f77f8d1d31b279")), // Physical_SupplyCrate_1_Inst4
                    Assets.find<EffectAsset>(new Guid("fb79716ebe084d469d488a7536a8d776")), // Physical_SupplyCrate_1_Inst5
                    Assets.find<EffectAsset>(new Guid("990e1c8f80f941ef9900eaaba8b94cae")), // Physical_SupplyCrate_1_Inst6
                    Assets.find<EffectAsset>(new Guid("06e514cb05dc42dc8b56d765ce051d60")), // Physical_SupplyCrate_1_Inst7
                    Assets.find<EffectAsset>(new Guid("b1fc3bb244c04f178c0f660f5fb02b65")), // Physical_SupplyCrate_1_Inst8
                    Assets.find<EffectAsset>(new Guid("cf58108482d1458fad98e7a76e034f53")), // Physical_SupplyCrate_1_Inst9
                    Assets.find<EffectAsset>(new Guid("d3f5d1af0f534b13b52986f6f44aede9"))  // Physical_SupplyCrate_1_Inst10
                ],
                this
            ),
            new FallingEffectGroup(
                Assets.find<ItemAsset>(new Guid("de94da4a993e4ebdbfd56d6e73d0af78")),       // AmmoCrate_1
                [
                    Assets.find<EffectAsset>(new Guid("f582c9cb20ee40dd926175591e632449")), // Physical_AmmoCrate_1_Inst1
                    Assets.find<EffectAsset>(new Guid("d97ce0ee9bd9427fbc4c00a2351d19fb")), // Physical_AmmoCrate_1_Inst2
                    Assets.find<EffectAsset>(new Guid("f48a15d6c2ea432cadcd3ca3a2a03a58")), // Physical_AmmoCrate_1_Inst3
                    Assets.find<EffectAsset>(new Guid("b77ea88db84c46c2a7c6f6fe9261d28a")), // Physical_AmmoCrate_1_Inst4
                    Assets.find<EffectAsset>(new Guid("0dfb69f64263421b858f498468098e78")), // Physical_AmmoCrate_1_Inst5
                    Assets.find<EffectAsset>(new Guid("e8fe0fca755244d8b4b7685c2e83478a")), // Physical_AmmoCrate_1_Inst6
                    Assets.find<EffectAsset>(new Guid("81f788d520854a68afe468c1313319ee")), // Physical_AmmoCrate_1_Inst7
                    Assets.find<EffectAsset>(new Guid("b0496a24aa714a2faef73b9f20a8c89d")), // Physical_AmmoCrate_1_Inst8
                    Assets.find<EffectAsset>(new Guid("48d1474f7e7f43909a77eefaa26bba08")), // Physical_AmmoCrate_1_Inst9
                    Assets.find<EffectAsset>(new Guid("6aaf8a12198c48148036522357773ea2"))  // Physical_AmmoCrate_1_Inst10
                ],
                this
            ),
            new FallingEffectGroup(
                Assets.find<ItemAsset>(new Guid("5c88dbd8e81444678f0f3a653f3e7e5d")),       // AmmoCrate_2
                [
                    Assets.find<EffectAsset>(new Guid("3703f73e6c814fbd8ff7f92d020ef32a")), // Physical_AmmoCrate_2_Inst1
                    Assets.find<EffectAsset>(new Guid("4bcddaa9c10248099b53325bea163c51")), // Physical_AmmoCrate_2_Inst2
                    Assets.find<EffectAsset>(new Guid("02932f975a2b4ef199ac304f82424b53")), // Physical_AmmoCrate_2_Inst3
                    Assets.find<EffectAsset>(new Guid("9b1d8a9a33b74c57a43403d32e7e4e26")), // Physical_AmmoCrate_2_Inst4
                    Assets.find<EffectAsset>(new Guid("c6b59e93a9dc401aaccaa16757eef32f")), // Physical_AmmoCrate_2_Inst5
                    Assets.find<EffectAsset>(new Guid("3495eb27c7e840808758c0c6598f2601")), // Physical_AmmoCrate_2_Inst6
                    Assets.find<EffectAsset>(new Guid("cb23b9ad7dc4491fb56239c2285b2bd1")), // Physical_AmmoCrate_2_Inst7
                    Assets.find<EffectAsset>(new Guid("0d3d079caa264477b374a34b7226eef1")), // Physical_AmmoCrate_2_Inst8
                    Assets.find<EffectAsset>(new Guid("374156907c8b4000a904593dbf966393")), // Physical_AmmoCrate_2_Inst9
                    Assets.find<EffectAsset>(new Guid("5557fdf828824cb3ab450a3b466a3f25"))  // Physical_AmmoCrate_2_Inst10
                ],
                this
            )
        ];
    }

    /// <summary>
    /// Spawn a falling effect at world position of <paramref name="parent"/>.
    /// </summary>
    /// <inheritdoc cref="CreateFallingEffect{TEffect,TState}(ItemAsset,Vector3,Quaternion,ref TState,ConfigureFallingEffect,Action)"/>
    public FallingEffect CreateFallingEffect(ItemAsset item, Transform parent, ConfigureFallingEffect? configure = null, Action? onSettle = null)
    {
        object? state = null;
        return CreateFallingEffect<FallingEffect<object?>, object?>(item, parent, ref state, configure, onSettle);
    }

    /// <summary>
    /// Spawn a falling effect at world position of <paramref name="parent"/>.
    /// </summary>
    /// <inheritdoc cref="CreateFallingEffect{TEffect,TState}(ItemAsset,Vector3,Quaternion,ref TState,ConfigureFallingEffect,Action)"/>
    public FallingEffect CreateFallingEffect(ItemAsset item, ITransformObject parent, ConfigureFallingEffect? configure = null, Action? onSettle = null)
    {
        object? state = null;
        return CreateFallingEffect<FallingEffect<object?>, object?>(item, parent, ref state, configure, onSettle);
    }

    /// <summary>
    /// Spawn a falling effect at the given position.
    /// </summary>
    /// <inheritdoc cref="CreateFallingEffect{TEffect,TState}(ItemAsset,Vector3,Quaternion,ref TState,ConfigureFallingEffect,Action)"/>
    public FallingEffect CreateFallingEffect(ItemAsset item, Vector3 position, Quaternion rotation, ConfigureFallingEffect? configure = null, Action? onSettle = null)
    {
        object? state = null;
        return CreateFallingEffect<FallingEffect<object?>, object?>(item, position, rotation, ref state, configure, onSettle);
    }

    /// <summary>
    /// Spawn a falling effect at world position of <paramref name="parent"/>.
    /// </summary>
    /// <inheritdoc cref="CreateFallingEffect{TEffect,TState}(ItemAsset,Vector3,Quaternion,ref TState,ConfigureFallingEffect,Action)"/>
    public TEffect CreateFallingEffect<TEffect, TState>(ItemAsset item, Transform parent, ref TState state, ConfigureFallingEffect? configure = null, Action? onSettle = null)
        where TEffect : FallingEffect<TState>
    {
        GameThread.AssertCurrent();

        parent.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        return CreateFallingEffect<TEffect, TState>(item, position, rotation, ref state, configure, onSettle);
    }

    /// <summary>
    /// Spawn a falling effect at world position of <paramref name="parent"/>.
    /// </summary>
    /// <inheritdoc cref="CreateFallingEffect{TEffect,TState}(ItemAsset,Vector3,Quaternion,ref TState,ConfigureFallingEffect,Action)"/>
    public TEffect CreateFallingEffect<TEffect, TState>(ItemAsset item, ITransformObject parent, ref TState state, ConfigureFallingEffect? configure = null, Action? onSettle = null)
        where TEffect : FallingEffect<TState>
    {
        GameThread.AssertCurrent();

        return CreateFallingEffect<TEffect, TState>(item, parent.Position, parent.Rotation, ref state, configure, onSettle);
    }

    /// <summary>
    /// Spawn a falling effect at the given position.
    /// </summary>
    /// <exception cref="ArgumentException">Item asset is not configured in a group.</exception>
    /// <exception cref="InvalidOperationException">Level not loaded or invalid asset configured.</exception>
    public TEffect CreateFallingEffect<TEffect, TState>(ItemAsset item, Vector3 position, Quaternion rotation, ref TState state, ConfigureFallingEffect? configure = null, Action? onSettle = null)
        where TEffect : FallingEffect<TState>
    {
        GameThread.AssertCurrent();

        FallingEffectGroup? group = Groups.FirstOrDefault(x => x.Item.GUID == item.GUID);
        if (group == null)
            throw new ArgumentException($"No group configured for item {item.name}.", nameof(item));
        
        int indexInGroup = group.GetNextFreeAssetIndex(out EffectAsset asset);
        bool replicate = true;
        if (indexInGroup < 0)
        {
            _logger.LogWarning($"Ran out of free assets for group {group.Item.name}. Only spawning on server.");
            replicate = false;
        }

        FallingEffectInstantiationArgs config = new FallingEffectInstantiationArgs(this, _serviceProvider, asset, group, indexInGroup)
        {
            IsReplicated = replicate
        };

        configure?.Invoke(ref config);

        if (EffectManager.instance == null)
            throw new InvalidOperationException("Level not loaded.");
        if (!config.Asset.spawnOnDedicatedServer)
            throw new InvalidOperationException($"Asset {config.Asset.name} will not spawn on the dedicated server. Add the Spawn_On_Dedicated_Server flag.");
        if (config.Asset.randomizeRotation)
            throw new InvalidOperationException($"Asset {config.Asset.name} will have its rotation randomized. Set Randomize_Rotation to false.");
        if (config.Asset.lifetimeSpread >= config.Asset.lifetime)
            throw new InvalidOperationException($"Asset {config.Asset.name} has a Lifetime_Spread which is >= than it's Lifetime.");

        GameObject prefab = config.Asset.effect;
        if (prefab == null)
            throw new InvalidOperationException($"Asset {config.Asset.name}'s Effect prefab didn't load.");

        // apply compression so position is the exact same on client and server
        MathUtility.CompressVector3(ref position);
        MathUtility.CompressQuaternion(ref rotation);

        TriggerEffectParameters p = new TriggerEffectParameters(config.Asset);
        p.SetRotation(rotation);
        p.position = position;
        p.reliable = false;
        p.wasInstigatedByPlayer = true;
        p.shouldReplicate = replicate;

        EffectManager.triggerEffect(p);
        PoolReference? lastSpawnedEffect = Interlocked.Exchange(ref LastEffectSpawnedOnDedicatedServerPatch.LastEffectSpawned, null);

        if (lastSpawnedEffect == null)
            throw new Exception("Failed to spawn effect. This shouldn't happen.");

        // check if effect is the correct type
        if (!EffectManager.pool.pools.TryGetValue(prefab, out GameObjectPool pool) || lastSpawnedEffect.pool != pool)
            throw new Exception("Failed to spawn effect. Failed to find pooled effect.");

        TEffect fallingEffect = lastSpawnedEffect.gameObject.GetOrAddComponent<TEffect>();

        fallingEffect.Initialize(ref state, in config, onSettle);

        if (replicate)
        {
            group.AddEffect(fallingEffect);
        }
        else
        {
            _nonReplicatedEffects.Add(fallingEffect);
        }

        return fallingEffect;
    }

    internal void DestroyFallingEffect(FallingEffect effect, bool settle = false, bool destroy = true)
    {
        if (settle && destroy)
            throw new ArgumentException("Can't settle on destroy.", nameof(destroy));

        GameThread.AssertCurrent();

        if (effect.IsDestroyed)
        {
            effect.LogMessage("Already destroyed.");
            return;
        }

        effect.LogMessage("Destroying...");
        if (settle)
        {
            effect.SkipWaitForSettle();
        }

        effect.IsDestroyed = true;
        if (effect.IsReplicated)
        {
            if (destroy)
            {
                effect.LogMessage("Clearing...");
                EffectManager.ClearEffectByGuid_AllPlayers(effect.Effect.GUID);
            }

            effect.Group.RemoveEffect(effect);
        }
        else
        {
            _nonReplicatedEffects.Remove(effect);
        }

        effect.LogMessage("Destroying effect.");
        Object.Destroy(effect);
    }

    public void Dispose()
    {
        foreach (FallingEffectGroup group in Groups)
        {
            group.Dispose();
        }

        Groups = ImmutableArray<FallingEffectGroup>.Empty;

        for (int i = _nonReplicatedEffects.Count - 1; i >= 0; i--)
        {
            FallingEffect effect = _nonReplicatedEffects[i];
            DestroyFallingEffect(effect);
        }

        _nonReplicatedEffects.Clear();
    }

    public bool IsFallingEffectObstructed(ItemAsset item, Transform transform)
    {
        GameThread.AssertCurrent();

        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        return IsFallingEffectObstructed(item, position, rotation);
    }

    public bool IsFallingEffectObstructed(ItemAsset item, ITransformObject transform)
    {
        GameThread.AssertCurrent();

        return IsFallingEffectObstructed(item, transform.Position, transform.Rotation);
    }

    public bool IsFallingEffectObstructed(ItemAsset item, Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();

        FallingEffectGroup? group = Groups.FirstOrDefault(x => x.Item.GUID == item.GUID);
        if (group == null)
            throw new ArgumentException($"No group configured for item {item.name}.", nameof(item));

        EffectAsset effectAsset = group.Effects[0];

        if (!BuildableExtensions.TryGetObjectBounds(effectAsset.effect, effectAsset, out Bounds bounds))
        {
            return false;
        }

        return Physics.CheckBox(
            position,
            bounds.extents * 1.5f,
            rotation,
            (RayMasks.BLOCK_COLLISION | RayMasks.DEBRIS) & ~RayMasks.VEHICLE,
            QueryTriggerInteraction.Ignore
        );
    }
}