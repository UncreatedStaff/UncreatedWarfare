using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class PreventCosmeticChangePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveVisualToggleRequest), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for preventing enabling cosmetics.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerClothing.ReceiveVisualToggleRequest))
                .DeclaredIn<PlayerClothing>(isStatic: false)
                .WithParameter<EVisualToggleType>("type")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for preventing enabling cosmetics.", _target);
        _target = null;
    }

    // SDG.Unturned.PlayerClothing
    /// <summary>
    /// Prefix for <see cref="PlayerClothing.ReceiveVisualToggleRequest"/> to prevent enabling cosmetics.
    /// </summary>
    private static bool Prefix(EVisualToggleType type)
    {
        return false;
    }
}