using HarmonyLib;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static partial class Patches
    {
        [HarmonyPatch]
        public class RegionsPatches
        {
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
                            F.InvokeSignUpdateForAllKits(sign, x, y, trimmedText);
                        else
                            F.InvokeSignUpdateForAll(sign, x, y, trimmedText);
                    }
                    else
                        F.InvokeSignUpdateForAll(sign, x, y, trimmedText);


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
                }
                else
                {
                    return true;
                }
            }

            [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.ReceiveTakeItemRequest))]
            [HarmonyPrefix]
            static void OnItemDropRemovedPrefix(
                ref SDG.Unturned.ItemData __state,
                in ServerInvocationContext context,
                byte x,
                byte y,
                uint instanceID,
                byte to_x,
                byte to_y,
                byte to_rot,
                byte to_page)
            {
                __state = null;

                ItemRegion region = ItemManager.regions[x, y];
                for (ushort index = 0; index < region.items.Count; ++index)
                {
                    SDG.Unturned.ItemData itemData = region.items[index];
                    if (itemData.instanceID == instanceID)
                    {
                        __state = itemData;
                    }
                }
            }

            [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.ReceiveTakeItemRequest))]
            [HarmonyPostfix]
            static void OnItemDropRemovedPostfix(
                SDG.Unturned.ItemData __state,
                in ServerInvocationContext context,
                byte x,
                byte y,
                uint instanceID,
                byte to_x,
                byte to_y,
                byte to_rot,
                byte to_page)
            {
                if (__state != null)
                {
                    FOBs.FOBManager.OnItemRemoved(__state);
                }
            }

            [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.dropItem))]
            [HarmonyPostfix]
            static void OnItemDropDropped(
                Item item,
                Vector3 point,
                bool playEffect,
                bool isDropped,
                bool wideSpread)
            {
                FOBs.FOBManager.OnItemDropped(item, point);
            }

            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.SendRegion(SteamPlayer client, byte x, byte y, ushort plant)"/> to set translation data of signs.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPrefix]
            static bool SendRegion(SteamPlayer client, BarricadeRegion region, byte x, byte y, NetId parentNetId, float sortOrder)
            {
                if (!UCWarfare.Config.Patches.SendRegion) return true;
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
                            writer.WriteNetId(parentNetId);
                            writer.WriteUInt8(packet);
                            writer.WriteUInt16((ushort)(count - index));
                            writer.WriteFloat(sortOrder);
                            for (; index < count; ++index)
                            {
                                BarricadeDrop drop = region.drops[index];
                                SDG.Unturned.BarricadeData serversideData = drop.GetServersideData();
                                InteractableStorage interactable = drop.interactable as InteractableStorage;
                                writer.WriteGuid(drop.asset.GUID);
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
                                        newtext = Translation.TranslateSign(newtext, client.playerID.steamID.m_SteamID, false);
                                        // size is not allowed in signs.
                                        newtext.Replace("<size=", "");
                                        newtext.Replace("</size>", "");
                                    }
                                    byte[] state = region.drops[index].GetServersideData().barricade.state;
                                    byte[] textbytes = Encoding.UTF8.GetBytes(newtext);// F.ClampToByteCount(, byte.MaxValue - 18, out bool requiredClamping);
                                    /*if (requiredClamping)
                                    {
                                        L.LogWarning(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 18} UTF8 bytes (was {textbytes.Length} bytes), it was clamped to :" + Encoding.UTF8.GetString(textbytes));
                                    }*/
                                    if (textbytes.Length > byte.MaxValue - 18)
                                    {
                                        L.LogError(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 18} UTF8 bytes (was {textbytes.Length} bytes)!");
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
                                writer.WriteUInt8((byte)Mathf.RoundToInt((float)((double)serversideData.barricade.health / (double)serversideData.barricade.asset.health * 100.0)));
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
}
