using Uncreated.Warfare.Players;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Events.Models.Zones;
/// <summary>
/// Event listener args which fires after a <see cref="WarfarePlayer"/> leaves a <see cref="Zone"/>.
/// </summary>
/// <remarks>NOTE: this event is not invoked when a player leaves a <see cref="FlagObjective"/>, as it has its own events for the same purpose.</remarks>
public class PlayerExitedZone : PlayerEvent
{
    /// <summary>
    /// The zone that the player left.
    /// </summary>
    public required Zone Zone { get; init; }
}