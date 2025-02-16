#if DEBUG

#define PROJECTILE_TRACERS

#endif

using DanielWillett.ReflectionTools;
using SDG.Framework.Landscapes;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Projectiles;

/// <summary>
/// Simulates projectiles to guess their landing position before they land.
/// </summary>
/// <remarks>To actually use it subscribe to <see cref="WarfareProjectile.OnProjectileSolved"/>.</remarks>
internal class ProjectileSolver : ILevelHostedService, IDisposable
{
    private readonly ILogger<ProjectileSolver> _logger;
    private readonly WarfareLifetimeComponent _warfareLifetimeComponent;

    private static readonly InstanceGetter<Rocket, bool>? GetIsExploded
        = Accessor.GenerateInstanceGetter<Rocket, bool>("isExploded", throwOnError: false);

    // note this is in simulated time, not actual time
    private const float MaximumSimulationTime = 30f;

    private readonly Queue<WarfareProjectile> _queue = new Queue<WarfareProjectile>(3);

    private Scene _simScene;
    private Scene _mainScene;
    private PhysicsScene _mainPhysxScene;
    private PhysicsScene _physxScene;
    private WarfareProjectile? _current;
    private int _isSetUp;
    private bool _isInitialized;

    public ProjectileSolver(ILogger<ProjectileSolver> logger, WarfareLifetimeComponent lifetimeComponent)
    {
        _logger = logger;
        _warfareLifetimeComponent = lifetimeComponent;
    }

    public void BeginSolvingProjectile(WarfareProjectile component)
    {
        GameThread.AssertCurrent();

        if (GetIsExploded == null)
            return;

        if (_isInitialized && _current == null)
        {
            _current = component;
            _warfareLifetimeComponent.StartCoroutine(Simulate());
        }
        else
            _queue.Enqueue(component);
    }

    UniTask ILevelHostedService.LoadLevelAsync(CancellationToken token)
    {
        _mainScene = SceneManager.GetActiveScene();
        _simScene = SceneManager.CreateScene("SimulationScene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        _physxScene = _simScene.GetPhysicsScene();
        _mainPhysxScene = _mainScene.GetPhysicsScene();

        if (Interlocked.Exchange(ref _isSetUp, 1) == 0)
        {
            Physics.autoSimulation = false;
            TimeUtility.physicsUpdated += FixedUpdate;
        }
        
        // creates a new physics scene containing all the major colliders from the main world
        //  this includes landscape and collision-important objects
        FieldInfo? tileField = typeof(Landscape).GetField("tiles", BindingFlags.Static | BindingFlags.NonPublic);

        if (tileField?.GetValue(null) is Dictionary<LandscapeCoord, LandscapeTile> d)
        {
            _logger.LogDebug("Found {0} tiles to add to scene.", d.Count);
            foreach (LandscapeTile tile in d.Values)
            {
                GameObject obj = Object.Instantiate(tile.gameObject, tile.gameObject.transform.position, tile.gameObject.transform.rotation);
                SceneManager.MoveGameObjectToScene(obj, _simScene);
                _logger.LogConditional("Adding ({0}, {1}) tile to scene.", tile.coord.x, tile.coord.y);
            }
        }
        else
        {
            _logger.LogWarning("Failed to add tiles to sim scene.");
        }

        for (int i = 0; i < Level.level.childCount; ++i)
        {
            Transform t = Level.level.GetChild(i);
            GameObject obj = Object.Instantiate(t.gameObject, t.position, t.rotation);
            SceneManager.MoveGameObjectToScene(obj, _simScene);
            _logger.LogDebug("Adding {0} clip to scene.", t.name);
        }

        foreach (ObjectInfo objInfo in LevelObjectUtility.EnumerateObjects())
        {
            LevelObject obj = objInfo.Object;
            Transform? transform = obj.transform;
            if (transform == null || obj.asset is not { isCollisionImportant: true })
                continue;

            GameObject? model = objInfo.Object.asset.GetOrLoadModel();
            if (model == null)
                continue;

            GameObject newObject = Object.Instantiate(model, transform.position, transform.rotation);
            if (objInfo.Object.asset.useScale)
            {
                newObject.transform.localScale = transform.localScale;
            }

            Rigidbody rigidbody = newObject.transform.GetComponent<Rigidbody>();
            if (rigidbody != null)
                Object.Destroy(rigidbody);

            SceneManager.MoveGameObjectToScene(newObject, _simScene);
        }

        _logger.LogInformation("Created projectile scene.");
        _isInitialized = true;
        if (_queue.TryDequeue(out _current))
        {
            _warfareLifetimeComponent.StartCoroutine(Simulate());
        }
        return UniTask.CompletedTask;
    }

    private void FixedUpdate()
    {
        // because Physics.autoSimulation is turned off, this continues to simulate the main physics scene.
        _mainPhysxScene.Simulate(Time.fixedDeltaTime);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isSetUp, 0) == 0)
            return;

