using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class BarricadeOnPreDamage : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(BarricadeManager).GetMethod(nameof(BarricadeManager.damage), BindingFlags.Static | BindingFlags.Public);
        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for on barricade on pre-damaged event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("damage")
                .DeclaredIn<BarricadeManager>(isStatic: true)
                .WithParameter<Transform>("transform")
                .WithParameter<float>("damage")
                .WithParameter<float>("times")
                .WithParameter<bool>("armor")
                .WithParameter<CSteamID>("instigatorSteamID")
                .WithParameter<EDamageOrigin>("damageOrigin")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for barricade on pre-damaged event.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? askDamageMethod = typeof(Barricade)
            .GetMethod("askDamage", BindingFlags.Public | BindingFlags.Instance);
        if (askDamageMethod == null)
        {
            return ctx.Fail(new MethodDefinition("askDamage")
                .DeclaredIn<Barricade>(isStatic: false)
                .WithParameter<ushort>("amount")
                .ReturningVoid()
            );
        }

        bool patchDone = false;
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(askDamageMethod))
            {
                continue;
            }

            // strategy: insert the invoker call just before the call to barricade.askDamage

            // local loaded just before askDamage
            LocalReference lclPendingDamage = PatchUtil.GetLocal(ctx[ctx.CaretIndex - 1], false);

            // find barricade index, move backwards from pendingDamage to last used local before that.
            LocalReference lclBarricade = default;
            for (int j = ctx.CaretIndex - 2; j >= 0; --j)
            {
                if (!ctx[j].IsLdloc())
                    continue;

                lclBarricade = PatchUtil.GetLocal(ctx[j], false);
                break;
            }

            if (lclPendingDamage.Index < 0)
                return ctx.Fail("Failed to find pending damage local.");

            if (lclBarricade.Index < 0)
                return ctx.Fail("Failed to find barricade local.");

            ctx.EmitAbove(emit =>
            {
                emit.LoadLocalValue(lclBarricade)
                    .LoadLocalValue(lclPendingDamage)
                    .LoadArgument(4)
                    .LoadArgument(5)
                    .Invoke(Accessor.GetMethod(PreBarricadeDamageInvoker)!);
            });
            patchDone = true;
            break;
        }

        if (!patchDone)
        {
            return ctx.Fail("Failed to find askDamage call.");
        }

        return ctx;
    }
    private static void PreBarricadeDamageInvoker(BarricadeDrop barricadeDrop, ushort pendingDamage, CSteamID instigatorId, EDamageOrigin damageOrigin)
    {
        BarricadePreDamaged args = new BarricadePreDamaged
        {
            Drop = barricadeDrop,
            Buildable = new BuildableBarricade(barricadeDrop),
            PendingDamage = pendingDamage,
            Instigator = instigatorId != default ? instigatorId : null,
            DamageOrigin = damageOrigin
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, allowAsync: false);
    }
}