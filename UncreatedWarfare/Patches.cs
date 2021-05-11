using HarmonyLib;
using JetBrains.Annotations;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare
{
    public static class Patches
    {
        public delegate void BarricadeDroppedHandler(BarricadeRegion region, BarricadeData data);
        public delegate void BarricadeDestroyedHandler(BarricadeData data, uint instanceID);
        public delegate void BarricadeHealthChangedHandler(BarricadeData data);

        public static event BarricadeDroppedHandler barricadeSpawnedHandler;
        public static event BarricadeDestroyedHandler barricadeDestroyedHandler;
        public static event BarricadeHealthChangedHandler barricadeHealthChangedHandler;

        [HarmonyPatch]
        public static class InternalPatches
        {
            public static void DoPatching()
            {
                var harmony = new Harmony("com.company.project.product");
                harmony.PatchAll();
            }

            [HarmonyPatch(typeof(BarricadeManager), "ServerSetSignTextInternal")]
            [HarmonyPrefix]
            static bool ServerSetSignTextInternalLang(InteractableSign sign, BarricadeRegion region, byte x, byte y, ushort plant, ushort index, string trimmedText)
            {
                CommandWindow.LogError("Updating sign with text " + trimmedText);
                if (trimmedText.StartsWith("sign_"))
                {
                    F.InvokeSignUpdateForAll(x, y, plant, index, trimmedText, region);
                    CommandWindow.LogError("skipping original");
                    byte[] state = region.barricades[index].barricade.state;
                    byte[] bytes = Encoding.UTF8.GetBytes(trimmedText);
                    byte[] numArray1 = new byte[17 + bytes.Length];
                    byte[] numArray2 = numArray1;
                    Buffer.BlockCopy(state, 0, numArray2, 0, 16);
                    numArray1[16] = (byte)bytes.Length;
                    if (bytes.Length != 0)
                        Buffer.BlockCopy(bytes, 0, numArray1, 17, bytes.Length);
                    region.barricades[index].barricade.state = numArray1;
                    sign.updateText(trimmedText);
                    return false;
                } else
                {
                    CommandWindow.LogError("running original");
                    return true;
                }
            }
            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPrefix]
            static bool SendRegion(SteamPlayer client, byte x, byte y, ushort plant)
            {
                BarricadeRegion region;
                if (!BarricadeManager.tryGetRegion(x, y, plant, out region))
                    return false;
                if (!region.barricades.Exists(b => b.IsSign(region))) return true; // run base function if there are no signs.
                if (region.barricades.Count > 0 && region.drops.Count == region.barricades.Count)
                {
                    byte packet = 0;
                    int index = 0;
                    int count = 0;
                    while (index < region.barricades.Count)
                    {
                        int num = 0;
                        while (count < region.barricades.Count)
                        {
                            num += 38 + region.barricades[count].barricade.state.Length;
                            count++;
                            if (num > Block.BUFFER_SIZE / 2)
                                break;
                        }
                        UCWarfare.SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, (writer =>
                        {
                            writer.WriteUInt8(x);
                            writer.WriteUInt8(y);
                            writer.WriteUInt16(plant);
                            writer.WriteUInt8(packet);
                            writer.WriteUInt16((ushort)(count - index));
                            for (; index < count; ++index)
                            {
                                BarricadeData barricade = region.barricades[index];
                                InteractableStorage interactable = region.drops[index].interactable as InteractableStorage;
                                writer.WriteUInt16(barricade.barricade.id);
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
                                    Array.Copy(barricade.barricade.state, 0, bytes1, 0, 16);
                                    writer.WriteUInt8((byte)bytes1.Length);
                                    writer.WriteBytes(bytes1);
                                }
                                else
                                {
                                    if (region.drops[index].interactable.GetType() == typeof(InteractableSign))
                                    {
                                        InteractableSign sign = region.drops[index].interactable as InteractableSign;
                                        string newtext = sign.text;
                                        if (newtext.StartsWith("sign_"))
                                            newtext = F.Translate(newtext, client.playerID.steamID.m_SteamID);
                                        byte[] state = region.barricades[index].barricade.state;
                                        byte[] bytes = Encoding.UTF8.GetBytes(newtext);
                                        byte[] numArray1 = new byte[17 + bytes.Length];
                                        byte[] numArray2 = numArray1;
                                        Buffer.BlockCopy(state, 0, numArray2, 0, 16);
                                        numArray1[16] = (byte)bytes.Length;
                                        if (bytes.Length != 0)
                                            Buffer.BlockCopy(bytes, 0, numArray1, 17, bytes.Length);
                                        writer.WriteUInt8((byte)numArray1.Length);
                                        writer.WriteBytes(numArray1);
                                    }
                                    else
                                    {
                                        writer.WriteUInt8((byte)barricade.barricade.state.Length);
                                        writer.WriteBytes(barricade.barricade.state);
                                    }
                                }
                                writer.WriteClampedVector3(barricade.point, fracBitCount: 11);
                                writer.WriteUInt8(barricade.angle_x);
                                writer.WriteUInt8(barricade.angle_y);
                                writer.WriteUInt8(barricade.angle_z);
                                writer.WriteUInt8((byte)Mathf.RoundToInt((float)(barricade.barricade.health / barricade.barricade.asset.health * 100.0)));
                                writer.WriteUInt64(barricade.owner);
                                writer.WriteUInt64(barricade.group);
                                writer.WriteUInt32(barricade.instanceID);
                            }
                        }));
                        packet++;
                    }
                    return false;
                }
                else return true; // no barricades
            }

            [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
            [HarmonyPostfix]
            [UsedImplicitly]
            static void DropBarricadePostFix(BarricadeRegion region, BarricadeData data, ref Transform result, ref uint instanceID)
            {
                if (result == null) return;

                var drop = region.drops.LastOrDefault();

                if (drop?.instanceID == instanceID)
                {
                    barricadeSpawnedHandler?.Invoke(region, data);
                }
            }

            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade")]
            [HarmonyPrefix]
            static void DestroyBarricadePostFix(ref BarricadeRegion region, byte x, byte y, ushort plant, ref ushort index)
            {
                if (region.barricades[index] != null)
                {
                    barricadeDestroyedHandler?.Invoke(region.barricades[index], region.barricades[index].instanceID);
                }
            }

            [HarmonyPatch(typeof(BarricadeManager), "sendHealthChanged")]
            [HarmonyPrefix]
            static void DamageBarricadePrefix(byte x, byte y, ushort plant, ref ushort index, ref BarricadeRegion region)
            {
                if (region.barricades[index] != null)
                {
                    barricadeHealthChangedHandler?.Invoke(region.barricades[index]);
                }
            }
        }
    }
}
