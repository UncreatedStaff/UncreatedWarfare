using DanielWillett.ReflectionTools;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Deaths;
public class DeathTracker : BaseReloadSingleton
{
    public const EDeathCause MainCampDeathCauseOffset = (EDeathCause)100;
    public const EDeathCause InEnemyMainDeathCause = (EDeathCause)37;
    private static DeathTracker Singleton;
    private static readonly Dictionary<ulong, InjuredDeathCache> _injuredPlayers = new Dictionary<ulong, InjuredDeathCache>(Provider.maxPlayers);
    public static bool Loaded => Singleton.IsLoaded();
    public DeathTracker() : base("deaths") { }
    public override void Load()
    {
        Singleton = this;
        PlayerLife.onPlayerDied += OnPlayerDied;
        EDeathCause[] causes = Enum.GetValues(typeof(EDeathCause)).Cast<EDeathCause>().ToArray();
        if (causes.Contains(InEnemyMainDeathCause))
            L.LogWarning("Death cause " + InEnemyMainDeathCause + " is already in use to be used as InEnemyMainDeathCause (#" + (int)InEnemyMainDeathCause + ").");
        foreach (EDeathCause cause in causes)
        {
            if (cause >= MainCampDeathCauseOffset)
                L.LogWarning("Death cause " + cause + " is already in use to be used as MainCampDeathCause offset (#" + (int)cause + ") for "
                             + (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset) + ".");
        }
    }
    public override void Reload()
    {
        Localization.Reload();
    }
    public override void Unload()
    {
        PlayerLife.onPlayerDied -= OnPlayerDied;
        Singleton = null!;
    }
    private static readonly InstanceSetter<PlayerLife, bool>? PVPDeathField = Accessor.GenerateInstancePropertySetter<PlayerLife, bool>("wasPvPDeath");
    private static readonly InstanceGetter<InteractableSentry, Player>? SentryTargetPlayerField =
        Accessor.GenerateInstanceGetter<InteractableSentry, Player>("targetPlayer");
    private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
    {
        UCPlayer? dead = UCPlayer.FromPlayer(sender.player);
        if (dead is null) return;
        if (cause == EDeathCause.BLEEDING)
        {
            if (dead.Player.TryGetPlayerData(out UCPlayerData deadData))
            {
                if (deadData.LastBleedingEvent is not null)
                {
                    deadData.LastBleedingArgs.Flags |= DeathFlags.Bleeding;
                    Localization.BroadcastDeath(deadData.LastBleedingEvent, deadData.LastBleedingArgs);
                    goto clear;
                }
            }
        }
        if (ReviveManager.Loaded
            && Data.Is(out IRevives revives)
            && revives.ReviveManager.IsInjured(dead.Steam64)
            && _injuredPlayers.TryGetValue(dead.Steam64, out InjuredDeathCache cache))
        {
            Localization.BroadcastDeath(cache.EventArgs, cache.MessageArgs);
        }
        else
        {
            PlayerDied e = new PlayerDied(dead);
            DeathMessageArgs args = new DeathMessageArgs();
            FillArgs(dead, cause, limb, instigator, ref args, e);
            Localization.BroadcastDeath(e, args);
        }
        clear:
        if (dead.Player.TryGetPlayerData(out UCPlayerData data))
        {
            data.LastInfectableConsumed = default;
            data.LastBleedingArgs = default;
            data.LastBleedingEvent = null;
            data.LastShreddedBy = default;
            data.LastVehicleHitBy = default;
        }
    }
    internal static PlayerDied OnInjured(in DamagePlayerParameters parameters)
    {
        UCPlayer? pl = UCPlayer.FromPlayer(parameters.player);
        if (pl is null) return null!;
        if (parameters.cause == EDeathCause.BLEEDING)
        {
            if (pl.Player.TryGetPlayerData(out UCPlayerData deadData))
            {
                if (deadData.LastBleedingEvent is not null)
                {
                    deadData.LastBleedingArgs.Flags |= DeathFlags.Bleeding;
                    _injuredPlayers.Add(pl.Steam64, new InjuredDeathCache(deadData.LastBleedingEvent, deadData.LastBleedingArgs));
                    return deadData.LastBleedingEvent;
                }
            }
        }
        PlayerDied e = new PlayerDied(pl);
        DeathMessageArgs args = new DeathMessageArgs();
        FillArgs(pl, parameters.cause, parameters.limb, parameters.killer, ref args, e);
        _injuredPlayers.Add(pl.Steam64, new InjuredDeathCache(e, args));
        return e;
    }
    private static void FillArgs(UCPlayer dead, EDeathCause cause, ELimb limb, CSteamID instigator, ref DeathMessageArgs args, PlayerDied e)
    {
        args.DeadPlayerName = dead.Name.CharacterName;
        ulong deadTeam = dead.GetTeam();
        args.DeadPlayerTeam = deadTeam;
        e.DeadTeam = deadTeam;
        args.DeathCause = cause;
        args.Limb = limb;
        args.Flags = DeathFlags.None;
        e.WasTeamkill = false;
        e.WasSuicide = false;
        e.Instigator = instigator;
        e.Limb = limb;
        e.Cause = cause;
        e.Point = dead.Position;
        e.Session = dead.CurrentSession;
        switch (cause)
        {
            // death causes only possible through PvE:
            case InEnemyMainDeathCause:
                args.SpecialKey = "maindeath";
                goto case EDeathCause.ZOMBIE;
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
                args.SpecialKey = "maincamp";
                args.DeathCause = (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset);
                PVPDeathField?.Invoke(dead.Player.life, true);
                break;
        }
        UCPlayer? killer = UCPlayer.FromCSteamID(instigator);
        dead.Player.TryGetPlayerData(out UCPlayerData? deadData);
        UCPlayerData? killerData = null;
        killer?.Player?.TryGetPlayerData(out killerData);
        e.Killer = killer;
        e.KillerSession = killer?.CurrentSession;
        e.KillerPoint = killer?.Position ?? e.Point;

        if (cause == EDeathCause.LANDMINE)
        {
            UCPlayer? triggerer = null;
            BarricadeDrop? drop = null;
            ThrowableComponent? throwable = null;
            bool isTriggerer = false;
            if (killerData != null)
            {
                drop = killerData.ExplodingLandmine;
                if (deadData != null && deadData.TriggeringLandmine == drop)
                {
                    isTriggerer = true;
                    throwable = deadData.TriggeringThrowable;
                }
            }
            else if (deadData != null && deadData.TriggeringLandmine != null)
            {
                isTriggerer = true;
                throwable = deadData.TriggeringThrowable;
            }
            if (drop != null)
            {
                args.Flags |= DeathFlags.Item;
                e.PrimaryAsset = drop.asset.GUID;
                args.ItemName = drop.asset.itemName;
                args.ItemGuid = drop.asset.GUID;
                if (!isTriggerer)
                {
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                    {
                        UCPlayer pl = PlayerManager.OnlinePlayers[i];
                        if (pl.Steam64 != dead.Steam64 && pl.Player.TryGetPlayerData(out UCPlayerData triggererData))
                        {
                            if (triggererData.TriggeringLandmine != null && triggererData.TriggeringLandmine == drop)
                            {
                                triggerer = pl;
                                throwable = triggererData.TriggeringThrowable;
                                break;
                            }
                        }
                    }
                }
            }
            else if (triggerer == null)
            {
                // if it didnt find the triggerer, look for nearby players that just triggered a landmine. Needed in case the owner leaves.
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (pl.Steam64 != dead.Steam64 && pl.Player.TryGetPlayerData(out UCPlayerData triggererData))
                    {
                        if (triggererData.TriggeringLandmine != null && (triggererData.TriggeringLandmine.model.position - dead.Position).sqrMagnitude < 225f)
                        {
                            drop = triggererData.TriggeringLandmine;
                            args.Flags |= DeathFlags.Item;
                            e.PrimaryAsset = drop.asset.GUID;
                            args.ItemName = drop.asset.itemName;
                            args.ItemGuid = drop.asset.GUID;
                            triggerer = pl;
                            throwable = triggererData.TriggeringThrowable;
                            break;
                        }
                    }
                }
            }
            if (triggerer != null)
            {
                // checks if the dead player triggered the trap and it's on their own team.
                if (isTriggerer && drop != null)
                {
                    if (drop.GetServersideData().group.GetTeam() == deadTeam)
                        args.Flags |= DeathFlags.Suicide;
                    else
                        args.Flags &= ~DeathFlags.Killer; // removes the killer as it's them but from the other team
                }
                else if (killer == null || triggerer.Steam64 != killer.Steam64)
                {
                    args.Player3Name = triggerer.Name.CharacterName;
                    args.Player3Team = triggerer.GetTeam();
                    args.Flags |= DeathFlags.Player3;
                    e.Player3 = triggerer;
                    e.Player3Id = triggerer.Steam64;
                    e.Player3Point = triggerer.Position;
                    e.Player3Session = triggerer.CurrentSession;
                    // if all 3 parties are on the same team count it as a teamkill on the triggerer, as it's likely intentional
                    if (triggerer.GetTeam() == deadTeam && killer != null && killer.GetTeam() == deadTeam)
                        args.IsTeamkill = true;
                }
                // if triggerer == placer, count it as a teamkill on the placer
                else if (killer.GetTeam() == deadTeam)
                {
                    args.IsTeamkill = true;
                }
            }
            if (throwable != null && Assets.find(throwable.Throwable) is ItemThrowableAsset asset)
            {
                args.Flags |= DeathFlags.Item2;
                e.SecondaryItem = asset.GUID;
                args.Item2Name = asset.itemName;
            }
        }
        else if (killer is not null && killer.Steam64 == dead.Steam64)
        {
            args.Flags |= DeathFlags.Suicide;
        }
        if (killer is not null)
        {
            if (killer.Steam64 != dead.Steam64)
            {
                args.KillerName = killer.Name.CharacterName;
                args.KillerTeam = killer.GetTeam();
                e.KillerTeam = args.KillerTeam;
                args.Flags |= DeathFlags.Killer;
                args.KillDistance = (killer.Position - dead.Position).magnitude;
                e.KillDistance = args.KillDistance;
                if (deadTeam == args.KillerTeam)
                {
                    args.IsTeamkill = true;
                    e.WasTeamkill = true;
                }
            }
            else
                e.WasSuicide = true;
        }

