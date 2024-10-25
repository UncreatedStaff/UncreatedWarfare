using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Events.Models.Squads;
/// <summary>
/// Event listener args which fires after a <see cref="Squad"/> is created.
/// </summary>
internal class SquadCreated
{
    /// <summary>
    /// The <see cref="Squad"/> that was created.
    /// </summary>
    public required Squad Squad { get; init; }
}
