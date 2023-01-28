using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;
internal class HeatSeekingController : MonoBehaviour // attach to a turrent's 'Aim' gameobject to allow it to control projectiles
{
    private const float AQUISITION_ANGLE = 50f;
    private const float AQUISITION_FREQUENCY = 0.5f;


    private float _horizontalRange = 300;
    private float? _verticalRange = 800;

    private InteractableVehicle _vehicle;
    private EffectAsset _effect;
    private UCPlayer? _lastKnownGunner;

    private float _aquisitionTime = 1.5f;
    private float _timeOutTime = 10;
    private float _timeOfAquisition;
    private float _timeOfLastScan;

    public Transform? LockOnTarget { get; private set; }
    public Vector3 AlternativeTargetPosition { get; private set; }
    public ELockOnMode Status { get; private set; }

    public static List<Transform> ActiveCountermeasures = new List<Transform>();


    private void FixedUpdate()
    {
        if (Time.time - _timeOfLastScan >= AQUISITION_FREQUENCY)
        {
            ScanForTargets();
            _timeOfLastScan = Time.time;
        }
    }

    public void Initialize(float horizontalRange, float verticalRange, JsonAssetReference<EffectAsset> lockOnEffect)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = horizontalRange;
        _verticalRange = verticalRange;

        _effect = lockOnEffect;

        if (!lockOnEffect.Exists)
            L.LogWarning("HEATSEAKER ERROR: Lock on sound effect not found: " + lockOnEffect.Guid);
    }
    public void Initialize(float range, JsonAssetReference<EffectAsset> lockOnEffect)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = range;

        _effect = lockOnEffect;

        if (!lockOnEffect.Exists)
            L.LogWarning("HEATSEAKER ERROR: Lock on sound effect not found: " + lockOnEffect.Guid);
    }

    public UCPlayer? GetGunner(InteractableVehicle vehicle)
    {
        foreach (Passenger turret in vehicle.turrets)
        {
            if (turret.turretAim == transform && turret.player != null)
            {
                return UCPlayer.FromSteamPlayer(turret.player);
            }
        }
        return null;
    }

    private void ScanForTargets()
    {
        AlternativeTargetPosition = GetRandomTarget();

        var gunner = GetGunner(_vehicle);

        if (gunner is null)
        {
            if (_lastKnownGunner is not null)
                CancelLockOnSound(_lastKnownGunner);
            _lastKnownGunner = null;
            return;
        }

        _lastKnownGunner = gunner;

        float bestTarget = AQUISITION_ANGLE;

        Transform? newTarget = null;

        foreach (InteractableVehicle v in VehicleManager.vehicles)
        {
            if ((v.asset.engine == EEngine.PLANE || v.asset.engine == EEngine.HELICOPTER) && !v.isDead/* && v.isEngineOn*/)
            {
                if (IsInRange(v.transform.position))
                {
                    Vector3 relativePos = transform.InverseTransformPoint(v.transform.position);
                    relativePos = new Vector3(Mathf.Abs(relativePos.x), Mathf.Abs(relativePos.y), 0);

                    float lockOnDistance = new Vector2(relativePos.x, relativePos.y).sqrMagnitude;
                    float angleBetween = Vector3.Angle(v.transform.position - transform.position, transform.forward);
                    if (angleBetween < 90 && new Vector2(relativePos.x, relativePos.y).sqrMagnitude < Mathf.Pow(bestTarget, 2))
                    {
                        bool raySuccess = Physics.Raycast(new Ray(transform.position, v.transform.position - transform.position), out RaycastHit hit, Vector3.Distance(v.transform.position, transform.position), RayMasks.GROUND | RayMasks.LARGE | RayMasks.MEDIUM);
                        if (!raySuccess)
                        {
                            bestTarget = lockOnDistance;
                            newTarget = v.transform;
                        }
                    }
                }
            }
        }

        bestTarget = AQUISITION_ANGLE;

        foreach (Transform countermeasure in ActiveCountermeasures)
        {
            Vector3 relativePos = transform.InverseTransformPoint(countermeasure.position);

            float lockOnDistance = new Vector2(relativePos.x, relativePos.y).sqrMagnitude;
            float angleBetween = Vector3.Angle(countermeasure.position - transform.position, transform.forward);
            if (angleBetween < 90 && new Vector2(relativePos.x, relativePos.y).sqrMagnitude < Mathf.Pow(bestTarget, 2))
            {
                bool raySuccess = Physics.Raycast(new Ray(transform.position, countermeasure.position - transform.position), out RaycastHit hit, Vector3.Distance(countermeasure.position, transform.position), RayMasks.GROUND | RayMasks.LARGE | RayMasks.MEDIUM);
                if (!raySuccess)
                {
                    bestTarget = lockOnDistance;
                    newTarget = countermeasure;
                }
            }
        }

        LockOn(newTarget, gunner);
    }

    private void LockOn(Transform? newTarget, UCPlayer gunner)
    {
        if (newTarget is null) // no target found
        {
            if (Status != ELockOnMode.IDLE)
            {
                Status = ELockOnMode.IDLE;
                CancelLockOnSound(gunner);
                LockOnTarget = null;
            }
            return;
        }

        if (LockOnTarget is null || LockOnTarget != newTarget) // new target has been identified
        {
            LockOnTarget = newTarget;
            _timeOfAquisition = Time.time;
            Status = ELockOnMode.ACQUIRING;

            PlayLockOnSound(gunner);
        }
        else // target is the same as the previous call
        {
            var timeSinceAquisition = Time.time - _timeOfAquisition;

            if (timeSinceAquisition >= _aquisitionTime)
            {
                Status = ELockOnMode.LOCKED_ON;

                // play warning sound?

                if (Time.time - _timeOfAquisition >= _timeOutTime)
                {
                    Status = ELockOnMode.IDLE;
                    LockOnTarget = null;
                }
            }
        }
    }
    private void PlayLockOnSound(UCPlayer gunner)
    {
        if (_effect is null)
            return;

        EffectManager.sendUIEffect(_effect.id, (short)_effect.id, gunner.Connection, true);
    }
    private void CancelLockOnSound(UCPlayer gunner)
    {
        if (_effect is null)
            return;

        EffectManager.ClearEffectByGuid(_effect.GUID, gunner.Connection);
    }

    public bool IsInRange(Vector3 target)
    {
        if (_verticalRange is null)
            return (target - transform.position).sqrMagnitude < Math.Pow(_horizontalRange, 2);

        Vector3 horizontal1 = new Vector3(target.x, 0, target.z);
        Vector3 horizontal2 = new Vector3(transform.position.x, 0, transform.position.z);

        return (horizontal1 - horizontal2).sqrMagnitude < Math.Pow(_horizontalRange, 2) && Mathf.Abs(target.y - transform.position.y) < _verticalRange;
    }

    private Vector3 GetRandomTarget() => transform.TransformPoint(new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 300));

    public enum ELockOnMode
    {
        IDLE,
        ACQUIRING,
        LOCKED_ON
    }
}
