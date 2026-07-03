using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class StructureManagerSendHealthChanged : IHarmonyPatch
{
    private static MethodInfo? _target;

    internal static DamageStructureRequested? LastDamageRequestedEvent;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(StructureManager).GetMethod("sendHealthChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (_target != null)
        {
            patcher.Patch(_target, postfix: Accessor.GetMethod(StructurePostfix));
            logger.LogDebug($"Patched {_target} for listening for post-structure damage.");
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("sendHealthChanged")
                    .DeclaredIn<StructureManager>(isStatic: false)
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .WithParameter<StructureDrop>("structure")
                    .ReturningVoid()
            );
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(StructurePostfix));
        logger.LogDebug($"Unpatched {_target} for listening for post-structure damage.");
        _target = null;
    }

    private static void StructurePostfix(byte x, byte y, StructureDrop structure)
    {
        DamageStructureRequested? reqArgs = LastDamageRequestedEvent;
        if (reqArgs == null || reqArgs.Structure != structure)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"[StructureManagerSendHealthChanged] Unable to identify damage requested event for barricade {structure.asset}.");
            return;
        }

        StructureDamaged args = new StructureDamaged
        {
            ServersideData = reqArgs.ServersideData,
            Structure = reqArgs.Structure,
            DamageOrigin = reqArgs.DamageOrigin,
            PrimaryAsset = reqArgs.PrimaryAsset,
            SecondaryAsset = reqArgs.SecondaryAsset,
            Region = reqArgs.Region,
            Direction = reqArgs.Direction,
            Damage = reqArgs.PendingDamage,
            InstanceId = reqArgs.InstanceId,
            Instigator = reqArgs.Instigator,
            InstigatorId = reqArgs.InstigatorId,
            InstigatorTeam = reqArgs.InstigatorTeam,
            RegionIndex = reqArgs.RegionIndex,
            RegionPosition = reqArgs.RegionPosition
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}