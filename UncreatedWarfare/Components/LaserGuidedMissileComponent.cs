using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class LaserGuidedMissileComponent : MonoBehaviour
{
    private UCPlayer _firer;
    private GameObject _projectile;

    private Rigidbody _rigidbody;
    private List<BoxCollider> _colliders;

    private float _guiderDistance;
    private float _aquisitionRange;
    private float _projectileSpeed;
    private float _maxTurnDegrees;
    private float _armingDistance;
    private float _fullGuidanceDelay;
    private float _turnMultiplier;
    private Transform _aim;

    public float InitializationTime { get; private set; }

    private SpottedComponent? _laserTarget;

    public bool LockedOn { get => _laserTarget != null; }

    bool _armed;

    private bool _isActive;

    public void Initialize(GameObject projectile, UCPlayer firer, float projectileSpeed, float responsiveness, float aquisitionRange, float armingDistance, float fullGuidanceDelay)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        this._projectile = projectile;
        this._firer = firer;
        this._maxTurnDegrees = responsiveness;
        this._projectileSpeed = projectileSpeed;
        this._aquisitionRange = aquisitionRange;
        this._armingDistance = armingDistance;
        this._fullGuidanceDelay = fullGuidanceDelay;
        this._guiderDistance = 30;
        this._turnMultiplier = 0;

        _count = 0;

        _armed = false;

        InitializationTime = Time.realtimeSinceStartup;

        _isActive = false;

        _laserTarget = null;

        if (projectile.TryGetComponent(out _rigidbody))
        {
            InteractableVehicle? vehicle = firer.CurrentVehicle;
            if (vehicle != null)
            {
                foreach (Passenger turret in vehicle.turrets)
                {
                    if (turret.player != null && turret.player.player == firer)
                    {
                        _aim = turret.turretAim;
                        _isActive = true;

                        projectile.transform.position = vehicle.transform.TransformPoint(new Vector3(-4, 0, -4));
                        projectile.transform.forward = Quaternion.AngleAxis(20, vehicle.transform.right) * vehicle.transform.forward;

                        _rigidbody.velocity = projectile.transform.forward * projectileSpeed;

                        _colliders = projectile.GetComponents<BoxCollider>().ToList();
                        _colliders.ForEach(c => c.enabled = false);

                        return;
                    }
                }
                L.LogDebug("LASER GUIDED MISSILE ERROR: player firing not found");
            }
            else
            {
                _aim = firer.Player.look.aim.transform;
                _isActive = true;
                projectile.transform.forward = _aim.forward;
                _rigidbody.velocity = projectile.transform.forward * projectileSpeed;
                _colliders = projectile.GetComponents<BoxCollider>().ToList();
                _colliders.ForEach(c => c.enabled = false);
                L.LogDebug("LASER GUIDED MISSILE ERROR: player was not in a vehicle");
            }
        }
        else
            L.LogDebug("LASER GUIDED MISSILE ERROR: could not find rigidbody");
    }

    private bool TryAcquireTarget()
    {
        if (_laserTarget != null)
        {
            if (_laserTarget.IsActive)
                return true;
            _laserTarget = null;
        }

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float minAngle = 45;

        if (_firer is null)
            return false;

        foreach (SpottedComponent spotted in SpottedComponent.ActiveMarkers)
        {
            if (spotted.SpottingTeam == _firer.Player.quests.groupID.m_SteamID && spotted.IsLaserTarget)
            {
                if ((spotted.transform.position - _projectile.transform.position).sqrMagnitude < _aquisitionRange * _aquisitionRange)
                {
                    float angleBetween = Vector3.Angle(spotted.transform.position - _projectile.transform.position, _projectile.transform.forward);
                    if (angleBetween < minAngle)
                    {
                        minAngle = angleBetween;
                        _laserTarget = spotted;
                    }
                }
            }
        }

        return _laserTarget != null;
    }

    private int _count;
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

            _turnMultiplier = Mathf.Clamp(_turnMultiplier + Time.fixedDeltaTime / _fullGuidanceDelay, 0, 1);

            if (_guiderDistance > 30 + _armingDistance && !_armed)
            {
                _colliders.ForEach(c => c.enabled = true);
                _armed = true;
            }

            Vector3 target = _aim.TransformPoint(new Vector3(0, 0, _guiderDistance));

            if (TryAcquireTarget())
            {
                target = _laserTarget!.transform.position;
            }

            Vector3 idealDirection = target - _projectile.transform.position;

            float maxAngle = Mathf.Deg2Rad * _maxTurnDegrees * _turnMultiplier;

            Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, maxAngle, maxAngle);

            _projectile.transform.forward = targetDirection;
            _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;

            if (Time.time - _lastSent > 0.05f)
            {
                JsonAssetReference<EffectAsset> id = Gamemode.Config.EffectLaserGuidedNoSound;
                if (_count % 10 == 0 && _armed || !id.ValidReference(out ushort _))
                {
                    id = Gamemode.Config.EffectLaserGuidedSound;
                    if (!id.ValidReference(out ushort _))
                        id = Gamemode.Config.EffectLaserGuidedNoSound;
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

            ++_count;
        }
    }
}
