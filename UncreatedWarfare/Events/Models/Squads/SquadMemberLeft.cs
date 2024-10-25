using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;
/// <summary>
/// Event listener args which fires after a player leaves a squad.
/// </summary>
internal class SquadMemberLeft
{
    /// <summary>
    /// The squad that the left joined.
    /// </summary>
    public required Squad Squad { get; init; }
    /// <summary>
    /// The player that left the squad.
    /// </summary>
    public required WarfarePlayer Player { get; init; }
    /// <summary>
    /// <see langword="true"/> if this event was invoked because the <see cref="Squad"/> was forcibly disbanded causing all members to leave, otherwise <see langword="false"/>.
    /// </summary>
    public bool IsForciblyDisbanded { get; init; } = false;
}
