using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;
/// <summary>
/// Event listener args which fires after <see cref="WarfarePlayer"/> exits the region of a <see cref="FlagObjective"/>.
/// </summary>
internal class PlayerExitedFlagRegion : PlayerEvent
{
    /// <summary>
    /// The flag that the player exited.
    /// </summary>
    public required FlagObjective Flag { get; init; }
}
