using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;

internal class HeatSeekingMissileComponent : MonoBehaviour
{
    //private Player _firer;
    private GameObject _projectile;

    private Rigidbody _rigidbody;
    private List<BoxCollider> _colliders;

    private float _guiderDistance;
    private float _aquisitionRange;
    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _armingDistance;
    private float _guidanceDelay;
    private Transform _aim;

    private DateTime _start;

    private InteractableVehicle? _vehicleLockedOn;
    private Transform? _countermeasureLockedOn;
    private Vector3 _alternativePointLockedOn;

    private SqlItem<VehicleData>? _vehicleLockedOnData;

    public static List<Transform> ActiveCountermeasures = new List<Transform>();

    private Vector3 Target
    {
        get
        {
            if ((DateTime.UtcNow - _start).TotalSeconds > _guidanceDelay)
            {
                if (_countermeasureLockedOn != null)
                    return _countermeasureLockedOn.position;
                if (_vehicleLockedOn != null && !_vehicleLockedOn.isDead)
                {
                    Transform center = _vehicleLockedOn.transform.Find("Center");
                    return center != null ? center.position : _vehicleLockedOn.transform.position;
                }
            }
            return _alternativePointLockedOn;
        }
    }
    bool LockedOnToVehicle { get => _vehicleLockedOn != null; }
    bool LockedOn { get => _vehicleLockedOn != null || _countermeasureLockedOn != null; }

    bool _armed;

    private bool _isActive;

    public void Initialize(GameObject projectile, Player firer, float projectileSpeed, float responsiveness, float aquisitionRange, float armingDistance, float guidanceDelay)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        this._projectile = projectile;
        //this._firer = firer;
        this._maxTurnDegrees = responsiveness;
        this._projectileSpeed = projectileSpeed;
        this._aquisitionRange = aquisitionRange;
        this._armingDistance = armingDistance;
        this._guidanceDelay = guidanceDelay;

        _armed = false;

        _start = DateTime.UtcNow;

        _guiderDistance = 30;
        _isActive = false;

        _vehicleLockedOn = null;
        _countermeasureLockedOn = null;

