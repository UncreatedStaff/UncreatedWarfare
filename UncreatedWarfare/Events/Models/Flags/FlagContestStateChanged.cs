using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a flag's <see cref="FlagObjective.CurrentContestState"/> changes to a different <see cref="FlagContestState.ContestState"/>.
/// </summary>
public class FlagContestStateChanged
{
    /// <summary>
    /// The flag that is being contested.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The flag's contest state before the change.
    /// </summary>
    public required FlagContestState OldState { get; init; }
    /// <summary>
    /// The flag's contest state after the change.
    /// </summary>
    public required FlagContestState NewState { get; init; }
}
