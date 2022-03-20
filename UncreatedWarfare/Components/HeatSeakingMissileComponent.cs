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
    internal class HeatSeakingMissileComponent : MonoBehaviour
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
        private Transform? countermeasureLockedOn;
        private Vector3 alternativePointLockedOn;

        private VehicleData? vehicleLockedOnData;

        public static List<Transform> ActiveCountermeasures = new List<Transform>();

        private Vector3 target
        {
            get
            {
                if ((DateTime.Now - start).TotalSeconds > guidanceDelay)
                {
                    if (countermeasureLockedOn != null)
                    {
                        return countermeasureLockedOn.position;
                    }
                    else if (!(vehicleLockedOn == null || vehicleLockedOn.isDead))
                    {
                        var center = vehicleLockedOn.transform.Find("Center");
                        if (center != null)
                            return center.position;
                        else
                            return vehicleLockedOn.transform.position;
                    }
                }
                return alternativePointLockedOn;
            }
        }
        bool lockedOnToVehicle { get => vehicleLockedOn != null; }
        bool lockedOn { get => vehicleLockedOn != null || countermeasureLockedOn != null; }

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

            armed = false;

            start = DateTime.Now;

            guiderDistance = 30;
            isActive = false;

            vehicleLockedOn = null;
            countermeasureLockedOn = null;

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

                            TryAcquireTarget(aim, 700);

                            return;
                        }
                    }
                    L.LogDebug("HEAT SEAKING MISSILE ERROR: player firing not found");
                }
                else
                    L.LogDebug("HEAT SEAKING MISSILE ERROR: player was not in a vehicle");
            }
            else
                L.LogDebug("HEAT SEAKING MISSILE ERROR: could not find rigidbody");
        }

        private void TryAcquireTarget(Transform lookOrigin, float range)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float minAngle = 10;

            foreach (InteractableVehicle v in VehicleManager.vehicles)
            {
                if ((v.asset.engine == EEngine.PLANE || v.asset.engine == EEngine.HELICOPTER) && !v.isDead)
                {
                    if ((v.transform.position - aim.position).sqrMagnitude < Math.Pow(aquisitionRange, 2))
                    {
                        float angleBetween = Vector3.Angle(v.transform.position - lookOrigin.position, lookOrigin.forward);
                        if (angleBetween < minAngle)
                        {
                            minAngle = angleBetween;
                            maxTurnDegrees *= Mathf.Clamp(1 - angleBetween / 10, 0.2F, 1);
                            vehicleLockedOn = v;
                            SetVehicleData(v);
                        }
                    }
                }
            }

            alternativePointLockedOn = GetRandomTarget(aim);
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
        private void VerifyPrimaryTarget(Transform lookOrigin)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (vehicleLockedOn != null && !vehicleLockedOn.isDead)
            {
                Vector3 idealDirection = target - lookOrigin.position;

                float angleBetween = Vector3.Angle(idealDirection, lookOrigin.forward);
                if (angleBetween < 30)
                {
                    return;
                }

                vehicleLockedOn = null;
                vehicleLockedOnData = null;
            }
        }
        private void VerifyAltTargets(Transform lookOrigin)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            countermeasureLockedOn = null;

            float minAngle = 30;

            foreach (Transform countermeasure in ActiveCountermeasures)
            {
                if (countermeasure.gameObject is null || !countermeasure.gameObject.activeInHierarchy)
                    continue;

                if ((countermeasure.position - projectile.transform.position).sqrMagnitude < Math.Pow(250, 2))
                {
                    Vector3 idealDirection = countermeasure.position - lookOrigin.position;

                    float angleBetween = Vector3.Angle(idealDirection, lookOrigin.forward);
                    if (angleBetween < minAngle)
                    {
                        countermeasureLockedOn = countermeasure;
                        minAngle = angleBetween;
                    }
                }
            }

            alternativePointLockedOn = GetRandomTarget(projectile.transform);
        }
        private void TrySendWarning()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (vehicleLockedOnData is null || vehicleLockedOn is null || vehicleLockedOn.isDead)
                return;

            for (byte seat = 0; seat < vehicleLockedOn.passengers.Length; seat++)
            {
                if (vehicleLockedOn.passengers[seat].player != null && vehicleLockedOnData.CrewSeats.Contains(seat))
                {
                    ushort effectID = VehicleBay.Config.MissileWarningID;
                    if (seat == 0)
                        effectID = VehicleBay.Config.MissileWarningDriverID;

                    EffectManager.sendUIEffect(effectID, (short)effectID, vehicleLockedOn.passengers[seat].player.transportConnection, true);
                }
            }
        }
        private Vector3 GetRandomTarget(Transform lookOrigin)
        {
            return lookOrigin.TransformPoint(new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 300));
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

                if (count == 0)
                {
                    TrySendWarning();
                }
                if (count % 5 == 0)
                {
                    VerifyAltTargets(projectile.transform);
                }

                VerifyPrimaryTarget(projectile.transform);

                Vector3 idealDirection = target - projectile.transform.position;

                float turnDegrees = 0.2f;
                if (lockedOn)
                {
                    if (lockedOnToVehicle)
                        turnDegrees = maxTurnDegrees;
                    else
                        turnDegrees = 0.5f;
                }

                Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * turnDegrees, Mathf.Deg2Rad * turnDegrees);

                projectile.transform.forward = targetDirection;
                rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                EffectManager.sendEffect(id, 1200, projectile.transform.position, projectile.transform.forward);

                count++;
                if (count >= 20)
                    count = 0;
            }
        }
    }
}
