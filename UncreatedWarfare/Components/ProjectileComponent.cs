using SDG.Unturned;
using System;
using JetBrains.Annotations;
using Uncreated.Warfare.Events;
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
    /*
#if DEBUG
    private float _lastSpawn;
    [UsedImplicitly]
    void Update()
    {
        float time = Time.time;

        if (time - _lastSpawn > 0.25f)
        {
            if (SquadManager.Config.EmptyMarker.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, Level.size * 2, this.transform.position);
            _lastSpawn = time;
        }
    }
#endif*/
}