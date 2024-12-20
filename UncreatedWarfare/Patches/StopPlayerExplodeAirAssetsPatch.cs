using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Uncreated.Warfare.Patches;
internal sealed class StopPlayerExplodeAirAssetsPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(Bumper).GetMethod("OnTriggerEnter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for blocking players from exploding air assets.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<Bumper>(isStatic: false)
                .WithParameter<Collider>("other")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for blocking players from exploding air assets.", _target);
        _target = null;
    }

    // SDG.Unturned.Bumper
    /// <summary>
    /// Transpiler for blocking players from exploding air assets.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(MethodBase method, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo getPlayerMethod = Accessor.GetMethod(DamageTool.getPlayer)!;

        FieldInfo? vehicleField = typeof(Bumper).GetField("vehicle",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        if (vehicleField == null)
        {
            return ctx.Fail(new FieldDefinition("vehicle")
                .DeclaredIn<Bumper>(isStatic: false)
                .WithFieldType<InteractableVehicle>()
            );
        }

        while (ctx.MoveNext())
        {
            // check for: Player player2 = DamageTool.getPlayer(other.transform);
            if (!ctx.Instruction.Calls(getPlayerMethod)
                || !ctx.MoveNext())
            {
                continue;
            }

            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(0)
                    .LoadInstanceFieldValue(vehicleField)
                    .AddLabel(out Label validLabel)
                    .Invoke(Accessor.GetMethod(OnHitPlayer)!)
                    .BranchIfTrue(validLabel)
                    .Return()
                    .MarkLabel(validLabel);
            });

            ctx.ApplyBlocksAndLabels();
        }

        return ctx;
    }

    private static bool OnHitPlayer(InteractableVehicle vehicle)
    {
        return vehicle.asset.engine is not (EEngine.BLIMP or EEngine.HELICOPTER or EEngine.PLANE);
    }
}