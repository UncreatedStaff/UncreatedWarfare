using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Kits;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Events;
internal static class EventPatches
{
    private static bool _fail;
    internal static void TryPatchAll()
    {
        _fail = false;
        PatchUtil.PatchMethod(
            typeof(BarricadeManager)
            .GetMethod(nameof(BarricadeManager.destroyBarricade),
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(BarricadeDrop), typeof(byte), typeof(byte), typeof(ushort) },
                null), ref _fail,
            postfix: PatchUtil.GetMethodInfo(DestroyBarricadePostFix));

        PatchUtil.PatchMethod(typeof(VehicleManager).GetMethod("addVehicle", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            postfix: PatchUtil.GetMethodInfo(OnVehicleSpawned));

        PatchUtil.PatchMethod(typeof(InteractableTrap).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            prefix: PatchUtil.GetMethodInfo(TrapOnTriggerEnter));

        PatchUtil.PatchMethod(typeof(InteractableSign).GetMethod("updateText", BindingFlags.Instance | BindingFlags.Public), ref _fail,
            postfix: PatchUtil.GetMethodInfo(OnTextUpdated));

        PatchUtil.PatchMethod(typeof(InteractableSign).GetMethod("ReceiveChangeTextRequest", BindingFlags.Instance | BindingFlags.Public), ref _fail,
            postfix: PatchUtil.GetMethodInfo(OnTextUpdateRequested));

        PatchUtil.PatchMethod(typeof(InteractableCharge).GetMethod("detonate", BindingFlags.Instance | BindingFlags.Public), ref _fail,
            prefix: PatchUtil.GetMethodInfo(PreDetonate), postfix: PatchUtil.GetMethodInfo(PostDetonate));

        PatchUtil.PatchMethod(typeof(InteractableVehicle).GetMethod("explode", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            prefix: PatchUtil.GetMethodInfo(ExplodeVehicle));

        PatchUtil.PatchMethod(typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic), ref _fail,
            prefix: PatchUtil.GetMethodInfo(RocketOnTriggerEnter));

        PatchUtil.PatchMethod(typeof(PlayerLife).GetMethod("doDamage", BindingFlags.NonPublic | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(PlayerDamageRequested));

        PatchUtil.PatchMethod(PatchUtil.GetMethodInfo(
                new Action<StructureDrop, byte, byte, Vector3, bool>(StructureManager.destroyStructure)), ref _fail,
            postfix: PatchUtil.GetMethodInfo(OnStructureDestroyed));

        PatchUtil.PatchMethod(PatchUtil.GetMethodInfo(new Action<SteamPending>(Provider.accept)), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnAcceptingPlayer));

        if (MthdRemoveItem != null)
        {
            if (MthdAddItem != null)
            {
                PatchUtil.PatchMethod(typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveDragItem), BindingFlags.Public | BindingFlags.Instance), ref _fail,
                    transpiler: PatchUtil.GetMethodInfo(TranspileReceiveDragItem));

                PatchUtil.PatchMethod(typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveSwapItem), BindingFlags.Public | BindingFlags.Instance), ref _fail,
                    transpiler: PatchUtil.GetMethodInfo(TranspileReceiveSwapItem));
            }
            else
            {
                L.LogError("Unable to find Items.addItem to transpile player inventory events.");
            }

            PatchUtil.PatchMethod(typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.ReceiveDropItem), BindingFlags.Public | BindingFlags.Instance), ref _fail,
                transpiler: PatchUtil.GetMethodInfo(TranspileReceiveDropItem));
        }
        else
        {
            L.LogError("Unable to find PlayerInventory.removeItem to transpile player inventory events.");
        }

        PatchUtil.PatchMethod(typeof(ItemManager).GetMethod(nameof(ItemManager.ReceiveTakeItemRequest), BindingFlags.Public | BindingFlags.Static), ref _fail,
            transpiler: PatchUtil.GetMethodInfo(TranspileReceiveTakeItemRequest));

        if (!PatchUtil.PatchMethod(typeof(InteractablePower).GetMethod("CalculateIsConnectedToPower", BindingFlags.NonPublic | BindingFlags.Instance), prefix: PatchUtil.GetMethodInfo(OnCalculatingPower)))
            Data.UseElectricalGrid = false;

        PatchUtil.PatchMethod(typeof(ObjectManager).GetMethod(nameof(ObjectManager.ReceiveToggleObjectBinaryStateRequest), BindingFlags.Public | BindingFlags.Static), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveToggleObjectBinaryStateRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapShirtRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapShirtRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapPantsRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapPantsRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapHatRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapHatRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapBackpackRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapBackpackRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapVestRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapVestRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapMaskRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapMaskRequest));

        PatchUtil.PatchMethod(typeof(PlayerClothing).GetMethod(nameof(PlayerClothing.ReceiveSwapGlassesRequest), BindingFlags.Public | BindingFlags.Instance), ref _fail,
            prefix: PatchUtil.GetMethodInfo(OnReceiveSwapGlassesRequest));
    }
    [OperationTest("Event Patches")]
    [Conditional("DEBUG")]
    [UsedImplicitly]
    private static void TestEventPatches() => Assert.IsFalse(_fail);
    // SDG.Unturned.InteractableSign
    /// <summary>
    /// Postfix of <see cref="InteractableSign.updateText(string)"/> to invoke <see cref="EventDispatcher.SignTextUpdated"/>.
    /// </summary>
    private static void OnTextUpdated(InteractableSign __instance, string newText) => EventDispatcher.InvokeOnSignTextChanged(__instance);
