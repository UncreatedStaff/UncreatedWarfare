using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;
/// <summary>
/// Event listener args which fires after a <see cref="Squad"/> is disbanded.
/// </summary>
internal class SquadDisbanded
{
    /// <summary>
    /// The <see cref="Squad"/> that was disbanded.
    /// </summary>
    public required Squad Squad { get; init; }
}
