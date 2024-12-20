using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class PreventFriendlyBlockMortarPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for preventing rocket explosion on friendlies.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<Rocket>(isStatic: false)
                .WithParameter<Collider>("other")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for preventing rocket explosion on friendlies.", _target);
        _target = null;
    }

    // SDG.Unturned.Rocket.OnTriggerEnter
    /// <summary>
    /// Checking for friendlies standing on mortars.
    /// </summary>
    private static bool Prefix(Collider other, Rocket __instance, bool ___isExploded)
    {
        if (___isExploded || other.isTrigger || (__instance.ignoreTransform != null && (__instance.ignoreTransform == other.transform || other.transform.IsChildOf(__instance.ignoreTransform))))
            return false;

        if (!other.transform.CompareTag("Player"))
            return true;
        
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();
        WarfarePlayer? target = playerService.GetOnlinePlayerOrNull(DamageTool.getPlayer(other.transform));
        if (target == null)
            return true;

        WarfarePlayer? pl = playerService.GetOnlinePlayerOrNull(__instance.killer);
        return pl == null || pl.Team.IsOpponent(target.Team);
    }
}