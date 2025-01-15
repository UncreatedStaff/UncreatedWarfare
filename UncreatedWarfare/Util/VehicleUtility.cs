using System;
using System.Diagnostics.CodeAnalysis;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for vehicles.
/// </summary>
public static class VehicleUtility
{
    /// <summary>
    /// Find the vehicle who's trunk storage is backed by <paramref name="trunk"/>. Used to identify the vehicle from item events.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool TryGetVehicleFromTrunkStorage([NotNullWhen(true)] Items? trunk, [MaybeNullWhen(false)] out InteractableVehicle vehicle)
    {
        GameThread.AssertCurrent();

        if (trunk is null)
        {
            vehicle = null;
            return false;
        }

        for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
        {
            InteractableVehicle v = VehicleManager.vehicles[i];
            if (v.trunkItems != trunk)
                continue;

            vehicle = v;
            return true;
        }

        vehicle = null;
        return false;
    }
}
