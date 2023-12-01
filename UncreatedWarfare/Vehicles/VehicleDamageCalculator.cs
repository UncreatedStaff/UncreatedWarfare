using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

internal class VehicleDamageCalculator
{
    private static readonly Dictionary<InteractableVehicle, float> damageRegister = new Dictionary<InteractableVehicle, float>();
    // TODO: move this to config later
    private static readonly List<Guid> AirAttackOnly = new List<Guid>()
    {
        new Guid("d9447148f8aa41f0ad885edd24ac5a02"), // J-10
        new Guid("0b21724b3a1f40e7b88de9484a1733bc"), // JH-7A
        new Guid("0a58fa7fdad2470c97af71e03059f181"), // Eurofighter
        new Guid("661a347f5e56406e85510a1b427bc4d6"), // F-15E
        new Guid("58b18a3fa1104ca58a7bdebef3ab6b29"), // Stinger
        new Guid("5ae39e59d299415d8c4d08b233206302"), // Igla
    };
    private static readonly List<Guid> GroundAttackOnly = new List<Guid>()
    {
        //commented these out since not sure if people would like this
        //new Guid("06cdbbf06436409b9c3ba9237cc486aa"), // AH-1Z rockets
        //new Guid("fc81c5f027364023b212900e9c3c7697"), // Mi-28 rockets
        //new Guid("45de0912d5994947800e1732937de373"), // Z-10 rockets
        //new Guid("e929441a7bf7471b80072654275255c1"), // AH-1Z gun
        //new Guid("222f108a7c50462f9c2f4d8730365df3"), // Mi-28 gun
        //new Guid("229d2003673d41949cfed6bdb07c9c5a"), // Eurocopter gun
        //new Guid("61423656d457489fb7f6928e907c03b3"), // Z-10 gun
    };
    private static readonly List<Guid> IgnoreArmorMultiplier = new List<Guid>()
    {
        new Guid("06cdbbf06436409b9c3ba9237cc486aa"), // F-15 bombs
        new Guid("8b61f77fa7194baaba65d1ec0a5c0e87"), // Su-34 bombs
        new Guid("433ea5249699420eb7adb67791a98134"), // F-15 laser guided
        new Guid("3754ca2527ee40e2ad0951c8930efb07"), // Su-34 laser guided
    };

    public static float GetComponentDamageMultiplier(ProjectileComponent projectileComponent, Collider vehicleCollider)
    {
        float multiplier = 1;

        if (vehicleCollider.name.StartsWith("damage_"))
            if (float.TryParse(vehicleCollider.name.Substring(7), NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                multiplier = result;

        return multiplier;
    }
    public static float GetComponentDamageMultiplier(InputInfo input)
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
        VehicleComponent? vehicleComponent = vehicle.transform.GetComponentInChildren<VehicleComponent>();

        //L.LogDebug("Attempting to apply damage...");
        if (damageRegister.TryGetValue(vehicle, out float multiplier))
        {
            finalDamage = (ushort)Mathf.RoundToInt(finalDamage * multiplier);

            if (AirAttackOnly.Contains(vehicleComponent.LastItem) && !vehicleComponent.IsAircraft)
                finalDamage = (ushort)Mathf.RoundToInt(finalDamage * 0.1f);

            if (GroundAttackOnly.Contains(vehicleComponent.LastItem) && vehicleComponent.IsAircraft)
                finalDamage = (ushort)Mathf.RoundToInt(finalDamage * 0.1f);

            damageRegister.Remove(vehicle);
            //L.LogDebug($"Successfully applied {multiplier}x damage: {finalDamage}");
        }
        else if (!IgnoreArmorMultiplier.Contains(vehicleComponent.LastItem) && !vehicleComponent.IsEmplacement)
        {
            finalDamage = (ushort)Mathf.RoundToInt(finalDamage * 0.1f);
            //L.LogDebug($"No direct hit, applied 0.1x damage: {finalDamage}");
        }
    }
    private static IEnumerator<WaitForFixedUpdate> TimeOutDamage(InteractableVehicle vehicle)
    {
        yield return new WaitForFixedUpdate();
        damageRegister.Remove(vehicle);
    }
}
