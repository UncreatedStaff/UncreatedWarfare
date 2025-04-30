using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

// ReSharper disable InconsistentNaming

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class TransformBuildablePatches : IHarmonyPatch
{
    private static MethodInfo? _barricadeTarget;
    private static MethodInfo? _structureTarget;

    private static uint _lastInstanceId;
    private static bool _lastIsBarricade;
    private static CSteamID _lastTransformRequest;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        BarricadeManager.onTransformRequested += TransformBarricadeRequested;
        StructureManager.onTransformRequested += TransformStructureRequested;

        _barricadeTarget = typeof(BarricadeDrop).GetMethod(nameof(BarricadeDrop.ReceiveTransform), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        _structureTarget = typeof(StructureDrop).GetMethod(nameof(StructureDrop.ReceiveTransform), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (_barricadeTarget != null)
        {
            patcher.Patch(_barricadeTarget, postfix: Accessor.GetMethod(BarricadePostfix));
            logger.LogDebug("Patched {0} for transform barricade event.", _barricadeTarget);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(BarricadeDrop.ReceiveTransform))
                    .DeclaredIn<BarricadeDrop>(isStatic: false)
                    .WithParameter<ClientInvocationContext>("context", ByRefTypeMode.In)
                    .WithParameter<byte>("old_x")
                    .WithParameter<byte>("old_y")
                    .WithParameter<ushort>("oldPlant")
                    .WithParameter<Vector3>("point")
                    .WithParameter<Quaternion>("rotation")
                    .ReturningVoid()
            );
        }
        if (_structureTarget != null)
        {
            patcher.Patch(_structureTarget, postfix: Accessor.GetMethod(StructurePostfix));
            logger.LogDebug("Patched {0} for transform structure event.", _structureTarget);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(StructureDrop.ReceiveTransform))
                    .DeclaredIn<StructureDrop>(isStatic: false)
                    .WithParameter<ClientInvocationContext>("context", ByRefTypeMode.In)
                    .WithParameter<byte>("old_x")
                    .WithParameter<byte>("old_y")
                    .WithParameter<Vector3>("point")
                    .WithParameter<Quaternion>("rotation")
                    .ReturningVoid()
            );
        }

    }

    private static void TransformStructureRequested(CSteamID instigator, byte x, byte y, uint instanceid, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldallow)
    {
        _lastInstanceId = instanceid;
        _lastIsBarricade = false;
        _lastTransformRequest = instigator;
    }

    private static void TransformBarricadeRequested(CSteamID instigator, byte x, byte y, ushort plant, uint instanceid, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldallow)
    {
        _lastInstanceId = instanceid;
        _lastIsBarricade = true;
        _lastTransformRequest = instigator;
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        BarricadeManager.onTransformRequested -= TransformBarricadeRequested;
        StructureManager.onTransformRequested -= TransformStructureRequested;

        if (_barricadeTarget != null)
        {
            patcher.Unpatch(_barricadeTarget, Accessor.GetMethod(BarricadePostfix));
            logger.LogDebug("Unpatched {0} for transform barricade event.", _barricadeTarget);
            _barricadeTarget = null;
        }

        if (_structureTarget == null)
            return;

        patcher.Unpatch(_structureTarget, Accessor.GetMethod(StructurePostfix));
        logger.LogDebug("Unpatched {0} for transform structure event.", _structureTarget);
        _structureTarget = null;
    }

    private static void BarricadePostfix(
        BarricadeDrop __instance,
        in ClientInvocationContext context,
        byte old_x,
        byte old_y,
        ushort oldPlant,
        Vector3 point,
        Quaternion rotation)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer? instigator = null;
        if (_lastIsBarricade && _lastInstanceId == __instance.instanceID)
        {
            instigator = playerService.GetOnlinePlayerOrNull(_lastTransformRequest);
        }

        _lastInstanceId = uint.MaxValue;

        BarricadeTransformed args = new BarricadeTransformed
        {
            Barricade = __instance,
            Buildable = new BuildableBarricade(__instance),
            Instigator = instigator
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }

    private static void StructurePostfix(
        StructureDrop __instance,
        in ClientInvocationContext context,
        byte old_x,
        byte old_y,
        Vector3 point,
        Quaternion rotation)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer? instigator = null;
        if (!_lastIsBarricade && _lastInstanceId == __instance.instanceID)
        {
            instigator = playerService.GetOnlinePlayerOrNull(_lastTransformRequest);
        }

        _lastInstanceId = uint.MaxValue;

        StructureTransformed args = new StructureTransformed
        {
            Structure = __instance,
            Buildable = new BuildableStructure(__instance),
            Instigator = instigator
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }

}