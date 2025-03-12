using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Projectiles;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Projectiles;

/// <summary>
/// Handles invoking the <see cref="ProjectileSpawned"/> and <see cref="ProjectileExploding"/> events.
/// </summary>
/// <remarks>Also adds a hotfix for ghost projectiles.</remarks>
public class WarfareProjectile : MonoBehaviour
{
    private static readonly Action<Rocket, Collider>? ExplodeMethod
        = Accessor.GenerateInstanceCaller<Rocket, Action<Rocket, Collider>>("OnTriggerEnter", throwOnError: false);

    private ProjectileSolved? _onProjectileSolved;

    private Vector3 _previousPosition;
    private bool _isSolving;

    private bool _solved;
    private DateTime _solvedImpactTime;
    private Vector3 _solvedLandingPosition;

#nullable disable
    private EventDispatcher _eventDispatcher;
    private ProjectileSolver _projectileSolver;

    public WarfarePlayer Owner { get; private set; }
    public Team Team { get; private set; }
    public UseableGun Gun { get; private set; }
    public ItemGunAsset Asset { get; private set; }
    public DateTime LaunchTime { get; private set; }
    public DateTime FireTime { get; private set; }
    public DateTime ImpactTime { get; private set; }
    public bool CanSolve { get; private set; }
    public Vector3 Origin { get; private set; }
    public Vector3 Direction { get; private set; }
    public Rocket Rocket { get; private set; }

#nullable restore
    public ItemMagazineAsset? Ammo { get; private set; }
    public ItemBarrelAsset? Barrel { get; private set; }
    public InteractableVehicle? OwnerVehicle { get; private set; }
    public bool HasLanded => HasExploded;
    public bool HasSpawned { get; private set; }
    public bool HasExploded { get; private set; }

