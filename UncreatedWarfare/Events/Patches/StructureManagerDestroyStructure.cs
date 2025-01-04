using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class StructureManagerDestroyStructure : IHarmonyPatch
{
    private static MethodInfo? _target;
    private static readonly List<IManualOnDestroy> DestroyEventComponents = new List<IManualOnDestroy>(4);

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = Accessor.GetMethod(new Action<StructureDrop, byte, byte, Vector3, bool>(StructureManager.destroyStructure));

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for destroy structure event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(StructureManager.destroyStructure))
                .DeclaredIn<StructureManager>(isStatic: true)
                .WithParameter<StructureDrop>("structure")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .WithParameter<Vector3>("ragdoll")
                .WithParameter<bool>("wasPickedUp")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for destroy structure event.", _target);
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
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find field: StructureManager.SendDestroyStructure");
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
                yield return new CodeInstruction(OpCodes.Call, Accessor.GetMethod(DestroyStructureInvoker));
                WarfareModule.Singleton.GlobalLogger.LogDebug("Inserted DestroyStructureInvoker call to StructureManager.destroyStructure.");
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
        bool wasSalvaged = false;
        if (structure.model.TryGetComponent(out DestroyerComponent comp))
        {
            destroyer = comp.Destroyer;
            float time = comp.RelevantTime;
            if (destroyer != 0 && Time.realtimeSinceStartup - time > 1f)
                destroyer = 0ul;
            else
            {
                origin = comp.DamageOrigin;
                wasSalvaged = comp.Salvaged;
            }
            Object.Destroy(comp);
        }
        else
        {
            destroyer = 0ul;
        }

        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer? player = playerService.GetOnlinePlayerOrNull(destroyer);

        StructureDestroyed args = new StructureDestroyed
        {
            Instigator = player,
            Region = region,
            Structure = structure,
            ServersideData = structure.GetServersideData(),
            InstanceId = structure.instanceID,
            RegionPosition = new RegionCoord(x, y),
            DamageOrigin = origin,
            InstigatorId = new CSteamID(destroyer),
            WasSalvaged = wasSalvaged,
            // todo Primary and Secondary assets need filling
            InstigatorTeam = player?.Team ?? Team.NoTeam
        };

        BuildableExtensions.SetDestroyInfo(structure.model, args, null);

        try
        {
            structure.model.GetComponents(DestroyEventComponents);
            ILogger? logger = null;
            foreach (IManualOnDestroy eventHandler in DestroyEventComponents)
            {
                try
                {
                    eventHandler.ManualOnDestroy();
                }
                catch (Exception ex)
                {
                    (logger ??= WarfareModule.Singleton.ServiceProvider.Resolve<ILogger<StructureManagerDestroyStructure>>())
                        .LogError(ex, "Error dispatching {0} in type {1}.", typeof(IManualOnDestroy), eventHandler.GetType());
                }
            }
        }
        finally
        {
            DestroyEventComponents.Clear();
        }

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
