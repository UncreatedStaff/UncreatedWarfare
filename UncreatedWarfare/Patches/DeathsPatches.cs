using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.FOBs;
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
        [HarmonyPatch(typeof(UseableGun), "project")]
        [HarmonyPostfix]
        static void OnPostProjected(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (lastProjected != null && lastProjected.activeInHierarchy)
            {
                if (FOBManager.Loaded && 
                    FOBManager.Config.Buildables.Any(x => x.Emplacement is not null && x.Emplacement.ShouldWarnFriendliesIncoming && x.Emplacement.EmplacementVehicle.Exists &&
                                                          x.Emplacement.EmplacementVehicle.Asset!.turrets.Any(x => x.itemID == __instance.equippedGunAsset.id)))
                {
                    Vector3 yaw = new Vector3(direction.x, 0, direction.z).normalized;
                    float dp = direction.x * yaw.x + direction.y * yaw.y + direction.z * yaw.z;
                    float angle = Mathf.Acos(dp / (direction.magnitude * yaw.magnitude)) - (Mathf.PI / 4);
                    float range = Mathf.Sin(2f * angle - Mathf.PI / 2f) / -(9.81f / (133.3f * 133.3f));
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
    }
}