    /// <summary>
    /// Run a callback when the projectile has been solved. If it's already been solved it'll be invoked immediately.
    /// </summary>
    /// <remarks>No need to unsubscribe after usage.</remarks>
    public event ProjectileSolved OnProjectileSolved
    {
        add
        {
            if (!CanSolve)
                return;

            if (GameThread.IsCurrent)
            {
                AddProjectileSolvedContinuation(value);
                return;
            }

            ProjectileSolved v = value;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                AddProjectileSolvedContinuation(v);
            });
        }
        remove
        {
            if (GameThread.IsCurrent)
            {
                if (!_solved && CanSolve)
                    _onProjectileSolved -= value;
                return;
            }

            ProjectileSolved v = value;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (!_solved && CanSolve)
                    _onProjectileSolved -= v;
            });
        }
    }

    internal void Initialize(Rocket rocket, WarfarePlayer owner, UseableGun gun, ItemGunAsset asset, ItemMagazineAsset? magazine, ItemBarrelAsset? barrel, Vector3 origin, Vector3 direction, bool canSolve, InteractableVehicle? ownerVehicle, EventDispatcher eventDispatcher, ProjectileSolver projectileSolver)
    {
        _eventDispatcher = eventDispatcher;
        _projectileSolver = projectileSolver;


        Rocket = rocket;
        Origin = origin;
        Direction = direction;

        OwnerVehicle = ownerVehicle;
        Owner = owner;
        Team = owner.Team;
        Asset = asset;
        Gun = gun;
        Ammo = magazine;
        Barrel = barrel;
        CanSolve = canSolve;

        FireTime = DateTime.UtcNow;
    }

    internal bool InvokeExploding(Collider other, ExplosionParameters parameters)
    {
        // cant use OnTriggerEnter like we originally did because we can't know it'll occur before the explosion
        HasExploded = true;
        ImpactTime = DateTime.UtcNow;
        
        InteractableVehicle? hitVehicle = other.GetComponentInParent<InteractableVehicle>();

        ProjectileExploding args = new ProjectileExploding(parameters)
        {
            Asset = Asset,
            Barrel = Barrel,
            Ammo = Ammo,
            Gun = Gun,
            HitObject = other.transform,
            HitCollider = other,
            HitPosition = _previousPosition,
            RocketComponent = Rocket,
            Object = gameObject,
            Player = Owner,
            Projectile = this,
            HitVehicle = hitVehicle,
            ImpactTime = ImpactTime,
            PredictedHitPosition = _solved ? _solvedLandingPosition : null,
            PredictedImpactTime = _solved ? _solvedImpactTime : null,
            LaunchedTime = LaunchTime,
            FiredTime = FireTime
        };

        EventContinuations.Dispatch(args, _eventDispatcher, CancellationToken.None, out bool shouldAllow,
            continuation: static args =>
            {
                ExplodeMethod?.Invoke(args.RocketComponent, args.HitCollider);
            }
        );

        return shouldAllow;
    }

    private void AddProjectileSolvedContinuation(ProjectileSolved solved)
    {
        if (solved == null)
            return;

        if (HasExploded)
        {
            TryInvoke(solved, _previousPosition, ImpactTime);
            return;
        }

        if (_solved)
        {
            TryInvoke(solved, _solvedLandingPosition, _solvedImpactTime);
            return;
        }

        _onProjectileSolved += solved;
        if (_isSolving)
            return;

        _projectileSolver.BeginSolvingProjectile(this);
        _isSolving = true;
    }

    internal void CompletePrediction(Vector3 position, DateTime impactTime)
    {
        _solved = true;
        _solvedImpactTime = impactTime;
        _solvedLandingPosition = position;
        _isSolving = false;

        if (_onProjectileSolved == null)
            return;

        if (HasExploded)
        {
            position = _previousPosition;
            impactTime = ImpactTime;
        }

        TryInvoke(_onProjectileSolved, position, impactTime);
        _onProjectileSolved = null;
    }

    private void TryInvoke(ProjectileSolved callbacks, Vector3 position, DateTime impactTime)
    {
        try
        {
            callbacks(this, position, impactTime);
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogError(ex, $"Error invoking ProjectileSolved callback for {Asset.itemName}'s projectile on player {Owner}.");
        }
    }

    [UsedImplicitly]
    private void Start()
    {
        LaunchTime = DateTime.UtcNow;
        _previousPosition = transform.position;

        HasSpawned = true;
        ProjectileSpawned args = new ProjectileSpawned
        {
            Player = Owner,
            Gun = Gun,
            Asset = Asset,
            Ammo = Ammo,
            Barrel = Barrel,
            Object = gameObject,
            RocketComponent = Rocket,
            Projectile = this,
            Vehicle = OwnerVehicle,
            LaunchedTime = LaunchTime,
            FiredTime = FireTime
        };

        _ = _eventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }

    [UsedImplicitly]
    public void FixedUpdate()
    {
        if (HasExploded || !Physics.Linecast(_previousPosition, transform.position, out RaycastHit hit, RayMasks.GROUND | RayMasks.GROUND2 | RayMasks.LARGE | RayMasks.MEDIUM))
        {
            _previousPosition = transform.position;
            return;
        }

        _previousPosition = transform.position;

        Collider other = hit.collider;
        if (other.isTrigger || Rocket.ignoreTransform != null
                                && (other.transform == Rocket.ignoreTransform || other.transform.IsChildOf(Rocket.ignoreTransform)))
        {
            return;
        }

        if (Rocket == null || ExplodeMethod == null)
            return;

        ExplodeMethod.Invoke(Rocket, other);
#if DEBUG
        WarfareModule.Singleton.GlobalLogger.LogWarning($"Ghost rocket prevented: {Asset}.");
#endif
    }

#if false && DEBUG // todo: Disable
    private float _lastSpawn;
    [UsedImplicitly]
    void Update()
    {
        float time = Time.time;

        if (time - _lastSpawn > 0.25f)
        {
            if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                EffectUtility.TriggerEffect(effect, Level.size * 2, this.transform.position, false);
            _lastSpawn = time;
        }
    }
#endif
}