using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Events;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    internal class ProjectileComponent : MonoBehaviour
    {
        internal ulong Owner;
        internal Guid GunID;
        internal int UnityInstanceID;
        internal Rocket RocketComponent;
        private bool _isExploded;
        public float LaunchTime;
        public bool IgnoreArmor { get; private set; }
        private void Awake()
        {
            UnityInstanceID = gameObject.GetInstanceID();
            RocketComponent = GetComponent<Rocket>();
            _isExploded = false;

            IgnoreArmor = transform.Find("IgnoreArmor") != null;
        }
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
#if DEBUG
        private float lastSpawn = 0f;
        void Update()
        {
            float time = Time.time;

            if (time - lastSpawn > 0.25f)
            {
                EffectManager.sendEffect(36100, Level.size * 2, this.transform.position);
                lastSpawn = time;
            }
        }
#endif
    }
}
