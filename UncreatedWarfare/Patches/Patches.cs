using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Squads;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    public static HarmonyLib.Harmony Patcher = new HarmonyLib.Harmony("net.uncreated.warfare");
    /// <summary>Patch methods</summary>
    public static void DoPatching()
    {
        Patcher.PatchAll();
        if (UCWarfare.Config.DisableMissingAssetKick)
            InternalPatches.ServerMessageHandler_ValidateAssets_Patch.Patch(Patcher);
    }
    /// <summary>Unpatch methods</summary>
    public static void Unpatch()
    {
        Patcher.UnpatchAll("net.uncreated.warfare");
    }
    public delegate void BarricadeDroppedEventArgs(BarricadeDrop drop, BarricadeRegion region, Barricade barricade, Vector3 point, Quaternion rotation, ulong owner, ulong group);
    public delegate void StructureDestroyedEventArgs(StructureData data, StructureDrop drop, uint instanceID);
    public delegate void BarricadeHealthEventArgs(BarricadeData data);
    public delegate void OnPlayerTogglesCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool allow);
    public delegate void OnPlayerSetsCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow);
    public delegate void BatteryStealingDelegate(SteamPlayer theif, ref bool allow);
    public delegate void PlayerTriedStoreItem(Player player, byte page, ItemJar jar, ref bool allow);
    public delegate void InventoryItemAdded(Player __instance, byte page, byte index, ItemJar jar);
    public delegate void PlayerGesture(Player player, EPlayerGesture gesture, ref bool allow);
    public delegate void PlayerMarker(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed);

    public static event OnPlayerTogglesCosmeticsDelegate OnPlayerTogglesCosmetics_Global;
    public static event OnPlayerSetsCosmeticsDelegate OnPlayerSetsCosmetics_Global;
    public static event BatteryStealingDelegate OnBatterySteal_Global;
    public static event PlayerTriedStoreItem OnPlayerTriedStoreItem_Global;
    public static event PlayerGesture OnPlayerGesture_Global;
    public static event PlayerMarker OnPlayerMarker_Global;
    [HarmonyPatch]
    public static class InternalPatches
    {
        /*
        //private static readonly string LOG_MESSAGE_ID_STR = L.NetCalls.SendLogMessage.ID.ToString(Data.Locale);
        // SDG.Unturned.Provider
        /// <summary>
        /// Prefix of <see cref="Console.WriteLine(string)"/> to send any logs to the tcp server and log them.
        /// </summary>
        [HarmonyPatch(typeof(Console), nameof(Console.WriteLine), typeof(string))]
        [HarmonyPrefix]
        static void ConsolePatch(string value)
        {
            if (!L.isRequestingLog || value.StartsWith("Sent over TCP server on", StringComparison.Ordinal) || value.StartsWith("Error writing to", StringComparison.Ordinal)) return;
            string[] splits = value.Split('\n');
            for (int i = 0; i < splits.Length; i++)
            {
                LogMessage log = new LogMessage(splits[i], Console.ForegroundColor);
                L.AddLog(log);
                if (UCWarfare.CanUseNetCall && value.IndexOf(LOG_MESSAGE_ID_STR, StringComparison.Ordinal) != 21)
                    L.NetCalls.SendLogMessage.Invoke(Data.NetClient!, log, 0);
            }
        }*/
        [HarmonyPatch(typeof(SteamPlayerID), "BypassIntegrityChecks", MethodType.Getter)]
        [HarmonyPostfix]
        [UsedImplicitly]
        static void GetBypassIntegrityChecksPrefix(SteamPlayerID __instance, ref bool __result)
        {
#if DEBUG
            __result = true;
#else
            if (!__result && __instance.steamID.m_SteamID == 76561198267927009UL || __instance.steamID.m_SteamID == 76561198857595123UL)
                __result = true;
#endif
        }


        // SDG.Unturned.Provider
        /// <summary>
        /// Postfix of <see cref="Provider.verifyNextPlayerInQueue()"/> to check if the new player in the queue is an admin, then pass them.
        /// </summary>
        [HarmonyPatch(typeof(Provider), "verifyNextPlayerInQueue")]
        [HarmonyPostfix]
        [UsedImplicitly]
        static void OnPlayerEnteredQueuePost()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        private static readonly string[] dontLogCommands =
        {
            "request",
            "req",
            "buy",
            "deploy",
            "dep",
            "ammo",
            "i",
            "lang",
            "kit",
            "kits",
            "vehiclebay",
            "vb",
            "whitelist",
            "wl",
            "struct",
            "structure",
            "permissions",
            "perms",
            "p"
        };
        private static bool ShouldLog(string message)
        {
            if (message == null || message.Length < 2) return false;
            if (message[0] != '/') return true;
            string cmd = message.Substring(1).Split(' ')[0];
            for (int i = 0; i < dontLogCommands.Length; ++i)
                if (cmd.Equals(dontLogCommands[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
        // SDG.Unturned.ChatManager
        /// <summary>
        /// Postfix of <see cref="ChatManager.ReceiveChatRequest(in ServerInvocationContext, byte, string)"/> to reroute local chats to squad.
        /// </summary>
        [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.ReceiveChatRequest))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnChatRequested(in ServerInvocationContext context, byte flags, string text)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            SteamPlayer callingPlayer = context.GetCallingPlayer();
            UCPlayer? caller = UCPlayer.FromSteamPlayer(callingPlayer);
            if (callingPlayer == null || callingPlayer.player == null || Time.realtimeSinceStartup - callingPlayer.lastChat < ChatManager.chatrate)
                return false;
            callingPlayer.lastChat = Time.realtimeSinceStartup;
            EChatMode mode = (EChatMode)(flags & sbyte.MaxValue);
            bool fromUnityEvent = (flags & 128) > 0;
            if (text.Length < 2 || true & fromUnityEvent && !Provider.configData.UnityEvents.Allow_Client_Messages)
                return false;
            text = text.Trim();
            if (text.Length < 2)
                return false;
            if (text.Length > ChatManager.MAX_MESSAGE_LENGTH)
                text = text.Substring(0, ChatManager.MAX_MESSAGE_LENGTH);
            if (CommandWindow.shouldLogChat)
            {
                PlayerNames n = caller is null ? new PlayerNames(callingPlayer) : caller.Name;
                string name = $"[{n.PlayerName} ({n.CharacterName})]:";
                int len = 40 - name.Length;
                if (len > 0)
                    name += new string(' ', len);
                switch (mode)
                {
                    case EChatMode.GLOBAL:
                        L.Log($"[ALL]  {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(ActionLogType.ChatGlobal, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    case EChatMode.LOCAL:
                        L.Log($"[A/S]  {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(ActionLogType.ChatAreaOrSquad, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    case EChatMode.GROUP:
                        L.Log($"[TEAM] {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(ActionLogType.ChatGroup, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    default:
                        return false;
                }
            }
            else if (mode is not EChatMode.GLOBAL and not EChatMode.LOCAL and not EChatMode.GROUP) return false;
            if (fromUnityEvent)
                L.Log($"UnityEventMsg {callingPlayer.playerID.steamID}: \"{text}\"", ConsoleColor.DarkCyan);
            ulong team = callingPlayer.GetTeam();
            Color chatted = Palette.AMBIENT;
            bool duty = caller is not null && caller.OnDuty();
            if (callingPlayer.isAdmin || duty)
                chatted = Palette.ADMIN;
            bool isRich = true;
            bool isVisible = true;
            ChatManager.onChatted?.Invoke(callingPlayer, mode, ref chatted, ref isRich, text, ref isVisible);
            if (!(ChatManager.process(callingPlayer, text, fromUnityEvent) && isVisible))
                return false;
            if (caller != null)
            {
                if ((caller.MuteType & Commands.EMuteType.TEXT_CHAT) == Commands.EMuteType.TEXT_CHAT &&
                    caller.TimeUnmuted > DateTime.Now)
                {
                    if (caller.TimeUnmuted == DateTime.MaxValue)
                        caller.SendChat(T.MuteTextChatFeedbackPermanent, caller.MuteReason ?? "unknown");
                    else
                        caller.SendChat(T.MuteTextChatFeedback, caller.TimeUnmuted, caller.MuteReason ?? "unknown");
                    return false;
                }
                if (!duty)
                {
                    Match match = Data.ChatFilter.Match(text);
                    if (match.Success && match.Length > 0)
                    {
                        caller.SendChat(T.ChatFilterFeedback, match.Value);
                        ActionLog.Add(ActionLogType.ChatFilterViolation, mode switch { EChatMode.LOCAL => "AREA/SQUAD: ", EChatMode.GLOBAL => "GLOBAL: ", _ => "TEAM: " } + text, caller);
                        return false;
                    }
                }
            }

            int txtType;
            string newText;
            string? imgui = null;
            if (callingPlayer.isAdmin || duty)
            {
                txtType = 1;
                newText = "<#" + Teams.TeamManager.AdminColorHex + ">%SPEAKER%</color>: " + text;
            }
            else if (caller != null && SquadManager.Loaded && SquadManager.Singleton.Commanders.IsCommander(caller))
            {
                txtType = 2;
                newText = "<#" + UCWarfare.GetColorHex("commander") + ">%SPEAKER%</color>: <noparse>" + text.Replace("</noparse>", string.Empty);
            }
            else
            {
                txtType = 3;
                string hx = Teams.TeamManager.GetTeamHexColor(team);
                newText = "<#" + hx + ">%SPEAKER%</color>: <noparse>" + text.Replace("</noparse>", string.Empty);
            }
            string GetIMGUIText()
            {
                return txtType switch
                {
                    1 => "<color=#" + Teams.TeamManager.AdminColorHex + ">%SPEAKER%</color>: " + text,
                    2 => "<color=#" + UCWarfare.GetColorHex("commander") + ">%SPEAKER%</color>: " + text.Replace('<', '{').Replace('>', '}'),
                    _ => "<color=#" + Teams.TeamManager.GetTeamHexColor(team) + ">%SPEAKER%</color>: " + text.Replace('<', '{').Replace('>', '}')
                };
            }
            if (mode == EChatMode.GLOBAL)
            {
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    ChatManager.serverSendMessage(pl.Save.IMGUI ? (imgui ??= GetIMGUIText()) : newText, chatted, callingPlayer,
                        pl.Player.channel.owner, EChatMode.GLOBAL, useRichTextFormatting: true);
                }
            }
            else if (mode == EChatMode.LOCAL)
            {
                const float num = 16384f;
                Vector3 pos = callingPlayer.player.transform.position;
                if (caller == null || caller.Squad == null || caller.Squad.Members == null)
                {
                    newText = "[A] " + newText;
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        UCPlayer pl = PlayerManager.OnlinePlayers[i];
                        if ((double)(pl.Position - pos).sqrMagnitude < num)
                            ChatManager.serverSendMessage(pl.Save.IMGUI ? (imgui ??= "[A] " + GetIMGUIText()) : newText, chatted, callingPlayer, pl.Player.channel.owner, EChatMode.LOCAL, useRichTextFormatting: isRich);
                    }
                }
                else
                {
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                    {
                        UCPlayer pl = PlayerManager.OnlinePlayers[i];
                        if (caller.Squad.ContainsMember(pl))
                            ChatManager.serverSendMessage("[SQ] " + (pl.Save.IMGUI ? (imgui ??= GetIMGUIText()) : newText), chatted, callingPlayer, pl.Player.channel.owner, EChatMode.LOCAL, useRichTextFormatting: isRich);
                        else if ((pl.Position - pos).sqrMagnitude < num)
                            ChatManager.serverSendMessage("[A] "  + (pl.Save.IMGUI ? (imgui ??= GetIMGUIText()) : newText), chatted, callingPlayer, pl.Player.channel.owner, EChatMode.LOCAL, useRichTextFormatting: isRich);
                    }
                }
            }
            else
            {
                if (!callingPlayer.player.quests.isMemberOfAGroup)
                    return false;
                newText = "[T] " + newText;
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (pl.Player.quests.isMemberOfSameGroupAs(callingPlayer.player))
                        ChatManager.serverSendMessage(pl.Save.IMGUI ? (imgui ??= "[T] " + GetIMGUIText()) : newText, chatted, callingPlayer, pl.Player.channel.owner, EChatMode.GROUP, useRichTextFormatting: isRich);
                }
            }
            Data.Reporter?.OnPlayerChat(callingPlayer.playerID.steamID.m_SteamID, text);
            return false;
        }
        // SDG.Unturned.PlayerAnimator
        /// <summary>
        /// Prefix of <see cref="PlayerAnimator.ReceiveGesture(EPlayerGesture)"/> to add an event.
        /// </summary>
        [HarmonyPatch(typeof(PlayerAnimator), nameof(PlayerAnimator.ReceiveGestureRequest))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool OnGestureReceived(EPlayerGesture newGesture, PlayerAnimator __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            bool isAllowed = true;
            OnPlayerMarker_Global.Invoke(__instance.player, ref newMarkerPosition, ref newMarkerTextOverride, ref newIsMarkerPlaced, ref isAllowed);
            return isAllowed;
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            bool allow = true;
            ItemJar jar = __instance.getItem(page_0, __instance.getIndex(page_0, x_0, y_0));
            if (page_1 == PlayerInventory.STORAGE)
                OnPlayerTriedStoreItem_Global?.Invoke(__instance.player, page_0, jar, ref allow);
            return allow;
        }

        // SDG.Unturned.GroupManager
        ///<summary>
        /// Prefix of <see cref="GroupManager.requestGroupExit(Player)"/> to disallow players leaving their group.
        ///</summary>
        [HarmonyPatch(typeof(GroupManager), nameof(GroupManager.requestGroupExit))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelLeavingGroup(Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl == null || pl.OnDutyOrAdmin()) return true;
            player.SendChat(T.NoLeavingGroup);
            return false;
        }
        // SDG.Unturned.PlayerClothing
        /// <summary>
        /// Prefix of <see cref="PlayerClothing.ReceiveVisualToggleRequest(EVisualToggleType)"/> to use an event to cancel it.
        /// </summary>
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveVisualToggleRequest))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelCosmeticChangesPrefix(EVisualToggleType type, PlayerClothing __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            EVisualToggleType newtype = type;
            bool allow = true;
            OnPlayerTogglesCosmetics_Global?.Invoke(ref newtype, __instance.player.channel.owner, ref allow);
            return allow;
        }
        // SDG.Unturned.PlayerClothing
        /// <summary>
        /// Prefix of <see cref="PlayerClothing.ServerSetVisualToggleState(EVisualToggleType, bool)"/> to use an event to cancel it.
        /// </summary>
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool CancelCosmeticSetPrefix(EVisualToggleType type, ref bool isVisible, PlayerClothing __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            EVisualToggleType newtype = type;
            bool allow = true;
            OnPlayerSetsCosmetics_Global?.Invoke(ref newtype, __instance.player.channel.owner, ref isVisible, ref allow);
            return allow;
        }
        // SDG.Unturned.VehicleManager
        /// <summary>
        /// Prefix of <see cref="VehicleManager.ReceiveStealVehicleBattery(in ServerInvocationContext)"/> to disable the removal of batteries from vehicles.
        /// </summary>
        [HarmonyPatch(typeof(VehicleManager), nameof(VehicleManager.ReceiveStealVehicleBattery))]
        [HarmonyPrefix]
        [UsedImplicitly]
        static bool BatteryStealingOverride(in ServerInvocationContext context)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            bool allow = true;
            OnBatterySteal_Global?.Invoke(context.GetCallingPlayer(), ref allow);
            return allow;
        }

        // SDG.Unturned.UseableGun
        /// <summary>
        /// prefix of <see cref="UseableMelee.fire()"/> to determine hits with the Entrenching Tool.
        /// </summary>
        [HarmonyPatch(typeof(UseableMelee), "fire")]
        [HarmonyPrefix]
        [UsedImplicitly]
        static void OnPreMeleeHit(UseableMelee __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ItemWeaponAsset weaponAsset = ((ItemWeaponAsset)__instance.player.equipment.asset);

            RaycastInfo info = DamageTool.raycast(new Ray(__instance.player.look.aim.position, __instance.player.look.aim.forward), weaponAsset.range, RayMasks.BARRICADE, __instance.player);
            if (info.transform != null)
            {
                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
                if (drop != null)
                {
                    UCPlayer? builder = UCPlayer.FromPlayer(__instance.player);

                    if (builder != null && builder.GetTeam() == drop.GetServersideData().group)
                    {
                        if (Gamemode.Config.ItemEntrenchingTool.MatchGuid(__instance.equippedMeleeAsset.GUID))
                        {
                            if (drop.model.TryGetComponent(out RepairableComponent repairable))
                                repairable.Repair(builder);
                            else if (drop.model.TryGetComponent(out BuildableComponent buildable))
                                buildable.IncrementBuildPoints(builder);
                            else if (drop.model.TryGetComponent(out FOBComponent radio))
                                radio.Parent.Repair(builder);
                        }
                    }
                }
            }
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
        public static class ServerMessageHandler_ValidateAssets_Patch
        {
            private const string READ_MESSSAGE_NAME = "ReadMessage";
            private const string VALIDATE_ASSETS_HANDLER_NAME = "ServerMessageHandler_ValidateAssets";
            private const string SEND_KICK_NETCALL_NAME = "SendKickForInvalidGuid";
            private static readonly Type? ValidateAssetsHandlerType = typeof(Provider).Assembly.GetType("SDG.Unturned." + VALIDATE_ASSETS_HANDLER_NAME);
            private static readonly FieldInfo? SendKickForInvalidGuidField = typeof(Assets).GetField(SEND_KICK_NETCALL_NAME, BindingFlags.NonPublic | BindingFlags.Static);
            private static readonly MethodInfo? LogMethod = typeof(L).GetMethod(nameof(L.LogWarning), BindingFlags.Public | BindingFlags.Static);
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
                        yield return new CodeInstruction(OpCodes.Ldc_I4, (int)ConsoleColor.Yellow);
                        yield return new CodeInstruction(OpCodes.Ldstr, "VALIDATE ASSETS");
                        yield return new CodeInstruction(OpCodes.Call, LogMethod);
                        yield return new CodeInstruction(OpCodes.Ret);
                        found = true;
                    }
                    yield return instruction;
                }
                if (!found)
                {
                    L.LogWarning("Patch on " + VALIDATE_ASSETS_HANDLER_NAME + "." + READ_MESSSAGE_NAME + " failed to find an injection point.");
                }
            }

            internal static void Patch(HarmonyLib.Harmony patcher)
            {
                if (SendKickForInvalidGuidField == null)
                {
                    L.LogWarning("Failed to find field: " + nameof(Assets) + "." + SEND_KICK_NETCALL_NAME + ".");
                    return;
                }
                if (ValidateAssetsHandlerType != null)
                {
                    MethodInfo? original = ValidateAssetsHandlerType.GetMethod(READ_MESSSAGE_NAME, BindingFlags.NonPublic | BindingFlags.Static);
                    if (original == null)
                    {
                        L.LogWarning("Failed to find method " + VALIDATE_ASSETS_HANDLER_NAME + "." + READ_MESSSAGE_NAME + ".");
                        return;
                    }
                    patcher.Patch(original, transpiler: new HarmonyMethod(typeof(ServerMessageHandler_ValidateAssets_Patch).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static)));
                }
                else
                {
                    L.LogWarning("Failed to find method " + VALIDATE_ASSETS_HANDLER_NAME + ".");
                }
            }
        }
    }
}