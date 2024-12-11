using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class PlayerInventoryReceiveDragOrSwapItem : IHarmonyPatch
{
    private static bool _ignoreCall;

    private static MethodInfo? _removeItemMtd;
    private static MethodInfo? _addItemMtd;
    private static MethodInfo? _getItemMtd;

    private static MethodInfo? _targetDrag;
    private static MethodInfo? _targetSwap;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _removeItemMtd = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.removeItem), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _addItemMtd = typeof(Items).GetMethod(nameof(Items.addItem), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _getItemMtd = typeof(Items).GetMethod(nameof(Items.getItem), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (_removeItemMtd == null)
        {
            logger.LogError("Unable to find {0} to transpile drag and swap item events.",
                new MethodDefinition(nameof(PlayerInventory.removeItem))
                    .DeclaredIn<PlayerInventory>(isStatic: false)
                    .WithParameter<byte>("page")
                    .WithParameter<byte>("index")
                    .ReturningVoid()
            );
        }
        if (_addItemMtd == null)
        {
            logger.LogError("Unable to find {0} to transpile drag and swap item events.",
                new MethodDefinition(nameof(Items.addItem))
                    .DeclaredIn<Items>(isStatic: false)
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .WithParameter<byte>("rot")
                    .WithParameter<Item>("item")
                    .ReturningVoid()
            );
        }
        if (_getItemMtd == null)
        {
            logger.LogError("Unable to find {0} to transpile drag item events.",
                new MethodDefinition(nameof(Items.getItem))
                    .DeclaredIn<Items>(isStatic: false)
                    .WithParameter<byte>("index")
                    .Returning<ItemJar>()
            );
        }

        if (_addItemMtd == null || _removeItemMtd == null)
        {
            return;
        }

        _targetDrag = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveDragItem), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _targetSwap = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveSwapItem), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_targetDrag != null)
        {
            patcher.Patch(_targetDrag, transpiler: Accessor.GetMethod(DragTranspiler));
            logger.LogDebug("Patched {0} for drag item event.", _targetDrag);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(PlayerInventory.ReceiveDragItem))
                    .DeclaredIn<PlayerInventory>(isStatic: false)
                    .WithParameter<byte>("page_0")
                    .WithParameter<byte>("x_0")
                    .WithParameter<byte>("y_0")
                    .WithParameter<byte>("page_1")
                    .WithParameter<byte>("x_1")
                    .WithParameter<byte>("y_1")
                    .WithParameter<byte>("rot_1")
                    .ReturningVoid()
            );
        }
        if (_targetSwap != null)
        {
            patcher.Patch(_targetSwap, transpiler: Accessor.GetMethod(SwapTranspiler));
            logger.LogDebug("Patched {0} for swap items event.", _targetSwap);
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(PlayerInventory.ReceiveSwapItem))
                    .DeclaredIn<PlayerInventory>(isStatic: false)
                    .WithParameter<byte>("page_0")
                    .WithParameter<byte>("x_0")
                    .WithParameter<byte>("y_0")
                    .WithParameter<byte>("rot_0")
                    .WithParameter<byte>("page_1")
                    .WithParameter<byte>("x_1")
                    .WithParameter<byte>("y_1")
                    .WithParameter<byte>("rot_1")
                    .ReturningVoid()
            );
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_targetDrag != null)
        {
            patcher.Unpatch(_targetDrag, Accessor.GetMethod(DragTranspiler));
            logger.LogDebug("Unpatched {0} for drag item event.", _targetDrag);
            _targetDrag = null;
        }

        if (_targetSwap == null)
            return;

        patcher.Unpatch(_targetSwap, Accessor.GetMethod(SwapTranspiler));
        logger.LogDebug("Unpatched {0} for swap items event.", _targetSwap);
        _targetSwap = null;
    }

    // SDG.Unturned.PlayerInventory
    /// <summary>
    /// Transpiler for <see cref="PlayerInventory.ReceiveDragItem"/> or <see cref="PlayerInventory.ReceiveSwapItem"/> to invoke item dragged and swapped events.
    /// </summary>
    private static IEnumerable<CodeInstruction> TranspileReceiveSwapOrDragItem(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method, bool swap)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        int extraAddItemCalls = swap ? 1 : 0;
        int postArg3Offset = swap ? 1 : 0;
        int removePatches = 0;
        while (ctx.MoveNext())
        {
            if (ctx.Instruction.Calls(_addItemMtd))
            {
                // swap has two addItem calls, need to wait for the second one
                if (--extraAddItemCalls >= 0)
                    continue;

                ctx.EmitBelow(emit =>
                {
                    emit.AddLocal<byte>(out LocalBuilder lclRotation);

                    bool setLclRotation = swap;
                    if (swap)
                    {
                        emit.LoadArgument(/* rot_0 */ 4)
                            .SetLocalValue(lclRotation);
                    }

                    if (!swap && _getItemMtd != null)
                    {
                        // find stloc after ItemJar jar = getItem to get old rotation
                        CodeInstruction? setter = ctx.Where((x, index) => x.IsStloc() && index > 0 && ctx[index - 1].Calls(_getItemMtd)).FirstOrDefault();
                        if (setter != null)
                        {
                            LocalBuilder? lcl = PatchUtility.GetLocal(setter, out int index, true);
                            emit.LoadLocalValue(lcl == null ? new LocalReference(index) : new LocalReference(lcl))
                                .LoadFieldValue((ItemJar j) => j.rot)
                                .SetLocalValue(lclRotation);
                            setLclRotation = true;
                        }
                    }

                    if (!setLclRotation)
                    {
                        emit.LoadConstantUInt8(0)
                            .SetLocalValue(lclRotation);
                    }

                    emit.LoadArgument(/* this */   0)
                        .LoadArgument(/* page_0 */ 1)
                        .LoadArgument(/* page_1 */ 4 + postArg3Offset)
                        .LoadArgument(/* x_0 */    2)
                        .LoadArgument(/* x_1 */    5 + postArg3Offset)
                        .LoadArgument(/* y_0 */    3)
                        .LoadArgument(/* y_1 */    6 + postArg3Offset)
                        .LoadLocalValue(lclRotation)
                        .LoadArgument(/* rot_1 */  7 + postArg3Offset)
                        .LoadConstantBoolean(swap)
                        
                        .Invoke(Accessor.GetMethod(DraggedOrSwappedItemInvoker)!);
                });
            }
            else if (removePatches == 0 && ctx.Instruction.opcode == OpCodes.Ldarg_0 && ctx.Count > ctx.CaretIndex + 3 && ctx[ctx.CaretIndex + 3].Calls(_removeItemMtd))
            {
                ++removePatches;
                ctx.EmitAbove(emit =>
                {
                    emit.AddLabel(out Label label)
                        .LoadFieldValue(() => _ignoreCall)
                        .BranchIfTrue(label)

                        .LoadArgument(/* this */   0)
                        .LoadArgument(/* page_0 */ 1)
                        .LoadArgument(/* page_1 */ 4 + postArg3Offset)
                        .LoadArgument(/* x_0 */    2)
                        .LoadArgument(/* x_1 */    5 + postArg3Offset)
                        .LoadArgument(/* y_0 */    3)
                        .LoadArgument(/* y_1 */    6 + postArg3Offset)
                        .LoadArgument(/* rot_1 */  7 + postArg3Offset)
                        .LoadConstantBoolean(swap)
                        
                        .Invoke(Accessor.GetMethod(DraggedOrSwappedItemRequestedInvoker)!)
                        .BranchIfTrue(label)
                        .Return()

                        .MarkLabel(label);
                });

                ctx.ApplyBlocksAndLabels();
            }
        }

        if (extraAddItemCalls >= 0)
        {
            ctx.LogWarning($"Failed to patch final invoker ({(swap ? "Swap" : "Drag")}.");
        }

        if (removePatches == 0)
        {
            ctx.LogWarning($"Failed to patch requested invoker ({(swap ? "Swap" : "Drag")}.");
        }

        return ctx;
    }

    private static IEnumerable<CodeInstruction> SwapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        => TranspileReceiveSwapOrDragItem(instructions, generator, method, true);

    private static IEnumerable<CodeInstruction> DragTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        => TranspileReceiveSwapOrDragItem(instructions, generator, method, false);

    private static readonly Lazy<ILogger> SwapLoggerGetter = new Lazy<ILogger>(() => WarfareModule.EventDispatcher.GetLogger(typeof(PlayerInventory), "ReceiveSwapItem"), LazyThreadSafetyMode.None);
    private static readonly Lazy<ILogger> DragLoggerGetter = new Lazy<ILogger>(() => WarfareModule.EventDispatcher.GetLogger(typeof(PlayerInventory), "ReceiveDragItem"), LazyThreadSafetyMode.None);

    internal static bool DraggedOrSwappedItemRequestedInvoker(PlayerInventory playerInv, byte pageFrom, byte pageTo, byte xFrom, byte xTo, byte yFrom, byte yTo, byte rotTo, bool swap)
    {
        ILifetimeScope lifetimeScope = WarfareModule.Singleton.ServiceProvider;

        EventDispatcher eventDispatcher = lifetimeScope.Resolve<EventDispatcher>();
        IPlayerService playerService = lifetimeScope.Resolve<IPlayerService>();

        Lazy<ILogger> logger = swap ? SwapLoggerGetter : DragLoggerGetter;

        WarfarePlayer player = playerService.GetOnlinePlayer(playerInv);

        ItemJar? existingJar = playerInv.getItem(pageFrom, playerInv.getIndex(pageFrom, xFrom, yFrom));
        if (existingJar == null)
        {
            logger.Value.LogWarning("Item not found from request.");
            return false;
        }

        ItemJar? swapJar = null;
        if (swap)
        {
            swapJar = playerInv.getItem(pageTo, playerInv.getIndex(pageTo, xTo, yTo));
            if (swapJar == null)
            {
                logger.Value.LogWarning("Swap item not found from request.");
                return false;
            }
        }

        ItemMoveRequested args = new ItemMoveRequested
        {
            Player = player,
            IsSwap = swap,
            Jar = existingJar,
            NewPage = (Page)pageTo,
            OldPage = (Page)pageFrom,
            NewRotation = pageTo < PlayerInventory.SLOTS ? (byte)0 : rotTo,
            OldRotation = existingJar.rot,
            NewX = xTo,
            OldX = xFrom,
            NewY = yTo,
            OldY = yFrom,
            SwappingJar = swapJar
        };

        EventContinuations.Dispatch(args, eventDispatcher, player.DisconnectToken, out bool shouldAllow, Continuation);
        
        if (!shouldAllow)
            return false;

        if (args.NewX == xTo && args.NewY == yTo && (byte)args.NewPage == pageTo && args.NewRotation == rotTo)
            return true;

        Continuation(args);
        return false;

        static void Continuation(ItemMoveRequested args)
        {
            if (!args.Player.IsOnline)
                return;

            PlayerInventory playerInv = args.Inventory;
            ItemJar? newSwapJar = playerInv.getItem((byte)args.NewPage, playerInv.getIndex((byte)args.NewPage, args.NewX, args.NewY));
            bool swap = newSwapJar == null;

            _ignoreCall = true;
            try
            {
                if (swap)
                    playerInv.ReceiveSwapItem((byte)args.OldPage, args.OldX, args.OldY, args.OldRotation, (byte)args.NewPage, args.NewX, args.NewY, args.NewRotation);
                else
                    playerInv.ReceiveDragItem((byte)args.OldPage, args.OldX, args.OldY, (byte)args.NewPage, args.NewX, args.NewY, args.NewRotation);
            }
            finally
            {
                _ignoreCall = false;
            }
        }
    }
    internal static void DraggedOrSwappedItemInvoker(PlayerInventory playerInv, byte pageFrom, byte pageTo, byte xFrom, byte xTo, byte yFrom, byte yTo, byte rotFrom, byte rotTo, bool swap)
    {
        ILifetimeScope lifetimeScope = WarfareModule.Singleton.ServiceProvider;

        EventDispatcher eventDispatcher = lifetimeScope.Resolve<EventDispatcher>();
        IPlayerService playerService = lifetimeScope.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(playerInv);

        Lazy<ILogger> logger = swap ? SwapLoggerGetter : DragLoggerGetter;

        ItemJar? existingJar = playerInv.getItem(pageFrom, playerInv.getIndex(pageFrom, xFrom, yFrom));
        if (existingJar == null)
        {
            logger.Value.LogWarning("Item not found.");
            return;
        }

        ItemJar? swapJar = null;
        if (swap)
        {
            swapJar = playerInv.getItem(pageTo, playerInv.getIndex(pageTo, xTo, yTo));
            if (swapJar == null)
            {
                logger.Value.LogWarning("Swap item not found.");
                return;
            }
        }

        ItemMoved args = new ItemMoved
        {
            Player = player,
            NewPage = (Page)pageTo,
            NewX = xTo,
            NewY = yTo,
            NewRotation = existingJar.rot,
            IsSwap = swap,
            SwappedJar = swapJar,
            Jar = existingJar,
            OldPage = (Page)pageFrom,
            OldRotation = swapJar?.rot ?? rotFrom,
            OldX = xFrom,
            OldY = yFrom
        };

        _ = eventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }
}