﻿using SDG.Unturned;
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
        internal SDG.Unturned.Rocket RocketComponent;
        private bool _isExploded;
        public bool IgnoreArmor { get; private set; }
        private void Awake()
        {
            UnityInstanceID = gameObject.GetInstanceID();
            RocketComponent = GetComponent<SDG.Unturned.Rocket>();
            _isExploded = false;

            IgnoreArmor = transform.Find("IgnoreArmor") != null;
        }
        internal void OnCollided(Collider other)
        {
            if (_isExploded || other.isTrigger ||
                (RocketComponent.ignoreTransform != null && (other.transform == RocketComponent.ignoreTransform || other.transform.IsChildOf(RocketComponent.ignoreTransform))))
                return;

            _isExploded = true;

            EventDispatcher.InvokeOnProjectileExploded(this, other);

        }
    }
}