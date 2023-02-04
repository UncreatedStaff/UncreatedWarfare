using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare.Components;

internal class GuidedMissileComponent : MonoBehaviour
{
    //private Player _firer; // player who fired the projectile
    private GameObject _projectile; // the projectile

    private Rigidbody _rigidbody; // projectile's rigidbody, used for setting its velocity

    private float _guiderDistance; // the distance between the point that the projectile should look at and the 'aim' gameobject. this gets updated every fixed udpate
    private float _cutoffDistance;
    private float _projectileSpeed; // meters per second
    private float _maxTurnDegrees; // projectile can turn at most this amount every fixed update (in degrees)
    private Transform _aim; // the turrets 'Aim' gameobject
    private Vector3 _lookAt; // the point that the projectile should look towards every fixed update

    private bool _isActive;

    public void Initialize(GameObject projectile, Player firer, float projectileSpeed, float responsiveness, float cutoffDistance = 1000)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        this._projectile = projectile;
        //this._firer = firer;
        this._maxTurnDegrees = responsiveness;
        this._projectileSpeed = projectileSpeed;
        this._cutoffDistance = cutoffDistance;

        _guiderDistance = 30; // offset the distance of the guider position so that it will always be 30m in front of the actual projectile
        // this also means you shouldn't make your projectile's hitbox long if you're still using it for client-side effects

        _isActive = false;


        // need to find the the player's turret so we can get the 'Aim' gameobject

        if (projectile.TryGetComponent(out _rigidbody))
        {
            InteractableVehicle vehicle = firer.movement.getVehicle(); // check if the player is in a vehicle
            if (vehicle != null)
            {
                foreach (Passenger? turret in vehicle.turrets)
                {
                    if (turret.player != null && turret.player.player == firer)
                    {
                        // turret has been found
                        _aim = turret.turretAim;
                        _isActive = true;
                        StartCoroutine(Tick());
                        // like everything else, nelson's projectile are rotated -90 on one side, so we have to correct this otherwise the guiding gets messed up
                        projectile.transform.forward = _aim.forward;
                        _rigidbody.velocity = projectile.transform.forward * projectileSpeed;
                        return;
                    }
                }
                L.LogDebug("GUIDED MISSILE ERROR: player firing not found");
            }
            else
                L.LogDebug("GUIDED MISSILE ERROR: player was not in a vehicle");
        }
        else
            L.LogDebug("GUIDED MISSILE ERROR: could not find rigidbody");
    }
    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (_isActive)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            // update the distance of the guider position so that it stays in front of the projectile
            _guiderDistance += Time.fixedDeltaTime * _projectileSpeed;

            if (_guiderDistance > _cutoffDistance)
                _isActive = false;

            _lookAt = _aim.TransformPoint(new Vector3(0, 0, _guiderDistance)); // the vector point we want to look at

            Vector3 idealDirection = _lookAt - transform.position; // this is the ideal direction vector that the projectile should aim at

            // rotate the current projectile direction at most (maxTurnDegrees) degrees towards the ideal direction
            Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * _maxTurnDegrees, Mathf.Deg2Rad * _maxTurnDegrees);

            // rotate the projectile and update its velocity
            transform.forward = targetDirection;
            _rigidbody.velocity = _projectile.transform.forward * _projectileSpeed;
        }
    }
    private IEnumerator<WaitForSeconds> Tick()
    {
        int count = 0;
        // need 2 different smoke effects so that the rocket sound doesnt overlap too much
        JsonAssetReference<EffectAsset> id = Gamemode.Config.EffectGuidedMissileNoSound; // effect ids. this one has no sound effect

        while (_isActive) // this loop runs every 0.05 seconds. every iteration it will send a small smoke trail effect to all clients here
        {
            if (count % 20 == 0 || !id.ValidReference(out ushort _))
            {
                id = Gamemode.Config.EffectGuidedMissileSound; // this one has a sound effect, so we will play it only after around 20 loops (1 second) have passed
                if (!id.ValidReference(out ushort _))
                    id = Gamemode.Config.EffectGuidedMissileNoSound;
            }
            yield return new WaitForSeconds(0.05f);

            if (id.ValidReference(out EffectAsset effect))  // send the effect to all clients here
            {
                EffectManager.triggerEffect(new TriggerEffectParameters(effect)
                {
                    relevantDistance = 1200f,
                    position = _projectile.transform.position,
                    direction = _projectile.transform.forward,
                    reliable = false
                });
            }
            count++;
        }
    }
}
