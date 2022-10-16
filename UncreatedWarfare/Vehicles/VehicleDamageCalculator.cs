using SDG.Unturned;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

internal class VehicleDamageCalculator
{
    private const float maxAngleBeforeDropOff = 20;

    private static readonly Dictionary<InteractableVehicle, float> damageRegister = new Dictionary<InteractableVehicle, float>();

    public static float GetDamageMultiplier(ProjectileComponent projectileComponent, Collider vehicleCollider)
    {
        if (projectileComponent.IgnoreArmor)
            return 1;

        float multiplier = 1;

        if (vehicleCollider.name.StartsWith("damage_"))
            if (float.TryParse(vehicleCollider.name.Substring(7), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                multiplier = result;

        return multiplier;
    }
    public static float GetDamageMultiplier(InputInfo input)
    {
        if (input.vehicle != null && input.colliderTransform != null)
        {
            float multiplier = 1;

            if (input.colliderTransform.name.StartsWith("damage_"))
                if (float.TryParse(input.colliderTransform.name.Substring(7), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
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
        L.LogDebug("Attempting to apply damage...");
        if (damageRegister.TryGetValue(vehicle, out float multiplier))
        {
            finalDamage = (ushort)Mathf.RoundToInt(finalDamage * multiplier);
            damageRegister.Remove(vehicle);
            L.LogDebug($"Successfully applied {multiplier}x damage: {finalDamage}");
        }
        else
        {
            finalDamage *= (ushort)Mathf.RoundToInt(finalDamage * 0.1f);
            L.LogDebug($"No direct hit, applied 0.1x damage: {finalDamage}");
        }
    }
    private static IEnumerator<WaitForFixedUpdate> TimeOutDamage(InteractableVehicle vehicle)
    {
        yield return new WaitForFixedUpdate();
        damageRegister.Remove(vehicle);
    }
}
