using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class DetonateChargeDeathTrackerPatches : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(InteractableCharge).GetMethod("detonate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix), postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for saving charge detonations.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(InteractableCharge.detonate))
                .DeclaredIn<InteractableCharge>(isStatic: false)
                .WithParameter<CSteamID>("killer")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for saving charge detonations.", _target);
        _target = null;
    }

    // SDG.Unturned.InteractableCharge
    /// <summary>
    /// Prefix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void Prefix(CSteamID killer, InteractableCharge __instance)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player == null)
            return;

        BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
        if (drop == null)
            return;

        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(player);
        comp.LastChargeDetonated = AssetLink.Create(drop.asset);
    }

    // SDG.Unturned.InteractableCharge
    /// <summary>
    /// Postfix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void Postfix(CSteamID killer)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player == null)
            return;

        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(player);
        comp.LastChargeDetonated = null;
    }
}