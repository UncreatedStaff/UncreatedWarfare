using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits.Items;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Events;
internal static class EventPatches
{
    private static bool _fail;
    internal static void TryPatchAll()
    {
        _fail = false;
        PatchUtil.PatchMethod(typeof(InteractableCharge).GetMethod("detonate", BindingFlags.Instance | BindingFlags.Public), ref _fail,
            prefix: PatchUtil.GetMethodInfo(PreDetonate), postfix: PatchUtil.GetMethodInfo(PostDetonate));

        PatchUtil.PatchMethod(typeof(InteractableVehicle).GetMethod("explode", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            prefix: PatchUtil.GetMethodInfo(ExplodeVehicle));

        PatchUtil.PatchMethod(typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            prefix: PatchUtil.GetMethodInfo(RocketOnTriggerEnter));

        PatchUtil.PatchMethod(typeof(PlayerLife).GetMethod("doDamage", BindingFlags.NonPublic | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(PlayerDamageRequested));

        if (MthdRemoveItem != null)
        {
            if (MthdAddItem != null)
            {
                PatchUtil.PatchMethod(typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveDragItem), BindingFlags.Public | BindingFlags.Instance), ref _fail,
                    transpiler: PatchUtil.GetMethodInfo(TranspileReceiveDragItem));

                PatchUtil.PatchMethod(typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveSwapItem), BindingFlags.Public | BindingFlags.Instance), ref _fail,
                    transpiler: PatchUtil.GetMethodInfo(TranspileReceiveSwapItem));
            }
            else
            {
                L.LogError("Unable to find Items.addItem to transpile player inventory events.");
            }
        }
        else
        {
            L.LogError("Unable to find PlayerInventory.removeItem to transpile player inventory events.");
        }

        PatchUtil.PatchMethod(typeof(ItemManager).GetMethod(nameof(ItemManager.ReceiveTakeItemRequest), BindingFlags.Public | BindingFlags.Static), ref _fail,
            transpiler: PatchUtil.GetMethodInfo(TranspileReceiveTakeItemRequest));

        if (!PatchUtil.PatchMethod(typeof(InteractablePower).GetMethod("CalculateIsConnectedToPower", BindingFlags.NonPublic | BindingFlags.Instance), prefix: PatchUtil.GetMethodInfo(OnCalculatingPower)))
            Data.UseElectricalGrid = false;

        PatchUtil.PatchMethod(typeof(ObjectManager).GetMethod(nameof(ObjectManager.ReceiveToggleObjectBinaryStateRequest), BindingFlags.Public | BindingFlags.Static), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveToggleObjectBinaryStateRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapShirtRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapShirtRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapPantsRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapPantsRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapHatRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapHatRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapBackpackRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapBackpackRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapVestRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapVestRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapMaskRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapMaskRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapGlassesRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapGlassesRequest));
    }

    // SDG.Unturned.InteractableCharge.detonate
    /// <summary>
    /// Prefix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void PreDetonate(CSteamID killer, InteractableCharge __instance)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player != null && player.TryGetPlayerData(out UCPlayerData data))
        {
            BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
            if (drop != null)
                data.LastChargeDetonated = drop.asset.GUID;
        }
    }
    // SDG.Unturned.InteractableCharge.detonate
    /// <summary>
    /// Postfix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void PostDetonate(CSteamID killer)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player != null && player.TryGetPlayerData(out UCPlayerData data))
            data.LastChargeDetonated = default;
    }
    // SDG.Unturned.InteractableVehicle.explode
    /// <summary>
    /// Overriding prefix of <see cref="InteractableVehicle.explode"/> to set an instigator.
    /// </summary>
    private static bool ExplodeVehicle(InteractableVehicle __instance)
    {
        if (!__instance.TryGetComponent(out VehicleComponent vehicleData))
            return true;
        EDamageOrigin lastDamageType = vehicleData.LastDamageOrigin;
        if (lastDamageType == EDamageOrigin.Unknown) return true;
        CSteamID instigator2;
        switch (lastDamageType)
        {
            // no one at fault
            default:
            case EDamageOrigin.VehicleDecay:
                instigator2 = CSteamID.Nil;
                break;
            // blame driver
            case EDamageOrigin.Vehicle_Collision_Self_Damage:
            case EDamageOrigin.Zombie_Swipe:
            case EDamageOrigin.Mega_Zombie_Boulder:
            case EDamageOrigin.Animal_Attack:
            case EDamageOrigin.Zombie_Electric_Shock:
            case EDamageOrigin.Zombie_Stomp:
            case EDamageOrigin.Zombie_Fire_Breath:
            case EDamageOrigin.Radioactive_Zombie_Explosion:
            case EDamageOrigin.Flamable_Zombie_Explosion:
                if (__instance.passengers.Length > 0)
                {
                    if (__instance.passengers[0].player != null)
                    {
                        instigator2 = __instance.passengers[0].player.playerID.steamID;
                        vehicleData.LastInstigator = instigator2.m_SteamID;
                    }
                    // no current driver, check if the last driver exited the vehicle within the last 30 seconds
                    else if (vehicleData.LastDriver != 0 && Time.realtimeSinceStartup - vehicleData.LastDriverTime <= 30f)
                    {
                        instigator2 = new CSteamID(vehicleData.LastDriver);
                        vehicleData.LastInstigator = instigator2.m_SteamID;
                    }
                    else instigator2 = CSteamID.Nil;
                }
                else
                {
                    instigator2 = CSteamID.Nil;
                }
                break;
            // use stored instigator
            case EDamageOrigin.Grenade_Explosion:
            case EDamageOrigin.Rocket_Explosion:
            case EDamageOrigin.Vehicle_Explosion:
            case EDamageOrigin.Useable_Gun:
            case EDamageOrigin.Useable_Melee:
            case EDamageOrigin.Bullet_Explosion:
            case EDamageOrigin.Food_Explosion:
            case EDamageOrigin.Trap_Explosion:
                instigator2 = new CSteamID(vehicleData.LastInstigator);
                break;
        }

        UCPlayerData? data = null;
        if (instigator2 != CSteamID.Nil && instigator2.TryGetPlayerData(out data))
            data.ExplodingVehicle = vehicleData;
        L.LogDebug("Decided explosion instigator: " + instigator2.ToString());
        Vector3 force = new Vector3(
            Random.Range(__instance.asset.minExplosionForce.x, __instance.asset.maxExplosionForce.x),
            Random.Range(__instance.asset.minExplosionForce.y, __instance.asset.maxExplosionForce.y),
            Random.Range(__instance.asset.minExplosionForce.z, __instance.asset.maxExplosionForce.z));
        __instance.GetComponent<Rigidbody>().AddForce(force);
        __instance.GetComponent<Rigidbody>().AddTorque(16f, 0.0f, 0.0f);
        __instance.dropTrunkItems();
        if (__instance.asset.ShouldExplosionCauseDamage)
            DamageTool.explode(__instance.transform.position, 8f, EDeathCause.VEHICLE,
                instigator2, 200f, 200f, 200f, 0.0f, 0.0f, 500f, 2000f, 500f, out _,
                damageOrigin: EDamageOrigin.Vehicle_Explosion);
        for (int index = 0; index < __instance.passengers.Length; ++index)
        {
            Passenger passenger = __instance.passengers[index];
            if (passenger.player != null && passenger.player.player != null && !passenger.player.player.life.isDead)
            {
                if (__instance.asset.ShouldExplosionCauseDamage)
                    passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SPINE, instigator2, out _);
                else
                    VehicleManager.forceRemovePlayer(__instance, passenger.player.playerID.steamID);
            }
        }
        // __instance.DropScrapItems();
        VehicleManager.sendVehicleExploded(__instance);
        EffectAsset effect = __instance.asset.FindExplosionEffectAsset();
        if (effect != null)
        {
            F.TriggerEffectReliable(effect, Provider.GatherRemoteClientConnections(), __instance.transform.position);
        }
        if (data != null)
            data.ExplodingVehicle = null;
        return false;
    }
    // SDG.Unturned.Rocket.OnTriggerEnter
    /// <summary>
    /// Checking for friendlies standing on mortars.
    /// </summary>
    private static bool RocketOnTriggerEnter(Collider other, Rocket __instance, bool ___isExploded)
    {
        if (___isExploded || other.isTrigger || (__instance.ignoreTransform != null && (__instance.ignoreTransform == other.transform || other.transform.IsChildOf(__instance.ignoreTransform))))
            return false;
        if (other.transform.CompareTag("Player"))
        {
            Player? target = DamageTool.getPlayer(other.transform);
            if (target != null)
            {
                Player? pl = PlayerTool.getPlayer(__instance.killer);
                return pl == null || target.GetTeam() != pl.GetTeam();
            }
        }
        return true;
    }
    // SDG.Unturned.PlayerLife.doDamage
    /// <summary>
    /// Actual onDamageRequested event.
    /// </summary>
    private static bool PlayerDamageRequested(PlayerLife __instance, byte amount, Vector3 newRagdoll, EDeathCause newCause, ELimb newLimb, CSteamID newKiller, ref EPlayerKill kill, bool trackKill, ERagdollEffect newRagdollEffect, bool canCauseBleeding)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(__instance.player);
        if (pl is not null && pl.GodMode)
        {
            if (pl.GodMode || Teams.TeamManager.IsInAnyMainOrLobby(pl))
                return false;
        }

        return true;
    }

    private static bool OnCalculatingPower(InteractablePower __instance, ref bool __result)
    {
        if (!Data.UseElectricalGrid) return true;
        if (Data.Gamemode is not FlagGamemode fg || fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.Disabled)
        {
            __result = false;
            return true;
        }
        if (fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.AllEnabled)
        {
            __result = true;
            return false;
        }
        if (__instance is InteractableObject obj)
        {
            __result = fg.IsPowerObjectEnabled(obj);
            return false;
        }

        __result = fg.IsInteractableEnabled(__instance);
        return false;
    }
    private static bool OnReceiveSwapShirtRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Shirt, pl, page, x, y);
    private static bool OnReceiveSwapPantsRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Pants, pl, page, x, y);
    private static bool OnReceiveSwapHatRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Hat, pl, page, x, y);
    private static bool OnReceiveSwapBackpackRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Backpack, pl, page, x, y);
    private static bool OnReceiveSwapVestRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Vest, pl, page, x, y);
    private static bool OnReceiveSwapMaskRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Mask, pl, page, x, y);
    private static bool OnReceiveSwapGlassesRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Glasses, pl, page, x, y);

    private static bool OnReceiveToggleObjectBinaryStateRequest(in ServerInvocationContext context, byte x, byte y, ushort index, bool isUsed)
    {
        if (!Data.UseElectricalGrid) return true;
        if (!Regions.checkSafe(x, y) || LevelObjects.objects == null)
            return false;
        Player player = context.GetPlayer();
        List<LevelObject> levelObjects = LevelObjects.objects[x, y];
        if (player == null || player.life.isDead || index >= levelObjects.Count)
            return false;
        LevelObject obj = levelObjects[index];
        L.LogDebug($"Received request from {player} for obj {obj.asset.FriendlyName} @ {obj.transform.position} to state: {isUsed}.");
        if (Data.Gamemode is not FlagGamemode fg || fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.Disabled)
            return true;

        return obj.interactable != null && fg.IsPowerObjectEnabled(obj.interactable);
    }
    private static readonly MethodInfo? MthdRemoveItem = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.removeItem));
    private static readonly MethodInfo? MthdAddItem = typeof(SDG.Unturned.Items).GetMethod(nameof(SDG.Unturned.Items.addItem));
    private static IEnumerable<CodeInstruction> TranspileReceiveSwapOrDragItem(IEnumerable<CodeInstruction> instructions, ILGenerator generator, bool swap)
    {
        List<CodeInstruction> insts = instructions.ToList();
        Label lbl = generator.DefineLabel();
        int c = 0;
        int c2 = 0;
        for (int i = 0; i < insts.Count; ++i)
        {
            CodeInstruction instruction = insts[i];
            if (instruction.Calls(MthdAddItem!))
            {
                ++c2;
                if (c2 == (swap ? 2 : 1))
                {
                    yield return instruction;

                    yield return new CodeInstruction(OpCodes.Ldarg_0);                        // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1);                        // page_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 5 : 4));  // page_1
                    yield return new CodeInstruction(OpCodes.Ldarg_2);                        // x_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 6 : 5));  // x_1
                    yield return new CodeInstruction(OpCodes.Ldarg_3);                        // y_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 7 : 6));  // y_1
                    if (swap)
                        yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);           // rot_0
                    else
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);                 
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 8 : 7));  // rot_1
                    yield return new CodeInstruction(swap ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnDraggedOrSwappedItem));
                    L.LogDebug("Patched " + (swap ? "ReceiveSwapItem" : "ReceiveDragItem") + " post event.");
                    continue;
                }
            }
            else if (instruction.opcode == OpCodes.Ldloc_0 && insts.Count > i + 3 && insts[i + 3].Calls(MthdRemoveItem))
            {
                ++c;
                if (c == (swap ? 2 : 1))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);           // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1);           // page_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)4); // page_1
                    yield return new CodeInstruction(OpCodes.Ldarg_2);           // x_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)5); // x_1
                    yield return new CodeInstruction(OpCodes.Ldarg_3);           // y_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)6); // y_1
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)7); // rot_1
                    yield return new CodeInstruction(swap ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnDraggingOrSwappingItem));
                    yield return new CodeInstruction(OpCodes.Brtrue, lbl);       // return if cancelled
                    yield return new CodeInstruction(OpCodes.Ret);
                    L.LogDebug("Patched " + (swap ? "ReceiveSwapItem" : "ReceiveDragItem") + " requested event.");
                    generator.MarkLabel(lbl);
                }
            }

            yield return instruction;
        }
    }
    private static IEnumerable<CodeInstruction> TranspileReceiveSwapItem(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) => TranspileReceiveSwapOrDragItem(instructions, generator, true);
    private static IEnumerable<CodeInstruction> TranspileReceiveDragItem(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) => TranspileReceiveSwapOrDragItem(instructions, generator, false);
    private static IEnumerable<CodeInstruction> TranspileReceiveTakeItemRequest(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        FieldInfo? rpcField = typeof(ItemManager).GetField("SendDestroyItem", BindingFlags.NonPublic | BindingFlags.Static);
        if (rpcField == null)
            L.LogWarning("Unable to find 'ItemManager.SendDestroyItem' while transpiling ReceiveTakeItemRequest.");

        int lcl = method.FindLocalOfType<ItemData>();
        if (lcl < 0)
            L.LogWarning("Unable to find local for ItemData while transpiling ReceiveTakeItemRequest.");

        int lcl2 = method.FindLocalOfType<Player>();
        if (lcl2 < 0)
            L.LogWarning("Unable to find local for Player while transpiling ReceiveTakeItemRequest.");

        List<CodeInstruction> insts = instructions.ToList();
        bool foundOne = false;
        bool nextRtnCallEvent = false;
        for (int i = 0; i < insts.Count; ++i)
        {
            CodeInstruction instruction = insts[i];
            if (!foundOne && rpcField != null && instruction.LoadsField(rpcField))
            {
                foundOne = true;
                nextRtnCallEvent = true;
            }
            else if (nextRtnCallEvent && instruction.opcode == OpCodes.Ret)
            {
                nextRtnCallEvent = false;
                yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2);  // player
                yield return new CodeInstruction(OpCodes.Ldarg_1);              // x
                yield return new CodeInstruction(OpCodes.Ldarg_2);              // y
                yield return new CodeInstruction(OpCodes.Ldarg_3);              // instanceID
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);     // to_x
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5);     // to_y
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)6);     // to_rot
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)7);     // to_page
                if (lcl > -1)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl); // to_page
                else
                    yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnPickedUpItem));
                L.LogDebug("Patched ReceiveTakeItemRequest.");
            }

            yield return instruction;
        }
    }
}
