using System;
using System.Reflection;
using Uncreated.Warfare.Events;

namespace Uncreated.Warfare.Components;

internal class ProjectileComponent : MonoBehaviour
{
    internal ulong Owner;
    internal Guid GunId;
    internal int UnityInstanceID;
    internal Rocket RocketComponent;
    private bool _isExploded;
    public float LaunchTime;
    private static readonly MethodInfo? ExplodeMethod = typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
    [UsedImplicitly]
    private void Awake()
    {
        UnityInstanceID = gameObject.GetInstanceID();
        RocketComponent = GetComponent<Rocket>();
        _isExploded = false;
    }
    [UsedImplicitly]
    private void Start()
    {
        LaunchTime = Time.realtimeSinceStartup;
        _lastpos = transform.position;
    }
    internal void OnCollided(Collider other)
    {
        if (_isExploded || other.isTrigger ||
            (RocketComponent.ignoreTransform != null && (other.transform == RocketComponent.ignoreTransform || other.transform.IsChildOf(RocketComponent.ignoreTransform))))
            return;

        _isExploded = true;

        EventDispatcher.InvokeOnProjectileExploded(this, other);

        L.LogDebug("Collided with " + other.gameObject.name + " at " + gameObject.transform.position.ToString("F1") + " after " + (Time.realtimeSinceStartup - LaunchTime) + " seconds. (Predicted: " + (PredictedImpactTime - LaunchTime) + " seconds)");
    }

    internal float PredictedImpactTime;
    internal Vector3 PredictedLandingPosition;
    private Vector3 _lastpos;

    [UsedImplicitly]
    void FixedUpdate()
    {
        if (!_isExploded && Physics.Linecast(_lastpos, transform.position, out RaycastHit hit, /*RayMasks.VEHICLE | */RayMasks.GROUND | RayMasks.GROUND2 | RayMasks.LARGE))
        {
            Collider other = hit.collider;
            if (other.isTrigger || (RocketComponent.ignoreTransform != null &&
                                    (other.transform == RocketComponent.ignoreTransform ||
                                     other.transform.IsChildOf(RocketComponent.ignoreTransform))))
                goto rtn;
            if (TryGetComponent(out Rocket rocket) && ExplodeMethod != null)
            {
                ExplodeMethod.Invoke(rocket, [ other ]);
#if DEBUG
                string gun = Assets.find(GunId)?.FriendlyName ?? GunId.ToString("N");
                L.LogWarning("Ghost rocket prevented: " + gun);
                //foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                //    player.SteamPlayer.SendString("Ghost Rocket Prevented for " + gun + "!", Color.green);
#endif
            }
        }
        rtn:
        _lastpos = transform.position;
    }

#if false && DEBUG
    private float _lastSpawn;
    [UsedImplicitly]
    void Update()
    {
        float time = Time.time;

        if (time - _lastSpawn > 0.25f)
        {
            if (Gamemode.Config.EffectAmmo.TryGetAsset(out EffectAsset? effect))
                F.TriggerEffectReliable(effect, Level.size * 2, this.transform.position);
            _lastSpawn = time;
        }
    }
#endif
}