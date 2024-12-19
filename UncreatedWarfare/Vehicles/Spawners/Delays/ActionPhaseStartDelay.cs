using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;
public class ActionPhaseStartDelay : ILayoutDelay
{
    public TimeSpan StartDelay { get; set; }

    public ActionPhaseStartDelay(TimeSpan startDelay)
    {
        StartDelay = startDelay;
    }
    public TimeSpan GetTimeLeft(Layout currentLayout)
    {
        if (currentLayout.ActivePhase is ActionPhase actionPhase)
        {
            return StartDelay - actionPhase.TimeElapsedSinceActive;
        }

        return StartDelay;
    }
}
