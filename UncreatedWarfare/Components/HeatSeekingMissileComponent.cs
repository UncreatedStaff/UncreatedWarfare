using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Components.HeatSeekingController;

namespace Uncreated.Warfare.Components;

internal class HeatSeekingMissileComponent : MonoBehaviour
{
    //private Player _firer;
    private GameObject _projectile;
    private HeatSeekingController _controller;
    private Transform? _lastKnownTarget;
    private Vector3 _alternativeTarget;

    private Rigidbody _rigidbody;

    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _guidanceRampTime;

    private float _startTime;
    private bool _lost;

    public void Initialize(GameObject projectile, UCPlayer firer, float projectileSpeed, float responsiveness, float guidanceRampTime)
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
                    var hardpoint = _controller.CycleHardpoint();
                    if (hardpoint != null)
                        projectile.transform.position = hardpoint.position;
                    projectile.transform.forward = passenger.turretAim.forward;
                }
            }
        }
        // TODO: add support for non-vehicle controller

        this._projectile = projectile;
        this._maxTurnDegrees = responsiveness;
        this._projectileSpeed = projectileSpeed;
        this._guidanceRampTime = guidanceRampTime;
        this._startTime = Time.time;
        if (_controller.Status == ELockOnMode.LOCKED_ON)
            this._lost = false;
        else
            this._lost = true;

        this._rigidbody = projectile.GetComponent<Rigidbody>();

        _alternativeTarget = _projectile.transform.TransformPoint(Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * new Vector3(0, 500, 500));
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

        if (c.Data is { Item: { } item })
        {
            for (byte seat = 0; seat < c.Vehicle.passengers.Length; seat++)
            {
                if (c.Vehicle.passengers[seat].player != null && item.CrewSeats.Contains(seat))
                {
                    ushort effectID = VehicleBay.Config.MissileWarningID;
                    if (seat == 0)
                        effectID = VehicleBay.Config.MissileWarningDriverID;

                    EffectManager.sendUIEffect(effectID, (short)effectID, c.Vehicle.passengers[seat].player.transportConnection, true);
                }
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

        if (_controller.LockOnTarget is not null)
        {
            _lastKnownTarget = _controller.LockOnTarget;
        }

        if (_controller.LockOnTarget != null && Vector3.Angle(_projectile.transform.forward, _controller.LockOnTarget.position - _projectile.transform.position) > 90)
        {
            _lost = true;
        }

        Vector3 idealDirection;
        float turnDegrees = _maxTurnDegrees;

        if (_lastKnownTarget is null || _lost)
        {
            idealDirection = _alternativeTarget - _projectile.transform.position;
        }
        else
        {
            idealDirection = (_lastKnownTarget.Find("Center") ?? _lastKnownTarget).position - _projectile.transform.position;
        }

        float guidanceMultiplier = Mathf.Clamp((Time.time - _startTime) / _guidanceRampTime, 0, _guidanceRampTime);

        Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * guidanceMultiplier * turnDegrees, 0);

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