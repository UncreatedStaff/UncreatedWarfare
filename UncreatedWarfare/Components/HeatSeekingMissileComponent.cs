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
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;

public class HeatSeekingMissileComponent : MonoBehaviour
{
    //private Player _firer;
    private GameObject _projectile;
    private HeatSeekingController _controller;
    private Transform? _lastKnownTarget;
    private Vector3 _randomRelativePosition;

    private Rigidbody _rigidbody;

    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _guidanceRampTime;

    private float _startTime;
    private bool _lost;

    private const float TIMEOUT = 10;

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

        _projectile = projectile;
        _maxTurnDegrees = responsiveness;
        _projectileSpeed = projectileSpeed;
        _guidanceRampTime = guidanceRampTime;
        _startTime = Time.time;
        if (_controller.Status == ELockOnMode.LOCKED_ON)
            _lost = false;
        else
            _lost = true;

        _rigidbody = projectile.GetComponent<Rigidbody>();

        _randomRelativePosition = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * new Vector3(0, 10, 100);
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

        c.ReceiveMissileWarning(this);
    }
    private float _timeOfLastLoop;
    [UsedImplicitly]
    private void FixedUpdate()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_controller == null)
        {
            Destroy(this);
            return;
        }
        if (_controller.LockOnTarget != null)
        {
            _lastKnownTarget = _controller.LockOnTarget;
        }

        if (_controller.LockOnTarget != null && Vector3.Angle(_projectile.transform.forward, _controller.LockOnTarget.position - _projectile.transform.position) > 40)
        {
            _lost = true;
        }

        Vector3 idealDirection;
        float turnDegrees = _maxTurnDegrees;
        float guidanceMultiplier = Mathf.Clamp((Time.time - _startTime) / _guidanceRampTime, 0, _guidanceRampTime);

        if (_lastKnownTarget == null || _lost)
        {
            idealDirection = _projectile.transform.TransformPoint(_randomRelativePosition) - _projectile.transform.position;
            guidanceMultiplier *= 0.1f;
        }
        else
        {
            idealDirection = (_lastKnownTarget.Find("Center") ?? _lastKnownTarget).position - _projectile.transform.position;
        }

        if ((Time.time - _startTime) > TIMEOUT)
        {
            if (!_rigidbody.useGravity)
                _rigidbody.useGravity = true;
        }
        else
        {
            Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * guidanceMultiplier * turnDegrees, 0);

            _projectile.transform.forward = targetDirection;
            _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;
        }

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