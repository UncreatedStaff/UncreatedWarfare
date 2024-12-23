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
    /// The current <see cref="FlagContestResult"/> of the flag. Its state will never be <see cref="FlagContestResult.ContestState.NotObjective"/> or <see cref="FlagContestResult.ContestState.NoPlayers"/>
    /// </summary>
    public required FlagContestResult ContestResult { get; init; }
}
