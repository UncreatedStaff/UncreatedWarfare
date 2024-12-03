using DanielWillett.ReflectionTools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Components;
using Module = SDG.Framework.Modules.Module;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    public static HarmonyLib.Harmony Patcher = new HarmonyLib.Harmony("network.uncreated.warfare");
    /// <summary>Patch methods</summary>
    public static void DoPatching(Module module, IServiceProvider serviceProvider)
    {
        Patcher.PatchAll();
        // if (!UCWarfare.Config.DisableMissingAssetKick)
        //     InternalPatches.ServerMessageHandler_ValidateAssets_Patch.Patch(Patcher);
    }
    /// <summary>Unpatch methods</summary>
    public static void Unpatch(IServiceProvider serviceProvider)
    {
        Patcher.UnpatchAll("network.uncreated.warfare");
    }
    public delegate void BarricadeDroppedEventArgs(BarricadeDrop drop, BarricadeRegion region, Barricade barricade, Vector3 point, Quaternion rotation, ulong owner, ulong group);
    public delegate void StructureDestroyedEventArgs(StructureData data, StructureDrop drop, uint instanceID);
    public delegate void BarricadeHealthEventArgs(BarricadeData data);
    public delegate void OnPlayerTogglesCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool allow);
    public delegate void OnPlayerSetsCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool allow);
    public delegate void BatteryStealingDelegate(SteamPlayer theif, ref bool allow);
    public delegate void PlayerTriedStoreItem(Player player, byte page, ItemJar jar, ref bool allow);
    public delegate void InventoryItemAdded(Player __instance, byte page, byte index, ItemJar jar);
    public delegate void PlayerGesture(Player player, EPlayerGesture gesture, ref bool allow);
    public delegate void PlayerMarker(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed);

    public static event OnPlayerTogglesCosmeticsDelegate OnPlayerTogglesCosmetics_Global;
    public static event BatteryStealingDelegate OnBatterySteal_Global;
    public static event PlayerTriedStoreItem OnPlayerTriedStoreItem_Global;
    public static event PlayerGesture OnPlayerGesture_Global;
    public static event PlayerMarker OnPlayerMarker_Global;

    [HarmonyPatch]
    public static class InternalPatches
    {
#if false
        // SDG.Unturned.Provider
        /// <summary>
        /// Postfix of <see cref="Provider.verifyNextPlayerInQueue()"/> to check if the new player in the queue is an admin, then pass them.
        /// </summary>
        [HarmonyPatch(typeof(Provider), "verifyNextPlayerInQueue")]
        [HarmonyPostfix]
        [UsedImplicitly]
        static void OnPlayerEnteredQueuePost()
        {
            if (Provider.pending.Count > 0)
            {
                for (int i = 0; i < Provider.pending.Count; i++)
                {
                    SteamPending pending = Provider.pending[i];
                    if (pending.hasSentVerifyPacket)
                        continue;
                    if (pending.playerID.steamID.m_SteamID.IsAdmin() || pending.playerID.steamID.m_SteamID.IsIntern() || PlayerManager.HasSave(pending.playerID.steamID.m_SteamID, out PlayerSave save) && save.HasQueueSkip)
                        pending.sendVerifyPacket();
                }
            }
        }
#endif

        // SDG.Unturned.PlayerAnimator
        /// <summary>
        /// Prefix of <see cref="PlayerAnimator.ReceiveGesture(EPlayerGesture)"/> to add an event.
        /// </summary>
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.ReceiveGestureRequest))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnGestureReceived(EPlayerGesture newGesture, PlayerAnimator __instance)
        {
            if (OnPlayerGesture_Global != null)
            {
                bool allow = true;
                OnPlayerGesture_Global.Invoke(__instance.player, newGesture, ref allow);
                return allow;
            }
            return true;
        }
        // SDG.Unturned.PlayerQuests
        /// <summary>
        /// Prefix of <see cref="PlayerQuests.replicateSetMarker(bool, Vector3, string)"/> to add an event.
        /// </summary>
        [HarmonyPatch(typeof(PlayerQuests), "replicateSetMarker")]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnPlayerMarked(ref bool newIsMarkerPlaced, ref Vector3 newMarkerPosition, ref string newMarkerTextOverride, PlayerQuests __instance)
        {
            if (OnPlayerMarker_Global != null)
            {
                bool isAllowed = true;
                OnPlayerMarker_Global.Invoke(__instance.player, ref newMarkerPosition, ref newMarkerTextOverride, ref newIsMarkerPlaced, ref isAllowed);
                return isAllowed;
            }

            return true;
        }
        // SDG.Unturned.PlayerInventory
        ///<summary>
        /// Prefix of <see cref="PlayerInventory.ReceiveDragItem(byte, byte, byte, byte, byte, byte, byte)"/> to disallow players leaving their group.
        ///</summary>
        [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.ReceiveDragItem))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelStoringNonWhitelistedItem(PlayerInventory __instance, byte page_0, byte x_0, byte y_0, byte page_1, byte x_1, byte y_1, byte rot_1)
        {
            bool allow = true;
            ItemJar jar = __instance.getItem(page_0, __instance.getIndex(page_0, x_0, y_0));
            if (page_1 == PlayerInventory.STORAGE)
                OnPlayerTriedStoreItem_Global?.Invoke(__instance.player, page_0, jar, ref allow);
            return allow;
        }

