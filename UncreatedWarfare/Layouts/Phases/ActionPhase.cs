using Microsoft.Extensions.Configuration;
using System;

namespace Uncreated.Warfare.Layouts.Phases;
public class ActionPhase : BasePhase<PhaseTeamSettings>
{
    public ActionPhase(IServiceProvider serviceProvider, IConfiguration config) : base(serviceProvider, config)
    {

    }
}