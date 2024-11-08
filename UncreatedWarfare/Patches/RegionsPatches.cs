using HarmonyLib;
using SDG.NetPak;
using SDG.NetTransport;
using System;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace Uncreated.Warfare.Harmony;

public static partial class Patches
{
    [HarmonyPatch]
    public class RegionsPatches
    {
#if false
        // SDG.Unturned.BarricadeManager
        /// <summary>
        /// Prefix of <see cref="BarricadeManager.ServerSetSignTextInternal(InteractableSign, BarricadeRegion, byte, byte, ushort, string)"/> to set translation data of signs.
        /// </summary>

        [HarmonyPatch(typeof(BarricadeManager), "ServerSetSignTextInternal")]
        [HarmonyPrefix]
        static bool ServerSetSignTextInternalLang(InteractableSign sign, BarricadeRegion region, byte x, byte y, ushort plant, string trimmedText)
        {
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
#endif
        
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
#if false
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
        }
#endif
    }
}
