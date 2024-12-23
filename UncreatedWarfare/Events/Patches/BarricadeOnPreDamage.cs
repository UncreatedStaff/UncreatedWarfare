using DanielWillett.ReflectionTools.Formatting;
using DanielWillett.ReflectionTools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Buildables;

namespace Uncreated.Warfare.Events.Patches;
[UsedImplicitly]
public class BarricadeOnPreDamage : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
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

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for barricade on pre-damaged event.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo askDamageMethod = typeof(Barricade).GetMethod("askDamage", BindingFlags.Public | BindingFlags.Instance);
        if (askDamageMethod == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find method: Barricade.askDamage");
        }

        bool patchDone = false;
        foreach (CodeInstruction instruction in instructions)
        {
            // strategy: insert the invoker call just before the call to barricade.askDamage

            if (!patchDone && askDamageMethod != null && instruction.Calls(askDamageMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4); // local variable 4: barricadeByRootTransform
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)6); // local variable 6: pendingDamage
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4); // method arg 4: instigatorSteamID
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5); // method arg 5: damageOrigin
                yield return new CodeInstruction(OpCodes.Call, Accessor.GetMethod(PreBarricadeDamageInvoker));
                WarfareModule.Singleton.GlobalLogger.LogInformation("Inserted PreBarricadeDamageInvoker call into method VehicleManager.damage.");

                CodeInstruction old = new CodeInstruction(instruction); // make sure the original askDamage instruction is still called
                yield return old;

                patchDone = true;
                continue;
            }

            yield return instruction;
        }
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

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
