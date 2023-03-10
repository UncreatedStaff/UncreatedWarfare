using SDG.Unturned;
using System;
using System.Reflection;
using JetBrains.Annotations;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Squads;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class ProjectileComponent : MonoBehaviour
{
    internal ulong Owner;
    internal Guid GunID;
    internal int UnityInstanceID;
    internal Rocket RocketComponent;
    private bool _isExploded;
    public float LaunchTime;
    private static MethodInfo? _explodeMethod = typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
    public bool IgnoreArmor { get; private set; }
    [UsedImplicitly]
    private void Awake()
    {
        UnityInstanceID = gameObject.GetInstanceID();
        RocketComponent = GetComponent<Rocket>();
        _isExploded = false;

        IgnoreArmor = transform.Find("IgnoreArmor") != null;
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
    void FixedUpdate()
    {
        if (!_isExploded && Physics.Linecast(_lastpos, transform.position, out RaycastHit hit, /*RayMasks.VEHICLE | */RayMasks.GROUND | RayMasks.GROUND2 | RayMasks.LARGE))
        {
            Collider other = hit.collider;
            if (other.isTrigger || (RocketComponent.ignoreTransform != null &&
                                    (other.transform == RocketComponent.ignoreTransform ||
                                     other.transform.IsChildOf(RocketComponent.ignoreTransform))))
                goto rtn;
            if (TryGetComponent(out Rocket rocket) && _explodeMethod != null)
            {
                _explodeMethod.Invoke(rocket, new object[] { hit.collider });
#if DEBUG
                string gun = Assets.find(GunID)?.FriendlyName ?? GunID.ToString("N");
                L.LogWarning("Ghost rocket prevented: " + gun);
                //foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                //    player.SteamPlayer.SendString("Ghost Rocket Prevented for " + gun + "!", Color.green);
#endif
            }
        }
        rtn:
        _lastpos = transform.position;
    }
    
#if DEBUG
    private float _lastSpawn;
    [UsedImplicitly]
    void Update()
    {
        float time = Time.time;

        if (time - _lastSpawn > 0.25f)
        {
            if (Gamemode.Config.EffectAmmo.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, Level.size * 2, this.transform.position);
            _lastSpawn = time;
        }
    }
#endif
}