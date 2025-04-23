using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;

/// <summary>
/// Event listener args which fires after a <see cref="Squad"/> is disbanded.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class SquadDisbanded : SquadUpdated
{
    public required WarfarePlayer PreviousOwner { get; init; }
    public required WarfarePlayer[] PreviousMembers { get; init; }
}