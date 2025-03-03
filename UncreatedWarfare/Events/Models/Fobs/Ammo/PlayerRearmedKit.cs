using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs.Ammo;

/// <summary>
/// Event listener args which fires after a player their kit rearms from a <see cref="IAmmoStorage"/>.
/// </summary>
public class PlayerRearmedKit : PlayerEvent
{
    /// <summary>
    /// The amount of ammo that was consumed from the <see cref="IAmmoStorage"/>.
    /// </summary>
    public required float AmmoConsumed { get; init; }
    
    /// <summary>
    /// The <see cref="IAmmoStorage"/> that was used to rearm the player's kit.
    /// </summary>
    public required IAmmoStorage AmmoStorage { get; init; }
}