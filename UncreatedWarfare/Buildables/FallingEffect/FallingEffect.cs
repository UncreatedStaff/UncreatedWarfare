using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Buildables;

public class FallingEffect : MonoBehaviour
{

#nullable disable

    protected IServiceProvider ServiceProvider => _config.ServiceProvider;
    internal bool NeedsClearing;
    internal int IndexInGroup => _config.IndexInGroup;
    internal FallingEffectGroup Group => _config.Group;
    internal bool IsDestroyed;
    internal bool IsReplicated => _config.IsReplicated;

    private Vector3 _previousPosition;
    private Quaternion _previousRotation;

    private bool _didSettle;
    private FallingEffectInstantiationArgs _config;

    // if the effect is alive for this long, just 
    private float _lifetime;

#nullable restore

    private Action? _onSettle;

    /// <summary>
    /// The player who dropped the item that spawned this effect.
    /// </summary>
    public WarfarePlayer? Owner => _config.Owner;

    /// <summary>
    /// The effect being used for this instance.
    /// </summary>
    public EffectAsset Effect => _config.Asset;

    /// <summary>
    /// The item which spawns this effect when dropped.
    /// </summary>
    public ItemAsset Item => Group.Item;

    private protected void BaseInitialize(in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        _onSettle = onSettle;
        _config = config;

        NeedsClearing = true;

        _lifetime = config.Asset.lifetime - config.Asset.lifetimeSpread;
        if (_lifetime < 0)
            throw new InvalidOperationException("Invalid effect asset: Lifetime - Lifetime_Spread is < 0.");

        transform.GetPositionAndRotation(out _previousPosition, out _previousRotation);

        _spawnTime = Time.realtimeSinceStartup;
        _startedSettling = float.NaN;
    }


    /// <summary>
    /// Cancels the current effect and destroys it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Already settled.</exception>
    public void Cancel()
    {
        if (_didSettle)
            throw new InvalidOperationException("Already settled.");

        if (IsDestroyed)
            return;

        _config.Manager.DestroyFallingEffect(this, false);
    }

    /// <summary>
    /// Instantly settles the effect without waiting on it to become still.
    /// </summary>
    public void SkipWaitForSettle()
    {
        if (IsDestroyed)
            throw new ObjectDisposedException(nameof(FallingEffect), "Already destroyed.");

        Settle();
    }

    private float _spawnTime;
    private float _startedSettling;

    protected virtual void FixedUpdate()
    {
        if (_didSettle)
            return;

        float t = Time.realtimeSinceStartup;

        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        bool isStill = MathfEx.IsNearlyEqual(position, _previousPosition, 0.05f)
                       && MathfEx.IsNearlyEqual(rotation, _previousRotation, 0.05f);

        if (!isStill)
        {
            _startedSettling = float.NaN;

            if (t - _spawnTime >= _lifetime)
            {
                Settle();
                NeedsClearing = !Mathf.Approximately(_config.Asset.lifetime, 0f);
            }
        }
        else if (float.IsNaN(_startedSettling))
        {
            _startedSettling = t;
        }
        else if (t - _startedSettling >= _config.SettleTime)
        {
            Settle();
            NeedsClearing = true;
        }
    }

    private void Settle()
    {
        if (_didSettle)
            return;

        _didSettle = true;

        try
        {
            OnSettle();
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, $"Exception thrown by parent OnSettle method {GetType()}.");
        }

        try
        {
            _onSettle?.Invoke();
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, $"Exception thrown by onSettle handler in {GetType()}.");
        }

        _config.Manager.DestroyFallingEffect(this);
    }

    protected virtual void OnSettle()
    {

    }

    // OnDisable is called because of pooling, not OnDestroy
    private void OnDisable()
    {
        if (!IsDestroyed)
        {
            _config.Manager.DestroyFallingEffect(this);
        }

        Dispose();
    }

    protected virtual void Dispose()
    {
    }
}

/// <summary>
/// Base class for more advanced falling effects that require special information to initialize.
/// </summary>
/// <typeparam name="TState">A type of struct or class that stores information to initialize the type.</typeparam>
public abstract class FallingEffect<TState> : FallingEffect
{
    protected internal virtual void Initialize(ref TState args, in FallingEffectInstantiationArgs config, Action? onSettle)
    {
        BaseInitialize(in config, onSettle);
    }
}

public struct FallingEffectInstantiationArgs
{
    internal readonly FallingEffectGroup Group;
    internal readonly int IndexInGroup;

    public readonly FallingEffectManager Manager;
    public readonly IServiceProvider ServiceProvider;
    public readonly EffectAsset Asset;
    public float SettleTime = 0.25f;
    public WarfarePlayer? Owner;

    internal bool IsReplicated;

    internal FallingEffectInstantiationArgs(FallingEffectManager manager, IServiceProvider serviceProvider, EffectAsset asset, FallingEffectGroup group, int indexInGroup)
    {
        Manager = manager;
        ServiceProvider = serviceProvider;
        Asset = asset;
        Group = group;
        IndexInGroup = indexInGroup;
    }
}