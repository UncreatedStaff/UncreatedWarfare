using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;

namespace Uncreated.Warfare.Patches;
internal sealed class StopPlayerExplodeAirAssetsPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
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

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
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

            LocalReference hitPlayerLocal = PatchUtil.GetLocal(ctx.Instruction, true);

            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(0)
                    .LoadInstanceFieldValue(vehicleField)
                    .LoadLocalValue(hitPlayerLocal)
                    .Invoke(Accessor.GetMethod(OnHitPlayer)!)
                    .AddLabel(out Label validLabel)
                    .BranchIfTrue(validLabel)
                    .Return()
                    .MarkLabel(validLabel);
            });

            ctx.ApplyBlocksAndLabels();
        }

        return ctx;
    }

    private static bool OnHitPlayer(InteractableVehicle vehicle, Player hitPlayer)
    {
        if (hitPlayer == null)
            return false;

        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(hitPlayer);
        comp.LastRoadkillVehicle = AssetLink.Create(vehicle.asset);

        return vehicle.asset.engine is not (EEngine.BLIMP or EEngine.HELICOPTER or EEngine.PLANE)
               || vehicle.ReplicatedSpeed > 10f;
    }
}