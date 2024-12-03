using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal class PreventRemovingBatteryPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(VehicleManager).GetMethod(nameof(VehicleManager.ReceiveStealVehicleBattery), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for preventing remoiving batteries from vehicles.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(VehicleManager.ReceiveStealVehicleBattery))
                .DeclaredIn<VehicleManager>(isStatic: true)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for preventing remoiving batteries from vehicles.", _target);
        _target = null;
    }

    // SDG.Unturned.PlayerClothing
    /// <summary>
    /// Prefix for <see cref="VehicleManager.ReceiveStealVehicleBattery"/> to prevent remoiving batteries from vehicles.
    /// </summary>
    private static bool Prefix(in ServerInvocationContext context)
    {
#if RELEASE
        return context.GetCallingPlayer().isAdmin;
#else
        return false;
#endif
    }
}