using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using UnityEngine;

namespace Uncreated.Warfare;

public static partial class Patches
{
    public static Harmony Patcher = new Harmony("net.uncreated.warfare");
    /// <summary>Patch methods</summary>
    public static void DoPatching()
    {
        Patcher.PatchAll();
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

    public static event StructureDestroyedEventArgs StructureDestroyedHandler;
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
        static void GetBypassIntegrityChecksPrefix(SteamPlayerID __instance, ref bool __result)
        {
            if (!__result && __instance.steamID.m_SteamID == 76561198267927009UL || __instance.steamID.m_SteamID == 76561198857595123UL)
                __result = true;
        }


        // SDG.Unturned.Provider
        /// <summary>
        /// Postfix of <see cref="Provider.verifyNextPlayerInQueue()"/> to check if the new player in the queue is an admin, then pass them.
        /// </summary>
        [HarmonyPatch(typeof(Provider), "verifyNextPlayerInQueue")]
        [HarmonyPostfix]
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
                    if (F.IsAdmin(pending.playerID.steamID.m_SteamID) || F.IsIntern(pending.playerID.steamID.m_SteamID) || PlayerManager.HasSave(pending.playerID.steamID.m_SteamID, out PlayerSave save) && save.HasQueueSkip)
                        pending.sendVerifyPacket();
                }
            }
        }
        private static readonly string[] dontLogCommands = new string[]
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
            "structure"
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
                FPlayerName n = F.GetPlayerOriginalNames(callingPlayer);
                string name = $"[{n.PlayerName} ({n.CharacterName})]:";
                int len = 40 - name.Length;
                if (len > 0)
                    name += new string(' ', len);
                switch (mode)
                {
                    case EChatMode.GLOBAL:
                        L.Log($"[ALL]  {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(EActionLogType.CHAT_GLOBAL, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    case EChatMode.LOCAL:
                        L.Log($"[A/S]  {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(EActionLogType.CHAT_AREA_OR_SQUAD, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    case EChatMode.GROUP:
                        L.Log($"[TEAM] {name} \"{text}\"", ConsoleColor.DarkGray);
                        if (ShouldLog(text))
                            ActionLog.Add(EActionLogType.CHAT_GROUP, text, callingPlayer.playerID.steamID.m_SteamID);
                        break;
                    default:
                        return false;
                }
            }
            else if (mode != EChatMode.GLOBAL || mode != EChatMode.LOCAL || mode != EChatMode.GROUP) return false;
            if (fromUnityEvent)
                L.Log($"UnityEventMsg {callingPlayer.playerID.steamID}: \"{text}\"", ConsoleColor.DarkCyan);
            ulong team = callingPlayer.GetTeam();
            Color chatted = Teams.TeamManager.GetTeamColor(team);
            if (callingPlayer.isAdmin && !Provider.hideAdmins)
                chatted = Palette.ADMIN;
            bool isRich = true;
            bool isVisible = true;
            ChatManager.onChatted?.Invoke(callingPlayer, mode, ref chatted, ref isRich, text, ref isVisible);
            if (!(ChatManager.process(callingPlayer, text, fromUnityEvent) && isVisible))
                return false;
            if (caller != null && (caller.MuteType & Commands.EMuteType.TEXT_CHAT) == Commands.EMuteType.TEXT_CHAT && caller.TimeUnmuted > DateTime.Now)
            {
                if (caller.TimeUnmuted == DateTime.MaxValue)
                    caller.SendChat("text_chat_feedback_muted_permanent", caller.MuteReason ?? "unknown");
                else
                    caller.SendChat("text_chat_feedback_muted", caller.TimeUnmuted.ToString("g") + " EST", caller.MuteReason ?? string.Empty);
                return false;
            }
            if (ChatManager.onServerFormattingMessage != null)
            {
                ChatManager.onServerFormattingMessage(callingPlayer, mode, ref text);
            }
            else
            {
                if (caller is not null && caller.OnDutyOrAdmin())
                    text = "<color=#" + Teams.TeamManager.GetTeamHexColor(callingPlayer.GetTeam()) + ">%SPEAKER%</color>: " + text;
                else
                    text = "<color=#" + Teams.TeamManager.GetTeamHexColor(callingPlayer.GetTeam()) + ">%SPEAKER%</color>: <noparse>" + text.Replace("</noparse>", "");
                if (mode == EChatMode.GROUP)
                    text = "[T] " + text;
            }
            if (mode == EChatMode.GLOBAL)
                ChatManager.serverSendMessage(text, chatted, callingPlayer, mode: EChatMode.GLOBAL, useRichTextFormatting: isRich);
            else if (mode == EChatMode.LOCAL)
            {
                float num = 16384f;
                if (caller == null || caller.Squad == null || caller.Squad.Members == null)
                {
                    foreach (SteamPlayer client in Provider.clients)
                    {
                        if (client.player != null && (double)(client.player.transform.position - callingPlayer.player.transform.position).sqrMagnitude < num)
                            ChatManager.serverSendMessage("[A] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                    }
                }
                else
                {
                    foreach (SteamPlayer client in Provider.clients)
                    {
                        if (caller.Squad.Members.Exists(x => x.Steam64 == client.playerID.steamID.m_SteamID))
                            ChatManager.serverSendMessage("[SQ] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                        else if (client.player != null && (client.player.transform.position - callingPlayer.player.transform.position).sqrMagnitude < num)
                            ChatManager.serverSendMessage("[A] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                    }
                }
            }
            else
            {
                if (mode != EChatMode.GROUP || callingPlayer.player.quests.groupID == CSteamID.Nil)
                    return false;
                foreach (SteamPlayer client in Provider.clients)
                {
                    if (client.player.quests.isMemberOfSameGroupAs(callingPlayer.player))
                        ChatManager.serverSendMessage(text, chatted, callingPlayer, client, EChatMode.GROUP, useRichTextFormatting: isRich);
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
        static bool CancelLeavingGroup(Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? pl = UCPlayer.FromPlayer(player);
            if (pl == null || pl.OnDutyOrAdmin()) return true;
            player.SendChat("cant_leave_group");
            return false;
        }
        // SDG.Unturned.PlayerClothing
        /// <summary>
        /// Prefix of <see cref="PlayerClothing.ReceiveVisualToggleRequest(EVisualToggleType)"/> to use an event to cancel it.
        /// </summary>
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveVisualToggleRequest))]
        [HarmonyPrefix]
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
        static void OnPreMeleeHit(UseableMelee __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            var weaponAsset = ((ItemWeaponAsset)__instance.player.equipment.asset);

            RaycastInfo info = DamageTool.raycast(new Ray(__instance.player.look.aim.position, __instance.player.look.aim.forward), weaponAsset.range, RayMasks.BARRICADE, __instance.player);
            if (info.transform != null)
            {
                var drop = BarricadeManager.FindBarricadeByRootTransform(info.transform);
                if (drop != null)
                {
                    UCPlayer? builder = UCPlayer.FromPlayer(__instance.player);

                    if (builder != null && builder.GetTeam() == drop.GetServersideData().group)
                    {
                        if (__instance.equippedMeleeAsset.GUID == Gamemode.Config.Items.EntrenchingTool)
                        {
                            if (drop.model.TryGetComponent(out RepairableComponent repairable))
                                repairable.Repair(builder);
                            else if (drop.model.TryGetComponent(out BuildableComponent buildable))
                                buildable.IncrementBuildPoints(builder);
                            else if (drop.model.TryGetComponent(out FOBComponent radio))
                                radio.parent.Repair(builder);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SDG.Unturned.Rocket), "OnTriggerEnter")]
        [HarmonyPrefix]
        static void OnProjectileCollided(SDG.Unturned.Rocket __instance, Collider other)
        {
            if (__instance.gameObject.TryGetComponent(out ProjectileComponent projectile))
            {
                projectile.OnCollided(other);
            }
        }
    }
}