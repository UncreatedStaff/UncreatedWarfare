using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a player joines a <see cref="Squad"/>.
/// </summary>
[EventModel(SynchronizedModelTags = [ "squads" ])]
public class SquadMemberJoined : SquadUpdated, IPlayerEvent
{
    /// <summary>
    /// The player that joined the squad.
    /// </summary>
    public required WarfarePlayer Player { get; init; }

    /// <summary>
    /// The ID of the player that joined the squad.
    /// </summary>
    public CSteamID Steam64 => Player.Steam64;

    /// <summary>
    /// <see langword="true"/> if this event was invoked because <see cref="Player"/> created a new squad and is the squad leader, otherwise <see langword="false"/>.
    /// </summary>
    public bool IsNewSquad { get; init; } = false;
}
