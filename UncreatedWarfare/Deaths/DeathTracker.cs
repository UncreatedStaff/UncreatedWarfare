using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.Injures;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Deaths;
public class DeathTracker : IHostedService
{
    private readonly ILogger<DeathTracker> _logger;
    private readonly DeathMessageResolver _deathMessageResolver;
    private readonly IPlayerService _playerService;

    public const EDeathCause MainCampDeathCauseOffset = (EDeathCause)100;
    public const EDeathCause InEnemyMainDeathCause = (EDeathCause)37;

    private static readonly InstanceSetter<PlayerLife, bool>? PVPDeathField = Accessor.GenerateInstancePropertySetter<PlayerLife, bool>("wasPvPDeath");
    private static readonly InstanceGetter<InteractableSentry, Player>? SentryTargetPlayerField = Accessor.GenerateInstanceGetter<InteractableSentry, Player>("targetPlayer");

    public DeathTracker(ILogger<DeathTracker> logger, DeathMessageResolver deathMessageResolver, IPlayerService playerService)
    {
        _logger = logger;
        _playerService = playerService;
        _deathMessageResolver = deathMessageResolver;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        CommandWindow.shouldLogDeaths = false;

        // not using event dispatcher for this because this class is responsible for dispatching the player died event.
        PlayerLife.onPlayerDied += OnPlayerDied;
        UseableGun.onProjectileSpawned += UseableGunOnProjectileSpawned;
        UseableThrowable.onThrowableSpawned += OnThrowableSpawned;
        UseableConsumeable.onConsumePerformed += UseableConsumeableOnConsumePerformed;

        EDeathCause[] causes = Enum.GetValues(typeof(EDeathCause)).Cast<EDeathCause>().ToArray();
        if (causes.Contains(InEnemyMainDeathCause))
        {
            _logger.LogWarning("Death cause {0} is already in use to be used as InEnemyMainDeathCause (#{1}).",
                InEnemyMainDeathCause, (int)InEnemyMainDeathCause
            );
        }

        foreach (EDeathCause cause in causes)
        {
            if (cause >= MainCampDeathCauseOffset)
            {
                _logger.LogWarning("Death cause {0} is already in use to be used as MainCampDeathCause offset (#{1}) for {2}.",
                    cause, (int)cause, (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset)
                );
            }
        }


        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        PlayerLife.onPlayerDied -= OnPlayerDied;
        UseableGun.onProjectileSpawned -= UseableGunOnProjectileSpawned;
        UseableThrowable.onThrowableSpawned -= OnThrowableSpawned;
        UseableConsumeable.onConsumePerformed -= UseableConsumeableOnConsumePerformed;

        return UniTask.CompletedTask;
    }

    private static void UseableConsumeableOnConsumePerformed(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset)
    {
        PlayerDeathTrackingComponent deathTrackingComponent = PlayerDeathTrackingComponent.GetOrAdd(instigatingPlayer);

        deathTrackingComponent.LastExplosiveConsumed = null;

        if (consumeableAsset.IsExplosive)
        {
            deathTrackingComponent.LastExplosiveConsumed = AssetLink.Create(consumeableAsset);
        }
        else if (consumeableAsset.virus != 0)
        {
            deathTrackingComponent.LastInfectionItemConsumed = AssetLink.Create(consumeableAsset);
        }
    }

    private static void UseableGunOnProjectileSpawned(UseableGun sender, GameObject projectile)
    {
        PlayerDeathTrackingComponent deathTrackingComponent = PlayerDeathTrackingComponent.GetOrAdd(sender.player);

        ItemGunAsset gun = sender.equippedGunAsset;

        deathTrackingComponent.LastRocketShot = AssetLink.Create(gun);

        InteractableVehicle? vehicle = sender.player.movement.getVehicle();
        if (vehicle is null)
        {
            deathTrackingComponent.LastRocketShotFromVehicle = null;
            return;
        }

        byte seat = sender.player.movement.getSeat();
        if (seat >= vehicle.passengers.Length || vehicle.passengers[seat].turret == null || !deathTrackingComponent.LastRocketShot.MatchId(vehicle.passengers[seat].turret.itemID))
        {
            deathTrackingComponent.LastRocketShotFromVehicle = null;
            return;
        }

        deathTrackingComponent.LastRocketShotFromVehicle = vehicle;

        if (seat != 0 && vehicle.isDriven)
        {
            deathTrackingComponent.LastRocketShotFromVehicleDriverAssist = vehicle.passengers[0].player.playerID.steamID;
        }
    }

