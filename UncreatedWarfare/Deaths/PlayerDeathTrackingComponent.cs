using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Deaths;
internal class PlayerDeathTrackingComponent : MonoBehaviour
{
    /// <summary>
    /// The player this component is a part of.
    /// </summary>
    public Player Player { get; private set; }

    /// <summary>
    /// Info about how a player began bleeding out most recently. Cleared when bleeding stops.
    /// </summary>
    internal PlayerDied? BleedOutInfo { get; set; }

    /// <summary>
    /// The last explosive trap that this player triggered.
    /// </summary>
    internal BarricadeDrop? TriggeredTrapExplosive { get; set; }

    /// <summary>
    /// The last trap that was set off that this player originally placed.
    /// </summary>
    internal BarricadeDrop? OwnedTrap { get; set; }

    /// <summary>
    /// The last item this player consumed that can cause virus infection.
    /// </summary>
    internal IAssetLink<ItemConsumeableAsset>? LastInfectionItemConsumed { get; set; }

    /// <summary>
    /// The last vehicle that ran over this player.
    /// </summary>
    internal IAssetLink<VehicleAsset>? LastRoadkillVehicle { get; set; }

    /// <summary>
    /// The last barricade to 'shred' the player.
    /// </summary>
    internal IAssetLink<ItemBarricadeAsset>? LastShreddedBy { get; set; }

    /// <summary>
    /// The last charge barricade to be detonated by this player.
    /// </summary>
    internal IAssetLink<ItemBarricadeAsset>? LastChargeDetonated { get; set; }

    /// <summary>
    /// The last explosive consumable to be consumed by this player.
    /// </summary>
    internal IAssetLink<ItemConsumeableAsset>? LastExplosiveConsumed { get; set; }

    /// <summary>
    /// The last gun (projectiles only) that this player shot.
    /// </summary>
    internal IAssetLink<ItemGunAsset>? LastRocketShot { get; set; }

    /// <summary>
    /// The vehicle from which <see cref="LastRocketShot"/> was shot (only if it's a turret).
    /// </summary>
    internal IAssetLink<VehicleAsset>? LastRocketShotFromVehicle { get; set; }

    /// <summary>
    /// The driver of the vehicle from which <see cref="LastRocketShot"/> was shot (only if it's a turret).
    /// </summary>
    internal CSteamID? LastRocketShotFromVehicleDriverAssist { get; set; }

    /// <summary>
    /// The last player to attack this player.
    /// </summary>
    internal CSteamID LastAttacker { get; private set; }
    
    /// <summary>
    /// The second-to-last player to attack this player.
    /// </summary>
    internal CSteamID SecondLastAttacker { get; private set; }
    
    /// <summary>
    /// The time the last player attacked this player and moved the second-to-last player down.
    /// </summary>
    internal DateTime SecondLastAttackerTimeReplaced { get; private set; }

    /// <summary>
    /// The throwable that was used to trigger <see cref="TriggeredTrapExplosive"/>.
    /// </summary>
    internal ThrowableComponent? ThrowableTrapTrigger { get; set; }

    /// <summary>
    /// The component of the last vehicle this player caused the explosion for.
    /// </summary>
    internal VehicleComponent? LastVehicleExploded { get; set; }

    /// <summary>
    /// List of all throwables that this player has thrown which are pending being cleaned up by the game.
    /// </summary>
    internal List<ThrowableComponent> ActiveThrownItems = new List<ThrowableComponent>(4);

    /// <summary>
    /// Get the existing component, or create a new one and set it up.
    /// </summary>
    public static PlayerDeathTrackingComponent GetOrAdd(Player player)
    {
        GameThread.AssertCurrent();

        if (player.TryGetComponent(out PlayerDeathTrackingComponent component))
        {
            return component;
        }

        component = player.gameObject.AddComponent<PlayerDeathTrackingComponent>();
        component.Player = player;

        return player.gameObject.GetOrAddComponent<PlayerDeathTrackingComponent>();
    }

    /// <summary>
    /// Move <see cref="LastAttacker"/> down to <see cref="SecondLastAttacker"/> and set <see cref="LastAttacker"/> to <paramref name="newLastAttacker"/>.
    /// </summary>
    public void TryUpdateLastAttacker(CSteamID newLastAttacker)
    {
        if (newLastAttacker.m_SteamID == LastAttacker.m_SteamID) return;

        SecondLastAttackerTimeReplaced = DateTime.UtcNow;
        SecondLastAttacker = LastAttacker;
        LastAttacker = newLastAttacker;
    }

    /// <summary>
    /// Reset <see cref="LastAttacker"/> and <see cref="SecondLastAttacker"/>.
    /// </summary>
    public void ResetAttackers()
    {
        LastAttacker = CSteamID.Nil;
        SecondLastAttacker = CSteamID.Nil;
        SecondLastAttackerTimeReplaced = default;
    }
}
