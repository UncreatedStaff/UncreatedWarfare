using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires at a slow interval for all <see cref="FlagObjective"/>s in the current layout that are at least one team's objective and have players.
/// </summary>
public class ObjectiveSlowTick
{
    /// <summary>
    /// The flag objective being ticked. Will always be a team's active objective.
    /// </summary>
    public required FlagObjective Flag { get; init; }

    /// <summary>
    /// The current <see cref="FlagContestState"/> of the flag. Its state will never be <see cref="FlagContestState.ContestState.NotObjective"/> or <see cref="FlagContestState.ContestState.NoPlayers"/>
    /// </summary>
    public required FlagContestState ContestState { get; init; }
}
