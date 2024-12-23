#if DEBUG

#define PROJECTILE_TRACERS

#endif

using DanielWillett.ReflectionTools;
using SDG.Framework.Landscapes;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Projectiles;
public class ProjectileSolver : ILevelHostedService, IDisposable, IEventListener<PlayerLeft>
{
    private readonly ILogger<ProjectileSolver> _logger;
    private readonly WarfareTimeComponent _warfareTimeComponent;
    private readonly IPlayerService _playerService;

    private static readonly InstanceGetter<Rocket, bool>? GetIsExploded
        = Accessor.GenerateInstanceGetter<Rocket, bool>("isExploded", throwOnError: false);

    private readonly PlayerDictionary<ItemMagazineAsset> _lastProjectileShot = new PlayerDictionary<ItemMagazineAsset>();

    // note this is in simulated time, not actual time
    private const float MaximumSimulationTime = 30f;

    private Scene _simScene;
    private Scene _mainScene;
    private PhysicsScene _mainPhysxScene;
    private PhysicsScene _physxScene;
    private readonly Queue<ProjectileData> _queue = new Queue<ProjectileData>(3);
    private ProjectileData _current;
    private int _isSetUp;

    public ProjectileSolver(ILogger<ProjectileSolver> logger, WarfareTimeComponent warfareTimeComponent, IPlayerService playerService)
    {
        _logger = logger;
        _warfareTimeComponent = warfareTimeComponent;
        _playerService = playerService;
    }

    internal void RegisterLastMagazineShot(CSteamID player, ItemMagazineAsset magazine)
    {
        _lastProjectileShot[player] = magazine;
    }

    public void BeginSolvingProjectile(GameObject projectileObject, Vector3 origin, Vector3 direction, UseableGun gun, ProjectileSolved? callback)
    {
        GameThread.AssertCurrent();

        if (GetIsExploded == null)
            return;

        ProjectileData data = default;
        data.Player = _playerService.GetOnlinePlayer(gun.player);
        data.ProjectileObject = projectileObject;
        data.Origin = origin;
        data.Direction = direction;
        data.Gun = gun;
        data.GunAsset = gun.equippedGunAsset;
        data.LaunchTime = Time.realtimeSinceStartup;
        data.Callback = callback;
        _lastProjectileShot.TryGetValue(gun.player, out data.AmmunitionType);
        data.MagazineForceMultiplier = data.AmmunitionType?.projectileLaunchForceMultiplier ?? 1f;

        if (_current.Gun is null)
        {
            _current = data;
            _warfareTimeComponent.StartCoroutine(Simulate());
        }
        else
            _queue.Enqueue(data);
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
        ProjectileData data = _current;

        if (data.Gun?.equippedGunAsset?.projectile != null)
        {
            Transform transform = Object.Instantiate(
                data.Gun.equippedGunAsset.projectile,
                data.Origin,
                Quaternion.LookRotation(data.Direction) * Quaternion.Euler(90f, 0.0f, 0.0f)
            ).transform;

            SceneManager.MoveGameObjectToScene(transform.gameObject, _simScene);

            transform.name = "Projectile_Simulation";
            Object.Destroy(transform.gameObject, data.Gun.equippedGunAsset.projectileLifespan);

            if (transform.TryGetComponent(out Rigidbody body))
            {
                body.AddForce(data.Direction * data.Gun.equippedGunAsset.ballisticForce * data.MagazineForceMultiplier);
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            DetectComponent c = transform.gameObject.AddComponent<DetectComponent>();

            if (!data.ProjectileObject.TryGetComponent(out Rocket rocket))
                yield break;

            c.Logger = _logger;
            c.IgnoreTransform = rocket.ignoreTransform;

            float fixedDeltaTime = Time.fixedDeltaTime;

            int iter = Mathf.CeilToInt(MaximumSimulationTime / fixedDeltaTime);
            int skip = Mathf.CeilToInt(1f / (fixedDeltaTime * 1.5f));

            int i = 0;
            float seconds;

#if PROJECTILE_TRACERS
            float lastTracerSent = 0f;
            EffectAsset? tracerAsset = Assets.find<EffectAsset>(new Guid("50dbb9c23ae647b8adb829a771742d4c"));
#endif

            for (; !c.IsExploded && i < iter; ++i)
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
                    if (GetIsExploded!(rocket))
                        yield break;
                }
            }

            seconds = i * fixedDeltaTime;

            Vector3 pos = transform.gameObject.transform.position;
            float landTime = data.LaunchTime + seconds;
            if (data.ProjectileObject != null && data.ProjectileObject.TryGetComponent(out ProjectileComponent comp))
            {
                comp.PredictedLandingPosition = pos;
                comp.PredictedImpactTime = landTime;
            }

            Object.Destroy(transform.gameObject);

            if (data.Callback != null)
            {
                try
                {
                    data.Callback.Invoke(data.Player, pos, landTime, data.GunAsset, data.AmmunitionType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invocing ProjectileSolver callback: {0}.", data.Callback.Method);
                }
            }
        }

        _current = default;
        if (_queue.TryDequeue(out _current))
        {
            _warfareTimeComponent.StartCoroutine(Simulate());
        }
    }

    [UsedImplicitly]
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    private void OnDestroy()
    {
        SceneManager.UnloadSceneAsync(_simScene);
        _simScene = default;
        _physxScene = default;
        Physics.autoSimulation = true;
    }
    private struct ProjectileData
    {
        public WarfarePlayer Player;
        public GameObject ProjectileObject;
        public Vector3 Origin;
        public Vector3 Direction;
        public UseableGun? Gun;
        public ProjectileSolved? Callback;
        public ItemMagazineAsset? AmmunitionType;
        public ItemGunAsset GunAsset;
        public float MagazineForceMultiplier;
        public float LaunchTime;
    }

    private class DetectComponent : MonoBehaviour
    {
        public ILogger<ProjectileSolver>? Logger;

        public bool IsExploded;
        public Transform? IgnoreTransform;

        [UsedImplicitly]
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        private void Start()
        {
#if PROJECTILE_TRACERS
            Logger?.LogConditional("Added component to {0}.", gameObject.name);
#endif
        }

        [UsedImplicitly]
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        private void OnTriggerEnter(Collider other)
        {
#if PROJECTILE_TRACERS
            Logger?.LogDebug("hit {0}.", other.name);
#endif

            if (IsExploded || other.isTrigger || IgnoreTransform != null && (other.transform == IgnoreTransform || other.transform.IsChildOf(IgnoreTransform)))
                return;

            IsExploded = true;

            if (gameObject.TryGetComponent(out Rigidbody body))
            {
                body.useGravity = false;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }

        [UsedImplicitly]
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        private void OnDestroy()
        {
            IsExploded = true;
        }
    }

    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        _lastProjectileShot.Remove(e.Steam64);
    }
}

public delegate void ProjectileSolved(WarfarePlayer? owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType);