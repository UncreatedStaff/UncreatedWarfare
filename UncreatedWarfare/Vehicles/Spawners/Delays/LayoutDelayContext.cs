using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;
public class LayoutDelayContext
{
    public Layout CurrentLayout { get; }
    public LayoutRole? AffectedTeam { get; }

    public LayoutDelayContext(Layout currentLayout, LayoutRole affectedTeam = LayoutRole.NotApplicable)
    {
        CurrentLayout = currentLayout;
        AffectedTeam = affectedTeam;
    }
}
