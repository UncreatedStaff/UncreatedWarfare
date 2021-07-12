using HarmonyLib;
using JetBrains.Annotations;
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
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Patches
    {
        public delegate void BarricadeDroppedEventArgs(BarricadeRegion region, BarricadeData data, ref Transform location);
        public delegate void BarricadeDestroyedEventArgs(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort index, ushort plant);
        public delegate void StructureDestroyedEventArgs(StructureRegion region, StructureData data, StructureDrop drop, uint instanceID);
        public delegate void BarricadeHealthEventArgs(BarricadeData data);
        public delegate void OnPlayerTogglesCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool allow);
        public delegate void OnPlayerSetsCosmeticsDelegate(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow);
        public delegate void BatteryStealingDelegate(SteamPlayer theif, ref bool allow);
        public delegate void PlayerTriedStoreItem(Player player, byte page, ItemJar jar, ref bool allow);

        public static event BarricadeDroppedEventArgs BarricadeSpawnedHandler;
        public static event BarricadeDestroyedEventArgs BarricadeDestroyedHandler;
        public static event StructureDestroyedEventArgs StructureDestroyedHandler;
        public static event BarricadeHealthEventArgs BarricadeHealthChangedHandler;
        public static event OnPlayerTogglesCosmeticsDelegate OnPlayerTogglesCosmetics_Global;
        public static event OnPlayerSetsCosmeticsDelegate OnPlayerSetsCosmetics_Global;
        public static event BatteryStealingDelegate OnBatterySteal_Global;
        public static event PlayerTriedStoreItem OnPlayerTriedStoreItem_Global;

        /// <summary>
        /// Stores all <see cref="Harmony"/> patches.
        /// </summary>
        [HarmonyPatch]
        public static class InternalPatches
        {
            /// <summary>Patch methods</summary>
            public static void DoPatching()
            {
                Harmony harmony = new Harmony("com.company.project.product");
                harmony.PatchAll();
            }
#pragma warning disable IDE0051
            // SDG.Unturned.PlayerInventory
            ///<summary>
            /// Prefix of <see cref="PlayerInventory.ReceiveDragItem(byte, byte, byte, byte, byte, byte, byte)"/> to disallow players leaving their group.
            ///</summary>
            [HarmonyPatch(typeof(PlayerInventory), "ReceiveDragItem")]
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
            [HarmonyPatch(typeof(GroupManager), "requestGroupExit")]
            [HarmonyPrefix]
            static bool CancelLeavingGroup(Player player)
            {
                if (!UCWarfare.Config.Patches.requestGroupExit) return true;
                player.SendChat("cant_leave_group", UCWarfare.GetColor("cant_leave_group"));
                return false;
            }
            // SDG.Unturned.PlayerClothing
            /// <summary>
            /// Prefix of <see cref="PlayerClothing.ReceiveVisualToggleRequest(EVisualToggleType)"/> to use an event to cancel it.
            /// </summary>
            [HarmonyPatch(typeof(PlayerClothing), "ReceiveVisualToggleRequest")]
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
            [HarmonyPatch(typeof(PlayerClothing), "ServerSetVisualToggleState")]
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
            [HarmonyPatch(typeof(VehicleManager), "ReceiveStealVehicleBattery")]
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
            /// Prefix of <see cref="BarricadeManager.ServerSetSignText(InteractableSign, string)"/> to set translation data of signs.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "ServerSetSignTextInternal")]
            [HarmonyPrefix]
            static bool ServerSetSignTextInternalLang(InteractableSign sign, BarricadeRegion region, byte x, byte y, ushort plant, ushort index, string trimmedText)
            {
                if (!UCWarfare.Config.Patches.ServerSetSignTextInternal) return true;
                if (trimmedText.StartsWith("sign_"))
                {
                    if(trimmedText.Length > 5)
                    {
                        if(Kits.KitManager.KitExists(trimmedText.Substring(5), out _))
                            Task.Run( async () => await F.InvokeSignUpdateForAllKits(x, y, plant, index, trimmedText));
                        else
                            Task.Run(async () => await F.InvokeSignUpdateForAll(x, y, plant, index, trimmedText));
                    } else
                        Task.Run(async () => await F.InvokeSignUpdateForAll(x, y, plant, index, trimmedText));
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
                    return true;
                }
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.SendRegion(SteamPlayer client, byte x, byte y, ushort plant)"/> to set translation data of signs.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "SendRegion")]
            [HarmonyPrefix]
            static bool SendRegion(SteamPlayer client, byte x, byte y, ushort plant)
            {
                if (!UCWarfare.Config.Patches.SendRegion) return true;
                if (!BarricadeManager.tryGetRegion(x, y, plant, out BarricadeRegion region))
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
                        Data.SendMultipleBarricades.Invoke(ENetReliability.Reliable, client.transportConnection, async writer =>
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
                                    if (region.drops[index].interactable != null && region.drops[index].interactable is InteractableSign sign)
                                    {
                                        string newtext = sign.text;
                                        if (newtext.StartsWith("sign_"))
                                        {
                                            newtext = await F.TranslateSign(newtext, client.playerID.steamID.m_SteamID, false);
                                        // size is not allowed in signs.
                                        newtext.Replace("<size=", "");
                                            newtext.Replace("</size>", "");
                                        }
                                        byte[] state = region.barricades[index].barricade.state;
                                        byte[] bytes = Encoding.UTF8.GetBytes(newtext);
                                        if (bytes.Length + 17 > byte.MaxValue)
                                        {
                                            F.LogError(sign.text + $" sign translation is too long, must be <= {byte.MaxValue - 17} UTF8 bytes!");
                                            bytes = Encoding.UTF8.GetBytes(sign.text);
                                        }
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
                        });
                        packet++;
                    }
                    return false;
                }
                else return true; // no barricades
            }
            public static event OnLandmineExplodeDelegate OnLandmineExplode;
            public delegate void OnLandmineExplodeDelegate(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner);

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
                        Player hit = DamageTool.getPlayer(other.transform);
                        Player driver = ___vehicle.passengers[0].player.player;
                        if (hit == null || driver == null || hit.movement.getVehicle() != null || !DamageTool.isPlayerAllowedToDamagePlayer(driver, hit)) return true;
                        if(F.TryGetPlaytimeComponent(driver, out PlaytimeComponent c))
                        {
                            c.lastRoadkilled = ___vehicle.asset.id;
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
            static bool LandmineExplodeOverride(Collider other, InteractableTrap __instance, float ___lastActive,
                bool ___isExplosive, ushort ___explosion2, float ___playerDamage, float ___zombieDamage,
                float ___animalDamage, float ___barricadeDamage, float ___structureDamage,
                float ___vehicleDamage, float ___resourceDamage, float ___objectDamage,
                float ___range2, bool ___isBroken)
            {
                if (!UCWarfare.Config.Patches.UseableTrapOnTriggerEnter) return true;
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
                if (other.isTrigger || Time.realtimeSinceStartup - ___lastActive < 0.25 || __instance.transform.parent != null && other.transform == __instance.transform.parent || !Provider.isServer)
                    return false;
                OnLandmineExplode?.Invoke(__instance, other, OwnerComponent);
                if (___isExplosive) // if hurts all in range, makes explosion
                {
                    if (other.transform.CompareTag("Player")) // if player hit.
                    {
                        if (!Provider.isPvP || !(other.transform.parent == null) && other.transform.parent.CompareTag("Vehicle"))
                            return false;
                        if(other.transform.TryGetComponent(out Player player))
                        {
                            if(F.TryGetPlaytimeComponent(player, out PlaytimeComponent c))
                                if(OwnerComponent != null)
                                    c.LastLandmineTriggered = new LandmineDataForPostAccess(__instance, OwnerComponent);
                        }
                        EffectManager.sendEffect(___explosion2, EffectManager.LARGE, __instance.transform.position); // fix vehicles exploded by landmines not applying proper kills.
                        DamageTool.explode(__instance.transform.position, ___range2, EDeathCause.LANDMINE, owner, ___playerDamage, ___zombieDamage, ___animalDamage, ___barricadeDamage, ___structureDamage, ___vehicleDamage, ___resourceDamage, ___objectDamage, out List<EPlayerKill> _, damageOrigin: EDamageOrigin.Trap_Explosion);
                    }
                    else
                    {
                        if (other.gameObject.TryGetComponent(out ThrowableOwnerDataComponent throwable))
                        {
                            F.Log("Found Throwable " + throwable.owner.channel.owner.playerID.playerName);
                            if(F.TryGetPlaytimeComponent(throwable.owner, out PlaytimeComponent c))
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
            static bool SimulatePlayerLifePre(uint simulation, PlayerLife __instance, uint ___lastBleed, ref bool ____isBleeding)
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
                                if (Data.ReviveManager != null && Data.ReviveManager.DownedPlayers.ContainsKey(__instance.player.channel.owner.playerID.steamID.m_SteamID) && __instance.health <= 10)
                                {
                                    ____isBleeding = false;
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
                                    p.damage = 1;
                                    __instance.askDamage(1, p.direction, p.cause, p.limb, p.killer, out EPlayerKill _, canCauseBleeding: false, bypassSafezone: true);
                                }
                            }
                        }
                    }
                }
            }
            // SDG.Unturned.VehicleManager
            [HarmonyPatch(typeof(VehicleManager), "damage")]
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
                            vehicle.gameObject.AddComponent<VehicleDamageOwnerComponent>().owner = instigatorSteamID;
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
                    if(__instance.passengers.Length > 0)
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
                        F.Log($"Damaging passenger {F.GetPlayerOriginalNames(passenger.player).PlayerName}: {instigator}");
                        passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SPINE, instigator, out _);
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
                EffectManager.sendEffect(__instance.asset.explosion, EffectManager.LARGE, __instance.transform.position);
                return false;
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.dropBarricadeIntoRegionInternal(BarricadeRegion region, Barricade barricade, Vector3 point, Quaternion rotation, ulong owner, ulong group, out BarricadeData data, out Transform result, out uint instanceID)"/> to invoke <see cref="BarricadeSpawnedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "dropBarricadeIntoRegionInternal")]
            [HarmonyPostfix]
            static void DropBarricadePostFix(BarricadeRegion region, BarricadeData data, ref Transform result, ref uint instanceID)
            {
                if (!UCWarfare.Config.Patches.dropBarricadeIntoRegionInternal) return;
                if (result == null) return;

                BarricadeDrop drop = region.drops.LastOrDefault();

                if (drop?.instanceID == instanceID)
                {
                    BarricadeSpawnedHandler?.Invoke(region, data, ref result);
                }
            }
            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.destroyBarricade(BarricadeRegion, byte, byte, ushort, ushort)"/> to invoke <see cref="BarricadeDestroyedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "destroyBarricade")]
            [HarmonyPrefix]
            static void DestroyBarricadePostFix(ref BarricadeRegion region, byte x, byte y, ushort plant, ref ushort index)
            {
                if (!UCWarfare.Config.Patches.destroyBarricade) return;
                if (region.barricades[index] != null)
                {
                    BarricadeDestroyedHandler?.Invoke(region, region.barricades[index], region.drops[index], region.barricades[index].instanceID, index, plant);
                }
            }

            // SDG.Unturned.StructureManager
            /// <summary>
            /// Prefix of <see cref="StructureManager.destroyStructure(StructureRegion, byte, byte, ushort, Vector3)"/> to invoke <see cref="StructureDestroyedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(StructureManager), "destroyStructure")]
            [HarmonyPrefix]
            static void DestroyStructurePostFix(StructureRegion region, byte x, byte y, ushort index, Vector3 ragdoll)
            {
                if (!UCWarfare.Config.Patches.destroyStructure) return;
                if (region.structures[index] != null)
                {
                    StructureDestroyedHandler?.Invoke(region, region.structures[index], region.drops[index], region.structures[index].instanceID);
                }
            }


            // SDG.Unturned.BarricadeManager
            /// <summary>
            /// Prefix of <see cref="BarricadeManager.sendHealthChanged(byte x, byte y, ushort plant, ushort index, BarricadeRegion region)"/> to invoke <see cref="BarricadeHealthChangedHandler"/>.
            /// </summary>
            [HarmonyPatch(typeof(BarricadeManager), "sendHealthChanged")]
            [HarmonyPrefix]
            static void DamageBarricadePrefix(ref ushort index, ref BarricadeRegion region)
            {
                if (!UCWarfare.Config.Patches.sendHealthChanged) return;
                if (region.barricades[index] != null)
                {
                    BarricadeHealthChangedHandler?.Invoke(region.barricades[index]);
                }
            }
#pragma warning restore IDE0051
        }
    }
}
