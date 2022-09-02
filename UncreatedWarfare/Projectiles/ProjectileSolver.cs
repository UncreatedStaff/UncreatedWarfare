using SDG.Framework.Landscapes;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Uncreated.Warfare.Gamemodes.Flags.ZoneModel;

namespace Uncreated.Warfare.Projectiles;
internal class ProjectileSolver : MonoBehaviour
{
    private Scene _simScene;
    private Scene _mainScene;
    private PhysicsScene _mainPhysxScene;
    private PhysicsScene _physxScene;
    private readonly Queue<ProjectileData> _queue = new Queue<ProjectileData>(3);
    private ProjectileData _current;

    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void Start()
    {
        _mainScene = SceneManager.GetActiveScene();
        _simScene = SceneManager.CreateScene("SimulationScene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        _physxScene = _simScene.GetPhysicsScene();
        _mainPhysxScene = _mainScene.GetPhysicsScene();
        Physics.autoSimulation = false;
        if (typeof(Landscape).GetField("tiles", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Dictionary<LandscapeCoord, LandscapeTile> d)
        {
            L.LogDebug("Found " + d.Count + " tiles to add to scene.");
            foreach (LandscapeTile tile in d.Values)
            {
                GameObject obj = Instantiate(tile.gameObject, tile.gameObject.transform.position, tile.gameObject.transform.rotation);
                SceneManager.MoveGameObjectToScene(obj, _simScene);
                L.LogDebug("Adding (" + tile.coord.x + ", " + tile.coord.y + ") tile to scene.");
            }
        }
        else
            L.LogWarning("Failed to add tiles to sim scene.");
        for (int i = 0; i < Level.level.childCount; ++i)
        {
            Transform t = Level.level.GetChild(i);
            GameObject obj = Instantiate(t.gameObject, t.position, t.rotation);
            SceneManager.MoveGameObjectToScene(obj, _simScene);
            L.LogDebug("Adding " + t.name + " clip to scene.");
        }

#if DEBUG
        List<BoxCollider> bcs = new List<BoxCollider>(8);
#endif
        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                foreach (LevelObject obj2 in LevelObjects.objects[x, y].Where(x => x.asset != null && x.asset.type != EObjectType.SMALL && !getIsDecal(x)))
                {
                    GameObject? orig = obj2.asset.GetOrLoadModel();
                    if (orig != null)
                    {
                        GameObject obj = Instantiate(orig, obj2.transform.position, obj2.transform.rotation);
                        if (obj2.asset.useScale)
                            obj.transform.localScale = obj2.transform.localScale;
                        Rigidbody rigidbody = this.transform.GetComponent<Rigidbody>();
                        if (rigidbody != null)
                            Destroy(rigidbody);

                        SceneManager.MoveGameObjectToScene(obj, _simScene);
#if DEBUG
                        obj.GetComponentsInChildren(bcs);
                        for (int i = 0; i < bcs.Count; ++i)
                        {
                            BoxCollider c = bcs[i];
                            if (c.transform.localScale.x < 0 || c.transform.localScale.y < 0 || c.transform.localScale.z < 0 ||
                                c.size.x < 0 || c.size.y < 0 || c.size.z < 0)
                            {
                                L.LogWarning(obj2.asset.objectName + " (" + obj2.asset.id + ", " +
                                             obj2.asset.GUID.ToString("N") +
                                             "): Negative scale or size detected, recommended to fix.");
                            }
                        }

                        bcs.Clear();
#endif
                    }
                }
            }
        }
    }
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void Update()
    {
        if (_queue.Count > 0 && _current.gun is null)
        {
            _current = _queue.Dequeue();
            StartCoroutine(Simulate());
        }
    }

