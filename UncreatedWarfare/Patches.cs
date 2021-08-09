using HarmonyLib;
using JetBrains.Annotations;
using Rocket.API;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Patches
    {
        public delegate void BarricadeDroppedEventArgs(BarricadeDrop drop, BarricadeRegion region, Barricade barricade, Vector3 point, Quaternion rotation, ulong owner, ulong group);
        public delegate void BarricadeDestroyedEventArgs(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant);
        public delegate void StructureDestroyedEventArgs(StructureData data, StructureDrop drop, uint instanceID);
        public delegate void BarricadeHealthEventArgs(BarricadeData data);
        public delegate void OnPlayerTogglesCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool allow);
        public delegate void OnPlayerSetsCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow);
        public delegate void BatteryStealingDelegate(SteamPlayer theif, ref bool allow);
        public delegate void PlayerTriedStoreItem(Player player, byte page, ItemJar jar, ref bool allow);
        public delegate void PlayerGesture(Player player, EPlayerGesture gesture, ref bool allow);
        public delegate void PlayerMarker(Player player, ref Vector3 position, ref string overrideText, ref bool isBeingPlaced, ref bool allowed);

        public static event BarricadeDestroyedEventArgs BarricadeDestroyedHandler;
        public static event StructureDestroyedEventArgs StructureDestroyedHandler;
        public static event OnPlayerTogglesCosmeticsDelegate OnPlayerTogglesCosmetics_Global;
        public static event OnPlayerSetsCosmeticsDelegate OnPlayerSetsCosmetics_Global;
        public static event BatteryStealingDelegate OnBatterySteal_Global;
        public static event PlayerTriedStoreItem OnPlayerTriedStoreItem_Global;
        public static event PlayerGesture OnPlayerGesture_Global;
        public static event PlayerMarker OnPlayerMarker_Global;

        /// <summary>
        /// Stores all <see cref="Harmony"/> patches.
        /// </summary>
        [HarmonyPatch]
        public static class InternalPatches
        {
            /// <summary>Patch methods</summary>
            public static void DoPatching()
            {
                Harmony harmony = new Harmony("net.uncreated.warfare");
                harmony.PatchAll();
            }
#pragma warning disable IDE0051
#pragma warning disable IDE0060 // Remove unused parameter
            internal static GameObject lastProjected;
            /*
            // SDG.Unturned.VehicleManager
            /// <summary>
            /// Prefix of <see cref="VehicleManager.getVehiclesInRadius(Vector3, float, List{InteractableVehicle})"/> to make it based off of a sphere collider instead of getting the center of vehicles.
            /// </summary>
            [HarmonyPatch(typeof(VehicleManager), "getVehiclesInRadius")]
            [HarmonyPrefix]
            static bool GetVehiclesInRadius(Vector3 center, float sqrRadius, List<InteractableVehicle> result)
            {
                Collider[] hits = Physics.OverlapSphere(center, Mathf.Sqrt(sqrRadius), RayMasks.VEHICLE);
                if (hits.Length == 0) return true;
                foreach (Collider hit in hits)
                {
                    InteractableVehicle[] vehicles = hit.gameObject.GetComponentsInParent<InteractableVehicle>();
                    foreach (InteractableVehicle vehicle in VehicleManager.vehicles)
                    {
                        
                    }
                    result.AddRange(vehicles.Where(x => !result.Contains(x)));
                }
                if (result.Count == 0) return true;
                return false;
            }
            */
            // SDG.Unturned.PlayerInventory
            /// <summary>
            /// Postfix of <see cref="PlayerInventory.closeStorage()"/> to stop the coroutine that auto-closes storages.
            /// </summary>
            [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.closeStorage))]
            [HarmonyPostfix]
            static void OnStopStoring(PlayerInventory __instance)
            {
                if (!UCWarfare.Config.Patches.closeStorage) return;
                UCPlayer player = UCPlayer.FromPlayer(__instance.player);
                if (player == null) return;
                if (player.StorageCoroutine != null)
                    player.Player.StopCoroutine(player.StorageCoroutine);
                return;
            }
            // SDG.Unturned.UseableGun
            /// <summary>
            /// Postfix of <see cref="UseableGun.project(Vector3, Vector3, ItemBarrelAsset, ItemMagazineAsset)"/> to predict mortar hits.
            /// </summary>
            [HarmonyPatch(typeof(UseableGun), "project")]
            [HarmonyPostfix]
            static void OnPostProjected(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
            {
                if (!(UCWarfare.Config.Patches.project && UCWarfare.Config.EnableMortarWarning)) return;
                if (lastProjected != null && lastProjected.activeInHierarchy)
                {
                    if (__instance.equippedGunAsset?.id == UCWarfare.Config.MortarWeapon)
                    {
                        Vector3 yaw = new Vector3(direction.x, 0, direction.z).normalized;
                        float dp = direction.x * yaw.x + direction.y * yaw.y + direction.z * yaw.z;
                        float angle = Mathf.Acos(dp / (direction.magnitude * yaw.magnitude)) - (Mathf.PI / 4);
                        float range = Mathf.Sin((2f * angle) - (Mathf.PI / 2f)) / (-(9.81f / (133.3f * 133.3f)));
                        Vector3 dest = origin + yaw * range;
                        // dest.y = F.GetTerrainHeightAt2DPoint(new Vector2(dest.x, dest.z)); (not needed)
                        Vector2 dest2d = new Vector2(dest.x, dest.z);
                        if (dest != Vector3.zero)
                        {
                            ulong team = __instance.channel.owner.GetTeam();
                            if (team == 1 || team == 2)
                            {
                                IEnumerator<WaitForSeconds> coroutine(GameObject obj)
                                {
                                    List<ulong> warned = new List<ulong>();
                                    while (obj != null)
                                    {
                                        IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
                                        while (players.MoveNext())
                                        {
                                            if (!warned.Contains(players.Current.playerID.steamID.m_SteamID) && players.Current.GetTeam() == team &&
                                                (new Vector2(players.Current.player.transform.position.x, players.Current.player.transform.position.z) - dest2d).sqrMagnitude <
                                                UCWarfare.Config.MortarWarningDistance * UCWarfare.Config.MortarWarningDistance)
                                            {
                                                ToastMessage.QueueMessage(players.Current, F.Translate("friendly_mortar_incoming", players.Current), ToastMessageSeverity.WARNING);
                                                warned.Add(players.Current.playerID.steamID.m_SteamID);
                                            }
                                        }
                                        players.Dispose();
                                        yield return new WaitForSeconds(1f);
                                    }
                                }
                                UCWarfare.I.StartCoroutine(coroutine(lastProjected));
                            }
                        }
                    }
                }
            }
            // SDG.Unturned.Provider
            /// <summary>
            /// Postfix of <see cref="Provider.verifyNextPlayerInQueue()"/> to check if the new player in the queue is an admin, then pass them.
            /// </summary>
            [HarmonyPatch(typeof(Provider), "verifyNextPlayerInQueue")]
            [HarmonyPostfix]
            static void OnPlayerEnteredQueue()
            {
                if (!UCWarfare.Config.Patches.EnableQueueSkip) return;
                if (Provider.pending.Count > 0)
                {
                    for (int i = 0; i < Provider.pending.Count; i++)
                    {
                        SteamPending pending = Provider.pending[i];
                        if (pending.hasSentVerifyPacket)
                            return;
                        RocketPlayer pl = new RocketPlayer(pending.playerID.steamID.m_SteamID.ToString(Data.Locale), pending.playerID.playerName, false);
                        if (pl.IsIntern() || pl.IsAdmin() ||   // checks for admin or intern status, then for 'HasQueueSkip' in the player's player save.
                           (PlayerManager.HasSave(pending.playerID.steamID.m_SteamID, out PlayerSave save) && save.HasQueueSkip))
                            pending.sendVerifyPacket();
                    }
                }
            }
            // SDG.Unturned.ChatManager
            /// <summary>
            /// Postfix of <see cref="ChatManager.ReceiveChatRequest(in ServerInvocationContext, byte, string)"/> to reroute local chats to squad.
            /// </summary>
            [HarmonyPatch(typeof(ChatManager), nameof(ChatManager.ReceiveChatRequest))]
            [HarmonyPrefix]
            static bool OnChatRequested(in ServerInvocationContext context, byte flags, string text)
            {
                if (!UCWarfare.Config.Patches.ReceiveChatRequest) return true;
                SteamPlayer callingPlayer = context.GetCallingPlayer();
                if (callingPlayer == null || callingPlayer.player == null || Time.realtimeSinceStartup - callingPlayer.lastChat < ChatManager.chatrate)
                    return false;
                callingPlayer.lastChat = Time.realtimeSinceStartup;
                EChatMode mode = (EChatMode)(flags & sbyte.MaxValue);
                bool fromUnityEvent = (flags & 128) > 0;
                if (text.Length < 2 || Dedicator.isDedicated & fromUnityEvent && !Provider.configData.UnityEvents.Allow_Client_Messages)
                    return false;
                text = text.Trim();
                if (text.Length < 2)
                    return false;
                if (text.Length > ChatManager.MAX_MESSAGE_LENGTH)
                    text = text.Substring(0, ChatManager.MAX_MESSAGE_LENGTH);
                if (CommandWindow.shouldLogChat)
                {
                    switch (mode)
                    {
                        case EChatMode.GLOBAL:
                            CommandWindow.Log(Provider.localization.format("Global", callingPlayer.playerID.characterName, callingPlayer.playerID.playerName, text));
                            break;
                        case EChatMode.LOCAL:
                            CommandWindow.Log(Provider.localization.format("Local", callingPlayer.playerID.characterName, callingPlayer.playerID.playerName, text));
                            break;
                        case EChatMode.GROUP:
                            CommandWindow.Log(Provider.localization.format("Group", callingPlayer.playerID.characterName, callingPlayer.playerID.playerName, text));
                            break;
                        default:
                            return false;
                    }
                }
                else if (mode != EChatMode.GLOBAL || mode != EChatMode.LOCAL || mode != EChatMode.GROUP) return false;
                if (fromUnityEvent)
                    UnturnedLog.info("UnityEventMsg {0}: '{1}'", callingPlayer.playerID.steamID, text);
                Color chatted = Color.white;
                ulong team = callingPlayer.GetTeam();
                chatted = Teams.TeamManager.GetTeamColor(team);
                if (callingPlayer.isAdmin && !Provider.hideAdmins)
                    chatted = Palette.ADMIN;
                bool isRich = false;
                bool isVisible = true;
                if (ChatManager.onChatted != null)
                    ChatManager.onChatted(callingPlayer, mode, ref chatted, ref isRich, text, ref isVisible);
                if (!(ChatManager.process(callingPlayer, text, fromUnityEvent) & isVisible))
                    return false;
                if (ChatManager.onServerFormattingMessage != null)
                {
                    ChatManager.onServerFormattingMessage(callingPlayer, mode, ref text);
                }
                else
                {
                    text = "%SPEAKER%: " + text;
                    if (mode != EChatMode.LOCAL)
                    {
                        if (mode == EChatMode.GROUP)
                            text = "[T] " + text;
                    }
                }
                if (mode == EChatMode.GLOBAL)
                    ChatManager.serverSendMessage(text, chatted, callingPlayer, mode: EChatMode.GLOBAL, useRichTextFormatting: isRich);
                else if (mode == EChatMode.LOCAL)
                {
                    float num = 16384f;
                    UCPlayer player = UCPlayer.FromSteamPlayer(callingPlayer);
                    if (player == null || player.Squad == null || player.Squad.Members == null)
                    {
                        foreach (SteamPlayer client in Provider.clients)
                        {
                            if (client.player != null && (double)(client.player.transform.position - callingPlayer.player.transform.position).sqrMagnitude < num)
                                ChatManager.serverSendMessage("[A] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                        }
                    } else
                    {
                        foreach (SteamPlayer client in Provider.clients)
                        {
                            if (player.Squad.Members.Exists(x => x.Steam64 == client.playerID.steamID.m_SteamID))
                                ChatManager.serverSendMessage("[S] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                            else if ((client.player != null && (client.player.transform.position - callingPlayer.player.transform.position).sqrMagnitude < num))
                                ChatManager.serverSendMessage("[A] " + text, chatted, callingPlayer, client, EChatMode.LOCAL, useRichTextFormatting: isRich);
                        }
                    }
                }
                else
                {
                    if (mode != EChatMode.GROUP || !(callingPlayer.player.quests.groupID != CSteamID.Nil))
                        return false;
                    foreach (SteamPlayer client in Provider.clients)
                    {
                        if (!(client.player == null) && client.player.quests.isMemberOfSameGroupAs(callingPlayer.player))
                            ChatManager.serverSendMessage(text, chatted, callingPlayer, client, EChatMode.GROUP, useRichTextFormatting: isRich);
                    }
                }
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
                if (!UCWarfare.Config.Patches.ReceiveGestureRequest) return true;
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
                if (!UCWarfare.Config.Patches.replicateSetMarker) return true;
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
                if (!UCWarfare.Config.Patches.ReceiveDragItem) return true;
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
                if (!UCWarfare.Config.Patches.requestGroupExit) return true;
                if (UCPlayer.FromPlayer(player).OnDutyOrAdmin()) return true;
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
                if (!UCWarfare.Config.Patches.ReceiveVisualToggleRequest) return true;
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
                if (!UCWarfare.Config.Patches.ServerSetVisualToggleState) return true;
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
                if (!UCWarfare.Config.Patches.ReceiveStealVehicleBattery) return true;
                bool allow = true;
                OnBatterySteal_Global?.Invoke(context.GetCallingPlayer(), ref allow);
                return allow;
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.ServerSetSignTextInternal(InteractableSign, BarricadeRegion, byte, byte, ushort, string)"/> to set translation data of signs.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "ServerSetSignTextInternal")]
            [HarmonyPrefix]
            static bool ServerSetSignTextInternalLang(InteractableSign sign, BarricadeRegion region, byte x, byte y, ushort plant, string trimmedText)
            {
                if (!UCWarfare.Config.Patches.ServerSetSignTextInternal) return true;
                if (trimmedText.StartsWith("sign_"))
                {
                    if (trimmedText.Length > 5)
                    {
                        if (Kits.KitManager.KitExists(trimmedText.Substring(5), out _))
                            Task.Run(async () => await F.InvokeSignUpdateForAllKits(sign, x, y, trimmedText));
                        else
                            Task.Run(async () => await F.InvokeSignUpdateForAll(sign, x, y, trimmedText));
                    } else
                        Task.Run(async () => await F.InvokeSignUpdateForAll(sign, x, y, trimmedText));
                    

                    BarricadeDrop drop = region.FindBarricadeByRootTransform(sign.transform);

                    byte[] state = drop.GetServersideData().barricade.state;
                    byte[] bytes = Encoding.UTF8.GetBytes(trimmedText);
                    byte[] numArray1 = new byte[17 + bytes.Length];
                    byte[] numArray2 = numArray1;
                    Buffer.BlockCopy(state, 0, numArray2, 0, 16);
                    numArray1[16] = (byte)bytes.Length;
                    if (bytes.Length != 0)
                        Buffer.BlockCopy(bytes, 0, numArray1, 17, bytes.Length);
                    drop.ReceiveUpdateState(numArray1);
                    sign.updateText(trimmedText);
                    return false;
                } else
                {
                    return true;
                }
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.SendRegion(SteamPlayer client, byte x, byte y, ushort plant)"/> to set translation data of signs.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPrefix]
            static bool SendRegion(SteamPlayer client, byte x, byte y, ushort plant, float sortOrder)
            {
                if (!UCWarfare.Config.Patches.SendRegion) return true;
                if (!BarricadeManager.tryGetRegion(x, y, plant, out BarricadeRegion region))
                    return false;
                if (region.drops.Count > 0)
                {
                    byte packet = 0;
                    int index = 0;
                    int count = 0;
                    while (index < region.drops.Count)
                    {
                        int num = 0;
                        while (count < region.drops.Count)
                        {
                            num += 38 + region.drops[count].GetServersideData().barricade.state.Length;
                            count++;
                            if (num > Block.BUFFER_SIZE / 2)
                                break;
                        }
                        Data.SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, writer =>
                        {
                            writer.WriteUInt8(x);
                            writer.WriteUInt8(y);
                            writer.WriteUInt16(plant);
                            writer.WriteUInt8(packet);
                            writer.WriteUInt16((ushort)(count - index));
                            writer.WriteFloat(sortOrder);
                            for (; index < count; ++index)
                            {
                                BarricadeDrop drop = region.drops[index];
                                BarricadeData serversideData = drop.GetServersideData();
                                InteractableStorage interactable = drop.interactable as InteractableStorage;
                                writer.WriteUInt16(serversideData.barricade.id);
                                if (interactable != null)
                                {
                                    byte[] bytes1;
                                    if (interactable.isDisplay)
                                    {
                                        byte[] bytes2 = Encoding.UTF8.GetBytes(interactable.displayTags);
                                        byte[] bytes3 = Encoding.UTF8.GetBytes(interactable.displayDynamicProps);
                                        bytes1 = new byte[20 + (interactable.displayItem != null ? interactable.displayItem.state.Length : 0) + 4 + 1 + bytes2.Length + 1 + bytes3.Length + 1];
                                        if (interactable.displayItem != null)
                                        {
                                            Array.Copy(BitConverter.GetBytes(interactable.displayItem.id), 0, bytes1, 16, 2);
                                            bytes1[18] = interactable.displayItem.quality;
                                            bytes1[19] = (byte)interactable.displayItem.state.Length;
                                            Array.Copy(interactable.displayItem.state, 0, bytes1, 20, interactable.displayItem.state.Length);
                                            Array.Copy(BitConverter.GetBytes(interactable.displaySkin), 0, bytes1, 20 + interactable.displayItem.state.Length, 2);
                                            Array.Copy(BitConverter.GetBytes(interactable.displayMythic), 0, bytes1, 20 + interactable.displayItem.state.Length + 2, 2);
                                            bytes1[20 + interactable.displayItem.state.Length + 4] = (byte)bytes2.Length;
                                            Array.Copy(bytes2, 0, bytes1, 20 + interactable.displayItem.state.Length + 5, bytes2.Length);
                                            bytes1[20 + interactable.displayItem.state.Length + 5 + bytes2.Length] = (byte)bytes3.Length;
                                            Array.Copy(bytes3, 0, bytes1, 20 + interactable.displayItem.state.Length + 5 + bytes2.Length + 1, bytes3.Length);
                                            bytes1[20 + interactable.displayItem.state.Length + 5 + bytes2.Length + 1 + bytes3.Length] = interactable.rot_comp;
                                        }
                                    }
                                    else
                                        bytes1 = new byte[16];
                                    Array.Copy(serversideData.barricade.state, 0, bytes1, 0, 16);
                                    writer.WriteUInt8((byte)bytes1.Length);
                                    writer.WriteBytes(bytes1);
                                }
                                else if (drop.interactable is InteractableSign sign)
                                {
                                    string newtext = sign.text;
                                    if (newtext.StartsWith("sign_"))
                                    {
                                        newtext = F.TranslateSign(newtext, client.playerID.steamID.m_SteamID, false).GetAwaiter().GetResult();
                                        // size is not allowed in signs.
                                        newtext.Replace("<size=", "");
                                        newtext.Replace("</size>", "");
                                    }
                                    byte[] state = region.drops[index].GetServersideData().barricade.state;
                                    byte[] textbytes = Encoding.UTF8.GetBytes(newtext);// F.ClampToByteCount(, byte.MaxValue - 18, out bool requiredClamping);
                                    /*if (requiredClamping)
                                    {
                                        F.LogWarning(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 18} UTF8 bytes (was {textbytes.Length} bytes), it was clamped to :" + Encoding.UTF8.GetString(textbytes));
                                    }*/
                                    if (textbytes.Length > byte.MaxValue - 18)
                                    {
                                        F.LogError(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 18} UTF8 bytes (was {textbytes.Length} bytes)!");
                                        textbytes = Encoding.UTF8.GetBytes(sign.text);
                                    }
                                    byte[] numArray1 = new byte[17 + textbytes.Length];
                                    numArray1[16] = (byte)textbytes.Length;
                                    if (textbytes.Length != 0)
                                        Buffer.BlockCopy(textbytes, 0, numArray1, 17, textbytes.Length);
                                    writer.WriteUInt8((byte)numArray1.Length);
                                    writer.WriteBytes(numArray1);
                                }
                                else
                                {
                                    writer.WriteUInt8((byte)serversideData.barricade.state.Length);
                                    writer.WriteBytes(serversideData.barricade.state);
                                }
                                writer.WriteClampedVector3(serversideData.point, fracBitCount: 11);
                                writer.WriteUInt8(serversideData.angle_x);
                                writer.WriteUInt8(serversideData.angle_y);
                                writer.WriteUInt8(serversideData.angle_z);
                                writer.WriteUInt8((byte)Mathf.RoundToInt(serversideData.barricade.health / (float)serversideData.barricade.asset.health * 100f));
                                writer.WriteUInt64(serversideData.owner);
                                writer.WriteUInt64(serversideData.group);
                                writer.WriteNetId(drop.GetNetId());
                            }
                        });
                        packet++;
                    }
                }
                else
                    Data.SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, writer =>
                    {
                        writer.WriteUInt8(x);
                        writer.WriteUInt8(y);
                        writer.WriteUInt16(plant);
                        writer.WriteUInt8(0);
                        writer.WriteUInt16(0);
                    });
                return false;
            }
            // SDG.Unturned.Bumper
            /// <summary>
            /// Adds the id of the vehicle that hit the player to their pt component.
            /// </summary>
            [HarmonyPatch(typeof(Bumper), "OnTriggerEnter")]
            [HarmonyPrefix]
            static bool TriggerEnterBumper(Collider other, InteractableVehicle ___vehicle)
            {
                if (!UCWarfare.Config.Patches.BumperOnTriggerEnter) return true;
                if (other == null || !Provider.isServer || ___vehicle == null || ___vehicle.asset == null || other.isTrigger || other.CompareTag("Debris"))
                    return false;
                if (other.transform.CompareTag("Player"))
                {
                    if (___vehicle.isDriven)
                    {
                        if (___vehicle.asset.engine != EEngine.HELICOPTER && ___vehicle.asset.engine != EEngine.PLANE)
                        {
                            Player hit = DamageTool.getPlayer(other.transform);
                            Player driver = ___vehicle.passengers[0].player.player;
                            if (hit == null || driver == null || hit.movement.getVehicle() != null || !DamageTool.isPlayerAllowedToDamagePlayer(driver, hit)) return true;
                            if (F.TryGetPlaytimeComponent(driver, out PlaytimeComponent c))
                            {
                                c.lastRoadkilled = ___vehicle.asset.id;
                            }
                        } else if (___vehicle.speed <= 10.0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            // SDG.Unturned.InteractableTrap
            /// <summary>
            /// Prefix of <see cref="InteractableTrap.OnTriggerEnter(Collider other)"/> to set the killer to the player that placed the landmine.
            /// </summary>
            [HarmonyPatch(typeof(InteractableTrap), "OnTriggerEnter")]
            [HarmonyPrefix]
            static bool LandmineExplodeOverride(Collider other, InteractableTrap __instance, float ___lastActive, float ___setupDelay,
                bool ___isExplosive, ushort ___explosion2, float ___playerDamage, float ___zombieDamage,
                float ___animalDamage, float ___barricadeDamage, float ___structureDamage,
                float ___vehicleDamage, float ___resourceDamage, float ___objectDamage,
                float ___range2, bool ___isBroken, ref float ___lastTriggered)
            {
                if (!UCWarfare.Config.Patches.UseableTrapOnTriggerEnter) return true;
                if (other.isTrigger || Time.realtimeSinceStartup - ___lastActive < ___setupDelay || __instance.transform.parent != null && other.transform == __instance.transform.parent || !Provider.isServer)
                    return false;
                CSteamID owner = CSteamID.Nil;
                BarricadeOwnerDataComponent OwnerComponent = null;
                if (Data.OwnerComponents != null && __instance.transform != null)
                {
                    int c = Data.OwnerComponents.FindIndex(x => x != null && x.transform != null && x.transform.position == __instance.transform.position);
                    if (c != -1)
                    {
                        owner = Data.OwnerComponents[c].ownerCSID;
                        OwnerComponent = Data.OwnerComponents[c];
                        UnityEngine.Object.Destroy(Data.OwnerComponents[c]);
                        Data.OwnerComponents.RemoveAt(c);
                    }
                }
                ___lastTriggered = Time.realtimeSinceStartup;
                if (___isExplosive) // if hurts all in range, makes explosion
                {
                    if (other.transform.CompareTag("Player")) // if player hit.
                    {
                        if (!Provider.isPvP || !(other.transform.parent == null) && other.transform.parent.CompareTag("Vehicle"))
                            return false;
                        if (other.transform.TryGetComponent(out Player player))
                        {
                            if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c))
                                if (OwnerComponent != null)
                                    c.LastLandmineTriggered = new LandmineDataForPostAccess(__instance, OwnerComponent);
                        }
                        EffectManager.sendEffect(___explosion2, EffectManager.LARGE, __instance.transform.position); // fix vehicles exploded by landmines not applying proper kills.
                        DamageTool.explode(__instance.transform.position, ___range2, EDeathCause.LANDMINE, owner, ___playerDamage, ___zombieDamage, ___animalDamage, ___barricadeDamage, ___structureDamage, ___vehicleDamage, ___resourceDamage, ___objectDamage, out List<EPlayerKill> _, damageOrigin: EDamageOrigin.Trap_Explosion);
                    }
                    else
                    {
                        if (other.gameObject.TryGetComponent(out ThrowableOwnerDataComponent throwable))
                        {
                            if (UCWarfare.Config.Debug)
                                F.Log("Found Throwable " + throwable.owner.channel.owner.playerID.playerName, ConsoleColor.DarkGray);
                            if (F.TryGetPlaytimeComponent(throwable.owner, out PlaytimeComponent c))
                                c.LastLandmineTriggered = new LandmineDataForPostAccess(__instance, OwnerComponent);
                        }
                        EffectManager.sendEffect(___explosion2, EffectManager.LARGE, __instance.transform.position);
                        DamageTool.explode(__instance.transform.position, ___range2, EDeathCause.LANDMINE, owner, ___playerDamage, ___zombieDamage, ___animalDamage, ___barricadeDamage, ___structureDamage, ___vehicleDamage, ___resourceDamage, ___objectDamage, out List<EPlayerKill> _, damageOrigin: EDamageOrigin.Trap_Explosion);
                    }
                }
                else if (other.transform.CompareTag("Player")) // else if hurts only trapped
                {
                    if (!Provider.isPvP || other.transform.parent != null && other.transform.parent.CompareTag("Vehicle"))
                        return false;
                    Player player = DamageTool.getPlayer(other.transform);
                    if (player == null) return false;
                    if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c))
                        if (OwnerComponent != null)
                            c.LastLandmineTriggered = new LandmineDataForPostAccess(__instance, OwnerComponent);
                    DamageTool.damage(player, EDeathCause.SHRED, ELimb.SPINE, owner, Vector3.up, ___playerDamage, 1f, out EPlayerKill _, trackKill: true);
                    if (___isBroken)
                        player.life.breakLegs();
                    EffectManager.sendEffect(5, EffectManager.SMALL, __instance.transform.position + Vector3.up, Vector3.down);
                    BarricadeManager.damage(__instance.transform.parent, 5f, 1f, false, damageOrigin: EDamageOrigin.Trap_Wear_And_Tear);
                }
                else
                {
                    if (!other.transform.CompareTag("Agent")) return false;
                    Zombie zombie = DamageTool.getZombie(other.transform);
                    if (zombie != null)
                    {
                        DamageTool.damageZombie(new DamageZombieParameters(zombie, __instance.transform.forward, ___zombieDamage)
                        {
                            instigator = __instance
                        }, out EPlayerKill _, out uint _);
                        EffectManager.sendEffect(zombie.isRadioactive ? (ushort)95 : (ushort)5, EffectManager.SMALL, __instance.transform.position + Vector3.up, Vector3.down);
                        BarricadeManager.damage(__instance.transform.parent, zombie.isHyper ? 10f : 5f, 1f, false, damageOrigin: EDamageOrigin.Trap_Wear_And_Tear);
                    }
                    else
                    {
                        Animal animal = DamageTool.getAnimal(other.transform);
                        if (animal == null) return false;
                        DamageTool.damageAnimal(new DamageAnimalParameters(animal, __instance.transform.forward, ___animalDamage)
                        {
                            instigator = __instance
                        }, out EPlayerKill _, out uint _);
                        EffectManager.sendEffect(5, EffectManager.SMALL, __instance.transform.position + Vector3.up, Vector3.down);
                        BarricadeManager.damage(__instance.transform.parent, 5f, 1f, false, instigatorSteamID: owner, damageOrigin: EDamageOrigin.Trap_Wear_And_Tear);
                    }
                }
                return false;
            }
            // SDG.Unturned.PlayerLife
            /// <summary>
            /// Turn off bleeding for the real function.
            /// </summary>
            [HarmonyPatch(typeof(PlayerLife), "simulate")]
            [HarmonyPrefix]
            static bool SimulatePlayerLifePre(uint simulation, PlayerLife __instance, uint ___lastBleed, ref bool ____isBleeding, ref uint ___lastRegenerate)
            {
                if (!UCWarfare.Config.Patches.simulatePlayerLife) return true;
                if (Provider.isServer)
                {
                    if (Level.info.type == ELevelType.SURVIVAL)
                    {
                        if (__instance.isBleeding)
                        {
                            if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                            {
                                if (Data.ReviveManager != null && Data.ReviveManager.DownedPlayers.ContainsKey(__instance.player.channel.owner.playerID.steamID.m_SteamID))
                                {
                                    ____isBleeding = false;
                                    ___lastRegenerate = simulation; // reset last regeneration to stop it from regenerating hp since it thinks the player isnt bleeding.
                                }
                            }
                        }
                    }
                }
                return true;
            }
            // SDG.Unturned.PlayerLife
            /// <summary>
            /// Turn back on bleeding and apply fix.
            /// </summary>
            [HarmonyPatch(typeof(PlayerLife), "simulate")]
            [HarmonyPostfix]
            static void SimulatePlayerLifePost(uint simulation, PlayerLife __instance, ref uint ___lastBleed, ref bool ____isBleeding)
            {
                if (!UCWarfare.Config.Patches.simulatePlayerLife) return;
                if (Provider.isServer)
                {
                    if (Level.info.type == ELevelType.SURVIVAL)
                    {
                        if (!__instance.isBleeding)
                        {
                            if (simulation - ___lastBleed > Provider.modeConfigData.Players.Bleed_Damage_Ticks)
                            {
                                if (Data.ReviveManager != null && Data.ReviveManager.DownedPlayers.ContainsKey(__instance.player.channel.owner.playerID.steamID.m_SteamID))
                                {
                                    ___lastBleed = simulation;
                                    ____isBleeding = true;
                                    DamagePlayerParameters p = Data.ReviveManager.DownedPlayers[__instance.player.channel.owner.playerID.steamID.m_SteamID];
                                    __instance.askDamage(1, p.direction, p.cause, p.limb, p.killer, out EPlayerKill _, canCauseBleeding: false, bypassSafezone: true);
                                }
                            }
                        }
                    }
                }
            }
            // SDG.Unturned.VehicleManager
            [HarmonyPatch(typeof(VehicleManager), nameof(VehicleManager.damage))]
            [HarmonyPrefix]
            static bool DamageVehicle(InteractableVehicle vehicle, float damage, float times, bool canRepair, CSteamID instigatorSteamID, EDamageOrigin damageOrigin)
            {
                if (!UCWarfare.Config.Patches.damageVehicleTool) return true;
                if (vehicle == null || vehicle.asset == null || vehicle.isDead) return false;
                if (!vehicle.asset.isVulnerable && !vehicle.asset.isVulnerableToExplosions && !vehicle.asset.isVulnerableToEnvironment)
                {
                    UnturnedLog.error("Somehow tried to damage completely invulnerable vehicle: " + vehicle + " " + damage + " " + times + " " + canRepair.ToString());
                    return false;
                }
                float newtimes = times * Provider.modeConfigData.Vehicles.Armor_Multiplier;
                if (Mathf.RoundToInt(damage * newtimes) >= vehicle.health)
                {
                    if (instigatorSteamID != default && instigatorSteamID != CSteamID.Nil)
                    {
                        if (vehicle.gameObject.TryGetComponent(out VehicleDamageOwnerComponent vc))
                        {
                            vc.owner = instigatorSteamID;
                        }
                        else
                        {
                            vc = vehicle.gameObject.AddComponent<VehicleDamageOwnerComponent>();
                            vc.owner = instigatorSteamID;
                            if (damageOrigin == EDamageOrigin.Grenade_Explosion)
                            {
                                if (F.TryGetPlaytimeComponent(instigatorSteamID, out PlaytimeComponent c))
                                {
                                    ThrowableOwnerDataComponent a = c.thrown.FirstOrDefault(x => x.asset.isExplosive);
                                    if (a != null)
                                        vc.item = a.asset.id;
                                }
                            } else if (damageOrigin == EDamageOrigin.Rocket_Explosion)
                            {
                                if (F.TryGetPlaytimeComponent(instigatorSteamID, out PlaytimeComponent c))
                                {
                                    vc.item = c.lastProjected;
                                }
                            } else if (damageOrigin == EDamageOrigin.Vehicle_Bumper)
                            {
                                if (F.TryGetPlaytimeComponent(instigatorSteamID, out PlaytimeComponent c))
                                {
                                    vc.item = c.lastExplodedVehicle;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (vehicle.gameObject.TryGetComponent(out VehicleDamageOwnerComponent vc))
                        {
                            UnityEngine.Object.Destroy(vc);
                        }
                    }
                }
                return true;
            }
            // SDG.Unturned.InteractableVehicle
            /// <summary>
            /// Call event before vehicle explode
            /// </summary>
            [HarmonyPatch(typeof(InteractableVehicle), "explode")]
            [HarmonyPrefix]
            static bool ExplodeVehicle(InteractableVehicle __instance)
            {
                if (!UCWarfare.Config.Patches.explodeInteractableVehicle) return true;
                if (!__instance.asset.ShouldExplosionCauseDamage) return true;
                CSteamID instigator = CSteamID.Nil;
                if (__instance.gameObject.TryGetComponent(out VehicleDamageOwnerComponent vc))
                {
                    instigator = vc.owner;
                } else
                {
                    if (__instance.passengers.Length > 0)
                    {
                        if (__instance.passengers[0].player != null)
                            instigator = __instance.passengers[0].player.playerID.steamID;
                    }
                }
                Vector3 force = new Vector3(UnityEngine.Random.Range(__instance.asset.minExplosionForce.x, __instance.asset.maxExplosionForce.x), UnityEngine.Random.Range(__instance.asset.minExplosionForce.y, __instance.asset.maxExplosionForce.y), UnityEngine.Random.Range(__instance.asset.minExplosionForce.z, __instance.asset.maxExplosionForce.z));
                __instance.GetComponent<Rigidbody>().AddForce(force);
                __instance.GetComponent<Rigidbody>().AddTorque(16f, 0.0f, 0.0f);
                __instance.dropTrunkItems();
                if (F.TryGetPlaytimeComponent(instigator, out PlaytimeComponent c))
                {
                    c.lastExplodedVehicle = __instance.asset.id;
                }
                DamageTool.explode(__instance.transform.position, 8f, EDeathCause.VEHICLE, instigator, 200f, 200f, 200f, 0.0f, 0.0f, 500f, 2000f, 500f, out _, damageOrigin: EDamageOrigin.Vehicle_Explosion);
                for (int index = 0; index < __instance.passengers.Length; ++index)
                {
                    Passenger passenger = __instance.passengers[index];
                    if (passenger != null && passenger.player != null && passenger.player.player != null && !passenger.player.player.life.isDead)
                    {
                        if (UCWarfare.Config.Debug)
                            F.Log($"Damaging passenger {F.GetPlayerOriginalNames(passenger.player).PlayerName}: {instigator}", ConsoleColor.DarkGray);
                        if (__instance.asset.ShouldExplosionCauseDamage)
                            passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SKULL, instigator, out _);
                        else
                            VehicleManager.forceRemovePlayer(__instance, passenger.player.playerID.steamID);
                    }
                }
                if (__instance.asset.dropsTableId > 0)
                {
                    int num = Mathf.Clamp(UnityEngine.Random.Range(__instance.asset.dropsMin, __instance.asset.dropsMax), 0, 100);
                    for (int index = 0; index < num; ++index)
                    {
                        float f = UnityEngine.Random.Range(0.0f, 6.283185f);
                        ushort newID = SpawnTableTool.resolve(__instance.asset.dropsTableId);
                        if (newID != 0)
                            ItemManager.dropItem(new Item(newID, EItemOrigin.NATURE), __instance.transform.position + new Vector3(Mathf.Sin(f) * 3f, 1f, Mathf.Cos(f) * 3f), false, Dedicator.isDedicated, true);
                    }
                }
                VehicleManager.sendVehicleExploded(__instance);
                if (__instance.asset.explosion == 0)
                    return false;
                EffectManager.sendEffect(__instance.asset.explosion, EffectManager.LARGE, __instance.transform.position);
                return false;
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.destroyBarricade(BarricadeRegion, byte, byte, ushort, ushort)"/> to invoke <see cref="BarricadeDestroyedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), nameof(BarricadeManager.destroyBarricade), typeof(BarricadeDrop), typeof(byte), typeof(byte), typeof(ushort))]
            [HarmonyPrefix]
            static void DestroyBarricadePostFix(BarricadeDrop barricade, byte x, byte y, ushort plant)
            {
                if (!UCWarfare.Config.Patches.destroyBarricade) return;
                BarricadeDestroyedHandler?.Invoke(barricade.GetServersideData(), barricade, barricade.GetServersideData().instanceID, plant);
            }
            // SDG.Unturned.StructureManager
            /// <summary>
            /// Prefix of <see cref="StructureManager.destroyStructure(StructureRegion, byte, byte, ushort, Vector3)"/> to invoke <see cref="StructureDestroyedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(StructureManager), nameof(StructureManager.destroyStructure), typeof(StructureDrop), typeof(byte), typeof(byte), typeof(Vector3))]
            [HarmonyPrefix]
            static void DestroyStructurePostFix(StructureDrop structure, byte x, byte y, Vector3 ragdoll)
            {
                if (!UCWarfare.Config.Patches.destroyStructure) return;
                StructureDestroyedHandler?.Invoke(structure.GetServersideData(), structure, structure.GetServersideData().instanceID);
            }
            // SDG.Unturned.PlayerLife
            /// <summary>Prefix of <see cref="PlayerLife.askStarve(byte)"/> to invoke prevent starving in main base.</summary>
            [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askStarve))]
            [HarmonyPrefix]
            static bool OnPlayerFoodTick(byte amount, PlayerLife __instance) => !UCWarfare.Config.Patches.askStarve || !Teams.TeamManager.IsInMainOrLobby(__instance.player);

            // SDG.Unturned.PlayerLife
            /// <summary>Prefix of <see cref="PlayerLife.askDehydrate(byte)"/> to invoke prevent dehydrating in main base.</summary>
            [HarmonyPatch(typeof(PlayerLife), nameof(PlayerLife.askDehydrate))]
            [HarmonyPrefix]
            static bool OnPlayerWaterTick(byte amount, PlayerLife __instance) => !UCWarfare.Config.Patches.askDehydrate || !Teams.TeamManager.IsInMainOrLobby(__instance.player);
#pragma warning restore IDE0051
#pragma warning restore IDE0060 // Remove unused parameter
        }
    }
}
