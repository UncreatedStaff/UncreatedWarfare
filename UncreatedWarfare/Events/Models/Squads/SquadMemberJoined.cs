using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;
/// <summary>
/// Event listener args which fires after a player joines a squad.
/// </summary>
internal class SquadMemberJoined
{
    /// <summary>
    /// The squad that the player joined.
    /// </summary>
    public required Squad Squad { get; init; }
    /// <summary>
    /// The player that joined the squad.
    /// </summary>
    public required WarfarePlayer Player { get; init; }
    /// <summary>
    /// <see langword="true"/> if this event was invoked because <see cref="Player"/> created a new squad and is the squad leader, otherwise <see langword="false"/>.
    /// </summary>
    public bool IsNewSquad { get; init; } = false;
}