    //private static readonly InstanceGetter<UseableGun, Attachments> getAttachments = F.GenerateInstanceGetter<UseableGun, Attachments>("thirdAttachments", BindingFlags.NonPublic);
    private static readonly InstanceGetter<LevelObject, bool> getIsDecal = F.GenerateInstanceGetter<LevelObject, bool>("isDecal", BindingFlags.NonPublic);
    private static readonly InstanceGetter<Rocket, bool> getIsExploded = F.GenerateInstanceGetter<Rocket, bool>("isExploded", BindingFlags.NonPublic);
    private IEnumerator Simulate()
    {
        ProjectileData data = _current;
        Transform transform = Instantiate(data.gun.equippedGunAsset.projectile, data.origin, Quaternion.LookRotation(data.direction) * Quaternion.Euler(90f, 0.0f, 0.0f)).transform;
        SceneManager.MoveGameObjectToScene(transform.gameObject, _simScene);

        transform.name = "Projectile_SimClone";
        Destroy(transform.gameObject, data.gun.equippedGunAsset.projectileLifespan);
        if (transform.TryGetComponent(out Rigidbody body))
        {
            body.AddForce(data.direction * data.gun.equippedGunAsset.ballisticForce * data.magazineForceMultiplier);
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        DetectComponent c = transform.gameObject.AddComponent<DetectComponent>();

        if (!data.obj.TryGetComponent(out Rocket rocket))
            yield break;

        c.ignoreTransform = rocket.ignoreTransform;
        c.OriginalRocketData = rocket;

        Stopwatch st = new Stopwatch();
        st.Start();
        int i = 0;
        float lastSent = 0f;
        int iter = Mathf.CeilToInt(MAX_TIME / Time.fixedDeltaTime);
        int skip = Mathf.CeilToInt(1f / Time.fixedDeltaTime);
        float seconds = 0f;
        for (; !c.isExploded && i < iter; ++i)
        {
            seconds = i * Time.fixedDeltaTime;
            if (seconds - lastSent > 0.25f)
            {
                EffectManager.sendEffect(36130, Level.size * 2, transform.gameObject.transform.position);
                lastSent = seconds;
            }
            _physxScene.Simulate(Time.fixedDeltaTime);

            if (i % skip == 0)
            {
                yield return null;
                if (getIsExploded(rocket))
                    yield break;
            }
        }
        st.Stop();

        Vector3 pos = transform.gameObject.transform.position;
        float landTime = data.launchTime + seconds;
        if (data.obj != null && data.obj.TryGetComponent(out ProjectileComponent comp))
        {
            comp.PredictedLandingPosition = pos;
            comp.PredictedImpactTime = landTime;
        }
        Destroy(transform.gameObject);
        _current = default;
        data.callback?.Invoke(!data.gun.player.isActiveAndEnabled ? null : data.gun.player, pos, landTime, data.gunAsset, data.ammunitionType);
    }
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void FixedUpdate()
    {
        _mainPhysxScene.Simulate(Time.fixedDeltaTime);
    }

    private const float MAX_TIME = 30f;
    internal void GetLandingPoint(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, ProjectileLandingPointCalculated callback)
    {
        ItemMagazineAsset? ammo = gun.player.TryGetPlayerData(out UCPlayerData data) ? data.LastProjectedAmmoType : null;
        if (_current.gun is null)
        {
            _current = new ProjectileData(obj, origin, direction, gun, ammo, Time.realtimeSinceStartup, callback);
            StartCoroutine(Simulate());
        }
        else
            _queue.Enqueue(new ProjectileData(obj, origin, direction, gun, ammo, Time.realtimeSinceStartup, callback));
    }
    [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
    private void OnDestroy()
    {
        SceneManager.UnloadSceneAsync(_simScene);
        _simScene = default;
        _physxScene = default;
        Physics.autoSimulation = true;
    }
    private struct ProjectileData
    {
        public readonly GameObject obj;
        public readonly Vector3 origin;
        public readonly Vector3 direction;
        public readonly UseableGun gun;
        public readonly ProjectileLandingPointCalculated callback;
        public readonly ItemMagazineAsset? ammunitionType;
        public readonly ItemGunAsset gunAsset;
        public readonly float magazineForceMultiplier;
        public readonly float launchTime;
        public ProjectileData(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, ItemMagazineAsset? ammunitionType, float launchTime, ProjectileLandingPointCalculated callback)
        {
            this.obj = obj;
            this.origin = origin;
            this.direction = direction;
            this.gun = gun;
            this.callback = callback;
            this.launchTime = launchTime;
            this.gunAsset = gun.equippedGunAsset;
            this.ammunitionType = ammunitionType;
            if (ammunitionType != null)
                this.magazineForceMultiplier = ammunitionType.projectileLaunchForceMultiplier;
            else
                this.magazineForceMultiplier = 1f;
        }
    }

    private class DetectComponent : MonoBehaviour
    {
        public bool isExploded;
        public Transform ignoreTransform;
        public Rocket OriginalRocketData;
        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        private void Start()
        {
            L.LogDebug("Added component to " + gameObject.name + ".");
        }
        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        private void OnTriggerEnter(Collider other)
        {
            L.LogDebug("hit " + other.name);

            if (this.isExploded || other.isTrigger || this.ignoreTransform != null && (other.transform == this.ignoreTransform || other.transform.IsChildOf(this.ignoreTransform)))
                return;

            isExploded = true;

            if (gameObject.TryGetComponent(out Rigidbody body))
            {
                body.useGravity = false;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }
        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        private void OnDestroy()
        {
            isExploded = true;
        }
    }
}

public delegate void ProjectileLandingPointCalculated(Player? owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType);