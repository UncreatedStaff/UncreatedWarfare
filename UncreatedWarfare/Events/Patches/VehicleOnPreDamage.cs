using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
public class VehicleOnPreDamage : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(VehicleManager).GetMethod(nameof(VehicleManager.damage),
            BindingFlags.Static | BindingFlags.Public);
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

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for vehicle on pre-damaged event.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(MethodBase method, IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        MethodInfo askDamageMethod =
            typeof(InteractableVehicle).GetMethod("askDamage", BindingFlags.Public | BindingFlags.Instance);
        if (askDamageMethod == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find method: InteractableVehicle.askDamage");
        }

        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        while (ctx.MoveNext())
        {
            // find the call to VehicleManager.damage()
            if (!ctx.Instruction.Calls(askDamageMethod))
            {
                continue;
            }

            // move 3 instructions back before the arguments of VehicleManager.damage() are loaded onto the stack
            while (ctx.Instruction.opcode != OpCodes.Ldarg_0)
            {
                ctx.CaretIndex--;
            }

            // get local variable 0 which is the 'pendingDamage' variable
            LocalReference pendingDamageLocal = PatchUtil.GetLocal(ctx[ctx.CaretIndex + 1], false);


            ctx.EmitAbove(emit =>
            {
                emit.LoadArgument(0)
                    .LoadLocalAddress(pendingDamageLocal)
                    .LoadArgumentAddress(3)
                    .LoadArgument(4)
                    .LoadArgument(5)
                    .Invoke(Accessor.GetMethod(PreVehicleDamageInvoker)!);
            });
            
            break;
        }

        return ctx;
    }
    
    private static void PreVehicleDamageInvoker(InteractableVehicle vehicle, ref ushort pendingDamage, ref bool canRepair, CSteamID instigatorId, EDamageOrigin damageOrigin)
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

        pendingDamage = args.PendingDamage;
        canRepair = args.CanRepair;
    }
}
    