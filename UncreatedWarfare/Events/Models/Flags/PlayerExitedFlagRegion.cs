﻿using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after <see cref="WarfarePlayer"/> exits the region of a <see cref="FlagObjective"/>.
/// </summary>
public class PlayerExitedFlagRegion : PlayerEvent
{
    /// <summary>
    /// The flag that the player exited.
    /// </summary>
    public required FlagObjective Flag { get; init; }
}