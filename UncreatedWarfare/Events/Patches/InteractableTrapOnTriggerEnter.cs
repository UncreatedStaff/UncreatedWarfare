using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Collections.Generic;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Patches;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class InteractableTrapOnTriggerEnter : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(InteractableTrap).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);

        if (_target != null)
        {
            Patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for trap trigger events.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<InteractableTrap>(isStatic: false)
                .WithParameter<Collider>("other")
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;
        
        Patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for trap trigger events.", Accessor.Formatter.Format(_target));
        _target = null;
    }

    // SDG.Unturned.InteractableTrap.OnTriggerEnter
    /// <summary>
    /// Prefix of <see cref="InteractableTrap.OnTriggerEnter(Collider)"/> to change explosion behavior.
    /// </summary>
    private static bool Prefix(Collider other, InteractableTrap __instance, float ___lastActive, float ___setupDelay, ref float ___lastTriggered,
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
        {
            return false;
        }

        if (__instance.transform.TryGetComponent(out TrapTrackingComponent trapComponent) && trapComponent.Triggers.Contains(other.gameObject))
            return false;

        ___lastTriggered = time;

        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(__instance.gameObject.transform.parent) ?? BarricadeManager.FindBarricadeByRootTransform(__instance.gameObject.transform);
        if (barricade == null)
        {
            return false;
        }

        ulong triggerTeam = 0;
        UCPlayer? playerTriggerer = null;
        Zombie? zombieTriggerer = null;
        Animal? animalTriggerer = null;
        ThrowableComponent? throwable = null;
        ItemThrowableAsset? throwableAsset = null;
        if (other.transform.CompareTag("Player"))
        {
            playerTriggerer = UCPlayer.FromPlayer(DamageTool.getPlayer(other.transform));
            if (playerTriggerer == null)
            {
                return false;
            }

            triggerTeam = playerTriggerer.GetTeam();
        }
        else if (other.transform.CompareTag("Vehicle"))
        {
            InteractableVehicle? vehicle = DamageTool.getVehicle(other.transform);
            
            if (vehicle == null)
            {
                return false;
            }

            if (vehicle.passengers.Length > 0 && vehicle.passengers[0].player != null)
            {
                playerTriggerer = UCPlayer.FromPlayer(vehicle.passengers[0].player.player);
            }

            if (playerTriggerer == null)
            {
                if (vehicle.TryGetComponent(out VehicleComponent comp2))
                    playerTriggerer = UCPlayer.FromID(comp2.LastDriver);
                if (playerTriggerer == null)
                {
                    return false;
                }
            }

            triggerTeam = playerTriggerer.GetTeam();
        }
        else if (other.transform.CompareTag("Agent"))
        {
            zombieTriggerer = DamageTool.getZombie(other.transform);
            if (zombieTriggerer == null)
            {
                animalTriggerer = DamageTool.getAnimal(other.transform);
                zombieTriggerer = null;
            }
        }
        else if (other.TryGetComponent(out throwable!))
        {
            playerTriggerer = UCPlayer.FromSteamPlayer(PlayerTool.getSteamPlayer(throwable.Owner));
            throwableAsset = Assets.find<ItemThrowableAsset>(throwable.Throwable);

            if (playerTriggerer == null)
            {
                return false;
            }
            
            triggerTeam = playerTriggerer.GetTeam();
        }

        BarricadeData serversideData = barricade.GetServersideData();
        TriggerTrapRequested args = new TriggerTrapRequested
        {
            Trap = __instance,
            Barricade = barricade,
            ServersideData = serversideData,
            BarricadeOwner = UCPlayer.FromID(serversideData.owner),
            TriggerCollider = other,
            TriggerObject = other.gameObject,
            TriggeringPlayer = playerTriggerer,
            TriggeringThrowable = throwable,
            TriggeringThrowableAssset = throwableAsset,
            TriggeringZombie = zombieTriggerer,
            TriggeringAnimal = animalTriggerer,
            TriggeringTeam = triggerTeam,
            PlayerDamage = ___playerDamage,
            AnimalDamage = ___animalDamage,
            ZombieDamage = ___zombieDamage,
            ExplosiveBarricadeDamage = ___barricadeDamage,
            ExplosionEffect = Assets.FindEffectAssetByGuidOrLegacyId(__instance.trapDetonationEffectGuid, ___explosion2),
            ExplosiveRange = ___range2,
            ExplosiveLaunchSpeed = ___explosionLaunchSpeed,
            ExplosiveObjectDamage = ___objectDamage,
            ExplosiveResourceDamage = ___resourceDamage,
            IsExplosive = ___isExplosive,
            ExplosiveStructureDamage = ___structureDamage,
            ExplosiveVehicleDamage = ___vehicleDamage,
            ShouldBreakLegs = ___isBroken,
            WearAndTearDamage = zombieTriggerer != null && zombieTriggerer.isHyper ? 10f : 5f
        };

        if (___isExplosive && (zombieTriggerer != null || animalTriggerer != null) || !___isExplosive && playerTriggerer == null && animalTriggerer == null && zombieTriggerer == null)
        {
            args.CancelAction();
        }

        UniTask<bool> task = WarfareModule.EventDispatcher.DispatchEventAsync(args);
        if (task.Status != UniTaskStatus.Pending)
        {
            if (args.IsActionCancelled)
            {
                return false;
            }

            TriggerTrap(args);
            return false;
        }

        trapComponent = __instance.gameObject.GetOrAddComponent<TrapTrackingComponent>();
        trapComponent.Triggers.Add(args.TriggerObject);

        UniTask.Create(async () =>
        {
            bool isCancelled = await task;

            await UniTask.SwitchToMainThread();

            if (args.Trap.gameObject.TryGetComponent(out TrapTrackingComponent trapComponent))
                trapComponent.Triggers.Remove(args.TriggerObject);

            if (isCancelled || args.ServersideData.barricade.isDead)
                return;

            TriggerTrap(args);
        });

        return false;
    }

    private static void TriggerTrap(TriggerTrapRequested args)
    {
        if (args.IsExplosive)
        {
            PlayerDeathTrackingComponent? ownerData = null,
                                          triggererData = null;

            if (args.BarricadeOwner is { IsOnline: true })
            {
                ownerData = PlayerDeathTrackingComponent.GetOrAdd(args.BarricadeOwner.Player);

                ownerData.OwnedTrap = args.Barricade;
            }

            if (args.TriggeringPlayer is { IsOnline: true })
            {
                triggererData = PlayerDeathTrackingComponent.GetOrAdd(args.TriggeringPlayer.Player);

                triggererData.TriggeredTrapExplosive = args.Barricade;
                triggererData.ThrowableTrapTrigger = args.TriggeringThrowable;
            }

            Vector3 position = args.ServersideData.point;
            DamageTool.explode(new ExplosionParameters(position, args.ExplosiveRange, EDeathCause.LANDMINE, new CSteamID(args.ServersideData.owner))
            {
                playerDamage = args.PlayerDamage,
                zombieDamage = args.ZombieDamage,
                animalDamage = args.AnimalDamage,
                barricadeDamage = args.ExplosiveBarricadeDamage,
                structureDamage = args.ExplosiveStructureDamage,
                vehicleDamage = args.ExplosiveVehicleDamage,
                resourceDamage = args.ExplosiveResourceDamage,
                objectDamage = args.ExplosiveObjectDamage,
                damageOrigin = EDamageOrigin.Trap_Explosion,
                launchSpeed = args.ExplosiveLaunchSpeed
            }, out _);

            if (args.ExplosionEffect != null)
            {
                EffectManager.triggerEffect(new TriggerEffectParameters(args.ExplosionEffect)
                {
                    position = position,
                    relevantDistance = EffectManager.LARGE
                });
            }

            if (ownerData != null)
            {
                ownerData.OwnedTrap = null;
            }
            if (triggererData != null)
            {
                triggererData.TriggeredTrapExplosive = null;
                triggererData.ThrowableTrapTrigger = null;
            }
        }
        else
        {

            PlayerDeathTrackingComponent? data = null;

            if (args.TriggeringZombie != null)
            {
                DamageTool.damageZombie(new DamageZombieParameters(args.TriggeringZombie, args.Trap.transform.forward, args.ZombieDamage)
                {
                    instigator = args.Trap
                }, out _, out _);
            }
            else if (args.TriggeringAnimal != null)
            {
                DamageTool.damageAnimal(new DamageAnimalParameters(args.TriggeringAnimal, args.Trap.transform.forward, args.AnimalDamage)
                {
                    instigator = args.Trap
                }, out _, out _);
            }
            else if (args.TriggeringPlayer is { IsOnline: true })
            {
                if (args.TriggeringPlayer.IsInVehicle)
                    return;

                data = PlayerDeathTrackingComponent.GetOrAdd(args.TriggeringPlayer.Player);
                data.LastShreddedBy = AssetLink.Create(args.Barricade.asset);

                DamageTool.damage(args.TriggeringPlayer.Player, EDeathCause.SHRED, ELimb.SPINE, new CSteamID(args.ServersideData.owner), Vector3.up, args.PlayerDamage, 1f, out _, trackKill: true);
                if (args.ShouldBreakLegs && !args.TriggeringPlayer.Player.life.isDead)
                {
                    args.TriggeringPlayer.Player.life.breakLegs();
                }
            }

            Data.ServerSpawnLegacyImpact?.Invoke(args.ServersideData.point + Vector3.up, Vector3.down, "Flesh", null, Provider.GatherRemoteClientConnectionsWithinSphere(args.ServersideData.point, EffectManager.SMALL));

            if (args.WearAndTearDamage > 0)
            {
                BarricadeManager.damage(args.Barricade.model, args.WearAndTearDamage, 1f, false, args.TriggeringPlayer?.CSteamID ?? CSteamID.Nil, EDamageOrigin.Trap_Wear_And_Tear);
            }
            else if (args.WearAndTearDamage < 0)
            {
                BarricadeManager.repair(args.Barricade.model, -args.WearAndTearDamage, 1f, args.TriggeringPlayer?.CSteamID ?? CSteamID.Nil);
            }

            if (data != null)
                data.LastShreddedBy = default;
        }

        TrapTriggered finalArgs = new TrapTriggered
        {
            Barricade = args.Barricade,
            ServersideData = args.ServersideData,
            Trap = args.Trap,
            TriggeringPlayer = args.TriggeringPlayer,
            TriggeringZombie = args.TriggeringZombie,
            TriggeringAnimal = args.TriggeringAnimal,
            TriggerCollider = args.TriggerCollider,
            TriggeringThrowableAssset = args.TriggeringThrowableAssset,
            TriggeringThrowable = args.TriggeringThrowable,
            BarricadeOwner = args.BarricadeOwner,
            TriggerObject = args.TriggerObject
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(finalArgs, CancellationToken.None);
    }

    // ReSharper disable once ClassNeverInstantiated.Local (added as component)

    /// <summary>
    /// Handles syncronizing trap events so that a trap can't go off while another event is pending.
    /// </summary>
    private class TrapTrackingComponent : MonoBehaviour, IManualOnDestroy
    {
        /// <summary>
        /// If the <see cref="TriggerTrapRequested"/> event is pending on this trap.
        /// </summary>
        public readonly List<GameObject> Triggers = new List<GameObject>(1);
        void IManualOnDestroy.ManualOnDestroy()
        {
            Destroy(this);
        }
    }
}