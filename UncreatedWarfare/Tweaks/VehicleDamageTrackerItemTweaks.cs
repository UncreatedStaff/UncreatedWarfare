using System;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

namespace Uncreated.Warfare.Tweaks;
internal sealed class VehicleDamageTrackerItemTweaks : IEventListener<VehiclePreDamaged>
{
    private readonly IPlayerService _playerService;

    public VehicleDamageTrackerItemTweaks(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [EventListener(MustRunInstantly = true, Priority = int.MaxValue)]
    void IEventListener<VehiclePreDamaged>.HandleEvent(VehiclePreDamaged e, IServiceProvider serviceProvider)
    {
        WarfareVehicle vehicle = e.Vehicle;
        VehicleDamageTracker tracker = vehicle.DamageTracker;

        if (!e.InstantaneousInstigator.HasValue)
        {
            if (e.InstantaneousDamageOrigin == EDamageOrigin.Trap_Explosion)
            {
                tracker.RecordDamage(EDamageOrigin.Trap_Explosion);
            }
            return;
        }

        CSteamID instigator64 = e.InstantaneousInstigator.Value;

        WarfarePlayer? instigator = _playerService.GetOnlinePlayerOrNull(instigator64);

        PlayerDeathTrackingComponent? deathComponent = instigator == null ? null : PlayerDeathTrackingComponent.GetOrAdd(instigator.UnturnedPlayer);
        
        
        switch (e.InstantaneousDamageOrigin)
        {
            default:
                tracker.UpdateLatestInstigatorWeapon(null);
                break;

            // Throwable
            case EDamageOrigin.Grenade_Explosion:
                ThrowableComponent? throwable = deathComponent?.ActiveThrownItems.Find(x => x.Throwable is { isExplosive: true });
                tracker.UpdateLatestInstigatorWeapon(throwable?.Throwable);
                break;

            // Projectile
            case EDamageOrigin.Rocket_Explosion:
                tracker.UpdateLatestInstigatorWeapon(deathComponent?.LastRocketShot?.GetAsset());
                break;

            // Another vehicle exploding damaged this vehicle
            case EDamageOrigin.Vehicle_Explosion:
                tracker.UpdateLatestInstigatorWeapon(deathComponent?.LastVehicleExploded?.Asset);
                break;

            // Instant damage that comes from a useable.
            case EDamageOrigin.Bullet_Explosion:
            case EDamageOrigin.Useable_Gun:
            case EDamageOrigin.Useable_Melee:
                ItemAsset? equippedGun = instigator?.UnturnedPlayer.equipment.asset;
                tracker.UpdateLatestInstigatorWeapon(equippedGun);
                break;

            // Eplosive chewing gum
            case EDamageOrigin.Food_Explosion:
                tracker.UpdateLatestInstigatorWeapon(deathComponent?.LastExplosiveConsumed?.GetAsset());
                break;

            // C4 Charge (Charge_Self_Destruct is the damage used to destroy a charge after it detonates, irrelevant here)
            case EDamageOrigin.Charge_Explosion:
                tracker.UpdateLatestInstigatorWeapon(deathComponent?.LastChargeDetonated?.GetAsset());
                break;

            // Landmines
            case EDamageOrigin.Trap_Explosion:
                WarfarePlayer? triggerer = null;
                CSteamID barricadeGroupId = default;

                bool setWeapon = false;

                // find exploding landmine from other players to find who triggered the landmine
                // TriggeredTrapExplosive gets set to null right after the explosion so there can only ever be one active at once
                foreach (SteamPlayer player in Provider.clients)
                {
                    PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(player.player);
                    if (comp.TriggeredTrapExplosive == null)
                        continue;

                    triggerer = _playerService.GetOnlinePlayer(player);
                    tracker.UpdateLatestInstigatorWeapon(comp.TriggeredTrapExplosive.asset);
                    barricadeGroupId = new CSteamID(comp.TriggeredTrapExplosive.GetServersideData().group);
                    setWeapon = true;
                    break;
                }

                if (!setWeapon)
                {
                    if (deathComponent?.OwnedTrap is { } trap)
                    {
                        tracker.UpdateLatestInstigatorWeapon(trap.asset);
                        barricadeGroupId = new CSteamID(trap.GetServersideData().group);
                    }
                    else
                    {
                        tracker.UpdateLatestInstigatorWeapon(null);
                    }
                }

                // lay fault onto the triggerer if they were on the same team as the player that placed the landmine
                if (triggerer != null && triggerer.Team.IsFriendly(barricadeGroupId))
                {
                    tracker.RecordDamage(triggerer.Steam64, e.PendingDamage, EDamageOrigin.Trap_Explosion, true);
                }
                else
                {
                    tracker.RecordDamage(instigator64, e.PendingDamage, EDamageOrigin.Trap_Explosion, false);
                }

                break;
        }
    }
}
