using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class PreventCosmeticChangePatch : IHarmonyPatch
{
    private static MethodInfo? _runtimeTarget;
    private static MethodInfo? _jointimeTarget;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _runtimeTarget = typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveVisualToggleRequest), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _jointimeTarget = typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.load), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_runtimeTarget != null)
        {
            patcher.Patch(_runtimeTarget, prefix: Accessor.GetMethod(RuntimePrefix));
            logger.LogDebug("Patched {0} for preventing enabling cosmetics.", _runtimeTarget);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(PlayerClothing.ReceiveVisualToggleRequest))
                    .DeclaredIn<PlayerClothing>(isStatic: false)
                    .WithParameter<EVisualToggleType>("type")
                    .ReturningVoid()
            );
        }

        if (_jointimeTarget != null)
        {
            patcher.Patch(_jointimeTarget, postfix: Accessor.GetMethod(JointimePostfix));
            logger.LogDebug("Patched {0} for disabling cosmetics on join.", _runtimeTarget);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(PlayerClothing.load))
                    .DeclaredIn<PlayerClothing>(isStatic: false)
                    .WithNoParameters()
                    .ReturningVoid()
            );
        }

    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_runtimeTarget == null)
            return;

        patcher.Unpatch(_runtimeTarget, Accessor.GetMethod(RuntimePrefix));
        logger.LogDebug("Unpatched {0} for preventing enabling cosmetics.", _runtimeTarget);
        _runtimeTarget = null;
    }

    // SDG.Unturned.PlayerClothing
    /// <summary>
    /// Prefix for <see cref="PlayerClothing.ReceiveVisualToggleRequest"/> to prevent enabling cosmetics.
    /// </summary>
    private static bool RuntimePrefix(EVisualToggleType type)
    {
        return false;
    }

    // SDG.Unturned.PlayerClothing
    /// <summary>
    /// Postfix for <see cref="PlayerClothing.load"/> to disable cosmetics on join before they're sent to the player.
    /// </summary>
    private static void JointimePostfix(PlayerClothing __instance)
    {
        if (__instance.isVisual)
            __instance.ReceiveVisualToggleState(EVisualToggleType.COSMETIC, false);

        if (__instance.isSkinned)
            __instance.ReceiveVisualToggleState(EVisualToggleType.SKIN, false);

        if (__instance.isMythic)
            __instance.ReceiveVisualToggleState(EVisualToggleType.MYTHIC, false);
    }
}