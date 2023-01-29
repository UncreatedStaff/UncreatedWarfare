using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Components.HeatSeekingController;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;

internal class HeatSeekingMissileComponent : MonoBehaviour
{
    //private Player _firer;
    private GameObject _projectile;
    private HeatSeekingController _controller;
    private Transform? _lastKnownTarget;

    private Rigidbody _rigidbody;
    private List<Collider> _colliders;

    private float _guiderDistance = 30;
    private float _aquisitionRange;
    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _armingDistance;
    private float _guidanceDelay;
    private bool _lost;

    private DateTime _start;

    bool _armed;

    public void Initialize(GameObject projectile, UCPlayer firer, float projectileSpeed, float responsiveness, float aquisitionRange, float armingDistance, float guidanceDelay)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        if (firer.CurrentVehicle is not null)
        {
            foreach (var passenger in firer.CurrentVehicle.turrets)
            {
                if (passenger.turretAim.TryGetComponent(out HeatSeekingController controller) && controller.GetGunner(firer.CurrentVehicle) == firer)
                {
                    _controller = controller;
                    projectile.transform.forward = passenger.turretAim.forward;
                }
            }
        }
        // TODO: add support for non-vehicle controller

        this._projectile = projectile;
        this._maxTurnDegrees = responsiveness;
        this._projectileSpeed = projectileSpeed;
        this._aquisitionRange = aquisitionRange;
        this._armingDistance = armingDistance;
        this._guidanceDelay = guidanceDelay;
        if (_controller.Status == ELockOnMode.LOCKED_ON)
            this._lost = false;
        else
            this._lost = true;

        this._rigidbody = projectile.GetComponent<Rigidbody>();
        this._colliders = transform.GetComponentsInChildren<Collider>().ToList();
        _colliders.ForEach(c => c.enabled = false);

        _armed = false;

        _start = DateTime.UtcNow;
    }

    private void TrySendWarning()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif


        if (
        _lost ||
        _controller.LockOnTarget is null ||
        !_controller.LockOnTarget.TryGetComponent(out VehicleComponent c) || 
        c.Vehicle.isDead ||
        c.Data is null)
        {
            return;
        }

        for (byte seat = 0; seat < c.Vehicle.passengers.Length; seat++)
        {
            if (c.Vehicle.passengers[seat].player != null &&
                c.Data.Item != null &&
                c.Data.Item.CrewSeats.Contains(seat))
            {
                ushort effectID = VehicleBay.Config.MissileWarningID;
                if (seat == 0)
                    effectID = VehicleBay.Config.MissileWarningDriverID;

                EffectManager.sendUIEffect(effectID, (short)effectID, c.Vehicle.passengers[seat].player.transportConnection, true);
            }
        }
    }
    private float _timeOfLastLoop;
    [UsedImplicitly]
    private void FixedUpdate()
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

        if (_controller.LockOnTarget is not null)
        {
            _lastKnownTarget = _controller.LockOnTarget;
        }

        if (_controller.LockOnTarget != null && Vector3.Angle(_projectile.transform.forward, _controller.LockOnTarget.position - _projectile.transform.position) > 90)
        {
            _lost = true;
        }

        Vector3 idealDirection;
        float turnDegrees;

        if (_lastKnownTarget is null || _lost)
        {
            idealDirection = _controller.AlternativeTargetPosition - _projectile.transform.position;
            turnDegrees = 0.2f;
        }
        else
        {
            idealDirection = (_lastKnownTarget.Find("Center") ?? _lastKnownTarget).position - _projectile.transform.position;
            
            if (_lastKnownTarget.TryGetComponent<InteractableVehicle>(out _))
                turnDegrees = _maxTurnDegrees;
            else
                turnDegrees = 0.5f;
        }

        Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * turnDegrees, Mathf.Deg2Rad * turnDegrees);

        _projectile.transform.forward = targetDirection;
        _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;
        float timeDifference = Time.time - _timeOfLastLoop;
        if (timeDifference > 0.05f)
        {
            JsonAssetReference<EffectAsset> id = Gamemode.Config.EffectHeatSeekingMissileNoSound;
            if (timeDifference > 0.4f)
            {
                id = Gamemode.Config.EffectHeatSeekingMissileSound;

                _timeOfLastLoop = Time.time;

                TrySendWarning();
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
        }
    }
}