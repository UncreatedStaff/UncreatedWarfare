using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts.Phases.Flags;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Layouts.Flags;
public interface IFlagRotationService
{
    public IReadOnlyList<FlagObjective> ActiveFlags { get; }
    public FlagObjective? GetObjective(Team team);
}
