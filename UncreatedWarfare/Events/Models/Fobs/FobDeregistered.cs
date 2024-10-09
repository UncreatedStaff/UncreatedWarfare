using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a FOB is deregistered.
/// </summary>
internal class FobDeregistered
{
    /// <summary>
    /// The FOB that was deregistered.
    /// </summary>
    public required IFob? Fob { get; init; }
}
