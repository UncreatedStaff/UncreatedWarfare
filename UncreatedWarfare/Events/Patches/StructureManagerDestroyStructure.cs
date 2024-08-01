using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Patches;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class StructureManagerDestroyStructure : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = Accessor.GetMethod(new Action<StructureDrop, byte, byte, Vector3, bool>(StructureManager.destroyStructure));

        if (_target != null)
        {
            Patcher.Patch(_target, transpiler: PatchUtil.GetMethodInfo(Transpiler));
            logger.LogDebug("Patched {0} for destroy structure event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition(nameof(StructureManager.destroyStructure))
                .DeclaredIn<StructureManager>(isStatic: true)
                .WithParameter<StructureDrop>("structure")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .WithParameter<Vector3>("ragdoll")
                .WithParameter<bool>("wasPickedUp")
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, PatchUtil.GetMethodInfo(Transpiler));
        logger.LogDebug("Unpatched {0} for destroy structure event.", Accessor.Formatter.Format(_target));
        _target = null;
    }

    // SDG.Unturned.StructureManager
    /// <summary>
    /// Transpiler for <see cref="StructureManager.destroyStructure(StructureDrop, byte, byte, Vector3, bool)"/> to invoke <see cref="StructureDestroyed"/>.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo? rpc = typeof(StructureManager).GetField("SendDestroyStructure", BindingFlags.NonPublic | BindingFlags.Static);
        if (rpc == null)
        {
            L.LogWarning("Unable to find field: StructureManager.SendDestroyStructure");
        }

        bool one = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!one && rpc != null && instruction.LoadsField(rpc))
            {
                CodeInstruction call = new CodeInstruction(OpCodes.Ldarg_0);
                call.labels.AddRange(instruction.labels);
                yield return call;
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_3);
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
                yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(DestroyStructureInvoker));
                L.LogDebug("Inserted DestroyStructureInvoker call to StructureManager.destroyStructure.");
                CodeInstruction old = new CodeInstruction(instruction);
                old.labels.Clear();
                yield return old;
                one = true;
                continue;
            }

            yield return instruction;
        }
    }

    private static void DestroyStructureInvoker(StructureDrop structure, byte x, byte y, Vector3 ragdoll, bool wasPickedUp)
    {
        if (structure == null)
            return;

        if (!Regions.checkSafe(x, y))
            return;

        StructureRegion region = StructureManager.regions[x, y];

        ulong destroyer;
        EDamageOrigin origin = EDamageOrigin.Unknown;
        if (structure.model.TryGetComponent(out DestroyerComponent comp))
        {
            destroyer = comp.Destroyer;
            float time = comp.RelevantTime;
            if (destroyer != 0 && Time.realtimeSinceStartup - time > 1f)
                destroyer = 0ul;
            else origin = comp.DamageOrigin;
            Object.Destroy(comp);
        }
        else
        {
            destroyer = 0ul;
        }

        UCPlayer? player = UCPlayer.FromID(destroyer);

        StructureDestroyed args = new StructureDestroyed
        {
            Instigator = player,
            Region = region,
            Structure = structure,
            ServersideData = structure.GetServersideData(),
            InstanceId = structure.instanceID,
            RegionPosition = new RegionCoord(x, y),
            DamageOrigin = origin,
            InstigatorId = new CSteamID(destroyer)
            // todo Primary and Secondary assets need filling
        };

        structure.model.GetComponents(WarfareModule.EventDispatcher.WorkingDestroyInfo);
        try
        {
            foreach (IDestroyInfo destroyInfo in WarfareModule.EventDispatcher.WorkingDestroyInfo)
            {
                destroyInfo.DestroyInfo = args;
            }
        }
        finally
        {
            WarfareModule.EventDispatcher.WorkingDestroyInfo.Clear();
        }

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
