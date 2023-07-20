using JetBrains.Annotations;
using SDG.Unturned;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;
using static Uncreated.Warfare.Components.HeatSeekingController;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Components;

public class HeatSeekingMissileComponent : MonoBehaviour
{
    //private Player _firer;
    private GameObject _projectile;
    private HeatSeekingController _controller;
    private Transform? _previousTarget;
    private Transform? _lastKnownTarget;
    private Vector3 _randomRelativePosition;
    private int _allowedPathAlterations;

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
        _allowedPathAlterations = 1;
        _startTime = Time.time;
        if (_controller.Status == ELockOnMode.LOCKED_ON)
            _lost = false;
        else
            _lost = true;

        _rigidbody = projectile.GetComponent<Rigidbody>();

        _randomRelativePosition = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * new Vector3(0, 10, 100);

        _controller.MissilesInFlight.Add(this);
    }

    private void OnDestroy()
    {
        L.LogDebug("Missile destroyed. In flight: " + _controller.MissilesInFlight.Count);
        _controller.MissilesInFlight.Remove(this);
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
            if (_previousTarget == null)
            {
                L.LogDebug($"Missile {gameObject.GetInstanceID()} initial target set");
                _previousTarget = _controller.LockOnTarget;
            }
                
            if (_lastKnownTarget != null && _lastKnownTarget != _previousTarget)
            {
                _allowedPathAlterations--;
                _previousTarget = _lastKnownTarget;
                L.LogDebug($"Missile {gameObject.GetInstanceID()} detected path change. Alterations left: " + _allowedPathAlterations);
            }

            if (_allowedPathAlterations > 0)
            {
                L.LogDebug($"Missile {gameObject.GetInstanceID()} successfully tracking target");
                _lastKnownTarget = _controller.LockOnTarget;
            }
        }
        else
        {
            _lastKnownTarget = null;
        }

        if (_lastKnownTarget != null && Vector3.Angle(_projectile.transform.forward, _lastKnownTarget.position - _projectile.transform.position) > 40)
        {
            L.LogDebug("In flight: " + _controller.MissilesInFlight.Count);
            _lost = true;
            L.LogDebug($"Missile {gameObject.GetInstanceID()} lost its target");
            L.LogDebug("In flight: " + _controller.MissilesInFlight.Count);
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
            {
                _rigidbody.useGravity = true;
                L.LogDebug($"Missile {gameObject.GetInstanceID()} timed out");
                L.LogDebug("In flight: " + _controller.MissilesInFlight.Count);
                _controller.MissilesInFlight.Remove(this);
                L.LogDebug("In flight: " + _controller.MissilesInFlight.Count);
                
            }
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