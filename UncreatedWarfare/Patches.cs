using HarmonyLib;
using JetBrains.Annotations;
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