#pragma warning disable CS1580
    // SDG.Unturned.InteractableSign
    /// <summary>
    /// Postfix of <see cref="InteractableSign.ReceiveChangeTextRequest(in ServerInvocationContext,string)"/>.
    /// </summary>
    private static void OnTextUpdateRequested(InteractableSign __instance, in ServerInvocationContext context, string newText)
    {
        Player pl = context.GetPlayer();
        if (pl != null && __instance.transform.TryGetComponent(out BarricadeComponent bcomp))
        {
            bcomp.LastEditor = pl.channel.owner.playerID.steamID.m_SteamID;
            bcomp.EditTick = UCWarfare.I.Debugger.Updates;
        }
    }
#pragma warning restore CS1580
    // SDG.Unturned.BarricadeManager
    /// <summary>
    /// Postfix of <see cref="BarricadeManager.destroyBarricade(BarricadeRegion, byte, byte, ushort, ushort)"/> to invoke <see cref="EventDispatcher.BarricadeDestroyed"/>.
    /// </summary>
    private static void DestroyBarricadePostFix(BarricadeDrop barricade, byte x, byte y, ushort plant)
    {
        if (barricade is null) return;
        BarricadeRegion region;
        if (plant == ushort.MaxValue)
        {
            if (Regions.checkSafe(x, y))
                region = BarricadeManager.regions[x, y];
            else return;
        }
        else if (BarricadeManager.vehicleRegions.Count > plant)
            region = BarricadeManager.vehicleRegions[plant];
        else return;
        EventDispatcher.InvokeOnBarricadeDestroyed(barricade, barricade.GetServersideData(), region, x, y, plant);
    }
    // SDG.Unturned.VehicleManager.addVehicle
    /// <summary>
    /// Postfix of <see cref="VehicleManager.addVehicle(Guid,ushort,ushort,float,Vector3,Quaternion,bool,bool,bool,bool,ushort,bool,ushort,ushort,CSteamID,CSteamID,bool,CSteamID[],byte[][],uint,byte,NetId)"/> to call OnVehicleSpawned
    /// </summary>
    private static void OnVehicleSpawned(Guid assetGuid, ushort skinID, ushort mythicID, float roadPosition, Vector3 point, Quaternion angle,
        bool sirens, bool blimp, bool headlights, bool taillights, ushort fuel, bool isExploded, ushort health, ushort batteryCharge, CSteamID owner,
        CSteamID group, bool locked, CSteamID[] passengers, byte[][] turrets, uint instanceID, byte tireAliveMask, NetId netId, InteractableVehicle __result)
    {
        if (__result != null)
        {
            EventDispatcher.InvokeOnVehicleSpawned(__result);
        }
    }
    // SDG.Unturned.InteractableTrap.OnTriggerEnter
    /// <summary>
    /// Prefix of <see cref="InteractableTrap.OnTriggerEnter(Collider)"/> to change explosion behavior.
    /// </summary>
    private static bool TrapOnTriggerEnter(Collider other, InteractableTrap __instance, float ___lastActive, float ___setupDelay, ref float ___lastTriggered,
        float ___cooldown, bool ___isExplosive, float ___playerDamage, float ___zombieDamage, float ___animalDamage, float ___barricadeDamage,
        float ___structureDamage, float ___vehicleDamage, float ___resourceDamage, float ___objectDamage, float ___range2, float ___explosionLaunchSpeed,
        ushort ___explosion2, bool ___isBroken)
    {
        float time = Time.realtimeSinceStartup;
        if (other.isTrigger ||                          // collider is another trigger
            time - ___lastActive < ___setupDelay ||     // in setup phase
                                                        // collider is part of the trap barricade
            __instance.transform.parent == other.transform.parent && other.transform.parent != null ||
            time - ___lastTriggered < ___cooldown ||    // on cooldown
                                                        // gamemode not active
            Data.Gamemode is null || Data.Gamemode.State != Gamemodes.State.Active
            )
            return false;
        ___lastTriggered = time;
        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(__instance.gameObject.transform.parent) ?? BarricadeManager.FindBarricadeByRootTransform(__instance.gameObject.transform);
        if (barricade is null) return false;
        UCPlayer? triggerer = null;
        ThrowableComponent? throwable = null;
        if (other.transform.CompareTag("Player"))
        {
            triggerer = UCPlayer.FromPlayer(DamageTool.getPlayer(other.transform));
            if (triggerer == null) return false;
        }
        else if (other.transform.CompareTag("Vehicle"))
        {
            InteractableVehicle? vehicle = DamageTool.getVehicle(other.transform);
            if (vehicle == null) return false;
            if (vehicle.passengers.Length > 0 && vehicle.passengers[0].player != null)
                triggerer = UCPlayer.FromPlayer(vehicle.passengers[0].player.player);
            if (triggerer == null)
            {
                if (vehicle.TryGetComponent(out VehicleComponent comp2))
                    triggerer = UCPlayer.FromID(comp2.LastDriver);
                if (triggerer == null) return false;
            }
        }
        else
        {
            if (other.TryGetComponent(out throwable))
                triggerer = UCPlayer.FromSteamPlayer(PlayerTool.getSteamPlayer(throwable!.Owner));
        }
        if (triggerer != null)
        {
            if (___isExplosive)
            {
                CSteamID owner = new CSteamID(barricade.GetServersideData().owner);
                UCPlayer? ownerPl = UCPlayer.FromCSteamID(owner);
                bool shouldExplode = true;
                EventDispatcher.InvokeOnLandmineExploding(ownerPl, barricade, __instance, triggerer, other.gameObject, ref shouldExplode);
                if (shouldExplode)
                {
                    UCPlayerData? ownerData = null;
                    if (ownerPl is not null && ownerPl.Player.TryGetPlayerData(out ownerData))
                    {
                        ownerData.ExplodingLandmine = barricade;
                    }
                    if (triggerer.Player.TryGetPlayerData(out UCPlayerData? triggererData))
                    {
                        triggererData.TriggeringLandmine = barricade;
                        triggererData.TriggeringThrowable = throwable;
                    }

                    Vector3 position = __instance.transform.position;
                    DamageTool.explode(new ExplosionParameters(position, ___range2, EDeathCause.LANDMINE, owner)
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
                    EffectAsset asset = Assets.FindEffectAssetByGuidOrLegacyId(__instance.trapDetonationEffectGuid, ___explosion2);
                    if (asset != null)
                    {
                        EffectManager.triggerEffect(new TriggerEffectParameters(asset)
                        {
                            position = position,
                            relevantDistance = EffectManager.LARGE
                        });
                    }
                    if (ownerData != null)
                        ownerData.ExplodingLandmine = null;
                    if (triggererData != null)
                    {
                        triggererData.TriggeringLandmine = null;
                        triggererData.TriggeringThrowable = null;
                    }
                }
            }
            else
            {
                if (other.transform.CompareTag("Player"))
                {
                    CSteamID owner = new CSteamID(barricade.GetServersideData().owner);
                    if (triggerer.Player.movement.getVehicle() != null)
                    {
                        return false;
                    }
                    if (triggerer.Player.TryGetPlayerData(out UCPlayerData data))
                        data.LastShreddedBy = barricade.asset.GUID;
                    DamageTool.damage(triggerer.Player, EDeathCause.SHRED, ELimb.SPINE, owner, Vector3.up, ___playerDamage, 1f, out _, trackKill: true);
                    if (___isBroken)
                        triggerer.Player.life.breakLegs();

                    Data.ServerSpawnLegacyImpact?
                        .Invoke(__instance.transform.position + Vector3.up, Vector3.down, "Flesh", null,
                            Provider.GatherRemoteClientConnectionsWithinSphere(__instance.transform.position, EffectManager.SMALL));

                    BarricadeManager.damage(barricade.model, 5f, 1f, false, triggerer.CSteamID, EDamageOrigin.Trap_Wear_And_Tear);
                    if (data != null)
                        data.LastShreddedBy = default;
                }
                else return false;
            }
        }
        else if (!___isExplosive)
        {
            if (!other.transform.CompareTag("Agent")) return false;
            Zombie zombie = DamageTool.getZombie(other.transform);
            if (zombie != null)
            {
                DamageTool.damageZombie(new DamageZombieParameters(zombie, __instance.transform.forward, ___zombieDamage)
                {
                    instigator = __instance
                }, out _, out _);

                Data.ServerSpawnLegacyImpact?
                    .Invoke(__instance.transform.position + Vector3.up, Vector3.down, zombie.isRadioactive ? "Alien" : "Flesh", null,
                        Provider.GatherRemoteClientConnectionsWithinSphere(__instance.transform.position, EffectManager.SMALL));

                BarricadeManager.damage(barricade.model, zombie.isHyper ? 10f : 5f, 1f, false, CSteamID.Nil, EDamageOrigin.Trap_Wear_And_Tear);
            }
            else
            {
                Animal animal = DamageTool.getAnimal(other.transform);
                DamageTool.damageAnimal(new DamageAnimalParameters(animal, __instance.transform.forward, ___animalDamage)
                {
                    instigator = __instance
                }, out _, out _);

                Data.ServerSpawnLegacyImpact?
                    .Invoke(__instance.transform.position + Vector3.up, Vector3.down, "Flesh", null,
                        Provider.GatherRemoteClientConnectionsWithinSphere(__instance.transform.position, EffectManager.SMALL));

                BarricadeManager.damage(barricade.model, 5f, 1f, false, CSteamID.Nil, EDamageOrigin.Trap_Wear_And_Tear);
            }
        }
        return false;
    }

    // SDG.Unturned.InteractableCharge.detonate
    /// <summary>
    /// Prefix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void PreDetonate(CSteamID killer, InteractableCharge __instance)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player != null && player.TryGetPlayerData(out UCPlayerData data))
        {
            BarricadeDrop? drop = BarricadeManager.FindBarricadeByRootTransform(__instance.transform);
            if (drop != null)
                data.LastChargeDetonated = drop.asset.GUID;
        }
    }
    // SDG.Unturned.InteractableCharge.detonate
    /// <summary>
    /// Postfix of <see cref="InteractableCharge.detonate(CSteamID)"/> to save the last charge detonated.
    /// </summary>
    private static void PostDetonate(CSteamID killer)
    {
        Player? player = PlayerTool.getPlayer(killer);
        if (player != null && player.TryGetPlayerData(out UCPlayerData data))
            data.LastChargeDetonated = default;
    }
    // SDG.Unturned.InteractableVehicle.explode
    /// <summary>
    /// Overriding prefix of <see cref="InteractableVehicle.explode"/> to set an instigator.
    /// </summary>
    private static bool ExplodeVehicle(InteractableVehicle __instance)
    {
        if (!__instance.TryGetComponent(out VehicleComponent vehicleData))
            return true;
        EDamageOrigin lastDamageType = vehicleData.LastDamageOrigin;
        if (lastDamageType == EDamageOrigin.Unknown) return true;
        CSteamID instigator2;
        switch (lastDamageType)
        {
            // no one at fault
            default:
            case EDamageOrigin.VehicleDecay:
                instigator2 = CSteamID.Nil;
                break;
            // blame driver
            case EDamageOrigin.Vehicle_Collision_Self_Damage:
            case EDamageOrigin.Zombie_Swipe:
            case EDamageOrigin.Mega_Zombie_Boulder:
            case EDamageOrigin.Animal_Attack:
            case EDamageOrigin.Zombie_Electric_Shock:
            case EDamageOrigin.Zombie_Stomp:
            case EDamageOrigin.Zombie_Fire_Breath:
            case EDamageOrigin.Radioactive_Zombie_Explosion:
            case EDamageOrigin.Flamable_Zombie_Explosion:
                if (__instance.passengers.Length > 0)
                {
                    if (__instance.passengers[0].player != null)
                    {
                        instigator2 = __instance.passengers[0].player.playerID.steamID;
                        vehicleData.LastInstigator = instigator2.m_SteamID;
                    }
                    // no current driver, check if the last driver exited the vehicle within the last 30 seconds
                    else if (vehicleData.LastDriver != 0 && Time.realtimeSinceStartup - vehicleData.LastDriverTime <= 30f)
                    {
                        instigator2 = new CSteamID(vehicleData.LastDriver);
                        vehicleData.LastInstigator = instigator2.m_SteamID;
                    }
                    else instigator2 = CSteamID.Nil;
                }
                else
                {
                    instigator2 = CSteamID.Nil;
                }
                break;
            // use stored instigator
            case EDamageOrigin.Grenade_Explosion:
            case EDamageOrigin.Rocket_Explosion:
            case EDamageOrigin.Vehicle_Explosion:
            case EDamageOrigin.Useable_Gun:
            case EDamageOrigin.Useable_Melee:
            case EDamageOrigin.Bullet_Explosion:
            case EDamageOrigin.Food_Explosion:
            case EDamageOrigin.Trap_Explosion:
                instigator2 = new CSteamID(vehicleData.LastInstigator);
                break;
        }

        UCPlayerData? data = null;
        if (instigator2 != CSteamID.Nil && instigator2.TryGetPlayerData(out data))
            data.ExplodingVehicle = vehicleData;
        L.LogDebug("Decided explosion instigator: " + instigator2.ToString());
        Vector3 force = new Vector3(
            Random.Range(__instance.asset.minExplosionForce.x, __instance.asset.maxExplosionForce.x),
            Random.Range(__instance.asset.minExplosionForce.y, __instance.asset.maxExplosionForce.y),
            Random.Range(__instance.asset.minExplosionForce.z, __instance.asset.maxExplosionForce.z));
        __instance.GetComponent<Rigidbody>().AddForce(force);
        __instance.GetComponent<Rigidbody>().AddTorque(16f, 0.0f, 0.0f);
        __instance.dropTrunkItems();
        if (__instance.asset.ShouldExplosionCauseDamage)
            DamageTool.explode(__instance.transform.position, 8f, EDeathCause.VEHICLE,
                instigator2, 200f, 200f, 200f, 0.0f, 0.0f, 500f, 2000f, 500f, out _,
                damageOrigin: EDamageOrigin.Vehicle_Explosion);
        for (int index = 0; index < __instance.passengers.Length; ++index)
        {
            Passenger passenger = __instance.passengers[index];
            if (passenger.player != null && passenger.player.player != null && !passenger.player.player.life.isDead)
            {
                if (__instance.asset.ShouldExplosionCauseDamage)
                    passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SPINE, instigator2, out _);
                else
                    VehicleManager.forceRemovePlayer(__instance, passenger.player.playerID.steamID);
            }
        }
        // __instance.DropScrapItems();
        VehicleManager.sendVehicleExploded(__instance);
        EffectAsset effect = __instance.asset.FindExplosionEffectAsset();
        if (effect != null)
        {
            F.TriggerEffectReliable(effect, Provider.GatherRemoteClientConnections(), __instance.transform.position);
        }
        if (data != null)
            data.ExplodingVehicle = null;
        return false;
    }
    // SDG.Unturned.Rocket.OnTriggerEnter
    /// <summary>
    /// Checking for friendlies standing on mortars.
    /// </summary>
    private static bool RocketOnTriggerEnter(Collider other, Rocket __instance, bool ___isExploded)
    {
        if (___isExploded || other.isTrigger || (__instance.ignoreTransform != null && (__instance.ignoreTransform == other.transform || other.transform.IsChildOf(__instance.ignoreTransform))))
            return false;
        if (other.transform.CompareTag("Player"))
        {
            Player? target = DamageTool.getPlayer(other.transform);
            if (target != null)
            {
                Player? pl = PlayerTool.getPlayer(__instance.killer);
                return pl == null || target.GetTeam() != pl.GetTeam();
            }
        }
        return true;
    }
    // SDG.Unturned.PlayerLife.doDamage
    /// <summary>
    /// Actual onDamageRequested event.
    /// </summary>
    private static bool PlayerDamageRequested(PlayerLife __instance, byte amount, Vector3 newRagdoll, EDeathCause newCause, ELimb newLimb, CSteamID newKiller, ref EPlayerKill kill, bool trackKill, ERagdollEffect newRagdollEffect, bool canCauseBleeding)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(__instance.player);
        if (pl is not null && pl.GodMode)
        {
            if (pl.GodMode || Teams.TeamManager.IsInAnyMainOrLobby(pl))
                return false;
        }

        return true;
    }
    // SDG.Unturned.StructureManager.destroyStructure
    /// <summary>
    /// Creates a post-structure destroyed event.
    /// </summary>
    private static void OnStructureDestroyed(StructureDrop structure, byte x, byte y, Vector3 ragdoll, bool wasPickedUp)
    {
        ulong destroyer;
        if (structure.model.TryGetComponent(out DestroyerComponent comp))
        {
            destroyer = comp.Destroyer;
            float time = comp.RelevantTime;
            if (destroyer != 0 && Time.realtimeSinceStartup - time > 1f)
                destroyer = 0ul;
            UnityEngine.Object.Destroy(comp);
        }
        else destroyer = 0ul;

        EventDispatcher.InvokeOnStructureDestroyed(structure, destroyer, ragdoll, wasPickedUp, x, y);
    }
    // SDG.Provider.accept
    /// <summary>
    /// Allows us to defer accepting a player to check stuff with async calls.
    /// </summary>
    internal static readonly List<ulong> Accepted = new List<ulong>(8);

    internal static ulong Accept = 0ul;
    private static bool OnAcceptingPlayer(SteamPending player)
    {
        if (Accept == player.playerID.steamID.m_SteamID)
            return true;
        if (Accepted.Contains(player.playerID.steamID.m_SteamID))
            return false;
        if (!EventDispatcher.InvokeOnAsyncPrePlayerConnect(player))
            return true;
        Accepted.Add(player.playerID.steamID.m_SteamID);
        return false;
    }

    private static bool OnCalculatingPower(InteractablePower __instance, ref bool __result)
    {
        if (!Data.UseElectricalGrid) return true;
        if (Data.Gamemode is not FlagGamemode fg || fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.Disabled)
        {
            __result = false;
            return true;
        }
        if (fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.AllEnabled)
        {
            __result = true;
            return false;
        }
        if (__instance is InteractableObject obj)
        {
            __result = fg.IsPowerObjectEnabled(obj);
            return false;
        }

        __result = fg.IsInteractableEnabled(__instance);
        return false;
    }
    private static bool OnReceiveSwapShirtRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Shirt, pl, page, x, y);
    private static bool OnReceiveSwapPantsRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Pants, pl, page, x, y);
    private static bool OnReceiveSwapHatRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Hat, pl, page, x, y);
    private static bool OnReceiveSwapBackpackRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Backpack, pl, page, x, y);
    private static bool OnReceiveSwapVestRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Vest, pl, page, x, y);
    private static bool OnReceiveSwapMaskRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Mask, pl, page, x, y);
    private static bool OnReceiveSwapGlassesRequest(PlayerClothing __instance, byte page, byte x, byte y) =>
        UCPlayer.FromPlayer(__instance.player) is not { } pl || EventDispatcher.InvokeSwapClothingRequest(ClothingType.Glasses, pl, page, x, y);

    private static bool OnReceiveToggleObjectBinaryStateRequest(in ServerInvocationContext context, byte x, byte y, ushort index, bool isUsed)
    {
        if (!Data.UseElectricalGrid) return true;
        if (!Regions.checkSafe(x, y) || LevelObjects.objects == null)
            return false;
        Player player = context.GetPlayer();
        List<LevelObject> levelObjects = LevelObjects.objects[x, y];
        if (player == null || player.life.isDead || index >= levelObjects.Count)
            return false;
        LevelObject obj = levelObjects[index];
        L.LogDebug($"Received request from {player} for obj {obj.asset.FriendlyName} @ {obj.transform.position} to state: {isUsed}.");
        if (Data.Gamemode is not FlagGamemode fg || fg.ElectricalGridBehavior == FlagGamemode.ElectricalGridBehaivor.Disabled)
            return true;

        return obj.interactable != null && fg.IsPowerObjectEnabled(obj.interactable);
    }
    private static readonly MethodInfo? MthdRemoveItem = typeof(PlayerInventory).GetMethod(nameof(PlayerInventory.removeItem));
    private static readonly MethodInfo? MthdAddItem = typeof(SDG.Unturned.Items).GetMethod(nameof(SDG.Unturned.Items.addItem));
    private static IEnumerable<CodeInstruction> TranspileReceiveSwapOrDragItem(IEnumerable<CodeInstruction> instructions, ILGenerator generator, bool swap)
    {
        List<CodeInstruction> insts = instructions.ToList();
        Label lbl = generator.DefineLabel();
        int c = 0;
        int c2 = 0;
        for (int i = 0; i < insts.Count; ++i)
        {
            CodeInstruction instruction = insts[i];
            if (instruction.Calls(MthdAddItem!))
            {
                ++c2;
                if (c2 == (swap ? 2 : 1))
                {
                    yield return instruction;

                    yield return new CodeInstruction(OpCodes.Ldarg_0);                        // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1);                        // page_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 5 : 4));  // page_1
                    yield return new CodeInstruction(OpCodes.Ldarg_2);                        // x_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 6 : 5));  // x_1
                    yield return new CodeInstruction(OpCodes.Ldarg_3);                        // y_0
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 7 : 6));  // y_1
                    if (swap)
                        yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);           // rot_0
                    else
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);                 
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)(swap ? 8 : 7));  // rot_1
                    yield return new CodeInstruction(swap ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnDraggedOrSwappedItem));
                    L.LogDebug("Patched " + (swap ? "ReceiveSwapItem" : "ReceiveDragItem") + " post event.");
                    continue;
                }
            }
            else if (instruction.opcode == OpCodes.Ldloc_0 && insts.Count > i + 3 && insts[i + 3].Calls(MthdRemoveItem))
            {
                ++c;
                if (c == (swap ? 2 : 1))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);           // this
                    yield return new CodeInstruction(OpCodes.Ldarg_1);           // page_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)4); // page_1
                    yield return new CodeInstruction(OpCodes.Ldarg_2);           // x_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)5); // x_1
                    yield return new CodeInstruction(OpCodes.Ldarg_3);           // y_0
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)6); // y_1
                    yield return new CodeInstruction(OpCodes.Ldarga_S, (byte)7); // rot_1
                    yield return new CodeInstruction(swap ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnDraggingOrSwappingItem));
                    yield return new CodeInstruction(OpCodes.Brtrue, lbl);       // return if cancelled
                    yield return new CodeInstruction(OpCodes.Ret);
                    L.LogDebug("Patched " + (swap ? "ReceiveSwapItem" : "ReceiveDragItem") + " requested event.");
                    generator.MarkLabel(lbl);
                }
            }

            yield return instruction;
        }
    }
    private static IEnumerable<CodeInstruction> TranspileReceiveSwapItem(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) => TranspileReceiveSwapOrDragItem(instructions, generator, true);
    private static IEnumerable<CodeInstruction> TranspileReceiveDragItem(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) => TranspileReceiveSwapOrDragItem(instructions, generator, false);
    private static IEnumerable<CodeInstruction> TranspileReceiveDropItem(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        int lcl2 = method.FindLocalOfType<ItemJar>();
        if (lcl2 < 0)
            L.LogWarning("Unable to find local for ItemJar while transpiling ReceiveDropItem.");

        MethodInfo? itemGetter = typeof(ItemJar).GetProperty(nameof(ItemJar.item), BindingFlags.Public | BindingFlags.Instance)?.GetMethod;
        if (itemGetter == null)
            L.LogWarning("Unable to find 'get ItemJar.item' while transpiling ReceiveDropItem.");
        
        FieldInfo? rotField = typeof(ItemJar).GetField(nameof(ItemJar.rot), BindingFlags.Public | BindingFlags.Instance);
        if (rotField == null)
            L.LogWarning("Unable to find 'ItemJar.rot' while transpiling ReceiveDropItem.");
        
        bool foundOne = false;
        foreach (CodeInstruction instruction in instructions)
        {
            yield return instruction;
            if (!foundOne && instruction.Calls(MthdRemoveItem!))
            {
                foundOne = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);                 // this
                yield return new CodeInstruction(OpCodes.Ldarg_1);                 // page
                yield return new CodeInstruction(OpCodes.Ldarg_2);                 // x
                yield return new CodeInstruction(OpCodes.Ldarg_3);                 // y
                if (lcl2 > -1)
                {
                    if (rotField != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2); // itemjar local
                        yield return new CodeInstruction(OpCodes.Ldfld, rotField);    // .rot
                    }
                    else
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);           // load 0 as backup

                    if (itemGetter != null)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2); // itemjar local
                        yield return new CodeInstruction(OpCodes.Callvirt, itemGetter); // .item
                    }
                    else
                        yield return new CodeInstruction(OpCodes.Ldnull);           // load null as backup
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);               // load 0 as backup
                    yield return new CodeInstruction(OpCodes.Ldnull);               // load null as backup
                }

                yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnDroppedItem));
                L.LogDebug("Patched ReceiveDropItem.");
            }
        }
    }
    private static IEnumerable<CodeInstruction> TranspileReceiveTakeItemRequest(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        FieldInfo? rpcField = typeof(ItemManager).GetField("SendDestroyItem", BindingFlags.NonPublic | BindingFlags.Static);
        if (rpcField == null)
            L.LogWarning("Unable to find 'ItemManager.SendDestroyItem' while transpiling ReceiveTakeItemRequest.");

        int lcl = method.FindLocalOfType<ItemData>();
        if (lcl < 0)
            L.LogWarning("Unable to find local for ItemData while transpiling ReceiveTakeItemRequest.");

        int lcl2 = method.FindLocalOfType<Player>();
        if (lcl2 < 0)
            L.LogWarning("Unable to find local for Player while transpiling ReceiveTakeItemRequest.");

        List<CodeInstruction> insts = instructions.ToList();
        bool foundOne = false;
        bool nextRtnCallEvent = false;
        for (int i = 0; i < insts.Count; ++i)
        {
            CodeInstruction instruction = insts[i];
            if (!foundOne && rpcField != null && instruction.LoadsField(rpcField))
            {
                foundOne = true;
                nextRtnCallEvent = true;
            }
            else if (nextRtnCallEvent && instruction.opcode == OpCodes.Ret)
            {
                nextRtnCallEvent = false;
                yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl2);  // player
                yield return new CodeInstruction(OpCodes.Ldarg_1);              // x
                yield return new CodeInstruction(OpCodes.Ldarg_2);              // y
                yield return new CodeInstruction(OpCodes.Ldarg_3);              // instanceID
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)4);     // to_x
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)5);     // to_y
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)6);     // to_rot
                yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)7);     // to_page
                if (lcl > -1)
                    yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)lcl); // to_page
                else
                    yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Call, PatchUtil.GetMethodInfo(EventDispatcher.OnPickedUpItem));
                L.LogDebug("Patched ReceiveTakeItemRequest.");
            }

            yield return instruction;
        }
    }
}
