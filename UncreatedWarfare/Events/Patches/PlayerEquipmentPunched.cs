using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerEquipmentPunched : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerEquipment).GetMethod("punch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [ typeof(EPlayerPunch) ], null);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for punch event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("punch")
                .DeclaredIn<PlayerEquipment>(isStatic: true)
                .WithParameter<EPlayerPunch>("mode")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for punch event.", _target);
        _target = null;
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? getInput = typeof(PlayerInput).GetMethod(nameof(PlayerInput.getInput),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            [ typeof(bool), typeof(ERaycastInfoUsage) ], null);

        if (getInput == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(PlayerInput.getInput))
                .DeclaredIn<PlayerInput>(isStatic: false)
                .WithParameter<bool>("doOcclusionCheck")
                .WithParameter<ERaycastInfoUsage>("usage")
                .Returning<InputInfo>());
        }

        bool patched = false;
        while (ctx.MoveNext())
        {
            // find call to getInput
            if (!ctx.Instruction.Calls(getInput))
                continue;
            
            // skip to next ldloc, br__ pattern
            while (ctx.MoveNext() && !PatchUtility.MatchPattern(ctx,
                       c => c.opcode.IsLdLoc(),
                       c => c.opcode.IsBrAny())
                   ) ;

            LocalReference lcl = PatchUtil.GetLocal(ctx.Instruction, false);
            ctx.MoveNext();
            if (ctx.Instruction.opcode.IsBr(brtrue: true))
            {
                // if brtrue, skip to where the label leads
                Label label = (Label)ctx.Instruction.operand;
                int lbl = PatchUtility.FindLabelDestinationIndex(ctx, label);
                if (lbl == -1)
                    break;

                ctx.CaretIndex = lbl;
            }
            else if (ctx.Instruction.opcode.IsBrAny())
            {
                if (!ctx.MoveNext())
                    break;
            }

            ctx.EmitAbove(emit =>
            {
                // insert method invocation
                emit.LoadLocalValue(lcl)
                    .LoadArgument(0)
                    .LoadArgument(1)
                    .Invoke(Accessor.GetMethod(InvokePunch)!);
            });

            patched = true;
            break;
        }

        return !patched
            ? ctx.Fail("Unable to locate injection location.")
            : ctx;
    }

    private static void InvokePunch(InputInfo input, PlayerEquipment playerCaller, EPlayerPunch punch)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        PlayerPunched args = new PlayerPunched
        {
            Player = playerService.GetOnlinePlayer(playerCaller),
            PunchType = punch,
            InputInfo = input
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }
}
