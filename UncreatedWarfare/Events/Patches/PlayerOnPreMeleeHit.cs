using DanielWillett.ReflectionTools.Formatting;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players;
using static Uncreated.Warfare.Harmony.Patches;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players.PendingTasks;
using Uncreated.Warfare.Players.Saves;
using Uncreated.Warfare.Events.Models.Fobs;

namespace Uncreated.Warfare.Events.Patches;
// OnPreMeleeHit(UseableMelee __instance)
[UsedImplicitly]
internal class PlayerOnPreMeleeHit : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(UseableMelee).GetMethod("fire", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_target != null)
        {
            Patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
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

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
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
