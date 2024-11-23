using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after <see cref="WarfarePlayer"/> enteres the region of a <see cref="FlagObjective"/>.
/// </summary>
internal class PlayerEnteredFlagRegion : PlayerEvent
{
    /// <summary>
    /// The flag that the player entered.
    /// </summary>
    public required FlagObjective Flag { get; init; }
}
