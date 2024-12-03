using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class InteractableVehicleRequestExit : IHarmonyPatch
{
    private static MethodInfo? _target;

    internal static Vector3 LastVelocity;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(VehicleManager).GetMethod(nameof(VehicleManager.ReceiveExitVehicleRequest), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for invoking exit vehicle event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(VehicleManager.ReceiveEnterVehicleRequest))
                .DeclaredIn<VehicleManager>(isStatic: true)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .WithParameter<Vector3>("velocity")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for invoking exit vehicle event.", _target);
        _target = null;
    }

    private static void Prefix(in ServerInvocationContext context, Vector3 velocity)
    {
        LastVelocity = velocity;
    }
}