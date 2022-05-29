using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Deaths;
public class DeathTracker : BaseReloadSingleton
{
    public const EDeathCause MAIN_CAMP = (EDeathCause)36;
    public const EDeathCause MAIN_DEATH = (EDeathCause)37;
    private static DeathTracker Singleton;
    public static bool Loaded => Singleton.IsLoaded();
    public DeathTracker() : base("deaths") { }
    public override void Load()
    {
        Singleton = this;
        PlayerLife.onPlayerDied += OnPlayerDied;
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
    private static readonly FieldInfo PVPDeathField = typeof(PlayerLife).GetField("<wasPvPDeath>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
    {
        UCPlayer? dead = UCPlayer.FromPlayer(sender.player);
        if (dead is null) return;
        PlayerDied e = new PlayerDied(dead);
        DeathMessageArgs args = new DeathMessageArgs();
        args.DeadPlayerName = F.GetPlayerOriginalNames(sender.player).CharacterName;
        ulong deadTeam = dead.GetTeam();
        args.DeadPlayerTeam = deadTeam;
        args.DeathCause = cause;
        args.Limb = limb;
        args.Flags = EDeathFlags.NONE;
        e.Intigator = instigator;
        e.Limb = limb;
        e.Cause = cause;
        switch (cause)
        {
            // death causes only possible through PvE:
            case EDeathCause.ACID:
            case EDeathCause.ANIMAL:
            case EDeathCause.BONES:
            case EDeathCause.BOULDER:
            case EDeathCause.BREATH:
            case EDeathCause.BURNER:
            case EDeathCause.BURNING:
            case EDeathCause.FOOD:
            case EDeathCause.FREEZING:
            case EDeathCause.INFECTION:
            case EDeathCause.SPARK:
            case EDeathCause.SPIT:
            case EDeathCause.SUICIDE:
            case EDeathCause.WATER:
            case EDeathCause.ZOMBIE:
                Localization.BroadcastDeath(e, args);
                return;
            case MAIN_CAMP:
            case MAIN_DEATH:
                PVPDeathField.SetValue(sender, true);
                break;
        }
        UCPlayer? killer = UCPlayer.FromCSteamID(instigator);
        dead.Player.TryGetPlayerData(out UCPlayerData? deadData);
        e.Killer = killer;

        if (cause == EDeathCause.LANDMINE)
        {
            UCPlayer? triggerer = null;
            BarricadeDrop? drop = null;
            ThrowableComponent? throwable = null;
            UCPlayerData? killerData = null;
            if (killer is not null)
                killer.Player.TryGetPlayerData(out killerData);
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
                args.Flags |= EDeathFlags.ITEM;
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
                            args.Flags |= EDeathFlags.ITEM;
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
                        args.Flags |= EDeathFlags.SUICIDE;
                    else
                        args.Flags &= ~EDeathFlags.KILLER; // removes the killer as it's them but from the other team
                }
                else if (killer == null || triggerer.Steam64 != killer.Steam64)
                {
                    FPlayerName names = F.GetPlayerOriginalNames(triggerer);
                    args.Player3Name = names.CharacterName;
                    args.Player3Team = triggerer.GetTeam();
                    args.Flags |= EDeathFlags.PLAYER3;
                    // if all 3 parties are on the same team count it as a teamkill on the triggerer, as it's likely intentional
                    if (triggerer.GetTeam() == deadTeam && killer != null && killer.GetTeam() == deadTeam)
                        args.isTeamkill = true;
                }
                // if triggerer == placer, count it as a teamkill on the placer
                else if (killer.GetTeam() == deadTeam)
                {
                    args.isTeamkill = true;
                }
            }
            if (throwable != null && Assets.find(throwable.Throwable) is ItemThrowableAsset asset)
            {
                args.Flags |= EDeathFlags.ITEM2;
                args.Item2Name = asset.itemName;
            }

            L.Log("Dead: " + dead.CharacterName);
            if (triggerer != null)
                L.Log("Triggerer: " + triggerer.CharacterName);
            if (killer != null)
                L.Log("Placer: " + killer.CharacterName);
            if (args.ItemName != null)
                L.Log("Item: " + args.ItemName);
            if (args.Item2Name != null)
                L.Log("Item2: " + args.Item2Name);
            L.Log("Flags: " + args.Flags.ToString());
        }
        if (killer is not null)
        {
            if (killer.Steam64 != dead.Steam64)
            {
                args.KillerName = F.GetPlayerOriginalNames(killer).CharacterName;
                args.KillerTeam = killer.GetTeam();
                args.Flags |= EDeathFlags.KILLER;
            }
        }

        Localization.BroadcastDeath(e, args);
    }
}
