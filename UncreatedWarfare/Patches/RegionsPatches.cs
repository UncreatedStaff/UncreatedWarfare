using HarmonyLib;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Traits;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    [HarmonyPatch]
    public class RegionsPatches
    {
        // SDG.Unturned.BarricadeManager
        /// <summary>
        /// Prefix of <see cref="BarricadeManager.ServerSetSignTextInternal(InteractableSign, BarricadeRegion, byte, byte, ushort, string)"/> to set translation data of signs.
        /// </summary>
        
        [HarmonyPatch(typeof(BarricadeManager), "ServerSetSignTextInternal")]
        [HarmonyPrefix]
        static bool ServerSetSignTextInternalLang(InteractableSign sign, BarricadeRegion region, byte x, byte y, ushort plant, string trimmedText)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (trimmedText.StartsWith(Signs.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                BarricadeDrop drop = region.FindBarricadeByRootTransform(sign.transform);
                if (drop == null)
                    return false;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(trimmedText);
                byte[] newState = new byte[sizeof(ulong) * 2 + 1 + bytes.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(drop.GetServersideData().owner), 0, newState, 0, sizeof(ulong));
                Buffer.BlockCopy(BitConverter.GetBytes(Teams.TeamManager.GetGroupID(3ul)), 0, newState, sizeof(ulong), sizeof(ulong));
                newState[sizeof(ulong) * 2] = (byte)bytes.Length;
                if (bytes.Length != 0)
                    Buffer.BlockCopy(bytes, 0, newState, sizeof(ulong) * 2 + 1, bytes.Length);
                BarricadeManager.updateState(drop.model, newState, newState.Length);
                sign.updateState(drop.asset, newState);
                Signs.CheckSign(drop);
                StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
                if (saver != null && saver.TryGetSaveNoLock(drop, out SavedStructure structure))
                {
                    structure.Metadata = Util.CloneBytes(newState);
                    Task.Run(() => Util.TryWrap(saver.AddOrUpdate(structure), "Error saving structure."));
                }

                if (TraitManager.Loaded && trimmedText.StartsWith(Signs.Prefix + Signs.TraitPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    TraitData? d = TraitManager.GetData(trimmedText.Substring(Signs.Prefix.Length + Signs.TraitPrefix.Length));
                    if (d != null)
                        TraitSigns.InitTraitSign(d, drop);
                    else
                        TraitSigns.TryRemoveComponent(drop);
                }
                else TraitSigns.TryRemoveComponent(drop);
                return false;
            }
            else
            {
                return true;
            }
        }
        
        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.ReceiveTakeItemRequest))]
        [HarmonyPrefix]
        static void OnItemDropRemovedPrefix(
            ref ItemData? __state,
            in ServerInvocationContext context,
            byte x,
            byte y,
            uint instanceID,
            byte to_x,
            byte to_y,
            byte to_rot,
            byte to_page)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            __state = null;

            ItemRegion region = ItemManager.regions[x, y];
            for (ushort index = 0; index < region.items.Count; ++index)
            {
                ItemData itemData = region.items[index];
                if (itemData.instanceID == instanceID)
                {
                    __state = itemData;
                }
            }
        }

        [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.ReceiveTakeItemRequest))]
        [HarmonyPostfix]
        static void OnItemDropRemovedPostfix(
            ItemData? __state,
            in ServerInvocationContext context,
            byte x,
            byte y,
            uint instanceID,
            byte to_x,
            byte to_y,
            byte to_rot,
            byte to_page)
        {
            EventFunctions.DroppedItemsOwners.Remove(instanceID);
        }

        // SDG.Unturned.BarricadeManager
        /// <summary>
        /// Prefix of <see cref="BarricadeManager.SendRegion(SteamPlayer, BarricadeRegion, byte, byte, NetId, float)"/> to set translation data of signs.
        /// </summary>
        [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
        [HarmonyPrefix]
        static bool SendRegion(SteamPlayer client, BarricadeRegion region, byte x, byte y, NetId parentNetId, float sortOrder)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? pl = UCPlayer.FromSteamPlayer(client);
            if (pl == null) return true;
            if (region.drops.Count > 0)
            {
                byte packet = 0;
                int index = 0;
                int count = 0;
                while (index < region.drops.Count)
                {
                    int num = 0;
                    bool hasSign = false;
                    while (count < region.drops.Count)
                    {
                        num += 44 + region.drops[count].GetServersideData().barricade.state.Length;
                        if (!hasSign)
                            hasSign = region.drops[count].interactable is InteractableSign;
                        count++;
                        if (num > Block.BUFFER_SIZE / 2)
                            break;
                    }
                    Data.SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, writer =>
                    {
                        writer.WriteUInt8(x);
                        writer.WriteUInt8(y);
                        writer.WriteNetId(parentNetId);
                        writer.WriteUInt8(packet);
                        writer.WriteUInt16((ushort)(count - index));
                        writer.WriteFloat(sortOrder);
                        for (; index < count; ++index)
                        {
                            BarricadeDrop drop = region.drops[index];
                            BarricadeData serversideData = drop.GetServersideData();
                            writer.WriteGuid(drop.asset.GUID);
                            if (drop.interactable is InteractableStorage interactable)
                            {
                                byte[] bytes1;
                                if (interactable.isDisplay)
                                {
                                    byte[] bytes2 = System.Text.Encoding.UTF8.GetBytes(interactable.displayTags);
                                    byte[] bytes3 = System.Text.Encoding.UTF8.GetBytes(interactable.displayDynamicProps);
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
                                goto skip;
                            }
                            if (drop.interactable is InteractableSign sign)
                            {
                                string newtext = sign.text;
                                if (!newtext.StartsWith(Signs.Prefix, StringComparison.OrdinalIgnoreCase))
                                    goto writeState;
                                newtext = Signs.GetClientText(drop, pl);
                                byte[] textbytes = System.Text.Encoding.UTF8.GetBytes(newtext);
                                byte[] state = serversideData.barricade.state;
                                if (textbytes.Length > byte.MaxValue - 17)
                                {
                                    L.LogError(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 17} UTF8 bytes (was {textbytes.Length} bytes)!");
                                    goto writeState;
                                }
                                byte[] numArray1 = new byte[17 + textbytes.Length];
                                Buffer.BlockCopy(state, 0, numArray1, 0, 16);
                                numArray1[16] = (byte)textbytes.Length;
                                if (textbytes.Length != 0)
                                    Buffer.BlockCopy(textbytes, 0, numArray1, 17, textbytes.Length);
                                writer.WriteUInt8((byte)numArray1.Length);
                                writer.WriteBytes(numArray1);
                                goto skip;
                            }
                            writeState:
                            writer.WriteUInt8((byte)serversideData.barricade.state.Length);
                            writer.WriteBytes(serversideData.barricade.state);
                            skip:
                            writer.WriteClampedVector3(serversideData.point, fracBitCount: 11);
                            writer.WriteSpecialYawOrQuaternion(serversideData.rotation);
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
                    writer.WriteNetId(NetId.INVALID);
                    writer.WriteUInt8(0);
                    writer.WriteUInt16(0);
                });
            return false;
        }
    }
}
