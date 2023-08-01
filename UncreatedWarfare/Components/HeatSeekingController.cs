using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Framework;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;
internal class HeatSeekingController : MonoBehaviour // attach to a turrent's 'Aim' gameobject to allow it to control projectiles
{
    private const float AQUISITION_ANGLE = 65f;
    private const float AQUISITION_FREQUENCY = 0.25f;


    private float _horizontalRange = 700;
    private float? _verticalRange = 1500;

    private InteractableVehicle _vehicle;
    private EffectAsset _effect;
    private UCPlayer? _lastKnownGunner;
    public List<Transform> Hardpoints { get; set; }
    public List<HeatSeekingMissileComponent> MissilesInFlight { get; set; }


    private float _aquisitionTime;
    private float _timeOutTime;

    private float _timeOfAquisition;
    private float _timeOfLastScan;
    public int _currentHardpoint;

    public Transform? LockOnTarget { get; private set; }
    public ELockOnMode Status { get; private set; }
    public Transform? CycleHardpoint()
    {
        if (Hardpoints.Count == 0)
            return null;

        _currentHardpoint++;
        if (_currentHardpoint >= Hardpoints.Count)
            _currentHardpoint = 0;

        return Hardpoints[_currentHardpoint];
    }


    private void FixedUpdate()
    {
        if (Time.time - _timeOfLastScan >= AQUISITION_FREQUENCY)
        {
            ScanForTargets();
            _timeOfLastScan = Time.time;
        }
    }

    public void Initialize(float horizontalRange, float verticalRange, JsonAssetReference<EffectAsset> lockOnEffect, float aquisitionTime, float timeOutTime)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = horizontalRange;
        _verticalRange = verticalRange;
        _aquisitionTime = aquisitionTime;
        _timeOutTime = timeOutTime;

