using SDG.Framework.Landscapes;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Uncreated.Warfare.Projectiles;
internal class ProjectileSolver : MonoBehaviour
{
    private Scene _simScene;
    private Scene _mainScene;
    private PhysicsScene _mainPhysxScene;
    private PhysicsScene _physxScene;
    private readonly Queue<ProjectileData> _queue = new Queue<ProjectileData>(3);
    private ProjectileData _current;
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


        for (int x = 0; x < Regions.WORLD_SIZE; ++x)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; ++y)
            {
                foreach (LevelObject obj2 in LevelObjects.objects[x, y].Where(x => x.asset != null && x.asset.type == EObjectType.LARGE))
                {
                    GameObject obj = Instantiate(obj2.transform.gameObject, obj2.transform.position, obj2.transform.rotation);

                    SceneManager.MoveGameObjectToScene(obj, _simScene);
                    //L.LogDebug("Adding " + obj2.asset.objectName + " object to scene.");
                }
            }
        }
    }
    private void Update()
    {
        if (_queue.Count > 0 && _current.gun is null)
        {
            _current = _queue.Dequeue();
            StartCoroutine(Simulate());
        }
    }

    private static readonly FieldInfo? attachmentField = typeof(UseableGun).GetField("thirdAttachments");
    private IEnumerator Simulate()
    {
        ProjectileData data = _current;
        Transform transform = Instantiate(data.gun.equippedGunAsset.projectile, data.origin, Quaternion.LookRotation(data.direction) * Quaternion.Euler(90f, 0.0f, 0.0f)).transform;
        SceneManager.MoveGameObjectToScene(transform.gameObject, _simScene);

        transform.name = "Projectile_SimClone";
        float magazineForceMultiplier = 1f;
        if (attachmentField?.GetValue(data.gun) is Attachments attachments)
            magazineForceMultiplier *= attachments.magazineAsset.projectileLaunchForceMultiplier;
        Destroy(transform.gameObject, data.gun.equippedGunAsset.projectileLifespan);
        if (transform.TryGetComponent(out Rigidbody body))
        {
            body.AddForce(data.direction * data.gun.equippedGunAsset.ballisticForce * magazineForceMultiplier);
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        DetectComponent c = transform.gameObject.AddComponent<DetectComponent>();
        if (data.obj.TryGetComponent(out Rocket rocket))
        {
            c.ignoreTransform = rocket.ignoreTransform;
            c.OriginalRocketData = rocket;
        }

        Stopwatch st = new Stopwatch();
        st.Start();
        int i = 0;
        float lastSent = 0f;
        int iter = Mathf.CeilToInt(MAX_TIME / Time.fixedDeltaTime);
        int skip = Mathf.CeilToInt(16f / Time.fixedDeltaTime);
        float seconds;
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
                yield return null;
        }
        st.Stop();

        L.LogDebug("Simmed " + (seconds = i * Time.fixedDeltaTime).ToString("F2") + " seconds in " + st.ElapsedMilliseconds.ToString("F5", Data.Locale) + "ms.");
        Vector3 pos = transform.gameObject.transform.position;
        Destroy(transform.gameObject);
        _current = default;
        data.callback(pos, seconds);
    }
    private void FixedUpdate()
    {
        _mainPhysxScene.Simulate(Time.fixedDeltaTime);
    }

    private const float MAX_TIME = 30f;
    internal void GetLandingPoint(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, Action<Vector3, float> callback)
    {
        if (_current.gun is null)
        {
            _current = new ProjectileData(obj, origin, direction, gun, callback);
            StartCoroutine(Simulate());
        }
        else
            _queue.Enqueue(new ProjectileData(obj, origin, direction, gun, callback));
    }
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
        public readonly Action<Vector3, float> callback;
        public ProjectileData(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, Action<Vector3, float> callback)
        {
            this.obj = obj;
            this.origin = origin;
            this.direction = direction;
            this.gun = gun;
            this.callback = callback;
        }
    }

    private class DetectComponent : MonoBehaviour
    {
        public bool isExploded;
        public Transform ignoreTransform;
        public Rocket OriginalRocketData;
        private void Start()
        {
            L.LogDebug("Added component to " + gameObject.name + ".");
        }
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
        private void OnDestroy()
        {
            isExploded = true;
        }
    }
}
