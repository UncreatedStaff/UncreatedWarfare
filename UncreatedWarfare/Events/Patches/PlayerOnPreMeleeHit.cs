using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

// OnPreMeleeHit(UseableMelee __instance)
[UsedImplicitly]
internal sealed class PlayerOnPreMeleeHit : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(UseableMelee).GetMethod("fire", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for on melee hit event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("fire")
                .DeclaredIn<UseableMelee>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for on melee hit event.", _target);
        _target = null;
    }

    private static bool Prefix(UseableMelee __instance)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer? hitter = playerService.GetOnlinePlayerOrNull(__instance.player);
        if (hitter != null)
        {
            MeleeHit meleeHit = new MeleeHit
            {
                Player = hitter,
                MeleeWeapon = __instance
            };
            _ = WarfareModule.EventDispatcher.DispatchEventAsync(meleeHit);
        }


        return true;
    }
}