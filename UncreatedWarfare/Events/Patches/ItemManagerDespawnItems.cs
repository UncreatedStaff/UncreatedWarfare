using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class ItemManagerDespawnItems : IHarmonyPatch
{
    // these events replace item deletions in various
    // places with ItemUtility.RemoveDroppedItemUnsafe
    // to invoke the event
    private static MethodInfo? _target1;
    private static MethodInfo? _target2;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target1 = typeof(ItemManager).GetMethod("despawnItems", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        _target2 = typeof(ItemManager).GetMethod(nameof(ItemManager.ServerClearItemsInSphere), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (_target1 == null)
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("despawnItems")
                    .DeclaredIn<ItemManager>(isStatic: false)
                    .WithNoParameters()
                    .ReturningVoid()
            );
        }

        if (_target2 == null)
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(ItemManager.ServerClearItemsInSphere))
                    .DeclaredIn<ItemManager>(isStatic: true)
                    .WithParameter<Vector3>("center")
                    .WithParameter<float>("radius")
                    .ReturningVoid()
            );
        }

        if (_target1 != null)
        {
            patcher.Patch(_target1, transpiler: Accessor.GetMethod(TranspilerDespawnItems));
            logger.LogDebug("Patched {0} for item despawned event.", _target1);
        }
        if (_target2 != null)
        {
            patcher.Patch(_target2, prefix: Accessor.GetMethod(PrefixServerClearItemsInSphere));
            logger.LogDebug("Patched {0} for replacing ServerClearItemsInSphere method.", _target2);
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target1 != null)
        {
            patcher.Unpatch(_target1, Accessor.GetMethod(TranspilerDespawnItems));
            logger.LogDebug("Unpatched {0} for item despawned event.", _target1);
            _target1 = null;
        }

        if (_target2 != null)
        {
            patcher.Unpatch(_target2, Accessor.GetMethod(PrefixServerClearItemsInSphere));
            logger.LogDebug("Unpatched {0} for replacing ServerClearItemsInSphere method.", _target2);
            _target2 = null;
        }
    }

    private static IEnumerable<CodeInstruction> TranspilerDespawnItems(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? instanceIdProperty = typeof(ItemData).GetProperty(nameof(ItemData.instanceID), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(true);
        if (instanceIdProperty == null)
        {
            return ctx.Fail(new PropertyDefinition(nameof(ItemData.instanceID))
                .DeclaredIn<ItemData>(isStatic: false)
                .WithPropertyType<uint>()
                .WithNoSetter()
            );
        }

        FieldInfo? despawnX = typeof(ItemManager).GetField("despawnItems_X", BindingFlags.NonPublic | BindingFlags.Static),
                   despawnY = typeof(ItemManager).GetField("despawnItems_Y", BindingFlags.NonPublic | BindingFlags.Static);

        bool patched = false;
        Label lastLabel = default;
        while (ctx.MoveNext())
        {
            if (ctx.Instruction.operand is Label lbl)
                lastLabel = lbl;

            if (!ctx.Instruction.Calls(instanceIdProperty))
                continue;

            if (!ctx.MoveNext() || !ctx.Instruction.opcode.IsStLoc())
                continue;

            LocalReference instanceIdLocal = PatchUtil.GetLocal(ctx.Instruction, true);

            ctx.EmitBelow(emit =>
            {
                emit.LoadLocalValue(instanceIdLocal);

                if (despawnX != null && despawnY != null)
                {
                    emit.LoadStaticFieldValue(despawnX)
                        .LoadStaticFieldValue(despawnY);
                }
                else
                {
                    emit.LoadConstantUInt8(0)
                        .LoadConstantUInt8(0);
                }

                emit.Invoke(Accessor.GetMethod(RemoveItemInvoker)!);
                emit.Branch(lastLabel);
            });
            patched = true;
        }

        if (!patched)
        {
            ctx.LogWarning("Unable to patch item despawn.");
        }

        return ctx;
    }

    private static void RemoveItemInvoker(uint instanceId, byte maybeX, byte maybeY)
    {
        ItemInfo item = ItemUtility.FindItem(instanceId, maybeX, maybeY);
        if (item.HasValue)
        {
            RegionCoord coord = item.Coord;
            ItemUtility.RemoveDroppedItemUnsafe(coord.x, coord.y, item.Index, true, CSteamID.Nil, false, 0, 0, 0, 0);
        }
    }

    private static bool PrefixServerClearItemsInSphere(Vector3 center, float radius)
    {
        WarfareModule.Singleton.GlobalLogger.LogWarning("Something is using ItemManager.ServerClearItemsInSphere instead of ItemUtility.DestroyDroppedItemsInRange.");
        WarfareModule.Singleton.GlobalLogger.LogWarning("{0}", new StackTrace(1));
        ItemUtility.DestroyDroppedItemsInRange(center, radius, false);
        return false;
    }
}
