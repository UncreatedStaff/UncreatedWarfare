using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Util;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class ItemManagerDespawnItems : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(ItemManager).GetMethod("despawnItems", BindingFlags.Public | BindingFlags.Instance);

        if (_target != null)
        {
            Patcher.Patch(_target, transpiler: PatchUtil.GetMethodInfo(Transpiler));
            logger.LogDebug("Patched {0} for item despawned event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition("despawnItems")
                .DeclaredIn<ItemManager>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, PatchUtil.GetMethodInfo(Transpiler));
        logger.LogDebug("Unpatched {0} for item despawned event.", Accessor.Formatter.Format(_target));
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

        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(instanceIdProperty))
                continue;

            if (!ctx.MoveNext() || !ctx.Instruction.opcode.IsStLoc())
                break;

            LocalBuilder? instanceIdLcl = PatchUtility.GetLocal(ctx.Instruction, out int index, true);

            if (!ctx.MoveNext())
                break;

            // load instance id
            if (instanceIdLcl == null)
            {
                ctx.Emit(OpCodes.Ldloc_S, (byte)index);
            }
            else
            {
                ctx.Emit(OpCodes.Ldloc, instanceIdLcl);
            }

            if (despawnX != null && despawnY != null)
            {
                ctx.Emit(OpCodes.Ldsfld, despawnX);
                ctx.Emit(OpCodes.Ldsfld, despawnY);
            }
            else
            {
                ctx.Emit(OpCodes.Ldc_I4_0);
                ctx.Emit(OpCodes.Conv_I1);

                ctx.Emit(OpCodes.Ldc_I4_0);
                ctx.Emit(OpCodes.Conv_I1);
            }

            ctx.Emit(OpCodes.Call, Accessor.GetMethod(RemoveItemInvoker)!);
        }

        return ctx;
    }

    private static void RemoveItemInvoker(uint instanceId, byte maybeX, byte maybeY)
    {
        ItemInfo item = ItemUtility.FindItem(instanceId, maybeX, maybeY);
        if (item.HasValue)
        {
            ItemUtility.InvokeOnItemDestroyed(item, true, false, CSteamID.Nil);
        }
    }
}
