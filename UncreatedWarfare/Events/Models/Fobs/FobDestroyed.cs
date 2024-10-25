using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after the main structure of any <see cref="BasePlayableFob"/> subclass is destroyed.
/// </summary>
internal class FobDestroyed
{
    /// <summary>
    /// The <see cref="BasePlayableFob"/> that was destroyed.
    /// </summary>
    public required BasePlayableFob Fob { get; init; }
}
