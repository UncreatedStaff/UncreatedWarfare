using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after a <see cref="BuildableFob"/> is built up.
/// </summary>
internal class FobBuilt
{
    /// <summary>
    /// The <see cref="BuildableFob"/> that was built up.
    /// </summary>
    public required BuildableFob Fob { get; init; }
}
