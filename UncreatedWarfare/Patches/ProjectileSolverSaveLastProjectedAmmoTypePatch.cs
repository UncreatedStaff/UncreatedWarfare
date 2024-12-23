using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Projectiles;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class ProjectileSolverSaveLastProjectedAmmoTypePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(UseableGun).GetMethod("fire", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for saving last projected ammo type before ProjectileSolver gets the projectile.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("fire")
                .DeclaredIn<UseableGun>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for saving last projected ammo type before ProjectileSolver gets the projectile.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        bool patched = false;
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.IsStloc() || ctx.Instruction.operand is not LocalBuilder builder ||
                builder.LocalType != typeof(ItemMagazineAsset))
            {
                continue;
            }

            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(0)
                    .LoadLocalValue(builder)
                    .Invoke(Accessor.GetMethod(OnPreProject)!);
            });

            patched = true;
        }

        if (!patched)
        {
            ctx.Fail("Unable to patch UseableGun.fire to save projectile type.");
        }

        return ctx;
    }

    private static void OnPreProject(UseableGun gun, ItemMagazineAsset magazine)
    {
        WarfareModule.Singleton.ServiceProvider.ResolveOptional<ProjectileSolver>()
            ?.RegisterLastMagazineShot(gun.player.channel.owner.playerID.steamID, magazine);
    }
}