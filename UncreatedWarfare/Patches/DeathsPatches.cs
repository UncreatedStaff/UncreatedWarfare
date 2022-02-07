using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static partial class Patches
    {
        [HarmonyPatch]
        public static class DeathsPatches
        {
            // SDG.Unturned.InteractableTrap
            /// <summary>
            /// Prefix of <see cref="InteractableTrap.OnTriggerEnter(Collider other)"/> to set the killer to the player that placed the landmine.
            /// </summary>
            [HarmonyPatch(typeof(InteractableTrap), "OnTriggerEnter")]
            [HarmonyPrefix]
            static bool LandmineExplodeOverride(Collider other, InteractableTrap __instance,
                ref float ___lastTriggered, float ___lastActive, float ___setupDelay, float ___cooldown,
                bool ___isExplosive, ushort ___explosion2, float ___range2,
                float ___playerDamage, float ___zombieDamage, float ___animalDamage, float ___barricadeDamage,
                float ___structureDamage, float ___vehicleDamage, float ___resourceDamage, float ___objectDamage,
                bool ___isBroken, float ___explosionLaunchSpeed)
            {
                using IDisposable profiler = ProfilingUtils.StartTracking();
                if (!UCWarfare.Config.Patches.UseableTrapOnTriggerEnter) return true;

                if (other.isTrigger ||
                    Time.realtimeSinceStartup - ___lastActive < ___setupDelay ||
                    __instance.transform.parent != null &&
                    other.transform == __instance.transform.parent ||
                    Time.realtimeSinceStartup - ___lastTriggered < ___cooldown)
                    return false;
                ___lastTriggered = Time.realtimeSinceStartup;
                if (!Provider.isServer)
                    return false;
                try
                {
                    if (___isExplosive)
                    {
                        if (other.transform.CompareTag("Player") && (!Provider.isPvP || !(other.transform.parent == null) && other.transform.parent.CompareTag("Vehicle")) && ___explosionLaunchSpeed <= 0.00999999977648258)
                            return false;
                    }


                    BarricadeComponent owner = __instance.transform.GetComponentInParent<BarricadeComponent>() ?? __instance.transform.GetComponent<BarricadeComponent>();

                    if (owner != null)
                    {
                        if (owner.Player == null)
                        {
                            SteamPlayer pl = PlayerTool.getSteamPlayer(owner.Owner);
                            owner.Player = pl?.player;
                        }
                        if (owner.Player != null)
                        {
                            if (owner.Player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                                c.LastLandmineExploded = new LandmineData(__instance, owner);
                            if (UCWarfare.Config.Debug)
                                L.LogDebug(F.GetPlayerOriginalNames(owner.Player).PlayerName + "'s trap was triggered", ConsoleColor.DarkGray);
                        }
                        else if (UCWarfare.Config.Debug && owner.Owner != 0)
                        {
                            FPlayerName names = Data.DatabaseManager.GetUsernames(owner.Owner);
                            L.LogDebug("[OFFLINE] " + names.PlayerName + "'s trap was triggered", ConsoleColor.DarkGray);
                        }
                    }
                    else if (UCWarfare.Config.Debug)
                    {
                        L.LogDebug("Unknown owner's trap was triggered", ConsoleColor.DarkGray);
                    }

                    if (___isExplosive)
                    {
                        if (other.transform.CompareTag("Player")) // landmine that player walks over
                        {
                            L.LogDebug("Landmine walked over by player");
                            if (!Provider.isPvP || other.transform.parent != null && other.transform.parent.CompareTag("Vehicle"))
                                return false;

                            Vector3 position = __instance.transform.position;
                            if (Teams.TeamManager.IsInAnyMainOrAMCOrLobby(position))
                            {
                                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
                                if (drop != null && Regions.tryGetCoordinate(position, out byte x, out byte y))
                                {
                                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                                    SDG.Unturned.BarricadeData sdata = drop.GetServersideData();
                                    if (sdata == null)
                                    {
                                        L.LogDebug("Trap attempted to explode in main owned by " + (drop.model.TryGetComponent(out BarricadeComponent bc) ? bc.Owner.ToString() : "unknown player"));
                                    }
                                    else
                                    {
                                        L.LogDebug("Trap attempted to explode in main owned by " + sdata.owner.ToString());
                                    }
                                }
                                else
                                {
                                    L.LogDebug("Trap attempted to explode in main owned by unknown player");
                                }
                                return false;
                            }
                            // triggerer
                            Player player = DamageTool.getPlayer(other.transform) ?? other.GetComponent<Player>();

                            if (owner != null && player != null && player.quests.groupID.m_SteamID == owner.Owner.GetTeamFromPlayerSteam64ID())
                            {
                                return false;
                            }

                            if (owner != null && player != null && player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                            {
                                c.LastLandmineTriggered = new LandmineData(__instance, owner);
                            }

                            DamageTool.explode(new ExplosionParameters(position, ___range2, EDeathCause.LANDMINE, owner.Player == null ? CSteamID.Nil : owner.Player.channel.owner.playerID.steamID)
                            {
                                playerDamage = ___playerDamage,
                                zombieDamage = ___zombieDamage,
                                animalDamage = ___animalDamage,
                                barricadeDamage = ___barricadeDamage,
                                structureDamage = ___structureDamage,
                                vehicleDamage = ___vehicleDamage,
                                resourceDamage = ___resourceDamage,
                                objectDamage = ___objectDamage,
                                damageOrigin = EDamageOrigin.Trap_Explosion,
                                launchSpeed = ___explosionLaunchSpeed
                            }, out _);
                            if (___explosion2 <= 0)
                                return false;
                            EffectManager.triggerEffect(new TriggerEffectParameters(___explosion2)
                            {
                                position = position,
                                relevantDistance = EffectManager.LARGE
                            });
                        }
                        else // other form of trigger (throwable, animal, etc)
                        {
                            Vector3 position = __instance.transform.position;
                            if (Teams.TeamManager.IsInAnyMainOrAMCOrLobby(position))
                            {
                                BarricadeDrop drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
                                if (drop != null && Regions.tryGetCoordinate(position, out byte x, out byte y))
                                {
                                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                                    SDG.Unturned.BarricadeData sdata = drop.GetServersideData();
                                    if (sdata == null)
                                    {
                                        L.Log("Trap attempted to explode in main owned by " + (drop.model.TryGetComponent(out BarricadeComponent bc) ? bc.Owner.ToString() : "unknown player"), ConsoleColor.Cyan);
                                    }
                                    else
                                    {
                                        L.Log("Trap attempted to explode in main owned by " + sdata.owner.ToString(), ConsoleColor.Cyan);
                                    }
                                }
                                else
                                {
                                    L.Log("Trap attempted to explode in main owned by unknown player", ConsoleColor.Cyan);
                                }
                                return false;
                            }
                            ThrowableOwner c = other.transform.GetComponent<ThrowableOwner>(); // throwable?
                            if (c != null)
                            {
                                SteamPlayer throwableOwner = PlayerTool.getSteamPlayer(c.ownerID);
                                if (throwableOwner != null)
                                {
                                    if (owner != null && throwableOwner.player.quests.groupID.m_SteamID == owner.Owner.GetTeamFromPlayerSteam64ID())
                                    {
                                        return false;
                                    }
                                    if (owner != null && throwableOwner.player.TryGetPlaytimeComponent(out PlaytimeComponent c2))
                                    {
                                        c2.LastLandmineTriggered = new LandmineData(__instance, owner);
                                    }
                                }
                            }
                            DamageTool.explode(new ExplosionParameters(position, ___range2, EDeathCause.LANDMINE, owner.Player == null ? CSteamID.Nil : owner.Player.channel.owner.playerID.steamID)
                            {
                                playerDamage = ___playerDamage,
                                zombieDamage = ___zombieDamage,
                                animalDamage = ___animalDamage,
                                barricadeDamage = ___barricadeDamage,
                                structureDamage = ___structureDamage,
                                vehicleDamage = ___vehicleDamage,
                                resourceDamage = ___resourceDamage,
                                objectDamage = ___objectDamage,
                                damageOrigin = EDamageOrigin.Trap_Explosion,
                                launchSpeed = ___explosionLaunchSpeed
                            }, out _);
                            if (___explosion2 <= 0)
                                return false;
                            EffectManager.triggerEffect(new TriggerEffectParameters(___explosion2)
                            {
                                position = position,
                                relevantDistance = EffectManager.LARGE
                            });
                        }
                    }
                    else if (other.transform.CompareTag("Player")) // non explosive and player walks over it (barbed, etc)
                    {
                        L.LogDebug("Trap triggered by player.");
                        if (!Provider.isPvP || other.transform.parent != null && other.transform.parent.CompareTag("Vehicle"))
                            return false;
                        Player player = DamageTool.getPlayer(other.transform);
                        if (player == null)
                            return false;

                        // triggerer
                        if (owner != null && player.TryGetPlaytimeComponent(out PlaytimeComponent c))
                        {
                            c.LastLandmineTriggered = new LandmineData(__instance, owner);
                        }
                        DamageTool.damage(player, EDeathCause.SHRED, ELimb.SPINE,
                            owner == null ? CSteamID.Nil : (owner.Player == null ? new CSteamID(owner.Owner) : owner.Player.channel.owner.playerID.steamID)
                            , Vector3.up, ___playerDamage, 1f, out EPlayerKill _, trackKill: true);
                        if (___isBroken)
                            player.life.breakLegs();
                        EffectManager.sendEffect(5, EffectManager.SMALL, __instance.transform.position + Vector3.up, Vector3.down);
                        BarricadeManager.damage(__instance.transform.parent, 5f, 1f, false, damageOrigin: EDamageOrigin.Trap_Wear_And_Tear);
                    }
                    else
                    {
                        if (!other.transform.CompareTag("Agent"))
                            return false;
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
                            if (animal == null)
                                return false;
                            DamageTool.damageAnimal(new DamageAnimalParameters(animal, __instance.transform.forward, ___animalDamage)
                            {
                                instigator = __instance
                            }, out EPlayerKill _, out uint _);
                            EffectManager.sendEffect(5, EffectManager.SMALL, __instance.transform.position + Vector3.up, Vector3.down);
                            BarricadeManager.damage(__instance.transform.parent, 5f, 1f, false, damageOrigin: EDamageOrigin.Trap_Wear_And_Tear);
                        }
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    L.LogError("Error in patched Landmine Trigger Override.");
                    L.LogError(ex);
                    return true; // run original code to cleanup.
                }
            }

            // SDG.Unturned.InteractableVehicle
            /// <summary>
            /// Call event before vehicle explode
            /// </summary>
            [HarmonyPatch(typeof(InteractableVehicle), "explode")]
            [HarmonyPrefix]
            static bool ExplodeVehicle(InteractableVehicle __instance)
            {
                using IDisposable profiler = ProfilingUtils.StartTracking();
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
                if (instigator.TryGetPlaytimeComponent(out PlaytimeComponent c))
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
                using IDisposable profiler = ProfilingUtils.StartTracking();
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
                using IDisposable profiler = ProfilingUtils.StartTracking();
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
                            if (driver.TryGetPlaytimeComponent(out PlaytimeComponent c))
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