        switch (cause)
        {
            case EDeathCause.BLEEDING:
                return;
            case EDeathCause.GUN:
            case EDeathCause.MELEE:
            case EDeathCause.SPLASH:
                if (killer is not null && killer.Player!.equipment.asset is not null)
                {
                    args.ItemName = killer.Player.equipment.asset.itemName;
                    args.ItemGuid = killer.Player.equipment.asset.GUID;
                    e.PrimaryAsset = killer.Player.equipment.asset.GUID;
                    args.Flags |= DeathFlags.Item;
                    if (cause != EDeathCause.MELEE)
                    {
                        InteractableVehicle? veh = killer.Player.movement.getVehicle();
                        if (veh != null)
                        {
                            for (int i = 0; i < veh.turrets.Length; ++i)
                            {
                                if (veh.turrets[i].turret != null && veh.turrets[i].turret.itemID == killer.Player.equipment.asset.id)
                                {
                                    e.TurretVehicleOwner = veh.asset.GUID;
                                    args.Item2Guid = veh.asset.GUID;
                                    args.Item2Name = veh.asset.vehicleName;
                                    args.Flags |= DeathFlags.Item2;
                                    if (veh.passengers.Length > 0 && veh.passengers[0].player is { } sp && sp.player != null)
                                    {
                                        e.DriverAssist = UCPlayer.FromSteamPlayer(sp);
                                        if (sp.playerID.steamID.m_SteamID != killer.Steam64)
                                        {
                                            args.Player3Name = e.DriverAssist?.Name.CharacterName ?? sp.playerID.characterName;
                                            args.Player3Team = sp.GetTeam();
                                            if (e.DriverAssist != null)
                                            {
                                                e.Player3 = e.DriverAssist;
                                                e.Player3Id = e.DriverAssist.Steam64;
                                                e.Player3Point = e.DriverAssist.Position;
                                                e.Player3Session = e.DriverAssist.CurrentSession;
                                            }
                                            args.Flags |= DeathFlags.Player3;
                                        }
                                    }
                                    break;
                                }
                            }
                            e.ActiveVehicle = veh;
                        }
                    }
                }
                break;
            case EDeathCause.INFECTION:
                if (deadData != null && deadData.LastInfectableConsumed != default)
                {
                    ItemAsset? a = Assets.find<ItemAsset>(deadData.LastInfectableConsumed);
                    if (a != null)
                    {
                        args.ItemName = a.itemName;
                        e.PrimaryAsset = a.GUID;
                        args.Flags |= DeathFlags.Item;
                        args.ItemGuid = a.GUID;
                    }
                }
                break;
            case EDeathCause.ROADKILL:
                if (deadData != null && deadData.LastVehicleHitBy != default)
                {
                    VehicleAsset? a = Assets.find<VehicleAsset>(deadData.LastVehicleHitBy);
                    if (a != null)
                    {
                        args.ItemName = a.vehicleName;
                        e.PrimaryAsset = a.GUID;
                        e.PrimaryAssetIsVehicle = true;
                        args.Flags |= DeathFlags.Item;
                        args.ItemIsVehicle = true;
                        args.ItemGuid = a.GUID;
                    }
                }
                break;
            case EDeathCause.VEHICLE:
                if (killerData != null && killerData.ExplodingVehicle != null && killerData.ExplodingVehicle.Vehicle != null)
                {
                    args.ItemName = killerData.ExplodingVehicle.Vehicle.asset.vehicleName;
                    args.Flags |= DeathFlags.Item;
                    e.PrimaryAsset = killerData.ExplodingVehicle.Vehicle.asset.GUID;
                    args.ItemIsVehicle = true;
                    args.ItemGuid = killerData.ExplodingVehicle.Vehicle.asset.GUID;
                    if (killerData.ExplodingVehicle.LastItem != default)
                    {
                        if (Assets.find(killerData.ExplodingVehicle.LastItem) is { } a)
                        {
                            args.Item2Name = a.FriendlyName;
                            args.Item2Guid = a.GUID;
                            e.SecondaryItem = a.GUID;
                            args.Flags |= DeathFlags.Item2;
                        }
                    }
                    if (killer is not null)
                    {
                        // removes distance if the driver is blamed
                        InteractableVehicle? veh = killer.Player!.movement.getVehicle();
                        if (veh != null)
                        {
                            if (veh.passengers.Length > 0 && veh.passengers[0].player is not null && veh.passengers[0].player.player != null)
                            {
                                if (killer.Steam64 == veh.passengers[0].player.playerID.steamID.m_SteamID)
                                {
                                    args.Flags = (args.Flags | DeathFlags.NoDistance) & ~DeathFlags.Player3;
                                }
                            }
                        }
                    }
                }
                break;
            case EDeathCause.GRENADE:
                if (killerData != null)
                {
                    ThrowableComponent? comp = killerData.ActiveThrownItems.FirstOrDefault(x => x.isActiveAndEnabled && x.IsExplosive);
                    if (comp == null) break;
                    ItemAsset? a = Assets.find<ItemAsset>(comp.Throwable);
                    if (a != null)
                    {
                        args.ItemName = a.itemName;
                        args.Flags |= DeathFlags.Item;
                        e.PrimaryAsset = a.GUID;
                        args.ItemGuid = a.GUID;
                    }
                }
                break;
            case EDeathCause.SHRED:
                if (killerData != null && killerData.LastShreddedBy != default)
                {
                    Asset? a = Assets.find(killerData.LastShreddedBy);
                    if (a != null)
                    {
                        args.ItemName = a.FriendlyName;
                        args.Flags |= DeathFlags.Item;
                        e.PrimaryAsset = a.GUID;
                        args.ItemGuid = a.GUID;
                    }
                }
                args.IsTeamkill = false;
                e.WasTeamkill = false;
                break;
            case EDeathCause.MISSILE:
                if (killer is not null)
                {
                    if (killer.Player!.TryGetPlayerData(out UCPlayerData data) && data.LastRocketShot != default && Assets.find(data.LastRocketShot) is ItemAsset asset)
                    {
                        args.ItemName = asset.itemName;
                        args.ItemGuid = asset.GUID;
                        e.PrimaryAsset = asset.GUID;
                        args.Flags |= DeathFlags.Item;
                        e.TurretVehicleOwner = data.LastRocketShotVehicleAsset;
                    }
                    else if (killer.Player!.equipment.asset is not null)
                    {
                        args.ItemName = killer.Player.equipment.asset.itemName;
                        args.ItemGuid = killer.Player.equipment.asset.GUID;
                        e.PrimaryAsset = killer.Player.equipment.asset.GUID;
                        args.Flags |= DeathFlags.Item;
                        InteractableVehicle? veh = killer.Player.movement.getVehicle();
                        if (veh != null)
                        {
                            for (int i = 0; i < veh.turrets.Length; ++i)
                            {
                                if (veh.turrets[i].turret != null && veh.turrets[i].turret.itemID == killer.Player.equipment.asset.id)
                                {
                                    e.TurretVehicleOwner = veh.asset.GUID;
                                    break;
                                }
                            }
                        }
                    }
                }
                break;
            case EDeathCause.CHARGE:
                if (killerData != null && killerData.LastChargeDetonated != default)
                {
                    Asset? a = Assets.find(killerData.LastChargeDetonated);
                    if (a != null)
                    {
                        args.ItemName = a.FriendlyName;
                        e.PrimaryAsset = a.GUID;
                        args.Flags |= DeathFlags.Item;
                        args.ItemGuid = a.GUID;
                    }/*
                    if (killer != null && killer.Player.equipment.asset != null && killer.Player.equipment.asset.useableType == typeof(UseableDetonator))
                    {
                        args.Item2Name = killer.Player.equipment.asset.itemName;
                        args.Flags |= EDeathFlags.ITEM2;
                    }*/
                }
                else if (deadData != null && deadData.LastExplosiveConsumed != default)
                {
                    Asset? a = Assets.find(deadData.LastExplosiveConsumed);
                    if (a != null)
                    {
                        args.ItemName = a.FriendlyName;
                        e.PrimaryAsset = a.GUID;
                        args.Flags = DeathFlags.Item; // intentional
                        args.ItemGuid = a.GUID;
                        args.SpecialKey = "explosive-consumable";
                    }
                }
                break;
            case EDeathCause.SENTRY:
                if (instigator != CSteamID.Nil)
                {
                    List<BarricadeDrop> drops = UCBarricadeManager.GetBarricadesWhere(x =>
                        x.GetServersideData().owner == instigator.m_SteamID &&
                        x.interactable is InteractableSentry sentry &&
                        SentryTargetPlayerField != null &&
                        SentryTargetPlayerField(sentry) is { } target &&
                        target != null && target.channel.owner.playerID.steamID.m_SteamID ==
                        dead.Steam64
                    );
                    if (drops.Count > 0)
                    {
                        BarricadeDrop drop = drops[0];
                        InteractableSentry sentry = (InteractableSentry)drop.interactable;
                        args.ItemName = drop.asset.itemName;
                        args.ItemGuid = drop.asset.GUID;
                        args.Flags |= DeathFlags.Item;
                        e.PrimaryAsset = drop.asset.GUID;
                        Item? item = sentry.items.items.FirstOrDefault()?.item;
                        if (item != null && Assets.find(EAssetType.ITEM, item.id) is ItemAsset a)
                        {
                            args.Item2Name = a.itemName;
                            args.Item2Guid = a.GUID;
                            e.SecondaryItem = a.GUID;
                            args.Flags |= DeathFlags.Item2;
                        }
                    }
                }
                break;
            case >= MainCampDeathCauseOffset:
                EDeathCause mainCampCause = args.DeathCause;
                GetItems(mainCampCause, instigator.m_SteamID, killer?.Player, killerData, deadData, dead, out Asset? item1, out Asset? item2);
                if (item1 != null)
                {
                    args.ItemName = item1.FriendlyName;
                    args.ItemIsVehicle = item1 is VehicleAsset;
                    args.Flags |= DeathFlags.Item;
                    e.PrimaryAsset = item1.GUID;
                    e.PrimaryAssetIsVehicle = args.ItemIsVehicle;
                    args.ItemGuid = item1.GUID;
                }
                if (item2 != null)
                {
                    args.Item2Name = item2.FriendlyName;
                    args.Item2Guid = item2.GUID;
                    e.SecondaryItem = item2.GUID;
                    args.Flags |= DeathFlags.Item2;
                }
                break;
        }
    }


    private static void GetItems(EDeathCause cause, ulong killerId, Player? killer, UCPlayerData? killerData, UCPlayerData? deadData, Player dead, out Asset? item1, out Asset? item2)
    {
        if (killer != null && killerData is null)
            killer.TryGetPlayerData(out killerData);
        if (deadData is null)
            dead.TryGetPlayerData(out deadData);
        item2 = null;
    repeat:
        switch (cause)
        {
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
            case >= MainCampDeathCauseOffset:
                cause = (EDeathCause)((int)cause - (int)MainCampDeathCauseOffset);
                if (cause >= MainCampDeathCauseOffset || killer == null)
                {
                    item1 = null;
                    return;
                }
                (dead, killer) = (killer, dead);
                goto repeat;
            case EDeathCause.GUN:
            case EDeathCause.MELEE:
            case EDeathCause.SPLASH:
#pragma warning disable IDE0031
                item1 = killer != null ? killer.equipment.asset : null;
#pragma warning restore IDE0031
                break;
            case EDeathCause.BLEEDING:
                if (deadData != null)
                {
                    item1 = Assets.find(deadData.LastBleedingArgs.ItemGuid);
                    item2 = Assets.find(deadData.LastBleedingArgs.Item2Guid);
                }
                else item1 = null;
                break;
            case EDeathCause.INFECTION:
                item1 = deadData != null && deadData.LastInfectableConsumed != default ? Assets.find(deadData.LastInfectableConsumed) : null;
                break;
            case EDeathCause.ROADKILL:
                item1 = deadData != null && deadData.LastVehicleHitBy != default ? Assets.find(deadData.LastInfectableConsumed) : null;
                break;
            case EDeathCause.VEHICLE:
                if (killerData != null && killerData.ExplodingVehicle != null)
                {
                    item1 = killerData.ExplodingVehicle.Vehicle.asset;
                    item2 = Assets.find(killerData.ExplodingVehicle.LastItem);
                }
                else item1 = null;
                break;
            case EDeathCause.GRENADE:
                if (killerData != null)
                {
                    ThrowableComponent? comp = killerData.ActiveThrownItems.FirstOrDefault(x => x.IsExplosive);
                    item1 = comp == null ? null : Assets.find(comp.Throwable);
                }
                else item1 = null;
                break;
            case EDeathCause.SHRED:
                item1 = deadData != null && deadData.LastShreddedBy != default ? Assets.find(deadData.LastShreddedBy) : null;
                break;
            case EDeathCause.LANDMINE:
                BarricadeDrop? drop = null;
                ThrowableComponent? throwable = null;
                if (killerData != null)
                {
                    drop = killerData.ExplodingLandmine;
                    if (drop != null && drop == killerData.TriggeringLandmine)
                    {
                        throwable = killerData.TriggeringThrowable;
                    }
                }
                else if (deadData != null && deadData.TriggeringLandmine != null)
                {
                    drop = deadData.TriggeringLandmine;
                    throwable = deadData.TriggeringThrowable;
                }
                else
                {
                    // if it didnt find the triggerer, look for nearby players that just triggered a landmine. Needed in case the owner leaves.
                    for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                    {
                        UCPlayer pl = PlayerManager.OnlinePlayers[i];
                        if (pl.Steam64 != dead.channel.owner.playerID.steamID.m_SteamID && pl.Player.TryGetPlayerData(out UCPlayerData triggererData))
                        {
                            if (triggererData.TriggeringLandmine != null && (triggererData.TriggeringLandmine.model.position - dead.transform.position).sqrMagnitude < 225f)
                            {
                                drop = triggererData.TriggeringLandmine;
                                throwable = triggererData.TriggeringThrowable;
                                break;
                            }
                        }
                    }
                }

                item1 = drop?.asset;
                item2 = throwable != null ? Assets.find(throwable.Throwable) : null;
                break;
            case EDeathCause.MISSILE:
                item1 = killerData != null && killerData.LastRocketShot != default ? Assets.find(killerData.LastRocketShot) : null;
                break;
            case EDeathCause.CHARGE:
                item1 = killerData != null && killerData.LastChargeDetonated != default ? Assets.find(killerData.LastChargeDetonated) : null;
                break;
            case EDeathCause.SENTRY:
                if (killerId != default)
                {
                    List<BarricadeDrop> drops = UCBarricadeManager.GetBarricadesWhere(x =>
                        x.GetServersideData().owner == killerId &&
                        x.interactable is InteractableSentry sentry &&
                        SentryTargetPlayerField != null &&
                        SentryTargetPlayerField(sentry) is { } target &&
                        target != null && target.channel.owner.playerID.steamID.m_SteamID ==
                        dead.channel.owner.playerID.steamID.m_SteamID
                    );
                    if (drops.Count == 0)
                    {
                        item1 = null;
                    }
                    else
                    {
                        drop = drops[0];
                        InteractableSentry sentry = (InteractableSentry)drop.interactable;
                        item1 = drop.asset;
                        Item? item = sentry.items.items.FirstOrDefault()?.item;
                        item2 = item != null ? Assets.find(EAssetType.ITEM, item.id) as ItemAsset : null;
                    }
                }
                else item1 = null;
                break;
        }
    }
    internal static void OnWillStartBleeding(ref DamagePlayerParameters parameters)
    {
        if (parameters.player.TryGetPlayerData(out UCPlayerData data))
        {
            if (parameters.cause != EDeathCause.BLEEDING)
            {
                UCPlayer? dead = UCPlayer.FromPlayer(parameters.player);
                if (dead is null) return;
                DeathMessageArgs args = new DeathMessageArgs();
                PlayerDied e = new PlayerDied(dead);
                FillArgs(dead, parameters.cause, parameters.limb, parameters.killer, ref args, e);
                data.LastBleedingArgs = args;
                data.LastBleedingEvent = e;
            }
        }
    }
    internal static void ReviveManagerUnloading()
    {
        _injuredPlayers.Clear();
    }
    internal static void RemovePlayerInfo(ulong steam64)
    {
        _injuredPlayers.Remove(steam64);
    }
    private readonly struct InjuredDeathCache
    {
        public readonly PlayerDied EventArgs;
        public readonly DeathMessageArgs MessageArgs;
        public InjuredDeathCache(PlayerDied eventArgs, DeathMessageArgs messageArgs)
        {
            EventArgs = eventArgs;
            MessageArgs = messageArgs;
        }
    }
}