        Physics.autoSimulation = true;
        TimeUtility.physicsUpdated -= FixedUpdate;
    }

    private IEnumerator Simulate()
    {
        WarfareProjectile component = _current!;

        if (component.Asset.projectile != null)
        {
            Transform transform = Object.Instantiate(
                component.Asset.projectile,
                component.Origin,
                Quaternion.LookRotation(component.Direction) * Quaternion.Euler(90f, 0.0f, 0.0f)
            ).transform;

            SceneManager.MoveGameObjectToScene(transform.gameObject, _simScene);

            transform.name = "Projectile_Simulation";
            Object.Destroy(transform.gameObject, component.Asset.projectileLifespan);

            if (transform.TryGetComponent(out Rigidbody body))
            {
                float force = component.Ammo?.projectileLaunchForceMultiplier ?? 1f;
                body.AddForce(component.Direction * component.Asset.ballisticForce * force);
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            DetectComponent c = transform.gameObject.AddComponent<DetectComponent>();

            c.Logger = _logger;
            c.IgnoreTransform = component.Rocket.ignoreTransform;

            float fixedDeltaTime = Time.fixedDeltaTime;

            int iter = Mathf.CeilToInt(MaximumSimulationTime / fixedDeltaTime);
            int skip = Mathf.CeilToInt(1f / (fixedDeltaTime * 1.5f));

            int i = 0;
            float seconds;

#if PROJECTILE_TRACERS
            float lastTracerSent = 0f;
            EffectAsset? tracerAsset = Assets.find<EffectAsset>(new Guid("50dbb9c23ae647b8adb829a771742d4c"));
#endif

            for (; !component.HasLanded && !c.IsExploded && i < iter; ++i)
            {

#if PROJECTILE_TRACERS
                seconds = i * fixedDeltaTime;
                if (seconds - lastTracerSent > 0.25f)
                {
                    if (tracerAsset != null)
                        EffectUtility.TriggerEffect(tracerAsset, Level.size * 2, transform.gameObject.transform.position, true);
                    lastTracerSent = seconds;
                }
#endif

                _physxScene.Simulate(fixedDeltaTime);

                if (i % skip == 0)
                {
                    yield return null;
                }
            }

            seconds = i * fixedDeltaTime;

            Vector3 projectedPosition = c.HitOne ? c.LastPosition : transform.position;
            DateTime projectedLandTime = component.LaunchTime.AddSeconds(seconds);
            component.CompletePrediction(projectedPosition, projectedLandTime);

            Object.Destroy(transform.gameObject);
        }

        if (_queue.TryDequeue(out _current))
        {
            _warfareLifetimeComponent.StartCoroutine(Simulate());
        }
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        SceneManager.UnloadSceneAsync(_simScene);
        _simScene = default;
        _physxScene = default;
        Physics.autoSimulation = true;
    }

    private class DetectComponent : MonoBehaviour
    {
        public ILogger<ProjectileSolver>? Logger;

        public Vector3 LastPosition;
        public bool HitOne;

        public bool IsExploded;
        public Transform? IgnoreTransform;

        [UsedImplicitly]
        private void Start()
        {
#if PROJECTILE_TRACERS
            Logger?.LogConditional("Added component to {0}.", gameObject.name);
#endif
        }

        [UsedImplicitly]
        private void OnTriggerEnter(Collider other)
        {
#if PROJECTILE_TRACERS
            Logger?.LogDebug("hit {0}.", other.name);
#endif

            if (IsExploded || other.isTrigger || IgnoreTransform != null && (other.transform == IgnoreTransform || other.transform.IsChildOf(IgnoreTransform)))
                return;

            IsExploded = true;

            if (!gameObject.TryGetComponent(out Rigidbody body))
                return;

            body.useGravity = false;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            HitOne = true;
            LastPosition = transform.position;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            IsExploded = true;
        }
    }
}

public delegate void ProjectileSolved(WarfareProjectile projectile, Vector3 projectedHitPosition, DateTime projectedImpactTime);