    private void OnThrowableSpawned(UseableThrowable useable, GameObject throwable)
    {
        ThrowableComponent comp = throwable.AddComponent<ThrowableComponent>();
        PlayerDeathTrackingComponent deathTrackingComponent = PlayerDeathTrackingComponent.GetOrAdd(useable.player);

        WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(useable.player);

        ItemThrowableAsset asset = useable.equippedThrowableAsset;

        deathTrackingComponent.ActiveThrownItems.Add(comp);
        comp.Throwable = asset;
        comp.Owner = player;
        comp.ToRemoveFrom = deathTrackingComponent.ActiveThrownItems;
        comp.Team = player?.Team ?? Team.NoTeam;
    }

    private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
    {
        WarfarePlayer dead = _playerService.GetOnlinePlayer(sender.player);

        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(sender.player);

        UniTask.Create(async () =>
        {
            if (cause != EDeathCause.BLEEDING || comp?.BleedOutInfo == null)
            {
                PlayerInjureComponent? injureComp = dead.ComponentOrNull<PlayerInjureComponent>();
                if (injureComp != null && injureComp is { State: PlayerHealthState.Injured, PendingDeathInfo: not null })
                {
                    PlayerDied deathInfo = injureComp.PendingDeathInfo;
                    injureComp.PendingDeathInfo = null;
                    await _deathMessageResolver.BroadcastDeath(deathInfo);
                }
                else
                {
                    PlayerDied e;
                    
                    if (dead.Data.TryRemove("LastPlayerDying", out object? dyingArgs) && dyingArgs is PlayerDying dying)
                    {
                        e = new PlayerDied(in dying.Parameters) { Player = dead };
                    }
                    else
                    {
                        DamagePlayerParameters parameters = new DamagePlayerParameters(dead.UnturnedPlayer)
                        {
                            limb = limb,
                            cause = cause,
                            killer = instigator
                        };
                        e = new PlayerDied(in parameters) { Player = dead };
                    }
                    FillArgs(dead, cause, limb, instigator, e);
                    await _deathMessageResolver.BroadcastDeath(e);
                }
            }
            else
            {
                comp.BleedOutInfo.MessageFlags |= DeathFlags.Bleeding;
                await _deathMessageResolver.BroadcastDeath(comp.BleedOutInfo);
            }
        });


        if (comp == null)
        {
            return;
        }

        comp.LastInfectionItemConsumed = null;
        comp.BleedOutInfo = null;
        comp.LastShreddedBy = null;
        comp.LastRoadkillVehicle = null;
    }

    internal PlayerDied OnInjured(in DamagePlayerParameters parameters)
    {
        WarfarePlayer pl = _playerService.GetOnlinePlayer(parameters.player);

        if (parameters.cause == EDeathCause.BLEEDING && PlayerDeathTrackingComponent.GetOrAdd(pl.UnturnedPlayer) is { BleedOutInfo: { } bleedOutInfo })
        {
            bleedOutInfo.MessageFlags |= DeathFlags.Bleeding;
            pl.Component<PlayerInjureComponent>().PendingDeathInfo = bleedOutInfo;
            return bleedOutInfo;
        }

        PlayerDied e = new PlayerDied(in parameters) { Player = pl };
        FillArgs(pl, parameters.cause, parameters.limb, parameters.killer, e);
        pl.Component<PlayerInjureComponent>().PendingDeathInfo = e;
        return e;
    }

