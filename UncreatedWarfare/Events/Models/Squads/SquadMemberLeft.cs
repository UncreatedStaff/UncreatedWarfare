using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a player leaves a <see cref="Squad"/>.
/// </summary>
[EventModel(SynchronizedModelTags = [ "squads" ])]
public class SquadMemberLeft : SquadUpdated, IPlayerEvent
{
    /// <summary>
    /// The player that left the squad.
    /// </summary>
    public required WarfarePlayer Player { get; init; }

    /// <summary>
    /// The ID of the player that left the squad.
    /// </summary>
    public CSteamID Steam64 => Player.Steam64;

    /// <summary>
    /// <see langword="true"/> if this event was invoked because the <see cref="Squad"/> was forcibly disbanded causing all members to leave, otherwise <see langword="false"/>.
    /// </summary>
    public bool IsForciblyDisbanded { get; init; } = false;
}
