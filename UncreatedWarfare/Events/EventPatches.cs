using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Events;
internal static class EventPatches
{
    internal static void TryPatchAll()
    {
        PatchMethod(
            typeof(BarricadeManager)
            .GetMethod(nameof(BarricadeManager.destroyBarricade), 
                BindingFlags.Static | BindingFlags.Public, 
                null, 
                new Type[] { typeof(BarricadeDrop), typeof(byte), typeof(byte), typeof(ushort) }, 
                null),
            postfix: GetMethodInfo(DestroyBarricadePostFix));

        PatchMethod(typeof(VehicleManager).GetMethod("addVehicle", BindingFlags.Instance | BindingFlags.NonPublic), postfix: GetMethodInfo(OnVehicleSpawned));

        PatchMethod(typeof(InteractableTrap).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic), prefix: GetMethodInfo(TrapOnTriggerEnter));

        PatchMethod(typeof(InteractableCharge).GetMethod("detonate", BindingFlags.Instance | BindingFlags.Public),
            prefix: GetMethodInfo(PreDetonate), postfix: GetMethodInfo(PostDetonate));

        PatchMethod(typeof(InteractableVehicle).GetMethod("explode", BindingFlags.Instance | BindingFlags.NonPublic),
            prefix: GetMethodInfo(ExplodeVehicle));
    }
    private static MethodInfo GetMethodInfo(Delegate method)
    {
        try
        {
            return method.GetMethodInfo();
        }
        catch (MemberAccessException)
        {
            L.LogWarning("Was unable to get a method info from a delegate.");
            return null!;
        }
    }
    private static void PatchMethod(Delegate original, Delegate? prefix = null, Delegate? postfix = null, Delegate? transpiler = null, Delegate? finalizer = null)
    {
        if (original is null || (prefix is null && postfix is null && transpiler is null && finalizer is null)) return;
        try
        {
            MethodInfo? originalInfo    = original.Method;
            MethodInfo? prefixInfo      = prefix?.Method;
            MethodInfo? postfixInfo     = prefix?.Method;
            MethodInfo? transpilerInfo  = prefix?.Method;
            MethodInfo? finalizerInfo   = prefix?.Method;
            if (originalInfo is null)
            {
                L.LogError("Error getting method info for patching.");
                return;
            }
            if (prefixInfo is null && postfixInfo is null && transpilerInfo is null && finalizerInfo is null)
            {
                L.LogError("Error getting method info for patching " +      originalInfo.FullDescription());
                return;
            }
            if (prefix is not null && prefixInfo is null)
                L.LogError("Error getting prefix info for patching " +      originalInfo.FullDescription());
            if (postfix is not null && postfixInfo is null)
                L.LogError("Error getting postfix info for patching " +     originalInfo.FullDescription());
            if (transpiler is not null && transpilerInfo is null)
                L.LogError("Error getting transpiler info for patching " +  originalInfo.FullDescription());
            if (finalizer is not null && finalizerInfo is null)
                L.LogError("Error getting finalizer info for patching " +   originalInfo.FullDescription());
            PatchMethod(originalInfo, prefixInfo, postfixInfo, transpilerInfo, finalizerInfo);
        }
        catch (MemberAccessException ex)
        {
            L.LogError("Error getting method info for patching.");
            L.LogError(ex);
        }
    }
    private static void PatchMethod(MethodInfo original, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
    {
        if (original is null || (prefix is null && postfix is null && transpiler is null && finalizer is null)) return;

        HarmonyMethod? prfx2 = prefix is null       ? null : new HarmonyMethod(prefix);
        HarmonyMethod? pofx2 = postfix is null      ? null : new HarmonyMethod(postfix);
        HarmonyMethod? tplr2 = transpiler is null   ? null : new HarmonyMethod(transpiler);
        HarmonyMethod? fnlr2 = finalizer is null    ? null : new HarmonyMethod(finalizer);
        try
        {
            Patches.Patcher.Patch(original, prefix: prfx2, postfix: pofx2, transpiler: tplr2, finalizer: fnlr2);
        }
        catch (Exception ex)
        {
            L.LogError("Error patching " + original.FullDescription());
            L.LogError(ex);
        }
    }
    // SDG.Unturned.BarricadeManager
    /// <summary>
    /// Postfix of <see cref="BarricadeManager.destroyBarricade(BarricadeRegion, byte, byte, ushort, ushort)"/> to invoke <see cref="BarricadeDestroyedHandler"/>.
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
    /// Postfix of <see cref="VehicleManager.addVehicle(a lot)"/> to call OnVehicleSpawned
    /// </summary>
    private static void OnVehicleSpawned(Guid assetGuid,
        ushort skinID,
        ushort mythicID,
        float roadPosition,
        Vector3 point,
        Quaternion angle,
        bool sirens,
        bool blimp,
        bool headlights,
        bool taillights,
        ushort fuel,
        bool isExploded,
        ushort health,
        ushort batteryCharge,
        CSteamID owner,
        CSteamID group,
        bool locked,
        CSteamID[] passengers,
        byte[][] turrets,
        uint instanceID,
        byte tireAliveMask,
        NetId netId, InteractableVehicle __result)
    {
        if (__result != null)
        {
            EventDispatcher.InvokeOnVehicleSpawned(__result);
        }
    }
    // SDG.Unturned.InteractableTrap.OnTriggerEnter
    /// <summary>
    /// Prefix of <see cref="InteractableTrap.OnTriggerEnter(Collider)"/> to call OnVehicleSpawned
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
            Data.Gamemode is null || Data.Gamemode.State != Gamemodes.EState.ACTIVE 
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
            for (int i = 0; i < vehicle.passengers.Length; ++i)
            {
                if (vehicle.passengers[i].player == null)
                    continue;
                triggerer = UCPlayer.FromPlayer(vehicle.passengers[i].player.player);
            }
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
                    if (___explosion2 != 0 && Assets.find(EAssetType.EFFECT, ___explosion2) is EffectAsset asset)
                    {
                        EffectManager.triggerEffect(new TriggerEffectParameters(asset)
                        {
                            position = position,
                            relevantDistance = EffectManager.LARGE,
                            reliable = true
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
                BarricadeManager.damage(barricade.model, zombie.isHyper ? 10f : 5f, 1f, false, CSteamID.Nil, EDamageOrigin.Trap_Wear_And_Tear);
            }
            else
            {
                Animal animal = DamageTool.getAnimal(other.transform);
                DamageTool.damageAnimal(new DamageAnimalParameters(animal, __instance.transform.forward, ___animalDamage)
                {
                    instigator = __instance
                }, out _, out _);
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
        // item drops commented out
#if false
        if (__instance.asset.dropsTableId > 0)
        {
            int num = Mathf.Clamp(Random.Range(__instance.asset.dropsMin, __instance.asset.dropsMax), 0, 100);
            for (int index = 0; index < num; ++index)
            {
                float f = Random.Range(0.0f, Mathf.PI * 2);
                ushort newID = SpawnTableTool.resolve(__instance.asset.dropsTableId);
                if (newID != 0)
                {
                    ItemManager.dropItem(new Item(newID, EItemOrigin.NATURE),
                        __instance.transform.position + new Vector3(Mathf.Sin(f) * 3f, 1f, Mathf.Cos(f) * 3f),
                        false, Dedicator.IsDedicatedServer, true);
                }
            }
        }
#endif
        VehicleManager.sendVehicleExploded(__instance);
        if (__instance.asset.explosion != 0)
            EffectManager.sendEffect(__instance.asset.explosion, Level.size, __instance.transform.position);
        if (data != null)
            data.ExplodingVehicle = null;
        return false;
    }
}