    internal void FillArgs(WarfarePlayer dead, EDeathCause cause, ELimb limb, CSteamID instigator, PlayerDied e)
    {
        Team deadTeam = dead.Team;
        e.DeadTeam = deadTeam;
        e.MessageFlags = DeathFlags.None;
        e.WasTeamkill = false;
        e.WasSuicide = false;
        e.Instigator = instigator;
        e.Limb = limb;
        e.Cause = cause;
        e.MessageCause = cause;
        e.Point = dead.Position;
        e.Session = dead.CurrentSession;
        if (e.Session != null)
            Interlocked.Increment(ref e.Session.EventCount);
        e.TimeDeployed = (float)(dead.ComponentOrNull<DeploymentComponent>()?.GetTimeDeployed().TotalSeconds ?? 0d);
        switch (cause)
        {
            // death causes only possible through PvE:
            case InEnemyMainDeathCause:
                e.MessageKey = "maindeath";
                return;

            case EDeathCause.ACID:
            case EDeathCause.ANIMAL:
            case EDeathCause.BONES:
            case EDeathCause.BOULDER:
            case EDeathCause.BREATH:
            case EDeathCause.BURNER:
            case EDeathCause.BURNING:
            case EDeathCause.FOOD:
            case EDeathCause.FREEZING:
            case EDeathCause.SPARK:
            case EDeathCause.SPIT:
            case EDeathCause.SUICIDE:
            case EDeathCause.WATER:
            case EDeathCause.ZOMBIE:
                return;

            case >= MainCampDeathCauseOffset:
                e.MessageKey = "maincamp";
                e.MessageCause = (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset);
                PVPDeathField?.Invoke(dead.UnturnedPlayer.life, true);
                break;
        }
        WarfarePlayer? killer = _playerService.GetOnlinePlayerOrNull(instigator);
        PlayerDeathTrackingComponent? deadData = dead.UnturnedPlayer == null ? null : PlayerDeathTrackingComponent.GetOrAdd(dead.UnturnedPlayer);
        PlayerDeathTrackingComponent? killerData = null;
        if (killer is { IsOnline: true })
        {
            killerData = PlayerDeathTrackingComponent.GetOrAdd(killer.UnturnedPlayer);
        }
        
        e.Killer = killer;
        if (killer != null)
        {
            e.KillerSession = killer.CurrentSession;
            if (e.KillerSession != null)
                Interlocked.Increment(ref e.KillerSession.EventCount);
            e.KillerPoint = killer.Position;
            KitPlayerComponent killerKitComp = killer.Component<KitPlayerComponent>();
            e.KillerKitName = killerKitComp.ActiveKitId;
            e.KillerClass = killerKitComp.ActiveClass;
            e.KillerBranch = killerKitComp.ActiveBranch;
        }
        else
        {
            e.KillerPoint = e.Point;
        }

        if (cause == EDeathCause.LANDMINE)
        {
            WarfarePlayer? triggerer = null;
            BarricadeDrop? drop = null;
            ThrowableComponent? throwable = null;
            bool isTriggerer = false;

            if (killerData != null)
            {
                drop = killerData.OwnedTrap;
                if (deadData != null && deadData.TriggeredTrapExplosive == drop)
                {
                    isTriggerer = true;
                    throwable = deadData.ThrowableTrapTrigger;
                }
            }
            else if (deadData != null && deadData.TriggeredTrapExplosive != null)
            {
                isTriggerer = true;
                throwable = deadData.ThrowableTrapTrigger;
            }

            if (drop != null)
            {
                e.MessageFlags |= DeathFlags.Item;
                e.PrimaryAsset = AssetLink.Create(drop.asset);
                if (!isTriggerer)
                {
                    foreach (WarfarePlayer player in _playerService.OnlinePlayers)
                    {
                        if (player.Equals(dead))
                            continue;

                        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(player.UnturnedPlayer);
                        if (comp.TriggeredTrapExplosive != drop)
                            continue;

                        triggerer = player;
                        throwable = comp.ThrowableTrapTrigger;
                        break;
                    }
                }
            }
            else if (triggerer == null)
            {
                // if it didnt find the triggerer, look for nearby players that just triggered a landmine. Needed in case the owner leaves.
                foreach (WarfarePlayer player in _playerService.OnlinePlayers)
                {
                    if (player.Equals(dead)
                        || !player.UnturnedPlayer.TryGetComponent(out PlayerDeathTrackingComponent triggererData)
                        || triggererData.TriggeredTrapExplosive == null
                        || !((triggererData.TriggeredTrapExplosive.model.position - dead.Position).sqrMagnitude < 400f /* 20m */)
                       )
                    {
                        continue;
                    }

                    drop = triggererData.TriggeredTrapExplosive;
                    e.MessageFlags |= DeathFlags.Item;
                    e.PrimaryAsset = AssetLink.Create(drop.asset);
                    triggerer = player;
                    throwable = triggererData.ThrowableTrapTrigger;
                    break;
                }
            }

            if (triggerer != null)
            {
                // checks if the dead player triggered the trap and it's on their own team.
                if (isTriggerer && drop != null)
                {
                    if (deadTeam.IsFriendly(new CSteamID(drop.GetServersideData().group)))
                        e.MessageFlags |= DeathFlags.Suicide;
                    else
                        e.MessageFlags &= ~DeathFlags.Killer; // removes the killer as it's them but from the other team
                }
                else if (killer == null || triggerer.Steam64 != killer.Steam64)
                {
                    e.MessageFlags |= DeathFlags.Player3;
                    e.ThirdParty = triggerer;
                    e.ThirdPartyId = triggerer.Steam64;
                    e.ThirdPartyPoint = triggerer.Position;
                    e.ThirdPartySession = triggerer.CurrentSession;
                    if (e.ThirdPartySession != null)
                        Interlocked.Increment(ref e.ThirdPartySession.EventCount);
                    e.ThirdPartyTeam = triggerer.Team;

                    // if all 3 parties are on the same team count it as a teamkill on the triggerer, as it's likely intentional
                    if (triggerer.Team.IsFriendly(deadTeam) && killer != null && killer.Team.IsFriendly(deadTeam))
                    {
                        e.WasTeamkill = true;
                        e.ThirdPartyAtFault = true;
                    }
                }
                // if triggerer == placer, count it as a teamkill on the placer
                else if (killer.Team.IsFriendly(deadTeam))
                {
                    e.WasTeamkill = true;
                }
            }
            if (throwable != null && throwable.Throwable != null)
            {
                e.MessageFlags |= DeathFlags.Item2;
                e.SecondaryAsset = AssetLink.Create(throwable.Throwable);
            }
        }
        else if (killer is not null && killer.Steam64 == dead.Steam64)
        {
            e.MessageFlags |= DeathFlags.Suicide;
        }

        if (killer is not null)
        {
            if (killer.Steam64 != dead.Steam64)
            {
                e.KillerTeam = killer.Team;
                e.MessageFlags |= DeathFlags.Killer;
                e.KillDistance = (killer.Position - dead.Position).magnitude;
                if (deadTeam.IsFriendly(e.KillerTeam))
                {
                    e.WasTeamkill = true;
                }
            }
            else
            {
                e.WasSuicide = true;
            }
        }

        InteractableVehicle? veh;
        switch (cause)
        {
            case EDeathCause.BLEEDING:
                return;
            case EDeathCause.GUN:
            case EDeathCause.MELEE:
            case EDeathCause.SPLASH:
                if (killer == null || killer.UnturnedPlayer.equipment.asset == null)
                    break;

                e.PrimaryAsset = AssetLink.Create(killer.UnturnedPlayer.equipment.asset);
                e.MessageFlags |= DeathFlags.Item;

                if (cause == EDeathCause.MELEE)
                    break;

                veh = killer.UnturnedPlayer.movement.getVehicle();
                if (veh == null || veh.isDead)
                    break;

                // check if the player is on a turret, use the vehicle as item2, and give the driver third-party.
                for (int i = 0; i < veh.turrets.Length; ++i)
                {
                    if (veh.turrets[i].turret == null || veh.turrets[i].turret.itemID != killer.UnturnedPlayer.equipment.asset.id)
                    {
                        continue;
                    }

                    e.TurretVehicleOwner = AssetLink.Create(veh.asset);
                    e.SecondaryAsset = e.TurretVehicleOwner;
                    e.MessageFlags |= DeathFlags.Item2;

                    if (veh.passengers.Length > 0 && veh.passengers[0].player is { } sp && sp.player != null)
                    {
                        e.DriverAssist = _playerService.GetOnlinePlayer(sp);
                        if (e.DriverAssist != null && sp.playerID.steamID.m_SteamID != killer.Steam64.m_SteamID)
                        {
                            e.ThirdParty = e.DriverAssist;
                            e.ThirdPartyId = e.DriverAssist.Steam64;
                            e.ThirdPartyPoint = e.DriverAssist.Position;
                            e.ThirdPartySession = e.DriverAssist.CurrentSession;
                            if (e.ThirdPartySession != null)
                                Interlocked.Increment(ref e.ThirdPartySession.EventCount);
                            e.MessageFlags |= DeathFlags.Player3;
                        }
                    }

                    break;
                }

                e.ActiveVehicle = veh;
                break;

            case EDeathCause.INFECTION:
                if (deadData != null && deadData.LastInfectionItemConsumed != null)
                {
                    ItemAsset? lastConsumed = deadData.LastInfectionItemConsumed.GetAsset();
                    if (lastConsumed != null)
                    {
                        e.PrimaryAsset = deadData.LastInfectionItemConsumed;
                        e.MessageFlags |= DeathFlags.Item;
                    }
                }
                break;

            case EDeathCause.ROADKILL:
                if (deadData != null && deadData.LastRoadkillVehicle != null)
                {
                    VehicleAsset? lastRoadkilledBy = deadData.LastRoadkillVehicle.GetAsset();
                    if (lastRoadkilledBy != null)
                    {
                        e.PrimaryAsset = deadData.LastRoadkillVehicle;
                        e.MessageFlags |= DeathFlags.Item;
                    }
                }
                break;

            case EDeathCause.VEHICLE:
                if (killerData == null || killerData.LastVehicleExploded == null || killerData.LastVehicleExploded.Vehicle == null)
                {
                    break;
                }

                WarfareVehicle vComp = killerData.LastVehicleExploded;
                e.MessageFlags |= DeathFlags.Item;
                e.PrimaryAsset = AssetLink.Create(vComp.Vehicle.asset);

                if (vComp.DamageTracker.LatestInstigatorWeapon != null)
                {
                    e.SecondaryAsset = AssetLink.Create(vComp.DamageTracker.LatestInstigatorWeapon);
                    e.MessageFlags |= DeathFlags.Item2;
                }

                if (killer is not null)
                {
                    // removes distance if the driver is blamed
                    veh = killer.UnturnedPlayer.movement.getVehicle();
                    if (veh != null
                        && veh.passengers.Length > 0
                        && veh.passengers[0].player?.player != null
                        && killer.Steam64.m_SteamID == veh.passengers[0].player.playerID.steamID.m_SteamID)
                    {
                        e.MessageFlags = (e.MessageFlags | DeathFlags.NoDistance) & ~DeathFlags.Player3;
                    }
                }
                break;
            
            case EDeathCause.GRENADE:
                if (killerData == null)
                    break;

                ThrowableComponent? comp = killerData.ActiveThrownItems.FirstOrDefault(x => x.isActiveAndEnabled && x.Throwable is { isExplosive: true });
                if (comp == null)
                    break;

                ItemThrowableAsset throwable = comp.Throwable!; // null checked in linq ^
                e.MessageFlags |= DeathFlags.Item;
                e.PrimaryAsset = AssetLink.Create(throwable);
                break;

            case EDeathCause.SHRED:
                if (deadData != null && deadData.LastShreddedBy != null)
                {
                    ItemBarricadeAsset? trap = deadData.LastShreddedBy.GetAsset();
                    if (trap != null)
                    {
                        e.MessageFlags |= DeathFlags.Item;
                        e.PrimaryAsset = AssetLink.Create(trap);
                    }
                }

                e.WasTeamkill = false;
                break;
            
            case EDeathCause.MISSILE:
                if (killer == null)
                    break;

                if (killerData != null && killerData.LastRocketShot?.GetAsset() is { } lastRocketShot)
                {
                    e.PrimaryAsset = AssetLink.Create(lastRocketShot);
                    e.MessageFlags |= DeathFlags.Item;
                    InteractableVehicle? turretOwner = killerData.LastRocketShotFromVehicle;

                    if (turretOwner is null)
                        break;

                    e.TurretVehicleOwner = AssetLink.Create(turretOwner.asset);
                    e.SecondaryAsset = e.TurretVehicleOwner;
                    e.MessageFlags |= DeathFlags.Item2;

                    CSteamID? driverAssist = killerData.LastRocketShotFromVehicleDriverAssist;
                    if (!driverAssist.HasValue || driverAssist.Value.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                        break;

                    e.DriverAssist = _playerService.GetOnlinePlayerOrNull(driverAssist.Value.m_SteamID);
                    if (e.DriverAssist != null && driverAssist.Value.m_SteamID != killer.Steam64.m_SteamID)
                    {
                        e.ThirdParty = e.DriverAssist;
                        e.ThirdPartyId = e.DriverAssist.Steam64;
                        e.ThirdPartyPoint = e.DriverAssist.Position;
                        e.ThirdPartySession = e.DriverAssist.CurrentSession;
                        if (e.ThirdPartySession != null)
                            Interlocked.Increment(ref e.ThirdPartySession.EventCount);
                        e.MessageFlags |= DeathFlags.Player3;
                    }
                }
                else if (killer.UnturnedPlayer.equipment.asset != null)
                {
                    e.PrimaryAsset = AssetLink.Create(killer.UnturnedPlayer.equipment.asset);
                    e.MessageFlags |= DeathFlags.Item;

                    veh = killer.UnturnedPlayer.movement.getVehicle();
                    if (veh == null)
                        break;

                    for (int i = 0; i < veh.turrets.Length; ++i)
                    {
                        TurretInfo? turretInfo = veh.turrets[i].turret;
                        if (turretInfo == null || turretInfo.itemID != killer.UnturnedPlayer.equipment.asset.id)
                            continue;

                        e.TurretVehicleOwner = AssetLink.Create(veh.asset);
                        e.SecondaryAsset = e.TurretVehicleOwner;
                        e.MessageFlags |= DeathFlags.Item2;

                        if (veh.passengers.Length > 0 && veh.passengers[0].player is { } sp && sp.player != null)
                        {
                            e.DriverAssist = _playerService.GetOnlinePlayerOrNull(sp);
                            if (e.DriverAssist != null && sp.playerID.steamID.m_SteamID != killer.Steam64.m_SteamID)
                            {
                                e.ThirdParty = e.DriverAssist;
                                e.ThirdPartyId = e.DriverAssist.Steam64;
                                e.ThirdPartyPoint = e.DriverAssist.Position;
                                e.ThirdPartySession = e.DriverAssist.CurrentSession;
                                if (e.ThirdPartySession != null)
                                    Interlocked.Increment(ref e.ThirdPartySession.EventCount);
                                e.MessageFlags |= DeathFlags.Player3;
                            }
                        }
                        break;
                    }
                }
                break;

            case EDeathCause.CHARGE:
                if (killerData != null && killerData.LastChargeDetonated != null)
                {
                    ItemBarricadeAsset? lastCharge = killerData.LastChargeDetonated.GetAsset();
                    if (lastCharge != null)
                    {
                        e.PrimaryAsset = killerData.LastChargeDetonated;
                        e.MessageFlags |= DeathFlags.Item;
                    }
                }
                else if (deadData != null && deadData.LastExplosiveConsumed != null)
                {
                    ItemConsumeableAsset? lastCharge = deadData.LastExplosiveConsumed.GetAsset();
                    if (lastCharge != null)
                    {
                        e.PrimaryAsset = deadData.LastExplosiveConsumed;
                        e.MessageFlags = DeathFlags.Item; // intentional
                        e.MessageKey = "explosive-consumable";
                    }
                }
                break;

            case EDeathCause.SENTRY:
                if (instigator.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                    break;

                // find target sentry
                List<BarricadeInfo> drops = BarricadeUtility.EnumerateBarricades()
                    .Where(x =>
                        x.Drop != null &&
                        x.Drop.GetServersideData().owner == e.Instigator.m_SteamID &&
                        x.Drop.interactable is InteractableSentry sentry &&
                        SentryTargetPlayerField?.Invoke(sentry) is { } target &&
                        target != null && target.channel.owner.playerID.steamID.m_SteamID == e.Steam64.m_SteamID
                    ).ToList();

                if (drops.Count == 0)
                {
                    break;
                }

                Vector3 pos = e.Point;

                // closest sentry
                BarricadeDrop drop = drops.Aggregate((closest, next) => (closest.Drop.GetServersideData().point - pos).sqrMagnitude > (next.Drop.GetServersideData().point - pos).sqrMagnitude ? next : closest).Drop;

                InteractableSentry sentry = (InteractableSentry)drop.interactable;
                e.MessageFlags |= DeathFlags.Item;
                e.PrimaryAsset = AssetLink.Create(drop.asset);
                Item? item = sentry.items.items.FirstOrDefault()?.item;
                if (item?.GetAsset() is ItemGunAsset gun)
                {
                    e.SecondaryAsset = AssetLink.Create(gun);
                    e.MessageFlags |= DeathFlags.Item2;
                }
                
                break;

            case >= MainCampDeathCauseOffset:
                EDeathCause mainCampCause = e.MessageCause;

                GetItems(mainCampCause, instigator.m_SteamID, killer?.UnturnedPlayer, killerData, deadData, dead.UnturnedPlayer!, out IAssetLink<Asset>? primary, out IAssetLink<Asset>? secondary);

                if (primary != null)
                {
                    e.MessageFlags |= DeathFlags.Item;
                    e.PrimaryAsset = primary;
                }

                if (secondary != null)
                {
                    e.SecondaryAsset = secondary;
                    e.MessageFlags |= DeathFlags.Item2;
                }

                break;
        }
    }

    private void GetItems(EDeathCause cause, ulong killerId, Player? killer, PlayerDeathTrackingComponent? killerData, PlayerDeathTrackingComponent? deadData, Player dead, out IAssetLink<Asset>? item1, out IAssetLink<Asset>? item2)
    {
        item2 = null;
    repeat:
        switch (cause)
        {
            // died from main camping
            case >= MainCampDeathCauseOffset:
                cause = (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset);
                if (cause >= MainCampDeathCauseOffset || killer == null)
                {
                    item1 = null;
                    return;
                }
                (dead, killer) = (killer, dead);
                goto repeat;

            // death causes that dont have a related item:
            default:
            case InEnemyMainDeathCause:
            case EDeathCause.BONES:
            case EDeathCause.FREEZING:
            case EDeathCause.BURNING:
            case EDeathCause.FOOD:
            case EDeathCause.WATER:
            case EDeathCause.ZOMBIE:
            case EDeathCause.ANIMAL:
            case EDeathCause.SUICIDE:
            case EDeathCause.KILL:
            case EDeathCause.PUNCH:
            case EDeathCause.BREATH:
            case EDeathCause.ARENA:
            case EDeathCause.ACID:
            case EDeathCause.BOULDER:
            case EDeathCause.BURNER:
            case EDeathCause.SPIT:
            case EDeathCause.SPARK:
                item1 = null;
                return;

            case EDeathCause.GUN:
            case EDeathCause.MELEE:
            case EDeathCause.SPLASH:
#pragma warning disable IDE0031
                item1 = killer != null ? AssetLink.Create(killer.equipment.asset) : null;
#pragma warning restore IDE0031
                break;

            case EDeathCause.BLEEDING:
                if (deadData != null)
                {
                    item1 = deadData.BleedOutInfo?.PrimaryAsset;
                    item2 = deadData.BleedOutInfo?.SecondaryAsset;
                }
                else item1 = null;
                break;

            case EDeathCause.INFECTION:
                item1 = deadData?.LastInfectionItemConsumed;
                break;

            case EDeathCause.ROADKILL:
                item1 = deadData?.LastRoadkillVehicle;
                break;

            case EDeathCause.VEHICLE:
                if (killerData != null && killerData.LastVehicleExploded != null)
                {
                    item1 = AssetLink.Create(killerData.LastVehicleExploded.Vehicle.asset);
                    if (killerData.LastVehicleExploded.DamageTracker.LatestInstigatorWeapon != null)
                        item2 = AssetLink.Create(killerData.LastVehicleExploded.DamageTracker.LatestInstigatorWeapon);
                }
                else item1 = null;
                break;

            case EDeathCause.GRENADE:
                if (killerData != null)
                {
                    ThrowableComponent? comp = killerData.ActiveThrownItems.FirstOrDefault(x => x.Throwable is { isExplosive: true });
                    item1 = comp == null ? null : AssetLink.Create(comp.Throwable);
                }
                else item1 = null;
                break;

            case EDeathCause.SHRED:
                item1 = deadData?.LastShreddedBy;
                break;

            case EDeathCause.LANDMINE:
                BarricadeDrop? drop = null;
                ThrowableComponent? throwable = null;
                if (killerData != null)
                {
                    drop = killerData.OwnedTrap;
                    if (drop != null && drop == killerData.TriggeredTrapExplosive)
                    {
                        throwable = killerData.ThrowableTrapTrigger;
                    }
                }
                else if (deadData != null && deadData.TriggeredTrapExplosive != null)
                {
                    drop = deadData.TriggeredTrapExplosive;
                    throwable = deadData.ThrowableTrapTrigger;
                }
                else
                {
                    // if it didnt find the triggerer, look for nearby players that just triggered a landmine. Needed in case the owner leaves.
                    foreach (WarfarePlayer player in _playerService.OnlinePlayers)
                    {
                        if (player.Steam64.m_SteamID == dead.channel.owner.playerID.steamID.m_SteamID
                            || !player.UnturnedPlayer.TryGetComponent(out PlayerDeathTrackingComponent triggererData)
                            || triggererData.TriggeredTrapExplosive == null
                            || !((triggererData.TriggeredTrapExplosive.model.position - dead.transform.position).sqrMagnitude < 400f /* 20m */)
                           )
                        {
                            continue;
                        }

                        drop = triggererData.TriggeredTrapExplosive;
                        throwable = triggererData.ThrowableTrapTrigger;
                        break;
                    }
                }

                item1 = drop != null ? AssetLink.Create(drop.asset) : null;
                item2 = throwable != null ? AssetLink.Create(throwable.Throwable) : null;
                break;

            case EDeathCause.MISSILE:
                item1 = killerData?.LastRocketShot;
                break;

            case EDeathCause.CHARGE:
                item1 = killerData?.LastChargeDetonated;
                break;

            case EDeathCause.SENTRY:
                if (killerId == 0ul)
                {
                    item1 = null;
                    break;
                }

                // find target sentry
                List<BarricadeInfo> drops = BarricadeUtility.EnumerateBarricades()
                    .Where(x =>
                        x.Drop != null &&
                        x.Drop.GetServersideData().owner == killerId &&
                        x.Drop.interactable is InteractableSentry sentry &&
                        SentryTargetPlayerField?.Invoke(sentry) is { } target &&
                        target != null && target.channel.owner.playerID.steamID.m_SteamID ==
                        dead.channel.owner.playerID.steamID.m_SteamID
                    ).ToList();

                if (drops.Count == 0)
                {
                    item1 = null;
                }
                else
                {
                    Vector3 pos = dead.transform.position;

                    // closest sentry
                    drop = drops.Aggregate((closest, next) => (closest.Drop.GetServersideData().point - pos).sqrMagnitude > (next.Drop.GetServersideData().point - pos).sqrMagnitude ? next : closest).Drop;

                    InteractableSentry sentry = (InteractableSentry)drop.interactable;
                    item1 = AssetLink.Create(drop.asset);

                    if (sentry.items.getItemCount() > 0)
                    {
                        Item item = sentry.items.getItem(0).item;
                        if (Assets.find(EAssetType.ITEM, item.id) is ItemGunAsset gun)
                        {
                            item2 = AssetLink.Create(gun);
                        }
                    }
                    else
                    {
                        item2 = null;
                    }
                }

                break;
        }
    }
    internal void OnWillStartBleeding(ref DamagePlayerParameters parameters)
    {
        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(parameters.player);
        
        if (parameters.cause == EDeathCause.BLEEDING)
            return;

        WarfarePlayer dead = _playerService.GetOnlinePlayer(parameters.player);

        PlayerDied e = new PlayerDied(in parameters) { Player = dead };
        FillArgs(dead, parameters.cause, parameters.limb, parameters.killer, e);
        e.WasBleedout = true;

        comp.BleedOutInfo = e;
    }
}
