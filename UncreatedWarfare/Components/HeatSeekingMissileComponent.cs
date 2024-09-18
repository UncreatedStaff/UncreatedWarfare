#if DEBUG
#endif
using System;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Logging;
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

    private IAssetLink<EffectAsset> _fxSilent = null!;
    private IAssetLink<EffectAsset> _fxSound = null!;

    public void Initialize(GameObject projectile, Player firer, IServiceProvider serviceProvider, float projectileSpeed, float responsiveness, float guidanceRampTime)
    {
        AssetConfiguration assetConfig = serviceProvider.GetRequiredService<AssetConfiguration>();
        _fxSilent = assetConfig.GetAssetLink<EffectAsset>("Effects:Projectiles:HeatSeekingSilent");
        _fxSound = assetConfig.GetAssetLink<EffectAsset>("Effects:Projectiles:HeatSeekingSound");

        InteractableVehicle vehicle = firer.movement.getVehicle();
        if (vehicle != null && !vehicle.isDead)
        {
            foreach (var passenger in vehicle.turrets)
            {
                if (passenger.turretAim.TryGetComponent(out HeatSeekingController controller) && controller.GetGunner(vehicle) == firer)
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
        if (_controller.Status == HeatSeekingController.ELockOnMode.LOCKED_ON)
            _lost = false;
        else
            _lost = true;

        _rigidbody = projectile.GetComponent<Rigidbody>();

        _randomRelativePosition = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward) * new Vector3(0, 10, 100);

        _controller.MissilesInFlight.Add(this);

        L.LogDebug($"    AA:     Missile launched - {_controller.Status}");
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

        if (timeDifference <= 0.05f)
            return;
        

        IAssetLink<EffectAsset> id = _fxSilent;
        if (timeDifference > 0.4f)
        {
            id = _fxSound;

            _timeOfLastLoop = Time.time;
        }

        if (!id.TryGetAsset(out EffectAsset? effect))
            return;

        TriggerEffectParameters parameters = new TriggerEffectParameters(effect)
        {
            relevantDistance = 1200f,
            position = _projectile.transform.position,
            reliable = false
        };
        parameters.SetDirection(_projectile.transform.forward);
        EffectManager.triggerEffect(parameters);
    }
}