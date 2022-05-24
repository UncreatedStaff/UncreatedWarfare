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

        private InteractableVehicle? vehicleLockedOn;

        private VehicleData? vehicleLockedOnData;

        bool lockedOnToVehicle { get => vehicleLockedOn != null; }

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

            guiderDistance = 30;
            isActive = false;

            vehicleLockedOn = null;

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

                            projectile.transform.forward = aim.forward;
                            rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                            colliders = projectile.GetComponents<BoxCollider>().ToList();
                            colliders.ForEach(c => c.enabled = false);

                            return;
                        }
                    }
                    L.LogDebug("LASER GUIDED MISSILE ERROR: player firing not found");
                }
                else
                    L.LogDebug("LASER GUIDED MISSILE ERROR: player was not in a vehicle");
            }
            else
                L.LogDebug("LASER GUIDED MISSILE ERROR: could not find rigidbody");
        }

        private void TryAcquireTarget(Transform lookOrigin, float range)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            vehicleLockedOn = null;

            float minAngle = 5;

            foreach (InteractableVehicle v in VehicleManager.vehicles)
            {
                if ((v.asset.engine == EEngine.CAR || v.asset.engine == EEngine.BOAT) && !v.isDead && v.anySeatsOccupied)
                {
                    if ((v.transform.position - aim.position).sqrMagnitude < Math.Pow(aquisitionRange, 2))
                    {
                        float angleBetween = Vector3.Angle(v.transform.position - lookOrigin.position, lookOrigin.forward);
                        if (angleBetween < minAngle)
                        {
                            minAngle = angleBetween;
                            vehicleLockedOn = v;
                            //SetVehicleData(v);
                        }
                    }
                }
            }
        }
        private void SetVehicleData(InteractableVehicle vehicle)
        {
            if (vehicle.transform.TryGetComponent(out VehicleComponent vehicleComponent))
            {
                vehicleLockedOnData = vehicleComponent.Data;
            }
            else
            {
                var vc = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
                vc.Initialize(vehicle);
                vehicleLockedOnData = vc.Data;
            }
        }
        int count = 0;
        private void FixedUpdate()
        {
            if (isActive)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                guiderDistance += Time.fixedDeltaTime * projectileSpeed;

                if (guiderDistance > 30 + armingDistance && !armed)
                {
                    colliders.ForEach(c => c.enabled = true);
                    armed = true;
                }

                ushort id = 26036;
                if (count % 10 == 0 && armed)
                    id = 26037;

                if ((DateTime.Now - start).TotalSeconds > guidanceDelay)
                {
                    Vector3 target = aim.TransformPoint(new Vector3(0, 0, guiderDistance));

                    TryAcquireTarget(aim, aquisitionRange);

                    if (lockedOnToVehicle && vehicleLockedOn != null)
                    {
                        var center = vehicleLockedOn.transform.Find("Center");
                        if (center != null)
                            target = center.position;
                        else
                            target = vehicleLockedOn.transform.position;
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
