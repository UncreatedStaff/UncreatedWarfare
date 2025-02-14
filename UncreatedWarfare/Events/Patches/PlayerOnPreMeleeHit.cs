using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler), postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for on melee hit events.", _target);
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

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for on melee hit events.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);
        
        MethodInfo? getInput = typeof(PlayerInput).GetMethod(nameof(PlayerInput.getInput), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, [ typeof(bool), typeof(ERaycastInfoUsage) ], null);
        if (getInput == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(PlayerInput.getInput))
                .DeclaredIn<PlayerInput>(isStatic: false)
                .WithParameter<bool>("doOcclusionCheck")
                .WithParameter<ERaycastInfoUsage>("usage")
                .Returning<InputInfo>());
        }

        MethodInfo? getUseableRagdollEffect = typeof(PlayerEquipment).GetMethod("getUseableRagdollEffect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getUseableRagdollEffect == null)
        {
            ctx.LogWarning($"Unable to find method: {Accessor.Formatter.Format(new MethodDefinition(nameof(PlayerEquipment.getUseableRagdollEffect))
                .DeclaredIn<PlayerEquipment>(isStatic: false)
                .WithNoParameters()
                .Returning<ERagdollEffect>())}.");
        }

        FieldInfo invokedHitField = typeof(PlayerOnPreMeleeHit).GetField("_invokedHit", BindingFlags.Static | BindingFlags.NonPublic)!;

        bool patched = false;

        while (ctx.MoveNext())
        {
            if (getUseableRagdollEffect != null && ctx.Instruction.Calls(getUseableRagdollEffect))
            {
                ctx.EmitBelow(emit =>
                {
                    emit.LoadConstantBoolean(true)
                        .SetStaticFieldValue(invokedHitField);
                });
            }

            if (patched) continue;
            if (!ctx.Instruction.Calls(getInput))
                continue;

            if (!ctx.MoveNext())
                break;

            // current instruction: stloc x InputInfo
            LocalReference local = PatchUtil.GetLocal(ctx.Instruction, true);
            if (local.Index < 0)
                break;

            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(0)
                    .LoadLocalValue(local)
                    .Invoke(Accessor.GetMethod(InvokeCallback)!)
                    .AddLabel(out Label label)
                    .BranchIfTrue(label)
                    .Return()
                    .MarkLabel(label);
            });

            ctx.ApplyBlocksAndLabels();
            patched = true;
        }

        return !patched ? ctx.Fail("Failed to patch.") : ctx;
    }

    private static bool _invokedHit;

    private static void Postfix(UseableMelee __instance)
    {
        if (!_invokedHit)
        {
            return;
        }

        _invokedHit = false;

        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer hitter = playerService.GetOnlinePlayer(__instance.player);

        if (!hitter.Data.TryRemove("LastMeleeInput", out object? data) || data is not PlayerMeleeRequested requestArgs)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("PlayerOnPreMeleeHit - LastMeleeInput not found.");
            return;
        }

        PlayerMeleed args = new PlayerMeleed
        {
            Player = hitter,
            Asset = requestArgs.Asset,
            InputInfo = requestArgs.InputInfo
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }

    private static bool InvokeCallback(UseableMelee useable, InputInfo info)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer hitter = playerService.GetOnlinePlayer(useable.player);
        PlayerMeleeRequested args = new PlayerMeleeRequested
        {
            Player = hitter,
            InputInfo = info,
            Asset = useable.equippedMeleeAsset
        };

        hitter.Data["LastMeleeInput"] = args;

        bool canContinue = WarfareModule.EventDispatcher.DispatchEventAsync(args, allowAsync: false).GetAwaiter().GetResult();
        if (!canContinue)
            hitter.Data.TryRemove("LastMeleeInput", out _);

        return canContinue;
    }
}