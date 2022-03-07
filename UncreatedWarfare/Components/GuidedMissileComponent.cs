using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare.Components
{
    internal class GuidedMissileComponent : MonoBehaviour
    {
        private Player firer; // player who fired the projectile
        private GameObject projectile; // the projectile

        private Rigidbody rigidbody; // projectile's rigidbody, used for setting its velocity

        private float guiderDistance; // the distance between the point that the projectile should look at and the 'aim' gameobject. this gets updated every fixed udpate
        private float cutoffDistance;
        private float projectileSpeed; // meters per second
        private float maxTurnDegrees; // projectile can turn at most this amount every fixed update (in degrees)
        private Transform aim; // the turrets 'Aim' gameobject
        private Vector3 lookAt; // the point that the projectile should look towards every fixed update

        private bool isActive;

        public void Initialize(GameObject projectile, Player firer, float projectileSpeed, float responsiveness, float cutoffDistance = 1000)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            this.projectile = projectile;
            this.firer = firer;
            this.maxTurnDegrees = responsiveness;
            this.projectileSpeed = projectileSpeed;
            this.cutoffDistance = cutoffDistance;

            guiderDistance = 30; // offset the distance of the guider position so that it will always be 30m in front of the actual projectile
            // this also means you shouldn't make your projectile's hitbox long if you're still using it for client-side effects

            isActive = false;


            // need to find the the player's turret so we can get the 'Aim' gameobject

            if (projectile.TryGetComponent(out rigidbody))
            {
                var vehicle = firer.movement.getVehicle(); // check if the player is in a vehicle
                if (vehicle != null)
                {
                    foreach (var turret in vehicle.turrets)
                    {
                        if (turret.player != null && turret.player.player == firer)
                        {
                            // turret has been found
                            aim = turret.turretAim;
                            isActive = true;
                            StartCoroutine(Tick());
                            // like everything else, nelson's projectile are rotated -90 on one side, so we have to correct this otherwise the guiding gets messed up
                            projectile.transform.forward = aim.forward;
                            rigidbody.velocity = projectile.transform.forward * projectileSpeed;
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
        private void FixedUpdate()
        {
            if (isActive)
            {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                // update the distance of the guider position so that it stays in front of the projectile
                guiderDistance += Time.fixedDeltaTime * projectileSpeed;

                if (guiderDistance > cutoffDistance)
                    isActive = false;

                lookAt = aim.TransformPoint(new Vector3(0, 0, guiderDistance)); // the vector point we want to look at

                Vector3 idealDirection = lookAt - transform.position; // this is the ideal direction vector that the projectile should aim at

                // rotate the current projectile direction at most (maxTurnDegrees) degrees towards the ideal direction
                Vector3 targetDirection = Vector3.RotateTowards(transform.forward, idealDirection, Mathf.Deg2Rad * maxTurnDegrees, Mathf.Deg2Rad * maxTurnDegrees);

                // rotate the projectile and update its velocity
                transform.forward = targetDirection;
                rigidbody.velocity = projectile.transform.forward * projectileSpeed;
            }
        }
        private IEnumerator<WaitForSeconds> Tick()
        {
            int count = 0;  

            while (isActive) // this loop runs every 0.05 seconds. every iteration it will send a small smoke trail effect to all clients here
            {
                // need 2 different smoke effects so that the rocket sound doesnt overlap too much
                ushort id = 26031; // effect ids. this one has no sound effect
                if (count == 0)
                    id = 26032; // this one has a sound effect, so we will play it only after around 20 loops (1 second) have passed

                yield return new WaitForSeconds(0.05f);
                EffectManager.sendEffect(id, 1000, projectile.transform.position, projectile.transform.forward); // send the effect to all clients here

                count++;
                if (count >= 20)
                    count = 0;
            }
        }
    }
}
