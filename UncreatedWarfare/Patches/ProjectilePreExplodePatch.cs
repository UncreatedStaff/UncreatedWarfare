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
internal sealed class ProjectilePreExplodePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug($"Patched {_target} for receiving projectile explode event.");
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<Rocket>(isStatic: false)
                .WithParameter<Collider>("other")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug($"Unpatched {_target} for receiving projectile explode event.");
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? explode = typeof(DamageTool).GetMethod(nameof(DamageTool.explode),
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null,
            [ typeof(ExplosionParameters), typeof(List<EPlayerKill>).MakeByRefType() ], null);

        if (explode == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(DamageTool.explode))
                .DeclaredIn(typeof(DamageTool), isStatic: true)
                .WithParameter<ExplosionParameters>("parameters")
                .WithParameter<List<EPlayerKill>>("kills", ByRefTypeMode.Ref)
                .ReturningVoid()
            );
        }

        bool patched = false;
        while (ctx.MoveNext())
        {
            if (ctx.CaretIndex >= ctx.Count - 1 || !ctx[ctx.CaretIndex + 1].Calls(explode))
                continue;

            patched = true;
            Label lbl = default;
            ctx.EmitAbove(emit =>
            {
                emit.Duplicate()
                    .LoadArgument(0)
                    .LoadArgument(1)
                    .Invoke(Accessor.GetMethod(InvokeExploding)!)
                    .AddLabel(out lbl)
                    .BranchIfTrue(lbl)
                    .PopFromStack()
                    .Return();
            });

            ctx.MarkLabel(lbl);
            ctx.ApplyBlocksAndLabels();
            break;
        }

        if (!patched)
        {
            return ctx.Fail("Failed to patch, didnt find explode method.");
        }

        return ctx;
    }

    private static bool InvokeExploding(ExplosionParameters parameters, Rocket rocket, Collider other)
    {
        if (rocket.gameObject.TryGetComponent(out WarfareProjectile projectile))
        {
            return projectile.InvokeExploding(other, parameters);
        }

        return false;
    }
}