#if false
        // SDG.Unturned.GroupManager
        ///<summary>
        /// Prefix of <see cref="GroupManager.requestGroupExit(Player)"/> to disallow players leaving their group.
        ///</summary>
        [HarmonyPatch(typeof(GroupManager), nameof(GroupManager.requestGroupExit))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelLeavingGroup(Player player)
        {
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl == null || pl.OnDutyOrAdmin()) return true;
            player.SendChat(T.NoLeavingGroup);
            return false;
        }
#endif

        // SDG.Unturned.PlayerClothing
        /// <summary>
        /// Prefix of <see cref="PlayerClothing.ReceiveVisualToggleRequest(EVisualToggleType)"/> to use an event to cancel it.
        /// </summary>
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveVisualToggleRequest))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelCosmeticChangesPrefix(EVisualToggleType type, PlayerClothing __instance)
        {
            EVisualToggleType newtype = type;
            bool allow = true;
            OnPlayerTogglesCosmetics_Global?.Invoke(ref newtype, __instance.player.channel.owner, ref allow);
            return allow;
        }
        // SDG.Unturned.VehicleManager
        /// <summary>
        /// Prefix of <see cref="VehicleManager.ReceiveStealVehicleBattery"/> to disable the removal of batteries from vehicles.
        /// </summary>
        [HarmonyPatch(typeof(VehicleManager), nameof(VehicleManager.ReceiveStealVehicleBattery))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool BatteryStealingOverride(in ServerInvocationContext context)
        {
            bool allow = true;
            OnBatterySteal_Global?.Invoke(context.GetCallingPlayer(), ref allow);
            return allow;
        }

        [HarmonyPatch(typeof(Rocket), "OnTriggerEnter")]
        [HarmonyPrefix]
        [UsedImplicitly]
        static void OnProjectileCollided(Rocket __instance, Collider other)
        {
            if (__instance.gameObject.TryGetComponent(out ProjectileComponent projectile))
            {
                projectile.OnCollided(other);
            }
        }

        private static void LogIntl(string msg)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning(msg);
        }

        public static class ServerMessageHandler_ValidateAssets_Patch
        {
            private const string READ_MESSSAGE_NAME = "ReadMessage";
            private const string VALIDATE_ASSETS_HANDLER_NAME = "ServerMessageHandler_ValidateAssets";
            private const string SEND_KICK_NETCALL_NAME = "SendKickForInvalidGuid";
            private static readonly Type? ValidateAssetsHandlerType = typeof(Provider).Assembly.GetType("SDG.Unturned." + VALIDATE_ASSETS_HANDLER_NAME);
            private static readonly FieldInfo? SendKickForInvalidGuidField = typeof(Assets).GetField(SEND_KICK_NETCALL_NAME, BindingFlags.NonPublic | BindingFlags.Static);
            private static readonly MethodInfo? LogMethod = Accessor.GetMethod(LogIntl);
            private static readonly MethodInfo? GuidToStringMethod = typeof(Guid).GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.Name.Equals(nameof(Guid.ToString), StringComparison.Ordinal) && x.GetParameters().Length == 2);
            private static readonly MethodInfo? Concat2Method = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(x => x.Name.Equals(nameof(string.Concat)) && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(string));
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool found = false;
                foreach (CodeInstruction instruction in instructions)
                {
                    if (!found && instruction.opcode == OpCodes.Ldsfld && instruction.LoadsField(SendKickForInvalidGuidField))
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, "Unknown asset found: ");
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 5);
                        yield return new CodeInstruction(OpCodes.Ldstr, "N");
                        yield return new CodeInstruction(OpCodes.Ldnull);
                        yield return new CodeInstruction(OpCodes.Callvirt, GuidToStringMethod);
                        yield return new CodeInstruction(OpCodes.Call, Concat2Method); // message + guid
                        yield return new CodeInstruction(OpCodes.Ldstr, "VALIDATE ASSETS");
                        yield return new CodeInstruction(OpCodes.Call, LogMethod);
                        yield return new CodeInstruction(OpCodes.Ret);
                        found = true;
                    }
                    yield return instruction;
                }
                if (!found)
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning("Patch on " + VALIDATE_ASSETS_HANDLER_NAME + "." + READ_MESSSAGE_NAME + " failed to find an injection point.");
                }
            }

            internal static void Patch(HarmonyLib.Harmony patcher)
            {
                if (SendKickForInvalidGuidField == null)
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning("Failed to find field: " + nameof(Assets) + "." + SEND_KICK_NETCALL_NAME + ".");
                    return;
                }
                if (ValidateAssetsHandlerType != null)
                {
                    MethodInfo? original = ValidateAssetsHandlerType.GetMethod(READ_MESSSAGE_NAME, BindingFlags.NonPublic | BindingFlags.Static);
                    if (original == null)
                    {
                        WarfareModule.Singleton.GlobalLogger.LogWarning("Failed to find method " + VALIDATE_ASSETS_HANDLER_NAME + "." + READ_MESSSAGE_NAME + ".");
                        return;
                    }
                    patcher.Patch(original, transpiler: new HarmonyMethod(typeof(ServerMessageHandler_ValidateAssets_Patch).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static)));
                }
                else
                {
                    WarfareModule.Singleton.GlobalLogger.LogWarning("Failed to find method " + VALIDATE_ASSETS_HANDLER_NAME + ".");
                }
            }
        }
    }
}