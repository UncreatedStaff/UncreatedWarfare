using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a <see cref="Squad"/> is created.
/// </summary>
[EventModel(SynchronizedModelTags = [ "squads" ])]
public class SquadCreated : SquadUpdated, IPlayerEvent
{
    /// <summary>
    /// The player that created the squad.
    /// </summary>
    public required WarfarePlayer Player { get; init; }

    /// <summary>
    /// The ID of the player that created the squad.
    /// </summary>
    public CSteamID Steam64 => Player.Steam64;
}