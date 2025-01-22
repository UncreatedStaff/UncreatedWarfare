using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Weapons;
internal class HeatSeekingController : MonoBehaviour // attach to a turrent's 'Aim' gameobject to allow it to control projectiles
{
    private static readonly InstanceGetter<UseableGun, bool>? GetUseableGunReloading
        = Accessor.GenerateInstanceGetter<UseableGun, bool>("isReloading", throwOnError: false);

    private const float AquisitionAngle = 65f;
    private const float AquisitionFrequency = 0.25f;


    private float _horizontalRange = 700;
    private float? _verticalRange = 1500;
#nullable disable

    private InteractableVehicle _vehicle;
    public List<Transform> Hardpoints { get; set; }
    public List<HeatSeekingMissileComponent> MissilesInFlight { get; set; }

#nullable restore

    private EffectAsset? _effect;
    private Player? _lastKnownGunner;

    private float _aquisitionTime;
    private float _timeOutTime;

    private float _timeOfAquisition;
    private float _timeOfLastScan;
    public int CurrentHardpoint;

    public Transform? LockOnTarget { get; private set; }
    public ELockOnMode Status { get; private set; }
    public Transform? CycleHardpoint()
    {
        if (Hardpoints.Count == 0)
            return null;

        CurrentHardpoint++;
        if (CurrentHardpoint >= Hardpoints.Count)
            CurrentHardpoint = 0;

        return Hardpoints[CurrentHardpoint];
    }

    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (Time.time - _timeOfLastScan >= AquisitionFrequency)
        {
            ScanForTargets();
            _timeOfLastScan = Time.time;
        }
    }

    public void Initialize(float horizontalRange, float verticalRange, IAssetLink<EffectAsset>? lockOnEffect, float aquisitionTime, float timeOutTime, ILogger logger)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = horizontalRange;
        _verticalRange = verticalRange;
        _aquisitionTime = aquisitionTime;
        _timeOutTime = timeOutTime;

        Hardpoints = new List<Transform>();
        MissilesInFlight = new List<HeatSeekingMissileComponent>();
        CurrentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + i);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

        if (!lockOnEffect.TryGetAsset(out _effect))
        {
            logger.LogWarning("HEATSEAKER ERROR: Lock on sound effect not found: {0}.", lockOnEffect?.Guid ?? Guid.Empty);
        }
    }
    public void Initialize(float range, IAssetLink<EffectAsset> lockOnEffect, float aquisitionTime, float timeOutTime, ILogger logger)
    {
        _vehicle = GetComponentInParent<InteractableVehicle>();
        _horizontalRange = range;
        _aquisitionTime = aquisitionTime;
        _timeOutTime = timeOutTime;

        Hardpoints = new List<Transform>();
        MissilesInFlight = new List<HeatSeekingMissileComponent>();
        CurrentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + i);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

        Hardpoints = new List<Transform>();
        CurrentHardpoint = 0;
        for (int i = 0; i < 8; i++)
        {
            var hardpoint = _vehicle.transform.Find("Hardpoint_" + 0);
            if (hardpoint != null)
                Hardpoints.Add(hardpoint);
        }

        if (!lockOnEffect.TryGetAsset(out _effect))
        {
            logger.LogWarning("HEATSEAKER ERROR: Lock on sound effect not found: {0}.", lockOnEffect?.Guid ?? Guid.Empty);
        }
    }

    public Player? GetGunner(InteractableVehicle vehicle)
    {
        if (vehicle.turrets == null)
            return null;
        foreach (Passenger turret in vehicle.turrets)
        {
            if (turret != null && turret.turretAim == transform && turret.player != null)
            {
                return turret.player.player;
            }
        }
        return null;
    }

    private void ScanForTargets()
    {
        Transform? newTarget = null;

        Player? gunner = GetGunner(_vehicle);

        if (gunner != null)
            _lastKnownGunner = gunner;
        else if (_lastKnownGunner != null) // gunner exited the vehicle
        {
            CancelLockOnSound(_lastKnownGunner);
            _lastKnownGunner = null;
        }

        float bestTarget = AquisitionAngle;

        foreach (InteractableVehicle v in VehicleManager.vehicles)
        {
            if (v.asset.engine is EEngine.PLANE or EEngine.HELICOPTER && !v.isDead && v.lockedGroup != _vehicle.lockedGroup && v.isEngineOn && !(v.anySeatsOccupied && false /* todo && TeamManager.IsInAnyMain(v.transform.position) */))
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

        bestTarget = AquisitionAngle;

        bool lockedOntoCountermeassure = LockOnTarget != null &&
            LockOnTarget.TryGetComponent(out FlareCountermeasure countermeasure) &&
            countermeasure.Burning;

        if (!lockedOntoCountermeassure)
        {
            foreach (FlareCountermeasure c in FlareCountermeasure.ActiveCountermeasures)
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

    private void LockOn(Transform? newTarget, Player? gunner)
    {
        bool noAmmo = gunner != null && gunner.equipment.state[10] == 0;

        UseableGun? gun = gunner?.equipment.useable as UseableGun;
        bool reloading = gun != null && GetUseableGunReloading != null && GetUseableGunReloading(gun);
        bool noActiveMissiles = MissilesInFlight.Count == 0;

        if (newTarget is null || noActiveMissiles && (noAmmo || reloading)) // no target found
        {
            //(reloading && noActiveMissiles) || (noAmmo && noActiveMissiles)
            if (Status != ELockOnMode.Idle)
            {

                Status = ELockOnMode.Idle;

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
            Status = ELockOnMode.Acquiring;

            if (gunner != null)
                PlayLockOnSound(gunner);
        }
        else // target is the same as the previous call
        {
            var timeSinceAquisition = Time.time - _timeOfAquisition;

            if (timeSinceAquisition >= _aquisitionTime)
            {
                Status = ELockOnMode.LockedOn;

                if (LockOnTarget.TryGetComponent(out WarfareVehicleComponent v) && gunner != null)
                    v.WarfareVehicle.FlareEmitter?.ReceiveMissileWarning();

                if (Time.time - _timeOfAquisition >= _timeOutTime)
                {
                    Status = ELockOnMode.Idle;
                    LockOnTarget = null;
                }
            }
        }
    }
    private static readonly short LockOnEffectKey = UnturnedUIKeyPool.Claim();
    private void PlayLockOnSound(Player gunner)
    {
        if (_effect == null || gunner == null)
            return;

        EffectManager.sendUIEffect(_effect.id, LockOnEffectKey, gunner.channel.owner.transportConnection, true);
    }
    private void CancelLockOnSound(Player gunner)
    {
        if (_effect == null || gunner == null)
            return;

        EffectManager.ClearEffectByGuid(_effect.GUID, gunner.channel.owner.transportConnection);
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
        Idle,
        Acquiring,
        LockedOn
    }
}