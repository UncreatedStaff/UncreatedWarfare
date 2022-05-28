using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components
{
    internal class LaserGuidedMissileComponent : MonoBehaviour
    {
        private Player firer;
        private GameObject projectile;

        private Rigidbody rigidbody;
        private List<BoxCollider> colliders;

        private float guiderDistance;
        private float aquisitionRange;
        private float projectileSpeed;
        private float maxTurnDegrees;
        private float armingDistance;
        private float guidanceDelay;
        private Transform aim;

        private DateTime start;

        private SpottedComponent? laserTarget;

        public bool LockedOn { get => laserTarget != null; }

        bool armed;

        private bool isActive;

        public void Initialize(GameObject projectile, Player firer, float projectileSpeed, float responsiveness, float aquisitionRange, float armingDistance, float guidanceDelay)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            this.projectile = projectile;
            this.firer = firer;
            this.maxTurnDegrees = responsiveness;
            this.projectileSpeed = projectileSpeed;
            this.aquisitionRange = aquisitionRange;
            this.armingDistance = armingDistance;
            this.guidanceDelay = guidanceDelay;
            this.guiderDistance = 30;

            armed = false;

            start = DateTime.Now;

            isActive = false;

            laserTarget = null;

            if (projectile.TryGetComponent(out rigidbody))
            {
                InteractableVehicle? vehicle = firer.movement.getVehicle();
                if (vehicle != null)
                {
                    foreach (Passenger turret in vehicle.turrets)
                    {
                        if (turret.player != null && turret.player.player == firer)
                        {
                            aim = turret.turretAim;
                            isActive = true;

                            projectile.transform.forward = Quaternion.AngleAxis(-30, aim.up - aim.right) * aim.forward;

                            rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                            colliders = projectile.GetComponents<BoxCollider>().ToList();
                            colliders.ForEach(c => c.enabled = false);

                            return;
                        }
                    }
                    L.LogDebug("LASER GUIDED MISSILE ERROR: player firing not found");
                }
                else
                {
                    aim = firer.look.aim.transform;
                    isActive = true;
                    projectile.transform.forward = aim.forward;
                    rigidbody.velocity = projectile.transform.forward * projectileSpeed;
                    colliders = projectile.GetComponents<BoxCollider>().ToList();
                    colliders.ForEach(c => c.enabled = false);
                    L.LogDebug("LASER GUIDED MISSILE ERROR: player was not in a vehicle");
                }
            }
            else
                L.LogDebug("LASER GUIDED MISSILE ERROR: could not find rigidbody");
        }

        private bool TryAcquireTarget(Transform lookOrigin, float range)
        {
            if (laserTarget != null)
                return true;

#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float minAngle = 3;

            foreach (var spotted in SpottedComponent.ActiveMarkers)
            {
                if (spotted.CurrentSpotter!.GetTeam() == firer.quests.groupID.m_SteamID && !(spotted.Type == SpottedComponent.ESpotted.AIRCRAFT || spotted.Type == SpottedComponent.ESpotted.INFANTRY))
                {
                    if ((spotted.transform.position - aim.position).sqrMagnitude < Math.Pow(aquisitionRange, 2))
                    {
                        float angleBetween = Vector3.Angle(spotted.transform.position - lookOrigin.position, lookOrigin.forward);
                        if (angleBetween < minAngle)
                        {
                            minAngle = angleBetween;
                            laserTarget = spotted;
                        }
                    }
                }
            }

            return laserTarget != null;
        }
        
        private void FixedUpdate()
        {
            if (isActive)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                int count = 0;

                guiderDistance += Time.fixedDeltaTime * projectileSpeed;

                if (guiderDistance > 30 + armingDistance && !armed)
                {
                    colliders.ForEach(c => c.enabled = true);
                    armed = true;
                }

                ushort id = 26044;
                if (count % 10 == 0 && armed)
                    id = 26045;

                if ((DateTime.Now - start).TotalSeconds > guidanceDelay)
                {
                    Vector3 target = aim.TransformPoint(new Vector3(0, 0, guiderDistance));

                    if (TryAcquireTarget(aim, aquisitionRange))
                    {
                        target = laserTarget!.transform.position;
                    }

                    Vector3 idealDirection = target - projectile.transform.position;

                    Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * maxTurnDegrees, Mathf.Deg2Rad * maxTurnDegrees);

                    projectile.transform.forward = targetDirection;
                    rigidbody.velocity = projectile.transform.forward * projectileSpeed;
                }

                EffectManager.sendEffect(id, 1200, projectile.transform.position, projectile.transform.forward);

                count++;
                if (count >= 20)
                    count = 0;
            }
        }
    }
}
