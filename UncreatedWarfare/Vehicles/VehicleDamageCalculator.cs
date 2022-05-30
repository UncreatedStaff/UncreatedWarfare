using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles
{
    internal class VehicleDamageCalculator
    {
        private const float maxAngleBeforeDropOff = 20;

        private static Dictionary<InteractableVehicle, float> damageRegister = new Dictionary<InteractableVehicle, float>();

        public static float GetDamageMultiplier(ProjectileComponent projectileComponent, Collider vehicleCollider)
        {
            if (projectileComponent.IgnoreArmor)
                return 1;

            float multiplier = 1;

            if (vehicleCollider.name.StartsWith("damage_"))
                if (float.TryParse(vehicleCollider.name.Replace("damage_", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                    multiplier = result;

            return multiplier;
        }
        public static float GetDamageMultiplier(InputInfo input)
        {
            if (input.vehicle != null)
            {
                float multiplier = 1;

                if (input.colliderTransform.name.StartsWith("damage_"))
                    if (float.TryParse(input.colliderTransform.name.Replace("damage_", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                        multiplier = result;

                return multiplier;
            }

            return 1;
        }
        public static void RegisterForAdvancedDamage(InteractableVehicle vehicle, float multiplier)
        {
            if (damageRegister.ContainsKey(vehicle))
                damageRegister[vehicle] = multiplier;
            else
                damageRegister.Add(vehicle, multiplier);

            vehicle.StartCoroutine(TimeOutDamage(vehicle));
        }
        public static void ApplyAdvancedDamage(InteractableVehicle vehicle, ref ushort finalDamage)
        {
            L.Log("Attempting to apply damage...");
            if (damageRegister.TryGetValue(vehicle, out float multiplier))
            {
                finalDamage = (ushort)Mathf.RoundToInt(finalDamage * multiplier);
                damageRegister.Remove(vehicle);
                L.Log($"Successfully applied {multiplier}x damage: {finalDamage}");
            }
            else
            {
                finalDamage *= (ushort)Mathf.RoundToInt(finalDamage * 0.1f);
                L.Log($"No direct hit, applied 0.1x damage: {finalDamage}");
            }
        }
        private static IEnumerator<WaitForFixedUpdate> TimeOutDamage(InteractableVehicle vehicle)
        {
            yield return new WaitForFixedUpdate();
            damageRegister.Remove(vehicle);
        }
    }
}
