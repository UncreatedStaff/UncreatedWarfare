using Microsoft.Extensions.Configuration;
using System;

namespace Uncreated.Warfare.Layouts.Phases.Flags;
public class FlagActionPhaseLayout : BasePhase<PhaseTeamSettings>
{
    public FlagActionPhaseLayout(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {

    }
}