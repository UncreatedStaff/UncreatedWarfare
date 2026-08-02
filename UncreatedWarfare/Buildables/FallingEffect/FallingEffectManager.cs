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

    internal void DestroyFallingEffect(FallingEffect effect, bool settle = false)
    {
        GameThread.AssertCurrent();

        if (effect.IsDestroyed)
            return;

        if (settle)
        {
            effect.SkipWaitForSettle();
        }

        effect.IsDestroyed = true;
        if (effect.IsReplicated)
        {
            EffectManager.ClearEffectByGuid_AllPlayers(effect.Effect.GUID);
            effect.Group.RemoveEffect(effect);
        }
        else
        {
            _nonReplicatedEffects.Remove(effect);
        }

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
}