        Hardpoints = new List<Transform>();
        MissilesInFlight = new List<HeatSeekingMissileComponent>();
        _currentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + i);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

        _effect = lockOnEffect;

        if (!lockOnEffect.Exists)
            L.LogWarning("HEATSEAKER ERROR: Lock on sound effect not found: " + lockOnEffect.Guid);
    }
    public void Initialize(float range, JsonAssetReference<EffectAsset> lockOnEffect, float aquisitionTime, float timeOutTime)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = range;
        _aquisitionTime = aquisitionTime;
        _timeOutTime = timeOutTime;

        Hardpoints = new List<Transform>();
        MissilesInFlight = new List<HeatSeekingMissileComponent>();
        _currentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + i);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

        Hardpoints = new List<Transform>();
        _currentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + 0);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

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
        Transform? newTarget = null;

        UCPlayer? gunner = GetGunner(_vehicle);

        if (gunner != null)
            _lastKnownGunner = gunner;
        else if (_lastKnownGunner != null) // gunner exited the vehicle
        {
            CancelLockOnSound(_lastKnownGunner);
            _lastKnownGunner = null;
        }

        float bestTarget = AQUISITION_ANGLE;

        foreach (InteractableVehicle v in VehicleManager.vehicles)
        {
            if ((v.asset.engine == EEngine.PLANE || v.asset.engine == EEngine.HELICOPTER) && !v.isDead && v.lockedGroup != _vehicle.lockedGroup && v.isEngineOn && !(v.anySeatsOccupied && TeamManager.IsInAnyMain(v.transform.position)))
            {
                if (IsInRange(v.transform.position))
                {
                    Vector3 relativePos = transform.InverseTransformPoint(v.transform.position);
                    relativePos = new Vector3(Mathf.Abs(relativePos.x), Mathf.Abs(relativePos.y), 0);

                    float lockOnDistance = new Vector2(relativePos.x, relativePos.y).sqrMagnitude;
                    float angleBetween = Vector3.Angle(v.transform.position - transform.position, transform.forward);
                    if (angleBetween < 90 && new Vector2(relativePos.x, relativePos.y).sqrMagnitude < Mathf.Pow(bestTarget, 2))
                    {
                        bool raySuccess = Physics.Linecast(transform.position, v.transform.position, out _, RayMasks.GROUND | RayMasks.LARGE | RayMasks.MEDIUM);
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

        bool lockedOntoCountermeassure = LockOnTarget != null &&
            LockOnTarget.TryGetComponent(out Countermeasure countermeasure) &&
            countermeasure.Burning;

        if (!lockedOntoCountermeassure)
        {
            foreach (Countermeasure c in Countermeasure.ActiveCountermeasures)
            {
                if (!c.Burning)
                    continue;

                if (IsInRange(c.transform.position))
                {
                    Vector3 relativePos = transform.InverseTransformPoint(c.transform.position);
                    relativePos = new Vector3(Mathf.Abs(relativePos.x), Mathf.Abs(relativePos.y), 0);

                    float lockOnDistance = new Vector2(relativePos.x, relativePos.y).sqrMagnitude;
                    float angleBetween = Vector3.Angle(c.transform.position - transform.position, transform.forward);
                    if (angleBetween < 90 && new Vector2(relativePos.x, relativePos.y).sqrMagnitude < Mathf.Pow(bestTarget, 2))
                    {
                        bool raySuccess = Physics.Linecast(transform.position, c.transform.position, out _, RayMasks.GROUND | RayMasks.LARGE | RayMasks.MEDIUM);
                        if (!raySuccess)
                        {
                            bestTarget = lockOnDistance;
                            newTarget = c.transform;
                        }
                    }
                }
            }
        }

        LockOn(newTarget, gunner);
    }

    private void LockOn(Transform? newTarget, UCPlayer? gunner)
    {
        bool noAmmo = gunner != null && gunner.Player.equipment.state[10] == 0;

        UseableGun? gun = gunner?.Player.equipment.useable as UseableGun;
        bool reloading = gun != null && Data.GetUseableGunReloading(gun);
        bool noActiveMissiles = MissilesInFlight.Count == 0;

        if (newTarget is null || (noActiveMissiles && (noAmmo || reloading))) // no target found
        {
            //(reloading && noActiveMissiles) || (noAmmo && noActiveMissiles)
            if (Status != ELockOnMode.IDLE)
            {

                Status = ELockOnMode.IDLE;

                if (gunner != null)
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

            L.LogDebug($"     AA: acquriing...");

            if (gunner != null)
                PlayLockOnSound(gunner);
        }
        else // target is the same as the previous call
        {
            var timeSinceAquisition = Time.time - _timeOfAquisition;

            if (timeSinceAquisition >= _aquisitionTime)
            {
                if (Status != ELockOnMode.LOCKED_ON)
                    L.LogDebug($"     AA: LOCKED");

                Status = ELockOnMode.LOCKED_ON;

                if (LockOnTarget.TryGetComponent(out VehicleComponent v) && gunner != null)
                    v.ReceiveMissileWarning();

                if (Time.time - _timeOfAquisition >= _timeOutTime)
                {
                    Status = ELockOnMode.IDLE;
                    LockOnTarget = null;
                }
            }
        }
    }
    private static short _lockOnEffectKey = UnturnedUIKeyPool.Claim();
    private void PlayLockOnSound(UCPlayer gunner)
    {
        if (_effect == null || gunner is not { IsOnline: true })
            return;

        L.LogDebug($"            tone: playing...");

        EffectManager.sendUIEffect(_effect.id, _lockOnEffectKey, gunner.Connection, true);
    }
    private void CancelLockOnSound(UCPlayer gunner)
    {
        if (_effect == null || gunner is not { IsOnline: true })
            return;

        EffectManager.ClearEffectByGuid(_effect.GUID, gunner.Connection);
        L.LogDebug($"            tone: cancelled");
    }

    public bool IsInRange(Vector3 target)
    {
        if (_verticalRange is null)
            return (target - transform.position).sqrMagnitude < Math.Pow(_horizontalRange, 2);

        Vector3 horizontal1 = new Vector3(target.x, 0, target.z);
        Vector3 horizontal2 = new Vector3(transform.position.x, 0, transform.position.z);

        return (horizontal1 - horizontal2).sqrMagnitude < Math.Pow(_horizontalRange, 2) && Mathf.Abs(target.y - transform.position.y) < _verticalRange;
    }

    public enum ELockOnMode
    {
        IDLE,
        ACQUIRING,
        LOCKED_ON
    }
}
