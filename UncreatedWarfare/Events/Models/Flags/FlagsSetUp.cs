﻿using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.Events.Models.Flags;

/// <summary>
/// Event listener args which fires after a <see cref="DualSidedFlagService"/> finishes building and setting up a new flag layout.
/// </summary>
public class FlagsSetUp
{
    /// <summary>
    /// A list of capturable <see cref="FlagObjective"/> created by the flag service. This list does not include Main Base locations.
    /// </summary>
    public required IReadOnlyList<FlagObjective> ActiveFlags { get; init; }
    /// <summary>
    /// The Flag Service that was responsible for creating this flag layout and dispatching this event.
    /// </summary>
    public required DualSidedFlagService FlagService { get; init; }
}