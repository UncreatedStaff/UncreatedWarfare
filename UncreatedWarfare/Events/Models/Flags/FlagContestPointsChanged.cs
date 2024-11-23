using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;
/// <summary>
/// Event listener args which fires after a flag's <see cref="FlagObjective.Contest"/>'s points change are altered.
/// </summary>
internal class FlagContestPointsChanged
{
    /// <summary>
    /// The flag that is being contested.
    /// </summary>
    public required FlagObjective Flag { get; init; }
    /// <summary>
    /// The change in contest points.
    /// </summary>
    public required int PointsChange { get; init; }
}
