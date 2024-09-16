using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util.Region;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class PlayerInventoryReceiveDropItem : IHarmonyPatch
{
    internal static bool LastPlayEffect;
    internal static bool LastIsDropped;
    internal static bool LastWideSpread;

    private static MethodInfo? _target;
    private static MethodInfo? _removeItemMtd;

    private static readonly FieldInfo LastPlayEffectField = typeof(PlayerInventoryReceiveDropItem).GetField(nameof(LastPlayEffect), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
    private static readonly FieldInfo LastIsDroppedField = typeof(PlayerInventoryReceiveDropItem).GetField(nameof(LastIsDropped), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
    private static readonly FieldInfo LastWideSpreadField = typeof(PlayerInventoryReceiveDropItem).GetField(nameof(LastWideSpread), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
    void IHarmonyPatch.Patch(ILogger logger)
    {
        _removeItemMtd = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.removeItem));
        if (_removeItemMtd == null)
        {
            logger.LogError("Unable to find {0} to transpile drop item event.",
                Accessor.Formatter.Format(new MethodDefinition(nameof(PlayerInventory.removeItem))
                    .DeclaredIn<PlayerInventory>(isStatic: false)
                    .WithParameter<byte>("page")
                    .WithParameter<byte>("index")
                    .ReturningVoid()
                )
            );
            return;
        }

        _target = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveDropItem), BindingFlags.Public | BindingFlags.Instance);

        if (_target != null)
        {
            Patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for drop item event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition(nameof(PlayerInventory.ReceiveDropItem))
                .DeclaredIn<PlayerInventory>(isStatic: false)
                .WithParameter<byte>("page")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for drop item event.", Accessor.Formatter.Format(_target));
        _target = null;
    }

    // SDG.Unturned.PlayerInventory
    /// <summary>
    /// Transpiler for <see cref="PlayerInventory.ReceiveDropItem"/> to invoke <see cref="ItemDropped"/> event.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
    {
        int lcl2 = method.FindLocalOfType<ItemJar>();
        if (lcl2 < 0)
            L.LogWarning("Unable to find local for ItemJar while transpiling ReceiveDropItem.");

        MethodInfo? itemGetter = typeof(ItemJar).GetProperty(nameof(ItemJar.item), BindingFlags.Public | BindingFlags.Instance)?.GetMethod;
        if (itemGetter == null)
            L.LogWarning("Unable to find 'get ItemJar.item' while transpiling ReceiveDropItem.");

        FieldInfo? rotField = typeof(ItemJar).GetField(nameof(ItemJar.rot), BindingFlags.Public | BindingFlags.Instance);
        if (rotField == null)
            L.LogWarning("Unable to find 'ItemJar.rot' while transpiling ReceiveDropItem.");

        yield return new CodeInstruction(OpCodes.Ldarg_3);
        yield return new CodeInstruction(OpCodes.Stsfld, LastPlayEffectField);
        
        yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
        yield return new CodeInstruction(OpCodes.Stsfld, LastIsDroppedField);
        
        yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5);
        yield return new CodeInstruction(OpCodes.Stsfld, LastWideSpreadField);

        bool foundOne = false;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (!foundOne && instruction.Calls(_removeItemMtd!))
            {
                foundOne = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);                 // this
                yield return new CodeInstruction(OpCodes.Ldarg_1);                 // page
                yield return new CodeInstruction(OpCodes.Ldarg_2);                 // x
                yield return new CodeInstruction(OpCodes.Ldarg_3);                 // y
                if (lcl2 > -1)
                {
                    if (rotField != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2); // itemjar local
                        yield return new CodeInstruction(OpCodes.Ldfld, rotField);    // .rot
                    }
                    else
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);           // load 0 as backup

                    if (itemGetter != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2); // itemjar local
                        yield return new CodeInstruction(OpCodes.Callvirt, itemGetter); // .item
                    }
                    else
                        yield return new CodeInstruction(OpCodes.Ldnull);           // load null as backup
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);               // load 0 as backup
                    yield return new CodeInstruction(OpCodes.Ldnull);               // load null as backup
                }

                yield return new CodeInstruction(OpCodes.Call, Accessor.GetMethod(DroppedItemInvoker));
                L.LogDebug("Patched ReceiveDropItem.");
            }
        }
    }

    private static void DroppedItemInvoker(PlayerInventory playerInv, byte page, byte x, byte y, byte rot, Item? item)
    {
        IServiceProvider serviceProvider = WarfareModule.Singleton.ServiceProvider;

        IPlayerService playerService = serviceProvider.GetRequiredService<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(playerInv);

        ItemData? data = null;
        ushort index = ushort.MaxValue;
        RegionCoord region = new RegionCoord((byte)(Regions.WORLD_SIZE / 2), (byte)(Regions.WORLD_SIZE / 2));
        if (ItemManager.regions != null && item != null)
        {
            foreach (RegionCoord reg in RegionUtility.EnumerateRegions(player.Position))
            {
                ItemRegion r = ItemManager.regions[reg.x, reg.y];
                for (int i = Math.Min(r.items.Count - 1, ushort.MaxValue); i >= 0; --i)
                {
                    if (!ReferenceEquals(r.items[i].item, item))
                        continue;

                    data = r.items[i];
                    index = (ushort)i;
                    region = reg;
                    break;
                }

                if (data != null)
                    break;
            }
        }

        ItemDropped args = new ItemDropped
        {
            Player = player,
            Region = ItemManager.regions?[region.x, region.y],
            RegionPosition = region,
            Index = index,
            Item = item,
            DroppedItem = data,
            OldPage = (Page)page,
            OldX = x,
            OldY = y,
            OldRotation = rot
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, WarfareModule.Singleton.UnloadToken);
    }
}