        if (projectile.TryGetComponent(out _rigidbody))
        {
            InteractableVehicle? vehicle = firer.movement.getVehicle();
            if (vehicle != null)
            {
                foreach (Passenger turret in vehicle.turrets)
                {
                    if (turret.player != null && turret.player.player == firer)
                    {
                        _aim = turret.turretAim;
                        _isActive = true;

                        projectile.transform.forward = _aim.forward;
                        _rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                        _colliders = projectile.GetComponents<BoxCollider>().ToList();
                        _colliders.ForEach(c => c.enabled = false);

                        TryAcquireTarget(_aim);//, 700);

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

    private void TryAcquireTarget(Transform lookOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float minAngle = 10;

        if (Physics.SphereCast(_projectile.transform.position, 4, _projectile.transform.up, out RaycastHit hit, _aquisitionRange, RayMasks.VEHICLE))
        {
            if (hit.transform != null && hit.transform.TryGetComponent(out InteractableVehicle v))
            {
                _vehicleLockedOn = v;
                SetVehicleData(v);
            }
        }

        if (_vehicleLockedOn == null)
        {
            foreach (InteractableVehicle v in VehicleManager.vehicles)
            {
                if ((v.asset.engine == EEngine.PLANE || v.asset.engine == EEngine.HELICOPTER) && !v.isDead)
                {
                    if ((v.transform.position - _aim.position).sqrMagnitude < Math.Pow(_aquisitionRange, 2))
                    {
                        float angleBetween = Vector3.Angle(v.transform.position - lookOrigin.position, lookOrigin.forward);
                        if (angleBetween < minAngle)
                        {
                            minAngle = angleBetween;
                            _maxTurnDegrees *= Mathf.Clamp(1 - angleBetween / 10, 0.2F, 1);
                            _vehicleLockedOn = v;
                            SetVehicleData(v);
                        }
                    }
                }
            }
        }

        _alternativePointLockedOn = GetRandomTarget(_aim);
    }
    private void SetVehicleData(InteractableVehicle vehicle)
    {
        if (vehicle.transform.TryGetComponent(out VehicleComponent vehicleComponent))
        {
            _vehicleLockedOnData = vehicleComponent.Data;
        }
        else
        {
            VehicleComponent vc = vehicle.transform.gameObject.AddComponent<VehicleComponent>();
            vc.Initialize(vehicle);
            _vehicleLockedOnData = vc.Data;
        }
    }
    private void VerifyPrimaryTarget(Transform lookOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_vehicleLockedOn != null && !_vehicleLockedOn.isDead)
        {
            Vector3 idealDirection = Target - lookOrigin.position;

            float angleBetween = Vector3.Angle(idealDirection, lookOrigin.forward);
            if (angleBetween < 30)
            {
                return;
            }

            _vehicleLockedOn = null;
            _vehicleLockedOnData = null;
        }
    }
    private void VerifyAltTargets(Transform lookOrigin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        _countermeasureLockedOn = null;

        float minAngle = 10;

        foreach (Transform countermeasure in ActiveCountermeasures)
        {
            if ((countermeasure.position - _projectile.transform.position).sqrMagnitude < Math.Pow(150, 2))
            {
                Vector3 idealDirection = countermeasure.position - lookOrigin.position;

                float angleBetween = Vector3.Angle(idealDirection, lookOrigin.forward);
                if (angleBetween < minAngle)
                {
                    _countermeasureLockedOn = countermeasure;
                    minAngle = angleBetween;
                }
            }
        }

        _alternativePointLockedOn = GetRandomTarget(_projectile.transform);
    }
    private void TrySendWarning()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_vehicleLockedOnData is null || _vehicleLockedOn is null || _vehicleLockedOn.isDead)
            return;

        for (byte seat = 0; seat < _vehicleLockedOn.passengers.Length; seat++)
        {
            if (_vehicleLockedOn.passengers[seat].player != null &&
                _vehicleLockedOnData?.Item != null &&
                _vehicleLockedOnData.Item.CrewSeats.Contains(seat))
            {
                ushort effectID = VehicleBay.Config.MissileWarningID;
                if (seat == 0)
                    effectID = VehicleBay.Config.MissileWarningDriverID;

                EffectManager.sendUIEffect(effectID, (short)effectID, _vehicleLockedOn.passengers[seat].player.transportConnection, true);
            }
        }
    }
    private Vector3 GetRandomTarget(Transform lookOrigin)
    {
        return lookOrigin.TransformPoint(new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), 300));
    }
    int _count;
    private float _lastSent;
    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (_isActive)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            _guiderDistance += Time.fixedDeltaTime * _projectileSpeed;

            if (_guiderDistance > 30 + _armingDistance && !_armed)
            {
                _colliders.ForEach(c => c.enabled = true);
                _armed = true;
            }


            if (_count == 0)
            {
                TrySendWarning();
            }
            if (_count % 5 == 0)
            {
                VerifyAltTargets(_projectile.transform);
            }

            VerifyPrimaryTarget(_projectile.transform);

            Vector3 idealDirection = Target - _projectile.transform.position;

            float turnDegrees = 0.2f;
            if (LockedOn)
            {
                turnDegrees = LockedOnToVehicle ? _maxTurnDegrees : 0.5f;
            }

            Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * turnDegrees, Mathf.Deg2Rad * turnDegrees);

            _projectile.transform.forward = targetDirection;
            _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;
            if (Time.time - _lastSent > 0.05f)
            {
                JsonAssetReference<EffectAsset> id = Gamemode.Config.EffectHeatSeekingMissileNoSound;
                if ((_count % 10 == 0 && _armed) || !id.ValidReference(out ushort _))
                {
                    id = Gamemode.Config.EffectHeatSeekingMissileSound;
                    if (!id.ValidReference(out ushort _))
                        id = Gamemode.Config.EffectHeatSeekingMissileNoSound;
                }
                if (id.ValidReference(out EffectAsset effect))
                {
                    EffectManager.triggerEffect(new TriggerEffectParameters(effect)
                    {
                        relevantDistance = 1200f,
                        position = _projectile.transform.position,
                        direction = _projectile.transform.forward,
                        reliable = false
                    });
                }
                _lastSent = Time.time;
            }

            _count++;
        }
    }
}