using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class ItemManagerDespawnItems : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(ItemManager).GetMethod("despawnItems", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for item despawned event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("despawnItems")
                .DeclaredIn<ItemManager>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for item despawned event.", _target);
        _target = null;
    }

    // SDG.Unturned.PlayerInventory
    /// <summary>
    /// Transpiler for <see cref="ItemManager.despawnItems"/> to invoke <see cref="ItemDestroyed"/> event.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? regionsProperty = typeof(ItemManager).GetProperty(nameof(ItemManager.regions), BindingFlags.Public | BindingFlags.Static)?.GetGetMethod(true);
        if (regionsProperty == null)
        {
            return ctx.Fail(new PropertyDefinition(nameof(ItemManager.regions))
                .DeclaredIn<ItemManager>(isStatic: true)
                .WithPropertyType<ItemRegion[,]>()
                .WithNoSetter()
            );
        }

        MethodInfo? instanceIdProperty = typeof(ItemData).GetProperty(nameof(ItemData.instanceID), BindingFlags.Public | BindingFlags.Static)?.GetGetMethod(true);
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
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(instanceIdProperty))
                continue;

            if (!ctx.MoveNext() || !ctx.Instruction.opcode.IsStLoc())
                break;

            LocalBuilder? instanceIdLclBldr = PatchUtility.GetLocal(ctx.Instruction, out int index, true);
            LocalReference instanceIdLocal = instanceIdLclBldr != null ? new LocalReference(instanceIdLclBldr) : new LocalReference(index);

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
            ItemUtility.InvokeOnItemDestroyed(item, true, false, CSteamID.Nil, 0, 0, 0, 0);
        }
    }
}
