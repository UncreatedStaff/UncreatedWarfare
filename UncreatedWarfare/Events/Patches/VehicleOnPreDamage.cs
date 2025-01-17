using DanielWillett.ReflectionTools.Formatting;
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players;
using HarmonyLib;
using System.Reflection.Emit;
using System.Linq;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Events.Models.Vehicles;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
public class VehicleOnPreDamage : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(VehicleManager).GetMethod(nameof(VehicleManager.damage), BindingFlags.Static | BindingFlags.Public);
        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for vehicle on pre-damaged event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("damage")
                .DeclaredIn<VehicleManager>(isStatic: true)
                .WithParameter<InteractableVehicle>("vehicle")
                .WithParameter<float>("damage")
                .WithParameter<float>("times")
                .WithParameter<bool>("canRepair")
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
        logger.LogDebug("Unpatched {0} for vehicle on pre-damaged event.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo askDamageMethod = typeof(InteractableVehicle).GetMethod("askDamage", BindingFlags.Public | BindingFlags.Instance);
        if (askDamageMethod == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find method: InteractableVehicle.askDamage");
        }

        bool patchDone = false;
        foreach (CodeInstruction instruction in instructions)
        {
            // strategy: insert the invoker call just before the call to vehicle.askDamage (near the end of the method)

            if (!patchDone && askDamageMethod != null && instruction.Calls(askDamageMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0); // method arg 0: vehicle
                yield return new CodeInstruction(OpCodes.Ldloc_0); // local variable 0: pendingDamage
                yield return new CodeInstruction(OpCodes.Ldarg_3); // method arg 3: canRepair
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4); // method arg 4: instigatorSteamID
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5); // method arg 5: damageOrigin
                yield return new CodeInstruction(OpCodes.Call, Accessor.GetMethod(PreVehicleDamageInvoker));
                WarfareModule.Singleton.GlobalLogger.LogInformation("Inserted PreVehicleDamageInvoker call into method VehicleManager.damage.");
                
                CodeInstruction old = new CodeInstruction(instruction); // make sure the original askDamage instruction is still called
                yield return old;

                patchDone = true;
                continue;
            }

            yield return instruction;
        }
    }
    private static void PreVehicleDamageInvoker(InteractableVehicle vehicle, ushort pendingDamage, bool canRepair, CSteamID instigatorId, EDamageOrigin damageOrigin)
    {
        VehicleService vehicleService = WarfareModule.Singleton.ServiceProvider.Resolve<VehicleService>();

        WarfareVehicle? warfareVehicle = vehicleService.GetVehicle(vehicle);
        if (warfareVehicle == null)
            return;

        WarfareVehicle? instigatorVehicle = null;
        WarfarePlayer? onlineInstigator = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>()?.GetOnlinePlayerOrNull(instigatorId);
        if (onlineInstigator != null && onlineInstigator.UnturnedPlayer.movement.getVehicle() != null)
            instigatorVehicle = onlineInstigator.UnturnedPlayer.movement.getVehicle().transform.GetComponent<WarfareVehicleComponent>().WarfareVehicle;

        // landmines get special treatment in VehicleDamageTrackerItemTweak
        if (damageOrigin != EDamageOrigin.Trap_Explosion)
        {
            if (onlineInstigator != null)
            {
                if (instigatorVehicle != null)
                    warfareVehicle.DamageTracker.RecordDamage(onlineInstigator, instigatorVehicle, pendingDamage, damageOrigin);
                else
                    warfareVehicle.DamageTracker.RecordDamage(onlineInstigator, pendingDamage, damageOrigin);
            }
            else if (instigatorId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
                warfareVehicle.DamageTracker.RecordDamage(instigatorId, pendingDamage, damageOrigin);
            else
                warfareVehicle.DamageTracker.RecordDamage(damageOrigin);
        }

        VehiclePreDamaged args = new VehiclePreDamaged
        {
            Vehicle = warfareVehicle,
            PendingDamage = pendingDamage,
            CanRepair = canRepair,
            InstantaneousInstigator = instigatorId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? instigatorId : null,
            LastKnownInstigator = warfareVehicle.DamageTracker.LastKnownDamageInstigator,
            InstantaneousDamageOrigin = damageOrigin
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, allowAsync: false);
    }
}
