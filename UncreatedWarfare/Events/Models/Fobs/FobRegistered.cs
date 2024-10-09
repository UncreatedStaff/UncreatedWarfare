using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a FOB is registered.
/// </summary>
internal class FobRegistered
{
    /// <summary>
    /// The FOB that was registered.
    /// </summary>
    public required IFob Fob { get; init; }
}
