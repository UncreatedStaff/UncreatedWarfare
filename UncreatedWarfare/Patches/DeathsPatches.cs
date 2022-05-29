using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static partial class Patches
    {
        [HarmonyPatch]
        public static class DeathsPatches
        {
            // SDG.Unturned.InteractableVehicle
            /// <summary>
            /// Call event before vehicle explode
            /// </summary>
            [HarmonyPatch(typeof(InteractableVehicle), "explode")]
            [HarmonyPrefix]
            static bool ExplodeVehicle(InteractableVehicle __instance)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (!UCWarfare.Config.Patches.explodeInteractableVehicle) return true;
                if (!__instance.asset.ShouldExplosionCauseDamage) return true;
                CSteamID instigator = CSteamID.Nil;

                if (__instance.gameObject.TryGetComponent(out VehicleComponent vc))
                {
                    instigator = vc.Vehicle.lockedOwner;
                }
                else
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
                //__instance.dropTrunkItems();
                if (instigator.TryGetPlayerData(out UCPlayerData c))
                {
                    c.lastExplodedVehicle = __instance.asset.GUID;
                }
                DamageTool.explode(__instance.transform.position, 8f, EDeathCause.VEHICLE, instigator, 200f, 200f, 200f, 0.0f, 0.0f, 500f, 2000f, 500f, out _, damageOrigin: EDamageOrigin.Vehicle_Explosion);
                for (int index = 0; index < __instance.passengers.Length; ++index)
                {
                    Passenger passenger = __instance.passengers[index];
                    if (passenger != null && passenger.player != null && passenger.player.player != null && !passenger.player.player.life.isDead)
                    {
                        L.LogDebug($"Damaging passenger {F.GetPlayerOriginalNames(passenger.player).PlayerName}: {instigator}", ConsoleColor.DarkGray);
                        passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SKULL, instigator, out _);
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
                            ItemManager.dropItem(new Item(newID, EItemOrigin.NATURE), __instance.transform.position + new Vector3(Mathf.Sin(f) * 3f, 1f, Mathf.Cos(f) * 3f), false, true, true);
                    }
                }
                VehicleManager.sendVehicleExploded(__instance);
                if (__instance.asset.explosion == 0)
                    return false;
                EffectManager.sendEffect(__instance.asset.explosion, EffectManager.LARGE, __instance.transform.position);
                return false;
            }

            internal static GameObject lastProjected;
            // SDG.Unturned.UseableGun
            /// <summary>
            /// Postfix of <see cref="UseableGun.project(Vector3, Vector3, ItemBarrelAsset, ItemMagazineAsset)"/> to predict mortar hits.
            /// </summary>
            [HarmonyPatch(typeof(UseableGun), "project")]
            [HarmonyPostfix]
            static void OnPostProjected(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                                                ToastMessage.QueueMessage(players.Current, new ToastMessage(Translation.Translate("friendly_mortar_incoming", players.Current), EToastMessageSeverity.WARNING));
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
            // SDG.Unturned.Bumper
            /// <summary>Adds the id of the vehicle that hit the player to their pt component.</summary>
            [HarmonyPatch(typeof(Bumper), "OnTriggerEnter")]
            [HarmonyPrefix]
            static bool TriggerEnterBumper(Collider other, InteractableVehicle ___vehicle)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                            if (driver.TryGetPlayerData(out UCPlayerData c))
                            {
                                c.lastRoadkilled = ___vehicle.asset.GUID;
                            }
                        }
                        else if (___vehicle.speed <= 10.0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
