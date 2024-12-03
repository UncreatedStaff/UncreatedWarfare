using System.Collections.Generic;
using Uncreated.Warfare.Layouts.Teams;

namespace Uncreated.Warfare.Layouts.Flags;
public interface IFlagRotationService
{
    public IReadOnlyList<FlagObjective> ActiveFlags { get; }
    public FlagObjective? GetObjective(Team team);
}
