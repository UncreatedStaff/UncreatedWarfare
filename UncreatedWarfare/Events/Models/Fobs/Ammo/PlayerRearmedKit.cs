using System;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Events.Models.Fobs.Ammo;

/// <summary>
/// Event listener args which fires after a player their kit rearms from a <see cref="IAmmoStorage"/>.
/// </summary>
public class PlayerRearmedKit : PlayerEvent, IActionLoggableEvent
{
    /// <summary>
    /// The amount of ammo that was consumed from the <see cref="IAmmoStorage"/>.
    /// </summary>
    public required float AmmoConsumed { get; init; }
    
    /// <summary>
    /// The <see cref="IAmmoStorage"/> that was used to rearm the player's kit.
    /// </summary>
    public required IAmmoStorage AmmoStorage { get; init; }

    /// <summary>
    /// The kit that is being rearmed, included up to <see cref="KitInclude.Giveable"/>.
    /// </summary>
    public required Kit Kit { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.KitRearmed,
            $"Kit {Kit.Key} ({Kit.Id}) rearmed for {AmmoConsumed:F2} ammo supplies from {AmmoStorage.Owner}'s ammo storage: {AmmoStorage}",
            Steam64.m_SteamID
        );
    }
}