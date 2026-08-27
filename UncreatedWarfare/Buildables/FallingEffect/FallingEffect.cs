using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Landscapes;
using System;
using System.Diagnostics;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

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

    public Team Team => _config.Team;

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
        LogMessage($"Initialized. Effect: {config.Asset.name}.");
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

        LogMessage("Cancelled.");
        _config.Manager.DestroyFallingEffect(this);
    }

    /// <summary>
    /// Instantly settles the effect without waiting on it to become still.
    /// </summary>
    public void SkipWaitForSettle()
    {
        if (IsDestroyed)
            throw new ObjectDisposedException(nameof(FallingEffect), "Already destroyed.");

        LogMessage("Skipped settle.");
        Settle();
    }

    private float _spawnTime;
    private float _startedSettling;
    protected virtual void StillTick(float timeStill)
    {

    }
    protected virtual void MovedTick()
    {

    }

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

            LandscapeCoord c = new LandscapeCoord(_previousPosition);
            HeightmapCoord h = new HeightmapCoord(c, _previousPosition);
            float approxHeight = Landscape.getTile(c)?.heightmap[h.x, h.y] ?? 0f;
            if (position.y < approxHeight)
            {
                float pt = TerrainUtility.GetHighestPoint(in _previousPosition, approxHeight);
                transform.position = _previousPosition with { y = pt };
                Settle();
            }
            else if (t - _spawnTime >= _lifetime)
            {
                Settle();
                NeedsClearing = !Mathf.Approximately(_config.Asset.lifetime, 0f);
            }
            else
            {
                MovedTick();
            }

            _previousPosition = position;
            _previousRotation = rotation;
        }
        else if (float.IsNaN(_startedSettling))
        {
            _startedSettling = t;
        }
        else
        {
            float stillTime = t - _startedSettling;
            StillTick(stillTime);
            if (stillTime >= _config.SettleTime)
            {
                Settle();
                NeedsClearing = true;
            }
        }
    }

    private void Settle(bool destroy = true)
    {
        if (_didSettle)
            return;

        _didSettle = true;

        LogMessage("Settling.");

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

        if (destroy)
        {
            _config.Manager.DestroyFallingEffect(this);
        }
    }

    protected virtual void OnSettle()
    {

    }

    // OnDisable is called because of pooling, not OnDestroy
    private void OnDisable()
    {
        if (!_didSettle && _config.SettleOnLifetimeElapsed)
        {
            Settle(destroy: false);
        }

        if (!IsDestroyed)
        {
            _config.Manager.DestroyFallingEffect(this, destroy: false);
        }

        Dispose();
    }

    protected virtual void Dispose()
    {
    }

    [Conditional("FALLING_EFFECT_DEBUG_LOGGING")]
    internal void LogMessage(string msg, LogLevel lvl = LogLevel.Debug)
    {
        ILogger<FallingEffect> logger = ServiceProvider.GetRequiredService<ILogger<FallingEffect>>();

        logger.Log(lvl, $"[{Item.FriendlyName} (#{IndexInGroup})] {msg}");
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

    internal bool IsReplicated;

    public readonly FallingEffectManager Manager;
    public readonly IServiceProvider ServiceProvider;
    public readonly EffectAsset Asset;

    public Team Team;
    public float SettleTime;
    public WarfarePlayer? Owner;
    public bool SettleOnLifetimeElapsed;

    internal FallingEffectInstantiationArgs(FallingEffectManager manager, IServiceProvider serviceProvider, EffectAsset asset, FallingEffectGroup group, int indexInGroup)
    {
        Manager = manager;
        ServiceProvider = serviceProvider;
        Asset = asset;
        Group = group;
        IndexInGroup = indexInGroup;

        SettleOnLifetimeElapsed = true;
        SettleTime = 0.4f;
        Team = Team.NoTeam;
    }
}