using JetBrains.Annotations;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Squads;
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

    [UsedImplicitly]
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
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
                foreach (LevelObject obj2 in LevelObjects.objects[x, y].Where(o => o.asset != null && o.asset.type != EObjectType.SMALL && !getIsDecal(o)))
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
    [UsedImplicitly]
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    private void Update()
    {
        if (_queue.Count > 0 && _current.Gun is null)
        {
            _current = _queue.Dequeue();
            StartCoroutine(Simulate());
        }
    }

    //private static readonly InstanceGetter<UseableGun, Attachments> getAttachments = Util.GenerateInstanceGetter<UseableGun, Attachments>("thirdAttachments", BindingFlags.NonPublic);
    private static readonly InstanceGetter<LevelObject, bool> getIsDecal = Util.GenerateInstanceGetter<LevelObject, bool>("isDecal", BindingFlags.NonPublic);
    private static readonly InstanceGetter<Rocket, bool> getIsExploded = Util.GenerateInstanceGetter<Rocket, bool>("isExploded", BindingFlags.NonPublic);
    private IEnumerator Simulate()
    {
        ProjectileData data = _current;
        Transform transform = Instantiate(data.Gun.equippedGunAsset.projectile, data.Origin, Quaternion.LookRotation(data.Direction) * Quaternion.Euler(90f, 0.0f, 0.0f)).transform;
        SceneManager.MoveGameObjectToScene(transform.gameObject, _simScene);

        transform.name = "Projectile_SimClone";
        Destroy(transform.gameObject, data.Gun.equippedGunAsset.projectileLifespan);
        if (transform.TryGetComponent(out Rigidbody body))
        {
            body.AddForce(data.Direction * data.Gun.equippedGunAsset.ballisticForce * data.MagazineForceMultiplier);
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        DetectComponent c = transform.gameObject.AddComponent<DetectComponent>();

        if (!data.Obj.TryGetComponent(out Rocket rocket))
            yield break;

        c.IgnoreTransform = rocket.ignoreTransform;
        c.OriginalRocketData = rocket;

        int i = 0;
        float lastSent = 0f;
        int iter = Mathf.CeilToInt(MAX_TIME / Time.fixedDeltaTime);
        int skip = Mathf.CeilToInt(1f / (Time.fixedDeltaTime * 1.5f));
        float seconds;
        for (; !c.IsExploded && i < iter; ++i)
        {
#if DEBUG
            seconds = i * Time.fixedDeltaTime;
            if (seconds - lastSent > 0.25f)
            {
                if (Gamemode.Config.EffectActionSuppliesAmmo.ValidReference(out EffectAsset asset))
                    F.TriggerEffectReliable(asset, Level.size * 2, transform.gameObject.transform.position);
                lastSent = seconds;
            }
#endif
            _physxScene.Simulate(Time.fixedDeltaTime);

            if (i % skip == 0)
            {
                yield return null;
                if (getIsExploded(rocket))
                    yield break;
            }
        }
        seconds = i * Time.fixedDeltaTime;

        Vector3 pos = transform.gameObject.transform.position;
        float landTime = data.LaunchTime + seconds;
        if (data.Obj != null && data.Obj.TryGetComponent(out ProjectileComponent comp))
        {
            comp.PredictedLandingPosition = pos;
            comp.PredictedImpactTime = landTime;
        }
        Destroy(transform.gameObject);
        _current = default;
        data.Callback?.Invoke(!data.Gun.player.isActiveAndEnabled ? null : data.Gun.player, pos, landTime, data.GunAsset, data.AmmunitionType);
    }
    [UsedImplicitly]
    [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
    private void FixedUpdate()
    {
        _mainPhysxScene.Simulate(Time.fixedDeltaTime);
    }

    private const float MAX_TIME = 30f;
    internal void GetLandingPoint(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, ProjectileLandingPointCalculated callback)
    {
        ItemMagazineAsset? ammo = gun.player.TryGetPlayerData(out UCPlayerData data) ? data.LastProjectedAmmoType : null;
        if (_current.Gun is null)
        {
            _current = new ProjectileData(obj, origin, direction, gun, ammo, Time.realtimeSinceStartup, callback);
            StartCoroutine(Simulate());
        }
        else
            _queue.Enqueue(new ProjectileData(obj, origin, direction, gun, ammo, Time.realtimeSinceStartup, callback));
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
    private readonly struct ProjectileData
    {
        public readonly GameObject Obj;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly UseableGun Gun;
        public readonly ProjectileLandingPointCalculated Callback;
        public readonly ItemMagazineAsset? AmmunitionType;
        public readonly ItemGunAsset GunAsset;
        public readonly float MagazineForceMultiplier;
        public readonly float LaunchTime;
        public ProjectileData(GameObject obj, Vector3 origin, Vector3 direction, UseableGun gun, ItemMagazineAsset? ammunitionType, float launchTime, ProjectileLandingPointCalculated callback)
        {
            this.Obj = obj;
            this.Origin = origin;
            this.Direction = direction;
            this.Gun = gun;
            this.Callback = callback;
            this.LaunchTime = launchTime;
            this.GunAsset = gun.equippedGunAsset;
            this.AmmunitionType = ammunitionType;
            this.MagazineForceMultiplier = ammunitionType != null ? ammunitionType.projectileLaunchForceMultiplier : 1f;
        }
    }

    private class DetectComponent : MonoBehaviour
    {
        public bool IsExploded;
        public Transform IgnoreTransform;
        public Rocket OriginalRocketData;
        [UsedImplicitly]
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        private void Start()
        {
            L.LogDebug("Added component to " + gameObject.name + ".");
        }
        [UsedImplicitly]
        [SuppressMessage(Data.SuppressCategory, Data.SuppressID)]
        private void OnTriggerEnter(Collider other)
        {
            L.LogDebug("hit " + other.name);

            if (this.IsExploded || other.isTrigger || this.IgnoreTransform != null && (other.transform == this.IgnoreTransform || other.transform.IsChildOf(this.IgnoreTransform)))
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
}

public delegate void ProjectileLandingPointCalculated(Player? owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType);