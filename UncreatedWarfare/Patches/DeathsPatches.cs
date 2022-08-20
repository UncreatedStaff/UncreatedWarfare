using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare;

public static partial class Patches
{
    [HarmonyPatch]
    public static class DeathsPatches
    {
        internal static GameObject lastProjected;
        // SDG.Unturned.UseableGun

        /// <summary>
        /// Postfix of <see cref="UseableGun.project(Vector3, Vector3, ItemBarrelAsset, ItemMagazineAsset)"/> to predict mortar hits.
        /// </summary>
        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        [HarmonyPatch(typeof(UseableGun), "project")]
        [HarmonyPostfix]
        private static void OnPostProjected(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (lastProjected != null && lastProjected.activeInHierarchy && __instance.equippedGunAsset.isTurret && FOBManager.Loaded)
            {
                BuildableData? data = FOBManager.Config.Buildables.Find(x =>
                    x.Emplacement is not null && (x.Emplacement.ShouldWarnFriendlies || x.Emplacement.ShouldWarnEnemies) &&
                    x.Emplacement.EmplacementVehicle.Exists &&
                    x.Emplacement.EmplacementVehicle.Asset!.turrets.Any(y =>
                        y.itemID == __instance.equippedGunAsset.id));
                if (data != null)
                {
                    UCWarfare.I.Solver.GetLandingPoint(lastProjected, origin, direction, __instance, OnMortarLandingPointFound);
                }
            }
        }
        
        private static void OnMortarLandingPointFound(Player? owner, Vector3 position, float impactTime, ItemGunAsset gun, ItemMagazineAsset? ammoType)
        {
            if (owner == null || ammoType == null)
                return;
            BuildableData? data = !FOBManager.Loaded ? null : FOBManager.Config.Buildables.Find(x =>
                x.Emplacement is not null && (x.Emplacement.ShouldWarnFriendlies || x.Emplacement.ShouldWarnEnemies) &&
                x.Emplacement.EmplacementVehicle.Exists &&
                x.Emplacement.EmplacementVehicle.Asset!.turrets.Any(y =>
                    y.itemID == gun.id));

            if (data == null) return;

            UCPlayer? player = UCPlayer.FromPlayer(owner!);
            if (player != null)
            {
                if (data.Emplacement!.ShouldWarnEnemies)
                    BadOmen.WarnEnemies(player, position, impactTime, gun, ammoType);

                if (data.Emplacement!.ShouldWarnFriendlies)
                    BadOmen.WarnFreindlies(player, position, impactTime, gun, ammoType);
            }
                
        }

        // SDG.Unturned.Bumper
        /// <summary>Adds the id of the vehicle that hit the player to their pt component.</summary>
        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        [HarmonyPatch(typeof(Bumper), "OnTriggerEnter")]
        [HarmonyPrefix]
        private static bool TriggerEnterBumper(Collider other, InteractableVehicle ___vehicle)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (other == null || !Provider.isServer || ___vehicle == null || ___vehicle.asset == null || other.isTrigger || other.CompareTag("Debris"))
                return false;
            if (other.transform.CompareTag("Player"))
            {
                if (___vehicle.isDriven)
                {
                    Player driver = ___vehicle.passengers[0].player.player;
                    if (driver.TryGetPlayerData(out UCPlayerData c))
                    {
                        c.LastVehicleHitBy = ___vehicle.asset.GUID;
                    }
                    if (___vehicle.asset.engine != EEngine.HELICOPTER && ___vehicle.asset.engine != EEngine.PLANE)
                    {
                        Player hit = DamageTool.getPlayer(other.transform);
                        if (hit == null || driver == null || hit.movement.getVehicle() != null || !DamageTool.isPlayerAllowedToDamagePlayer(driver, hit)) return true;
                    }
                    else if (___vehicle.speed <= 10.0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
        private static void OnPreProject(UseableGun gun, ItemMagazineAsset magazine)
        {
            if (gun.player.TryGetPlayerData(out UCPlayerData data))
            {
                data.LastProjectedAmmoType = magazine;
            }
        }

        [HarmonyPatch(typeof(UseableGun))]
        [HarmonyPatch("fire")]
        internal static class ProjectTranspiler
        {
            private readonly static MethodInfo info = typeof(DeathsPatches).GetMethod("OnPreProject", BindingFlags.Static | BindingFlags.NonPublic);

            [SuppressMessage(Data.SUPPRESS_CATEGORY, Data.SUPPRESS_ID)]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.IsStloc() && instruction.operand is LocalBuilder builder && builder.LocalType == typeof(ItemMagazineAsset))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldloc, builder);
                        yield return new CodeInstruction(OpCodes.Call, info);
                    }
                }
            }
        }
    }
}
