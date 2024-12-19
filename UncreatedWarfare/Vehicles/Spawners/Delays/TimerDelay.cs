using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;
public class TimerDelay : ILayoutDelay<LayoutDelayContext>
{
    public TimeSpan Timer { get; set; }
    public LayoutRole AffectedTeam { get; set; } = LayoutRole.NotApplicable;

    public TimeSpan GetTimeLeft(LayoutDelayContext context)
    {
        if (AffectedTeam != LayoutRole.NotApplicable && AffectedTeam != context.AffectedTeam)
            return TimeSpan.Zero;

        if (context.CurrentLayout.ActivePhase is ActionPhase actionPhase)
        {
            return Timer - actionPhase.TimeElapsedSinceActive;
        }

        return Timer;
    }
}
